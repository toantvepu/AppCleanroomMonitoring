namespace AppCleanRoom
{
    partial class frmTest
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
            this.txtLog = new System.Windows.Forms.TextBox();
            this.cboThanhGhi = new System.Windows.Forms.ComboBox();
            this.btnTest = new System.Windows.Forms.Button();
            this.txtGiaTriNhanDuoc = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnTestvoiMotdiachiIP = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtPortForTest = new System.Windows.Forms.TextBox();
            this.txtIPForTest = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtLog.Location = new System.Drawing.Point(5, 188);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(748, 343);
            this.txtLog.TabIndex = 31;
            // 
            // cboThanhGhi
            // 
            this.cboThanhGhi.FormattingEnabled = true;
            this.cboThanhGhi.Items.AddRange(new object[] {
            "110",
            "120",
            "130",
            "140",
            "150"});
            this.cboThanhGhi.Location = new System.Drawing.Point(94, 69);
            this.cboThanhGhi.Name = "cboThanhGhi";
            this.cboThanhGhi.Size = new System.Drawing.Size(96, 21);
            this.cboThanhGhi.TabIndex = 36;
            // 
            // btnTest
            // 
            this.btnTest.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnTest.Location = new System.Drawing.Point(196, 17);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(96, 102);
            this.btnTest.TabIndex = 34;
            this.btnTest.Text = "Lấy dữ liệu";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // txtGiaTriNhanDuoc
            // 
            this.txtGiaTriNhanDuoc.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.txtGiaTriNhanDuoc.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.txtGiaTriNhanDuoc.Location = new System.Drawing.Point(94, 96);
            this.txtGiaTriNhanDuoc.Name = "txtGiaTriNhanDuoc";
            this.txtGiaTriNhanDuoc.Size = new System.Drawing.Size(96, 23);
            this.txtGiaTriNhanDuoc.TabIndex = 35;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(20, 101);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(34, 13);
            this.label7.TabIndex = 32;
            this.label7.Text = "Giá trị";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 13);
            this.label4.TabIndex = 33;
            this.label4.Text = "Thanh ghi:";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(112, 43);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(78, 20);
            this.txtPort.TabIndex = 39;
            this.txtPort.Text = "502";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(20, 20);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(61, 13);
            this.label8.TabIndex = 37;
            this.label8.Text = "IP Address:";
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(82, 17);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(108, 20);
            this.txtIP.TabIndex = 40;
            this.txtIP.Text = "10.33.0.111";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(20, 50);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(29, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "Port:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtPort);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.txtIP);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.cboThanhGhi);
            this.groupBox1.Controls.Add(this.btnTest);
            this.groupBox1.Controls.Add(this.txtGiaTriNhanDuoc);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(382, 35);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(298, 126);
            this.groupBox1.TabIndex = 41;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Lấy dữ liệu từ thanh ghi";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnTestvoiMotdiachiIP);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txtPortForTest);
            this.groupBox2.Controls.Add(this.txtIPForTest);
            this.groupBox2.Location = new System.Drawing.Point(81, 35);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(291, 126);
            this.groupBox2.TabIndex = 43;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Lấy dữ liệu theo dải IP address";
            // 
            // btnTestvoiMotdiachiIP
            // 
            this.btnTestvoiMotdiachiIP.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.btnTestvoiMotdiachiIP.Location = new System.Drawing.Point(182, 17);
            this.btnTestvoiMotdiachiIP.Name = "btnTestvoiMotdiachiIP";
            this.btnTestvoiMotdiachiIP.Size = new System.Drawing.Size(96, 102);
            this.btnTestvoiMotdiachiIP.TabIndex = 41;
            this.btnTestvoiMotdiachiIP.Text = "Lấy dữ liệu";
            this.btnTestvoiMotdiachiIP.UseVisualStyleBackColor = true;
            this.btnTestvoiMotdiachiIP.Click += new System.EventHandler(this.btnTestvoiMotdiachiIP_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 37;
            this.label2.Text = "Port";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 37;
            this.label1.Text = "IP Address:";
            // 
            // txtPortForTest
            // 
            this.txtPortForTest.Location = new System.Drawing.Point(73, 50);
            this.txtPortForTest.Name = "txtPortForTest";
            this.txtPortForTest.Size = new System.Drawing.Size(103, 20);
            this.txtPortForTest.TabIndex = 40;
            this.txtPortForTest.Text = "502";
            // 
            // txtIPForTest
            // 
            this.txtIPForTest.Location = new System.Drawing.Point(73, 24);
            this.txtIPForTest.Name = "txtIPForTest";
            this.txtIPForTest.Size = new System.Drawing.Size(103, 20);
            this.txtIPForTest.TabIndex = 40;
            this.txtIPForTest.Text = "10.33.0.111";
            // 
            // frmTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(916, 543);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtLog);
            this.Name = "frmTest";
            this.Text = "TEST";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ComboBox cboThanhGhi;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.TextBox txtGiaTriNhanDuoc;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnTestvoiMotdiachiIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtIPForTest;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPortForTest;
    }
}