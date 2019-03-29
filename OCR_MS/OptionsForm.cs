using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OCR_MS
{
    public partial class OptionsForm : Form
    {
        public string APIKEY_CV
        {
            get { return edAPIKEY_CV.Text.Trim(); }
            set { edAPIKEY_CV.Text = value; }
        }

        public string APIKEYTITLE_CV
        {
            set { lblAPIKEY_CV.Text = value; }
        }

        public string APIKEY_TT
        {
            get { return edAPIKEY_TT.Text.Trim(); }
            set { edAPIKEY_TT.Text = value; }
        }

        public string APIKEYTITLE_TT
        {
            set { lblAPIKEY_TT.Text = value; }
        }

        public OptionsForm()
        {
            InitializeComponent();
        }
    }
}
