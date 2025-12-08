using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace EscalateSQLWriter
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.ComboBox cmbServices;
        private System.Windows.Forms.Label lblService;
        private System.Windows.Forms.Button btnExploit;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton rbSqlUser;
        private System.Windows.Forms.RadioButton rbWinUser;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPass;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnScan = new System.Windows.Forms.Button();
            this.cmbServices = new System.Windows.Forms.ComboBox();
            this.lblService = new System.Windows.Forms.Label();
            this.btnExploit = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPass = new System.Windows.Forms.Label();
            this.rbSqlUser = new System.Windows.Forms.RadioButton();
            this.rbWinUser = new System.Windows.Forms.RadioButton();
            this.grpMode.SuspendLayout();
            this.SuspendLayout();
            //
            // btnScan
            //
            this.btnScan.Location = new System.Drawing.Point(12, 12);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(120, 30);
            this.btnScan.TabIndex = 0;
            this.btnScan.Text = "1. Skanuj System";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
            //
            // lblService
            //
            this.lblService.AutoSize = true;
            this.lblService.Location = new System.Drawing.Point(12, 55);
            this.lblService.Name = "lblService";
            this.lblService.Size = new System.Drawing.Size(115, 13);
            this.lblService.TabIndex = 1;
            this.lblService.Text = "2. Wybierz usługę SQL:";
            //
            // cmbServices
            //
            this.cmbServices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbServices.Enabled = false;
            this.cmbServices.FormattingEnabled = true;
            this.cmbServices.Location = new System.Drawing.Point(133, 52);
            this.cmbServices.Name = "cmbServices";
            this.cmbServices.Size = new System.Drawing.Size(250, 21);
            this.cmbServices.TabIndex = 2;
            //
            // btnExploit
            //
            this.btnExploit.Enabled = false;
            this.btnExploit.Location = new System.Drawing.Point(12, 172);
            this.btnExploit.Name = "btnExploit";
            this.btnExploit.Size = new System.Drawing.Size(371, 35);
            this.btnExploit.TabIndex = 3;
            this.btnExploit.Text = "4. Uruchom Exploit";
            this.btnExploit.UseVisualStyleBackColor = true;
            this.btnExploit.Click += new System.EventHandler(this.btnExploit_Click);
            //
            // txtLog
            //
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.ForeColor = System.Drawing.Color.Lime;
            this.txtLog.Location = new System.Drawing.Point(12, 222);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(560, 227);
            this.txtLog.TabIndex = 4;
            this.txtLog.Text = "";
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(399, 21);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 5;
            //
            // grpMode
            //
            this.grpMode.Controls.Add(this.txtPassword);
            this.grpMode.Controls.Add(this.lblPass);
            this.grpMode.Controls.Add(this.rbSqlUser);
            this.grpMode.Controls.Add(this.rbWinUser);
            this.grpMode.Location = new System.Drawing.Point(12, 85);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(371, 81);
            this.grpMode.TabIndex = 6;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "3. Wybierz cel ataku";
            //
            // txtPassword
            //
            this.txtPassword.Enabled = false;
            this.txtPassword.Location = new System.Drawing.Point(180, 48);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(185, 20);
            this.txtPassword.TabIndex = 3;
            //
            // lblPass
            //
            this.lblPass.AutoSize = true;
            this.lblPass.Location = new System.Drawing.Point(137, 51);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(39, 13);
            this.lblPass.TabIndex = 2;
            this.lblPass.Text = "Hasło:";
            //
            // rbSqlUser
            //
            this.rbSqlUser.AutoSize = true;
            this.rbSqlUser.Location = new System.Drawing.Point(7, 49);
            this.rbSqlUser.Name = "rbSqlUser";
            this.rbSqlUser.Size = new System.Drawing.Size(124, 17);
            this.rbSqlUser.TabIndex = 1;
            this.rbSqlUser.Text = "Konto SQL (rootwk)";
            this.rbSqlUser.UseVisualStyleBackColor = true;
            this.rbSqlUser.CheckedChanged += new System.EventHandler(this.rbSqlUser_CheckedChanged);
            //
            // rbWinUser
            //
            this.rbWinUser.AutoSize = true;
            this.rbWinUser.Checked = true;
            this.rbWinUser.Location = new System.Drawing.Point(7, 20);
            this.rbWinUser.Name = "rbWinUser";
            this.rbWinUser.Size = new System.Drawing.Size(165, 17);
            this.rbWinUser.TabIndex = 0;
            this.rbWinUser.TabStop = true;
            this.rbWinUser.Text = "Obecny użytkownik Windows";
            this.rbWinUser.UseVisualStyleBackColor = true;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 461);
            this.Controls.Add(this.grpMode);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnExploit);
            this.Controls.Add(this.cmbServices);
            this.Controls.Add(this.lblService);
            this.Controls.Add(this.btnScan);
            this.Name = "MainForm";
            this.Text = "Escalate SQL Writer Tool";
            this.grpMode.ResumeLayout(false);
            this.grpMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
