<!-- Source: docs/wiki/WSAA-y-certificados.md. Generated wiki mirror; edit the repository source. -->

# WSAA y certificados

WSAA autoriza a un certificado para un servicio concreto y ambiente concreto. El
flujo NetArcaWs genera el TRA, firma CMS, invoca `loginCms`, analiza el TA y
reutiliza el ticket válido. La API es asíncrona y permite cancelación.

## Material del certificado

`WsaaCertificateContent` representa certificado y clave sin requerir una ruta:

```csharp
var pem = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem, password);
var pfx = WsaaCertificateContent.FromPkcs12(pkcs12Bytes, password);
var base64 = WsaaCertificateContent.FromPkcs12Base64(pkcs12Base64, password);
```

Base64 solo representa los bytes; no cifra el PFX. La aplicación carga secretos
desde su propia configuración protegida, vault o base de datos. NetArcaWs no
incluye SDKs de vault. `WsaaOptions.Certificate` se captura al crear el servicio
singleton. Para una identidad individual y una rotación por operación, cargar el
contenido actualizado y usar `AuthenticateWithContentAsync`; para seleccionar un
tenant utilizar [Contexto multitenant](Contexto-multitenant).

`LoadCertificate()` devuelve un `X509Certificate2` que debe liberar quien lo
llama. Las operaciones WSAA que reciben el contenido importan y liberan su propio
certificado. El objeto de contenido es inmutable y oculta el secreto en
`ToString`, pero strings PEM y valores administrados no garantizan borrado
inmediato de memoria.

La importación PFX usa `EphemeralKeySet` en Windows y Linux y no tiene fallback a
disco; .NET no admite esa importación efímera en macOS, donde la aplicación debe
usar PEM. La ruta PEM no escribe archivos temporales explícitos. .NET usa el
proveedor criptográfico del sistema operativo, así que la biblioteca no afirma
que el proveedor jamás use keychain u otro almacenamiento interno. Ver la
[documentación multiplataforma de .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography).

## Firma, endpoint y caché

La firma actual usa CMS attached con RSA/SHA-256. El cliente SOAP WSAA usa el
contrato guardado en `docs/reference/wsaa-homologation.wsdl`; el transporte
valida TLS normal de .NET y no registra los mensajes SOAP. Los errores SOAP
exponen campos tipados; los detalles del proveedor pueden contener datos que no
deben escribirse sin filtrar.

La clave de caché incluye endpoint, huella SHA-256 del certificado y servicio.
Rotar a otro certificado separa la entrada; el caché `IMemoryCache` se pierde al
reiniciar y no se distribuye entre réplicas. La biblioteca no renueva un ticket
anticipadamente ni reintenta automáticamente un login fallido. Compartir
certificado entre procesos puede provocar `coe.alreadyAuthenticated`.

WSAA no publica un método `dummy`. El check separado de WSAA comprueba HTTP/WSDL
únicamente; no valida firma, certificado ni autorización.

## Preparar una CSR

El CLI `netarcaws cert-dev` y `cert-prod` generan clave privada nueva y CSR; no
emiten ni habilitan un certificado ARCA. La [guía del CLI](Herramienta-de-certificados)
describe instalación local, protección de archivos y trámites oficiales. El
procedimiento vigente está en [Certificados digitales de ARCA](https://www.arca.gob.ar/ws/programadores/certificados-digitales.asp).
