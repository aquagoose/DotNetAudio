namespace DNA.Mixer;

public struct BufferInfo
{
    public AudioFormat Format;
    
    public BufferType Type;

    public BufferInfo(BufferType type, AudioFormat format)
    {
        Type = type;
        Format = format;
    }
}