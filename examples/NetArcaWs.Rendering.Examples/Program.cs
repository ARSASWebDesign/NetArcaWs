using NetArcaWs.Rendering.Examples;

string output = "artifacts/comprobantes";
if (args.Length > 0)
{
    if (args.Length != 2 || args[0] != "--output")
    {
        Console.Error.WriteLine("Uso: NetArcaWs.Rendering.Examples [--output <directorio>]");
        return 2;
    }
    output = args[1];
}

string run = SampleGallery.Generate(output);
Console.WriteLine($"Galería sintética generada: {Path.GetFullPath(run)}");
Console.WriteLine($"Muestras HTML/PDF: {SampleGallery.CreateCases().Count * 3 + 2} salidas (3 perfiles por escenario, variante de color y logo apaisado).");
return 0;
