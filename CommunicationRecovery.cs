using Modbus.Device;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace vfd_monitor_nmodbus
{
    internal static class TcpConnectionState
    {
        public static bool IsDisconnected(TcpClient client)
        {
            if (client == null || !client.Connected) return true;

            try
            {
                Socket socket = client.Client;
                return socket == null ||
                    (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (SocketException)
            {
                return true;
            }
        }
    }

    internal sealed class RegisterReadResult
    {
        private RegisterReadResult(
            bool success,
            ushort value,
            string errorMessage)
        {
            Success = success;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; private set; }
        public ushort Value { get; private set; }
        public string ErrorMessage { get; private set; }

        public static RegisterReadResult Succeeded(ushort value)
        {
            return new RegisterReadResult(true, value, null);
        }

        public static RegisterReadResult Failed(string errorMessage)
        {
            return new RegisterReadResult(false, 0, errorMessage);
        }

        public bool TryGetValue(out ushort value)
        {
            value = Value;
            return Success;
        }
    }

    internal sealed class ModbusChannelConnection : IDisposable
    {
        public ModbusChannelConnection(TcpClient client, ModbusSerialMaster master)
        {
            Client = client;
            Master = master;
        }

        public TcpClient Client { get; private set; }
        public ModbusSerialMaster Master { get; private set; }

        public void Dispose()
        {
            try
            {
                if (Master != null) Master.Dispose();
            }
            catch
            {
            }

            try
            {
                if (Client != null) Client.Dispose();
            }
            catch
            {
            }

            Master = null;
            Client = null;
        }
    }

    /// <summary>
    /// Creates the TCP/RTU transport only. This type deliberately has no API for
    /// writing run, reverse, or stop commands.
    /// </summary>
    internal sealed class ModbusConnectionFactory
    {
        private const int TRANSPORT_READ_TIMEOUT = 1500;
        private const int TRANSPORT_WRITE_TIMEOUT = 1500;
        private const int TRANSPORT_RETRIES = 2;
        private const int TRANSPORT_RETRY_DELAY = 100;

        public async Task<ModbusChannelConnection> ConnectAsync(
            string ip,
            int port,
            int connectTimeoutMilliseconds)
        {
            TcpClient client = new TcpClient
            {
                ReceiveTimeout = connectTimeoutMilliseconds,
                SendTimeout = connectTimeoutMilliseconds
            };

            try
            {
                Task connectTask = client.ConnectAsync(ip, port);
                Task completedTask = await Task.WhenAny(
                    connectTask,
                    Task.Delay(connectTimeoutMilliseconds));
                if (!ReferenceEquals(completedTask, connectTask))
                {
                    client.Dispose();
                    ObserveLateConnectFailure(connectTask);
                    throw new TimeoutException(
                        string.Format("TCP 连接超时（{0}ms）", connectTimeoutMilliseconds));
                }

                await connectTask;
                if (!client.Connected)
                    throw new SocketException((int)SocketError.NotConnected);

                ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(client);
                master.Transport.ReadTimeout = TRANSPORT_READ_TIMEOUT;
                master.Transport.WriteTimeout = TRANSPORT_WRITE_TIMEOUT;
                master.Transport.Retries = TRANSPORT_RETRIES;
                master.Transport.WaitToRetryMilliseconds = TRANSPORT_RETRY_DELAY;
                ((Modbus.IO.ModbusSerialTransport)master.Transport).CheckFrame = true;
                return new ModbusChannelConnection(client, master);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        private static void ObserveLateConnectFailure(Task connectTask)
        {
            connectTask.ContinueWith(
                task =>
                {
                    Exception ignored = task.Exception;
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
