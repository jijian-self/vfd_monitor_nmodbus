namespace vfd_monitor_nmodbus
{
    partial class ChannelSelectForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnSave = new System.Windows.Forms.Button();
            this.btnAllForbid = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAutoComplete = new System.Windows.Forms.Button();
            this.channelPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(700, 630);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 35);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "保存并应用";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnAllForbid
            // 
            this.btnAllForbid.Location = new System.Drawing.Point(581, 630);
            this.btnAllForbid.Name = "btnAllForbid";
            this.btnAllForbid.Size = new System.Drawing.Size(100, 35);
            this.btnAllForbid.TabIndex = 1;
            this.btnAllForbid.Text = "全部禁用";
            this.btnAllForbid.Click += new System.EventHandler(this.btnAllForbid_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(25, 630);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(400, 35);
            this.label1.TabIndex = 3;
            this.label1.Text = "本产品使用通道按照编号大小顺序排列";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnAutoComplete
            // 
            this.btnAutoComplete.Location = new System.Drawing.Point(452, 630);
            this.btnAutoComplete.Name = "btnAutoComplete";
            this.btnAutoComplete.Size = new System.Drawing.Size(100, 35);
            this.btnAutoComplete.TabIndex = 4;
            this.btnAutoComplete.Text = "启用全部通道";
            this.btnAutoComplete.Click += new System.EventHandler(this.btnAutoComplete_Click);
            //
            // channelPanel
            //
            this.channelPanel.AutoScroll = true;
            this.channelPanel.Location = new System.Drawing.Point(0, 0);
            this.channelPanel.Name = "channelPanel";
            this.channelPanel.Size = new System.Drawing.Size(834, 615);
            this.channelPanel.TabIndex = 5;
            // 
            // ChannelSelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 680);
            this.Controls.Add(this.channelPanel);
            this.Controls.Add(this.btnAutoComplete);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnAllForbid);
            this.Controls.Add(this.btnSave);
            this.MaximizeBox = false;
            this.Name = "ChannelSelectForm";
            this.Text = "通道选择";
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAllForbid;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAutoComplete;
        private System.Windows.Forms.Panel channelPanel;
    }
}
