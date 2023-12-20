using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DNA.Mixer;

public unsafe class AudioMixer : IDisposable
{
    private NativeList<SoundBuffer> _buffers;

    // Tells the CreateBuffer function which buffers in our array are free.
    private Queue<nuint> _availableBufferSlots;

    private NativeList<Voice> _voices;

    public readonly uint SampleRate;

    public readonly uint NumVoices;

    public AudioMixer(uint sampleRate, uint numVoices)
    {
        SampleRate = sampleRate;
        NumVoices = numVoices;
        
        _buffers = new NativeList<SoundBuffer>();
        
        _availableBufferSlots = new Queue<nuint>();

        _voices = new NativeList<Voice>(numVoices);
        for (uint i = 0; i < numVoices; i++)
            _voices.Array[i] = new Voice();
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

        buffer->Data = (byte*) NativeMemory.Alloc(dataLength);

        fixed (void* pData = data)
            Unsafe.CopyBlock(buffer->Data, pData, (uint) dataLength);

        buffer->DataLength = dataLength;
        buffer->Info = info;

        return new AudioBuffer(availableBuffer);
    }

    public void DestroyBuffer(AudioBuffer buffer)
    {
        // TODO: Stop all voices from playing the buffer.
        
        NativeMemory.Free(_buffers.Array[buffer.Handle].Data);
        
        _availableBufferSlots.Enqueue(buffer.Handle);
    }

    public void PlayBuffer(AudioBuffer buffer, uint voice)
    {
        SoundBuffer* buf = &_buffers.Array[buffer.Handle];

        Voice* v = &_voices.Array[voice];

        v->Position = 0;
        v->FinePosition = 0.0;
        
        v->Buffer = buf;
        v->Playing = true;
    }

    public void MixStereoFloat(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i += 2)
        {
            buffer[i + 0] = 0;
            buffer[i + 1] = 0;
            
            for (uint c = 0; c < NumVoices; c++)
            {
                Voice* voice = &_voices.Array[c];
                
                if (!voice->Playing)
                    continue;
                
                SoundBuffer* buf = voice->Buffer;
                
                buffer[i + 0] = GetSample(buf->Data, voice->Position, buf->Info.Format.Type);
                buffer[i + 1] = GetSample(buf->Data, voice->Position + 2, buf->Info.Format.Type);

                voice->Position += 4;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private float GetSample(byte* buffer, nuint alignedPosition, DataType dType)
    {
        return dType switch
        {
            DataType.I8 => throw new NotImplementedException(),
            DataType.U8 => (buffer[alignedPosition] - sbyte.MaxValue) / (float) sbyte.MaxValue,
            DataType.I16 => (short) (buffer[alignedPosition] | (buffer[alignedPosition + 1] << 8)) / (float) short.MaxValue,
            DataType.U16 => throw new NotImplementedException(),
            DataType.I32 => throw new NotImplementedException(),
            DataType.F32 => (float) (buffer[alignedPosition] | (buffer[alignedPosition + 1] << 8) | (buffer[alignedPosition + 2] << 16) | (buffer[alignedPosition + 3] << 24)),
            _ => throw new ArgumentOutOfRangeException(nameof(dType), dType, null)
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _voices.Dispose();
        _buffers.Dispose();
    }
    
    private struct SoundBuffer
    {
        public byte* Data;
        
        public nuint DataLength;

        public BufferInfo Info;

        public byte Alignment;
    }

    private struct Voice
    {
        // The current buffer, or null for none.
        public SoundBuffer* Buffer;

        public nuint Position;
        public double FinePosition;

        public nuint EndPosition;
        
        public bool Playing;
    }
}