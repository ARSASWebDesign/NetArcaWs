# Arquitectura del Hito 1

La biblioteca `src/NetArcaWs` apunta a net10.0. Las pruebas se separan en
`NetArcaWs.UnitTests` y `NetArcaWs.IntegrationTests`. No requiere un host ASP.NET;
las dependencias Microsoft se distribuyen como paquetes NuGet.

## Flujo

1. `AuthenticateAsync` valida servicio, el certificado configurado en opciones y
   su vigencia; `AuthenticateWithContentAsync` permite pasar contenido distinto
   por operación. Ambos consultan IMemoryCache usando endpoint, certificado y
   servicio como identidad.
2. Si falta un TA vigente, espera un semáforo compartido por las instancias
   que usan esa caché y vuelve a consultarla. Un conjunto fijo de 64 semáforos
   evita acumular locks por cada clave; colisiones solo serializan llamadas.
3. `CreateTra` serializa un TRA UTF-8, con uniqueId uint32 y fechas UTC. El TTL
   simétrico conserva el comportamiento de `create_tra` y es configurable.
4. `SignTra` usa SignedCms, RSA/SHA-256 y contenido encapsulado. Solo incluye el
   certificado firmante. AsnWriter codifica el CSR PKCS#10 y su DN.
5. `LoginCmsAsync` obtiene un HttpClient de la fábrica y llama SOAP 1.1
   document/literal, con SOAPAction vacío y namespaces del WSDL oficial.
6. XmlSerializer transforma el envelope y el TA en DTOs. Se prohíben DTD,
   se limita la respuesta y se valida estructura, fechas, campos y expiración.
7. El TA inmutable se guarda hasta expirationTime. No se cachean errores ni
   se renueva anticipadamente. Todos los caminos liberan el semáforo.

## Integración y límites

El contrato compilado está basado en el WSDL guardado en
`docs/reference/wsaa-homologation.wsdl`; no se descarga ni genera código en cada
llamada. Los DTOs públicos existen para XmlSerializer; la API de negocio es
WsaaService/WsaaTicket. Los prefijos XML son irrelevantes, las URI no.

TimeProvider permite comprobar expiraciones sin esperas. IHttpClientFactory
administra handlers y conexiones; el cliente se libera por operación. Si la
aplicación obtiene un `X509Certificate2` con `LoadCertificate()`, quien llama
es responsable de desecharlo. La aplicación puede configurar proxy/TLS mediante
el builder de DI. No hay bypass TLS ni ejecución de procesos externos.

### Certificados en memoria

`WsaaCertificateContent` encapsula de forma inmutable el material de certificado
y clave privada sin exponerlo en `ToString`. `FromPem(certificatePem,
privateKeyPem, password)` requiere certificado y clave; `FromPkcs12(data,
password)` acepta bytes PFX y `FromPkcs12Base64(base64, password)` decodifica
Base64 en memoria. Base64 solo representa los bytes y no cifra por sí mismo el
PFX. `LoadCertificate()` devuelve un `X509Certificate2` que es propiedad del
llamador y este debe liberar.

Las aplicaciones pueden cargar certificados y claves de sus propios almacenes
de manera asíncrona y pasar el contenido resultante. NetArcaWs no incorpora
integraciones ni SDK de vault, y no crea archivos temporales como alternativa
de importación. El contenido configurado en `WsaaOptions.Certificate` se
copia al crear el singleton del servicio; esa opción no sigue cambios posteriores
del proveedor de configuración. Para una rotación explícita de una identidad
individual, el llamador crea el contenido actualizado y lo pasa a
`AuthenticateWithContentAsync(service, content, cancellationToken)` en cada
operación. La ruta de opciones permite
`AuthenticateAsync(service, cancellationToken)` con la credencial configurada.

La importación PKCS#12 usa `EphemeralKeySet` en Windows y Linux para mantener la
clave fuera del almacén persistente. La implementación de .NET no admite esa
importación efímera de PFX en macOS; allí la aplicación debe proporcionar PEM.
No hay fallback a disco. La ruta PEM no escribe archivos temporales explícitos y
no necesita que la aplicación materialice archivos; al importar, .NET usa el
proveedor criptográfico del sistema operativo, por lo que la biblioteca no promete
que ese proveedor nunca use un keychain u otro backing storage del sistema. Las
operaciones criptográficas dependen de los proveedores del sistema operativo y sus
capacidades, como describe la
[documentación multiplataforma de .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography).

La caché TA sigue identificando la credencial por endpoint, huella SHA-256 del
certificado y servicio. Una rotación cambia la huella y separa la entrada; usar
un certificado en más de una instancia no proporciona una caché distribuida ni
coordinación entre procesos. La aplicación debe elegir y proteger su propia
estrategia de persistencia compartida si la necesita.

### Contexto de tenant para WSAA

`ArcaTenantContext` es un valor inmutable con `TenantId`, `Cuit`, `Environment`
(`NetArcaWs.HealthChecks.ArcaEnvironment`) y `Certificate` (`WsaaCertificateContent`).
`WsaaService.AuthenticateForTenantAsync(service, tenant, cancellationToken)` usa el
ambiente del contexto para seleccionar el endpoint oficial sin cambiar el estado de
`WsaaOptions` compartido. La clave local de caché agrega tenant y CUIT a endpoint,
servicio y huella SHA-256 del certificado; así una misma biblioteca singleton puede
atender contextos distintos sin mezclar sus tickets locales.

El contexto no autentica ni autoriza quién puede seleccionar un tenant. La capa de
aplicación debe obtener tenant, CUIT representado, ambiente y contenido del
certificado desde una resolución autenticada y autorizada; nunca debe tratar valores
libres de un request como contexto confiable. El CUIT representado puede no ser el
del titular del certificado cuando existe una delegación vigente de ARCA, por lo
que el sistema no debe exigir que ambos coincidan. `AuthenticateWithContentAsync`
sigue siendo una primitiva de llamada por contenido, pero no sustituye el contexto
para aislamiento multitenant.

El tenant separa entradas de caché locales, no la identidad que ARCA ve en el
certificado. Reutilizar una misma credencial en dos contextos puede producir
`coe.alreadyAuthenticated` y no promete sesiones remotas independientes. WSFEv1,
WSFEXv1, WSMTXCA y Padrones todavía no implementan clientes de negocio; cuando se
agreguen, cada operación de esos servicios debe recibir el contexto resuelto para
elegir certificado, CUIT y ambiente de forma coherente. Los health checks siguen
siendo probes de infraestructura por servicio/ambiente: no usan contexto ni
certificados y no verifican permiso fiscal del tenant.

La futura autorización de facturas también debe mantener la clave fiscal global
definida en [ADR 0001](docs/adr/0001-safe-invoice-retries.md): ambiente, CUIT
emisora, punto de venta, tipo y número. No se debe agregar tenant como parte de esa
unicidad. Dos tenants que operan la misma CUIT no pueden registrar dos veces el
mismo comprobante; el límite por tenant controla acceso y visibilidad, no duplica
identidad fiscal.

Un token enlazado aplica el timeout del cliente al envío y a toda la lectura
del cuerpo, también con ResponseHeadersRead. La cancelación del consumidor se
conserva; un vencimiento propio se comunica como TaskCanceledException con
TimeoutException interna.

La lectura de claves tradicionales RSA AES-256-CBC conserva compatibilidad con
las generadas por PyAfipWs. Solo esa importación legacy usa EVP_BytesToKey/MD5;
las nuevas claves usan PKCS#8/PBKDF2-SHA256 y las firmas usan RSA/SHA256. Los
buffers privados de descifrado se limpian después del uso. Los strings PEM y
credenciales son administrados y no garantizan borrado de memoria.

El TA contiene secretos. Su ToString los oculta, pero las propiedades Token,
Sign y Xml siguen siendo sensibles. El servicio no registra mensajes SOAP.
No hay reintentos automáticos ni persistencia distribuida. Reinicios, expulsión
de entradas y procesos independientes pueden perder un TA todavía válido en
ARCA; el error correspondiente no debe ocultarse ni tratarse como éxito.

## Agentes y validación

Luna Cripto desarrolló las operaciones criptográficas; Luna DevOps estructuró
proyectos, empaquetado y licencias; Luna Testing escribió las pruebas. El
coordinador implementó el servicio e integró los componentes. El rol Luna
Integración revisó el contrato SOAP y la caché en una segunda tarea del agente
Cripto, debido al límite de threads del entorno.

Las pruebas comprueban CMS y CSR con primitivas independientes de verificación,
simulan HTTP/SOAP y controlan el reloj. La suite real queda separada y requiere
certificados autorizados. Compilar y pasar mocks no demuestra aceptación por
ARCA. El inventario y las diferencias con Python están documentados en
`docs/plans/hito-1.md`; no se declara compatibilidad binaria/COM ni paridad de
todos los módulos del repositorio original.

## Health checks opt-in

`NetArcaWs.HealthChecks` implementa `IHealthCheck` sin dependencia de ASP.NET ni
certificados. `AddHealthChecks().AddWsfev1HealthCheck(...)` registra únicamente
WSFEv1; los demás WS se agregan individualmente. Cada registro tiene nombre,
entorno, endpoint, timeout y tags propios. No hay un registro global implícito,
trabajo en segundo plano ni una dependencia del cliente WSAA.

La fábrica HttpClient administra conexiones; cada ejecución tiene un token de
timeout que cubre envío y lectura. Los probes no se reintentan ni cachean: el
monitor decide cuándo volver a consultar. La cancelación del consumidor se
propaga y no se registra como una caída de ARCA. HTTP, TLS/red, XML inválido,
respuesta excesiva y estados no OK se convierten en Unhealthy con motivo acotado.

Los perfiles del protocolo son explícitos: WSFE/FEX tienen resultado y campos
calificados; Padrón devuelve `return` y campos sin namespace; MTXCA usa Body vacío
y campos directos. XmlSerializer y lectores sin DTD preservan esas diferencias.
WSAA verifica únicamente el WSDL, nunca solicita un ticket. Los detalles y fuentes
del contrato están en `docs/reference/healthchecks-contracts.md`.

## Reintentos de facturación

El diseño está en `docs/adr/0001-safe-invoice-retries.md`. Todavía no hay cliente
de emisión ni diario durable implementados. WSAA y los health checks actuales
no tienen retries automáticos. Un health check exitoso no cambia un resultado
fiscal incierto a rechazado ni autoriza a reenviar una factura.

En los próximos hitos, las autorizaciones deberán conservar identidad y payload
de negocio, persistir el estado y reconciliar tras resultados inciertos. Esa
política no se puede reemplazar por un retry genérico del transporte HTTP.

## Herramienta de certificados

`NetArcaWs.Tool` es un paquete .NET tool independiente que referencia la biblioteca.
`CertificateTool` separa la ejecución de comandos de consola y recibe salida,
errores y acceso al entorno para probarlos sin exponer secretos. Reutiliza la
criptografía nativa del port: RSA, CSR PKCS#10 y claves PKCS#8 cifradas. No ejecuta
procesos externos ni conecta con ARCA.

Los comandos exigen un destino nuevo. Escriben cuatro archivos en una carpeta
hermana temporal privada y publican el conjunto con Directory.Move. Los permisos
Unix son 0700/0600; Windows hereda las ACL del directorio padre. La contraseña se
obtiene de una variable de entorno, nunca de un argumento literal. El manifiesto
incluye la huella de la clave pública, nunca la clave privada ni su contraseña.

`cert-info` lee cada entrada desde un único handle con límite de 1 MiB, exige RSA,
comprueba vigencia local y opcionalmente la correspondencia de la clave privada.
No determina confianza, revocación ni habilitaciones de ARCA. Los strings PEM
administrados conservan la limitación de borrado de memoria descrita arriba.
La suite `NetArcaWs.Tool.Tests` valida comandos, CSR, claves, concurrencia,
permisos, cancelación y errores. CI empaqueta e instala la herramienta para
comprobar también su ejecución fuera de la solución.

## Documentación de decisiones y publicación

El [ADR 0003](docs/adr/0003-in-memory-certificates.md) formaliza el contrato de
certificados en memoria y complementa el contexto multitenant del ADR 0002.
El [hito final del wiki](docs/plans/hito-7-wiki.md) consolidará toda la
documentación y las decisiones, con trazabilidad hacia las fuentes de PyAfipWs
y una matriz explícita de compatibilidad y extras de NetArcaWs.
