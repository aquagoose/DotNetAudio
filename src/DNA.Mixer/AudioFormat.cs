namespace DNA.Mixer;

public struct AudioFormat
{
    public uint SampleRate;
    
    public DataType Type;

    public byte Channels;

    public AudioFormat(DataType type, uint sampleRate, byte channels)
    {
        SampleRate = sampleRate;
        Type = type;
        Channels = channels;
    }
}