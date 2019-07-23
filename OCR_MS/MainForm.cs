using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace OCR_MS
{
    public partial class MainForm : Form
    {
        private string AppPath = Path.GetDirectoryName(Application.ExecutablePath);
        private string AppName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);

        private static Dictionary<string, string> ApiKey = new Dictionary<string, string>();

        private static string[] exts_img = new string[] { ".bmp", ".jpg", ".png", ".jpeg", ".tif", ".tiff", ".gif" };
        private static string[] exts_txt = new string[] { ".txt", ".text", ".md", ".htm", ".html", ".rst", ".ini", ".csv", ".mo", ".ssa", ".ass", ".srt" };

        private bool CFGLOADED = false;
        private bool CLOSE_TO_TRAY = false;
        private bool CLIPBOARD_CLEAR = false;
        private bool CLIPBOARD_WATCH = true;

        private bool OCR_HISTORY = false;

        private bool SPEECH_AUTO = false;
        private bool SPEECH_SLOW = false;
        private string SPEECH_TEXT = string.Empty;

        #region OCR with microsoft cognitive api
        private string APIKEYTITLE_CV = "Computer Vision API";
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

        internal async Task<string> MakeRequest_OCR(Bitmap src, string lang = "unk")
        {
            string result = "";
            string ApiKey_CV = ApiKey.ContainsKey( APIKEYTITLE_CV ) ? ApiKey[APIKEYTITLE_CV] : string.Empty;

            if (string.IsNullOrEmpty(ApiKey_CV)) return (result);

            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString( string.Empty );

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey_CV);

            // Request parameters
            queryString["language"] = lang;
            queryString["detectOrientation "] = "true";
            var uri = "https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr?" + queryString;

            HttpResponseMessage response;

            using (Stream png = new MemoryStream())
            {
                src.Save(png, ImageFormat.Png);
                byte[] buffer = ( (MemoryStream) png ).ToArray();
                string buf = "data:image/png;base64," + Convert.ToBase64String( buffer );

                string W_SEP = "";

                // Request body
                using (var content = new ByteArrayContent(buffer))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await client.PostAsync(uri, content);
                    string ocr_result = await response.Content.ReadAsStringAsync();

                    JToken token = JToken.Parse( ocr_result );
                    Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
                    Result_JSON = Result_JSON.Replace("\\\"", "\"");

                    JToken language = token.SelectToken( "$..language" );
                    if (language != null)
                    {
                        Result_Lang = language.ToString().ToLower();
                        if (Result_Lang.StartsWith("zh-") || Result_Lang.StartsWith("ja") || Result_Lang.StartsWith("ko"))
                            W_SEP = "";
                        else W_SEP = " ";
                    }

                    StringBuilder sb = new StringBuilder();
                    IEnumerable<JToken> regions = token.SelectTokens( "$..regions", false );
                    foreach (var region in regions)
                    {
                        List<string> ocr_line = new List<string>();
                        IEnumerable<JToken> lines = region.SelectTokens( "$..lines", false );
                        foreach (var line in lines)
                        {
                            IEnumerable<JToken> words = line.SelectTokens( "$..words", false );
                            foreach (var word in words)
                            {
                                List<string> ocr_word = new List<string>();
                                IEnumerable<JToken> texts = word.SelectTokens( "$..text", false );
                                foreach (var text in texts)
                                {
                                    ocr_word.Add(text.ToString());
                                    //sb.Append( W_SEP + text.ToString() );
                                }
                                sb.AppendLine(string.Join(W_SEP, ocr_word));
                            }
                            sb.AppendLine();
                        }
                        sb.AppendLine();
                    }
                    result = sb.ToString().Trim();
                }
            }
            return (result);
        }
        #endregion

        #region Translate with microsoft cognitive api
        private const string APIKEYTITLE_TT = "Translator Text API";
        internal async Task<string> MakeRequest_Translate(string src, string langDst = "zh-Hans", string langSrc = "")
        {
            string result = "";
            string ApiKey_TT = ApiKey.ContainsKey( APIKEYTITLE_TT ) ? ApiKey[APIKEYTITLE_TT] : string.Empty;
            if (string.IsNullOrEmpty(ApiKey_TT)) return (result);

            var queryString = HttpUtility.ParseQueryString( string.Empty );
            // Request parameters
            queryString["api-version"] = "3.0";
            queryString["textType"] = "html";
            if (!string.IsNullOrEmpty(langSrc)) queryString["from"] = langSrc;
            if (!string.IsNullOrEmpty(langDst)) queryString["to"] = langDst;
            queryString["toScript"] = "Latn";
            //queryString["allowFallback"] = "true";

            // Global      : api.cognitive.microsofttranslator.com
            // North       : America: api-nam.cognitive.microsofttranslator.com
            // Europe      : api-eur.cognitive.microsofttranslator.com
            // Asia Pacific: api-apc.cognitive.microsofttranslator.com
            var uri = $"https://api.cognitive.microsofttranslator.com/translate?" + queryString;

            //var lines = src.Split(new string[] { "\n\r", "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries );
            //var srclines = lines.Select(l => $"{{'Text':'{l.Replace("'", "\\'").Replace("\"", "\\\"")}'}}");            
            //var content = new StringContent($"[{string.Join(",", srclines)}]");
            var content = new StringContent($"[{{'Text':'{src.Replace("'", "\\'").Replace("\"", "\\\"")}'}}]");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var client = new HttpClient();
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey_TT);

            HttpResponseMessage response = await client.PostAsync(uri, content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string translate_result = await response.Content.ReadAsStringAsync();

                JToken token = JToken.Parse( translate_result);
                Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
                Result_JSON = Result_JSON.Replace("\\\"", "\"");

                StringBuilder sb = new StringBuilder();
                IEnumerable<JToken> translations = token.SelectTokens( "$..translations", false );
                foreach (var translation in translations)
                {
                    IEnumerable<JToken> translate_text = translation.SelectTokens( "$..text", false ).First();
                    IEnumerable<JToken> translate_latn = translation.SelectTokens( "$..transliteration.text", false ).First();
                    IEnumerable<JToken> translate_to = translation.SelectTokens( "$..to", false ).First();

                    sb.AppendLine(translate_text.Value<string>());
                }
                result = sb.ToString().Trim();
            }

            return (result);
        }
        private bool TRANSLATING_AUTO = false;
        #endregion

        #region Speech
        private SpeechSynthesizer synth = null;
        private string voice_default = string.Empty;
        #endregion

        #region Monitor Clipboard
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClipboardSequenceNumber();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // WM_DRAWCLIPBOARD message
        private const int WM_DRAWCLIPBOARD   = 0x0308;
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int WM_CHANGECBCHAIN   = 0x030D;
        // Our variable that will hold the value to identify the next window in the clipboard viewer chain
        private IntPtr _clipboardViewerNext;
        private bool ClipboardChanged = false;
        private int lastClipboardSN = 0;
        #endregion

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CHANGECBCHAIN)
            {
                // If the next window is closing, repair the chain. 
                if (m.WParam == _clipboardViewerNext)
                    _clipboardViewerNext = m.LParam;
                // Otherwise, pass the message to the next link. 
                else if (_clipboardViewerNext != IntPtr.Zero)
                    SendMessage(_clipboardViewerNext, m.Msg, m.WParam, m.LParam);
            }

            base.WndProc(ref m);    // Process the message 

            if (!CLIPBOARD_WATCH) return;

            //if ( m.Msg == WM_CLIPBOARDUPDATE )
            if (m.Msg == WM_DRAWCLIPBOARD)
            {
                ClipboardChanged = false;
                var cbsn = GetClipboardSequenceNumber();
                if (lastClipboardSN != cbsn)
                {
                    lastClipboardSN = cbsn;
                    // Clipboard's data
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Bitmap))
                    {
                        // Clipboard image
                        ClipboardChanged = true;
                        btnOCR.PerformClick();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadConfig()
        {
            var cfg = Path.Combine( AppPath, AppName + ".json" );
            if (File.Exists(cfg))
            {
                var json = File.ReadAllText(cfg);
                JToken token = JToken.Parse(json);

                #region API Keys
                IEnumerable<JToken> apis = token.SelectTokens("$..api", false);
                foreach (var api in apis)
                {
                    foreach(var kv in api)
                    {
                        var apikey = kv.SelectTokens("$..key", false).First().ToString();
                        var apiname = kv.SelectTokens("$..name", false).First().ToString();
                        if (apikey != null && apiname != null)
                        {
                            ApiKey[apiname] = apikey;
                        }
                    }
                }
                #endregion

                #region Form Position
                JToken pos = token.SelectToken("$..pos", false);
                if (pos != null)
                {
                    var x = pos.SelectToken("$..x", false).ToString();
                    var y = pos.SelectToken("$..y", false).ToString();
                    if (x != null && y != null)
                    {
                        try
                        {
                            this.Left = Convert.ToInt32(x);
                            this.Top = Convert.ToInt32(y);
                        }
                        catch (Exception)
                        {
                            //throw;
                        }
                    }
                }
                #endregion

                #region Form Size
                JToken size = token.SelectToken("$..size", false);
                if (size != null)
                {
                    var w = size.SelectToken("$..w", false).ToString();
                    var h = size.SelectToken("$..h", false).ToString();
                    if (w != null && h != null)
                    {
                        try
                        {
                            this.Width = Math.Max(this.MinimumSize.Width, Convert.ToInt32(w));
                            this.Height = Math.Max(this.MinimumSize.Height, Convert.ToInt32(h));
                        }
                        catch (Exception)
                        {
                            //throw;
                        }
                    }
                }
                #endregion

                #region Form Opacity
                JToken opacity = token.SelectToken("$..opacity", false);
                if (opacity != null)
                {
                    this.Opacity = Convert.ToDouble(opacity.ToString());
                    string os = $"{Math.Round(this.Opacity * 100, 0)}%";
                    foreach (ToolStripMenuItem mi in tsmiOpacity.DropDownItems)
                    {
                        if (mi.Text == os)
                            mi.Checked = true;
                        else
                            mi.Checked = false;
                    }
                }
                #endregion

                #region Close Form to Notify Tray Area
                JToken tray = token.SelectToken("$..close_to_tray", false);
                if (tray != null)
                {
                    try
                    {
                        CLOSE_TO_TRAY = Convert.ToBoolean(tray);
                        tsmiCloseToTray.Checked = CLOSE_TO_TRAY;
                    }
                    catch (Exception) { }
                }
                #endregion

                #region Clipboard Options
                JToken clipboard = token.SelectToken("$..clipboard", false);
                if (clipboard != null)
                {
                    var clear = clipboard.SelectToken("$..clear", false).ToString();
                    if (clear != null)
                    {
                        try
                        {
                            CLIPBOARD_CLEAR = Convert.ToBoolean(clear);
                            tsmiClearClipboard.Checked = CLIPBOARD_CLEAR;
                        }
                        catch (Exception) { }
                    }
                    var watch = clipboard.SelectToken("$..watch", false).ToString();
                    if (watch != null)
                    {
                        try
                        {
                            CLIPBOARD_WATCH = Convert.ToBoolean(watch);
                            tsmiWatchClipboard.Checked = CLIPBOARD_WATCH;
                            chkAutoClipboard.Checked = CLIPBOARD_WATCH;
                        }
                        catch (Exception) { }
                    }
                }
                #endregion

                #region OCR History
                JToken ocrhist = token.SelectToken("$..ocr.log_history", false);
                if (ocrhist != null)
                {
                    try
                    {
                        OCR_HISTORY = Convert.ToBoolean(ocrhist);

                    }
                    catch (Exception) { }
                }
                tsmiLogOCRHistory.Checked = OCR_HISTORY;
                tsmiHistory.Visible = OCR_HISTORY;
                tsmiHistory.Enabled = OCR_HISTORY;
                tsmiHistoryClear.Visible = OCR_HISTORY;
                tsmiHistoryClear.Enabled = OCR_HISTORY;
                #endregion

                #region Translate Options
                JToken autotrans = token.SelectToken("$..translate.auto_translate", false);
                if (autotrans != null)
                {
                    try
                    {
                        TRANSLATING_AUTO = Convert.ToBoolean(autotrans);
                        tsmiTranslateAuto.Checked = TRANSLATING_AUTO;
                    }
                    catch (Exception) { }
                }
                JToken trans_to = token.SelectToken("$..translate.translate_to", false);
                if (trans_to != null)
                {
                    try
                    {
                        tsmiTranslateDst.Tag = Convert.ToString(trans_to);
                        foreach(var item in tsmiTranslateDst.DropDownItems)
                        {
                            if(item is ToolStripMenuItem)
                            {
                                var tsmi = item as ToolStripMenuItem;
                                if (((string)tsmi.Tag).Equals((string)tsmiTranslateDst.Tag, StringComparison.CurrentCultureIgnoreCase))
                                    tsmi.Checked = true;
                                else tsmi.Checked = false;
                            }
                        }
                    }
                    catch (Exception) { }
                }
                #endregion

                #region Speech Options
                JToken autospeech = token.SelectToken("$..speech.auto_speech", false);
                if (autospeech != null)
                {
                    try
                    {
                        SPEECH_AUTO = Convert.ToBoolean(autospeech);
                        tsmiTextAutoSpeech.Checked = SPEECH_AUTO;
                    }
                    catch (Exception) { }
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
                { "clipboard", new Dictionary<string, bool>()
                    {
                        { "clear", CLIPBOARD_CLEAR },
                        { "watch", CLIPBOARD_WATCH }
                    }
                },
                { "close_to_tray", CLOSE_TO_TRAY },
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
                { "ocr",new Dictionary<string, object>()
                    {
                        { "log_history", OCR_HISTORY },
                    }
                },
                {"translate", new Dictionary<string, object>()
                    {
                        {"auto_translate", tsmiTranslateAuto.Checked },
                        {"translate_to", (string)tsmiTranslateDst.Tag }
                    }
                },
                {"speech",  new Dictionary<string, object>()
                    {
                        {"auto_speech", tsmiTextAutoSpeech.Checked }
                    }
                },
                { "api", ApiKey.Select( o => new Dictionary<string, string>() { { "name", o.Key }, { "key", o.Value } } ).ToList() }
            };

            File.WriteAllText(cfg, JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        private async Task<string> Run_OCR(Bitmap src, string lang = "unk")
        {
            edResult.Text = await MakeRequest_OCR(src, lang);
            if (tsmiTextAutoSpeech.Checked) btnSpeech.PerformClick();
            if (tsmiTranslateAuto.Checked) btnTranslate.PerformClick();
            if (!string.IsNullOrEmpty(edResult.Text))
            {
                tsmiShowWindow.PerformClick();
                if (OCR_HISTORY)
                {
                    if (ResultHistory.Count >= ResultHistoryLimit) ResultHistory.RemoveAt(0);
                    ResultHistory.Add(new KeyValuePair<string, string>(edResult.Text, Result_Lang));
                }
            }
            return (edResult.Text);
        }

        private void Synth_StateChanged(object sender, StateChangedEventArgs e)
        {
            if (synth == null) return;

            if (synth.State == SynthesizerState.Paused)
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

        private double font_size_default = 9;
        private void FontSizeChange(int action, int max = 48, int min = 9)
        {
            var font_old = edResult.Font;
            double font_size = font_old.SizeInPoints;
            if (action < 0)
            {
                font_size -= .5;
                font_size = font_size < min ? min : font_size;
            }
            else if (action == 0)
            {
                font_size = font_size_default;
            }
            else if (action > 0)
            {
                font_size += .5;
                font_size = font_size > max ? max : font_size;
            }
            if (font_size != font_old.SizeInPoints)
            {
                var font_new = new Font(font_old.FontFamily, (float)font_size);
                edResult.Font = font_new;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            synth = new SpeechSynthesizer();
            voice_default = synth.Voice.Name;
            synth.SpeakStarted += Synth_SpeakStarted;
            synth.SpeakProgress += Synth_SpeakProgress;
            synth.StateChanged += Synth_StateChanged;
            synth.SpeakCompleted += Synth_SpeakCompleted;

            notify.Icon = Icon;
            notify.BalloonTipTitle = this.Text;
            notify.BalloonTipText = $"Using \"{APIKEYTITLE_CV}\" OCR feature.";
            notify.Text = this.Text;

            hint.ToolTipTitle = this.Text;

            // Adds our form to the chain of clipboard viewers.
            _clipboardViewerNext = SetClipboardViewer(this.Handle);

            ////init_ocr_lang();
            cbLanguage.Items.Clear();
            cbLanguage.DataSource = new BindingSource(ocr_languages, null);
            cbLanguage.DisplayMember = "Value";
            cbLanguage.ValueMember = "Key";

            tsmiTranslateSrc.DropDownItems.Clear();
            tsmiTranslateDst.DropDownItems.Clear();
            foreach (var kv in ocr_languages)
            {
                var cv = kv.Key.Equals("unk", StringComparison.CurrentCultureIgnoreCase);
                tsmiTranslateSrc.DropDownItems.Add(new ToolStripMenuItem(kv.Value, null, tsmiTranslateLanguage_Click, $"tsmiTranslateSrc_{kv.Key}") { Tag = kv.Key, Checked = cv });
                tsmiTranslateDst.DropDownItems.Add(new ToolStripMenuItem(kv.Value, null, tsmiTranslateLanguage_Click, $"tsmiTranslateDst_{kv.Key}") { Tag = kv.Key, Checked = cv });
            }
            tsmiTranslateSrc.Tag = "unk";
            tsmiTranslateDst.Tag = "unk";

            edResult.AcceptsReturn = true;
            edResult.AcceptsTab = true;
            edResult.AllowDrop = false;
            edResult.MouseWheel += edResult_MouseWheel;
            font_size_default = edResult.Font.SizeInPoints;

            LoadConfig();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && CLOSE_TO_TRAY)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                if (_clipboardViewerNext != IntPtr.Zero)
                    ChangeClipboardChain(this.Handle, _clipboardViewerNext);
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                tsmiSaveState.PerformClick();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                var fmts = e.Data.GetFormats();
#if DEBUG
                Console.WriteLine($"{string.Join(", ", fmts)}");
#endif
                if (fmts.Contains("System.String") ||
                    fmts.Contains("UnicodeText") ||
                    fmts.Contains("Text") ||
                    //fmts.Contains("PNG") ||
                    //fmts.Contains("Bitmap") ||
                    fmts.Contains("FileName"))
                {
                    e.Effect = DragDropEffects.Copy;
                    //edResult.AllowDrop = true;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                    //edResult.AllowDrop = false;
                }
            }
            catch (Exception) { }
        }

        private async void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var fmts = e.Data.GetFormats();
                if (fmts.Contains("System.String"))
                {
                    //if (edResult.AllowDrop)
                    {
                        var text = (string)e.Data.GetData("System.String");
                        var idx = edResult.SelectionStart;
                        edResult.Text = edResult.Text.Insert(idx, text);
                        edResult.SelectionStart = idx;
                    }
                }
                else if (fmts.Contains("UnicodeText"))
                {
                    //if (edResult.AllowDrop)
                    {
                        var text = (string)e.Data.GetData("UnicodeText");
                        var idx = edResult.SelectionStart;
                        edResult.Text = edResult.Text.Insert(idx, text);
                        edResult.SelectionStart = idx;
                    }
                }
                else if (fmts.Contains("Text"))
                {
                    //if (edResult.AllowDrop)
                    {
                        var text = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data.GetData("Text") as string));
                        var idx = edResult.SelectionStart;
                        edResult.Text = edResult.Text.Insert(idx, text);
                        edResult.SelectionStart = idx;
                    }
                }
                else if (fmts.Contains("PNG"))
                {
                    var src = (Bitmap)(e.Data.GetData("PNG"));
                    string lang = cbLanguage.SelectedValue.ToString();
                    edResult.Text = await Run_OCR(src, lang);
                }
                else if (fmts.Contains("Bitmap"))
                {
                    var src = (Bitmap)e.Data.GetData("Bitmap");
                    string lang = cbLanguage.SelectedValue.ToString();
                    edResult.Text = await Run_OCR(src, lang);
                }
                else if (fmts.Contains("FileName"))
                {
                    try
                    {
                        btnOCR.Enabled = false;
                        pbar.Style = ProgressBarStyle.Marquee;

                        var fns = (string[])e.Data.GetData("FileName");
                        if (fns.Length > 0)
                        {
                            foreach (var fn in fns)
                            {
                                var ext = Path.GetExtension(fn).ToLower();

                                StringBuilder sb = new StringBuilder();
                                if (exts_img.Contains(ext))
                                {
                                    try
                                    {
                                        string[] lines = File.ReadAllLines(fn);
                                        foreach(var l in lines)
                                        {
                                            sb.AppendLine(l);
                                        }
                                    }
                                    catch (Exception) { }
                                }
                                else if (exts_img.Contains(ext))
                                {
                                    try
                                    {
                                        using (Bitmap src = (Bitmap)Image.FromFile(fn))
                                        {
                                            string lang = cbLanguage.SelectedValue.ToString();
                                            sb.AppendLine(await Run_OCR(src, lang));
                                            src.Dispose();
                                        }
                                    }
                                    catch (Exception) { }
                                }

                                var idx = edResult.SelectionStart;
                                edResult.Text = edResult.Text.Insert(idx, sb.ToString());
                                edResult.SelectionStart = idx;
                            }
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        pbar.Style = ProgressBarStyle.Blocks;
                        btnOCR.Enabled = true;
                    }
                }
            }
            catch (Exception) { }
        }

        private void notify_Click(object sender, EventArgs e)
        {
            // Show() method can not autoclosed like system contextmenu's behaving
            //notifyMenu.Show( this, Control.MousePosition );

            MethodInfo mi = typeof( NotifyIcon ).GetMethod( "ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic );
            mi.Invoke(notify, null);
        }

        private void notify_MouseClick(object sender, MouseEventArgs e)
        {
            //notifyMenu.Show( this, Cursor.Position.X, Cursor.Position.Y );
        }

        private void notify_DoubleClick(object sender, EventArgs e)
        {
            tsmiShowWindow.PerformClick();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (chkAutoClipboard.Checked && ClipboardChanged)
            {
                btnOCR.PerformClick();
            }
        }

        private void cbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            SPEECH_SLOW = true;
        }

        private void edResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.OemMinus)
            {
                FontSizeChange(-1);
            }
            else if (e.Control && e.KeyCode == Keys.Oemplus)
            {
                FontSizeChange(+1);
            }
            else if (e.Control && e.KeyCode == Keys.D0)
            {
                FontSizeChange(0);
            }
        }

        private void edResult_KeyUp(object sender, KeyEventArgs e)
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

        private void edResult_MouseMove(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control && e.Button == MouseButtons.Left && edResult.SelectionLength > 0)
            {
                edResult.DoDragDrop(edResult.SelectedText, DragDropEffects.Copy);
            }
        }

        private void edResult_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                FontSizeChange(e.Delta);
            }
        }

        private async void btnOCR_Click(object sender, EventArgs e)
        {
            if (ApiKey.ContainsKey(APIKEYTITLE_CV) && btnOCR.Enabled)
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Bitmap))
                {
                    try
                    {
                        btnOCR.Enabled = false;
                        pbar.Style = ProgressBarStyle.Marquee;

                        //(Bitmap) iData.GetData( DataFormats.Bitmap );
                        Bitmap src = (Bitmap)Clipboard.GetImage();
                        string lang = cbLanguage.SelectedValue.ToString();
                        await Run_OCR(src, lang);
                        if (CLIPBOARD_CLEAR && !string.IsNullOrEmpty(edResult.Text)) Clipboard.Clear();
                    }
                    catch (Exception) { }
                    finally
                    {
                        ClipboardChanged = false;
                        pbar.Style = ProgressBarStyle.Blocks;
                        btnOCR.Enabled = true;
                    }
                }
            }

            if (CFGLOADED && !ApiKey.ContainsKey(APIKEYTITLE_CV))
            {
                MessageBox.Show("Microsoft Azure Cognitive Servise Computer Vision API key is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            edResult.Focus();
        }

        private void btnShowJSON_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Result_JSON.Trim())) return;
            int len = Result_JSON.Length > 512 ? 512 : Result_JSON.Length;
            if (DialogResult.Yes == MessageBox.Show(Result_JSON.Substring(0, len), "Copy OCR Result?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification))
            {
                Clipboard.SetText(Result_JSON);
            }
        }

        private void btnSpeech_Click(object sender, EventArgs e)
        {
            List<string> lang_cn = new List<string>() { "zh-hans", "zh-cn", "zh" };
            List<string> lang_tw = new List<string>() { "zh-hant", "zh-tw" };
            List<string> lang_jp = new List<string>() { "ja-jp", "ja", "jp" };
            List<string> lang_en = new List<string>() { "en-us", "us", "en" };

            try
            {
                string text = edResult.SelectionLength > 0 ? edResult.SelectedText : edResult.Text;

                synth.SelectVoice(voice_default);
                string lang = cbLanguage.SelectedValue.ToString();
                if (lang.Equals("unk", StringComparison.CurrentCultureIgnoreCase))
                {
                    lang = Result_Lang;
                    //
                    // 中文：[\u4e00-\u9fcc, \u3400-\u4db5, \u20000-\u2a6d6, \u2a700-\u2b734, \u2b740-\u2b81d, \uf900-\ufad9, \u2f800-\u2fa1d]
                    // 日文：[\u0800-\u4e00] [\u3041-\u31ff]
                    // 韩文：[\uac00-\ud7ff]
                    //
                    //var m_jp = Regex.Matches(text, @"([\u0800-\u4e00])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    //var m_zh = Regex.Matches(text, @"([\u4e00-\u9fbb])", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    if (Regex.Matches(text, @"[\u3041-\u31ff]", RegexOptions.Multiline).Count > 0)
                    {
                        lang = "ja";
                    }
                    else if (Regex.Matches(text, @"[\u4e00-\u9fbb]", RegexOptions.Multiline).Count > 0)
                    {
                        lang = "zh";
                    }
                }

                // Initialize a new instance of the SpeechSynthesizer.
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    var vl = info.Culture.IetfLanguageTag;

                    if (lang_cn.Contains(vl.ToLower()) &&
                        lang.StartsWith("zh", StringComparison.CurrentCultureIgnoreCase) &&
                        voice.VoiceInfo.Name.ToLower().Contains("huihui"))
                    {
                        synth.SelectVoice(voice.VoiceInfo.Name);
                        break;
                    }
                    else if (lang_jp.Contains(vl.ToLower()) &&
                        lang.StartsWith("ja", StringComparison.CurrentCultureIgnoreCase) &&
                        voice.VoiceInfo.Name.ToLower().Contains("haruka"))
                    {
                        synth.SelectVoice(voice.VoiceInfo.Name);
                        break;
                    }
                    else if (lang_en.Contains(vl.ToLower()) &&
                        lang.StartsWith("en", StringComparison.CurrentCultureIgnoreCase) &&
                        voice.VoiceInfo.Name.ToLower().Contains("zira"))
                    {
                        synth.SelectVoice(voice.VoiceInfo.Name);
                        break;
                    }
                }

                //synth.Volume = 100;  // 0...100
                //synth.Rate = 0;     // -10...10
                if (text.Equals(SPEECH_TEXT, StringComparison.CurrentCultureIgnoreCase))
                    SPEECH_SLOW = !SPEECH_SLOW;
                else
                    SPEECH_SLOW = false;

                if (SPEECH_SLOW) synth.Rate = -5;
                else synth.Rate = 0;


                // Synchronous
                //synth.Speak( text );
                // Asynchronous
                synth.SpeakAsyncCancelAll();
                synth.Resume();
                synth.SpeakAsync(text);
                SPEECH_TEXT = text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Data.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            edResult.Focus();
        }

        private async void btnTranslate_Click(object sender, EventArgs e)
        {
            if (ApiKey.ContainsKey(APIKEYTITLE_TT) && !string.IsNullOrEmpty(edResult.Text))
            {
                try
                {
                    pbar.Style = ProgressBarStyle.Marquee;

                    string lang = cbLanguage.SelectedValue.ToString();
                    var langSrc = Result_Lang.Equals("unk", StringComparison.CurrentCultureIgnoreCase) ? string.Empty : Result_Lang;
                    var langDst = string.Empty;
                    var ls = (string)tsmiTranslateSrc.Tag;
                    var ld = (string)tsmiTranslateDst.Tag;
                    if (!ls.Equals("unk", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(ls))
                        langSrc = ls;
                    //else langSrc = string.Empty;
                    if (string.IsNullOrEmpty(langSrc) && !lang.Equals("unk", StringComparison.CurrentCultureIgnoreCase)) langSrc = lang;

                    if (!ld.Equals("unk", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(ld))
                        langDst = ld;
                    else
                    {
                        var cl = CultureInfo.CurrentCulture;
                        langDst = cl.Parent.IetfLanguageTag;
                    }

                    string text = edResult.SelectionLength > 0 ? edResult.SelectedText : edResult.Text;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(edResult.Text.Trim());
                    sb.AppendLine();
                    sb.AppendLine(await MakeRequest_Translate(text, langDst, langSrc));
                    edResult.Text = sb.ToString();
                }
                catch (Exception) { }
                finally
                {
                    pbar.Style = ProgressBarStyle.Blocks;
                }
            }

            if (CFGLOADED && !ApiKey.ContainsKey(APIKEYTITLE_TT))
            {
                MessageBox.Show($"Microsoft Azure Cognitive Servise {APIKEYTITLE_TT} key is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            edResult.Focus();
        }

        private void chkAutoClipboard_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == tsmiWatchClipboard)
            {
                CLIPBOARD_WATCH = (sender as ToolStripMenuItem).Checked;
                chkAutoClipboard.Checked = CLIPBOARD_WATCH;
            }
            else if (sender == chkAutoClipboard)
            {
                CLIPBOARD_WATCH = (sender as CheckBox).Checked;
                tsmiWatchClipboard.Checked = CLIPBOARD_WATCH;
            }
            else if (sender == tsmiClearClipboard)
            {
                CLIPBOARD_CLEAR = (sender as ToolStripMenuItem).Checked;
                //tsmiClearClipboard.Checked = CLIPBOARD_CLEAR;
            }

            if (_clipboardViewerNext != IntPtr.Zero)
                ChangeClipboardChain(this.Handle, _clipboardViewerNext);
            if (CLIPBOARD_WATCH)
                _clipboardViewerNext = SetClipboardViewer(this.Handle);
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            if (synth != null)
            {
                synth.Resume();
                synth.SpeakAsyncCancelAll();
            }

            Application.Exit();
        }

        private void tsmiShowWindow_Click(object sender, EventArgs e)
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
            edResult.SelectAll();
            edResult.Focus();
        }

        private void tsmiTopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = tsmiTopMost.Checked;
        }

        private void tsmiShowLastOCRResultJSON_Click(object sender, EventArgs e)
        {
            btnShowJSON.PerformClick();
        }

        private void tsmiClearClipboard_Click(object sender, EventArgs e)
        {
            CLIPBOARD_CLEAR = tsmiClearClipboard.Checked;
        }

        private void tsmiSaveState_Click(object sender, EventArgs e)
        {
            //if ( !ApiKey.ContainsKey( "Computer Vision API" ) && edResult.Text.Trim().Length == 32 )
            if (edResult.Text.Trim().Length == 32)
            {
                var dlgResult = MessageBox.Show( $"Text in result box will be saved as {APIKEYTITLE_CV} Key!", "Note", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning );
                if (dlgResult == DialogResult.OK)
                    ApiKey[APIKEYTITLE_CV] = edResult.Text.Trim();
            }
            SaveConfig();
        }

        private void tsmiOpacityValue_Click(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(ToolStripMenuItem))
            {
                try
                {
                    var vs = ( sender as ToolStripMenuItem ).Text.Trim(new char[] { '%' });
                    this.Opacity = double.Parse(vs, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat) / 100d;
                }
                catch (Exception)
                {
                }
            }
            try
            {
                foreach (ToolStripMenuItem mi in tsmiOpacity.DropDownItems)
                {
                    if (mi == sender)
                        mi.Checked = true;
                    else
                        mi.Checked = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private void tsmiHistory_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem mi in tsmiHistory.DropDownItems)
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
            if (OCR_HISTORY)
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
        }

        private void tsmiHistoryClear_Click(object sender, EventArgs e)
        {
            ResultHistory.Clear();
            tsmiHistory.DropDownItems.Clear();
        }

        private void tsmiCloseToTray_CheckedChanged(object sender, EventArgs e)
        {
            CLOSE_TO_TRAY = tsmiCloseToTray.Checked;
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

        private void tsmiTranslate_Click(object sender, EventArgs e)
        {
            btnTranslate.PerformClick();
        }

        private void tsmiOptions_Click(object sender, EventArgs e)
        {
            OptionsForm opt = new OptionsForm() {
                Icon = Icon,
                APIKEYTITLE_CV = APIKEYTITLE_CV,
                APIKEYTITLE_TT = APIKEYTITLE_TT,
                APIKEY_CV = ApiKey.ContainsKey(APIKEYTITLE_CV) ? ApiKey[APIKEYTITLE_CV] : string.Empty,
                APIKEY_TT = ApiKey.ContainsKey(APIKEYTITLE_TT) ? ApiKey[APIKEYTITLE_TT] : string.Empty
            };
            
            if(opt.ShowDialog() == DialogResult.OK)
            {
                ApiKey[APIKEYTITLE_CV] = opt.APIKEY_CV;
                ApiKey[APIKEYTITLE_TT] = opt.APIKEY_TT;
                SaveConfig();
            }
            opt.Dispose();
        }

        private void tsmiTranslateLanguage_Click(object sender, EventArgs e)
        {
            if(sender is ToolStripMenuItem)
            {
                var obj = sender as ToolStripMenuItem;
                if(obj.Name.StartsWith("tsmiTranslateSrc_", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var item in tsmiTranslateSrc.DropDownItems)
                    {
                        if(item is ToolStripMenuItem)
                        {
                            var tsmi = item as ToolStripMenuItem;
                            if (tsmi == sender) tsmi.Checked = true;
                            else tsmi.Checked = false;
                        }                        
                    }
                    tsmiTranslateSrc.Tag = obj.Tag;
                }
                else if (obj.Name.StartsWith("tsmiTranslateDst_", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var item in tsmiTranslateDst.DropDownItems)
                    {
                        if (item is ToolStripMenuItem)
                        {
                            var tsmi = item as ToolStripMenuItem;
                            if (tsmi == sender) tsmi.Checked = true;
                            else tsmi.Checked = false;
                        }
                    }
                    tsmiTranslateDst.Tag = obj.Tag;
                }
            }
        }

        private void tsmiLogOCRHistory_CheckedChanged(object sender, EventArgs e)
        {
            OCR_HISTORY = tsmiLogOCRHistory.Checked;
            tsmiHistory.Visible = OCR_HISTORY;
            tsmiHistory.Enabled = OCR_HISTORY;
            tsmiHistoryClear.Visible = OCR_HISTORY;
            tsmiHistoryClear.Enabled = OCR_HISTORY;
        }

        private void tsmiTranslateAuto_CheckedChanged(object sender, EventArgs e)
        {
            TRANSLATING_AUTO = tsmiTranslateAuto.Checked;
        }

        private void tsmiTextAutoSpeech_CheckedChanged(object sender, EventArgs e)
        {
            SPEECH_AUTO = tsmiTextAutoSpeech.Checked;
        }

    }
}
