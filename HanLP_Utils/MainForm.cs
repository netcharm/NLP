using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using com.hankcs.hanlp;
using com.hankcs.hanlp.dictionary;
using com.hankcs.hanlp.dictionary.stopword;
using com.hankcs.hanlp.tokenizer;

namespace HanLP_Utils
{
    public partial class MainForm : Form
    {
        private string CWD = Path.GetDirectoryName(Application.ExecutablePath);

        private string ROOT = Path.GetDirectoryName(Application.ExecutablePath);
        private string CoreDictionaryPath = $"data/dictionary/CoreNatureDictionary.txt";
        private string BiGramDictionaryPath = $"data/dictionary/CoreNatureDictionary.ngram.txt";
        private string CoreStopWordDictionaryPath = $"data/dictionary/stopwords.txt";
        private string CoreSynonymDictionaryDictionaryPath = $"data/dictionary/synonym/CoreSynonym.txt";
        private string PersonDictionaryPath = $"data/dictionary/person/nr.txt";
        private string PersonDictionaryTrPath = $"data/dictionary/person/nr.tr.txt";
        private string TraditionalChineseDictionaryPath = $"data/dictionary/tc/TraditionalChinese.txt";
        private string CustomDictionaryPath = $"data/dictionary/custom/CustomDictionary.txt; 现代汉语补充词库.txt; 全国地名大全.txt ns; 人名词典.txt; 机构名词典.txt; 上海地名.txt ns; netcharm.txt nrf; 战舰少女N.txt nrf; data/dictionary/person/nrf.txt nrf";
        private string CRFSegmentModelPath = $"data/model/segment/CRFSegmentModel.txt";
        private string HMMSegmentModelPath = $"data/model/segment/HMMSegmentModel.bin";
        private bool ShowTermNature = true;

        public MainForm()
        {
            InitializeComponent();
        }

        private void LoadConfig()
        {
            var config = Path.Combine(CWD, "hanlp.properties");
            if ( File.Exists( config ) )
            {
                var lines = File.ReadAllLines(config);
                foreach ( string line in lines )
                {
                    if ( line.StartsWith( "#" ) ) continue;
                    var kv = line.Split('=');
                    if ( kv.Length == 2 )
                    {
                        var k = kv[0].Trim();
                        var v = kv[1].Trim();

                        if ( k.Equals( "root", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            ROOT = v;
                        }
                        else if ( k.Equals( "CoreDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            CoreDictionaryPath = v;
                        }
                        else if ( k.Equals( "BiGramDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            BiGramDictionaryPath = v;
                        }
                        else if ( k.Equals( "CoreStopWordDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            CoreStopWordDictionaryPath = v;
                        }
                        else if ( k.Equals( "CoreSynonymDictionaryDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            CoreSynonymDictionaryDictionaryPath = v;
                        }
                        else if ( k.Equals( "PersonDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            PersonDictionaryPath = v;
                        }
                        else if ( k.Equals( "PersonDictionaryTrPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            PersonDictionaryTrPath = v;
                        }
                        else if ( k.Equals( "TraditionalChineseDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            TraditionalChineseDictionaryPath = v;
                        }
                        else if ( k.Equals( "CustomDictionaryPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            CustomDictionaryPath = v;
                        }
                        else if ( k.Equals( "CRFSegmentModelPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            CRFSegmentModelPath = v;
                        }
                        else if ( k.Equals( "HMMSegmentModelPath", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            HMMSegmentModelPath = v;
                        }
                        else if ( k.Equals( "ShowTermNature", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            if ( bool.TryParse( v, out ShowTermNature ) )
                            {
                                //HanLP.Config.ShowTermNature = ShowTermNature;
                            }
                        }
                    }
                }
            }
            java.lang.System.getProperties().setProperty( "java.class.path", $"{ROOT};." );
            HanLP.Config.ShowTermNature = ShowTermNature;
            chkTermNature.Checked = HanLP.Config.ShowTermNature;
        }

        private void AddCustomDict()
        {
            List<string> filelist = new List<string>();
            filelist.AddRange(CustomDictionary.path);
            var CustomDict = CustomDictionaryPath.Split(';');
            if ( CustomDict.Length > filelist.Count )
            {
                filelist.Clear();
                string lastfolder = "";
                for ( int i = 0; i < CustomDict.Length; i++ )
                {
                    if ( string.IsNullOrEmpty( Path.GetDirectoryName( CustomDict[i] ) ) )
                    {
                        filelist.Add( Path.Combine( ROOT, lastfolder, CustomDict[i].Trim() ) );
                    }
                    else
                    {
                        filelist.Add( Path.Combine( ROOT, CustomDict[i].Trim() ) );
                        lastfolder = Path.GetDirectoryName( CustomDict[i] );
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            List<string> ss = new List<string>();
            foreach ( string file in filelist )
            {
                try
                {
                    var fn = $"{Path.GetDirectoryName(file)}\\{Path.GetFileNameWithoutExtension(file)}.txt";
                    if(!Directory.Exists(ROOT))
                    {
                        fn = fn.Replace( Path.GetFullPath(ROOT), CWD+Path.DirectorySeparatorChar );
                    }
                    var nt = Path.GetExtension(file).Split();
                    if ( File.Exists( fn ) )
                    {
                        var lines = File.ReadAllLines(fn);
                        if ( nt.Length > 1 )
                        {
                            var nu = string.Join(" ", nt.Skip(1));
                            for ( int i = 0; i < lines.Length; i++ )
                            {
                                lines[i] = $"{lines[i]} {nu} 1";
                            }
                        }
                        ss.AddRange( lines );
                    }
                }
                catch ( Exception ex )
                {
                    MessageBox.Show( ex.ToString() );
                }
            }
            foreach ( var w in ss )
            {
                try
                {
                    var ws = w.Split();
                    if ( string.IsNullOrEmpty( w ) )
                        continue;
                    else if ( ws.Length == 1 )
                        CustomDictionary.add( ws[0].Trim() );
                    else if ( ws.Length >= 2 )
                    {
                        var nf = string.Join(" ", ws.Skip(1));
                        CustomDictionary.add( ws[0].Trim(), nf.Trim() );
                    }
                }
                catch { }
            }
            var stopwords = new string[] { "。" , "，", "　" };
            foreach ( var w in stopwords )
            {
                CoreStopWordDictionary.add( w );
            }
        }

        private void MainForm_Load( object sender, EventArgs e )
        {
            Icon = Icon.ExtractAssociatedIcon( Application.ExecutablePath );
            try
            {
                LoadConfig();
                AddCustomDict();
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.ToString() );
            }
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
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                var text = HanLP.segment( line.Trim().Replace("　", " ") ).toArray();
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
                var text = NLPTokenizer.segment( line.Trim().Replace("　", " ") ).toArray();
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
            var ro = RegexOptions.IgnoreCase | RegexOptions.Multiline;
            edDst.Text = Regex.Replace(string.Join( ", ", text), @"[　| ]{2,}", " ", ro );
        }

        private void btnPhrase_Click( object sender, EventArgs e )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                var text = HanLP.extractPhrase( line.Trim().Replace("　", " "), 10 ).toArray();                
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

        private void chkTermNature_CheckedChanged( object sender, EventArgs e )
        {
            HanLP.Config.ShowTermNature = chkTermNature.Checked;
        }

    }
}
