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
            this.edSrc = new System.Windows.Forms.TextBox();
            this.btnSegment = new System.Windows.Forms.Button();
            this.edDst = new System.Windows.Forms.TextBox();
            this.btnTokenizer = new System.Windows.Forms.Button();
            this.btnSummary = new System.Windows.Forms.Button();
            this.btnKeyword = new System.Windows.Forms.Button();
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlTools = new System.Windows.Forms.Panel();
            this.chkTermNature = new System.Windows.Forms.CheckBox();
            this.btnTC2SC = new System.Windows.Forms.Button();
            this.btnSC2TC = new System.Windows.Forms.Button();
            this.btnPhrase = new System.Windows.Forms.Button();
            this.btnSrc2Py = new System.Windows.Forms.Button();
            this.btnSrc2PyT = new System.Windows.Forms.Button();
            this.btnSrc2PyTM = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.layoutMain.SuspendLayout();
            this.pnlTools.SuspendLayout();
            this.SuspendLayout();
            // 
            // edSrc
            // 
            this.edSrc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edSrc.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.edSrc.Location = new System.Drawing.Point(3, 3);
            this.edSrc.Multiline = true;
            this.edSrc.Name = "edSrc";
            this.edSrc.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.edSrc.Size = new System.Drawing.Size(350, 375);
            this.edSrc.TabIndex = 0;
            this.edSrc.KeyUp += new System.Windows.Forms.KeyEventHandler(this.edSrc_KeyUp);
            // 
            // btnSegment
            // 
            this.btnSegment.Location = new System.Drawing.Point(13, 6);
            this.btnSegment.Name = "btnSegment";
            this.btnSegment.Size = new System.Drawing.Size(75, 23);
            this.btnSegment.TabIndex = 1;
            this.btnSegment.Text = "Segment";
            this.toolTip.SetToolTip(this.btnSegment, "Take the Segments from Source Text");
            this.btnSegment.UseVisualStyleBackColor = true;
            this.btnSegment.Click += new System.EventHandler(this.btnSegment_Click);
            // 
            // edDst
            // 
            this.edDst.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edDst.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.edDst.Location = new System.Drawing.Point(359, 3);
            this.edDst.Multiline = true;
            this.edDst.Name = "edDst";
            this.edDst.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.edDst.Size = new System.Drawing.Size(350, 375);
            this.edDst.TabIndex = 2;
            // 
            // btnTokenizer
            // 
            this.btnTokenizer.Location = new System.Drawing.Point(94, 6);
            this.btnTokenizer.Name = "btnTokenizer";
            this.btnTokenizer.Size = new System.Drawing.Size(75, 23);
            this.btnTokenizer.TabIndex = 3;
            this.btnTokenizer.Text = "Tokenizer";
            this.toolTip.SetToolTip(this.btnTokenizer, "Take the Tokenizer from Source Text");
            this.btnTokenizer.UseVisualStyleBackColor = true;
            this.btnTokenizer.Click += new System.EventHandler(this.btnTokenizer_Click);
            // 
            // btnSummary
            // 
            this.btnSummary.Location = new System.Drawing.Point(256, 6);
            this.btnSummary.Name = "btnSummary";
            this.btnSummary.Size = new System.Drawing.Size(75, 23);
            this.btnSummary.TabIndex = 4;
            this.btnSummary.Text = "Summary";
            this.toolTip.SetToolTip(this.btnSummary, "Take the Summary from Source Text");
            this.btnSummary.UseVisualStyleBackColor = true;
            this.btnSummary.Click += new System.EventHandler(this.btnSummary_Click);
            // 
            // btnKeyword
            // 
            this.btnKeyword.Location = new System.Drawing.Point(175, 6);
            this.btnKeyword.Name = "btnKeyword";
            this.btnKeyword.Size = new System.Drawing.Size(75, 23);
            this.btnKeyword.TabIndex = 5;
            this.btnKeyword.Text = "Keyword";
            this.toolTip.SetToolTip(this.btnKeyword, "Take the Keyword from Source Text");
            this.btnKeyword.UseVisualStyleBackColor = true;
            this.btnKeyword.Click += new System.EventHandler(this.btnKeyword_Click);
            // 
            // layoutMain
            // 
            this.layoutMain.ColumnCount = 2;
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.layoutMain.Controls.Add(this.pnlTools, 0, 1);
            this.layoutMain.Controls.Add(this.edSrc, 0, 0);
            this.layoutMain.Controls.Add(this.edDst, 1, 0);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.RowCount = 2;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.layoutMain.Size = new System.Drawing.Size(712, 453);
            this.layoutMain.TabIndex = 6;
            // 
            // pnlTools
            // 
            this.layoutMain.SetColumnSpan(this.pnlTools, 2);
            this.pnlTools.Controls.Add(this.btnSrc2PyTM);
            this.pnlTools.Controls.Add(this.btnSrc2PyT);
            this.pnlTools.Controls.Add(this.btnSrc2Py);
            this.pnlTools.Controls.Add(this.chkTermNature);
            this.pnlTools.Controls.Add(this.btnTC2SC);
            this.pnlTools.Controls.Add(this.btnSC2TC);
            this.pnlTools.Controls.Add(this.btnPhrase);
            this.pnlTools.Controls.Add(this.btnKeyword);
            this.pnlTools.Controls.Add(this.btnSegment);
            this.pnlTools.Controls.Add(this.btnTokenizer);
            this.pnlTools.Controls.Add(this.btnSummary);
            this.pnlTools.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTools.Location = new System.Drawing.Point(3, 384);
            this.pnlTools.Name = "pnlTools";
            this.pnlTools.Size = new System.Drawing.Size(706, 66);
            this.pnlTools.TabIndex = 7;
            // 
            // chkTermNature
            // 
            this.chkTermNature.AutoSize = true;
            this.chkTermNature.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkTermNature.Checked = true;
            this.chkTermNature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTermNature.Dock = System.Windows.Forms.DockStyle.Right;
            this.chkTermNature.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTermNature.Location = new System.Drawing.Point(581, 0);
            this.chkTermNature.Name = "chkTermNature";
            this.chkTermNature.Padding = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.chkTermNature.Size = new System.Drawing.Size(125, 66);
            this.chkTermNature.TabIndex = 9;
            this.chkTermNature.Text = "Show Term Nature";
            this.chkTermNature.UseVisualStyleBackColor = true;
            this.chkTermNature.CheckedChanged += new System.EventHandler(this.chkTermNature_CheckedChanged);
            // 
            // btnTC2SC
            // 
            this.btnTC2SC.Location = new System.Drawing.Point(94, 37);
            this.btnTC2SC.Name = "btnTC2SC";
            this.btnTC2SC.Size = new System.Drawing.Size(75, 23);
            this.btnTC2SC.TabIndex = 8;
            this.btnTC2SC.Text = "TC->SC";
            this.toolTip.SetToolTip(this.btnTC2SC, "Convert Source Text To Simplified Chinese");
            this.btnTC2SC.UseVisualStyleBackColor = true;
            this.btnTC2SC.Click += new System.EventHandler(this.btnTC2SC_Click);
            // 
            // btnSC2TC
            // 
            this.btnSC2TC.Location = new System.Drawing.Point(13, 37);
            this.btnSC2TC.Name = "btnSC2TC";
            this.btnSC2TC.Size = new System.Drawing.Size(75, 23);
            this.btnSC2TC.TabIndex = 7;
            this.btnSC2TC.Text = "SC->TC";
            this.toolTip.SetToolTip(this.btnSC2TC, "Convert Source Text To Traditional Chinese");
            this.btnSC2TC.UseVisualStyleBackColor = true;
            this.btnSC2TC.Click += new System.EventHandler(this.btnSC2TC_Click);
            // 
            // btnPhrase
            // 
            this.btnPhrase.Location = new System.Drawing.Point(337, 6);
            this.btnPhrase.Name = "btnPhrase";
            this.btnPhrase.Size = new System.Drawing.Size(75, 23);
            this.btnPhrase.TabIndex = 6;
            this.btnPhrase.Text = "Phrase";
            this.toolTip.SetToolTip(this.btnPhrase, "Take the Phrase from Source Text");
            this.btnPhrase.UseVisualStyleBackColor = true;
            this.btnPhrase.Click += new System.EventHandler(this.btnPhrase_Click);
            // 
            // btnSrc2Py
            // 
            this.btnSrc2Py.Location = new System.Drawing.Point(175, 37);
            this.btnSrc2Py.Name = "btnSrc2Py";
            this.btnSrc2Py.Size = new System.Drawing.Size(75, 23);
            this.btnSrc2Py.TabIndex = 10;
            this.btnSrc2Py.Tag = "0";
            this.btnSrc2Py.Text = "To Pinyin";
            this.toolTip.SetToolTip(this.btnSrc2Py, "Convert Source Text To Pinyin");
            this.btnSrc2Py.UseVisualStyleBackColor = true;
            this.btnSrc2Py.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // btnSrc2PyT
            // 
            this.btnSrc2PyT.Location = new System.Drawing.Point(256, 37);
            this.btnSrc2PyT.Name = "btnSrc2PyT";
            this.btnSrc2PyT.Size = new System.Drawing.Size(75, 23);
            this.btnSrc2PyT.TabIndex = 11;
            this.btnSrc2PyT.Tag = "1";
            this.btnSrc2PyT.Text = "To PinyinT";
            this.toolTip.SetToolTip(this.btnSrc2PyT, "Convert Source Text To Pinyin with Tone number");
            this.btnSrc2PyT.UseVisualStyleBackColor = true;
            this.btnSrc2PyT.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // btnSrc2PyTM
            // 
            this.btnSrc2PyTM.Location = new System.Drawing.Point(337, 37);
            this.btnSrc2PyTM.Name = "btnSrc2PyTM";
            this.btnSrc2PyTM.Size = new System.Drawing.Size(75, 23);
            this.btnSrc2PyTM.TabIndex = 12;
            this.btnSrc2PyTM.Tag = "2";
            this.btnSrc2PyTM.Text = "To PinyinM";
            this.toolTip.SetToolTip(this.btnSrc2PyTM, "Convert Source Text To Pinyin with Tone mark");
            this.btnSrc2PyTM.UseVisualStyleBackColor = true;
            this.btnSrc2PyTM.Click += new System.EventHandler(this.btnSrc2Py_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(712, 453);
            this.Controls.Add(this.layoutMain);
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "MainForm";
            this.Text = "HanLP";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.layoutMain.ResumeLayout(false);
            this.layoutMain.PerformLayout();
            this.pnlTools.ResumeLayout(false);
            this.pnlTools.PerformLayout();
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
        private System.Windows.Forms.CheckBox chkTermNature;
        private System.Windows.Forms.Button btnSrc2Py;
        private System.Windows.Forms.Button btnSrc2PyTM;
        private System.Windows.Forms.Button btnSrc2PyT;
        private System.Windows.Forms.ToolTip toolTip;
    }
}

