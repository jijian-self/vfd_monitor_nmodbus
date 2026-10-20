using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace vfd_monitor_nmodbus
{
    [DataContract]
    public sealed class ConnectionSettings
    {
        [DataMember(Name = "portsPerIp", IsRequired = true)]
        public int PortsPerIp { get; set; }

        [DataMember(Name = "startPort", IsRequired = true)]
        public int StartPort { get; set; }

        [DataMember(Name = "ipAddresses", IsRequired = true)]
        public string[] IpAddresses { get; set; }

        public int MaxChannels => checked(PortsPerIp * IpAddresses.Length);

        public static string DefaultPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "connection.json");

        public static ConnectionSettings Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("连接配置文件不存在，请将 Config 目录与程序一起部署。", path);

            ConnectionSettings settings;
            using (FileStream stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(ConnectionSettings));
                try
                {
                    settings = (ConnectionSettings)serializer.ReadObject(stream);
                }
                catch (SerializationException ex)
                {
                    throw new InvalidDataException("连接配置 JSON 格式错误或缺少必填字段。", ex);
                }
            }

            if (settings == null)
                throw new InvalidDataException("连接配置不能为空。");
            settings.Validate();
            return settings;
        }

        public bool TryGetEndpoint(int channel, out string ip, out int port)
        {
            ip = null;
            port = 0;
            if (channel < 0 || channel >= MaxChannels) return false;

            ip = IpAddresses[channel / PortsPerIp];
            port = StartPort + channel % PortsPerIp;
            return true;
        }

        private void Validate()
        {
            if (PortsPerIp <= 0)
                throw new InvalidDataException("portsPerIp 必须大于 0。");
            if (StartPort < 1 || (long)StartPort + PortsPerIp - 1 > 65535)
                throw new InvalidDataException("startPort 与 portsPerIp 对应的端口必须在 1–65535 之间。");
            if (IpAddresses == null || IpAddresses.Length == 0)
                throw new InvalidDataException("ipAddresses 至少需要一个 IPv4 地址。");
            if ((long)PortsPerIp * IpAddresses.Length > int.MaxValue)
                throw new InvalidDataException("配置的通道总数过大。");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string text in IpAddresses)
            {
                if (!IPAddress.TryParse(text, out IPAddress address) ||
                    address.AddressFamily != AddressFamily.InterNetwork)
                    throw new InvalidDataException($"无效的 IPv4 地址：{text}");
                if (!seen.Add(address.ToString()))
                    throw new InvalidDataException($"IP 地址重复：{text}");
            }
        }
    }
}
