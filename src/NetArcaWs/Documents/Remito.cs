namespace NetArcaWs.Documents;

/// <summary>Clase normativa de remito cubierta por este modelo local.</summary>
public enum ClaseRemito { R, X }

/// <summary>Bien transportado en un remito. La cantidad debe ser positiva.</summary>
public sealed record LineaRemito(string Descripcion, string Unidad, decimal Cantidad);

/// <summary>Datos del establecimiento gráfico que imprimió un talonario clase R.</summary>
public sealed record DatosImprentaRemito(string RazonSocial, long Cuit, DateOnly FechaImpresion,
    long PrimerNumero, long UltimoNumero, string NumeroHabilitacion);

/// <summary>Datos de la empresa transportista cuando el traslado lo realiza un tercero.</summary>
public sealed record DatosTransportista(string RazonSocial, string Domicilio, long Cuit);

/// <summary>
/// Datos declarados por la aplicación para emitir localmente un remito. La numeración no se asigna
/// ni se solicita a ARCA. Para clase X debe indicarse el predio, polo o parque industrial común.
/// Esta primera versión requiere receptor con CUIT, domicilio y condición tributaria completos;
/// no modela las variantes de consumidor final ni las excepciones sectoriales (por ejemplo, DTV-e).
/// La RG 1415, artículo 30, fija un mínimo de 15 x 20 cm para remitos impresos; la representación
/// digital exceptuada del tamaño sigue sujeta a las responsabilidades de entrega y conservación del emisor.
/// </summary>
public sealed record DatosRemito
{
    private IReadOnlyList<LineaRemito> _mercaderia = Array.AsReadOnly(Array.Empty<LineaRemito>());

    public DatosRemito(ClaseRemito clase, DateOnly fechaEmision, int puntoVenta, long numero,
        DatosReceptor receptor, string destino, IReadOnlyList<LineaRemito> mercaderia,
        bool esOriginal = true, DatosTransportista? transportista = null, string? cai = null,
        long? primerNumero = null, long? ultimoNumero = null, DateOnly? vencimiento = null,
        DatosImprentaRemito? imprenta = null, bool esAutoimpresor = false, string? ambitoInterno = null)
    {
        Clase = clase;
        FechaEmision = fechaEmision;
        PuntoVenta = puntoVenta;
        Numero = numero;
        Receptor = receptor;
        Destino = destino;
        Mercaderia = mercaderia;
        EsOriginal = esOriginal;
        Transportista = transportista;
        Cai = cai;
        PrimerNumero = primerNumero;
        UltimoNumero = ultimoNumero;
        Vencimiento = vencimiento;
        Imprenta = imprenta;
        EsAutoimpresor = esAutoimpresor;
        AmbitoInterno = ambitoInterno;
    }

    public ClaseRemito Clase { get; init; }
    public DateOnly FechaEmision { get; init; }
    public int PuntoVenta { get; init; }
    public long Numero { get; init; }
    public DatosReceptor Receptor { get; init; }
    public string Destino { get; init; }
    public IReadOnlyList<LineaRemito> Mercaderia
    {
        get => _mercaderia;
        init => _mercaderia = Array.AsReadOnly(value?.ToArray() ?? []);
    }
    public bool EsOriginal { get; init; }
    public DatosTransportista? Transportista { get; init; }
    public string? Cai { get; init; }
    public long? PrimerNumero { get; init; }
    public long? UltimoNumero { get; init; }
    public DateOnly? Vencimiento { get; init; }
    public DatosImprentaRemito? Imprenta { get; init; }
    public bool EsAutoimpresor { get; init; }
    public string? AmbitoInterno { get; init; }
    public override string ToString() => nameof(DatosRemito);
}

/// <summary>
/// Representación inmutable de un remito generado en la aplicación. Es un documento local, no
/// contiene resultado ni estado de autorización ARCA y no convierte un CAI declarado en verificado.
/// </summary>
public sealed class RemitoGenerado
{
    public RemitoGenerado(int version, ClaseRemito clase, DatosEmisor emisor, DateOnly fechaEmision,
        int puntoVenta, long numero, DatosReceptor receptor, string destino,
        IReadOnlyList<LineaRemito> mercaderia, bool esOriginal, DatosTransportista? transportista,
        string? cai, long? primerNumero, long? ultimoNumero, DateOnly? vencimiento,
        DatosImprentaRemito? imprenta, bool esAutoimpresor, string? ambitoInterno)
    {
        Version = version;
        Clase = clase;
        Emisor = emisor;
        FechaEmision = fechaEmision;
        PuntoVenta = puntoVenta;
        Numero = numero;
        Receptor = receptor;
        Destino = destino;
        Mercaderia = Array.AsReadOnly(mercaderia?.ToArray() ?? []);
        EsOriginal = esOriginal;
        Transportista = transportista;
        Cai = cai;
        PrimerNumero = primerNumero;
        UltimoNumero = ultimoNumero;
        Vencimiento = vencimiento;
        Imprenta = imprenta;
        EsAutoimpresor = esAutoimpresor;
        AmbitoInterno = ambitoInterno;
    }

    public int Version { get; }
    public ClaseRemito Clase { get; }
    public DatosEmisor Emisor { get; }
    public long CuitEmisor => Emisor.Identificacion;
    public DateOnly FechaEmision { get; }
    public int PuntoVenta { get; }
    public long Numero { get; }
    public DatosReceptor Receptor { get; }
    public string Destino { get; }
    public IReadOnlyList<LineaRemito> Mercaderia { get; }
    public bool EsOriginal { get; }
    public DatosTransportista? Transportista { get; }
    public string? Cai { get; }
    public long? PrimerNumero { get; }
    public long? UltimoNumero { get; }
    public DateOnly? Vencimiento { get; }
    public DatosImprentaRemito? Imprenta { get; }
    public bool EsAutoimpresor { get; }
    public string? AmbitoInterno { get; }
    public int? CodigoTipoComprobante => Clase == ClaseRemito.R ? 91 : null;
    public string Leyenda => "DOCUMENTO NO VALIDO COMO FACTURA";
    public string Estado => "Local";
    public override string ToString() => $"RemitoGenerado {{ Version = {Version}, Clase = {Clase}, Estado = Local }}";
}

/// <summary>
/// Crea remitos locales. Verifica estructura declarada conforme a RG 1415/2003, artículos 28-30
/// y Anexo V; no verifica registros, CAI ni habilitaciones ante ARCA. Regímenes sectoriales y
/// controladores fiscales quedan fuera de este modelo.
/// </summary>
public sealed class Remito(DatosEmisor emisor)
{
    private readonly DatosEmisor _emisor = emisor ?? throw new ArgumentNullException(nameof(emisor));

    public RemitoGenerado GenerarComprobante(DatosRemito datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ValidateGenerated(_emisor, datos);

        return new RemitoGenerado(1, datos.Clase, _emisor, datos.FechaEmision,
            datos.PuntoVenta, datos.Numero, datos.Receptor, datos.Destino, datos.Mercaderia,
            datos.EsOriginal, datos.Transportista, datos.Cai, datos.PrimerNumero, datos.UltimoNumero,
            datos.Vencimiento, datos.Imprenta, datos.EsAutoimpresor, datos.AmbitoInterno);
    }

    internal static void ValidateGenerated(DatosEmisor issuer, DatosRemito data)
    {
        ValidateIssuer(issuer);
        ValidateCommon(data);
        if (data.Clase == ClaseRemito.R) ValidateR(data, issuer);
        else if (data.Clase == ClaseRemito.X) ValidateX(data);
        else throw Invalid("La clase de remito debe ser R o X.");
    }

    private static void ValidateIssuer(DatosEmisor issuer)
    {
        if (!ValidCuit(issuer.Identificacion) || string.IsNullOrWhiteSpace(issuer.RazonSocial) ||
            string.IsNullOrWhiteSpace(issuer.Domicilio) || string.IsNullOrWhiteSpace(issuer.CondicionTributaria) ||
            string.IsNullOrWhiteSpace(issuer.IngresosBrutos))
            throw Invalid("El perfil del emisor requiere CUIT, razón social, domicilio, condición tributaria e inscripción IIBB o condición de no contribuyente.");
    }

    private static void ValidateCommon(DatosRemito data)
    {
        if (data.Receptor is null || !ValidCuit(data.Receptor.Identificacion) ||
            string.IsNullOrWhiteSpace(data.Receptor.Nombre) || string.IsNullOrWhiteSpace(data.Receptor.Domicilio) ||
            string.IsNullOrWhiteSpace(data.Receptor.CondicionTributaria))
            throw Invalid("El receptor requiere CUIT, nombre o razón social, domicilio y condición tributaria.");
        if (data.PuntoVenta is < 1 or > 99_998 || data.Numero is < 1 or > 99_999_999)
            throw Invalid("El punto de venta debe tener hasta cinco dígitos y el número hasta ocho, ambos positivos.");
        if (string.IsNullOrWhiteSpace(data.Destino)) throw Invalid("El destino es obligatorio.");
        if (data.Mercaderia is null || data.Mercaderia.Count == 0 || data.Mercaderia.Any(x => x is null ||
            string.IsNullOrWhiteSpace(x.Descripcion) || string.IsNullOrWhiteSpace(x.Unidad) || x.Cantidad <= 0))
            throw Invalid("Se requiere al menos un bien con descripción, unidad y cantidad positiva.");
        if (data.Transportista is { } carrier && (string.IsNullOrWhiteSpace(carrier.RazonSocial) ||
            string.IsNullOrWhiteSpace(carrier.Domicilio) || !ValidCuit(carrier.Cuit)))
            throw Invalid("Los datos del transportista tercero están incompletos.");
    }

    private static void ValidateR(DatosRemito data, DatosEmisor issuer)
    {
        if (data.AmbitoInterno is not null) throw Invalid("El ámbito interno solo corresponde a remitos X.");
        if (data.EsAutoimpresor)
        {
            if (!IsEligibleAutoPrinterStatus(issuer.CondicionTributaria))
                throw Invalid("La excepción de autoimpresor para remitos R requiere condición IVA responsable inscripto o exento.");
            if (!ValidCai(data.Cai) || data.Vencimiento is null || data.FechaEmision > data.Vencimiento)
                throw Invalid("El remito R autoimpresor conserva el CAI y la fecha de vencimiento; la vigencia debe cubrir la fecha histórica de emisión.");
            if (data.PrimerNumero is not null || data.UltimoNumero is not null || data.Imprenta is not null)
                throw Invalid("El autoimpresor no consigna datos de imprenta ni el rango del talonario impreso.");
            if (issuer.InicioActividades is null || data.FechaEmision < issuer.InicioActividades)
                throw Invalid("La fecha de inicio de actividad del emisor es obligatoria y no puede ser posterior al remito.");
            return;
        }

        if (!ValidCai(data.Cai))
            throw Invalid("El remito R requiere un CAI declarado de catorce dígitos.");
        if (data.PrimerNumero is null || data.UltimoNumero is null || data.PrimerNumero is < 1 or > 99_999_999 ||
            data.UltimoNumero is < 1 or > 99_999_999 || data.UltimoNumero < data.PrimerNumero ||
            data.Numero < data.PrimerNumero || data.Numero > data.UltimoNumero)
            throw Invalid("El rango de numeración declarado debe ser válido e incluir el número del remito.");
        if (data.Vencimiento is null || data.FechaEmision > data.Vencimiento)
            throw Invalid("El vencimiento del talonario debe estar informado y cubrir la fecha histórica de emisión.");
        if (data.Imprenta is not { } printer || string.IsNullOrWhiteSpace(printer.RazonSocial) ||
            !ValidCuit(printer.Cuit) || string.IsNullOrWhiteSpace(printer.NumeroHabilitacion) ||
            printer.PrimerNumero != data.PrimerNumero || printer.UltimoNumero != data.UltimoNumero ||
            printer.FechaImpresion > data.FechaEmision)
            throw Invalid("Los datos de imprenta deben estar completos y coincidir con el rango declarado.");
        if (issuer.InicioActividades is null || data.FechaEmision < issuer.InicioActividades)
            throw Invalid("La fecha de inicio de actividad del emisor es obligatoria y no puede ser posterior al remito.");
    }

    private static void ValidateX(DatosRemito data)
    {
        if (data.EsAutoimpresor || data.Cai is not null || data.PrimerNumero is not null || data.UltimoNumero is not null ||
            data.Vencimiento is not null || data.Imprenta is not null)
            throw Invalid("El remito X no lleva CAI, vigencia ni datos de impresión de un talonario R.");
        if (string.IsNullOrWhiteSpace(data.AmbitoInterno))
            throw Invalid("El remito X debe identificar el predio, polo o parque industrial de uso interno.");
    }

    private static bool ValidCuit(long value) => value is >= 10_000_000_000 and <= 99_999_999_999;
    private static bool ValidCai(string? value) => value is { Length: 14 } && value.All(c => c is >= '0' and <= '9');
    private static bool IsEligibleAutoPrinterStatus(string condition)
    {
        string normalized = string.Join(' ', condition.Trim().ToUpperInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized is "RESPONSABLE INSCRIPTO" or "RESPONSABLE INSCRITO" or
            "IVA RESPONSABLE INSCRIPTO" or "IVA RESPONSABLE INSCRITO" or
            "RESPONSABLE INSCRIPTO IVA" or "RESPONSABLE INSCRITO IVA" or
            "EXENTO" or "IVA EXENTO" or "IVA SUJETO EXENTO";
    }
    private static ArgumentException Invalid(string message) => new(message);
}

/// <summary>Valida también snapshots deserializados antes de su representación.</summary>
public static class RemitoValidator
{
    public static void Validate(RemitoGenerado remito)
    {
        ArgumentNullException.ThrowIfNull(remito);
        if (remito.Version != 1) throw new ArgumentException("La versión del snapshot de remito no está soportada.", nameof(remito));
        ArgumentNullException.ThrowIfNull(remito.Emisor);
        var data = new DatosRemito(remito.Clase, remito.FechaEmision, remito.PuntoVenta, remito.Numero,
            remito.Receptor, remito.Destino, remito.Mercaderia, remito.EsOriginal, remito.Transportista,
            remito.Cai, remito.PrimerNumero, remito.UltimoNumero, remito.Vencimiento, remito.Imprenta,
            remito.EsAutoimpresor, remito.AmbitoInterno);
        Remito.ValidateGenerated(remito.Emisor, data);
    }
}
