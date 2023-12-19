using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DNA.Mixer;

public unsafe class AudioMixer : IDisposable
{
    private NativeList<SoundBuffer> _buffers;

    // Tells the CreateBuffer function which buffers in our array are free.
    private Queue<nuint> _availableBufferSlots;

    public readonly uint SampleRate;

    public readonly uint NumVoices;

    public AudioMixer(uint sampleRate, uint numVoices)
    {
        SampleRate = sampleRate;
        NumVoices = numVoices;
        
        _buffers = new NativeList<SoundBuffer>();
        _availableBufferSlots = new Queue<nuint>();
    }
    
    ~AudioMixer()
    {
        Dispose();
    }

    public AudioBuffer CreateBuffer<T>(in BufferInfo info, in ReadOnlySpan<T> data) where T : unmanaged
    {
        if (!_availableBufferSlots.TryDequeue(out nuint availableBuffer))
            availableBuffer = _buffers.Add(new SoundBuffer());
        
        SoundBuffer* buffer = &_buffers.Array[availableBuffer];

        nuint dataLength = (nuint) (data.Length * sizeof(T));

        buffer->Data = NativeMemory.Alloc(dataLength);

        fixed (void* pData = data)
            Unsafe.CopyBlock(buffer->Data, pData, (uint) dataLength);

        buffer->DataLength = dataLength;

        buffer->Info = info;

        return new AudioBuffer(availableBuffer);
    }

    public void DestroyBuffer(AudioBuffer buffer)
    {
        NativeMemory.Free(_buffers.Array[buffer.Handle].Data);
        
        _availableBufferSlots.Enqueue(buffer.Handle);
    }
    
    private struct SoundBuffer
    {
        public void* Data;
        
        public nuint DataLength;

        public BufferInfo Info;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _buffers.Dispose();
    }
}