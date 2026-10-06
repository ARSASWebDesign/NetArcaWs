namespace NetArcaWs.Transport;

/// <summary>Stops XML serialization before an in-memory buffer can grow without a bound.</summary>
internal sealed class BoundedMemoryStream(int maximumBytes) : MemoryStream
{
    private void CheckWrite(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > maximumBytes - Position) throw new FormatException("Serialized XML exceeds the configured size limit.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        CheckWrite(count);
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckWrite(buffer.Length);
        base.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        CheckWrite(1);
        base.WriteByte(value);
    }

    public override void SetLength(long value)
    {
        if (value > maximumBytes) throw new FormatException("Serialized XML exceeds the configured size limit.");
        base.SetLength(value);
    }
}
