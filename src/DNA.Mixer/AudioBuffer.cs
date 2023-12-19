namespace DNA.Mixer;

public struct AudioBuffer
{
    public nuint Handle;

    public AudioBuffer(nuint handle)
    {
        Handle = handle;
    }
}