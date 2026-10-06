<!-- Source: docs/certificates-cli.md. Generated wiki mirror; edit the repository source. -->

# CLI de certificados de NetArcaWs

`netarcaws` crea localmente una clave privada PKCS#8 y una solicitud de firma de certificado (CSR) para iniciar el trámite ante ARCA. **No emite ni valida un certificado ARCA**, no registra relaciones de servicio, no envía la CSR y no se conecta a ARCA. La opción `cert-prod` solo prepara los archivos para el trámite productivo; no los convierte en credenciales válidas.

La clave usa RSA de 4096 bits y se cifra con contraseña por defecto. La contraseña se toma de una variable de entorno nombrada con `--password-env`; no se acepta como argumento de línea de comandos. `--unencrypted` permite explícitamente una clave sin cifrar. Cada comando de generación exige `--output DIR`, y la carpeta debe ser nueva: el CLI no sobrescribe archivos existentes. En Unix, la carpeta se crea con modo 0700 y los archivos con modo 0600. En Windows se heredan las ACL de la carpeta padre: elegir una ubicación privada con los permisos apropiados.

## Instalar desde el paquete local

La integración continua genera `NetArcaWs.Tool.0.5.0.nupkg` dentro de `artifacts/`. Estos ejemplos usan ese paquete local y no presuponen que se haya publicado en nuget.org.

Instalación local mediante manifiesto, versionada junto al proyecto (similar a EF Core):

```sh
dotnet new tool-manifest
dotnet tool install --local NetArcaWs.Tool --add-source ./artifacts --version 0.5.0
dotnet tool run netarcaws --help
```

Omitir el primer comando si ya existe un manifiesto. En otro checkout, usar
`dotnet tool restore --add-source /ruta/a/artifacts`. No guardar claves ni
contraseñas en el manifiesto. Los ejemplos siguientes muestran `netarcaws`
para instalación global; con instalación local usar `dotnet tool run netarcaws`.

Instalación global:

```sh
dotnet tool install --global NetArcaWs.Tool --add-source ./artifacts --version 0.5.0
netarcaws --help
```

Instalación aislada en una carpeta del proyecto:

```sh
dotnet tool install NetArcaWs.Tool --tool-path ./.tools --add-source ./artifacts --version 0.5.0
./.tools/netarcaws --help
```

La herramienta requiere el runtime .NET 10. Si .NET está instalado en una ruta
personalizada y el ejecutable informa que no encuentra el runtime, configurar
`DOTNET_ROOT` con la carpeta de esa instalación (la que contiene `dotnet`).

## Crear una solicitud de desarrollo

```sh
netarcaws cert-dev \
  --cuit "$ARCA_CUIT" \
  --organization "Mi organización" \
  --name "Integración de desarrollo" \
  --output ./certificados/arca-dev-2026 \
  --password-env NETARCA_KEY_PASSWORD
```

Definí `ARCA_CUIT` y `NETARCA_KEY_PASSWORD` en el entorno mediante tu gestor de secretos o mecanismo local seguro antes de ejecutar el comando. El CUIT se valida por formato de 11 dígitos en esta versión; no verifica titularidad, dígito verificador ni permisos de ARCA.

La carpeta nueva de `--output` contiene `privada.key`, `solicitud.csr`, `instructions.txt` y `manifest.json`. La clave queda cifrada con la contraseña indicada por el entorno; el comando informa los archivos creados.

Para crear la clave sin cifrar, omitir `--password-env` y optar expresamente por:

```sh
netarcaws cert-dev \
  --cuit "$ARCA_CUIT" \
  --organization "Mi organización" \
  --name "Integración de desarrollo" \
  --output ./certificados/arca-dev-2026 \
  --unencrypted
```

`--password-env` y `--unencrypted` son mutuamente excluyentes. `cert-prod` usa los mismos argumentos y prepara una CSR para el flujo productivo; ambos comandos son locales y generan una clave nueva.

## Consultar un certificado

```sh
netarcaws cert-info --certificate ./certificados/certificado.crt
```

Para comprobar que una clave privada local corresponde al certificado, incluir la clave. Si está cifrada, pasar el nombre de la variable de entorno que contiene su contraseña:

```sh
netarcaws cert-info \
  --certificate ./certificados/certificado.crt \
  --key ./certificados/privada.key \
  --password-env NETARCA_KEY_PASSWORD
```

La consulta exige un certificado RSA PEM o DER, comprueba sus fechas contra el reloj local y muestra metadatos del certificado y, cuando se proporciona clave, compara localmente su clave pública. No valida cadena de confianza, revocación, vigencia ante ARCA, asociación de servicios ni acceso remoto. No hace llamadas de red.

## Completar el trámite ante ARCA

- **Desarrollo/homologación:** seguir el procedimiento de [Certificados digitales para homologación](https://www.arca.gob.ar/ws/programadores/certificados-digitales.asp) en WSASS.
- **Producción:** usar Administrador de Certificados Digitales y luego Administrador de Relaciones para asociar el servicio correspondiente a la CUIT representada, según el [procedimiento publicado por ARCA](https://www.arca.gob.ar/ws/programadores/certificados-digitales.asp).

Conservá la clave privada en un lugar protegido y no la compartas. La CSR contiene la clave pública y los datos que se enviarán durante el trámite; verificá esos datos antes de subirla.

## Resultados y cancelación

Código 0: éxito; 1: error de archivos, criptografía o certificado fuera de vigencia;
2: argumentos inválidos; 130: cancelación por Ctrl+C. Certificados y claves de
entrada tienen un límite de 1 MiB. La generación escribe en una carpeta temporal
privada y la mueve al destino al completar los cuatro archivos; ante una falla
intenta eliminar solamente su propia carpeta temporal. No reemplaza destinos
existentes, incluso ante dos generaciones concurrentes.
