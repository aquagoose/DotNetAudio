using System.Runtime.InteropServices;

namespace DNA.Mixer;

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    private nuint _capacity;
    
    public T* Array;

    public nuint Length;

    public NativeList(nuint initialCapacity = 1)
    {
        Length = 0;

        _capacity = initialCapacity;

        Array = (T*) NativeMemory.Alloc(initialCapacity * (nuint) sizeof(T));
    }

    public void Add(in T item)
    {
        if (++Length >= _capacity)
        {
            _capacity <<= 1;
            NativeMemory.Realloc(Array, _capacity * (nuint) sizeof(T));
        }

        Array[Length] = item;
    }

    public void Dispose()
    {
        NativeMemory.Free(Array);
    }
}