namespace DNA.Mixer;

public struct AudioBuffer
{
    public ulong Handle;

    public AudioBuffer(ulong handle)
    {
        Handle = handle;
    }
}