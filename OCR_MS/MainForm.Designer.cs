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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnOCR = new System.Windows.Forms.Button();
            this.edResult = new System.Windows.Forms.TextBox();
            this.chkAutoClipboard = new System.Windows.Forms.CheckBox();
            this.cbLanguage = new System.Windows.Forms.ComboBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.pbar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // btnOCR
            // 
            resources.ApplyResources(this.btnOCR, "btnOCR");
            this.btnOCR.Name = "btnOCR";
            this.btnOCR.UseVisualStyleBackColor = true;
            this.btnOCR.Click += new System.EventHandler(this.btnOCR_Click);
            // 
            // edResult
            // 
            resources.ApplyResources(this.edResult, "edResult");
            this.edResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.edResult.HideSelection = false;
            this.edResult.Name = "edResult";
            this.edResult.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edResult_KeyUp);
            // 
            // chkAutoClipboard
            // 
            resources.ApplyResources(this.chkAutoClipboard, "chkAutoClipboard");
            this.chkAutoClipboard.AutoEllipsis = true;
            this.chkAutoClipboard.Checked = true;
            this.chkAutoClipboard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoClipboard.Name = "chkAutoClipboard";
            this.chkAutoClipboard.UseVisualStyleBackColor = true;
            // 
            // cbLanguage
            // 
            resources.ApplyResources(this.cbLanguage, "cbLanguage");
            this.cbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLanguage.FormattingEnabled = true;
            this.cbLanguage.Name = "cbLanguage";
            // 
            // lblLanguage
            // 
            resources.ApplyResources(this.lblLanguage, "lblLanguage");
            this.lblLanguage.AutoEllipsis = true;
            this.lblLanguage.Name = "lblLanguage";
            // 
            // timer
            // 
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // pbar
            // 
            resources.ApplyResources(this.pbar, "pbar");
            this.pbar.Name = "pbar";
            // 
            // MainForm
            // 
            this.AcceptButton = this.btnOCR;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pbar);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.cbLanguage);
            this.Controls.Add(this.chkAutoClipboard);
            this.Controls.Add(this.edResult);
            this.Controls.Add(this.btnOCR);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
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
        private System.Windows.Forms.ProgressBar pbar;
    }
}

