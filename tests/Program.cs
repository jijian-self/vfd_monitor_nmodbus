using System;
using System.IO;
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

        private static void ConnectionSettingsUseConfiguredIpAndPortGroups()
        {
            Assert(File.Exists(ConnectionSettings.DefaultPath),
                "connection JSON is copied beside the application");
            ConnectionSettings.Load(ConnectionSettings.DefaultPath);

            string path = Path.Combine(Path.GetTempPath(), "vfd-connection-test-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path,
                    "{\"portsPerIp\":4,\"startPort\":5001,\"ipAddresses\":[\"192.168.0.201\",\"192.168.0.202\"]}");
                ConnectionSettings settings = ConnectionSettings.Load(path);
                Assert(settings.MaxChannels == 8, "channel count is derived from JSON");

                string ip;
                int port;
                Assert(settings.TryGetEndpoint(0, out ip, out port) && ip == "192.168.0.201" && port == 5001,
                    "first channel uses the first IP and start port");
                Assert(settings.TryGetEndpoint(3, out ip, out port) && ip == "192.168.0.201" && port == 5004,
                    "last channel in a group uses the last port");
                Assert(settings.TryGetEndpoint(4, out ip, out port) && ip == "192.168.0.202" && port == 5001,
                    "next group starts at the next IP and start port");
                Assert(settings.TryGetEndpoint(7, out ip, out port) && ip == "192.168.0.202" && port == 5004,
                    "last configured channel is mapped");
                Assert(!settings.TryGetEndpoint(8, out ip, out port),
                    "channels outside the configured range are rejected");

                File.WriteAllText(path,
                    "{\"portsPerIp\":0,\"startPort\":5001,\"ipAddresses\":[\"192.168.0.201\"]}");
                bool rejectedInvalidSettings = false;
                try { ConnectionSettings.Load(path); }
                catch (InvalidDataException) { rejectedInvalidSettings = true; }
                Assert(rejectedInvalidSettings, "invalid connection settings are rejected before startup");

                File.WriteAllText(path, "{");
                bool rejectedMalformedJson = false;
                try { ConnectionSettings.Load(path); }
                catch (InvalidDataException) { rejectedMalformedJson = true; }
                Assert(rejectedMalformedJson, "malformed JSON is rejected before startup");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
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
                ConnectionSettingsUseConfiguredIpAndPortGroups();
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
