using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vfd_monitor_nmodbus
{
    public class ChannelFreq
    {
        public bool Enabled { get; set; }
        public bool Opposite { get; set; }
        public double ForwardFreq { get; set; }
        public double BackwardFreq { get; set; }
    }

    public class IODb
    {
        private readonly int MAX_CHANNEL = 96;
        private readonly double MAX_FREQ = 599.0;
        private readonly double MIN_FREQ = 0.0;
        public Dictionary<int, ChannelFreq> ChannelFreqDict;
        public IODb()
        {
            ChannelFreqDict = new Dictionary<int, ChannelFreq>();
            for (int i = 0; i < MAX_CHANNEL; i++) 
            {
                ChannelFreqDict[i] = new ChannelFreq
                {
                    Enabled = i < 64,
                    Opposite = false,
                    ForwardFreq = 0,
                    BackwardFreq = 0
                };
            }
        }

        public void LoadChannelFreq(string path)
        {
            if (path == null) { return; }
            string line = null;
            Encoding encoding = Encoding.Default;
            StreamReader reader = new StreamReader(path, encoding);
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 按逗号分隔
                string[] parts = line.Split(',');

                // 跳过标题行
                if (parts[0] == "channel") continue;

                if (parts.Length >= 5
                    && int.TryParse(parts[0], out int ch)
                    && bool.TryParse(parts[1], out bool able)
                    && bool.TryParse(parts[2], out bool oppo)
                    && double.TryParse(parts[3], out double forfreq)
                    && double.TryParse(parts[4], out double backfreq))
                {
                    if (forfreq < MIN_FREQ || forfreq > MAX_FREQ || backfreq < MIN_FREQ || backfreq > MAX_FREQ)
                    {
                        VfdMonitorForm.Instance.AppendLog($"变频器支持频率范围{MIN_FREQ}-{MAX_FREQ}，请检查频率输入值");
                        continue;
                    }
                    if (ChannelFreqDict[ch] == null)
                    {
                        ChannelFreqDict[ch] = new ChannelFreq
                        {
                            Enabled = able,
                            Opposite = oppo,
                            ForwardFreq = forfreq,
                            BackwardFreq = backfreq
                        };
                    }
                    else
                    {
                        ChannelFreqDict[ch].Enabled = able;
                        ChannelFreqDict[ch].Opposite = oppo;
                        ChannelFreqDict[ch].ForwardFreq = forfreq;
                        ChannelFreqDict[ch].BackwardFreq = backfreq;
                    }
                }
            }
        }

        public void WriteChannelFreq(string path)
        {
            if (path == null) { return; }
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("channel,enable,opposite,forward,backward");
                foreach (KeyValuePair<int, ChannelFreq> iter in ChannelFreqDict)
                {
                    if (iter.Value == null) continue;
                    string enable_str = iter.Value.Enabled ? "true" : "false";
                    string oppo_str = iter.Value.Opposite ? "true" : "false";
                    sb.AppendLine($"{iter.Key},{enable_str},{oppo_str},{iter.Value.ForwardFreq},{iter.Value.BackwardFreq}");
                }

                // 写入文件，如果已有则覆盖
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                VfdMonitorForm.Instance.AppendLog("导出 CSV 成功！");
            }
            catch (Exception ex)
            {
                VfdMonitorForm.Instance.AppendLog("导出失败：" + ex.Message);
            }
        }
    }
}
