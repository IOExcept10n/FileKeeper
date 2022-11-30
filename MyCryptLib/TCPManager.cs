using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyCryptLib
{
    /// <summary>
    /// Represents a class to handle TCP connections.
    /// </summary>
    public class TCPManager
    {
        /// <summary>
        /// Invokes when file sending state is updated.
        /// </summary>
        public EventHandler<FileSendData> FileStatusUpdated { get; set; }
        /// <summary>
        /// Invokes when server gets new connection.
        /// </summary>
        public EventHandler<TcpConnectionArgs> NewClientConnection { get; set; }
        /// <summary>
        /// Runs a server which will try to send a file until cancelling of task.
        /// </summary>
        /// <param name="port">A port to send a file.</param>
        /// <param name="fileName">Path to a file to send.</param>
        /// <param name="token">Token to disconnect a server.</param>
        /// <returns>An asyncronous task with working server.</returns>
        public async Task RunSendingServer(int port, string fileName, CancellationToken token)
        {
            // Creates and starts the server.
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();
            // Tries to send a file to all incoming connections.
            while (!token.IsCancellationRequested)
            {
                CancellationTokenSource src = new CancellationTokenSource();
                var client = await server.AcceptTcpClientAsync(src.Token);
                var args = new TcpConnectionArgs(client);
                NewClientConnection?.DynamicInvoke(this, args);
                if (args.Cancel)
                    src.Cancel();
                else _ = Task.Run(() => Serve(client, fileName), CancellationToken.None);
            }
        }
        /// <summary>
        /// Starts a client which downloads file from the server.
        /// </summary>
        /// <param name="serverAddress">Address of server which </param>
        /// <param name="serverPort"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task RunReceivingClient(IPAddress serverAddress, int serverPort, string fileName)
        {
            // Tries to connect to a server.
            using var client = new TcpClient(serverAddress.ToString(), serverPort);
            var stream = client.GetStream();
            // Creates a buffer to read a stream.
            byte[] buf = new byte[65536];
            // Gets a length of an incoming file.
            await ReadBytes(sizeof(long), stream, buf);
            long remainingLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buf, 0));
            long totalLength = remainingLength;
            // Downloads incoming file.
            using var file = File.Create(fileName);
            while (remainingLength > 0)
            {
                int lengthToRead = (int)Math.Min(remainingLength, buf.Length);
                await ReadBytes(lengthToRead, stream, buf);
                await file.WriteAsync(buf.AsMemory(0, lengthToRead));
                FileStatusUpdated?.Invoke(this, new FileSendData(totalLength, (double)(totalLength - remainingLength)/totalLength, SendResult.Progress));
                remainingLength -= lengthToRead;
            }
            FileStatusUpdated?.Invoke(this, FileSendData.GetSuccessData(totalLength));
        }

        async Task ReadBytes(int howmuch, Stream stream, byte[] buf)
        {
            // Reads a chunk of a file.
            int readPos = 0;
            while (readPos < howmuch)
            {
                var actuallyRead = await stream.ReadAsync(buf.AsMemory(readPos, howmuch - readPos));
                // If can't read data which should be read, throw an exception.
                if (actuallyRead == 0)
                {
                    FileStatusUpdated?.Invoke(this, new FileSendData(howmuch, (double)readPos/howmuch, SendResult.Failure));
                    throw new EndOfStreamException();
                }
                readPos += actuallyRead;
            }
        }

        async Task Serve(TcpClient client, string filename)
        {
            // Creates a connection and writes a file to a client.
            using var _ = client;
            var stream = client.GetStream();
            using var file = File.OpenRead(filename);
            var length = file.Length;
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
            await stream.WriteAsync(lengthBytes);
            await file.CopyToAsync(stream);
            FileStatusUpdated?.Invoke(this, FileSendData.GetSuccessData(length));
        }

        async Task ServeEncrypted(TcpClient client, string filename, string key)
        {
            using var _ = client;
            var stream = client.GetStream();
            using var file = File.OpenRead(filename);
            var length = file.Length;
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));
            await stream.WriteAsync(lengthBytes);
            await file.EncryptStreamAsync(stream, key);
            FileStatusUpdated?.Invoke(this, FileSendData.GetSuccessData(length));
        }

        /// <summary>
        /// Gets self IP addresses.
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetSelfIPAddresses()
        {
            string Host = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(Host);
            return addresses;
        }

        /// <summary>
        /// Creates new encrypted connection specified for clients with <see cref="RunEncryptedReceivingClient(IPAddress, int, string, string)"/> run method.
        /// </summary>
        /// <param name="port">A port of the server.</param>
        /// <param name="fileName">Name of the file to send.</param>
        /// <param name="aesKey">Key to encrypt the stream.</param>
        /// <param name="cancellationToken">A token to cancel an operation.</param>
        /// <returns></returns>
        public async Task RunEncryptedSendingServer(int port, string fileName, string aesKey, CancellationToken cancellationToken)
        {
            // Creates and starts the server.
            var server = new TcpListener(IPAddress.Any, port);
            server.Start();
            // Tries to send a file to all incoming connections.
            while (!cancellationToken.IsCancellationRequested)
            {
                CancellationTokenSource src = new CancellationTokenSource();
                var client = await server.AcceptTcpClientAsync(src.Token);
                var args = new TcpConnectionArgs(client);
                NewClientConnection?.DynamicInvoke(this, args);
                if (args.Cancel)
                    src.Cancel();
                else _ = Task.Run(() => ServeEncrypted(client, fileName, aesKey), CancellationToken.None);
            }
        }
        /// <summary>
        /// Creates an encrypted connection to receive an decrypt files.
        /// </summary>
        /// <param name="serverAddress">Address of a server to get a file.</param>
        /// <param name="serverPort">Port of connection.</param>
        /// <param name="fileName">Path to save a file.</param>
        /// <param name="aesKey">Key to decrypt a file.</param>
        /// <returns></returns>
        public async Task RunEncryptedReceivingClient(IPAddress serverAddress, int serverPort, string fileName, string aesKey)
        {
            // Tries to connect to a server.
            using var client = new TcpClient();
            client.Connect(serverAddress.ToString(), serverPort);
            var stream = client.GetStream();
            // Creates a buffer to read a stream.
            byte[] buf = new byte[65536];
            // Gets a length of an incoming file.
            await ReadBytes(sizeof(long), stream, buf);
            long remainingLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buf, 0));
            long totalLength = remainingLength;
            // Downloads incoming file.
            using var file = File.Create(fileName);
            await stream.DecryptStreamAsync(file, aesKey);
            FileStatusUpdated?.Invoke(this, FileSendData.GetSuccessData(totalLength));
        }

        /// <summary>
        /// Tests a lag between client and server to check if server available.
        /// </summary>
        /// <param name="host">Hostname of a server.</param>
        /// <param name="port">Port of connection.</param>
        /// <param name="timeOut">Maximum timeout to ping.</param>
        /// <returns>Length of a lag or -1 if server is unavailable.</returns>
        public static async Task<long> Ping(string host, int port, int timeOut)
        {
            long elapsed = -1;
            Stopwatch watch = new Stopwatch();

            using (TcpClient tcp = new TcpClient())
            {
                try
                {
                    using CancellationTokenSource cts = new CancellationTokenSource();
                    StartConnection(host, port, tcp, watch, cts);
                    await Task.Delay(timeOut, cts.Token);
                }
                finally
                {
                    if (tcp.Connected)
                    {
                        tcp.GetStream().Close();
                        elapsed = watch.ElapsedMilliseconds;
                    }
                    tcp.Close();
                }
            }

            return elapsed;
        }

        private static async void StartConnection(string host, int port, TcpClient tcp, Stopwatch watch, CancellationTokenSource cts)
        {
            try
            {
                watch.Start();
                await tcp.ConnectAsync(host, port);
                watch.Stop();
                cts.Cancel();
            }
            catch { }
        }
    }
    /// <summary>
    /// Represents data of file sending operation.
    /// </summary>
    public struct FileSendData
    {
        /// <summary>
        /// Total size of a sended file. 
        /// </summary>
        public long FileSize { get; }
        /// <summary>
        /// Progress of a file sending.
        /// </summary>
        public double Progress { get; }
        /// <summary>
        /// Result of an operation.
        /// </summary>
        public SendResult Result { get; }
        /// <summary>
        /// Creates new <see cref="FileSendData"/>.
        /// </summary>
        /// <param name="fileSize">Total file size.</param>
        /// <param name="progress">Progress of an operation.</param>
        /// <param name="result">Result.</param>
        public FileSendData(long fileSize, double progress, SendResult result)
        {
            FileSize = fileSize;
            Progress = progress;
            Result = result;
        }
        /// <summary>
        /// Gets data for success file sending operation.
        /// </summary>
        /// <param name="fileSize">Size of a file.</param>
        /// <returns></returns>
        public static FileSendData GetSuccessData(long fileSize) => new FileSendData(fileSize, 1, SendResult.Success);
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Result}: {Progress * 100}% of {FileSize}B";
        }
    }
    /// <summary>
    /// File sending result. 
    /// </summary>
    public enum SendResult
    {
        /// <summary>
        /// Unknown result.
        /// </summary>
        Unknown,
        /// <summary>
        /// File has been successfully uploaded/downloaded.
        /// </summary>
        Success,
        /// <summary>
        /// Operation is in progress.
        /// </summary>
        Progress,
        /// <summary>
        /// There is an exception while file sending.
        /// </summary>
        Failure,
    }

    /// <summary>
    /// Represent arguments given with new TCP connection event. 
    /// </summary>
    public class TcpConnectionArgs : CancelEventArgs
    {
        /// <summary>
        /// Creates new <see cref="TcpConnectionArgs"/> instance.
        /// </summary>
        /// <param name="client">TCP client.</param>
        public TcpConnectionArgs(TcpClient client)
        {
            Client=client;
        }

        /// <summary>
        /// Connected client.
        /// </summary>
        public TcpClient Client { get; }
    }
}
