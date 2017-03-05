using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using com.hankcs.hanlp;
using com.hankcs.hanlp.dictionary;
using com.hankcs.hanlp.dictionary.py;
using com.hankcs.hanlp.dictionary.stopword;
using com.hankcs.hanlp.tokenizer;
using HtmlAgilityPack;

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
            filelist.AddRange( CustomDictionary.path );
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
                    if ( !Directory.Exists( ROOT ) )
                    {
                        fn = fn.Replace( Path.GetFullPath( ROOT ), CWD + Path.DirectorySeparatorChar );
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

        private string ReadUrl( string url )
        {
            string result = string.Empty;

            //HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(URL);
            //myRequest.Method = "GET";
            //WebResponse myResponse = myRequest.GetResponse();
            //StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            //result = sr.ReadToEnd();
            //sr.Close();
            //myResponse.Close();

            HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);
            var scripts = doc.DocumentNode.SelectNodes( "//script" );
            var styles = doc.DocumentNode.SelectNodes( "//style" );
            var links = doc.DocumentNode.SelectNodes( "//a" );
            var comments = doc.DocumentNode.SelectNodes( "//comment()" );
            foreach ( var node in scripts ) { node.Remove(); }
            foreach ( var node in styles ) { node.Remove(); }
            foreach ( var node in links ) { node.Remove(); }
            foreach ( var node in comments ) { node.Remove(); }

            result = doc.DocumentNode.SelectSingleNode( "//body" ).InnerText.Trim();

            result = Regex.Replace( result, @"[ |(\r\n)|(\t)]{2,}", ", ", RegexOptions.IgnoreCase );
            result = Regex.Replace( result, @"[(&gt;)|(&lt;)|(&amp;)]{1,}", " ", RegexOptions.IgnoreCase );

            return ( result );
        }

        private string[] GetLinks(string html)
        {
            List<string> links = new List<string>();

            HtmlAgilityPack.HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument(); //web.Load(html);
            doc.LoadHtml( html );
            var alist = doc.DocumentNode.SelectNodes( "//a" );
            foreach(var a in alist)
            {
                string href = a.GetAttributeValue( "href", "" );
                links.Add( href );
            }

            return ( links.ToArray() );
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

        private void MainForm_DragOver( object sender, DragEventArgs e )
        {
            if ( e.Data.GetDataPresent( DataFormats.FileDrop ) )
            {
                string[] dragFiles = (string [])e.Data.GetData(DataFormats.FileDrop, true);
                if ( dragFiles.Length > 0 )
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
            else if ( e.Data.GetDataPresent( DataFormats.Text ) || e.Data.GetDataPresent( DataFormats.UnicodeText ) )
            {
                e.Effect = DragDropEffects.Copy;
            }
            return;
        }

        private void MainForm_DragDrop( object sender, DragEventArgs e )
        {
            // Determine whether string data exists in the drop data. If not, then 
            // the drop effect reflects that the drop cannot occur. 
            if ( e.Data.GetDataPresent( DataFormats.FileDrop ) )
            {
                //e.Effect = DragDropEffects.Copy;
                try
                {
                    string[] dragFiles = (string [])e.Data.GetData(DataFormats.FileDrop, true);
                    if ( dragFiles.Length > 0 )
                    {
                        string dragFileName = dragFiles[0].ToString();
                        string ext = Path.GetExtension(dragFileName).ToLower();
                        string[] text = { ".txt", ".text"};
                        string[] html = { ".htm", ".html", ".xml"};

                        if ( dragFileName.EndsWith( ".url", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            var content = File.ReadAllLines( dragFileName );
                            foreach ( var line in content )
                            {

                            }
                        }
                        else if ( text.Contains( ext )) 
                        {
                            edSrc.Text = File.ReadAllText( dragFileName );
                        }
                        else if ( html.Contains( ext ) ) 
                        {
                            edSrc.Text = ReadUrl( dragFileName );
                        }
                    }
                }
                catch
                {

                }
            }
            //else if ( e.Data.GetDataPresent( DataFormats.Html ) )
            //{
            //    var content = e.Data.GetData( DataFormats.Html, true ).ToString();
            //    edSrc.Text = string.Join("\n", GetLinks( content ));
            //}
            else if ( e.Data.GetDataPresent( DataFormats.Text ) || 
                      e.Data.GetDataPresent( DataFormats.UnicodeText ))
            {
                var content = e.Data.GetData( "System.String", true ).ToString();
                if ( content.StartsWith( "http://", StringComparison.CurrentCultureIgnoreCase ) ||
                     content.StartsWith( "https://", StringComparison.CurrentCultureIgnoreCase ) ||
                     content.StartsWith( "ftp://", StringComparison.CurrentCultureIgnoreCase ) ||
                     content.StartsWith( "file://", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    edSrc.Text = ReadUrl( content );
                }
                else
                {
                    edSrc.Text = content;
                }
            }
            return;
        }

        private void chkTermNature_CheckedChanged( object sender, EventArgs e )
        {
            HanLP.Config.ShowTermNature = chkTermNature.Checked;
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

        private void btnSrc2Py_Click( object sender, EventArgs e )
        {
            var mode = Convert.ToInt32( ( sender as Button ).Tag );
            StringBuilder sb = new StringBuilder();
            foreach ( string line in edSrc.Lines )
            {
                List<string> text = new List<string>();
                foreach ( Pinyin py in HanLP.convertToPinyinList( line.Trim().Replace( "　", " " ) ).toArray() )
                {
                    if ( mode == 0 )
                        text.Add( py.getPinyinWithoutTone().ToString() );
                    else if ( mode == 1 )
                        text.Add( py.ToString() );
                    else if ( mode == 2 )
                        text.Add( py.getPinyinWithToneMark().ToString() );
                }
                if ( text.Count <= 0 ) continue;
                sb.AppendLine( string.Join( ", ", text ).Trim() );
            }
            edDst.Text = string.Join( "\n", sb );
        }
    }
}
