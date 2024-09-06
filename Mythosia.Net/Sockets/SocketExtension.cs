using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mythosia.Net.Sockets
{
    public static class SocketExtension
    {
        /// <summary>
        /// Converts a given <see cref="Socket"/> into a <see cref="NetworkStream"/>.
        /// </summary>
        /// <param name="socket">The socket to be converted into a NetworkStream.</param>
        /// <param name="ownsSocket">If set to <c>true</c>, 
        /// the resulting NetworkStream will take ownership of the socket 
        /// and close it when the NetworkStream is closed. Default is <c>false</c>.</param>
        /// <returns>A NetworkStream associated with the given socket.</returns>
        public static NetworkStream ToNetworkStream(this Socket socket, bool ownsSocket = false)
            => new NetworkStream(socket, ownsSocket);


        /*
        public static Task<int> ReceiveAsync(this Socket socket, byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            socket.ReceiveAsync()

            return Task.Factory.FromAsync(
                               (callback, state) => socket.BeginReceive(buffer, offset, size, socketFlags, callback, state),
                                              socket.EndReceive,
                                                             null);
        }
        */
    }
}
