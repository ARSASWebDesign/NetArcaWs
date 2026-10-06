# Health checks

`NetArcaWs.HealthChecks` integra `IHealthCheck` con ASP.NET Core. El registro es
opt-in y por servicio/ambiente; no solicita certificado, no llama WSAA para un
ticket y no requiere que se registren los clientes de negocio.

```csharp
builder.Services.AddHealthChecks()
    .AddWsaaHealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3))
    .AddWsfev1HealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3));
```

Se exponen checks para WSAA, WSFEv1, WSFEXv1, WSMTXCA, Padrón A4, A5 histórico
(Constancia), A10, A13, WSCDC, WSFECred y WSCPE. Cada registro admite entorno, timeout, endpoint
alternativo y nombre; los probes no reintentan ni cachean y no se ejecutan en
segundo plano. El host que consume `IHealthCheck` decide frecuencia y alertas.

WSFE/FEX envían su `Dummy` vacío con namespace y SOAPAction del WSDL. Padrón usa
su `dummy` sin token/sign; MTXCA tiene `dummyRequest` sin partes y SOAPAction del
binding publicado; WSCDC, WSFECred y WSCPE tienen su `Dummy` independiente, sin ticket.
WSCPE también envía un cuerpo SOAP vacío, con respuesta `DummyResp/respuesta`.
Los contratos por operación, namespace, respuesta y fuentes
están en [healthchecks-contracts.md](../reference/healthchecks-contracts.md).
Los WSDL pueden cambiar: al actualizar fixtures, comprobar contrato desplegado y
manual oficial.

WSAA no tiene `Dummy`: ese check lee el WSDL por HTTPS y devuelve
`authenticationVerified=false`. Un check exitoso mide disponibilidad desde el
proceso local, no acceso fiscal del tenant ni aceptación de una transacción. Un
fallo de red local tampoco demuestra caída global del servicio ARCA.

Separar los probes remotos de liveness para que una interrupción externa no
reinicie la aplicación. Evitar exponer datos internos del resultado a clientes
no autorizados. Para API/ejemplo del mapper ver el README raíz.
