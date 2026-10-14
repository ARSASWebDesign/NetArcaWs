# Licencias y distribución

NetArcaWs y NetArcaWs.Tool se distribuyen bajo LGPL-3.0-or-later. La licencia completa del proyecto y las atribuciones PyAfipWs se conservan en `LICENSE`, `COPYING` y `THIRD-PARTY-NOTICES.md`. El paquete de herramienta incluye además el inventario de dependencias y los archivos de licencia, copyright y NOTICE que acompañan a las dependencias NuGet redistribuidas.

`licenses/policy.json` define las expresiones SPDX admitidas y las excepciones revisadas. `licenses/dependencies.json` se deriva de todos los `packages.lock.json` versionados; registra versiones, expresiones declaradas, hashes de contenido NuGet, hashes del archivo comprimido, hashes de binarios y hashes de materiales legales. Los textos de licencia y avisos proceden del `.nupkg` restaurado. Si un paquete omite el texto completo de una licencia estándar aprobada, se incluye la copia SPDX fijada en `licenses/standards/`, mientras el copyright del nuspec y los NOTICE del paquete permanecen sin cambios. Se exige el copyright del nuspec, excepto cuando un aviso legal original del paquete contenga una línea de copyright concreta; una plantilla genérica de licencia estándar no satisface ese requisito. La auditoría no infiere licencias cuando la declaración falta o es ambigua.

`Microsoft.Data.SqlClient.SNI.runtime` 7.1.0 se registra solo como referencia revisada. Su licencia se fija por hash en la política para detectar cambios; la licencia y los binarios de ese paquete no se copian a la distribución NetArcaWs. Los paquetes de release que incluyan binarios SNI se rechazan. Si un consumidor Windows redistribuye o despliega esos componentes mediante su propia aplicación, debe revisar y cumplir las condiciones aplicables de Microsoft, incluida la entrega de términos a usuarios finales y las demás obligaciones del texto de licencia. El modo de red administrada de Microsoft.Data.SqlClient sirve para pruebas y depuración; no es una alternativa soportada para producción. Véase la [documentación de Microsoft sobre los switches de SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/appcontext-switches?view=sql-server-ver17).

El provider SQL Server fija directamente `Microsoft.Data.SqlClient` 7.1.1 para mantener una versión compatible con EF Core 10 y evitar que la resolución seleccione una versión anterior. Su grafo base ya no contiene `Microsoft.Identity.Client.NativeInterop`. La autenticación Entra mediante connection string requiere instalar y revisar por separado el paquete opcional `Microsoft.Data.SqlClient.Extensions.Azure`, que no forma parte del grafo ni de los paquetes NetArcaWs; la autenticación SQL y Windows sigue disponible mediante el driver base. No se habilitan switches globales de red administrada.

La LGPL permite el uso comercial sujeto a sus condiciones. Quien distribuya NetArcaWs debe conservar la posibilidad práctica de reemplazar la biblioteca, entregar la licencia y los avisos aplicables, y proporcionar el código fuente correspondiente de la biblioteca o una oferta escrita válida. Cada release producida por el workflow actual publica `SOURCE.zip` junto a los paquetes NuGet en la [página de releases](https://github.com/ARSASWebDesign/NetArcaWs/releases); elegí el asset de la misma versión del paquete. El ZIP se genera desde el commit exacto, conserva el árbol completo rastreado y contiene las licencias, avisos e insumos de compilación. `LICENSE_EVIDENCE.json` vincula el commit, `licenses/dependencies.json` y el hash del ZIP; `SHA256SUMS` cubre los trece paquetes y ambos archivos de fuente/evidencia. `BUILD_COMMIT` y el comentario del ZIP identifican el mismo commit. El archivo incluye instrucciones reproducibles para restaurar y compilar con el SDK de `global.json`. No empaquetes esta biblioteca como un artefacto de archivo único, AOT o exclusivamente estático que quite al destinatario la posibilidad práctica de reemplazarla. Cuando corresponda, la LGPL permite la ingeniería inversa para depurar modificaciones de la biblioteca.

La auditoría automatizada comprueba la consistencia de metadatos y archivos de paquetes; no constituye asesoría ni certificación legal. Quien redistribuye NetArcaWs debe evaluar las obligaciones aplicables a su aplicación y entorno.

## Fuente y compilación de cada release

En releases creadas con el control de distribución actual, la página de GitHub incluye `SOURCE.zip` junto a los paquetes NuGet de esa versión. Descargá ambos desde la [misma release](https://github.com/ARSASWebDesign/NetArcaWs/releases); el archivo no se obtiene del branch actual ni de una release distinta. `LICENSE_EVIDENCE.json`, `BUILD_COMMIT` y `SHA256SUMS` permiten comprobar la relación entre el commit, el inventario de licencias, la fuente y los paquetes.

El ZIP incluye el árbol completo de archivos rastreados, las licencias LGPL y de terceros, los lockfiles y los proyectos de compilación. Para restaurar y compilar la solución con el SDK indicado en `global.json`:

```sh
dotnet restore NetArcaWs.slnx --locked-mode
dotnet build NetArcaWs.slnx --configuration Release --no-restore
```

La release 0.6.0 antecede a la incorporación de estos assets; no infieras que releases históricas contienen `SOURCE.zip` o evidencia de licencia.

Para actualizar el inventario después de una restauración bloqueada y revisar los cambios:

```sh
dotnet run --project tools/NetArcaWs.Build -- licenses --update
dotnet run --project tools/NetArcaWs.Build -- licenses --check
dotnet run --project tools/NetArcaWs.Build -- licenses --check-packages artifacts
```
