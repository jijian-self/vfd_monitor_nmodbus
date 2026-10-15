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
    public partial class SetFreqForm : Form
    {
        private readonly int MAX_CHANNEL = VfdMonitorForm.Instance.GetMaxChannel();
        private const int ENABLED_COLUMN_INDEX = 1;
        private const int DIRECTION_COLUMN_INDEX = 2;
        private const int FORWARD_FREQ_COLUMN_INDEX = 3;
        private const int BACKWARD_FREQ_COLUMN_INDEX = 4;
        private bool _isUpdatingFreqText;
        private bool _isUpdatingFreqStepText;

        public SetFreqForm()
        {
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            IODb Db = VfdMonitorForm.Instance.database;
            for (int iter = 0; iter < MAX_CHANNEL; iter++)
            {
                AddRow(iter, Db.ChannelFreqDict[iter]);
                if (!Db.ChannelFreqDict[iter].Enabled)
                {
                    dataGridView.Rows[iter].Cells[0].Style.BackColor = Color.Gray;
                    dataGridView.Rows[iter].Cells[1].Style.BackColor = Color.Gray;
                    dataGridView.Rows[iter].Cells[2].Style.BackColor = Color.Gray;
                    dataGridView.Rows[iter].Cells[3].Style.BackColor = Color.Gray;
                    dataGridView.Rows[iter].Cells[4].Style.BackColor = Color.Gray;
                }
            }
        }

        public void AddRow(int ch, ChannelFreq data)
        {
            if (data == null) return;
            object[] row =
            {
                ch,
                data.Enabled,
                data.Opposite,
                data.ForwardFreq,
                data.BackwardFreq
            };
            dataGridView.Rows.Add(row);
        }

        private void dataGridView_BeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            int row_iter = dgv.CurrentCell.RowIndex;
            if (!VfdMonitorForm.Instance.database.ChannelFreqDict[row_iter].Enabled)
            {
                e.Cancel = true;
            }
        }

        private void textBoxFreq_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBoxFreq_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFreqText) return;

            string originalText = textBoxFreq.Text;
            string filteredText = new string(originalText.Where(char.IsDigit).ToArray());
            if (originalText == filteredText) return;

            int selectionStart = textBoxFreq.SelectionStart;
            _isUpdatingFreqText = true;
            textBoxFreq.Text = filteredText;
            textBoxFreq.SelectionStart = Math.Min(selectionStart, textBoxFreq.Text.Length);
            _isUpdatingFreqText = false;
        }

        private void textBoxFreqStep_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;

            TextBox textBox = (TextBox)sender;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == '-' && textBox.SelectionStart == 0 && !textBox.Text.Contains("-")) return;

            e.Handled = true;
        }

        private void textBoxFreqStep_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFreqStepText) return;

            string originalText = textBoxFreqStep.Text;
            string filteredText = FilterSignedIntegerText(originalText);
            if (originalText == filteredText) return;

            int selectionStart = textBoxFreqStep.SelectionStart;
            _isUpdatingFreqStepText = true;
            textBoxFreqStep.Text = filteredText;
            textBoxFreqStep.SelectionStart = Math.Min(selectionStart, textBoxFreqStep.Text.Length);
            _isUpdatingFreqStepText = false;
        }

        private string FilterSignedIntegerText(string text)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
                else if (c == '-' && sb.Length == 0)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private bool TryGetInputFreq(out int freq)
        {
            dataGridView.EndEdit();
            if (int.TryParse(textBoxFreq.Text, out freq))
            {
                return true;
            }

            MessageBox.Show("请输入整数频率值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            textBoxFreq.Focus();
            return false;
        }

        private bool TryGetInputFreqStep(out int step)
        {
            dataGridView.EndEdit();
            if (int.TryParse(textBoxFreqStep.Text, out step))
            {
                return true;
            }

            MessageBox.Show("请输入整数公差值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            textBoxFreqStep.Focus();
            return false;
        }

        private bool GetCellBool(int rowIndex, int columnIndex)
        {
            object value = dataGridView.Rows[rowIndex].Cells[columnIndex].Value;
            return value != null && Convert.ToBoolean(value);
        }

        private int GetForwardFreqColumnIndex(bool direction)
        {
            return direction ? BACKWARD_FREQ_COLUMN_INDEX : FORWARD_FREQ_COLUMN_INDEX;
        }

        private int GetBackwardFreqColumnIndex(bool direction)
        {
            return direction ? FORWARD_FREQ_COLUMN_INDEX : BACKWARD_FREQ_COLUMN_INDEX;
        }

        private void btnFreqFixAll_Click(object sender, EventArgs e)
        {
            if (!TryGetInputFreq(out int freq)) return;

            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                if (!GetCellBool(ch, ENABLED_COLUMN_INDEX)) continue;

                dataGridView.Rows[ch].Cells[FORWARD_FREQ_COLUMN_INDEX].Value = freq;
                dataGridView.Rows[ch].Cells[BACKWARD_FREQ_COLUMN_INDEX].Value = freq;
            }
        }

        private void btnForwardFreqFix_Click(object sender, EventArgs e)
        {
            if (!TryGetInputFreq(out int startFreq)) return;
            if (!TryGetInputFreqStep(out int freqStep)) return;

            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                bool direction = GetCellBool(ch, DIRECTION_COLUMN_INDEX);
                int freqColumnIndex = GetForwardFreqColumnIndex(direction);
                dataGridView.Rows[ch].Cells[freqColumnIndex].Value = startFreq + ch * freqStep;
            }
        }

        private void btnBackwardFreqFix_Click(object sender, EventArgs e)
        {
            if (!TryGetInputFreq(out int startFreq)) return;
            if (!TryGetInputFreqStep(out int freqStep)) return;

            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                bool direction = GetCellBool(ch, DIRECTION_COLUMN_INDEX);
                int freqColumnIndex = GetBackwardFreqColumnIndex(direction);
                dataGridView.Rows[ch].Cells[freqColumnIndex].Value = startFreq + ch * freqStep;
            }
        }

        private void btnSaveFreq_Click(object sender, EventArgs e)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled = Convert.ToBoolean(dataGridView.Rows[ch].Cells[1].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Opposite = Convert.ToBoolean(dataGridView.Rows[ch].Cells[2].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].ForwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[3].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].BackwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[4].Value);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnWriteForward_Click(object sender, EventArgs e)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled = Convert.ToBoolean(dataGridView.Rows[ch].Cells[1].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Opposite = Convert.ToBoolean(dataGridView.Rows[ch].Cells[2].Value);
                if (!VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled) continue;
                bool opposite = VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Opposite;

                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].ForwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[3].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].BackwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[4].Value);

                double forward_freq = opposite ? VfdMonitorForm.Instance.database.ChannelFreqDict[ch].BackwardFreq : VfdMonitorForm.Instance.database.ChannelFreqDict[ch].ForwardFreq;
                int freq_val = VfdMonitorForm.Instance.DataUnitCalc(forward_freq, VfdMonitorForm.Instance.REG_ADDR_WRITE);
                VfdMonitorForm.Instance.SendCmd(ch, VfdMonitorForm.Instance.REG_ADDR_WRITE, freq_val);
            }
        }

        private void btnWriteBackward_Click(object sender, EventArgs e)
        {
            for (int ch = 0; ch < MAX_CHANNEL; ch++)
            {
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled = Convert.ToBoolean(dataGridView.Rows[ch].Cells[1].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Opposite = Convert.ToBoolean(dataGridView.Rows[ch].Cells[2].Value);
                if (!VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Enabled) continue;
                bool opposite = VfdMonitorForm.Instance.database.ChannelFreqDict[ch].Opposite;

                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].ForwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[3].Value);
                VfdMonitorForm.Instance.database.ChannelFreqDict[ch].BackwardFreq = Convert.ToDouble(dataGridView.Rows[ch].Cells[4].Value);

                double forward_freq = opposite ? VfdMonitorForm.Instance.database.ChannelFreqDict[ch].ForwardFreq : VfdMonitorForm.Instance.database.ChannelFreqDict[ch].BackwardFreq;
                int freq_val = VfdMonitorForm.Instance.DataUnitCalc(forward_freq, VfdMonitorForm.Instance.REG_ADDR_WRITE);
                VfdMonitorForm.Instance.SendCmd(ch, VfdMonitorForm.Instance.REG_ADDR_WRITE, freq_val);
            }
        }
    }
}
