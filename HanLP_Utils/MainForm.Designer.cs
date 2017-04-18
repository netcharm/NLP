namespace HanLP_Utils
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
            this.edSrc = new System.Windows.Forms.TextBox();
            this.btnSegment = new System.Windows.Forms.Button();
            this.edDst = new System.Windows.Forms.TextBox();
            this.btnTokenizer = new System.Windows.Forms.Button();
            this.btnSummary = new System.Windows.Forms.Button();
            this.btnKeyword = new System.Windows.Forms.Button();
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlTools = new System.Windows.Forms.Panel();
            this.cmActions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmiActionFilterSub = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiActionFilterLrc = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiActionFilterMlTag = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiSep0 = new System.Windows.Forms.ToolStripSeparator();
            this.cmiOptionCutCRF = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiOptionCutStandard = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlOption = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.chkTermNature = new System.Windows.Forms.CheckBox();
            this.btnWordFreq = new System.Windows.Forms.Button();
            this.btnSrc2PyTM = new System.Windows.Forms.Button();
            this.btnSrc2PyT = new System.Windows.Forms.Button();
            this.btnSrc2Py = new System.Windows.Forms.Button();
            this.btnTC2SC = new System.Windows.Forms.Button();
            this.btnSC2TC = new System.Windows.Forms.Button();
            this.btnPhrase = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.cmiOptionCutNShort = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiOptionCutIndex = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiOptionCutNLP = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiOptionCutHighSpeed = new System.Windows.Forms.ToolStripMenuItem();
            this.layoutMain.SuspendLayout();
            this.pnlTools.SuspendLayout();
            this.cmActions.SuspendLayout();
            this.pnlOption.SuspendLayout();
            this.SuspendLayout();
            // 
            // edSrc
            // 
            resources.ApplyResources(this.edSrc, "edSrc");
            this.edSrc.Name = "edSrc";
            this.edSrc.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edSrc_KeyUp);
            // 
            // btnSegment
            // 
            resources.ApplyResources(this.btnSegment, "btnSegment");
            this.btnSegment.Name = "btnSegment";
            this.toolTip.SetToolTip(this.btnSegment, resources.GetString("btnSegment.ToolTip"));
            this.btnSegment.UseVisualStyleBackColor = true;
            this.btnSegment.Click += new System.EventHandler(this.btnSegment_Click);
            // 
            // edDst
            // 
            resources.ApplyResources(this.edDst, "edDst");
            this.edDst.Name = "edDst";
            this.edDst.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edDst_KeyUp);
            // 
            // btnTokenizer
            // 
            resources.ApplyResources(this.btnTokenizer, "btnTokenizer");
            this.btnTokenizer.Name = "btnTokenizer";
            this.toolTip.SetToolTip(this.btnTokenizer, resources.GetString("btnTokenizer.ToolTip"));
            this.btnTokenizer.UseVisualStyleBackColor = true;
            this.btnTokenizer.Click += new System.EventHandler(this.btnTokenizer_Click);
            // 
            // btnSummary
            // 
            resources.ApplyResources(this.btnSummary, "btnSummary");
            this.btnSummary.Name = "btnSummary";
            this.toolTip.SetToolTip(this.btnSummary, resources.GetString("btnSummary.ToolTip"));
            this.btnSummary.UseVisualStyleBackColor = true;
            this.btnSummary.Click += new System.EventHandler(this.btnSummary_Click);
            // 
            // btnKeyword
            // 
            resources.ApplyResources(this.btnKeyword, "btnKeyword");
            this.btnKeyword.Name = "btnKeyword";
            this.toolTip.SetToolTip(this.btnKeyword, resources.GetString("btnKeyword.ToolTip"));
            this.btnKeyword.UseVisualStyleBackColor = true;
            this.btnKeyword.Click += new System.EventHandler(this.btnKeyword_Click);
            // 
            // layoutMain
            // 
            resources.ApplyResources(this.layoutMain, "layoutMain");
            this.layoutMain.Controls.Add(this.pnlTools, 0, 1);
            this.layoutMain.Controls.Add(this.edSrc, 0, 0);
            this.layoutMain.Controls.Add(this.edDst, 1, 0);
            this.layoutMain.Name = "layoutMain";
            // 
            // pnlTools
            // 
            this.layoutMain.SetColumnSpan(this.pnlTools, 2);
            this.pnlTools.ContextMenuStrip = this.cmActions;
            this.pnlTools.Controls.Add(this.pnlOption);
            this.pnlTools.Controls.Add(this.btnWordFreq);
            this.pnlTools.Controls.Add(this.btnSrc2PyTM);
            this.pnlTools.Controls.Add(this.btnSrc2PyT);
            this.pnlTools.Controls.Add(this.btnSrc2Py);
            this.pnlTools.Controls.Add(this.btnTC2SC);
            this.pnlTools.Controls.Add(this.btnSC2TC);
            this.pnlTools.Controls.Add(this.btnPhrase);
            this.pnlTools.Controls.Add(this.btnKeyword);
            this.pnlTools.Controls.Add(this.btnSegment);
            this.pnlTools.Controls.Add(this.btnTokenizer);
            this.pnlTools.Controls.Add(this.btnSummary);
            resources.ApplyResources(this.pnlTools, "pnlTools");
            this.pnlTools.Name = "pnlTools";
            // 
            // cmActions
            // 
            this.cmActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmiActionFilterSub,
            this.cmiActionFilterLrc,
            this.cmiActionFilterMlTag,
            this.cmiSep0,
            this.cmiOptionCutStandard,
            this.cmiOptionCutCRF,
            this.cmiOptionCutNShort,
            this.cmiOptionCutIndex,
            this.cmiOptionCutNLP,
            this.cmiOptionCutHighSpeed});
            this.cmActions.Name = "cmFilterText";
            resources.ApplyResources(this.cmActions, "cmActions");
            // 
            // cmiActionFilterSub
            // 
            this.cmiActionFilterSub.Name = "cmiActionFilterSub";
            resources.ApplyResources(this.cmiActionFilterSub, "cmiActionFilterSub");
            this.cmiActionFilterSub.Click += new System.EventHandler(this.cmiFilterText_Click);
            // 
            // cmiActionFilterLrc
            // 
            this.cmiActionFilterLrc.Name = "cmiActionFilterLrc";
            resources.ApplyResources(this.cmiActionFilterLrc, "cmiActionFilterLrc");
            this.cmiActionFilterLrc.Click += new System.EventHandler(this.cmiFilterText_Click);
            // 
            // cmiActionFilterMlTag
            // 
            this.cmiActionFilterMlTag.Name = "cmiActionFilterMlTag";
            resources.ApplyResources(this.cmiActionFilterMlTag, "cmiActionFilterMlTag");
            this.cmiActionFilterMlTag.Click += new System.EventHandler(this.cmiFilterText_Click);
            // 
            // cmiSep0
            // 
            this.cmiSep0.Name = "cmiSep0";
            resources.ApplyResources(this.cmiSep0, "cmiSep0");
            // 
            // cmiOptionCutCRF
            // 
            this.cmiOptionCutCRF.CheckOnClick = true;
            this.cmiOptionCutCRF.Name = "cmiOptionCutCRF";
            resources.ApplyResources(this.cmiOptionCutCRF, "cmiOptionCutCRF");
            this.cmiOptionCutCRF.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // cmiOptionCutStandard
            // 
            this.cmiOptionCutStandard.Checked = true;
            this.cmiOptionCutStandard.CheckOnClick = true;
            this.cmiOptionCutStandard.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cmiOptionCutStandard.Name = "cmiOptionCutStandard";
            resources.ApplyResources(this.cmiOptionCutStandard, "cmiOptionCutStandard");
            this.cmiOptionCutStandard.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // pnlOption
            // 
            this.pnlOption.Controls.Add(this.lblInfo);
            this.pnlOption.Controls.Add(this.chkTermNature);
            resources.ApplyResources(this.pnlOption, "pnlOption");
            this.pnlOption.Name = "pnlOption";
            // 
            // lblInfo
            // 
            this.lblInfo.AutoEllipsis = true;
            resources.ApplyResources(this.lblInfo, "lblInfo");
            this.lblInfo.Name = "lblInfo";
            // 
            // chkTermNature
            // 
            resources.ApplyResources(this.chkTermNature, "chkTermNature");
            this.chkTermNature.Checked = true;
            this.chkTermNature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTermNature.Name = "chkTermNature";
            this.chkTermNature.UseVisualStyleBackColor = true;
            this.chkTermNature.CheckedChanged += new System.EventHandler(this.chkTermNature_CheckedChanged);
            // 
            // btnWordFreq
            // 
            resources.ApplyResources(this.btnWordFreq, "btnWordFreq");
            this.btnWordFreq.Name = "btnWordFreq";
            this.btnWordFreq.UseVisualStyleBackColor = true;
            this.btnWordFreq.Click += new System.EventHandler(this.btnWordFreq_Click);
            // 
            // btnSrc2PyTM
            // 
            resources.ApplyResources(this.btnSrc2PyTM, "btnSrc2PyTM");
            this.btnSrc2PyTM.Name = "btnSrc2PyTM";
            this.btnSrc2PyTM.Tag = "2";
            this.toolTip.SetToolTip(this.btnSrc2PyTM, resources.GetString("btnSrc2PyTM.ToolTip"));
            this.btnSrc2PyTM.UseVisualStyleBackColor = true;
            this.btnSrc2PyTM.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // btnSrc2PyT
            // 
            resources.ApplyResources(this.btnSrc2PyT, "btnSrc2PyT");
            this.btnSrc2PyT.Name = "btnSrc2PyT";
            this.btnSrc2PyT.Tag = "1";
            this.toolTip.SetToolTip(this.btnSrc2PyT, resources.GetString("btnSrc2PyT.ToolTip"));
            this.btnSrc2PyT.UseVisualStyleBackColor = true;
            this.btnSrc2PyT.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // btnSrc2Py
            // 
            resources.ApplyResources(this.btnSrc2Py, "btnSrc2Py");
            this.btnSrc2Py.Name = "btnSrc2Py";
            this.btnSrc2Py.Tag = "0";
            this.toolTip.SetToolTip(this.btnSrc2Py, resources.GetString("btnSrc2Py.ToolTip"));
            this.btnSrc2Py.UseVisualStyleBackColor = true;
            this.btnSrc2Py.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // btnTC2SC
            // 
            resources.ApplyResources(this.btnTC2SC, "btnTC2SC");
            this.btnTC2SC.Name = "btnTC2SC";
            this.toolTip.SetToolTip(this.btnTC2SC, resources.GetString("btnTC2SC.ToolTip"));
            this.btnTC2SC.UseVisualStyleBackColor = true;
            this.btnTC2SC.Click += new System.EventHandler(this.btnTC2SC_Click);
            // 
            // btnSC2TC
            // 
            resources.ApplyResources(this.btnSC2TC, "btnSC2TC");
            this.btnSC2TC.Name = "btnSC2TC";
            this.toolTip.SetToolTip(this.btnSC2TC, resources.GetString("btnSC2TC.ToolTip"));
            this.btnSC2TC.UseVisualStyleBackColor = true;
            this.btnSC2TC.Click += new System.EventHandler(this.btnSC2TC_Click);
            // 
            // btnPhrase
            // 
            resources.ApplyResources(this.btnPhrase, "btnPhrase");
            this.btnPhrase.Name = "btnPhrase";
            this.toolTip.SetToolTip(this.btnPhrase, resources.GetString("btnPhrase.ToolTip"));
            this.btnPhrase.UseVisualStyleBackColor = true;
            this.btnPhrase.Click += new System.EventHandler(this.btnPhrase_Click);
            // 
            // cmiOptionCutNShort
            // 
            this.cmiOptionCutNShort.CheckOnClick = true;
            this.cmiOptionCutNShort.Name = "cmiOptionCutNShort";
            resources.ApplyResources(this.cmiOptionCutNShort, "cmiOptionCutNShort");
            this.cmiOptionCutNShort.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // cmiOptionCutIndex
            // 
            this.cmiOptionCutIndex.CheckOnClick = true;
            this.cmiOptionCutIndex.Name = "cmiOptionCutIndex";
            resources.ApplyResources(this.cmiOptionCutIndex, "cmiOptionCutIndex");
            this.cmiOptionCutIndex.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // cmiOptionCutNLP
            // 
            this.cmiOptionCutNLP.CheckOnClick = true;
            this.cmiOptionCutNLP.Name = "cmiOptionCutNLP";
            resources.ApplyResources(this.cmiOptionCutNLP, "cmiOptionCutNLP");
            this.cmiOptionCutNLP.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // cmiOptionCutHighSpeed
            // 
            this.cmiOptionCutHighSpeed.CheckOnClick = true;
            this.cmiOptionCutHighSpeed.Name = "cmiOptionCutHighSpeed";
            resources.ApplyResources(this.cmiOptionCutHighSpeed, "cmiOptionCutHighSpeed");
            this.cmiOptionCutHighSpeed.Click += new System.EventHandler(this.cmiOptionCut_Click);
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutMain);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.MainForm_DragOver);
            this.layoutMain.ResumeLayout(false);
            this.layoutMain.PerformLayout();
            this.pnlTools.ResumeLayout(false);
            this.cmActions.ResumeLayout(false);
            this.pnlOption.ResumeLayout(false);
            this.pnlOption.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox edSrc;
        private System.Windows.Forms.Button btnSegment;
        private System.Windows.Forms.TextBox edDst;
        private System.Windows.Forms.Button btnTokenizer;
        private System.Windows.Forms.Button btnSummary;
        private System.Windows.Forms.Button btnKeyword;
        private System.Windows.Forms.TableLayoutPanel layoutMain;
        private System.Windows.Forms.Panel pnlTools;
        private System.Windows.Forms.Button btnTC2SC;
        private System.Windows.Forms.Button btnSC2TC;
        private System.Windows.Forms.Button btnPhrase;
        private System.Windows.Forms.Button btnSrc2Py;
        private System.Windows.Forms.Button btnSrc2PyTM;
        private System.Windows.Forms.Button btnSrc2PyT;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnWordFreq;
        private System.Windows.Forms.Panel pnlOption;
        private System.Windows.Forms.CheckBox chkTermNature;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.ContextMenuStrip cmActions;
        private System.Windows.Forms.ToolStripMenuItem cmiActionFilterSub;
        private System.Windows.Forms.ToolStripMenuItem cmiActionFilterLrc;
        private System.Windows.Forms.ToolStripMenuItem cmiActionFilterMlTag;
        private System.Windows.Forms.ToolStripSeparator cmiSep0;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutStandard;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutCRF;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutNShort;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutIndex;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutNLP;
        private System.Windows.Forms.ToolStripMenuItem cmiOptionCutHighSpeed;
    }
}

