using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using com.hankcs.hanlp;
using com.hankcs.hanlp.tokenizer;

namespace HanLP_Utils
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon( Application.ExecutablePath );
        }

        private void edSrc_KeyUp( object sender, KeyEventArgs e )
        {
            if ( e.Control && e.KeyCode == Keys.A )
            {
                edSrc.SelectAll();
            }
        }

        private void btnSegment_Click( object sender, EventArgs e )
        {
            //string segments = HanLP.segment( "你好，欢迎在CSharp中调用HanLP的API！" ).ToString();
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines)
            {
                var text = HanLP.segment( line ).toArray();
                if ( text.Length <= 0 ) continue;
                sb.AppendLine( string.Join( ", ", text ).Trim() );
            }
            edDst.Text = string.Join( "\n", sb );
        }

        private void btnTokenizer_Click( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                var text = NLPTokenizer.segment( line ).toArray();
                if ( text.Length <= 0 ) continue;
                sb.AppendLine( string.Join( ", ", text ).Trim() );
            }
            edDst.Text = string.Join( "\n", sb);
        }

        private void btnKeyword_Click( object sender, EventArgs e )
        {
            var text = HanLP.extractKeyword( edSrc.Text, 25 ).toArray();
            if ( text.Length <= 0 ) return;
            edDst.Text = string.Join( ", ", text);
        }

        private void btnSummary_Click( object sender, EventArgs e )
        {
            var text = HanLP.extractSummary( edSrc.Text, 15 ).toArray();
            if ( text.Length <= 0 ) return;
            edDst.Text = string.Join( ", ", text);
        }

        private void btnPhrase_Click( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                var text = HanLP.extractPhrase( line, 10 ).toArray();                
                if ( text.Length <= 0 ) continue;
                sb.AppendLine( string.Join(", ", text ).Trim() );
            }
            edDst.Text = string.Join( "\n", sb );

        }

        private void btnSC2TC_Click( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                sb.AppendLine( HanLP.convertToTraditionalChinese( line ).ToString() );
            }
            edDst.Text = string.Join( "\n", sb );
            
        }

        private void btnTC2SC_Click( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                sb.AppendLine( HanLP.convertToSimplifiedChinese( line ).ToString() );
            }
            edDst.Text = string.Join( "\n", sb );
        }
    }
}
