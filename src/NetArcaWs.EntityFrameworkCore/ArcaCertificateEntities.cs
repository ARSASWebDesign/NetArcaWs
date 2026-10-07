namespace NetArcaWs.EntityFrameworkCore;

internal sealed class ArcaCertificateSlotEntity
{
    public string TenantHash { get; set; } = "";
    public long Cuit { get; set; }
    public int Environment { get; set; }
    public Guid ActiveVersionId { get; set; }
    public long Generation { get; set; }
}

internal sealed class ArcaCertificateVersionEntity
{
    public string TenantHash { get; set; } = "";
    public long Cuit { get; set; }
    public int Environment { get; set; }
    public Guid VersionId { get; set; }
    public long CreatedAtUtcTicks { get; set; }
    public string ThumbprintSha256 { get; set; } = "";
    public long NotBeforeUtcTicks { get; set; }
    public long NotAfterUtcTicks { get; set; }
    public string KeyId { get; set; } = "";
    public byte[] Nonce { get; set; } = [];
    public byte[] Ciphertext { get; set; } = [];
    public byte[] Tag { get; set; } = [];
}
