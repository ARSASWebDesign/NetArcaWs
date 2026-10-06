# ADR 0002: Contexto explícito de tenant para servicios ARCA

- Estado: Aceptado
- Fecha: 2026-10-06
- Alcance actual: selección de identidad, endpoint y caché para WSAA y las
  operaciones autenticadas de las fachadas SOAP
- Validación de negocio e integración: sigue abierta para Hitos 2–5

## Contexto

Una instancia compartida del servicio puede autenticar CUIT y certificados de
varios clientes. Mantener un `WsaaOptions.Endpoint` mutable por operación permitiría
que una llamada concurrente reutilice el entorno de otra. Pasar solo contenido de
certificado selecciona una credencial, pero no expresa el tenant, CUIT representado
ni ambiente que la aplicación autorizó.

ARCA permite que una credencial opere por una CUIT representada mediante una
delegación autorizada. Por eso el titular del certificado y la CUIT representada
pueden diferir; la igualdad de esos valores no es una regla válida de autorización.
También puede ocurrir que dos registros de tenant internos representen la misma
CUIT.

## Decisión

NetArcaWs expone `ArcaTenantContext` en `NetArcaWs.Multitenancy`. Es un objeto
inmutable con propiedades de solo lectura `TenantId`, `Cuit`, `Environment` y
`Certificate`. Su constructor valida formato y presencia, pero el contexto es una
entrada confiable creada por la aplicación; **no autentica ni autoriza** al tenant.

La aplicación autentica al usuario, resuelve el tenant asociado a su identidad,
autoriza acceso a la CUIT representada y obtiene el ambiente y
`WsaaCertificateContent` desde su configuración protegida. No debe formar el
contexto con valores de tenant, CUIT, ambiente o certificados enviados libremente
por el request. La aplicación debe aplicar las reglas de delegación de ARCA; no
debe rechazar una relación autorizada solo porque CUIT representada y titular del
certificado sean distintos.

La autenticación WSAA por tenant usa:

```csharp
await wsaa.AuthenticateForTenantAsync("wsfe", tenantContext, cancellationToken);
```

El ambiente selecciona el endpoint oficial de homologación o producción en la
operación, sin mutar `WsaaOptions` del servicio compartido. No hay failover entre
ambientes. El identificador de caché local se compone de:

```text
(tenantId, CUIT representada, endpoint, servicio, huella SHA-256 del certificado)
```

Esto separa las entradas de tickets locales cuando una misma instancia atiende
varios tenants. El servicio mantiene semáforos stripeados y acotados por caché:
dos claves distintas pueden caer en la misma franja y serializarse sin compartir
el ticket. Esos semáforos no son un lock durable ni coordinan procesos distintos.
`AuthenticateWithContentAsync` continúa disponible como API de bajo nivel para
llamadas con contenido explícito y rotación, pero no es la frontera de aislamiento
multitenant: la aplicación debe usar `AuthenticateForTenantAsync` para esos flujos.

La separación local no cambia la identidad criptográfica que ARCA ve en el
certificado. Compartir el mismo certificado entre tenants puede provocar
`coe.alreadyAuthenticated` y no garantiza sesiones independientes en el servidor.
La caché actual es en memoria; no ofrece coordinación entre procesos ni
persistencia distribuida. No se promete independencia remota aunque las claves
locales de caché sean distintas.

## Alcance de otros servicios

Las fachadas autenticadas de WSFEv1, WSFEXv1, WSMTXCA y Padrón reciben el
contexto por operación y lo usan para seleccionar ticket y endpoint. Sus
contratos y suites locales están verificados; la autenticación fiscal real no se
ejecutó por falta de certificados autorizados. No mutan opciones globales para
alternar tenants. Esto no acredita que una CUIT esté habilitada para cada
operación.

Los health checks siguen siendo checks de infraestructura por servicio y ambiente.
No usan `ArcaTenantContext` ni certificados y no verifican autorización fiscal.
Un resultado saludable no prueba que la CUIT de un tenant esté habilitada.

El diario durable de facturación futura debe respetar la identidad fiscal definida
en [ADR 0001](0001-safe-invoice-retries.md):
`(ambiente, CUIT emisora, punto de venta, tipo, número)`. No se agrega `tenantId` a
esa restricción de unicidad. Si una CUIT actúa en dos tenants internos, el mismo
comprobante sigue siendo una única identidad fiscal; el tenant restringe acceso y
visibilidad, no permite duplicar su registro. Esto mantiene separadas la
autorización por tenant y la consistencia contable.

## Origen de certificados y documentación

El [ADR 0003](0003-in-memory-certificates.md) define la carga PEM/PFX desde
variables, vaults o bases de datos, ownership y rotación. El contexto multitenant
recibe ese material y no exige archivos. Compartirlo entre instancias no crea una
caché distribuida de TA.

Esta decisión y su adopción en todos los clientes de negocio deberán integrarse
en el [wiki integral del hito final](../plans/hito-7-wiki.md), junto con ejemplos
por tenant y límites explícitos de cada servicio.

## Consecuencias

- Una sola instancia de `WsaaService` puede atender ambientes y credenciales
  distintos sin cambiar configuración compartida por operación.
- La resolución y autorización de tenant/CUIT sigue bajo responsabilidad de la
  aplicación y debe ocurrir antes de construir el contexto.
- La huella del certificado y los campos de tenant separan tickets locales, pero
  ARCA puede rechazar autenticaciones concurrentes que compartan certificado.
- La presencia de fachadas en otros WS no cierra su validación funcional ni
  cambia el límite de unicidad fiscal definido en ADR 0001.
- La rotación de secreto y cualquier caché compartida entre réplicas requieren
  coordinación de la aplicación; la biblioteca no integra un proveedor de vault.
