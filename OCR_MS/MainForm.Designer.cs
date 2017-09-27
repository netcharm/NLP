namespace OCR_MS
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnOCR = new System.Windows.Forms.Button();
            this.edResult = new System.Windows.Forms.TextBox();
            this.chkAutoClipboard = new System.Windows.Forms.CheckBox();
            this.cbLanguage = new System.Windows.Forms.ComboBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // btnOCR
            // 
            this.btnOCR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOCR.Location = new System.Drawing.Point(189, 90);
            this.btnOCR.Name = "btnOCR";
            this.btnOCR.Size = new System.Drawing.Size(75, 36);
            this.btnOCR.TabIndex = 0;
            this.btnOCR.Text = "OCR";
            this.btnOCR.UseVisualStyleBackColor = true;
            this.btnOCR.Click += new System.EventHandler(this.btnOCR_Click);
            // 
            // edResult
            // 
            this.edResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.edResult.Location = new System.Drawing.Point(7, 12);
            this.edResult.Multiline = true;
            this.edResult.Name = "edResult";
            this.edResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.edResult.Size = new System.Drawing.Size(258, 70);
            this.edResult.TabIndex = 1;
            this.edResult.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edResult_KeyUp);
            // 
            // chkAutoClipboard
            // 
            this.chkAutoClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAutoClipboard.AutoEllipsis = true;
            this.chkAutoClipboard.CheckAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.chkAutoClipboard.Checked = true;
            this.chkAutoClipboard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoClipboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkAutoClipboard.Location = new System.Drawing.Point(105, 90);
            this.chkAutoClipboard.Name = "chkAutoClipboard";
            this.chkAutoClipboard.Size = new System.Drawing.Size(78, 36);
            this.chkAutoClipboard.TabIndex = 2;
            this.chkAutoClipboard.Text = "Clipboard Monitor";
            this.chkAutoClipboard.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.chkAutoClipboard.UseVisualStyleBackColor = true;
            // 
            // cbLanguage
            // 
            this.cbLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLanguage.FormattingEnabled = true;
            this.cbLanguage.Location = new System.Drawing.Point(7, 106);
            this.cbLanguage.Name = "cbLanguage";
            this.cbLanguage.Size = new System.Drawing.Size(92, 20);
            this.cbLanguage.TabIndex = 3;
            // 
            // lblLanguage
            // 
            this.lblLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLanguage.AutoEllipsis = true;
            this.lblLanguage.Location = new System.Drawing.Point(7, 85);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(84, 20);
            this.lblLanguage.TabIndex = 4;
            this.lblLanguage.Text = "Language:";
            this.lblLanguage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(276, 141);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.cbLanguage);
            this.Controls.Add(this.chkAutoClipboard);
            this.Controls.Add(this.edResult);
            this.Controls.Add(this.btnOCR);
            this.Name = "MainForm";
            this.Text = "Microsoft Azure Cognitive Service OCR";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOCR;
        private System.Windows.Forms.TextBox edResult;
        private System.Windows.Forms.CheckBox chkAutoClipboard;
        private System.Windows.Forms.ComboBox cbLanguage;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.Timer timer;
    }
}

