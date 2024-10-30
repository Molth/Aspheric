using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erinn
{
    /// <summary>
    ///     Network event
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct NetworkOnErroredEvent : IDisposable
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
        public NetworkOnErroredEvent(int capacity) => _events = new NativeHashSet<nint>(capacity);

        /// <summary>
        ///     Add
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(OnErroredDelegate @delegate)
        {
            var methodInfo = @delegate.Method;
            if (!methodInfo.IsStatic || methodInfo.DeclaringType == null || methodInfo.DeclaringType.IsNested)
                throw new UnreachableException(nameof(@delegate));
            _events.Add(methodInfo.MethodHandle.GetFunctionPointer());
        }

        /// <summary>
        ///     Adds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Adds()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                Adds(assembly);
        }

        /// <summary>
        ///     Adds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Adds(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<RpcServiceAttribute>() != null)
                    Adds(type);
            }
        }

        /// <summary>
        ///     Adds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Adds(Type type)
        {
            if (!((type.IsClass || type.IsValueType) && !type.IsNested))
                return;
            foreach (var methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = methodInfo.GetCustomAttribute<OnErroredAttribute>();
                if (attribute != null && IsValid(methodInfo))
                    _events.Add(methodInfo.MethodHandle.GetFunctionPointer());
            }
        }

        /// <summary>
        ///     Add
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(delegate* managed<in NetworkPeer, in NetworkPacketFlag, in Span<byte>, in Exception, void> @event) => _events.Add((nint)@event);

        /// <summary>
        ///     Removes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                Removes(assembly);
        }

        /// <summary>
        ///     Removes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removes(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<RpcServiceAttribute>() != null)
                    Removes(type);
            }
        }

        /// <summary>
        ///     Removes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Removes(Type type)
        {
            if (!((type.IsClass || type.IsValueType) && !type.IsNested))
                return;
            foreach (var methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = methodInfo.GetCustomAttribute<OnErroredAttribute>();
                if (attribute != null && IsValid(methodInfo))
                    _events.Remove(methodInfo.MethodHandle.GetFunctionPointer());
            }
        }

        /// <summary>
        ///     Remove
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(delegate* managed<in NetworkPeer, in NetworkPacketFlag, in Span<byte>, in Exception, void> @event) => _events.Remove((nint)@event);

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
        public void Invoke(in NetworkPeer peer, in NetworkPacketFlag flags, in byte* buffer, in int length, in Exception e)
        {
            var span = MemoryMarshal.CreateSpan(ref *buffer, length);
            foreach (var value in _events)
            {
                var @event = (delegate* managed<in NetworkPeer, in NetworkPacketFlag, in Span<byte>, in Exception, void>)value;
                @event(peer, flags, span, e);
            }
        }

        /// <summary>
        ///     Check is valid
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValid(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType != typeof(void))
                return false;
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 4 && parameters[0].ParameterType == typeof(NetworkPeer).MakeByRefType() && parameters[0].IsIn && parameters[1].ParameterType == typeof(NetworkPacketFlag).MakeByRefType() && parameters[1].IsIn && parameters[2].ParameterType == typeof(Span<byte>).MakeByRefType() && parameters[2].IsIn && parameters[3].ParameterType == typeof(Exception).MakeByRefType() && parameters[3].IsIn;
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _events.Dispose();
    }
}