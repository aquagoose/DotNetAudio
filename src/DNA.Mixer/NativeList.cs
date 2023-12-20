using System.Runtime.InteropServices;

namespace DNA.Mixer;

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    private nuint _capacity;
    
    public T* Array;

    public nuint Length;

    public NativeList()
    {
        Length = 0;
        _capacity = 1;
        Array = (T*) NativeMemory.AllocZeroed(_capacity * (nuint) sizeof(T));
    }

    public NativeList(nuint initialCapacity)
    {
        Length = 0;

        _capacity = initialCapacity;

        Array = (T*) NativeMemory.AllocZeroed(initialCapacity * (nuint) sizeof(T));
    }

    public nuint Add(in T item)
    {
        if (Length + 1 > _capacity)
        {
            _capacity <<= 1;
            NativeMemory.Realloc(Array, _capacity * (nuint) sizeof(T));
        }

        Array[Length++] = item;

        return Length - 1;
    }

    public void Dispose()
    {
        NativeMemory.Free(Array);
    }
}