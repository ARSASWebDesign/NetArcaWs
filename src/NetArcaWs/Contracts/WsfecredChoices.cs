namespace NetArcaWs.Contracts.WsfeCred;

/// <summary>Preserves the mutually exclusive identifier alternatives required by the WSDL.</summary>
public partial class IdCtaCteType
{
    public bool ShouldSerializeCodCtaCteValue()
    {
        ValidateChoice();
        return CodCtaCteValueSpecified;
    }

    public bool ShouldSerializeIdFactura()
    {
        ValidateChoice();
        return IdFactura is not null;
    }

    private void ValidateChoice()
    {
        if (CodCtaCteValueSpecified == (IdFactura is not null))
            throw new InvalidOperationException("IdCtaCte requires exactly one of CodCtaCte or IdFactura.");
    }
}

/// <summary>Preserves the mutually exclusive transfer alternatives required by the WSDL.</summary>
public partial class InfoTransferenciaType
{
    public bool ShouldSerializeInfoAgtDptoCltv()
    {
        ValidateChoice();
        return InfoAgtDptoCltv is not null;
    }

    public bool ShouldSerializeInfoSca()
    {
        ValidateChoice();
        return InfoSca is not null;
    }

    private void ValidateChoice()
    {
        if ((InfoAgtDptoCltv is not null) == (InfoSca is not null))
            throw new InvalidOperationException("InfoTransferencia requires exactly one of InfoAgtDptoCltv or InfoSca.");
    }
}
