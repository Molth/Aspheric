using Erinn;

namespace App;

internal sealed class Program
{
    static unsafe void Main()
    {
        NativeSlice<byte> buffer = stackalloc byte[1024];
        NativeStream d = stackalloc byte[8];
        d.Flush();
        d.SetBuffer(buffer);
        Test(d);

        var b = d.Read<int>();
        var c = d.Read<string>();
        
        Console.WriteLine(b);
        Console.WriteLine(c);
    }

    static void Test(NativeStream d)
    {
        d.Write(100);
        d.Write("sb");
    }
}