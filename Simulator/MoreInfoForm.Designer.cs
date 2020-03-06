namespace simulator
{
    partial class MoreInfoForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoreInfoForm));
            this.rtbMoreInfo = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtbMoreInfo
            // 
            this.rtbMoreInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMoreInfo.Location = new System.Drawing.Point(0, 0);
            this.rtbMoreInfo.Name = "rtbMoreInfo";
            this.rtbMoreInfo.Size = new System.Drawing.Size(602, 325);
            this.rtbMoreInfo.TabIndex = 0;
            this.rtbMoreInfo.Text = resources.GetString("rtbMoreInfo.Text");
            // 
            // MoreInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(602, 325);
            this.Controls.Add(this.rtbMoreInfo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MoreInfoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MoreInfoForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbMoreInfo;
    }
}