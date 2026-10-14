# Licencias y distribución

NetArcaWs y NetArcaWs.Tool se distribuyen bajo LGPL-3.0-or-later. La licencia completa del proyecto y las atribuciones PyAfipWs se conservan en `LICENSE`, `COPYING` y `THIRD-PARTY-NOTICES.md`. El paquete de herramienta incluye además el inventario de dependencias y los archivos de licencia, copyright y NOTICE que acompañan a las dependencias NuGet redistribuidas.

`licenses/policy.json` define las expresiones SPDX admitidas y las excepciones revisadas. `licenses/dependencies.json` se deriva de todos los `packages.lock.json` versionados; registra versiones, expresiones declaradas, hashes de contenido NuGet, hashes del archivo comprimido, hashes de binarios y hashes de materiales legales. Los textos de licencia y avisos proceden del `.nupkg` restaurado. Si un paquete omite el texto completo de una licencia estándar aprobada, se incluye la copia SPDX fijada en `licenses/standards/`, mientras el copyright del nuspec y los NOTICE del paquete permanecen sin cambios. Se exige el copyright del nuspec, excepto cuando un aviso legal original del paquete contenga una línea de copyright concreta; una plantilla genérica de licencia estándar no satisface ese requisito. La auditoría no infiere licencias cuando la declaración falta o es ambigua.

`Microsoft.Data.SqlClient.SNI.runtime` 6.0.3 se registra solo como referencia revisada. Su licencia se fija por hash en la política para detectar cambios; la licencia y los binarios de ese paquete no se copian a la distribución NetArcaWs. Los paquetes de release que incluyan binarios SNI se rechazan. Si un consumidor Windows redistribuye o despliega esos componentes mediante su propia aplicación, debe revisar y cumplir las condiciones aplicables de Microsoft, incluida la entrega de términos a usuarios finales y las demás obligaciones del texto de licencia. El modo de red administrada de Microsoft.Data.SqlClient sirve para pruebas y depuración; no es una alternativa soportada para producción. Véase la [documentación de Microsoft sobre los switches de SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/appcontext-switches?view=sql-server-ver17).

El grafo opcional de SQL Server también contiene `Microsoft.Identity.Client.NativeInterop` 0.20.6. Sus términos de licencia de Microsoft se registran como referencia y hash porque la sección 3(e) restringe la redistribución. La auditoría rechaza sus binarios administrados y nativos en cualquier paquete de release de NetArcaWs. Esta restricción crea un riesgo de despliegue y publicación para consumidores que usen el grafo opcional de SQL Server en Windows, Linux o macOS; deben obtener un permiso de redistribución o reemplazar esa dependencia antes de publicar.

La LGPL permite el uso comercial sujeto a sus condiciones. Quien distribuya NetArcaWs debe conservar la posibilidad práctica de reemplazar la biblioteca, entregar la licencia y los avisos aplicables, y proporcionar el código fuente correspondiente de la biblioteca o una oferta escrita válida. El archivo fuente de release debe contener el código y los insumos de compilación correspondientes al commit exacto; Task 2 incorpora y vincula ese archivo a los checksums de release. No empaquete esta biblioteca como un artefacto de archivo único, AOT o exclusivamente estático que quite al destinatario la posibilidad práctica de reemplazarla. Cuando corresponda, la LGPL permite la ingeniería inversa para depurar modificaciones de la biblioteca.

La auditoría automatizada comprueba la consistencia de metadatos y archivos de paquetes; no constituye asesoría ni certificación legal. Quien redistribuye NetArcaWs debe evaluar las obligaciones aplicables a su aplicación y entorno.

Para actualizar el inventario después de una restauración bloqueada y revisar los cambios:

```sh
dotnet run --project tools/NetArcaWs.Build -- licenses --update
dotnet run --project tools/NetArcaWs.Build -- licenses --check
dotnet run --project tools/NetArcaWs.Build -- licenses --check-packages artifacts
```
