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
    public partial class WriteForm : Form
    {
        private readonly int MAX_CHANNEL = VfdMonitorForm.Instance.GetMaxChannel();
        public WriteForm()
        {
            InitializeComponent();
        }

        private void WriteForm_Load(object sender, EventArgs e)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled) {
                    int ch_word = ch + 1;
                    comboBoxChannel.Items.Add(ch_word.ToString());
                }
            }
            comboBoxChannel.SelectedIndex = 0;
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (!VfdMonitorForm.Instance.isRunning)
            {
                MessageBox.Show("未启动控制系统", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (textBoxAddress.TextLength != 4)
            {
                MessageBox.Show("请输入4位整数值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (comboBoxChannel.SelectedIndex == 0)
            {
                MessageBox.Show("请选择需要查询的通道号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int address = Convert.ToInt32(textBoxAddress.Text, 16);
            int channel = Convert.ToInt32(comboBoxChannel.SelectedItem) - 1;
            ReadChannel(channel, address);
        }

        private async void ReadChannel(int channel, int address)
        {
            ushort rst = await VfdMonitorForm.Instance.SendReadCmd(channel, address);
            textBoxValue.Text = rst.ToString();
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            if (!VfdMonitorForm.Instance.isRunning)
            {
                MessageBox.Show("未启动控制系统", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (textBoxAddress.TextLength != 4 || textBoxValue.TextLength != 4)
            {
                MessageBox.Show("请输入4位整数值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int address = Convert.ToInt32(textBoxAddress.Text, 16);
            int value = Convert.ToInt32(textBoxValue.Text, 16);
            if (comboBoxChannel.SelectedIndex > 0)
            {
                int ch = Convert.ToInt32(comboBoxChannel.SelectedItem) - 1;
                VfdMonitorForm.Instance.SendCmd(ch, address, value);
            }
            else
            {
                for (int ch = 0; ch < MAX_CHANNEL; ch++)
                {
                    VfdMonitorForm.Instance.SendCmd(ch, address, value);
                }
                this.Close();
            }
        }
    }
}
