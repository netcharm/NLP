using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Speech.Synthesis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OCR_MS
{
    public partial class MainForm : Form
    {
        private string AppPath = Path.GetDirectoryName(Application.ExecutablePath);
        private string AppName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);

        private static Dictionary<string, string> ApiKey = new Dictionary<string, string>();

        private bool CFGLOADED = false;

        #region Monitor Clipboard
        [DllImport( "User32.dll", CharSet = CharSet.Auto )]
        public static extern IntPtr SetClipboardViewer( IntPtr hWndNewViewer );

        [DllImport( "User32.dll", CharSet = CharSet.Auto )]
        public static extern bool ChangeClipboardChain( IntPtr hWndRemove, IntPtr hWndNewNext );

        // WM_DRAWCLIPBOARD message
        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CLIPBOARDUPDATE = 0x031D;
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
            {"ja","Japanese"},
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

        internal string Result_JSON = string.Empty;
        internal string Result_Lang = string.Empty;
        internal static int ResultHistoryLimit = 100;
        internal List<KeyValuePair<string, string>> ResultHistory = new List<KeyValuePair<string, string>>(ResultHistoryLimit);

        internal void init_ocr_lang()
        {
            ocr_lang.Clear();
            foreach( var k in ocr_languages.Keys )
            {
                ocr_lang[ocr_languages[k]] = k;
            }
        }

        internal static ImageCodecInfo GetEncoder( ImageFormat format )
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach( ImageCodecInfo codec in codecs )
            {
                if( codec.FormatID == format.Guid )
                {
                    return codec;
                }
            }
            return null;
        }

        internal string ocr_ms( Bitmap src, string lang = "unk" )
        {
            string result = "";

            var uri = @"https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr?language=unk&detectOrientation=true";

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create( uri );
            request.Method = "POST";
            //request.Timeout = 10000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; rv:37.0) Gecko/20190101 Firefox/87.0";
            request.Referer = @"https://westus.api.cognitive.microsoft.com";
            request.ContentType = "application/octet-stream";
            request.Headers["Ocp-Apim-Subscription-Key"] = "cd959382432345968384df3cd4663129";

            using( Stream png = new MemoryStream() )
            {
                src.Save( png, ImageFormat.Png );
                byte[] buffer = ( (MemoryStream) png ).ToArray();
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
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            }

            return ( result );
        }

        internal async Task<string> MakeRequest_OCR( Bitmap src, string lang = "unk" )
        {
            string result = "";
            string ApiKey_CV = ApiKey.ContainsKey( "Computer Vision API" ) ? ApiKey["Computer Vision API"] : string.Empty;

            if( string.IsNullOrEmpty( ApiKey_CV ) ) return ( result );

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString( string.Empty );

            // Request headers
            client.DefaultRequestHeaders.Add( "Ocp-Apim-Subscription-Key", ApiKey_CV );

            // Request parameters
            queryString["language"] = lang;
            queryString["detectOrientation "] = "true";
            var uri = "https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr?" + queryString;

            HttpResponseMessage response;

            using( Stream png = new MemoryStream() )
            {
                src.Save( png, ImageFormat.Png );
                byte[] buffer = ( (MemoryStream) png ).ToArray();
                string buf = "data:image/png;base64," + Convert.ToBase64String( buffer );

                string W_SEP = "";

                // Request body
                using( var content = new ByteArrayContent( buffer ) )
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );
                    response = await client.PostAsync( uri, content );
                    string ocr_result = await response.Content.ReadAsStringAsync();

                    JToken token = JObject.Parse( ocr_result );
                    Result_JSON = JsonConvert.SerializeObject( token, Formatting.Indented );
                    Result_JSON = Result_JSON.Replace( "\\\"", "\"" );

                    JToken language = token.SelectToken( "$..language" );
                    if( language != null )
                    {
                        Result_Lang = language.ToString().ToLower();
                        if( Result_Lang.StartsWith( "zh-" ) || Result_Lang.StartsWith( "ja" ) || Result_Lang.StartsWith( "ko" ) )
                            W_SEP = "";
                        else W_SEP = " ";
                    }

                    StringBuilder sb = new StringBuilder();
                    IEnumerable<JToken> regions = token.SelectTokens( "$..regions", false );
                    foreach( var region in regions )
                    {
                        List<string> ocr_line = new List<string>();
                        IEnumerable<JToken> lines = region.SelectTokens( "$..lines", false );
                        foreach( var line in lines )
                        {
                            IEnumerable<JToken> words = line.SelectTokens( "$..words", false );
                            foreach( var word in words )
                            {
                                List<string> ocr_word = new List<string>();
                                IEnumerable<JToken> texts = word.SelectTokens( "$..text", false );
                                foreach( var text in texts )
                                {
                                    ocr_word.Add( text.ToString() );
                                    //sb.Append( W_SEP + text.ToString() );
                                }
                                sb.AppendLine( string.Join( W_SEP, ocr_word ) );
                            }
                            sb.AppendLine();
                        }
                        sb.AppendLine();
                    }
                    result = sb.ToString().Trim();
                }
            }
            return ( result );
        }
        #endregion

        #region Speech
        private SpeechSynthesizer synth = null;
        private string voice_default = string.Empty;
        #endregion

        protected override void WndProc( ref Message m )
        {
            base.WndProc( ref m );    // Process the message 

            //if ( m.Msg == WM_CLIPBOARDUPDATE )
            if( m.Msg == WM_DRAWCLIPBOARD )
            {
                ClipboardChanged = false;

                // Clipboard's data
                IDataObject iData = Clipboard.GetDataObject();

                if( iData.GetDataPresent( DataFormats.Bitmap ) )
                {
                    // Clipboard image
                    ClipboardChanged = true;
                    if( chkAutoClipboard.Checked )
                    {
                        //tsmiShowWindow.PerformClick();
                        btnOCR.PerformClick();
                    }
                }
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadConfig()
        {
            var cfg = Path.Combine( AppPath, AppName + ".json" );
            if( File.Exists( cfg ) )
            {
                var json = File.ReadAllText( cfg );
                JToken token = JObject.Parse( json );

                #region API Key
                IEnumerable<JToken> apis = token.SelectTokens( "$..api", false );
                foreach( var api in apis )
                {
                    var apikey = api.SelectToken( "$..key", false ).ToString();
                    var apiname = api.SelectToken( "$..name", false ).ToString();
                    if( apikey != null && apiname != null )
                    {
                        ApiKey[apiname] = apikey;
                    }
                }
                #endregion

                #region Form Position
                JToken pos = token.SelectToken( "$..pos", false );
                if( pos != null )
                {
                    var x = pos.SelectToken( "$..x", false ).ToString();
                    var y = pos.SelectToken( "$..y", false ).ToString();
                    if( x != null && y != null )
                    {
                        try
                        {
                            this.Left = Convert.ToInt32( x );
                            this.Top = Convert.ToInt32( y );
                        }
                        catch( Exception )
                        {
                            //throw;
                        }
                    }
                }
                #endregion

                #region Form Size
                JToken size = token.SelectToken( "$..size", false );
                if( size != null )
                {
                    var w = size.SelectToken( "$..w", false ).ToString();
                    var h = size.SelectToken( "$..h", false ).ToString();
                    if( w != null && h != null )
                    {
                        try
                        {
                            this.Width = Math.Max( this.MinimumSize.Width, Convert.ToInt32( w ) );
                            this.Height = Math.Max( this.MinimumSize.Height, Convert.ToInt32( h ) );
                        }
                        catch( Exception )
                        {
                            //throw;
                        }
                    }
                }
                #endregion

                #region Form Opacity
                JToken opacity = token.SelectToken( "$..opacity", false );
                if( opacity != null )
                {
                    this.Opacity = Convert.ToDouble( opacity.ToString() );
                    string os = $"{Math.Round( this.Opacity * 100, 0 )}%";
                    foreach( ToolStripMenuItem mi in tsmiOpacity.DropDownItems )
                    {
                        if( mi.Text == os )
                            mi.Checked = true;
                        else
                            mi.Checked = false;
                    }
                }
                #endregion
            }
            CFGLOADED = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SaveConfig()
        {
            var cfg = Path.Combine( AppPath, AppName + ".json" );

            Dictionary<string, object> json = new Dictionary<string, object>()
            {
                { "opacity", this.Opacity },
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

            synth = new SpeechSynthesizer();
            voice_default = synth.Voice.Name;
            synth.SpeakStarted += Synth_SpeakStarted;
            synth.SpeakProgress += Synth_SpeakProgress;
            synth.StateChanged += Synth_StateChanged;
            synth.SpeakCompleted += Synth_SpeakCompleted;

            notify.Icon = Icon;
            notify.BalloonTipTitle = this.Text;
            notify.BalloonTipText = "Using \"Computer Vision API\" OCR feature.";
            notify.Text = this.Text;

            hint.ToolTipTitle = this.Text;

            // Adds our form to the chain of clipboard viewers.
            _clipboardViewerNext = SetClipboardViewer( this.Handle );

            ////init_ocr_lang();
            cbLanguage.Items.Clear();
            cbLanguage.DataSource = new BindingSource( ocr_languages, null );
            cbLanguage.DisplayMember = "Value";
            cbLanguage.ValueMember = "Key";

            LoadConfig();
        }

        private void Synth_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (synth == null) return;

            if(synth.State == SynthesizerState.Paused)
            {
                tsmiTextPlay.Checked = true;
                tsmiTextPause.Checked = true;
                tsmiTextStop.Checked = false;
            }
            else if (synth.State == SynthesizerState.Speaking)
            {
                tsmiTextPlay.Checked = true;
                tsmiTextPause.Checked = false;
                tsmiTextStop.Checked = false;
            }
            else if (synth.State == SynthesizerState.Ready)
            {
                tsmiTextPlay.Checked = false;
                tsmiTextPause.Checked = false;
                tsmiTextStop.Checked = true;
            }
        }

        private void Synth_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            tsmiTextPlay.Checked = true;
            tsmiTextPause.Checked = false;
            tsmiTextStop.Checked = false;
        }

        private void Synth_SpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            tsmiTextPlay.Checked = false;
            tsmiTextPause.Checked = false;
            tsmiTextStop.Checked = true;
        }

        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            if( e.CloseReason == CloseReason.UserClosing )
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void MainForm_KeyUp( object sender, KeyEventArgs e )
        {
            if( e.Control && e.KeyCode == Keys.S )
            {
                tsmiSaveState.PerformClick();
            }
            else if( e.KeyCode == Keys.Escape )
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void notify_Click( object sender, EventArgs e )
        {
            // Show() method can not autoclosed like system contextmenu's behaving
            //notifyMenu.Show( this, Control.MousePosition );

            MethodInfo mi = typeof( NotifyIcon ).GetMethod( "ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic );
            mi.Invoke( notify, null );
        }

        private void notify_MouseClick( object sender, MouseEventArgs e )
        {
            //notifyMenu.Show( this, Cursor.Position.X, Cursor.Position.Y );
        }

        private void notify_DoubleClick( object sender, EventArgs e )
        {
            tsmiShowWindow.PerformClick();
        }

        private void timer_Tick( object sender, EventArgs e )
        {
            if( chkAutoClipboard.Checked && ClipboardChanged )
            {
                btnOCR.PerformClick();
            }
        }

        private void edResult_KeyUp( object sender, KeyEventArgs e )
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                edResult.SelectAll();
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                //if (ResultHistory.Count > 0)
                //{
                //    edResult.Text = ResultHistory.Last().Key;
                //    Result_Lang = ResultHistory.Last().Value;
                //    edResult.Tag = 
                //}
            }
        }

        private async void btnOCR_Click( object sender, EventArgs e )
        {
            if( ApiKey.ContainsKey( "Computer Vision API" ) && btnOCR.Enabled )
            {
                IDataObject iData = Clipboard.GetDataObject();
                if( iData.GetDataPresent( DataFormats.Bitmap ) )
                {
                    btnOCR.Enabled = false;
                    pbar.Style = ProgressBarStyle.Marquee;

                    //(Bitmap) iData.GetData( DataFormats.Bitmap );
                    Bitmap src = (Bitmap) Clipboard.GetImage();
                    string lang = cbLanguage.SelectedValue.ToString();
                    edResult.Text = await MakeRequest_OCR( src, lang );
                    if( !string.IsNullOrEmpty( edResult.Text ) )
                    {
                        tsmiShowWindow.PerformClick();
                        if (ResultHistory.Count >= ResultHistoryLimit) ResultHistory.RemoveAt(0);
                        ResultHistory.Add(new KeyValuePair<string, string>(edResult.Text, Result_Lang));
                    }
                    Clipboard.Clear();

                    pbar.Style = ProgressBarStyle.Blocks;
                    ClipboardChanged = false;
                    btnOCR.Enabled = true;
                }
            }

            if( CFGLOADED && !ApiKey.ContainsKey( "Computer Vision API" ) )
            {
                MessageBox.Show( "Microsoft Azure Cognitive Servise Computer Vision API key is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
            }
        }

        private void btnShowJSON_Click( object sender, EventArgs e )
        {
            if( string.IsNullOrEmpty( Result_JSON.Trim() ) ) return;
            int len = Result_JSON.Length > 512 ? 512 : Result_JSON.Length;
            if( DialogResult.Yes == MessageBox.Show( Result_JSON.Substring( 0, len ), "Copy OCR Result?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification ) )
            {
                Clipboard.SetText( Result_JSON );
            }
        }

        private void btnSpeech_Click( object sender, EventArgs e )
        {
            List<string> lang_cn = new List<string>() { "zh-hans", "zh-cn", "zh" };
            List<string> lang_tw = new List<string>() { "zh-hant", "zh-tw" };
            List<string> lang_jp = new List<string>() { "ja-jp", "ja", "jp" };
            List<string> lang_en = new List<string>() { "en-us", "us", "en" };

            try
            {
                synth.SelectVoice( voice_default );
                string lang = cbLanguage.SelectedValue.ToString();
                if( lang.Equals("unk", StringComparison.CurrentCultureIgnoreCase) ) lang = Result_Lang;

                // Initialize a new instance of the SpeechSynthesizer.
                foreach( InstalledVoice voice in synth.GetInstalledVoices() )
                {
                    VoiceInfo info = voice.VoiceInfo;
                    var vl = info.Culture.IetfLanguageTag;

                    if( lang_cn.Contains( vl.ToLower() ) &&
                        lang.StartsWith( "zh", StringComparison.CurrentCultureIgnoreCase ) &&
                        voice.VoiceInfo.Name.ToLower().Contains( "huihui" ) )
                    {
                        synth.SelectVoice( voice.VoiceInfo.Name );
                        break;
                    }
                    else if( lang_jp.Contains( vl.ToLower() ) &&
                        lang.StartsWith( "ja", StringComparison.CurrentCultureIgnoreCase ) &&
                        voice.VoiceInfo.Name.ToLower().Contains( "haruka" ) )
                    {
                        synth.SelectVoice( voice.VoiceInfo.Name );
                        break;
                    }
                    else if( lang_en.Contains( vl.ToLower() ) &&
                        lang.StartsWith( "en", StringComparison.CurrentCultureIgnoreCase ) &&
                        voice.VoiceInfo.Name.ToLower().Contains( "zira" ) )
                    {
                        synth.SelectVoice( voice.VoiceInfo.Name );
                        break;
                    }
                }

                //synth.Volume = 100;  // 0...100
                //synth.Rate = 0;     // -10...10

                // Synchronous
                //synth.Speak( edResult.Text );
                // Asynchronous
                synth.SpeakAsyncCancelAll();
                synth.Resume();
                synth.SpeakAsync( edResult.Text );
            }
            catch( Exception ex )
            {
                MessageBox.Show( this, ex.Data.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification );
            }
        }

        private void tsmiExit_Click( object sender, EventArgs e )
        {
            if(synth != null)
            {
                synth.Resume();
                synth.SpeakAsyncCancelAll();
            }

            Application.Exit();
        }

        private void tsmiShowWindow_Click( object sender, EventArgs e )
        {
            this.Show();
            if ( this.WindowState == FormWindowState.Minimized )
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
            edResult.SelectAll();
            edResult.Focus();
        }

        private void tsmiTopMost_Click( object sender, EventArgs e )
        {
            this.TopMost = tsmiTopMost.Checked;
        }

        private void tsmiShowLastOCRResultJSON_Click( object sender, EventArgs e )
        {
            btnShowJSON.PerformClick();
        }

        private void tsmiWatchClipboard_Click( object sender, EventArgs e )
        {
            chkAutoClipboard.Checked = tsmiWatchClipboard.Checked;
        }

        private void tsmiSaveState_Click( object sender, EventArgs e )
        {
            //if ( !ApiKey.ContainsKey( "Computer Vision API" ) && edResult.Text.Trim().Length == 32 )
            if ( edResult.Text.Trim().Length == 32 )
            {
                var dlgResult = MessageBox.Show( "Text in result box will be saved as API Key!", "Note", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning );
                if ( dlgResult == DialogResult.OK )
                    ApiKey["Computer Vision API"] = edResult.Text.Trim();
            }
            SaveConfig();
        }

        private void tsmiOpacityValue_Click( object sender, EventArgs e )
        {
            if( sender.GetType() == typeof(ToolStripMenuItem) )
            {
                try
                {
                    var vs = ( sender as ToolStripMenuItem ).Text.Trim(new char[] { '%' });
                    this.Opacity = double.Parse( vs, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat ) / 100d;
                }
                catch ( Exception )
                {
                }
            }
            try
            {
                foreach ( ToolStripMenuItem mi in tsmiOpacity.DropDownItems )
                {
                    if ( mi == sender )
                        mi.Checked = true;
                    else
                        mi.Checked = false;
                }
            }
            catch ( Exception )
            {
            }
        }

        private void tsmiHistory_Click(object sender, EventArgs e)
        {
            foreach(ToolStripMenuItem mi in tsmiHistory.DropDownItems)
            {
                if (sender == mi)
                {
                    mi.Checked = true;
                    edResult.Text = mi.Text;
                    Result_Lang = (string)mi.Tag;
                    tsmiHistory.Tag = mi.Name;
                }
                else mi.Checked = false;
            }
        }

        private void tsmiHistory_DropDownOpening(object sender, EventArgs e)
        {
            tsmiHistory.DropDownItems.Clear();
            foreach (var item in ResultHistory)
            {
                ToolStripMenuItem mi = new ToolStripMenuItem(item.Key);
                mi.Name = $"ResultHistory_{ResultHistory.IndexOf(item)}";
                mi.Text = item.Key;
                mi.Tag = item.Value;
                mi.Click += tsmiHistory_Click;
                tsmiHistory.DropDownItems.Insert(0, mi);
                if ((string)tsmiHistory.Tag == mi.Name) mi.Checked = true;
                if (tsmiHistory.DropDownItems.Count > 25) break;
            }
        }

        private void tsmiTextSpeech_Click(object sender, EventArgs e)
        {
            if (synth == null) return;

            if (sender == tsmiTextPlay)
            {
                btnSpeech.PerformClick();
            }
            else if (sender == tsmiTextPause)
            {
                if (synth.State == SynthesizerState.Paused)
                    synth.Resume();
                else if (synth.State == SynthesizerState.Speaking)
                    synth.Pause();
            }
            else if (sender == tsmiTextStop)
            {
                synth.SpeakAsyncCancelAll();
                synth.Resume();
            }
        }
    }
}
