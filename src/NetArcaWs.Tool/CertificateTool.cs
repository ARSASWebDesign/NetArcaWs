using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using NetArcaWs.Cryptography;

namespace NetArcaWs.Tool;

/// <summary>Offline tools for generating ARCA enrollment requests and inspecting certificates.</summary>
public sealed class CertificateTool(TextWriter output, TextWriter error, Func<string, string?> environment)
{
    private const string CertificateGuide = "https://www.arca.gob.ar/ws/programadores/certificados-digitales.asp";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (args.Length == 0 || args is ["--help"] or ["-h"] ||
                (args.Length == 2 && IsCommand(args[0]) && args[1] is "--help" or "-h"))
            {
                await output.WriteLineAsync(Help);
                return 0;
            }
            if (args is ["--version"])
            {
                var version = typeof(CertificateTool).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
                await output.WriteLineAsync($"netarcaws {version}");
                return 0;
            }
            if (!IsCommand(args[0])) throw new UsageException("Comando desconocido. Usa netarcaws --help.");
            var options = ParseOptions(args);
            return args[0] == "cert-info"
                ? await InspectAsync(options, cancellationToken)
                : await GenerateAsync(args[0], options, cancellationToken);
        }
        catch (UsageException ex)
        {
            await error.WriteLineAsync(ex.Message);
            return 2;
        }
        catch (CryptographicException)
        {
            await error.WriteLineAsync("No se pudo procesar el material criptográfico. Comprueba certificado, clave y contraseña.");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            await error.WriteLineAsync("No se pudo acceder a los archivos. El destino debe ser nuevo y los permisos deben permitir la operación.");
            return 1;
        }
        catch (ArgumentException)
        {
            await error.WriteLineAsync("Algún argumento, ruta o dato del certificado no es válido. Usa netarcaws --help.");
            return 2;
        }
    }

    private async Task<int> GenerateAsync(string command, Dictionary<string, string> options, CancellationToken token)
    {
        var cuit = NormalizeCuit(Required(options, "--cuit"));
        var organization = ValidateName(Required(options, "--organization"));
        var commonName = ValidateName(Required(options, "--name"));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Required(options, "--output")));
        var parent = Path.GetDirectoryName(target);
        if (string.IsNullOrEmpty(parent)) throw new UsageException("--output debe indicar una carpeta nueva, no la raíz del volumen.");
        if (options.ContainsKey("--password-env") == options.ContainsKey("--unencrypted"))
            throw new UsageException("Elige --password-env VARIABLE o --unencrypted, nunca ambos.");
        var password = options.ContainsKey("--password-env") ? ReadPassword(options) : null;
        var keySize = 4096;
        if (options.TryGetValue("--key-size", out var size) &&
            (!int.TryParse(size, NumberStyles.None, CultureInfo.InvariantCulture, out keySize) || keySize is not (2048 or 3072 or 4096)))
            throw new UsageException("--key-size debe ser 2048, 3072 o 4096.");
        if (Path.Exists(target) || new DirectoryInfo(target).LinkTarget is not null || new FileInfo(target).LinkTarget is not null)
            throw new IOException("Output already exists.");

        token.ThrowIfCancellationRequested();
        var privatePem = WsaaCryptography.CreatePrivateKey(keySize, password);
        using var key = RSA.Create();
        if (password is null) key.ImportFromPem(privatePem);
        else key.ImportFromEncryptedPem(privatePem, password);
        var csr = WsaaCryptography.CreateCertificateRequest(key, commonName, "AR", organization, cuit);
        var scope = command == "cert-dev" ? "Homologation" : "Production";
        var instructions = $"""
            Entorno solicitado: {scope}
            Se generaron una clave privada RSA y un pedido de certificado (CSR).
            NO se generó un certificado emitido por ARCA.

            1. Conserva privada.key localmente; nunca la subas al portal.
            2. Presenta solicitud.csr en {(scope == "Homologation" ? "WSASS para homologación" : "Administrador de Certificados Digitales para producción")}.
            3. Descarga el certificado emitido por ARCA y autoriza los servicios que necesites
               mediante el flujo de {(scope == "Homologation" ? "WSASS" : "Administrador de Relaciones de Clave Fiscal")}.
            4. Usa netarcaws cert-info --certificate certificado.crt --key privada.key
               y --password-env VARIABLE si la clave está cifrada, para comprobar la pareja local.

            La etiqueta de entorno no certifica autorizaciones ni sustituye la emisión por ARCA.
            Guía oficial: {CertificateGuide}
            """;
        var manifest = new CertificateRequestManifest(scope, cuit, organization, commonName,
            "privada.key", "solicitud.csr", DateTimeOffset.UtcNow, keySize, password is not null,
            Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())));

        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $".netarcaws-{Guid.NewGuid():N}");
        var published = false;
        try
        {
            if (OperatingSystem.IsWindows()) Directory.CreateDirectory(staging);
            else Directory.CreateDirectory(staging, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            await WritePrivateAsync(Path.Combine(staging, "privada.key"), privatePem, token);
            await WritePrivateAsync(Path.Combine(staging, "solicitud.csr"), csr, token);
            await WritePrivateAsync(Path.Combine(staging, "instructions.txt"), instructions, token);
            await WritePrivateAsync(Path.Combine(staging, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions), token);
            token.ThrowIfCancellationRequested();
            Directory.Move(staging, target);
            published = true;
        }
        finally
        {
            if (!published && Directory.Exists(staging))
            {
                try { Directory.Delete(staging, recursive: true); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    await error.WriteLineAsync("No se pudo limpiar la carpeta temporal privada; revisa las carpetas .netarcaws-* del destino.");
                }
            }
        }
        await output.WriteLineAsync($"Clave privada y CSR generados para {scope} en: {target}");
        await output.WriteLineAsync("No se emitió un certificado ARCA. Sigue instructions.txt para obtenerlo y autorizar servicios.");
        return 0;
    }

    private async Task<int> InspectAsync(Dictionary<string, string> options, CancellationToken token)
    {
        var path = Required(options, "--certificate");
        if (options.ContainsKey("--password-env") && !options.ContainsKey("--key"))
            throw new UsageException("--password-env requiere --key en cert-info.");
        var password = options.ContainsKey("--password-env") ? ReadPassword(options) : null;
        using var certificate = X509CertificateLoader.LoadCertificate(await ReadBoundedAsync(path, token));
        using var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null) throw new CryptographicException("RSA certificate required.");
        if (options.TryGetValue("--key", out var keyPath))
        {
            var keyBytes = await ReadBoundedAsync(keyPath, token);
            try
            {
                using var pair = WsaaCryptography.LoadPem(certificate.ExportCertificatePem(), Encoding.UTF8.GetString(keyBytes), password);
            }
            finally { CryptographicOperations.ZeroMemory(keyBytes); }
            await output.WriteLineAsync("Clave privada: coincide con el certificado.");
        }
        var info = WsaaCryptography.AnalyzeCertificate(certificate);
        var now = DateTimeOffset.UtcNow;
        var status = now < info.NotBefore ? "aún no válido" : now >= info.NotAfter ? "vencido" : "vigente";
        await output.WriteLineAsync($"Sujeto: {info.Subject}\nEmisor: {info.Issuer}\nDesde UTC: {info.NotBefore:O}\nHasta UTC: {info.NotAfter:O}\nHuella SHA-256: {certificate.GetCertHashString(HashAlgorithmName.SHA256)}\nVigencia local: {status}");
        await output.WriteLineAsync("No se verificó la confianza de ARCA ni la autorización a servicios.");
        return status == "vigente" ? 0 : 1;
    }

    private string ReadPassword(Dictionary<string, string> options)
    {
        var name = Required(options, "--password-env");
        if (name.Any(c => c is '=' or '\0')) throw new UsageException("Nombre de variable de entorno inválido.");
        var password = environment(name);
        if (string.IsNullOrEmpty(password)) throw new UsageException("La variable de --password-env debe existir y contener una contraseña no vacía.");
        return password;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        string[] allowed = args[0] == "cert-info"
            ? ["--certificate", "--key", "--password-env"]
            : ["--cuit", "--organization", "--name", "--output", "--password-env", "--unencrypted", "--key-size"];
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length; i++)
        {
            var name = args[i];
            if (!allowed.Contains(name, StringComparer.Ordinal)) throw new UsageException("Opción desconocida para el comando. Usa netarcaws --help.");
            if (result.ContainsKey(name)) throw new UsageException("No se permiten opciones repetidas.");
            if (name == "--unencrypted") { result.Add(name, "true"); continue; }
            if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("--", StringComparison.Ordinal))
                throw new UsageException($"Falta un valor para {name}.");
            result.Add(name, args[i]);
        }
        return result;
    }

    private static string Required(Dictionary<string, string> options, string name)
        => options.TryGetValue(name, out var value) ? value : throw new UsageException($"Falta {name}.");

    private static string NormalizeCuit(string value)
    {
        if (value.Length == 13 && value[2] == '-' && value[11] == '-') value = value.Remove(11, 1).Remove(2, 1);
        if (value.Length != 11 || value.Any(c => c is < '0' or > '9'))
            throw new UsageException("--cuit debe tener 11 dígitos ASCII o formato XX-XXXXXXXX-X.");
        return value;
    }

    private static string ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Normalize(NormalizationForm.FormKD).Length > 64)
            throw new UsageException("El nombre y la organización deben tener entre 1 y 64 caracteres normalizados, sin controles.");
        return value;
    }

    private static async Task WritePrivateAsync(string path, string text, CancellationToken token)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        await using var stream = new FileStream(path, options);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(text.AsMemory(), token);
        await writer.FlushAsync(token);
    }

    private static async Task<byte[]> ReadBoundedAsync(string path, CancellationToken token)
    {
        const int maximum = 1024 * 1024;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        var buffer = new byte[maximum + 1];
        try
        {
            var count = 0;
            while (count < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(count), token);
                if (read == 0) return buffer.AsSpan(0, count).ToArray();
                count += read;
            }
            throw new IOException("Cryptographic file exceeds limit.");
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }

    private static bool IsCommand(string value) => value is "cert-dev" or "cert-prod" or "cert-info";
    private sealed class UsageException(string message) : Exception(message);
    private sealed record CertificateRequestManifest(string Environment, string Cuit, string Organization, string CommonName,
        string KeyFile, string CsrFile, DateTimeOffset CreatedUtc, int KeySize, bool Encrypted, string PublicKeySha256);

    private const string Help = """
        NetArcaWs — herramientas locales para certificados ARCA (.NET 10)

        netarcaws cert-dev  --cuit CUIT --organization RAZON_SOCIAL --name NOMBRE --output CARPETA_NUEVA --password-env VARIABLE
        netarcaws cert-prod --cuit CUIT --organization RAZON_SOCIAL --name NOMBRE --output CARPETA_NUEVA --password-env VARIABLE
        netarcaws cert-info --certificate ARCHIVO [--key CLAVE_PEM] [--password-env VARIABLE]

        cert-dev: genera clave privada + CSR para presentar en WSASS (homologación).
        cert-prod: genera clave privada + CSR para Administrador de Certificados Digitales.
        ARCA debe emitir el certificado y autorizar sus servicios por separado.
        No se generan certificados autofirmados ni se realizan llamadas a ARCA.

        --key-size 2048|3072|4096   Tamaño RSA; por defecto 4096.
        --unencrypted             Alternativa explícita a --password-env: clave sin cifrar.
        --password-env VARIABLE   Nombre de variable de entorno; nunca la contraseña en argumentos.
        --output CARPETA_NUEVA     Debe no existir. Nunca se reemplazan claves existentes.
        --help / -h               Muestra esta ayuda.
        --version                 Versión de la herramienta.

        Salidas: privada.key, solicitud.csr, instructions.txt, manifest.json.
        cert-info comprueba vigencia y pareja de claves localmente, no confianza ni permisos ARCA.
        Códigos de salida: 0 éxito, 1 archivos/criptografía/vigencia, 2 uso, 130 cancelación.
        """;
}
