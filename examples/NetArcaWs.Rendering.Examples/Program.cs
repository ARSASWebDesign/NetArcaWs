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
Console.WriteLine($"Muestras: {SampleGallery.CreateCases().Count} escenarios × 3 perfiles de marca + 1 variante de color.");
return 0;
