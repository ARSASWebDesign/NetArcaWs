# ADR 0003: Certificados en memoria y origen de secretos

- Estado: Aceptado e implementado para WSAA y las fachadas autenticadas actuales;
  suite local verificada y homologación autenticada pendiente de certificados
- Fecha: 2026-10-06
- Complementa: [ADR 0002: contexto multitenant](0002-arca-tenant-context.md)

## Contexto

Una aplicación puede guardar certificados y claves en un vault, variables de
entorno o una base de datos compartida entre instancias. Exigir rutas de archivos
obligaría a materializar secretos en cada servidor y complicaría la selección por
tenant y la rotación. El origen del secreto no debe condicionar el cliente SOAP.

## Decisión

`WsaaCertificateContent` recibe contenido, no rutas:

| Entrada | API | Datos necesarios |
| --- | --- | --- |
| PEM | `FromPem` | Certificado, clave privada y contraseña si está cifrada |
| PFX/P12 binario | `FromPkcs12` | Bytes y contraseña si corresponde |
| PFX/P12 en Base64 | `FromPkcs12Base64` | Texto Base64 y contraseña si corresponde |

La aplicación consulta su almacén, resuelve y autoriza la identidad y entrega el
contenido. La biblioteca no incluye un SDK específico de vault ni acceso a bases
de datos. Base64 es una representación, no un mecanismo de cifrado.

Para una identidad fija, `WsaaOptions.Certificate` configura el contenido y
`AuthenticateAsync(service, cancellationToken)` lo utiliza. El singleton captura
esa referencia inmutable al crearse; no implementa recarga de opciones en vivo.
Para rotación explícita de una identidad, se construye un nuevo contenido y se usa
`AuthenticateWithContentAsync`. Para una aplicación multitenant, la ruta requerida
es `ArcaTenantContext` y `AuthenticateForTenantAsync`: pasar solo un certificado
no expresa el aislamiento de tenant y CUIT.

Los métodos de autenticación que reciben contenido importan y liberan su propio
`X509Certificate2` por operación. Si el consumidor usa `LoadCertificate()` o la
sobrecarga que recibe `X509Certificate2`, administra él su `Dispose`. Los bytes de
entrada PFX se copian para evitar mutaciones externas. El contenido no expone
propiedades con secretos y su `ToString()` los oculta; no deben registrarse los
valores originales del almacén. Los strings administrados y el contenido retenido
no garantizan borrado inmediato de memoria: limitar su vida útil sigue siendo
responsabilidad de quien los almacena.

La importación PFX usa exclusivamente `EphemeralKeySet`, sin alternativa que
persista la clave. En macOS esa modalidad no está soportada por .NET y se rechaza;
se debe suministrar PEM. La biblioteca no crea archivos temporales para PEM,
pero la implementación criptográfica del sistema operativo conserva sus propias
semánticas de almacenamiento. Ver la [documentación oficial de .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography).

## Alternativas consideradas

- Exigir archivos locales: conserva un flujo familiar, pero no cubre vaults ni
  bases de datos sin materialización adicional. El consumidor todavía puede leer
  sus archivos y entregar el contenido si ese es su modelo de despliegue.
- Incluir SDKs de proveedores: acoplaría el paquete a decisiones de infraestructura
  de cada aplicación. El contrato de contenido permite cualquiera de ellos.
- Cambiar una credencial global por request: introduce carreras entre tenants.
  Se utiliza un contexto inmutable por operación.

## Consecuencias y límites

La misma credencial puede obtenerse desde un almacén compartido por varias
instancias. Eso **no comparte el TA ni coordina los logins**: IMemoryCache y sus
semáforos siguen siendo locales al proceso. Una solución distribuida de tickets
requiere un diseño e implementación adicionales; no forma parte de esta entrega.

Una rotación que cambia el certificado separa la caché por su huella; cambiar
solo la representación del mismo certificado no crea una identidad nueva.
El certificado público sin su clave privada no permite firmar. WSAA valida
vigencia y firma, mientras que ARCA determina la habilitación de los servicios.

Las fachadas autenticadas actuales de WSFEv1, WSFEXv1, WSMTXCA y Padrón reutilizan
este contrato y el contexto multitenant, sin exigir archivos ni cambiar opciones
globales. Suites y wire contracts se verificaron localmente; la homologación
autenticada sigue pendiente de certificados autorizados.

## Evidencia

Las suites `WsaaCertificateContentTests` y `ArcaTenantContextTests` verifican
formatos, claves y contraseñas, importación por plataforma, copia de bytes,
ausencia de secretos en representaciones públicas, rotación y aislamiento de
firmas/tickets bajo concurrencia. Las pruebas SOAP son simuladas: no sustituyen
la homologación con certificados autorizados por ARCA.
