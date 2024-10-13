using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MemoryPack;

namespace Erinn
{
    public sealed unsafe class TestStream : IBufferWriter<byte>
    {
        public int BytesWritten;
        public int BytesRead;
        public NativeChunkedStream Stream = new(512, 1);
        public NativeArray<byte> Buffer;

        public void Advance(int count)
        {
            var buffer = stackalloc byte[4];
            Console.WriteLine(Buffer.Length+" "+count);
            Unsafe.WriteUnaligned(buffer, count);
            Stream.Write(buffer, 4);
            Stream.Write(Buffer.AsSpan(0,count));
            BytesWritten += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => throw new NotSupportedException();

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Buffer = stackalloc byte[sizeHint];
            return Buffer.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>()
        {
            var obj = default(T);
            var buffer = stackalloc byte[4];
            Stream.Read(buffer, 4);
            var length = Unsafe.ReadUnaligned<int>(buffer);
        
            var newBuffer = stackalloc byte[length];
            var span = MemoryMarshal.CreateReadOnlySpan(ref *newBuffer, length);
            BytesRead += MemoryPackSerializer.Deserialize(span, ref obj);
            return obj;
        }

        public void Write<T>(in T obj) => MemoryPackSerializer.Serialize(this, in obj);
    }

    public sealed class Program
    {
        private static void Main()
        {
            var d = new TestStream();

            Test(d);

            var b = d.Read<int>();
            var c = d.Read<string>();

            Console.WriteLine(b);
            Console.WriteLine(c);
        }

        private static void Test(TestStream d)
        {
            d.Write(100);
            d.Write("sb");
        }
    }
}