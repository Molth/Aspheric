using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erinn
{
    /// <summary>
    ///     Rpc methods
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct RpcMethods : IDisposable
    {
        /// <summary>
        ///     Command to address
        /// </summary>
        private readonly NativeDictionary<uint, nint> _commandToAddress;

        /// <summary>
        ///     Address to command
        /// </summary>
        private readonly NativeDictionary<nint, uint> _addressToCommand;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="rpcMethodCount">Rpc method count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RpcMethods(int rpcMethodCount)
        {
            _commandToAddress = new NativeDictionary<uint, nint>(rpcMethodCount);
            _addressToCommand = new NativeDictionary<nint, uint>(rpcMethodCount);
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _commandToAddress.Dispose();
            _addressToCommand.Dispose();
        }

        /// <summary>
        ///     Add command
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommand(uint command, delegate*<in NetworkPeer, in NetworkPacketFlag, in DataStream, void> address) => _commandToAddress[command] = (nint)address;

        /// <summary>
        ///     Add command
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommand(uint command, nint address) => _commandToAddress[command] = address;

        /// <summary>
        ///     Add command
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommand(uint command, RpcDelegate @delegate)
        {
            var methodInfo = @delegate.Method;
            if (!methodInfo.IsStatic || methodInfo.DeclaringType == null)
                throw new UnreachableException(nameof(@delegate));
            _commandToAddress[command] = methodInfo.MethodHandle.GetFunctionPointer();
        }

        /// <summary>
        ///     Add commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                AddCommands(assembly);
        }

        /// <summary>
        ///     Add commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommands(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(t => (t.IsClass || t.IsValueType) && !t.IsNested);
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<RpcServiceAttribute>() != null)
                    AddCommands(type);
            }
        }

        /// <summary>
        ///     Add commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommands(Type type)
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = methodInfo.GetCustomAttribute<RpcManualAttribute>();
                if (attribute != null && IsValidRpcDelegate(methodInfo))
                    _commandToAddress[attribute.Command] = methodInfo.MethodHandle.GetFunctionPointer();
            }
        }

        /// <summary>
        ///     Remove command
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCommand(uint command) => _commandToAddress.Remove(command);

        /// <summary>
        ///     Remove commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                RemoveCommands(assembly);
        }

        /// <summary>
        ///     Remove commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCommands(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(t => (t.IsClass || t.IsValueType) && !t.IsNested);
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<RpcServiceAttribute>() != null)
                    RemoveCommands(type);
            }
        }

        /// <summary>
        ///     Remove commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCommands(Type type)
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = methodInfo.GetCustomAttribute<RpcManualAttribute>();
                if (attribute != null && IsValidRpcDelegate(methodInfo))
                    _commandToAddress.Remove(attribute.Command);
            }
        }

        /// <summary>
        ///     Clear commands
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearCommands() => _commandToAddress.Clear();

        /// <summary>
        ///     Try get command
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCommand(nint address, out uint command) => _addressToCommand.TryGetValue(address, out command);

        /// <summary>
        ///     Check is valid rpc delegate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidRpcDelegate(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType != typeof(void))
                return false;
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 3 && parameters[0].ParameterType == typeof(NetworkPeer).MakeByRefType() && parameters[0].IsIn && parameters[1].ParameterType == typeof(NetworkPacketFlag).MakeByRefType() && parameters[1].IsIn && parameters[2].ParameterType == typeof(DataStream).MakeByRefType() && parameters[2].IsIn;
        }

        /// <summary>
        ///     Add address
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAddress(nint address, uint command) => _addressToCommand[address] = command;

        /// <summary>
        ///     Remove address
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAddress(nint address) => _addressToCommand.Remove(address);

        /// <summary>
        ///     Clear addresses
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAddresses() => _addressToCommand.Clear();

        /// <summary>
        ///     Try get address
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetAddress(uint command, out delegate*<in NetworkPeer, in NetworkPacketFlag, in DataStream, void> address)
        {
            if (_commandToAddress.TryGetValue(command, out var value))
            {
                address = (delegate*<in NetworkPeer, in NetworkPacketFlag, in DataStream, void>)value;
                return true;
            }

            address = null;
            return false;
        }
    }
}