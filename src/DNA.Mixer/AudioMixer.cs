namespace DNA.Mixer;

public unsafe class AudioMixer
{
    private NativeList<SoundBuffer> _buffers;

    public readonly uint SampleRate;

    public readonly uint NumVoices;

    public AudioMixer(uint sampleRate, uint numVoices)
    {
        SampleRate = sampleRate;
        NumVoices = numVoices;
        
        _buffers = new NativeList<SoundBuffer>();
        
        
    }
    
    private struct SoundBuffer
    {
        public byte* Data;
        
        public nuint DataLength;

        public AudioFormat Format;
    }
}