# Arquitectura del Hito 1

La biblioteca `src/NetArcaWs` apunta a net10.0. Las pruebas se separan en
`NetArcaWs.UnitTests` y `NetArcaWs.IntegrationTests`. No requiere un host ASP.NET;
las dependencias Microsoft se distribuyen como paquetes NuGet.

## Flujo

1. `AuthenticateAsync` valida servicio, certificado y vigencia, y consulta
   IMemoryCache usando endpoint, certificado y servicio como identidad.
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
administra handlers y conexiones; el cliente se libera por operación y el
certificado sigue siendo responsabilidad de quien llama. La aplicación puede
configurar proxy/TLS mediante el builder de DI. No hay bypass TLS ni ejecución
de procesos externos.

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
