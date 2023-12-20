namespace DNA.Mixer;

public enum DataType : byte
{
    I8,
    U8,
    
    I16,
    U16,
    
    // I24,
    // U24, ???
    
    I32,
    
    // I64,
    
    F32,
    
    // F64
}

public static class DataTypeExtensions
{
    public static byte Bits(this DataType type)
    {
        return type switch
        {
            DataType.I8 => 8,
            DataType.U8 => 8,
            DataType.I16 => 16,
            DataType.U16 => 16,
            DataType.I32 => 32,
            DataType.F32 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static byte Bytes(this DataType type) => (byte) (type.Bits() / 8);
}