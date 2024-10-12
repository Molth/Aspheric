using System;
using System.Net.Sockets;

namespace Erinn
{
    public unsafe struct RpcDictionary : IDisposable
    {
        public NativeDictionary<uint, nint> CommandToAddress;
        public NativeDictionary<nint, uint> AddressToCommand;

        public RpcDictionary()
        {
            CommandToAddress = new NativeDictionary<uint, nint>(RpcManager.RPC_METHOD_COUNT);
            AddressToCommand = new NativeDictionary<nint, uint>(RpcManager.RPC_METHOD_COUNT);
            RpcManager._Initialize(CommandToAddress, AddressToCommand);
        }

        public void Dispose()
        {
            CommandToAddress.Dispose();
            AddressToCommand.Dispose();
        }

        public bool TryGetCommand(nint address, out uint command) => AddressToCommand.TryGetValue(address, out command);

        public bool TryGetAddress(uint command, out delegate* <NetworkPeer, uint, NetworkStream, void> address)
        {
            if (CommandToAddress.TryGetValue(command, out var value))
            {
                address = (delegate*<NetworkPeer, uint, NetworkStream, void>)value;
                return true;
            }

            address = null;
            return false;
        }
    }
}