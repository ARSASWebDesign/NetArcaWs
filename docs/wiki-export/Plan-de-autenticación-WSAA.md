<!-- Source: docs/plans/hito-1.md. Generated wiki mirror; edit the repository source. -->

# Hito 1: WSAA nativo en .NET 10

## Contrato y decisiones

Se implementa el flujo WSAA de PyAfipWs con API C# asíncrona, DI, HttpClient,
XmlSerializer, SignedCms y AsnWriter. No se ejecutan procesos criptográficos ni
se incorporan bibliotecas criptográficas de terceros. Los métodos Python que
usan estado mutable se expresan como operaciones y tickets inmutables.

Referencia: `reingart/pyafipws`, revisión
`d595b072110accec9dae1ddb58165ab847b8520a`, archivo `wsaa.py`, SHA-256
`32b13f31b05b5aaa354986d8ce91383a1d68a89e0c94470bf1a78a75d137f135`.
WSDL de homologación consultado el 2026-10-06 y conservado en
`docs/reference/wsaa-homologation.wsdl`.

La especificación ARCA 1.2.2 describe RSA/SHA-1; el código Python moderno firma
con RSA/SHA-256. Este port sigue SHA-256 del upstream moderno. La aceptación con
certificados reales debe verificarse mediante la suite de homologación.

## Implementación por responsabilidad

1. Luna Cripto: CMS attached, claves RSA, PEM cifrado, CSR ASN.1, certificados.
2. Coordinador e Integración: TRA, contrato SOAP document/literal, TA, Faults,
   cancelación, tamaños máximos, caché y sincronización entre consumidores.
3. Luna Testing: pruebas independientes de cripto, SOAP, caché y homologación.
4. Luna DevOps: proyectos, paquetes, licencias y documentación de uso.
5. Revisión independiente de integración, ejecución de pruebas y creación NuGet.

## Equivalencias y cambios deliberados

| Python | API .NET / decisión |
|---|---|
| create_tra / CreateTRA | WsaaService.CreateTra, TTL simétrico configurable, reloj inyectable |
| sign_tra / SignTRA | WsaaCryptography.SignTra, CMS binario encapsulado SHA-256 |
| call_wsaa / CallWSAA / LoginCMS | WsaaService.LoginCmsAsync, transporte configurable por DI |
| Conectar | AddNetArcaWs + WsaaOptions.Endpoint + IHttpClientBuilder para proxy/TLS |
| AnalizarXml / ObtenerTagXml | ParseTicket / WsaaTicket.GetTag |
| Expirado | WsaaTicket.IsExpired, comparación con zona horaria e igualdad vencida |
| Autenticar | AuthenticateAsync, IMemoryCache por endpoint/certificado/servicio |
| AnalizarCertificado | AnalyzeCertificate / LoadPem |
| CrearClavePrivada | CreatePrivateKey; resultado PEM, el consumidor decide su persistencia |
| CrearPedidoCertificado | CreateCertificateRequest; resultado PEM |
| Excepcion / Traceback / SoapFault | Excepciones .NET tipadas con códigos y detalle SOAP |
| Token / Sign / ExpirationTime | Propiedades de WsaaTicket |
| COM, CLI, InstallDir, DebugLog con secretos | No son APIs del paquete .NET; no se replican wrappers Python/Windows ni volcados automáticos de credenciales |
| Caché de archivos basada en mtime | Sustituida explícitamente por IMemoryCache y expirationTime real |

La caché es local al proceso: no sobrevive reinicios ni coordina múltiples
réplicas. La aplicación debe conservar el proceso o proporcionar una estrategia
compartida antes de operar múltiples instancias con las mismas credenciales.
No hay renovación anticipada ni reintentos automáticos: ARCA exige reutilizar TA
vigentes y aplica restricciones a nuevos pedidos tras ciertos errores.

## Verificación exigida

- CMS verificable con clave pública; contenido UTF-8 íntegro y SHA-256.
- CSR verificable y con subject ARCA, incluida serialNumber/CUIT.
- SOAP según WSDL, TA válido y Faults tanto HTTP 200 como 500.
- XML sin DTD/entidades externas; rechazo de campos ausentes y fechas inválidas.
- Caché hasta expiración real, concurrencia y aislamiento de claves.
- Homologación explícitamente omitida si no se configuraron credenciales.
- Compilación Release, tests y paquete inspeccionado antes de declarar entrega.

## Ruta global

- [x] Hito 1: código WSAA, configuración NuGet y suite de pruebas entregados.
- [ ] Validación WSAA contra homologación real: requiere certificados autorizados.
- [x] Hito 2: WSFEv1 — contrato, fachada y suite verificados (22 operaciones).
- [x] Hito 3: WSFEXv1 — contrato, fachada y suite verificados (19 operaciones).
- [x] Hito 4: WSMTXCA — contrato, fachada y suite verificados (27 operaciones).
- [x] Hito 5: Padrón A4/Constancia A5/A10/A13 — contratos y suite verificados.
- [x] Hito 6: README, arquitectura, ADR y guías integrales.
- [ ] Hito 7: wiki integral; la publicación sigue bloqueada y el inventario upstream requiere revisión final.

Este checklist mide entregables, no un porcentaje de paridad del repositorio
Python entero: el inventario global de PyAfipWs excede estos seis hitos y debe
auditarse antes de afirmar un port del 100%.

## Evidencia local (2026-10-06)

- SDK 10.0.401, runtime 10.0.12, macOS arm64.
- Build Release de los tres proyectos: 0 errores y 0 advertencias.
- Suite completa: 36 pruebas aprobadas, 0 fallidas, 1 omitida (homologación sin
  variables de credenciales).
- Dos regresiones reproducidas antes de corregirse: timeout durante lectura del
  body y contraseña vacía en LoadPem. El Fault con detalle textual también se
  conserva mediante XmlAnyElement y está cubierto con HTTP 200 y 500.
- Los Dummies de homologación QA respondieron; no se validaron operaciones fiscales autenticadas sin certificados autorizados.
