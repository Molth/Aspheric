using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erinn
{
    /// <summary>
    ///     Network event
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkOnConnectedEvent : IDisposable
    {
        /// <summary>
        ///     Events
        /// </summary>
        private readonly NativeHashSet<nint> _events;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="capacity">Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkOnConnectedEvent(int capacity) => _events = new NativeHashSet<nint>(capacity);

        /// <summary>
        ///     Add
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(OnConnectedDelegate @delegate)
        {
            var methodInfo = @delegate.Method;
            if (!methodInfo.IsStatic || methodInfo.DeclaringType == null)
                throw new UnreachableException(nameof(@delegate));
            _events.Add(methodInfo.MethodHandle.GetFunctionPointer());
        }

        /// <summary>
        ///     Add
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(delegate* managed<in NetworkPeer, void> @event) => _events.Add((nint)@event);

        /// <summary>
        ///     Remove
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(delegate* managed<in NetworkPeer, void> @event) => _events.Remove((nint)@event);

        /// <summary>
        ///     Add
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(nint @event) => _events.Add(@event);

        /// <summary>
        ///     Remove
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(nint @event) => _events.Remove(@event);

        /// <summary>
        ///     Clear
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _events.Clear();

        /// <summary>
        ///     Ensure capacity
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity) => _events.EnsureCapacity(capacity);

        /// <summary>
        ///     Trim excess
        /// </summary>
        /// <returns>New capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TrimExcess() => _events.TrimExcess();

        /// <summary>
        ///     Invoke
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(in NetworkPeer peer)
        {
            foreach (var value in _events)
            {
                var @event = (delegate* managed<in NetworkPeer, void>)value;
                @event(peer);
            }
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _events.Dispose();
    }
}