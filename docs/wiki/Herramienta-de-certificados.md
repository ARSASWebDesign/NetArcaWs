# Herramienta de certificados

`NetArcaWs.Tool` instala el comando `netarcaws` y expone tres acciones locales:
`cert-dev`, `cert-prod` y `cert-info`. Los comandos de generación crean una clave
PKCS#8 RSA de 4096 bits cifrada por defecto y una solicitud PKCS#10. No solicitan
el certificado, no lo validan ante ARCA y no hacen llamadas de red.

Ver la [guía completa de certificados](../certificates-cli.md) para los
argumentos soportados, incluidos `--output`, `--cuit`, `--organization`,
`--name`, `--password-env` y `--unencrypted`, ejemplos y códigos de salida.
La contraseña se lee desde el entorno, nunca como argumento literal; el destino
de generación debe ser una carpeta nueva. `cert-info` muestra metadatos y puede
comparar certificado y clave localmente, pero no verifica confianza, revocación,
asociación del servicio o acceso remoto.

El paquete es instalable como tool local o global desde una carpeta local de
NuGet. La versión 0.5.0 del tool fue aceptada e indexada por NuGet; los badges
de [Inicio](Home.md) indican la versión disponible actualmente. También pueden
usarse paquetes construidos localmente. El trámite de homologación se hace en WSASS y el
de producción en Administrador de Certificados/Relaciones, según el
[procedimiento oficial de ARCA](https://www.arca.gob.ar/ws/programadores/certificados-digitales.asp).
