using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace vfd_monitor_nmodbus.Tests
{
    internal static class Program
    {
        private static int _failures;

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                Console.WriteLine("PASS: " + message);
                return;
            }

            _failures++;
            Console.WriteLine("FAIL: " + message);
        }

        private static void FailedReadDoesNotProduceMeasurement()
        {
            RegisterReadResult result = RegisterReadResult.Failed("timeout");
            ushort measurement;
            bool produced = result.TryGetValue(out measurement);

            Assert(!produced, "failed read does not produce a replacement measurement");
        }

        private static void SuccessfulZeroIsAValidMeasurement()
        {
            RegisterReadResult result = RegisterReadResult.Succeeded(0);
            ushort measurement;
            bool produced = result.TryGetValue(out measurement);

            Assert(produced && measurement == 0,
                "successful zero remains distinguishable from a failed read");
        }

        private static async Task OpeningConnectionSendsNoControlPayloadAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> accepted = listener.AcceptTcpClientAsync();

            ModbusConnectionFactory factory = new ModbusConnectionFactory();
            ModbusChannelConnection connection = await factory.ConnectAsync(
                IPAddress.Loopback.ToString(),
                port,
                1000);
            TcpClient server = await accepted;

            await Task.Delay(100);
            Assert(!server.GetStream().DataAvailable,
                "opening or reopening a channel sends no Modbus control payload");

            connection.Dispose();
            server.Dispose();
            listener.Stop();
        }

        private static async Task ClosedPeerIsDetectedAsDisconnectedAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> accepted = listener.AcceptTcpClientAsync();

            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            TcpClient server = await accepted;
            Assert(!TcpConnectionState.IsDisconnected(client),
                "an open TCP connection is not treated as disconnected");

            server.Dispose();
            bool disconnected = false;
            for (int attempt = 0; attempt < 20 && !disconnected; attempt++)
            {
                await Task.Delay(25);
                disconnected = TcpConnectionState.IsDisconnected(client);
            }

            Assert(disconnected,
                "a peer-closed TCP connection is detected for automatic reconnect");
            client.Dispose();
            listener.Stop();
        }

        private static int Main()
        {
            try
            {
                FailedReadDoesNotProduceMeasurement();
                SuccessfulZeroIsAValidMeasurement();
                OpeningConnectionSendsNoControlPayloadAsync().GetAwaiter().GetResult();
                ClosedPeerIsDetectedAsDisconnectedAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine("FAIL: unexpected exception: " + ex);
            }

            Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : _failures + " TEST(S) FAILED");
            return _failures == 0 ? 0 : 1;
        }
    }
}
