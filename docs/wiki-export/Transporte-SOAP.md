<!-- Source: docs/wiki/Transporte-SOAP.md. Generated wiki mirror; edit the repository source. -->

# Transporte SOAP común

Los adaptadores fiscales usan `ISoapTransport.SendAsync<TRequest,TResponse>` como
frontera común para enviar contratos tipados. La implementación `SoapTransport`
usa SOAP 1.1 y `HttpClient`; el tipo de request y response debe reflejar el
contrato del servicio concreto.

La URI debe ser HTTPS y no puede incluir credenciales, query ni fragment. La
acción se transmite en el header `SOAPAction`. La solicitud se serializa en un
buffer acotado por `SoapTransportOptions.MaxRequestBytes`; la respuesta se
limita con `SoapTransportOptions.MaxResponseBytes`. Ambos topes son 4 MiB por
defecto y admiten de 1 KiB a 32 MiB. Un request excedido se rechaza antes del
envío HTTP. El lector no permite DTD y exige exactamente un elemento hijo del
SOAP Body. Los faults se expresan con `SoapFaultException` y campos separados
de código, razón, detalle y status HTTP. Se propaga cancelación; el timeout HTTP
aparece como `TaskCanceledException` con `TimeoutException` interna.

El transporte no sabe qué significa un fault para el negocio, no consigue
credenciales, no renueva TA y no reintenta. Los adaptadores deben manejar el
ticket tenant con `IArcaTicketProvider`, mapear fault/response con el contrato
oficial y aplicar sólo las reglas de reconciliación autorizadas por ese manual.
No registrar XML fiscal ni credenciales en logs ordinarios.
