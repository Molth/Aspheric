using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MemoryPack;

#pragma warning disable CS8601
#pragma warning disable CS8603

// ReSharper disable ALL

namespace Erinn
{
    /// <summary>
    ///     Data stream
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeCollection]
    public unsafe struct DataStream : IEquatable<DataStream>, IBufferWriter<byte>
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
        ///     Buffer
        /// </summary>
        private NativeSlice<byte> _buffer;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataStream(byte* array, int length)
        {
            if (length < 8)
                throw new ArgumentOutOfRangeException(nameof(length), $"Requires size is {8}, but buffer length is {length}.");
            _handle = (NativeStreamHandle*)array;
            _buffer = new NativeSlice<byte>(array + 8, length - 8);
        }

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="other">Other</param>
        /// <returns>Equals</returns>
        public bool Equals(DataStream other) => other == this;

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>Equals</returns>
        public override bool Equals(object? obj) => obj is DataStream dataStream && dataStream == this;

        /// <summary>
        ///     Get hashCode
        /// </summary>
        /// <returns>HashCode</returns>
        public override int GetHashCode() => (int)(nint)_handle;

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString() => "DataStream";

        /// <summary>
        ///     Equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Equals</returns>
        public static bool operator ==(DataStream left, DataStream right) => left._handle == right._handle;

        /// <summary>
        ///     Not equals
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Not equals</returns>
        public static bool operator !=(DataStream left, DataStream right) => left._handle != right._handle;

        /// <summary>
        ///     Advance
        /// </summary>
        /// <param name="count">Count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) => BytesWritten += count;

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
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var bytesCanWrite = BytesCanWrite;
            if (bytesCanWrite < sizeHint)
                MemoryPackSerializationException.ThrowInvalidRange(sizeHint, bytesCanWrite);
            return _buffer.AsSpan(BytesWritten, sizeHint);
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => BytesWritten <= BytesRead;

        /// <summary>
        ///     Bytes
        /// </summary>
        public byte* Bytes => _buffer.Array + _buffer.Offset;

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
            get => _buffer.Count - BytesWritten;
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
        public void SetBuffer(NativeSlice<byte> buffer)
        {
            _buffer = buffer;
            BytesRead = 0;
            BytesWritten = buffer.Count;
        }

        /// <summary>
        ///     Set bytes read
        /// </summary>
        /// <param name="bytesRead">Bytes read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBytesRead(int bytesRead)
        {
            if (bytesRead < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesRead), bytesRead, "MustBeNonNegative");
            BytesRead = bytesRead;
        }

        /// <summary>
        ///     Set bytes written
        /// </summary>
        /// <param name="bytesWritten">Bytes written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBytesWritten(int bytesWritten)
        {
            if (bytesWritten < 0)
                throw new ArgumentOutOfRangeException(nameof(bytesWritten), bytesWritten, "MustBeNonNegative");
            BytesWritten = bytesWritten;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T Read<T>()
        {
            var handle = _handle;
            var obj = default(T);
            handle->BytesRead += MemoryPackSerializer.Deserialize(_buffer.AsReadOnlySpan(handle->BytesRead), ref obj);
            return obj;
        }

        /// <summary>
        ///     Read
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <returns>object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Read<T>(ref T obj)
        {
            var handle = _handle;
            handle->BytesRead += MemoryPackSerializer.Deserialize(_buffer.AsReadOnlySpan(handle->BytesRead), ref obj);
        }

        /// <summary>
        ///     Read bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ReadBytes(byte* buffer, int length)
        {
            var handle = _handle;
            var bytesCanRead = handle->BytesWritten - handle->BytesRead;
            if (length > bytesCanRead)
                MemoryPackSerializationException.ThrowInvalidRange(length, bytesCanRead);
            Unsafe.CopyBlockUnaligned(buffer, _buffer.Array + _buffer.Offset + handle->BytesRead, (uint)length);
            handle->BytesRead += length;
        }

        /// <summary>
        ///     Write
        /// </summary>
        /// <param name="obj">object</param>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Write<T>(in T obj) => MemoryPackSerializer.Serialize(this, in obj);

        /// <summary>
        ///     Write bytes
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void WriteBytes(byte* buffer, int length)
        {
            var handle = _handle;
            var bytesCanWrite = _buffer.Count - handle->BytesWritten;
            if (length > bytesCanWrite)
                MemoryPackSerializationException.ThrowInvalidRange(length, bytesCanWrite);
            Unsafe.CopyBlockUnaligned(_buffer.Array + _buffer.Offset + handle->BytesWritten, buffer, (uint)length);
            handle->BytesWritten += length;
        }

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => _buffer.AsSpan(0, BytesWritten);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start) => _buffer.AsSpan(start, BytesWritten - start);

        /// <summary>
        ///     As span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Count</param>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int start, int count) => _buffer.AsSpan(start, count);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan() => _buffer.AsReadOnlySpan(0, BytesWritten);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int start) => _buffer.AsReadOnlySpan(start, BytesWritten - start);

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Count</param>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan(int start, int count) => _buffer.AsReadOnlySpan(start, count);

        /// <summary>
        ///     As native stream
        /// </summary>
        /// <param name="span">Span</param>
        /// <returns>DataStream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DataStream(Span<byte> span) => new((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);

        /// <summary>
        ///     As native stream
        /// </summary>
        /// <param name="span">ReadOnlySpan</param>
        /// <returns>DataStream</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DataStream(ReadOnlySpan<byte> span) => new((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length);

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte>(DataStream dataStream) => dataStream.AsSpan();

        /// <summary>
        ///     As readOnly span
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(DataStream dataStream) => dataStream.AsReadOnlySpan();

        /// <summary>
        ///     As native array
        /// </summary>
        /// <returns>NativeArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<byte>(DataStream dataStream) => new(dataStream._buffer.Array + dataStream._buffer.Offset, dataStream.BytesWritten);

        /// <summary>
        ///     As native memory array
        /// </summary>
        /// <returns>NativeMemoryArray</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeMemoryArray<byte>(DataStream dataStream) => new(dataStream._buffer.Array + dataStream._buffer.Offset, dataStream.BytesWritten);

        /// <summary>
        ///     As native slice
        /// </summary>
        /// <returns>NativeSlice</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSlice<byte>(DataStream dataStream) => dataStream._buffer[..dataStream.BytesWritten];
    }
}