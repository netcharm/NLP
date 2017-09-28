using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OCR_MS
{
    public partial class MainForm : Form
    {
        private string AppPath = Path.GetDirectoryName(Application.ExecutablePath);
        private string AppName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
        private static Dictionary<string, string> ApiKey = new Dictionary<string, string>();
        private bool CFGSAVE = false;
        private bool CFGLOADED = false;

        #region Monitor Clipboard
        [DllImport( "User32.dll", CharSet = CharSet.Auto )]
        public static extern IntPtr SetClipboardViewer( IntPtr hWndNewViewer );

        [DllImport( "User32.dll", CharSet = CharSet.Auto )]
        public static extern bool ChangeClipboardChain( IntPtr hWndRemove, IntPtr hWndNewNext );

        // WM_DRAWCLIPBOARD message
        private const int WM_DRAWCLIPBOARD = 0x0308;
        // Our variable that will hold the value to identify the next window in the clipboard viewer chain
        private IntPtr _clipboardViewerNext;
        private bool ClipboardChanged = false;
        #endregion

        #region OCR with microsoft cognitive api
        internal Dictionary<string, string> ocr_languages = new Dictionary<string, string>() {
            {"unk","AutoDetect"},
            {"zh-Hans","ChineseSimplified"},
            {"zh-Hant","ChineseTraditional"},
            {"cs","Czech"},
            {"da","Danish"},
            {"nl","Dutch"},
            {"en","English"},
            {"fi","Finnish"},
            {"fr","French"},
            {"de","German"},
            {"el","Greek"},
            {"hu","Hungarian"},
            {"it","Italian"},
            {"Ja","Japanese"},
            {"ko","Korean"},
            {"nb","Norwegian"},
            {"pl","Polish"},
            {"pt","Portuguese"},
            {"ru","Russian"},
            {"es","Spanish"},
            {"sv","Swedish"},
            {"tr","Turkish"},
            {"ar","Arabic"},
            {"ro","Romanian"},
            {"sr-Cyrl","SerbianCyrillic"},
            {"sr-Latn","SerbianLatin"},
            {"sk","Slovak"}
        };
        internal Dictionary<string, string> ocr_lang = new Dictionary<string, string>();

        internal void init_ocr_lang()
        {
            ocr_lang.Clear();
            foreach (var k in ocr_languages.Keys)
            {
                ocr_lang[ocr_languages[k]] = k;
            }
        }

        internal static ImageCodecInfo GetEncoder( ImageFormat format )
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach ( ImageCodecInfo codec in codecs )
            {
                if ( codec.FormatID == format.Guid )
                {
                    return codec;
                }
            }
            return null;
        }

        internal string ocr_ms( Bitmap src )
        {
            string result = "";

            var uri = @"https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr?language=unk&detectOrientation=true";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            //request.Timeout = 10000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; rv:37.0) Gecko/20190101 Firefox/87.0";
            request.Referer = @"https://westus.api.cognitive.microsoft.com";
            request.ContentType = "application/octet-stream";
            request.Headers["Ocp-Apim-Subscription-Key"] = "cd959382432345968384df3cd4663129";

            using ( Stream png = new MemoryStream() )
            {
                src.Save( png, ImageFormat.Png );
                byte[] buffer = ((MemoryStream)png).ToArray();
                string buf = "data:image/png;base64," + Convert.ToBase64String( buffer );
                request.ContentLength = buf.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write( Encoding.ASCII.GetBytes( buf ), 0, buf.Length );
                requestStream.Close();
                //using ( Stream requestStream = request.GetRequestStream() )
                //{
                //    //png.CopyTo( requestStream );
                //    requestStream.Write( Encoding.ASCII.GetBytes( buf ), 0, buf.Length );
                //    //requestStream.Flush();
                //}
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            }

            return ( result );
        }

        static async Task<string> MakeRequest( Bitmap src, string lang = "unk" )
        {
            string result = "";
            string ApiKey_CV = ApiKey.ContainsKey( "Computer Vision API" ) ? ApiKey["Computer Vision API"] : string.Empty;

            if ( string.IsNullOrEmpty( ApiKey_CV ) ) return ( result );

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add( "Ocp-Apim-Subscription-Key", ApiKey_CV );

            // Request parameters
            queryString["language"] = lang;
            queryString["detectOrientation "] = "true";
            var uri = "https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr?" + queryString;

            HttpResponseMessage response;

            using ( Stream png = new MemoryStream() )
            {
                src.Save( png, ImageFormat.Png );
                byte[] buffer = ((MemoryStream)png).ToArray();
                string buf = "data:image/png;base64," + Convert.ToBase64String( buffer );

                string T_SEP = "";

                // Request body
                using ( var content = new ByteArrayContent( buffer ) )
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );
                    response = await client.PostAsync( uri, content );
                    string ocr_result = await response.Content.ReadAsStringAsync();

                    JToken token = JObject.Parse( ocr_result );

                    JToken language = token.SelectToken( "$..language" );
                    if( language != null)
                    {
                        var ls = language.ToString().ToLower();
                        if ( ls.StartsWith( "zh-" ) || ls.StartsWith( "ja" ) || ls.StartsWith( "ko" ) )
                            T_SEP = "";
                        else T_SEP = " ";
                    }

                    IEnumerable<JToken> regions = token.SelectTokens("$..regions", false);
                    foreach ( var region in regions )
                    {
                        IEnumerable<JToken> lines = region.SelectTokens("$..lines", false);
                        foreach ( var line in lines )
                        {
                            IEnumerable<JToken> words = line.SelectTokens("$..words", false);
                            foreach ( var word in words )
                            {
                                IEnumerable<JToken> texts = word.SelectTokens("$..text", false);
                                foreach ( var text in texts )
                                {
                                    result += T_SEP + text.ToString();
                                }
                                result += "\r\n";
                            }
                            result += "\r\n";
                        }
                        result += "\r\n\r\n";
                    }
                }
            }
            return ( result );
        }
        #endregion

        protected override void WndProc( ref Message m )
        {
            base.WndProc( ref m );    // Process the message 

            if ( m.Msg == WM_DRAWCLIPBOARD )
            {
                ClipboardChanged = false;

                // Clipboard's data
                IDataObject iData = Clipboard.GetDataObject();

                if ( iData.GetDataPresent( DataFormats.Text ) )
                {
                    // Clipboard text
                    string text = (string)iData.GetData(DataFormats.Text);
                    // do something with it
                }
                else if ( iData.GetDataPresent( DataFormats.Bitmap ) )
                {
                    // Clipboard image
                    //Bitmap image = (Bitmap)iData.GetData(DataFormats.Bitmap);
                    // do something with it
                    ClipboardChanged = true;
                    if ( chkAutoClipboard.Checked && ClipboardChanged )
                    {
                        btnOCR.PerformClick();
                    }
                }
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void LoadConfig()
        {
            var cfg = Path.Combine(AppPath, AppName + ".json");
            if (File.Exists(cfg))
            {
                var json = File.ReadAllText(cfg);
                JToken token = JObject.Parse( json );

                IEnumerable<JToken> apis = token.SelectTokens("$..api", false);
                foreach ( var api in apis )
                {
                    var apikey = api.SelectToken( "$..key", false ).ToString();
                    var apiname = api.SelectToken( "$..name", false ).ToString();
                    if(apikey != null && apiname != null )
                    {
                        ApiKey[apiname] = apikey;
                    }
                }

                JToken pos = token.SelectToken("$..pos", false);
                if ( pos != null )
                {
                    var x = pos.SelectToken( "$..x", false ).ToString();
                    var y = pos.SelectToken( "$..y", false ).ToString();
                    if(x != null && y != null)
                    {
                        try
                        {
                            this.Left = Convert.ToInt32( x );
                            this.Top = Convert.ToInt32( y );
                        }
                        catch ( Exception )
                        {
                            //throw;
                        }
                    }
                }

                JToken size = token.SelectToken("$..size", false);
                if(size != null)
                {
                    var w = size.SelectToken( "$..w", false ).ToString();
                    var h = size.SelectToken( "$..h", false ).ToString();
                    if ( w != null && h != null )
                    {
                        try
                        {
                            this.Width = Convert.ToInt32( w );
                            this.Height = Convert.ToInt32( h );
                        }
                        catch ( Exception )
                        {
                            //throw;
                        }
                    }
                }
            }
            CFGLOADED = true;
        }

        private void SaveConfig()
        {
            var cfg = Path.Combine(AppPath, AppName + ".json");

            Dictionary<string, object> json = new Dictionary<string, object>()
            {
                { "pos", new Dictionary<string, int>()
                    {
                        { "x", this.Left },
                        { "y", this.Top  }
                    }
                },
                { "size", new Dictionary<string, int>()
                    {
                        { "w", this.Width },
                        { "h", this.Height }
                    }
                },
                { "api", ApiKey.Select( o => new Dictionary<string, string>() { { "name", o.Key }, { "key", o.Value } } ).ToList() }
            };
            

            File.WriteAllText( cfg, JsonConvert.SerializeObject( json, Formatting.Indented ) );
        }

        private void MainForm_Load( object sender, EventArgs e )
        {
            Icon = Icon.ExtractAssociatedIcon( Application.ExecutablePath );

            // Adds our form to the chain of clipboard viewers.
            _clipboardViewerNext = SetClipboardViewer( this.Handle );

            ////init_ocr_lang();
            cbLanguage.Items.Clear();
            cbLanguage.DataSource = new BindingSource( ocr_languages, null );
            cbLanguage.DisplayMember = "Value";
            cbLanguage.ValueMember = "Key";

            LoadConfig();
        }

        private void MainForm_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Control && e.KeyCode == Keys.S )
            {
                CFGSAVE = true;
                SaveConfig();
                CFGSAVE = false;
            }
        }

        private async void btnOCR_Click( object sender, EventArgs e )
        {
            if ( ApiKey.ContainsKey( "Computer Vision API" ) && btnOCR.Enabled && Clipboard.ContainsImage() )
            {
                btnOCR.Enabled = false;
                //this.UseWaitCursor = true;
                //edResult.UseWaitCursor = true;
                pbar.Style = ProgressBarStyle.Marquee;

                Bitmap src = (Bitmap)Clipboard.GetImage();
                string lang = cbLanguage.SelectedValue.ToString();
                edResult.Text = await MakeRequest( src, lang );
                if ( !string.IsNullOrEmpty( edResult.Text ) )
                {
                    //Clipboard.SetText( edResult.Text );
                    edResult.SelectAll();
                }

                pbar.Style = ProgressBarStyle.Blocks;
                //edResult.UseWaitCursor = false;
                //this.UseWaitCursor = false;
                ClipboardChanged = false;
                btnOCR.Enabled = true;
            }

            if ( !ApiKey.ContainsKey( "Computer Vision API" ) && edResult.Text.Trim().Length == 32 )
            {
                var dlgResult = MessageBox.Show( "Text in result box will be saved as API Key!", "Note", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning );
                if ( dlgResult == DialogResult.OK )
                    ApiKey["Computer Vision API"] = edResult.Text;
            }

            if ( CFGLOADED && !ApiKey.ContainsKey( "Computer Vision API" ) )
            {
                MessageBox.Show( "Microsoft Azure Cognitive Servise Computer Vision API key is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
            }
        }

        private void timer_Tick( object sender, EventArgs e )
        {
            if(chkAutoClipboard.Checked && ClipboardChanged)
            {
                btnOCR.PerformClick();
            }
        }

        private void edResult_KeyUp( object sender, KeyEventArgs e )
        {
            if ( e.Control && e.KeyCode == Keys.A )
            {
                edResult.SelectAll();
            }
        }

    }
}
