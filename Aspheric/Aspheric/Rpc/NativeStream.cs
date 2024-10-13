using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MemoryPack;

#pragma warning disable CS8601
#pragma warning disable CS8603

namespace Erinn
{
    /// <summary>
    ///     Native stream
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeCollection]
    public unsafe struct NativeStream : IEquatable<NativeStream>, IBufferWriter<byte>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct NativeStreamHandle
        {
            /// <summary>
            ///     Bytes read
            /// </summary>
            [FieldOffset(0)] public int BytesRead;

            /// <summary>
            ///     Bytes written
            /// </summary>
            [FieldOffset(4)] public int BytesWritten;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeStreamHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="handle">Handle</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeStream(byte* handle) => _handle = (NativeStreamHandle*)handle;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(NativeStream other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is NativeStream nativeStream && nativeStream == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "NativeStream";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(NativeStream left, NativeStream right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(NativeStream left, NativeStream right) => left._handle != right._handle;

        /// <summary>
        ///     Advance
        /// </summary>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            var bytesWritten = BytesWritten + count;
            if (bytesWritten < 0)
                MemoryPackSerializationException.ThrowInvalidRange(count, BytesWritten);
            var bytesCanWrite = BytesCanWrite;
            if (bytesCanWrite < count)
                MemoryPackSerializationException.ThrowInvalidRange(count, bytesCanWrite);
            BytesWritten = bytesWritten;
        }

        /// <summary>
        ///     Get Memory
        /// </summary>
        /// <param name="sizeHint">Size hint</param>
        /// <returns>Memory</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0) => throw new NotSupportedException();

        /// <summary>
        ///     Get Span
        /// </summary>
        /// <param name="sizeHint">Size hint</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0) => Buffer.AsSpan(BytesWritten, sizeHint);

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => BytesWritten <= BytesRead;

        /// <summary>
        ///     Buffer
        /// </summary>
        public NativeSlice<byte> Buffer { get; private set; }

        /// <summary>
        ///     Bytes read
        /// </summary>
        public int BytesRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->BytesRead;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => _handle->BytesRead = value;
        }

        /// <summary>
        ///     Bytes written
        /// </summary>
        public int BytesWritten
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle->BytesWritten;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => _handle->BytesWritten = value;
        }

        /// <summary>
        ///     Bytes can read
        /// </summary>
        public int BytesCanRead
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BytesWritten - BytesRead;
        }

        /// <summary>
        ///     Bytes can write
        /// </summary>
        public int BytesCanWrite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.Count - BytesWritten;
        }

        /// <summary>
        ///     Flush
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            BytesRead = 0;
            BytesWritten = 0;
        }

        /// <summary>
        ///     Set buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBuffer(NativeSlice<byte> buffer) => Buffer = buffer;

        /// <summary>
        ///     Read
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>()
        {
            if (BytesCanRead <= 0)
                MemoryPackSerializationException.ThrowSequenceReachedEnd();
            var obj = default(T);
            BytesRead += MemoryPackSerializer.Deserialize(Buffer.AsReadOnlySpan(BytesRead), ref obj);
            return obj;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read<T>(ref T obj)
        {
            if (BytesCanRead <= 0)
                MemoryPackSerializationException.ThrowSequenceReachedEnd();
            BytesRead += MemoryPackSerializer.Deserialize(Buffer.AsReadOnlySpan(BytesRead), ref obj);
        }

        /// <summary>
        ///     Read bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte* buffer, int length)
        {
            var bytesCanRead = BytesCanRead;
            if (length > bytesCanRead)
                throw new ArgumentOutOfRangeException(nameof(length), $"Requires size is {length}, but buffer length is {bytesCanRead}.");
            Unsafe.CopyBlockUnaligned(buffer, Buffer.Array + Buffer.Offset + BytesRead, (uint)length);
            BytesRead += length;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(in T obj) => MemoryPackSerializer.Serialize(this, in obj);

        /// <summary>
        ///     Write bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte* buffer, int length)
        {
            var bytesCanWrite = BytesCanWrite;
            if (length > bytesCanWrite)
                throw new ArgumentOutOfRangeException(nameof(length), $"Requires size is {length}, but buffer length is {bytesCanWrite}.");
            Unsafe.CopyBlockUnaligned(Buffer.Array + Buffer.Offset + BytesWritten, buffer, (uint)length);
            BytesWritten += length;
        }

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => Buffer.AsSpan(0, BytesWritten);

        /// <summary>
        ///     As native stream
        /// </summary>
        /// <param name="span">Span</param>
        /// <returns>NativeStream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeStream(Span<byte> span)
        {
            if (span.Length < 8)
                throw new ArgumentOutOfRangeException(nameof(span), $"Requires size is 8, but buffer length is {span.Length}.");
            return new NativeStream((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)));
        }
    }
}