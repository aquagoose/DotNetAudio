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

        byte dataTypeSizeInBytes = info.Format.Type.Bytes();

        buffer->StereoAlignment = (byte) (dataTypeSizeInBytes * (info.Format.Channels - 1));
        buffer->Alignment = (byte) (dataTypeSizeInBytes * info.Format.Channels);

        return new AudioBuffer(availableBuffer);
    }

    public void DestroyBuffer(AudioBuffer buffer)
    {
        // TODO: Stop all voices from playing the buffer.
        
        NativeMemory.Free(_buffers.Array[buffer.Handle].Data);
        
        _availableBufferSlots.Enqueue(buffer.Handle);
    }

    public void PlayBuffer(AudioBuffer buffer, uint voice, in VoiceProperties properties)
    {
        SoundBuffer* buf = &_buffers.Array[buffer.Handle];

        Voice* v = &_voices.Array[voice];

        v->Position = 0;
        v->FinePosition = 0.0;

        v->Speed = buf->Info.Format.SampleRate / (double) SampleRate;
        v->Properties = properties;
        
        v->Buffer = buf;
        v->Playing = true;
    }

    public ref VoiceProperties GetVoicePropertiesRef(uint voice)
        => ref _voices.Array[voice].Properties;

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
                AudioFormat* format = &buf->Info.Format;
                VoiceProperties* props = &voice->Properties;

                nuint bufferPosition = voice->Position * buf->Alignment;
                
                buffer[i + 0] += GetSample(buf->Data, bufferPosition, format->Type) * props->Volume;
                buffer[i + 1] += GetSample(buf->Data, bufferPosition + buf->StereoAlignment, format->Type) * props->Volume;

                voice->FinePosition += voice->Speed * props->Pitch;

                voice->Position += (nuint) voice->FinePosition;
                voice->FinePosition -= (int) voice->FinePosition;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static float GetSample(byte* buffer, nuint alignedPosition, DataType dType)
    {
        switch (dType)
        {
            case DataType.I8:
                return (sbyte) buffer[alignedPosition] / (float) sbyte.MaxValue;
            case DataType.U8:
                return (buffer[alignedPosition] - sbyte.MaxValue) / (float) sbyte.MaxValue;
            case DataType.I16:
                return (short) (buffer[alignedPosition] | (buffer[alignedPosition + 1] << 8)) / (float) short.MaxValue;
            case DataType.U16:
                throw new NotImplementedException();
            case DataType.I32:
                return (buffer[alignedPosition] | (buffer[alignedPosition + 1] << 8) |
                       (buffer[alignedPosition + 2] << 16) | (buffer[alignedPosition + 3] << 24)) / (float) int.MaxValue;
            case DataType.F32:
                int b = buffer[alignedPosition] | (buffer[alignedPosition + 1] << 8) |
                        (buffer[alignedPosition + 2] << 16) | (buffer[alignedPosition + 3] << 24);

                // Have to reinterpret cast the int to float.
                return *(float*) &b;
            default:
                throw new ArgumentOutOfRangeException(nameof(dType), dType, null);
        }
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

        // Used for stereo sounds - represents the offset in bytes between the left and the right channels.
        // A mono buffer will have a stereo alignment of 0.
        public byte StereoAlignment;
        
        // The alignment, in bytes, of this buffer. This determines how many bytes per full sample there are.
        public byte Alignment;
    }

    private struct Voice
    {
        // The current buffer, or null for none.
        public SoundBuffer* Buffer;

        public nuint Position;
        public double FinePosition;

        public double Speed;

        public nuint EndPosition;
        
        public bool Playing;

        public VoiceProperties Properties;
    }
}