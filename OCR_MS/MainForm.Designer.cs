﻿namespace OCR_MS
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
            this.edResult = new System.Windows.Forms.TextBox();
            this.cbLanguage = new System.Windows.Forms.ComboBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.pbar = new System.Windows.Forms.ProgressBar();
            this.hint = new System.Windows.Forms.ToolTip(this.components);
            this.btnShowJSON = new System.Windows.Forms.Button();
            this.chkAutoClipboard = new System.Windows.Forms.CheckBox();
            this.btnOCR = new System.Windows.Forms.Button();
            this.btnSpeech = new System.Windows.Forms.Button();
            this.notify = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiShowWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowOCRResult = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiHistory = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSep0 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiTopMost = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity100 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity90 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity80 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity70 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity60 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity50 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity40 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity30 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity20 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpacity10 = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiWatchClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiSaveState = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.notifyMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // edResult
            // 
            this.edResult.AcceptsReturn = true;
            this.edResult.AcceptsTab = true;
            this.edResult.AccessibleRole = System.Windows.Forms.AccessibleRole.Client;
            resources.ApplyResources(this.edResult, "edResult");
            this.edResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.edResult.HideSelection = false;
            this.edResult.Name = "edResult";
            this.edResult.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edResult_KeyUp);
            // 
            // cbLanguage
            // 
            resources.ApplyResources(this.cbLanguage, "cbLanguage");
            this.cbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLanguage.FormattingEnabled = true;
            this.cbLanguage.Name = "cbLanguage";
            this.hint.SetToolTip(this.cbLanguage, resources.GetString("cbLanguage.ToolTip"));
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
            // hint
            // 
            this.hint.ShowAlways = true;
            this.hint.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            // 
            // btnShowJSON
            // 
            resources.ApplyResources(this.btnShowJSON, "btnShowJSON");
            this.btnShowJSON.FlatAppearance.BorderColor = System.Drawing.Color.DarkGray;
            this.btnShowJSON.Image = global::OCR_MS.Properties.Resources.JSON_32x;
            this.btnShowJSON.Name = "btnShowJSON";
            this.hint.SetToolTip(this.btnShowJSON, resources.GetString("btnShowJSON.ToolTip"));
            this.btnShowJSON.UseVisualStyleBackColor = true;
            this.btnShowJSON.Click += new System.EventHandler(this.btnShowJSON_Click);
            // 
            // chkAutoClipboard
            // 
            resources.ApplyResources(this.chkAutoClipboard, "chkAutoClipboard");
            this.chkAutoClipboard.AutoEllipsis = true;
            this.chkAutoClipboard.Checked = true;
            this.chkAutoClipboard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoClipboard.Image = global::OCR_MS.Properties.Resources.Watch_32x;
            this.chkAutoClipboard.Name = "chkAutoClipboard";
            this.hint.SetToolTip(this.chkAutoClipboard, resources.GetString("chkAutoClipboard.ToolTip"));
            this.chkAutoClipboard.UseVisualStyleBackColor = true;
            // 
            // btnOCR
            // 
            resources.ApplyResources(this.btnOCR, "btnOCR");
            this.btnOCR.Name = "btnOCR";
            this.hint.SetToolTip(this.btnOCR, resources.GetString("btnOCR.ToolTip"));
            this.btnOCR.UseVisualStyleBackColor = true;
            this.btnOCR.Click += new System.EventHandler(this.btnOCR_Click);
            // 
            // btnSpeech
            // 
            resources.ApplyResources(this.btnSpeech, "btnSpeech");
            this.btnSpeech.FlatAppearance.BorderColor = System.Drawing.Color.DarkGray;
            this.btnSpeech.Image = global::OCR_MS.Properties.Resources.Speech_32x;
            this.btnSpeech.Name = "btnSpeech";
            this.hint.SetToolTip(this.btnSpeech, resources.GetString("btnSpeech.ToolTip"));
            this.btnSpeech.UseVisualStyleBackColor = true;
            this.btnSpeech.Click += new System.EventHandler(this.btnSpeech_Click);
            // 
            // notify
            // 
            this.notify.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.notify.ContextMenuStrip = this.notifyMenu;
            resources.ApplyResources(this.notify, "notify");
            this.notify.Click += new System.EventHandler(this.notify_Click);
            this.notify.DoubleClick += new System.EventHandler(this.notify_DoubleClick);
            this.notify.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notify_MouseClick);
            // 
            // notifyMenu
            // 
            this.notifyMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiShowWindow,
            this.tsmiShowOCRResult,
            this.tsmiSep3,
            this.tsmiHistory,
            this.tsmiSep0,
            this.tsmiTopMost,
            this.tsmiOpacity,
            this.tsmiWatchClipboard,
            this.tsmiSep1,
            this.tsmiSaveState,
            this.tsmiSep2,
            this.tsmiExit});
            this.notifyMenu.Name = "notifyMenu";
            resources.ApplyResources(this.notifyMenu, "notifyMenu");
            // 
            // tsmiShowWindow
            // 
            this.tsmiShowWindow.Name = "tsmiShowWindow";
            resources.ApplyResources(this.tsmiShowWindow, "tsmiShowWindow");
            this.tsmiShowWindow.Click += new System.EventHandler(this.tsmiShowWindow_Click);
            // 
            // tsmiShowOCRResult
            // 
            this.tsmiShowOCRResult.Name = "tsmiShowOCRResult";
            resources.ApplyResources(this.tsmiShowOCRResult, "tsmiShowOCRResult");
            this.tsmiShowOCRResult.Click += new System.EventHandler(this.tsmiShowLastOCRResultJSON_Click);
            // 
            // tsmiSep3
            // 
            this.tsmiSep3.Name = "tsmiSep3";
            resources.ApplyResources(this.tsmiSep3, "tsmiSep3");
            // 
            // tsmiHistory
            // 
            this.tsmiHistory.Name = "tsmiHistory";
            resources.ApplyResources(this.tsmiHistory, "tsmiHistory");
            this.tsmiHistory.DropDownOpening += new System.EventHandler(this.tsmiHistory_DropDownOpening);
            // 
            // tsmiSep0
            // 
            this.tsmiSep0.Name = "tsmiSep0";
            resources.ApplyResources(this.tsmiSep0, "tsmiSep0");
            // 
            // tsmiTopMost
            // 
            this.tsmiTopMost.CheckOnClick = true;
            this.tsmiTopMost.Name = "tsmiTopMost";
            resources.ApplyResources(this.tsmiTopMost, "tsmiTopMost");
            this.tsmiTopMost.Click += new System.EventHandler(this.tsmiTopMost_Click);
            // 
            // tsmiOpacity
            // 
            this.tsmiOpacity.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpacity100,
            this.tsmiOpacity90,
            this.tsmiOpacity80,
            this.tsmiOpacity70,
            this.tsmiOpacity60,
            this.tsmiOpacity50,
            this.tsmiOpacity40,
            this.tsmiOpacity30,
            this.tsmiOpacity20,
            this.tsmiOpacity10});
            this.tsmiOpacity.Name = "tsmiOpacity";
            resources.ApplyResources(this.tsmiOpacity, "tsmiOpacity");
            // 
            // tsmiOpacity100
            // 
            this.tsmiOpacity100.Checked = true;
            this.tsmiOpacity100.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsmiOpacity100.Name = "tsmiOpacity100";
            resources.ApplyResources(this.tsmiOpacity100, "tsmiOpacity100");
            this.tsmiOpacity100.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity90
            // 
            this.tsmiOpacity90.Name = "tsmiOpacity90";
            resources.ApplyResources(this.tsmiOpacity90, "tsmiOpacity90");
            this.tsmiOpacity90.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity80
            // 
            this.tsmiOpacity80.Name = "tsmiOpacity80";
            resources.ApplyResources(this.tsmiOpacity80, "tsmiOpacity80");
            this.tsmiOpacity80.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity70
            // 
            this.tsmiOpacity70.Name = "tsmiOpacity70";
            resources.ApplyResources(this.tsmiOpacity70, "tsmiOpacity70");
            this.tsmiOpacity70.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity60
            // 
            this.tsmiOpacity60.Name = "tsmiOpacity60";
            resources.ApplyResources(this.tsmiOpacity60, "tsmiOpacity60");
            this.tsmiOpacity60.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity50
            // 
            this.tsmiOpacity50.Name = "tsmiOpacity50";
            resources.ApplyResources(this.tsmiOpacity50, "tsmiOpacity50");
            this.tsmiOpacity50.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity40
            // 
            this.tsmiOpacity40.Name = "tsmiOpacity40";
            resources.ApplyResources(this.tsmiOpacity40, "tsmiOpacity40");
            this.tsmiOpacity40.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity30
            // 
            this.tsmiOpacity30.Name = "tsmiOpacity30";
            resources.ApplyResources(this.tsmiOpacity30, "tsmiOpacity30");
            this.tsmiOpacity30.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity20
            // 
            this.tsmiOpacity20.Name = "tsmiOpacity20";
            resources.ApplyResources(this.tsmiOpacity20, "tsmiOpacity20");
            this.tsmiOpacity20.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiOpacity10
            // 
            this.tsmiOpacity10.Name = "tsmiOpacity10";
            resources.ApplyResources(this.tsmiOpacity10, "tsmiOpacity10");
            this.tsmiOpacity10.Click += new System.EventHandler(this.tsmiOpacityValue_Click);
            // 
            // tsmiWatchClipboard
            // 
            this.tsmiWatchClipboard.Checked = true;
            this.tsmiWatchClipboard.CheckOnClick = true;
            this.tsmiWatchClipboard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsmiWatchClipboard.Name = "tsmiWatchClipboard";
            resources.ApplyResources(this.tsmiWatchClipboard, "tsmiWatchClipboard");
            this.tsmiWatchClipboard.Click += new System.EventHandler(this.tsmiWatchClipboard_Click);
            // 
            // tsmiSep1
            // 
            this.tsmiSep1.Name = "tsmiSep1";
            resources.ApplyResources(this.tsmiSep1, "tsmiSep1");
            // 
            // tsmiSaveState
            // 
            this.tsmiSaveState.Name = "tsmiSaveState";
            resources.ApplyResources(this.tsmiSaveState, "tsmiSaveState");
            this.tsmiSaveState.Click += new System.EventHandler(this.tsmiSaveState_Click);
            // 
            // tsmiSep2
            // 
            this.tsmiSep2.Name = "tsmiSep2";
            resources.ApplyResources(this.tsmiSep2, "tsmiSep2");
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            resources.ApplyResources(this.tsmiExit, "tsmiExit");
            this.tsmiExit.Click += new System.EventHandler(this.tsmiExit_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.btnOCR;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ContextMenuStrip = this.notifyMenu;
            this.Controls.Add(this.btnSpeech);
            this.Controls.Add(this.btnShowJSON);
            this.Controls.Add(this.pbar);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.cbLanguage);
            this.Controls.Add(this.chkAutoClipboard);
            this.Controls.Add(this.edResult);
            this.Controls.Add(this.btnOCR);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
            this.notifyMenu.ResumeLayout(false);
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
        private System.Windows.Forms.ToolTip hint;
        private System.Windows.Forms.Button btnShowJSON;
        private System.Windows.Forms.NotifyIcon notify;
        private System.Windows.Forms.ContextMenuStrip notifyMenu;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowWindow;
        private System.Windows.Forms.ToolStripSeparator tsmiSep1;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiTopMost;
        private System.Windows.Forms.ToolStripSeparator tsmiSep2;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowOCRResult;
        private System.Windows.Forms.ToolStripMenuItem tsmiWatchClipboard;
        private System.Windows.Forms.ToolStripSeparator tsmiSep0;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveState;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity100;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity90;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity80;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity70;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity60;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity50;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity40;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity30;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity20;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpacity10;
        private System.Windows.Forms.Button btnSpeech;
        private System.Windows.Forms.ToolStripSeparator tsmiSep3;
        private System.Windows.Forms.ToolStripMenuItem tsmiHistory;
    }
}

