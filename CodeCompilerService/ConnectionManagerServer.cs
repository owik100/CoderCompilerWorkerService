using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CodeCompilerService
{
    public class ConnectionManagerServer
    {
        private readonly ILogger<Worker> _logger;
        Socket _server;

        List<Socket> _clientsList = new List<Socket>();
        byte[] _buffer = new byte[512];
        IPAddress _ipAdress = IPAddress.Parse("127.0.0.1");
        int _port = 3055;

        public ConnectionManagerServer(ILogger<Worker> logger, int port)
        {
            _logger = logger;
            _port = port;
            RunServer();
        }

        private void RunServer()
        {
            try
            {
                _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _server.Bind(new IPEndPoint(_ipAdress, _port));
                _server.Listen(3);
                _server.BeginAccept(_server.ReceiveBufferSize, new AsyncCallback(AcceptConnection), null);
                _logger.LogInformation($"Server listening on port {_port}...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private void AcceptConnection(IAsyncResult asyncCallback)
        {
            try
            {
                byte[] buffer = new byte[_server.ReceiveBufferSize];
                Socket socket = _server.EndAccept(out buffer, asyncCallback);

                _clientsList.Add(socket);
                _logger.LogInformation("Client from manager connected!");

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReciveCallback), socket);
                _server.BeginAccept(_server.ReceiveBufferSize, AcceptConnection, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private void ReciveCallback(IAsyncResult asyncResult)
        {
            Socket socket = asyncResult.AsyncState as Socket;
            try
            {
                DisconnectClient(socket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        private void DisconnectClient(Socket socket)
        {
            try
            {
                _clientsList.Remove(socket);

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();

                _logger.LogInformation("Client from manager disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public void SendToClient(string message)
        {
            try
            {
                byte[] dataBuf = Encoding.UTF8.GetBytes(message);
                foreach (var item in _clientsList)
                {
                    item.BeginSend(dataBuf, 0, dataBuf.Length, SocketFlags.None, null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
