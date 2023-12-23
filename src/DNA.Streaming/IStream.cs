using DNA.Base;

namespace DNA.Streaming;

public interface IStream : IDisposable
{
    public AudioFormat Format { get; }

    public ulong GetBuffer(Span<byte> buffer);

    public byte[] GetPCM();
}