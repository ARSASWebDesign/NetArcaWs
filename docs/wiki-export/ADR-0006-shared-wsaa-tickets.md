<!-- Source: docs/adr/0006-shared-wsaa-tickets.md. Generated wiki mirror; edit the repository source. -->

# ADR 0006: Tickets WSAA compartidos entre réplicas

- Estado: Aceptado e implementado en `NetArcaWs.EntityFrameworkCore`
- Fecha: 2026-10-06
- Complementa: [contexto multitenant](Decisi%C3%B3n-2-Contexto-multitenant) y [certificados en memoria](Decisi%C3%B3n-3-Certificados-en-memoria)

## Contexto

La caché WSAA incorporada al paquete base usa `IMemoryCache` y coordina logins
solo dentro de un proceso. Tres réplicas con el mismo certificado pueden intentar
`loginCms` al mismo tiempo. La identidad remota de WSAA depende del certificado,
ambiente y servicio; los identificadores internos de tenant y CUIT representada
no crean sesiones remotas distintas.

## Decisión

La persistencia compartida es opt-in y vive en el paquete opcional
`NetArcaWs.EntityFrameworkCore`. Se seleccionan explícitamente los servicios
autenticados en la configuración inmutable del modelo:

```csharp
NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(options =>
    options.AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));

modelBuilder.AddNetArcaWs(modelOptions);
services.AddNetArcaWsEntityFrameworkStores<MyDbContext>(modelOptions);
```

`AddWsaaTickets` acepta servicios tipados `ArcaService`, excepto `Wsaa`, que no
es una identidad de servicio para el ticket. Cada fachada conserva su valor
`TicketService`; en particular, Padron A5 usa `ws_sr_constancia_inscripcion`.
La selección es fija al iniciar la aplicación y forma parte de la clave del
modelo EF. Tickets y diario fiscal son módulos independientes: un modelo solo
con tickets contiene únicamente `NetArcaWsaaTickets`, y el registro de tickets
no requiere `IInvoiceJournal`.

La aplicación debe registrar explícitamente un `IWsaaTicketProtector` antes de
habilitar el módulo. El protector integrado `AesGcmWsaaTicketProtector` recibe un
ID activo y un keyring suministrados por la aplicación. Cifra el XML completo
del TA con AES-GCM y vincula el ciphertext mediante datos autenticados asociados
a la identidad remota. Las claves no se guardan en la base de datos ni se
derivan de ella. Quien opera las réplicas debe distribuir el mismo keyring por
un mecanismo externo seguro y conservar las claves antiguas necesarias para
descifrar filas aún vigentes durante una rotación.

La clave de una fila se deriva de:

```text
(huella SHA-256 del certificado, endpoint oficial WSAA del ambiente, servicio WSAA)
```

No incluye tenant ni CUIT representada. La aplicación sigue resolviendo y
autorizando `ArcaTenantContext` antes de llamar al cliente. El ticket retornado
para una identidad criptográfica compartida no mezcla la CUIT del contexto en
las llamadas SOAP posteriores. La huella identifica filas locales; no prueba
que ARCA considere dos certificados distintos como credenciales remotas
independientes. Rotar el certificado cambia la fila local, pero no garantiza que
ARCA acepte un login inmediato mientras una sesión anterior siga retenida.

El estado de coordinación se persiste antes de iniciar el login remoto. La
reserva es atómica y usa versión/fencing; no se conserva una transacción de base
de datos durante la llamada SOAP. Un estado `Pending` no se reasigna cuando
vence su lease: ese vencimiento sirve como dato operativo y nunca prueba que
ARCA no haya recibido el login. El propietario original puede persistir una
respuesta positiva tardía mientras conserve su fencing. Los demás procesos
esperan con polling limitado y cancelable.

Un TA persistido se reutiliza hasta su expiración efectiva informada por WSAA.
Si falla el login o no puede confirmarse su persistencia, la fila queda
`Unknown` cuando la base lo permite; si la base también falla, `Pending` sigue
bloqueando nuevos logins. Ambos estados producen una excepción explícita y no
provocan fallback a la caché local ni otro `loginCms` automático. Un fallo al
descifrar o verificar el payload también falla cerrado y no elimina ni reemplaza
la fila.

La primera versión no expone importación manual de TA ni reset de filas
`Unknown`/`Pending`. Resolver un resultado permanentemente perdido requiere una
acción operativa fuera de la biblioteca, basada en evidencia de WSAA; no se
declara automáticamente que la sesión remota haya desaparecido. Tampoco se
incorpora almacenamiento de certificados, SDK de vault, key management,
coordinación distribuida adicional ni recuperación automática de filas inciertas.

## Consecuencias y validación

- Varias instancias que comparten base de datos y keyring pueden reutilizar el
  mismo TA y coordinan un solo intento por identidad remota conocida.
- La disponibilidad del almacenamiento y de las claves externas pasa a ser
  requisito para el proveedor compartido. Si falta uno, el login no se ejecuta.
- El proveedor compartido se registra como `IArcaTicketProvider` para las
  fachadas SOAP. Las llamadas directas a `WsaaService.AuthenticateForTenantAsync`
  conservan el comportamiento de caché local y no consultan esta base.
- La protección AES-GCM evita que token, Sign o XML del TA queden en claro en la
  base. La aplicación sigue siendo responsable de proteger backups, logs y el
  canal de administración de claves.
- El vencimiento de retención interna de WSAA puede variar; la implementación
  no codifica un intervalo de `alreadyAuthenticated` ni reintenta esa respuesta.
  La sección 10.6 del [manual oficial de WSAA](https://www.afip.gob.ar/ws/WSAA/WSAAmanualDev.pdf)
  describe rechazos por solicitudes repetidas del mismo servicio dentro de una
  retención configurable, con ejemplos de 10 minutos en testing y 2 minutos en
  producción, y advierte que esos valores pueden cambiar sin aviso.
- La vigencia del certificado se contrasta con su `NotBefore`/`NotAfter` real;
  no se presupone una duración fija para certificados ARCA.
- Las pruebas locales usan certificados y SOAP sintéticos; no prueban
  autenticación, autorización ni interoperabilidad contra ARCA.

La cobertura local incluye coordinación entre tres proveedores independientes,
reapertura del proveedor, aislamiento por servicio/ambiente/certificado,
reutilización entre tenants con el mismo certificado, expiración efectiva,
rotación de claves, corrupción criptográfica, caída de persistencia, resultado
desconocido y vencimiento del lease durante un login en curso.
