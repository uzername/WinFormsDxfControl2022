using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsDxfControl
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if ((String.IsNullOrEmpty( this.textBox1.Text)==false)&&(System.IO.File.Exists(this.textBox1.Text)))
            {
                bool doMirror = this.checkBoxMirror.Checked;
                double angleDegr = (double)this.numericUpDown1.Value;
                this.winFormsDxfRenderer1.processDXFfileNow(true, this.textBox1.Text, angleDegr, doMirror);
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "DXF files|*.dxf";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                bool doMirror = this.checkBoxMirror.Checked;
                double angleDegr = (double)this.numericUpDown1.Value;
                this.textBox1.Text = fileDialog.FileName;
                this.winFormsDxfRenderer1.processDXFfileNow(true, this.textBox1.Text, angleDegr, doMirror);
            }
        }

        private void checkBoxMirror_CheckedChanged(object sender, EventArgs e)
        {
            if ((String.IsNullOrEmpty(this.textBox1.Text) == false) && (System.IO.File.Exists(this.textBox1.Text)))
            {
                bool doMirror = this.checkBoxMirror.Checked;
                double angleDegr = (double)this.numericUpDown1.Value;
                this.winFormsDxfRenderer1.processDXFfileNow(false, this.textBox1.Text, angleDegr, doMirror);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if ((String.IsNullOrEmpty(this.textBox1.Text) == false) && (System.IO.File.Exists(this.textBox1.Text)))
            {
                bool doMirror = this.checkBoxMirror.Checked;
                double angleDegr = (double)this.numericUpDown1.Value;
                this.winFormsDxfRenderer1.processDXFfileNow(false, this.textBox1.Text, angleDegr, doMirror);
            }
        }
    }
}
