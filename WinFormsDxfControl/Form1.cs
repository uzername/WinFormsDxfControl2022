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
            if (String.IsNullOrEmpty( this.textBox1.Text)==false)
            {
                this.winFormsDxfRenderer1.processDXFfileNow(true, this.textBox1.Text, 0, false);
            }
        }
    }
}
