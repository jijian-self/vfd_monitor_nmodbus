using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vfd_monitor_nmodbus
{
    public partial class ChannelSelectForm : Form
    {
        private const int MAX_CHANNEL = 96;
        private const int MAX_USEFUL_CHANNEL = 64;
        private const int GROUP_SIZE = 32;

        public ChannelSelectForm()
        {
            InitializeComponent();
            CreateCheckboxes();
        }

        private void CreateCheckboxes()
        {
            int x = 20, y = 20;
            for (int g = 0; g < 3; g++)
            {
                Label groupLabel = new Label
                {
                    Text = $"第{g + 1}组 ({g * 32 + 1}~{(g + 1) * 32})",
                    Location = new System.Drawing.Point(x, y),
                    AutoSize = true
                };
                this.Controls.Add(groupLabel);
                y += 25;

                for (int i = 0; i < GROUP_SIZE; i++)
                {
                    int ch = g * GROUP_SIZE + i;
                    CheckBox cb = new CheckBox
                    {
                        Text = (ch + 1).ToString(),
                        Size = new Size(48, 20),
                        Location = new System.Drawing.Point(x, y),
                        Checked = VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled,
                        Tag = ch
                    };
                    this.Controls.Add(cb);

                    x += 50;
                    if ((i + 1) % 16 == 0)
                    {
                        x = 20;
                        y += 22;
                    }
                }

                y += 30;
                x = 20;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            int useful_channel = 0;
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox cb && cb.Checked) { useful_channel++; }
            }
            if (useful_channel > MAX_USEFUL_CHANNEL)
            {
                VfdMonitorForm.Instance.AppendLog("选择多于64个变频器，无法同时启动");
                return;
            }
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox cb)
                {
                    int ch = (int)cb.Tag;
                    VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled = cb.Checked;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
            if (VfdMonitorForm.Instance.isRunning)
            {
                VfdMonitorForm.Instance.RestartConnection();
                VfdMonitorForm.Instance.AppendLog("通道配置已保存，系统已自动重连");
            }
            else
            {
                VfdMonitorForm.Instance.AppendLog("通道配置已保存，系统启动后生效");
            }
            VfdMonitorForm.Instance.haveSaved = false;
        }

        private void btnAllForbid_Click(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox cb && cb.Checked) { cb.Checked = false; }
            }
        }

        private void btnUse64_Click(object sender, EventArgs e)
        {
            int useful_channel = 0;
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox cb) {
                    cb.Checked = true;
                    useful_channel++;
                }
                if (useful_channel == MAX_USEFUL_CHANNEL) break;
            }
        }

        private void btnAutoComplete_Click(object sender, EventArgs e)
        {
            int useful_channel = 0;
            int channel_iter = 1;
            foreach (Control c in this.Controls)
            {
                if (useful_channel >= MAX_USEFUL_CHANNEL) return;
                if (c is CheckBox cb)
                {
                    if (cb.Checked)
                    {
                        useful_channel++;
                    }
                    else
                    {
                        if (channel_iter > MAX_USEFUL_CHANNEL && useful_channel < MAX_USEFUL_CHANNEL)
                        {
                            cb.Checked = true;
                            useful_channel++;
                        }
                    }
                    channel_iter++;
                }
            }
        }
    }
}
