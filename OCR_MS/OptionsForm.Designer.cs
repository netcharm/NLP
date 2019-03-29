namespace OCR_MS
{
    partial class OptionsForm
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
            this.edAPIKEY_CV = new System.Windows.Forms.TextBox();
            this.lblAPIKEY_CV = new System.Windows.Forms.Label();
            this.lblAPIKEY_TT = new System.Windows.Forms.Label();
            this.edAPIKEY_TT = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // edAPIKEY_CV
            // 
            this.edAPIKEY_CV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.edAPIKEY_CV.Location = new System.Drawing.Point(13, 35);
            this.edAPIKEY_CV.Name = "edAPIKEY_CV";
            this.edAPIKEY_CV.Size = new System.Drawing.Size(329, 21);
            this.edAPIKEY_CV.TabIndex = 0;
            this.edAPIKEY_CV.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblAPIKEY_CV
            // 
            this.lblAPIKEY_CV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAPIKEY_CV.AutoSize = true;
            this.lblAPIKEY_CV.Location = new System.Drawing.Point(13, 17);
            this.lblAPIKEY_CV.Name = "lblAPIKEY_CV";
            this.lblAPIKEY_CV.Size = new System.Drawing.Size(41, 12);
            this.lblAPIKEY_CV.TabIndex = 1;
            this.lblAPIKEY_CV.Text = "label1";
            // 
            // lblAPIKEY_TT
            // 
            this.lblAPIKEY_TT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAPIKEY_TT.AutoSize = true;
            this.lblAPIKEY_TT.Location = new System.Drawing.Point(13, 76);
            this.lblAPIKEY_TT.Name = "lblAPIKEY_TT";
            this.lblAPIKEY_TT.Size = new System.Drawing.Size(41, 12);
            this.lblAPIKEY_TT.TabIndex = 3;
            this.lblAPIKEY_TT.Text = "label1";
            // 
            // edAPIKEY_TT
            // 
            this.edAPIKEY_TT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.edAPIKEY_TT.Location = new System.Drawing.Point(13, 94);
            this.edAPIKEY_TT.Name = "edAPIKEY_TT";
            this.edAPIKEY_TT.Size = new System.Drawing.Size(329, 21);
            this.edAPIKEY_TT.TabIndex = 2;
            this.edAPIKEY_TT.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(186, 226);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(267, 226);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // OptionsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(354, 261);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblAPIKEY_TT);
            this.Controls.Add(this.edAPIKEY_TT);
            this.Controls.Add(this.lblAPIKEY_CV);
            this.Controls.Add(this.edAPIKEY_CV);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox edAPIKEY_CV;
        private System.Windows.Forms.Label lblAPIKEY_CV;
        private System.Windows.Forms.Label lblAPIKEY_TT;
        private System.Windows.Forms.TextBox edAPIKEY_TT;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}