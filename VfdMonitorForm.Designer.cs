using System;
using System.Windows.Forms;

namespace vfd_monitor_nmodbus
{
    partial class VfdMonitorForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region 窗体设计器生成代码
        private void InitializeComponent()
        {
            this.btnChannelSelect = new System.Windows.Forms.Button();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.lampPanel = new System.Windows.Forms.Panel();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtData = new System.Windows.Forms.TextBox();
            this.chartA = new ScottPlot.WinForms.FormsPlot();
            this.chartB = new ScottPlot.WinForms.FormsPlot();
            this.chartC = new ScottPlot.WinForms.FormsPlot();
            this.btnRefreshFreq = new System.Windows.Forms.Button();
            this.textBoxRefreshFreq = new System.Windows.Forms.TextBox();
            this.btnReadFault = new System.Windows.Forms.Button();
            this.btnStopOperation = new System.Windows.Forms.Button();
            this.btnBackward = new System.Windows.Forms.Button();
            this.btnForward = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.btnSaveFreq = new System.Windows.Forms.Button();
            this.btnFreqOption = new System.Windows.Forms.Button();
            this.btnInitReg = new System.Windows.Forms.Button();
            this.btnWriteForm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnChannelSelect
            // 
            this.btnChannelSelect.Location = new System.Drawing.Point(280, 830);
            this.btnChannelSelect.Name = "btnChannelSelect";
            this.btnChannelSelect.Size = new System.Drawing.Size(100, 30);
            this.btnChannelSelect.TabIndex = 0;
            this.btnChannelSelect.Text = "映射设置";
            this.btnChannelSelect.Click += new System.EventHandler(this.btnChannelSelect_Click);
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(60, 830);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(100, 30);
            this.btnStartStop.TabIndex = 7;
            this.btnStartStop.Text = "启动控制";
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(60, 870);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(100, 30);
            this.btnImport.TabIndex = 6;
            this.btnImport.Text = "导入配置";
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // lampPanel
            // 
            this.lampPanel.BackColor = System.Drawing.Color.Red;
            this.lampPanel.Location = new System.Drawing.Point(20, 830);
            this.lampPanel.Name = "lampPanel";
            this.lampPanel.Size = new System.Drawing.Size(30, 30);
            this.lampPanel.TabIndex = 5;
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(1160, 10);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(275, 810);
            this.txtLog.TabIndex = 0;
            // 
            // txtData
            // 
            this.txtData.Location = new System.Drawing.Point(20, 620);
            this.txtData.Multiline = true;
            this.txtData.Name = "txtData";
            this.txtData.ReadOnly = true;
            this.txtData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtData.Size = new System.Drawing.Size(1130, 200);
            this.txtData.TabIndex = 1;
            // 
            // chartA
            // 
            this.chartA.Location = new System.Drawing.Point(150, 10);
            this.chartA.Name = "chartA";
            this.chartA.Size = new System.Drawing.Size(1000, 190);
            this.chartA.TabIndex = 2;
            // 
            // chartB
            // 
            this.chartB.Location = new System.Drawing.Point(150, 210);
            this.chartB.Name = "chartB";
            this.chartB.Size = new System.Drawing.Size(1000, 190);
            this.chartB.TabIndex = 3;
            // 
            // chartC
            // 
            this.chartC.Location = new System.Drawing.Point(150, 410);
            this.chartC.Name = "chartC";
            this.chartC.Size = new System.Drawing.Size(1000, 200);
            this.chartC.TabIndex = 4;
            // 
            // btnRefreshFreq
            // 
            this.btnRefreshFreq.Location = new System.Drawing.Point(60, 950);
            this.btnRefreshFreq.Name = "btnRefreshFreq";
            this.btnRefreshFreq.Size = new System.Drawing.Size(100, 30);
            this.btnRefreshFreq.TabIndex = 8;
            this.btnRefreshFreq.Text = "刷新间隔";
            this.btnRefreshFreq.UseVisualStyleBackColor = true;
            this.btnRefreshFreq.Click += new System.EventHandler(this.btnRefreshFreq_Click);
            // 
            // textBoxRefreshFreq
            // 
            this.textBoxRefreshFreq.Location = new System.Drawing.Point(170, 953);
            this.textBoxRefreshFreq.Name = "textBoxRefreshFreq";
            this.textBoxRefreshFreq.Size = new System.Drawing.Size(100, 21);
            this.textBoxRefreshFreq.TabIndex = 9;
            // 
            // btnReadFault
            // 
            this.btnReadFault.Location = new System.Drawing.Point(170, 830);
            this.btnReadFault.Name = "btnReadFault";
            this.btnReadFault.Size = new System.Drawing.Size(100, 30);
            this.btnReadFault.TabIndex = 11;
            this.btnReadFault.Text = "清除故障";
            this.btnReadFault.UseVisualStyleBackColor = true;
            this.btnReadFault.Click += new System.EventHandler(this.btnReadFault_Click);
            // 
            // btnStopOperation
            // 
            this.btnStopOperation.Location = new System.Drawing.Point(280, 910);
            this.btnStopOperation.Name = "btnStopOperation";
            this.btnStopOperation.Size = new System.Drawing.Size(100, 30);
            this.btnStopOperation.TabIndex = 12;
            this.btnStopOperation.Text = "停止运行";
            this.btnStopOperation.UseVisualStyleBackColor = true;
            this.btnStopOperation.Click += new System.EventHandler(this.btnStopOperation_Click);
            // 
            // btnBackward
            // 
            this.btnBackward.Enabled = false;
            this.btnBackward.Location = new System.Drawing.Point(170, 910);
            this.btnBackward.Name = "btnBackward";
            this.btnBackward.Size = new System.Drawing.Size(100, 30);
            this.btnBackward.TabIndex = 13;
            this.btnBackward.Text = "反转运行";
            this.btnBackward.UseVisualStyleBackColor = true;
            this.btnBackward.Click += new System.EventHandler(this.btnBackward_Click);
            // 
            // btnForward
            // 
            this.btnForward.Enabled = false;
            this.btnForward.Location = new System.Drawing.Point(60, 910);
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(100, 30);
            this.btnForward.TabIndex = 14;
            this.btnForward.Text = "正转运行";
            this.btnForward.UseVisualStyleBackColor = true;
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "当前频率",
            "设定频率",
            "母线电压",
            "输出电流",
            "输出电压",
            "输出功率",
            "实际转速",
            "温度"});
            this.comboBox1.Location = new System.Drawing.Point(20, 10);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(120, 20);
            this.comboBox1.TabIndex = 16;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "当前频率",
            "设定频率",
            "母线电压",
            "输出电流",
            "输出电压",
            "输出功率",
            "实际转速",
            "温度"});
            this.comboBox2.Location = new System.Drawing.Point(20, 210);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(120, 20);
            this.comboBox2.TabIndex = 17;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // comboBox3
            // 
            this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Items.AddRange(new object[] {
            "当前频率",
            "设定频率",
            "母线电压",
            "输出电流",
            "输出电压",
            "输出功率",
            "实际转速",
            "温度"});
            this.comboBox3.Location = new System.Drawing.Point(20, 410);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(120, 20);
            this.comboBox3.TabIndex = 18;
            this.comboBox3.SelectedIndexChanged += new System.EventHandler(this.comboBox3_SelectedIndexChanged);
            // 
            // btnSaveFreq
            // 
            this.btnSaveFreq.Location = new System.Drawing.Point(280, 870);
            this.btnSaveFreq.Name = "btnSaveFreq";
            this.btnSaveFreq.Size = new System.Drawing.Size(100, 30);
            this.btnSaveFreq.TabIndex = 19;
            this.btnSaveFreq.Text = "保存配置";
            this.btnSaveFreq.Click += new System.EventHandler(this.btnSaveFreq_Click);
            // 
            // btnFreqOption
            // 
            this.btnFreqOption.Location = new System.Drawing.Point(170, 870);
            this.btnFreqOption.Name = "btnFreqOption";
            this.btnFreqOption.Size = new System.Drawing.Size(100, 30);
            this.btnFreqOption.TabIndex = 20;
            this.btnFreqOption.Text = "选择配置";
            this.btnFreqOption.Click += new System.EventHandler(this.btnFreqOption_Click);
            // 
            // btnInitReg
            // 
            this.btnInitReg.Location = new System.Drawing.Point(280, 950);
            this.btnInitReg.Name = "btnInitReg";
            this.btnInitReg.Size = new System.Drawing.Size(100, 30);
            this.btnInitReg.TabIndex = 21;
            this.btnInitReg.Text = "变频器初始化";
            this.btnInitReg.UseVisualStyleBackColor = true;
            this.btnInitReg.Click += new System.EventHandler(this.btnInitReg_Click);
            //
            // btnWriteForm
            //
            this.btnWriteForm.Location = new System.Drawing.Point(390, 830);
            this.btnWriteForm.Name = "btnWriteForm";
            this.btnWriteForm.Size = new System.Drawing.Size(100, 30);
            this.btnWriteForm.TabIndex = 22;
            this.btnWriteForm.Text = "批量写入";
            this.btnWriteForm.UseVisualStyleBackColor = true;
            this.btnWriteForm.Click += new System.EventHandler(this.btnWriteForm_Click);
            // 
            // VfdMonitorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1448, 1011);
            this.Controls.Add(this.btnWriteForm);
            this.Controls.Add(this.btnInitReg);
            this.Controls.Add(this.btnFreqOption);
            this.Controls.Add(this.btnSaveFreq);
            this.Controls.Add(this.comboBox3);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.btnForward);
            this.Controls.Add(this.btnBackward);
            this.Controls.Add(this.btnStopOperation);
            this.Controls.Add(this.btnReadFault);
            this.Controls.Add(this.textBoxRefreshFreq);
            this.Controls.Add(this.btnRefreshFreq);
            this.Controls.Add(this.btnChannelSelect);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.chartC);
            this.Controls.Add(this.chartB);
            this.Controls.Add(this.chartA);
            this.Controls.Add(this.lampPanel);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.btnStartStop);
            this.MaximizeBox = true;
            this.Name = "VfdMonitorForm";
            this.Text = "64路VFD变频器监控系统";
            this.Load += new System.EventHandler(this.VfdMonitorForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private Button btnStartStop;
        private Button btnImport;
        private Panel lampPanel;
        private TextBox txtLog;
        private TextBox txtData;
        private Button btnChannelSelect;
        private ScottPlot.WinForms.FormsPlot chartA;
        private ScottPlot.WinForms.FormsPlot chartB;
        private ScottPlot.WinForms.FormsPlot chartC;
        private Button btnRefreshFreq;
        private TextBox textBoxRefreshFreq;
        private Button btnReadFault;
        private Button btnStopOperation;
        private Button btnBackward;
        private Button btnForward;
        private ComboBox comboBox1;
        private ComboBox comboBox2;
        private ComboBox comboBox3;
        private Button btnSaveFreq;
        private Button btnFreqOption;
        private Button btnInitReg;
        private Button btnWriteForm;
    }
}

