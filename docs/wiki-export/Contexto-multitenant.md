<!-- Source: docs/wiki/Contexto-multitenant.md. Generated wiki mirror; edit the repository source. -->

# Contexto multitenant

`ArcaTenantContext` es un valor inmutable para seleccionar un tenant confiable,
CUIT representada, entorno y certificado. WSAA ofrece
`AuthenticateForTenantAsync(service, tenant, cancellationToken)` y usa el
ambiente para elegir el endpoint oficial sin cambiar opciones compartidas.

```csharp
var tenant = new ArcaTenantContext(
    resolvedTenantId,
    resolvedCuit,
    environment,
    certificateContent);

var ticket = await wsaa.AuthenticateForTenantAsync("wsfe", tenant, cancellationToken);
```

La aplicación debe autenticar al usuario, resolver y autorizar su acceso al
tenant/CUIT, obtener ambiente y credenciales de almacenamiento confiable y recién
entonces construir el contexto. Nunca derivar esos valores de campos libres del
request. La CUIT representada puede diferir de la titular del certificado cuando
ARCA autorizó delegación; no imponer una igualdad entre ambas.

La clave de caché local incluye tenant, CUIT representada, endpoint/ambiente,
servicio y huella del certificado. Esto separa tickets locales en una misma
instancia, pero no cambia la identidad remota del certificado. Compartir una
credencial entre tenants puede causar `coe.alreadyAuthenticated`; no hay promesa
de sesiones independientes en ARCA, caché distribuida ni coordinación entre
procesos.

El contexto no comprueba permisos fiscales ni reemplaza la autorización de la
aplicación. El diario fiscal disponible usa `tenantId` para separar operaciones
e idempotencia, pero la unicidad fiscal sigue siendo
`(ambiente, CUIT emisora, punto de venta, tipo, número)` según
[ADR 0001](Decisi%C3%B3n-1-Emisi%C3%B3n-y-reintentos-seguros). Dos tenants internos
que representan una misma CUIT no deben registrar dos veces el mismo comprobante.

Los wrappers tipados WSFEv1/WSFEXv1/WSMTXCA y Padrón reciben el contexto en las
llamadas autenticadas; la integración se verificó con la suite y contratos
archivados. No se ejecutaron operaciones autenticadas reales por falta de
certificados autorizados. Los health checks son por servicio y ambiente: no
reciben certificado ni contexto y no prueban autorización fiscal. Detalle: [ADR 0002](Decisi%C3%B3n-2-Contexto-multitenant) y
[ADR 0003](Decisi%C3%B3n-3-Certificados-en-memoria).
