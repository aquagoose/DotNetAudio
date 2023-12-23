using DNA.Base;
using DNA.Streaming.Exceptions;

namespace DNA.Streaming;

public class Wav : IStream
{
    private const uint Riff = 0x46464952;
    private const uint Wave = 0x45564157;
    private const uint Fmt  = 0x20746D66;
    private const uint Data = 0x61746164;
    
    private BinaryReader _reader;
    private uint _dataLength;
    private long _dataStartPosition;
    
    public AudioFormat Format { get; }

    public Wav(string path) : this(new BufferedStream(File.OpenRead(path))) { }

    public Wav(byte[] data) : this(new MemoryStream(data)) { }

    public Wav(Stream stream)
    {
        _reader = new BinaryReader(stream);

        if (_reader.ReadUInt32() != Riff)
            throw new WavReadException("Expected RIFF, was not found. Is this file a wav file?");

        _ = _reader.ReadUInt32(); // File size.
        
        if (_reader.ReadUInt32() != Wave)
            throw new WavReadException("Expected WAVE, was not found. This wav file may be in a format that is not supported.");

        // Keep reading until data is read. The wav specification guarantees that the data will not be at the very start
        // of the file, so this is safe to perform.
        while (_dataStartPosition == 0)
        {
            uint chunk = _reader.ReadUInt32();
            uint chunkSize = _reader.ReadUInt32();

            switch (chunk)
            {
                case Fmt:
                    ushort formatType = _reader.ReadUInt16();
                    ushort channels = _reader.ReadUInt16();
                    uint sampleRate = _reader.ReadUInt32();

                    _ = _reader.ReadUInt32(); // Bytes per second.
                    _ = _reader.ReadUInt16(); // Bytes per sample.

                    ushort bitsPerSample = _reader.ReadUInt16();

                    if (formatType is not (1 or 3))
                        throw new WavReadException($"Unsupported format type '{formatType}'.");

                    AudioFormat format = new AudioFormat()
                    {
                        Channels = (byte) channels,
                        SampleRate = sampleRate
                    };

                    format.Type = bitsPerSample switch
                    {
                        8 => DataType.U8,
                        16 => DataType.I16,
                        32 => formatType == 3 ? DataType.F32 : DataType.I32,
                        _ => throw new ArgumentOutOfRangeException(nameof(bitsPerSample), bitsPerSample, null)
                    };

                    Format = format;

                    break;
                
                case Data:
                    _dataStartPosition = _reader.BaseStream.Position;
                    _dataLength = chunkSize;
                    break;
                
                default:
                    // Ignore any chunks we don't care about.
                    _reader.BaseStream.Position += chunkSize;
                    break;
            }
        }
    }
    
    public ulong GetBuffer(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public byte[] GetPCM()
    {
        _reader.BaseStream.Position = _dataStartPosition;
        return _reader.ReadBytes((int) _dataLength);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}