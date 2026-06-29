namespace vfd_monitor_nmodbus
{
    partial class SetFreqForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.Channel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Enabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Direction = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.ForwardFreq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BackwardFreq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnSaveFreq = new System.Windows.Forms.Button();
            this.btnWriteForward = new System.Windows.Forms.Button();
            this.btnWriteBackward = new System.Windows.Forms.Button();
            this.btnFreqFixAll = new System.Windows.Forms.Button();
            this.btnForwardFreqFix = new System.Windows.Forms.Button();
            this.btnBackwardFreqFix = new System.Windows.Forms.Button();
            this.textBoxFreq = new System.Windows.Forms.TextBox();
            this.labelFreq = new System.Windows.Forms.Label();
            this.labelFreqStep = new System.Windows.Forms.Label();
            this.textBoxFreqStep = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Channel,
            this.Enabled,
            this.Direction,
            this.ForwardFreq,
            this.BackwardFreq});
            this.dataGridView.Location = new System.Drawing.Point(12, 12);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowTemplate.Height = 23;
            this.dataGridView.Size = new System.Drawing.Size(504, 426);
            this.dataGridView.TabIndex = 0;
            this.dataGridView.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dataGridView_BeginEdit);
            // 
            // Channel
            // 
            this.Channel.HeaderText = "Channel";
            this.Channel.Name = "Channel";
            // 
            // Enabled
            // 
            this.Enabled.HeaderText = "Enabled";
            this.Enabled.Name = "Enabled";
            // 
            // Direction
            // 
            this.Direction.HeaderText = "Direction";
            this.Direction.Name = "Direction";
            // 
            // ForwardFreq
            // 
            this.ForwardFreq.HeaderText = "ForwardFreq";
            this.ForwardFreq.Name = "ForwardFreq";
            // 
            // BackwardFreq
            // 
            this.BackwardFreq.HeaderText = "BackwardFreq";
            this.BackwardFreq.Name = "BackwardFreq";
            // 
            // btnSaveFreq
            // 
            this.btnSaveFreq.Location = new System.Drawing.Point(530, 12);
            this.btnSaveFreq.Name = "btnSaveFreq";
            this.btnSaveFreq.Size = new System.Drawing.Size(100, 30);
            this.btnSaveFreq.TabIndex = 1;
            this.btnSaveFreq.Text = "保存设置";
            this.btnSaveFreq.UseVisualStyleBackColor = true;
            this.btnSaveFreq.Click += new System.EventHandler(this.btnSaveFreq_Click);
            // 
            // btnWriteForward
            // 
            this.btnWriteForward.Location = new System.Drawing.Point(531, 48);
            this.btnWriteForward.Name = "btnWriteForward";
            this.btnWriteForward.Size = new System.Drawing.Size(100, 30);
            this.btnWriteForward.TabIndex = 2;
            this.btnWriteForward.Text = "正转频率写入";
            this.btnWriteForward.UseVisualStyleBackColor = true;
            this.btnWriteForward.Click += new System.EventHandler(this.btnWriteForward_Click);
            // 
            // btnWriteBackward
            // 
            this.btnWriteBackward.Location = new System.Drawing.Point(530, 84);
            this.btnWriteBackward.Name = "btnWriteBackward";
            this.btnWriteBackward.Size = new System.Drawing.Size(100, 30);
            this.btnWriteBackward.TabIndex = 3;
            this.btnWriteBackward.Text = "反转频率写入";
            this.btnWriteBackward.UseVisualStyleBackColor = true;
            this.btnWriteBackward.Click += new System.EventHandler(this.btnWriteBackward_Click);
            // 
            // btnFreqFixAll
            // 
            this.btnFreqFixAll.Location = new System.Drawing.Point(531, 120);
            this.btnFreqFixAll.Name = "btnFreqFixAll";
            this.btnFreqFixAll.Size = new System.Drawing.Size(100, 30);
            this.btnFreqFixAll.TabIndex = 4;
            this.btnFreqFixAll.Text = "频率批量修改";
            this.btnFreqFixAll.UseVisualStyleBackColor = true;
            this.btnFreqFixAll.Click += new System.EventHandler(this.btnFreqFixAll_Click);
            // 
            // btnForwardFreqFix
            // 
            this.btnForwardFreqFix.Location = new System.Drawing.Point(531, 156);
            this.btnForwardFreqFix.Name = "btnForwardFreqFix";
            this.btnForwardFreqFix.Size = new System.Drawing.Size(100, 30);
            this.btnForwardFreqFix.TabIndex = 5;
            this.btnForwardFreqFix.Text = "正转频率等差";
            this.btnForwardFreqFix.UseVisualStyleBackColor = true;
            this.btnForwardFreqFix.Click += new System.EventHandler(this.btnForwardFreqFix_Click);
            // 
            // btnBackwardFreqFix
            // 
            this.btnBackwardFreqFix.Location = new System.Drawing.Point(531, 192);
            this.btnBackwardFreqFix.Name = "btnBackwardFreqFix";
            this.btnBackwardFreqFix.Size = new System.Drawing.Size(100, 30);
            this.btnBackwardFreqFix.TabIndex = 6;
            this.btnBackwardFreqFix.Text = "反转频率等差";
            this.btnBackwardFreqFix.UseVisualStyleBackColor = true;
            this.btnBackwardFreqFix.Click += new System.EventHandler(this.btnBackwardFreqFix_Click);
            // 
            // textBoxFreq
            // 
            this.textBoxFreq.Location = new System.Drawing.Point(531, 252);
            this.textBoxFreq.Name = "textBoxFreq";
            this.textBoxFreq.Size = new System.Drawing.Size(100, 21);
            this.textBoxFreq.TabIndex = 7;
            this.textBoxFreq.TextChanged += new System.EventHandler(this.textBoxFreq_TextChanged);
            this.textBoxFreq.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxFreq_KeyPress);
            // 
            // labelFreq
            // 
            this.labelFreq.AutoSize = true;
            this.labelFreq.Location = new System.Drawing.Point(531, 234);
            this.labelFreq.Name = "labelFreq";
            this.labelFreq.Size = new System.Drawing.Size(53, 12);
            this.labelFreq.TabIndex = 8;
            this.labelFreq.Text = "频率数值";
            // 
            // labelFreqStep
            // 
            this.labelFreqStep.AutoSize = true;
            this.labelFreqStep.Location = new System.Drawing.Point(531, 282);
            this.labelFreqStep.Name = "labelFreqStep";
            this.labelFreqStep.Size = new System.Drawing.Size(53, 12);
            this.labelFreqStep.TabIndex = 9;
            this.labelFreqStep.Text = "等差公差";
            // 
            // textBoxFreqStep
            // 
            this.textBoxFreqStep.Location = new System.Drawing.Point(531, 300);
            this.textBoxFreqStep.Name = "textBoxFreqStep";
            this.textBoxFreqStep.Size = new System.Drawing.Size(100, 21);
            this.textBoxFreqStep.TabIndex = 10;
            this.textBoxFreqStep.Text = "2";
            this.textBoxFreqStep.TextChanged += new System.EventHandler(this.textBoxFreqStep_TextChanged);
            this.textBoxFreqStep.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxFreqStep_KeyPress);
            // 
            // SetFreqForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 450);
            this.Controls.Add(this.textBoxFreqStep);
            this.Controls.Add(this.labelFreqStep);
            this.Controls.Add(this.labelFreq);
            this.Controls.Add(this.textBoxFreq);
            this.Controls.Add(this.btnBackwardFreqFix);
            this.Controls.Add(this.btnForwardFreqFix);
            this.Controls.Add(this.btnFreqFixAll);
            this.Controls.Add(this.btnWriteBackward);
            this.Controls.Add(this.btnWriteForward);
            this.Controls.Add(this.btnSaveFreq);
            this.Controls.Add(this.dataGridView);
            this.MaximizeBox = false;
            this.Name = "SetFreqForm";
            this.Text = "SetFreqForm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Channel;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ChannelEnabled;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Direction;
        private System.Windows.Forms.DataGridViewTextBoxColumn ForwardFreq;
        private System.Windows.Forms.DataGridViewTextBoxColumn BackwardFreq;
        private System.Windows.Forms.Button btnSaveFreq;
        private System.Windows.Forms.Button btnWriteForward;
        private System.Windows.Forms.Button btnWriteBackward;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Enabled;
        private System.Windows.Forms.Button btnFreqFixAll;
        private System.Windows.Forms.Button btnForwardFreqFix;
        private System.Windows.Forms.Button btnBackwardFreqFix;
        private System.Windows.Forms.TextBox textBoxFreq;
        private System.Windows.Forms.Label labelFreq;
        private System.Windows.Forms.Label labelFreqStep;
        private System.Windows.Forms.TextBox textBoxFreqStep;
    }
}
