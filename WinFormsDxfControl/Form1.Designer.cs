
namespace WinFormsDxfControl
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.WinFormsDxfRendererInstance = new WinFormsDxfControl1.WinFormsDxfRenderer();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.labelPath = new System.Windows.Forms.Label();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.checkBoxMirror = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // WinFormsDxfRendererInstance
            // 
            this.WinFormsDxfRendererInstance.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WinFormsDxfRendererInstance.Location = new System.Drawing.Point(0, 0);
            this.WinFormsDxfRendererInstance.Name = "WinFormsDxfRendererInstance";
            this.WinFormsDxfRendererInstance.Size = new System.Drawing.Size(797, 451);
            this.WinFormsDxfRendererInstance.TabIndex = 0;
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(90, 454);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(659, 23);
            this.textBox1.TabIndex = 1;
            // 
            // labelPath
            // 
            this.labelPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPath.AutoSize = true;
            this.labelPath.Location = new System.Drawing.Point(0, 457);
            this.labelPath.Name = "labelPath";
            this.labelPath.Size = new System.Drawing.Size(84, 15);
            this.labelPath.TabIndex = 2;
            this.labelPath.Text = "Path to dxf file";
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(755, 454);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(42, 23);
            this.buttonBrowse.TabIndex = 3;
            this.buttonBrowse.Text = "...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            // 
            // checkBoxMirror
            // 
            this.checkBoxMirror.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxMirror.AutoSize = true;
            this.checkBoxMirror.Location = new System.Drawing.Point(3, 481);
            this.checkBoxMirror.Name = "checkBoxMirror";
            this.checkBoxMirror.Size = new System.Drawing.Size(59, 19);
            this.checkBoxMirror.TabIndex = 4;
            this.checkBoxMirror.Text = "Mirror";
            this.checkBoxMirror.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(68, 482);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "Angle, from 0 to 360 deg";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 503);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxMirror);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.labelPath);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.WinFormsDxfRendererInstance);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WinFormsDxfControl1.WinFormsDxfRenderer WinFormsDxfRendererInstance;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label labelPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.CheckBox checkBoxMirror;
        private System.Windows.Forms.Label label1;
    }
}

