namespace NetArcaWs.EntityFrameworkCore;

internal enum WsaaTicketState
{
    Pending = 1,
    Unknown = 2,
    Ticket = 3
}

/// <summary>Durable coordination row for one WSAA remote credential identity.</summary>
internal sealed class WsaaTicketEntity
{
    public string KeyHash { get; set; } = "";
    public string CertificateHash { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Service { get; set; } = "";
    public int State { get; set; }
    public string? OwnerId { get; set; }
    public long Fence { get; set; }
    public long Version { get; set; }
    public long? LeaseUntilUtcTicks { get; set; }
    public long? ExpiresUtcTicks { get; set; }
    public string? KeyId { get; set; }
    public byte[]? Nonce { get; set; }
    public byte[]? Ciphertext { get; set; }
    public byte[]? Tag { get; set; }
    public long UpdatedUtcTicks { get; set; }
}
