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
        private readonly int MAX_CHANNEL = VfdMonitorForm.Instance.GetMaxChannel();
        private readonly int GROUP_SIZE = VfdMonitorForm.Instance.GetGroupSize();

        public ChannelSelectForm()
        {
            InitializeComponent();
            Text = $"通道选择（{MAX_CHANNEL}路 {(MAX_CHANNEL + GROUP_SIZE - 1) / GROUP_SIZE}组）";
            btnAutoComplete.Text = $"启用全部{MAX_CHANNEL}路";
            CreateCheckboxes();
        }

        private void CreateCheckboxes()
        {
            int x = 20, y = 20;
            channelPanel.SuspendLayout();
            for (int g = 0; g < (MAX_CHANNEL + GROUP_SIZE - 1) / GROUP_SIZE; g++)
            {
                CheckBox groupCheckBox = new CheckBox
                {
                    Text = $"第{g + 1}组 ({g * GROUP_SIZE + 1}~{Math.Min((g + 1) * GROUP_SIZE, MAX_CHANNEL)}) 全选",
                    Location = new System.Drawing.Point(x, y),
                    AutoSize = true
                };
                channelPanel.Controls.Add(groupCheckBox);
                List<CheckBox> groupChannels = new List<CheckBox>();
                y += 25;

                for (int i = 0; i < GROUP_SIZE && g * GROUP_SIZE + i < MAX_CHANNEL; i++)
                {
                    int ch = g * GROUP_SIZE + i;
                    CheckBox cb = new CheckBox
                    {
                        Text = (ch + 1).ToString(),
                        Size = new Size(45, 20),
                        Location = new System.Drawing.Point(x, y),
                        Checked = VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled,
                        Tag = ch
                    };
                    channelPanel.Controls.Add(cb);
                    groupChannels.Add(cb);
                    cb.CheckedChanged += (sender, args) =>
                        groupCheckBox.Checked = groupChannels.All(channel => channel.Checked);

                    x += 49;
                    if ((i + 1) % 16 == 0)
                    {
                        x = 20;
                        y += 22;
                    }
                }
                groupCheckBox.Checked = groupChannels.All(channel => channel.Checked);
                groupCheckBox.Click += (sender, args) =>
                {
                    bool selected = groupCheckBox.Checked;
                    foreach (CheckBox channel in groupChannels) channel.Checked = selected;
                };

                y += 30;
                x = 20;
            }
            channelPanel.ResumeLayout();
        }

        private IEnumerable<CheckBox> ChannelCheckBoxes()
        {
            return channelPanel.Controls.OfType<CheckBox>().Where(cb => cb.Tag is int);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in ChannelCheckBoxes())
            {
                int ch = (int)cb.Tag;
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled = cb.Checked;
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
            foreach (CheckBox cb in ChannelCheckBoxes())
            {
                cb.Checked = false;
            }
        }

        private void btnAutoComplete_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in ChannelCheckBoxes())
            {
                cb.Checked = true;
            }
        }
    }
}
