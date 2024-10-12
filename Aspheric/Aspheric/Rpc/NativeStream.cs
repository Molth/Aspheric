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
    ///     Native stream
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeCollection]
    public readonly unsafe struct NativeStream : IDisposable, IEquatable<NativeStream>, IBufferWriter<byte>
    {
        /// <summary>
        ///     Handle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeStreamHandle
        {
            /// <summary>
            ///     Buffer
            /// </summary>
            public NativeSlice<byte> Buffer;

            /// <summary>
            ///     Bytes read
            /// </summary>
            public int BytesRead;

            /// <summary>
            ///     Bytes written
            /// </summary>
            public int BytesWritten;
        }

        /// <summary>
        ///     Handle
        /// </summary>
        private readonly NativeStreamHandle* _handle;

        /// <summary>
        ///     Structure
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeStream() => _handle = (NativeStreamHandle*)NativeMemoryAllocator.AllocZeroed((uint)sizeof(NativeStreamHandle));

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
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => NativeMemoryAllocator.Free(_handle);

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
        public Span<byte> GetSpan(int sizeHint = 0) => _handle->Buffer.AsSpan(BytesWritten, sizeHint);

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsCreated => _handle != null;

        /// <summary>
        ///     Is empty
        /// </summary>
        public bool IsEmpty => BytesWritten <= BytesRead;

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
            get => _handle->Buffer.Count - BytesWritten;
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
        public void SetBuffer(NativeSlice<byte> buffer) => _handle->Buffer = buffer;

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
            BytesRead += MemoryPackSerializer.Deserialize(_handle->Buffer.AsSpan(BytesRead), ref obj);
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
            BytesRead += MemoryPackSerializer.Deserialize(_handle->Buffer.AsSpan(BytesRead), ref obj);
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
            Unsafe.CopyBlockUnaligned(buffer, _handle->Buffer.Array + _handle->Buffer.Offset + BytesRead, (uint)length);
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
            Unsafe.CopyBlockUnaligned(_handle->Buffer.Array + _handle->Buffer.Offset + BytesWritten, buffer, (uint)length);
            BytesWritten += length;
        }

        /// <summary>
        ///     As span
        /// </summary>
        /// <returns>Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan() => _handle->Buffer.AsSpan(0, _handle->BytesWritten);
    }
}