using GI.Screenshot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        private string AppConfigFile { get; set; } = $"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}.json";

        private static string[] exts_img = new string[] { ".bmp", ".jpg", ".png", ".jpeg", ".tif", ".tiff", ".gif" };
        private static string[] exts_txt = new string[] { ".txt", ".text", ".md", ".htm", ".html", ".rst", ".ini", ".csv", ".mo", ".ssa", ".ass", ".srt" };
        private static string[] split_symbol = new string[] { Environment.NewLine, "\n\r", "\r\n", "\r", "\n", "<br/>", "<br />", "<br>", "</br>" };

        private InputLanguage CurrentInputLanguage { get; set; } = InputLanguage.DefaultInputLanguage;

        private bool CFGLOADED = false;
        private bool ALWAYS_ON_TOP = false;
        private bool CLOSE_TO_TRAY = false;
        private bool CLIPBOARD_CLEAR = false;
        private bool CLIPBOARD_WATCH = true;

        private bool OCR_HISTORY = false;

        private const string API_TITLE_CV = "Computer Vision API";
        private const string API_TITLE_TT = "Translator Text API";

        private string GetLanguageFrom(string lang = "")
        {
            var lang_src = string.IsNullOrEmpty(lang) ? "unk" : lang;
            try
            {
                if (ModifierKeys == Keys.Control && !string.IsNullOrEmpty((string)tsmiTranslateSrc.Tag))
                    lang_src = (string)tsmiTranslateSrc.Tag;
                else if (ModifierKeys == Keys.Shift)
                    lang_src = cbLanguage.SelectedValue.ToString();
                else if (ModifierKeys == Keys.Alt)
                    lang_src = "unk";
                else if (ModifierKeys == Keys.None)
                    lang_src = string.IsNullOrEmpty(Result_Lang) ? "unk" : Result_Lang;
            }
            catch { }
            return (lang_src.ToLower());
        }

        private string GetLangiageTo()
        {
            string lang_dst = "zh-hans";

            var ld = (string)tsmiTranslateDst.Tag;
            if (!ld.Equals("unk", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(ld))
                lang_dst = ld;
            else
            {
                var cl = CultureInfo.CurrentCulture;
                lang_dst = cl.Parent.IetfLanguageTag;
            }

            return (lang_dst.ToLower());
        }

        #region OCR Auto Correction Table
        private string CorrectionDictFile { get; set; } = "ocr_correction.json";
        private string CorrectionDictEditor { get; set; } = string.Empty;
        private CorrectionDicts AutoCorrections { get; set; } = new CorrectionDicts();
        private void LoadCorrectionDictionary()
        {
            var file = Path.Combine(AppPath, CorrectionDictFile);
            if (File.Exists(file))
            {
                if (AutoCorrections == null) AutoCorrections = new CorrectionDicts();
                else AutoCorrections.Clear();
                try
                {
                    var json = File.ReadAllText(file);
                    AutoCorrections = JsonConvert.DeserializeObject<CorrectionDicts>(json);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadCorrectionDictionary => {ex.Message}"); }
            }
            else SaveCorrectionDictionary();
        }

        private void SaveCorrectionDictionary()
        {
            var file = Path.Combine(AppPath, CorrectionDictFile);
            if (AutoCorrections is CorrectionDicts)
            {
                try
                {
                    foreach (var lang in azure_languages)
                    {
                        if (!AutoCorrections.Dictionaries.ContainsKey(lang.Key))
                            AutoCorrections.Dictionaries.Add(lang.Key, new CorrectionDict() { Language = lang.Key, Description = lang.Value });
                    }
                    foreach (var lang in baidu_languages)
                    {
                        if (!AutoCorrections.Dictionaries.ContainsKey(lang.Key))
                            AutoCorrections.Dictionaries.Add(lang.Key, new CorrectionDict() { Language = lang.Key, Description = lang.Value });
                    }

                    var json = JsonConvert.SerializeObject(AutoCorrections, Formatting.Indented);
                    File.WriteAllText(file, json);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SaveCorrectionDictionary => {ex.Message}"); }
            }
        }

        private string AutoCorrecting(string text, string lang)
        {
            string result = text;

            try
            {
                if (AutoCorrections is CorrectionDicts && !string.IsNullOrEmpty(lang) && AutoCorrections.Dictionaries.ContainsKey(lang))
                {
                    var dict = AutoCorrections.Dictionaries[lang].Words;
                    if (dict is OrderedDictionary)
                    {
                        foreach (DictionaryEntry entry in dict)
                        {
                            var k = entry.Key is string ? (entry.Key as string).Trim() : string.Empty;
                            var v = entry.Value is string ? (entry.Value as string).Trim() : string.Empty;
                            result = Regex.Replace(result, k, v, RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"AutoCorrecting => {ex.Message}"); }

            return (result);
        }
        #endregion

        #region OCR with microsoft cognitive api
        private static Dictionary<string, AzureAPI> AzureApi = new Dictionary<string, AzureAPI>();

        internal Dictionary<string, string> azure_languages = new Dictionary<string, string>() {
            {"unk","AutoDetect"},
            {"zh-Hans","ChineseSimplified"}, {"zh-Hant","ChineseTraditional"},
            {"cs","Czech"}, {"da","Danish"}, {"nl","Dutch"}, {"en","English"}, {"fi","Finnish"}, {"fr","French"}, {"de","German"},
            {"el","Greek"}, {"hu","Hungarian"}, {"it","Italian"}, {"ja","Japanese"}, {"ko","Korean"}, {"nb","Norwegian"}, {"pl","Polish"},
            {"pt","Portuguese"}, {"ru","Russian"}, {"es","Spanish"}, {"sv","Swedish"}, {"tr","Turkish"}, {"ar","Arabic"}, {"ro","Romanian"},
            {"sr-Cyrl","SerbianCyrillic"}, {"sr-Latn","SerbianLatin"}, {"sk","Slovak"},
            {"th", "Tailand" }, {"yue", "粤语" }, {"wyw", "文言文" },
        };

        internal string Result_JSON = string.Empty;
        internal string Result_Lang = string.Empty;
        internal static int ResultHistoryLimit = 100;
        internal List<KeyValuePair<string, string>> ResultHistory = new List<KeyValuePair<string, string>>(ResultHistoryLimit);

        internal async Task<string> MakeRequest_Azure_OCR(Image src, string lang = "unk")
        {
            string result = "";
            string ApiKey_CV = AzureApi.ContainsKey(API_TITLE_CV) ? AzureApi[API_TITLE_CV].ApiKey : string.Empty;
            if (string.IsNullOrEmpty(ApiKey_CV)) return (result);
            string ApiEndpoint_CV = AzureApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(AzureApi[API_TITLE_CV].EndPoint) ? AzureApi[API_TITLE_CV].EndPoint : "https://westus.api.cognitive.microsoft.com/";

            if (!AzureApi.ContainsKey(API_TITLE_CV))
                AzureApi.Add(API_TITLE_CV, new AzureAPI() { ApiKey = ApiKey_CV, EndPoint = ApiEndpoint_CV });
            else if (string.IsNullOrEmpty(AzureApi[API_TITLE_CV].EndPoint))
                AzureApi[API_TITLE_CV].EndPoint = ApiEndpoint_CV;

            if (string.IsNullOrEmpty(ApiKey_CV)) return (result);

            var queryString = HttpUtility.ParseQueryString( string.Empty );
            // Request parameters
            queryString["language"] = lang;
            queryString["detectOrientation"] = "true";
            var uri = $"{ApiEndpoint_CV}/vision/v3.1/ocr?" + queryString;
            uri = uri.Replace("//vision", "/vision");

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (Stream png = new MemoryStream())
            {
                src.Save(png, ImageFormat.Png);
                byte[] buffer = ( (MemoryStream) png ).ToArray();
                //string buf = "data:image/png;base64," + Convert.ToBase64String( buffer );

                string W_SEP = "";

                // Request body
                HttpClient client = new HttpClient();
                // Request headers
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey_CV);
                //client.DefaultRequestHeaders.Add("Accept", "*/*");
                //client.DefaultRequestHeaders.Add("User-Agent", "curl/7.55.1");

                using (var content = new ByteArrayContent(buffer))
                {
                    string ocr_result = string.Empty;
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    try
                    {
                        HttpResponseMessage response = await client.PostAsync(uri, content);
                        ocr_result = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception ex) { result = ex.Message; Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace); }
                    if (string.IsNullOrEmpty(ocr_result)) return ($"ERROR:{result}");

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
                                var boxes = word.SelectTokens("$..boundingBox", false ).ToList();

                                int boxIndex = 0;
                                int lastBoundingBoxRight = -1;
                                char lastTail = ' ';
                                double lastSpaceWidth = .0F;
                                foreach (var text in texts)
                                {
                                    var t = text.ToString();
                                    if (t.Length <= 0) continue;

                                    // Calc space for insert whitespace to text header
                                    var prefix = string.Empty;
                                    if (string.IsNullOrEmpty(W_SEP))
                                    {
                                        var boxValue = boxes[boxIndex].ToString().Split(',').Select(x => int.Parse(x)).ToArray();
                                        var boxLeft = boxValue[0];
                                        var boxWidth = boxValue[2];
                                        var spaceWidth = boxWidth / t.Length * 0.33;
                                        if (boxIndex == 0)
                                            lastBoundingBoxRight = boxLeft + boxWidth;
                                        else
                                        {
                                            var c = t.First();
                                            if (char.IsPunctuation(c)) prefix = string.Empty;
                                            else if (char.IsSymbol(c)) prefix = string.Empty;
                                            else if (char.IsPunctuation(lastTail)) prefix = string.Empty;
                                            else if (char.IsSymbol(lastTail)) prefix = string.Empty;
                                            else if (char.IsWhiteSpace(lastTail)) prefix = string.Empty;
                                            else if (boxLeft - lastBoundingBoxRight > Math.Max(lastSpaceWidth, spaceWidth)) prefix = " ";
                                        }
                                        lastBoundingBoxRight = boxLeft + boxWidth;
                                        lastTail = t.Last();
                                        lastSpaceWidth = spaceWidth;
                                        boxIndex++;
                                    }
                                    ocr_word.Add($"{prefix}{t}");
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

        internal async Task<string> Run_Azure_OCR(Image src, string lang = "unk")
        {
            var result = string.Empty;
            if (AzureApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(AzureApi[API_TITLE_CV].ApiKey))
            {
                try
                {
                    result = await MakeRequest_Azure_OCR(src, lang);
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
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
            if (CFGLOADED && (!AzureApi.ContainsKey(API_TITLE_CV) || string.IsNullOrEmpty(AzureApi[API_TITLE_CV].ApiKey)))
            {
                MessageBox.Show($"Microsoft Azure Cognitive Servise {API_TITLE_CV} key is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return (result);
        }
        #endregion

        #region Translate with microsoft cognitive api
        internal async Task<string> MakeRequest_Azure_Translate(string src, string langDst = "zh-Hans", string langSrc = "")
        {
            string result = "";
            string ApiKey_TT = AzureApi.ContainsKey(API_TITLE_TT) ? AzureApi[API_TITLE_TT].ApiKey : string.Empty;
            if (string.IsNullOrEmpty(ApiKey_TT)) return (result);
            string ApiEndpoint_TT = AzureApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(AzureApi[API_TITLE_TT].EndPoint) ? AzureApi[API_TITLE_TT].EndPoint : "https://api.cognitive.microsofttranslator.com/";

            if (!AzureApi.ContainsKey(API_TITLE_TT))
                AzureApi.Add(API_TITLE_TT, new AzureAPI() { ApiKey = ApiKey_TT, EndPoint = ApiEndpoint_TT });
            else if (string.IsNullOrEmpty(AzureApi[API_TITLE_CV].EndPoint))
                AzureApi[API_TITLE_TT].EndPoint = ApiEndpoint_TT;

            var queryString = HttpUtility.ParseQueryString( string.Empty );
            // Request parameters
            queryString["api-version"] = "3.0";
            queryString["textType"] = "plain"; //"html"
            if (!(string.IsNullOrEmpty(langSrc) || langSrc.Equals("unk", StringComparison.CurrentCultureIgnoreCase))) queryString["from"] = langSrc;
            if (!string.IsNullOrEmpty(langDst)) queryString["to"] = langDst;
            queryString["toScript"] = "Latn";
            //queryString["allowFallback"] = "true";

            // Global      : api.cognitive.microsofttranslator.com
            // North       : America: api-nam.cognitive.microsofttranslator.com
            // Europe      : api-eur.cognitive.microsofttranslator.com
            // Asia Pacific: api-apc.cognitive.microsofttranslator.com
            //var uri = $"https://api.cognitive.microsofttranslator.com/translate?" + queryString;
            var uri = $"{ApiEndpoint_TT}/translate?" + queryString;
            uri = uri.Replace("//translate", "/translate");

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Content = new StringContent($"[{{'Text':'{src.Replace("'", "\\'").Replace("\"", "\\\"")}'}}]", Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey_TT);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", "global");

                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string translate_result = await response.Content.ReadAsStringAsync();

                        JToken token = JToken.Parse(translate_result);
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
                }
            }

            return (result);
        }
        private bool TRANSLATING_AUTO = false;

        internal async Task<string> Run_Azure_Translate(string src, string from = "")
        {
            string result = string.Empty;
            if (AzureApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(AzureApi[API_TITLE_TT].ApiKey) && !string.IsNullOrEmpty(edResult.Text))
            {
                try
                {
                    pbar.Style = ProgressBarStyle.Marquee;

                    var langSrc = GetLanguageFrom(from);
                    var langDst = GetLangiageTo();

                    result = await MakeRequest_Azure_Translate(src, langDst, langSrc);
                }
                catch (Exception) { }
                finally
                {
                    pbar.Style = ProgressBarStyle.Blocks;
                }
            }
            return (result.Trim());
        }
        #endregion

        #region Baidu OCR/Translating
        private static Dictionary<string, BaiduAPI> BaiduApi = new Dictionary<string, BaiduAPI>();
        internal Dictionary<string, string> baidu_languages = new Dictionary<string, string>() {
            { "unk", "auto" },
            { "zh-hans", "zh" }, { "zh-hant", "zh" }, {"yue", "yue" }, {"wyw", "wyw" },
            { "en", "en" }, { "ja", "jp" }, { "ko", "kor" }, { "fr", "fra" }, { "es", "spa" }, { "th", "th" },
            { "ar", "ara" }, { "ru", "ru" }, { "pt", "pt" }, { "de", "de" }, { "it", "it" }, { "el", "el" }, { "nl", "nl" }, { "pl", "pl" }
        };

        internal Dictionary<string, string> baidu_languages_tt = new Dictionary<string, string>() {
            //{ "unk", "auto_detect" }, { "auto", "auto_detect" }, 
            { "unk", "CHN_ENG" }, { "auto", "CHN_ENG" }, { "auto_detect", "CHN_ENG" },
            { "zh", "CHN_ENG" }, { "zh-hans", "CHN_ENG" }, { "zh-hant", "CHN_ENG" },
            { "en", "ENG" }, { "ja", "JAP" }, { "jp", "JAP" }, { "ko", "KOR" }, { "kor", "KOR" },
            { "fr", "FRE" }, { "fra", "FRE" }, { "es", "SPA" }, { "spa", "SPA" }, { "po", "POR" }, { "por", "POR" },
            { "it", "ITA" }, { "ita", "ITA" }, { "ru", "RUS" }, { "rus", "RUS" }, { "da", "DAN" }, { "dan", "DAN" },
            { "nl", "DUT" }, { "dut", "DUT" }, { "sv", "SWE" }, { "swe", "SWE" }, { "pl", "POL" }, { "pol", "POL"},
            { "ar", "ara" }, { "pt", "pt" }, { "de", "de" }, { "el", "el" },
        };

        internal async Task<string> Run_Baidu_OCR(Image src, string lang = "unk")
        {
            string result = string.Empty;

            var ran = new Random(65536);
            var salt = ran.Next(32768, 65536);
            var tokenURL = BaiduApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_CV].TokenURL) ? BaiduApi[API_TITLE_CV].TokenURL : "https://aip.baidubce.com/oauth/2.0/token";
            var apiURL = BaiduApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_CV].EndPoint) ? BaiduApi[API_TITLE_CV].EndPoint : "https://aip.baidubce.com/rest/2.0/ocr/v1/accurate_basic";
            var appID = BaiduApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_CV].AppId) ? BaiduApi[API_TITLE_CV].AppId : string.Empty;
            var apiKEY = BaiduApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_CV].AppKey) ? BaiduApi[API_TITLE_CV].AppKey : string.Empty;
            var appKEY = BaiduApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_CV].SecretKey) ? BaiduApi[API_TITLE_CV].SecretKey : string.Empty;
            if (string.IsNullOrEmpty(appID) || string.IsNullOrEmpty(apiKEY) || string.IsNullOrEmpty(appKEY)) return (result);

            var md5hash = MD5.Create();
            var signs = md5hash.ComputeHash(Encoding.UTF8.GetBytes($"{appID}{src}{salt}{appKEY}"));
            var sign = string.Join("", signs.Select(v => v.ToString("x2")));

            var lang_src = GetLanguageFrom(lang);
            lang_src = baidu_languages_tt.ContainsKey(lang_src) ? baidu_languages_tt[lang_src] : "CHN_ENG";

            var param_token = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_id", apiKEY },
                { "client_secret", appKEY }
            };

            using (var client = new HttpClient())
            {
                var access_token = string.Empty;
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, tokenURL))
                {
                    request.Content = new FormUrlEncodedContent(param_token);
                    using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var token_result = await response.Content.ReadAsStringAsync();
                            JToken token = JToken.Parse(token_result);
                            Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
                            Result_JSON = Result_JSON.Replace("\\\"", "\"");

                            JToken oauths = token.SelectToken("$", false);
                            var o_access_token = oauths.SelectToken("$.access_token");
                            var o_scope = oauths.SelectToken("$.scope");
                            var o_expires_in = oauths.SelectToken("$.expires_in");
                            var o_refresh_token = oauths.SelectToken("$.refresh_token");
                            var o_session_key = oauths.SelectToken("$.session_key");
                            var o_session_secret = oauths.SelectToken("$.session_secret");

                            var scopes = o_scope.Value<string>().Split().ToList();
                            if (scopes.IndexOf("brain_all_scope") >= 0 || scopes.IndexOf("brain_ocr_scope") >= 0)
                            {
                                access_token = o_access_token.Value<string>();
                            }

                        }
                    }
                }
                if (string.IsNullOrEmpty(access_token)) return (result);

                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{apiURL}?access_token={access_token}"))
                {
                    using (MemoryStream png = new MemoryStream())
                    {
                        src.Save(png, ImageFormat.Png);

                        byte[] buffer_png = png.ToArray();
                        char[] buffer_b64 = new char[buffer_png.Length*2];
                        int ret = Convert.ToBase64CharArray(buffer_png, 0, buffer_png.Length, buffer_b64, 0, Base64FormattingOptions.None);
                        byte[] content_b64 = buffer_b64.Take(ret).Select(c => Convert.ToByte(c)).ToArray();

                        var param = new Dictionary<string, string>()
                        {
                            { "image", Encoding.Default.GetString(content_b64) },
                            { "detect_direction", "true" },
                            { "probability", "true" },
                            { "paragraph", "true" },
                            { "language_type", lang_src },
                        };
                        request.Content = new FormUrlEncodedContent(param);
                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                var ocr_result = await response.Content.ReadAsStringAsync();

                                JToken token = JToken.Parse(ocr_result);
                                Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
                                Result_JSON = Result_JSON.Replace("\\\"", "\"");

                                JToken words_list = token.SelectToken("$.words_result", false);
                                if (words_list != null)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (var words_item in words_list.Children())
                                    {
                                        JToken words = words_item.SelectToken("$.words", false);
                                        JToken words_probability = words_item.SelectToken("$.probability", false);
                                        JToken words_location = words_item.SelectToken("$.location", false);
                                        sb.AppendLine(words.Value<string>().Trim());
                                    }
                                    result = sb.ToString().Trim();
                                }
                            }
                        }
                    }
                }
            }
            return (result);
        }

        internal async Task<string> Run_Baidu_Translate(string src, string from = "")
        {
            string result = string.Empty;

            var ran = new Random(65536);
            var salt = ran.Next(32768, 65536);
            var apiURL = BaiduApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_TT].EndPoint) ? BaiduApi[API_TITLE_TT].EndPoint : "https://api.fanyi.baidu.com/api/trans/vip/translate";
            var appID = BaiduApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_TT].AppId) ? BaiduApi[API_TITLE_TT].AppId : string.Empty;
            var appKEY = BaiduApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(BaiduApi[API_TITLE_TT].SecretKey) ? BaiduApi[API_TITLE_TT].SecretKey : string.Empty;
            if (string.IsNullOrEmpty(appID) || string.IsNullOrEmpty(appKEY)) return (result);

            var md5hash = MD5.Create();
            var signs = md5hash.ComputeHash(Encoding.UTF8.GetBytes($"{appID}{src}{salt}{appKEY}"));
            var sign = string.Join("", signs.Select(v => v.ToString("x2")));

            var lang_src = GetLanguageFrom(from);
            lang_src = baidu_languages.ContainsKey(lang_src) ? baidu_languages[lang_src] : "auto";

            var lang_dst = GetLangiageTo();
            lang_dst = baidu_languages.ContainsKey(lang_dst) ? lang_dst = baidu_languages[lang_dst] : "auto";

            var param = new Dictionary<string, string>()
            {
                { "q", src },
                { "from", lang_src },
                { "to", lang_dst },
                { "appid", appID },
                { "salt", $"{salt}" },
                { "sign", sign },
            };

            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.PostAsync(apiURL, new FormUrlEncodedContent(param)))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var translate_result = await response.Content.ReadAsStringAsync();

                        JToken token = JToken.Parse(translate_result);
                        Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
                        Result_JSON = Result_JSON.Replace("\\\"", "\"");

                        JToken translations = token.SelectToken("$", false);
                        JToken t_from = translations.SelectToken("$.from", false);
                        JToken t_to = translations.SelectToken("$.to", false);
                        JToken t_results = translations.SelectToken("$.trans_result", false);
                        if (t_results != null)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var t_result in t_results.Children())
                            {
                                JToken t_result_src = t_result.SelectToken("$..src", false);
                                JToken t_result_dst = t_result.SelectToken("$..dst", false);
                                sb.AppendLine(t_result_dst.Value<string>().Trim());
                            }
                            result = sb.ToString().Trim();
                        }
                    }
                }
            }
            return (result.Trim());
        }
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

        #region Get Image from Clipboard or Screen Capture
        internal System.Windows.Media.Color CaptureBorderColor { get; set; } = System.Windows.Media.Colors.CadetBlue;
        internal int CaptureBorderThickness { get; set; } = 2;
        internal double CaptureBackgroundOpacity { get; set; } = 0.50;
        internal ScreenshotOptions CaptureOption { get; set; } = null;

        internal Image GetCaptureScreen()
        {
            Image result = null;
            using (var ms = new MemoryStream())
            {
                try
                {
                    if (CaptureOption == null) CaptureOption = new ScreenshotOptions()
                    {
                        BackgroundOpacity = CaptureBackgroundOpacity,
                        SelectionRectangleBorderBrush = new System.Windows.Media.SolidColorBrush(CaptureBorderColor)
                    };

                    var capture = Screenshot.CaptureRegion(CaptureOption);
                    if (capture.PixelWidth >= 16 && capture.PixelHeight >= 16)
                    {
                        var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));
                        png.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        result = Image.FromStream(ms);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }
            return (result);
        }

        internal Image GetClipboardImage()
        {
            Image result = null;
            IDataObject iData = Clipboard.GetDataObject();
            var fmts_c = iData.GetFormats();
            var fmts = new List<string>() { "PNG", "image/png", "image/jpg", "image/jpeg", "image/bmp", "image/tif", "image/tiff", "Bitmap" };
            foreach (var fmt in fmts)
            {
                if (fmts_c.Contains(fmt) && iData.GetDataPresent(fmt, true))
                {
                    try
                    {
                        using (var img = (MemoryStream)iData.GetData(fmt, true))
                        {
                            result = Image.FromStream(img);
                        }
                        break;
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"GetClipData[{fmt}] => {ex.Message}"); }
                }
            }
            return (result);
        }

        internal string GetFirefoxPath()
        {
            string result = string.Empty;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Process");

            var firefox_name = "firefox.exe";
            var items = searcher.Get().Cast<ManagementObject>().Where(p => p["Name"].ToString().Equals(firefox_name)).ToList();
            foreach (var item in items)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"{item["ProcessId"]} => {item["Name"]}, {item["ExecutablePath"]}");
#endif
                result = item["ExecutablePath"].ToString();
                break;
            }
            return (result);
        }

        internal IList<string> ConvertTextV2H(IEnumerable<string> lines)
        {
            var result = new List<string>();
            try
            {
                int w = lines.Max(l => l.Length), h = lines.Count();
                var laa = lines.Select(l => l.Replace(" ", "　").ToArray()).ToArray();
                List<List<char>> matrix = new List<List<char>>();

                for (int i = 0; i < w; i++)
                {
                    var l = new List<char>();
                    for (int j = 0; j < h; j++)
                    {
                        try { l.Add(laa[j][w - i - 1]); }
                        catch { l.Add('　'); }
                    }
                    matrix.Add(l);
                }
                result = matrix.Select(r => string.Join("", r)).ToList();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{ex.Message}{Environment.NewLine}{ex.StackTrace}"); }
            return (result);
        }

        internal IList<string> ConvertTextH2V(IEnumerable<string> lines)
        {
            var result = new List<string>();
            try
            {
                int w = lines.Max(l => l.Length), h = lines.Count();
                var laa = lines.Select(l => l.Replace(" ", "　").ToArray()).ToArray();
                List<List<char>> matrix = new List<List<char>>();

                for (int i = 0; i < w; i++)
                {
                    var l = new List<char>();
                    for (int j = 0; j < h; j++)
                    {
                        try { l.Add(laa[h - j - 1][i]); }
                        catch { l.Add('　'); }
                    }
                    matrix.Add(l);
                }
                result = matrix.Select(r => string.Join("", r)).ToList();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{ex.Message}{Environment.NewLine}{ex.StackTrace}"); }
            return (result);
        }

        internal IList<string> ConvertSpaceToFull(IEnumerable<string> lines)
        {
            return (lines.Select(l => l.Replace(" ", "　")).ToList());
        }

        internal IList<string> ConvertSpaceToHalf(IEnumerable<string> lines)
        {
            return (lines.Select(l => l.Replace("　", " ")).ToList());
        }
        #endregion

        private int lastSelectionStart = 0;
        private int lastSelectionLength  = 0;

        private List<ToolStripMenuItem> AzureOCR_Endpoints = new List<ToolStripMenuItem>();

        protected override void WndProc(ref Message m)
        {
            try
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
                        if (iData.GetDataPresent("PNG", true) ||
                            iData.GetDataPresent("image/png", true) ||
                            iData.GetDataPresent("image/bmp", true) ||
                            iData.GetDataPresent(DataFormats.Bitmap, true))
                        {
                            // Clipboard image
                            System.Diagnostics.Debug.WriteLine("ClipboardChanged");
                            ClipboardChanged = true;
                            btnOCR.PerformClick();
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"WndProc => {ex.Message}"); }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadConfig()
        {
            var cfg = Path.Combine(AppPath, AppConfigFile);
            try
            {
                if (File.Exists(cfg))
                {
                    var json = File.ReadAllText(cfg);
                    JToken token = JToken.Parse(json);
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
                    JToken ontop = token.SelectToken("$..topmost", false);
                    if (ontop != null)
                    {
                        bool.TryParse(ontop.ToString(), out ALWAYS_ON_TOP);
                        tsmiTopMost.Checked = ALWAYS_ON_TOP;
                        this.TopMost = ALWAYS_ON_TOP;
                    }
                    #endregion

                    #region Form Opacity
                    JToken form_opacity = token.SelectToken("$..opacity", false);
                    if (form_opacity != null)
                    {
                        Opacity = Convert.ToDouble(form_opacity.ToString());
                        string os = $"{Math.Round(Opacity * 100, 0)}%";
                        foreach (ToolStripMenuItem mi in tsmiOpacity.DropDownItems)
                        {
                            if (mi.Text == os)
                                mi.Checked = true;
                            else
                                mi.Checked = false;
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

                    #region Capture Options
                    var patten_htmlcolor = @"^#?([0-9a-f]{2}){3,4}$";
                    JToken capture_opts = token.SelectToken("$..capture", false);
                    if (capture_opts != null)
                    {
                        var capture_opacity = capture_opts.SelectToken("$..BackgroundOpacity", false).ToString();
                        if (!string.IsNullOrEmpty(capture_opacity))
                        {
                            double capture_opacity_value = CaptureBackgroundOpacity;
                            if (double.TryParse(capture_opacity, out capture_opacity_value)) CaptureBackgroundOpacity = capture_opacity_value;
                        }
                        var capture_borderthickness = capture_opts.SelectToken("$..BorderThickness", false).ToString();
                        if (!string.IsNullOrEmpty(capture_borderthickness))
                        {
                            int capture_borderthickness_value = CaptureBorderThickness;
                            if (int.TryParse(capture_borderthickness, out capture_borderthickness_value)) CaptureBorderThickness = capture_borderthickness_value;
                        }
                        var capture_bordercolor = capture_opts.SelectToken("$..BorderColor", false).ToString();
                        if (!string.IsNullOrEmpty(capture_bordercolor))
                        {
                            try
                            {
                                if (Regex.IsMatch(capture_bordercolor, patten_htmlcolor, RegexOptions.IgnoreCase))
                                {
                                    var capture_bordercolor_value = System.Windows.Media.ColorConverter.ConvertFromString(capture_bordercolor);
                                    if (capture_bordercolor_value is System.Windows.Media.Color)
                                        CaptureBorderColor = (System.Windows.Media.Color)capture_bordercolor_value;
                                }
                                else
                                {
                                    var color = Color.FromName(capture_bordercolor);
                                    if (color.ToArgb() != 0)
                                        CaptureBorderColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
                                }
                            }
                            catch (Exception)
                            {
                                if (Regex.IsMatch(capture_bordercolor, patten_htmlcolor, RegexOptions.IgnoreCase))
                                {
                                    var colors = Regex.Replace(capture_bordercolor.Trim('#'), @"..(?!$)", "$0,", RegexOptions.IgnoreCase).Split(',');
                                    if (colors.Length == 4) CaptureBorderColor = System.Windows.Media.Color.FromArgb(
                                        byte.Parse(colors[0], NumberStyles.HexNumber),
                                        byte.Parse(colors[1], NumberStyles.HexNumber),
                                        byte.Parse(colors[2], NumberStyles.HexNumber),
                                        byte.Parse(colors[3], NumberStyles.HexNumber));
                                    else if (colors.Length == 3) CaptureBorderColor = System.Windows.Media.Color.FromRgb(
                                        byte.Parse(colors[0], NumberStyles.HexNumber),
                                        byte.Parse(colors[1], NumberStyles.HexNumber),
                                        byte.Parse(colors[2], NumberStyles.HexNumber));
                                }
                            }
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

                    #region OCR Correction Table
                    JToken ocr_corr_file = token.SelectToken("$..ocr.correction_file", false);
                    if (ocr_corr_file != null)
                    {
                        try
                        {
                            CorrectionDictFile = Convert.ToString(ocr_corr_file);
                        }
                        catch (Exception) { }
                    }
                    JToken ocr_corr_editor = token.SelectToken("$..ocr.correction_editor", false);
                    if (ocr_corr_editor != null)
                    {
                        try
                        {
                            CorrectionDictEditor = Convert.ToString(ocr_corr_editor);
                        }
                        catch (Exception) { }
                    }
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
                            foreach (var item in tsmiTranslateDst.DropDownItems)
                            {
                                if (item is ToolStripMenuItem)
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
                    JToken autospeechrate = token.SelectToken("$..speech.auto_rate", false);
                    if (autospeechrate != null)
                    {
                        try
                        {
                            tsmiTextAutoSpeechingRate.Checked = autospeechrate.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    JToken autospeech = token.SelectToken("$..speech.auto_speech", false);
                    if (autospeech != null)
                    {
                        try
                        {
                            tsmiTextAutoSpeech.Checked = autospeech.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    JToken altmixedculture = token.SelectToken("$..speech.alt_play_mixed_culture", false);
                    if (altmixedculture != null)
                    {
                        try
                        {
                            Speech.AltPlayMixedCulture = altmixedculture.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    JToken simpledetectculture = token.SelectToken("$..speech.simple_detect_culture", false);
                    if (simpledetectculture != null)
                    {
                        try
                        {
                            Speech.AltPlayMixedCulture = simpledetectculture.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    JToken culturevoice = token.SelectToken("$..speech.voice", false);
                    if (culturevoice != null)
                    {
                        try
                        {
                            var voice_list = culturevoice.ToObject<Dictionary<string, List<string>>>();
                            foreach (var kv in voice_list)
                            {
                                Speech.SetVoice(kv.Key, kv.Value);
                            }
                        }
                        catch (Exception) { }
                    }
                    #endregion

                    #region Result Editor Option
                    JToken editor_use_ime = token.SelectToken("$..result.use_last_ime", false);
                    if (editor_use_ime != null)
                    {
                        try
                        {
                            tsmiUseLastIme.Checked = editor_use_ime.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    if (tsmiUseLastIme.Checked)
                    {
                        JToken editor_ime = token.SelectToken("$..result.ime", false);
                        if (editor_ime != null)
                        {
                            try
                            {
                                var lang = editor_ime.ToString();
                                foreach (InputLanguage input in InputLanguage.InstalledInputLanguages)
                                {
                                    if (input.LayoutName.Equals(lang, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        CurrentInputLanguage = input;
                                        InputLanguage.CurrentInputLanguage = input;
                                        break;
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    JToken editor_use_lang = token.SelectToken("$..result.use_last_language", false);
                    if (editor_use_lang != null)
                    {
                        try
                        {
                            tsmiUseLastOCRLanguage.Checked = editor_use_lang.Value<bool>();
                        }
                        catch (Exception) { }
                    }
                    if (tsmiUseLastOCRLanguage.Checked)
                    {
                        JToken editor_lang = token.SelectToken("$..result.language", false);
                        if (editor_lang != null)
                        {
                            try
                            {
                                var lang = editor_lang.ToString();
                                foreach (var item in cbLanguage.Items)
                                {
                                    if (item is string && item.ToString().Equals(lang, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        cbLanguage.Text = lang;
                                        break;
                                    }
                                    else if (item is KeyValuePair<string, string>)
                                    {
                                        var kv = (KeyValuePair<string, string>)item;
                                        if (lang.Equals(kv.Key, StringComparison.CurrentCultureIgnoreCase) ||
                                            lang.Equals(kv.Value, StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            cbLanguage.Text = lang;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    JToken editor_font = token.SelectToken("$..result.font", false);
                    if (editor_font != null)
                    {
                        try
                        {
                            var s = Convert.ToString(editor_font);
                            var cvt = new FontConverter();
                            edResult.Font = cvt.ConvertFromInvariantString(s) as Font;
                            font_size_default = edResult.Font.SizeInPoints;
                        }
                        catch (Exception) { }
                    }
                    #endregion

                    #region API Keys
                    JToken apis_azure = token.SelectToken("$..apis_azure", false);
                    if (apis_azure is JToken)
                    {
                        var azure = apis_azure.ToObject<List<AzureAPI>>();
                        AzureApi = azure.ToDictionary(api => api.Name, api => api);
                    }
                    JToken apis_baidu = token.SelectToken("$..apis_baidu", false);
                    if (apis_baidu is JToken)
                    {
                        var baidu = apis_baidu.ToObject<List<BaiduAPI>>();
                        BaiduApi = baidu.ToDictionary(api => api.Name, api => api);
                    }
                    JToken ocr_engine = token.SelectToken("$..ocr_engine", false);
                    if (ocr_engine is JToken)
                    {
                        var engine = ocr_engine.Value<string>().ToLower();
                        if (engine.Equals("azure")) { tsmiOcrEngineAzure.Checked = true; tsmiOcrEngineBaidu.Checked = false; }
                        else if (engine.Equals("baidu")) { tsmiOcrEngineAzure.Checked = false; tsmiOcrEngineBaidu.Checked = true; }
                    }
                    JToken translate_engine = token.SelectToken("$..translate_engine", false);
                    if (translate_engine is JToken)
                    {
                        var engine = translate_engine.Value<string>().ToLower();
                        if (engine.Equals("azure")) { tsmiTranslateEngineAzure.Checked = true; tsmiTranslateEngineBaidu.Checked = false; }
                        else if (engine.Equals("baidu")) { tsmiTranslateEngineAzure.Checked = false; tsmiTranslateEngineBaidu.Checked = true; }
                    }
                    if (AzureApi.ContainsKey(API_TITLE_CV))
                    {
                        AzureApi[$"{API_TITLE_CV}_Default"] = new AzureAPI()
                        {
                            Name = $"{API_TITLE_CV}_Default",
                            ApiKey = AzureApi[API_TITLE_CV].ApiKey,
                            EndPoint = AzureApi[API_TITLE_CV].EndPoint,
                            EndPointName = "Default"
                        };
                    }
                    foreach (var api in AzureApi.Where(azure => azure.Key.StartsWith($"{API_TITLE_CV}_")))
                    {
                        if (api.Key.StartsWith(API_TITLE_CV, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(api.Value.EndPointName))
                            {
                                var endpoint = Regex.Replace(api.Value.EndPoint, @"https?://(.*?)(\..*?\.com.*)", "$1", RegexOptions.IgnoreCase);
                                api.Value.EndPointName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(endpoint);
                            }
                            var menuitem = new ToolStripMenuItem()
                            {
                                Name = $"AzureCV_{api.Value.EndPointName}",
                                Text = api.Value.EndPointName,
                                CheckOnClick = false
                            };
                            menuitem.Click += tsmiOcrEngine_Click;
                            tsmiOcrEngineAzure.DropDownItems.Add(menuitem);
                            if (api.Value.EndPointName.Equals("Default", StringComparison.CurrentCultureIgnoreCase)) menuitem.Checked = true;
                            AzureOCR_Endpoints.Add(menuitem);
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadConfig => {ex.Message}"); MessageBox.Show(ex.Message); }

            LoadCorrectionDictionary();

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
                { "topmost", ALWAYS_ON_TOP },
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
                { "capture", new Dictionary<string, string>()
                    {
                        { "BackgroundOpacity", $"{CaptureBackgroundOpacity}" },
                        { "BorderThickness", $"{CaptureBorderThickness}" },
                        { "BorderColor", CaptureBorderColor.ToString() }
                    }
                },
                { "ocr", new Dictionary<string, object>()
                    {
                        { "log_history", OCR_HISTORY },
                        { "correction_file", CorrectionDictFile },
                        { "correction_editor", CorrectionDictEditor },
                    }
                },
                { "translate", new Dictionary<string, object>()
                    {
                        { "auto_translate", tsmiTranslateAuto.Checked },
                        { "translate_to", (string)tsmiTranslateDst.Tag }
                    }
                },
                { "speech",  new Dictionary<string, object>()
                    {
                        { "auto_rate", tsmiTextAutoSpeechingRate.Checked },
                        { "auto_speech", tsmiTextAutoSpeech.Checked },
                        { "alt_play_mixed_culture", Speech.AltPlayMixedCulture },
                        { "simple_detect_culture", Speech.SimpleCultureDetect },
                        { "voice",Speech.GetVoices() }
                    }
                },
                { "result", new Dictionary<string, object>()
                    {
                        { "use_last_ime", tsmiUseLastIme.Checked },
                        { "ime", CurrentInputLanguage.LayoutName },
                        { "use_last_language", tsmiUseLastOCRLanguage.Checked },
                        { "language", cbLanguage.Text },
                        { "font", (new FontConverter()).ConvertToInvariantString(edResult.Font)}
                    }
                },
                { "ocr_engine", tsmiOcrEngineBaidu.Checked ? "baidu" : "azure" },
                { "translate_engine", tsmiTranslateEngineBaidu.Checked ? "baidu" : "azure" },
                { "apis_azure", AzureApi.Values.ToList()},
                { "apis_baidu", BaiduApi.Values.ToList()}
            };

            File.WriteAllText(cfg, JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        private double font_size_default { get; set; } = 9;
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

        private int cursor_offset = -1;
        private List<string> words = new List<string>();
        private void MainForm_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            #region Speech Synthesis Events Action
            Speech.SpeakStarted = new Action<SpeakStartedEventArgs>((evt) =>
            {
                words.Clear();
                if (!Speech.IsSlicing)
                {
                    edResult.ReadOnly = true;
                    lastSelectionStart = edResult.SelectionStart;
                    lastSelectionLength = edResult.SelectionLength;
                    tsmiTextPlay.Checked = true;
                    tsmiTextPause.Checked = false;
                    tsmiTextStop.Checked = false;
                    if (cursor_offset == -1) cursor_offset = edResult.SelectionLength > 0 ? edResult.SelectionStart : 0;
                }
            });

            Speech.SpeakProgress = new Action<SpeakProgressEventArgs>((evt) =>
            {
                var ei = evt as SpeakProgressEventArgs;
                words.Add(ei.Text);
                if (!Speech.IsSlicing)
                {
                    edResult.SelectionStart = cursor_offset >= 0 ? ei.CharacterPosition + cursor_offset : ei.CharacterPosition;
                    edResult.SelectionLength = ei.CharacterCount;
                }
            });

            Speech.StateChanged = new Action<StateChangedEventArgs>((evt) =>
            {
                if (Speech.State == SynthesizerState.Paused)
                {
                    tsmiTextPlay.Checked = true;
                    tsmiTextPause.Checked = true;
                    tsmiTextStop.Checked = false;
                }
                else if (Speech.State == SynthesizerState.Speaking)
                {
                    tsmiTextPlay.Checked = true;
                    tsmiTextPause.Checked = false;
                    tsmiTextStop.Checked = false;
                }
                else if (Speech.State == SynthesizerState.Ready)
                {
                    tsmiTextPlay.Checked = false;
                    tsmiTextPause.Checked = false;
                    tsmiTextStop.Checked = true;
                }
            });

            Speech.SpeakCompleted = new Action<SpeakCompletedEventArgs>((evt) =>
            {
                if (!Speech.IsSlicing)
                {
                    tsmiTextPlay.Checked = false;
                    tsmiTextPause.Checked = false;
                    tsmiTextStop.Checked = true;
                    cursor_offset = -1;
                    edResult.SelectionStart = lastSelectionStart;
                    edResult.SelectionLength = lastSelectionLength;
                    edResult.ReadOnly = false;
                }
            });
            #endregion

            notify.Icon = Icon;
            notify.BalloonTipTitle = this.Text;
            notify.BalloonTipText = $"Using \"{API_TITLE_CV}\" OCR feature.";
            notify.Text = this.Text;

            hint.ToolTipTitle = this.Text;

            // Adds our form to the chain of clipboard viewers.
            _clipboardViewerNext = SetClipboardViewer(this.Handle);

            ////init_ocr_lang();
            cbLanguage.Items.Clear();
            cbLanguage.DataSource = new BindingSource(azure_languages, null);
            cbLanguage.DisplayMember = "Value";
            cbLanguage.ValueMember = "Key";

            tsmiTranslateSrc.DropDownItems.Clear();
            tsmiTranslateDst.DropDownItems.Clear();
            foreach (var kv in azure_languages)
            {
                var cv = kv.Key.Equals("unk", StringComparison.CurrentCultureIgnoreCase);
                tsmiTranslateSrc.DropDownItems.Add(new ToolStripMenuItem(kv.Value, null, tsmiTranslateLanguage_Click, $"tsmiTranslateSrc_{kv.Key}") { Tag = kv.Key, Checked = cv });
                tsmiTranslateDst.DropDownItems.Add(new ToolStripMenuItem(kv.Value, null, tsmiTranslateLanguage_Click, $"tsmiTranslateDst_{kv.Key}") { Tag = kv.Key, Checked = cv });
            }
            tsmiTranslateSrc.Tag = "unk";
            tsmiTranslateDst.Tag = "unk";

            tsmiTranslateEngineAzure.Checked = true;
            tsmiOcrEngineAzure.Checked = true;

            edResult.AcceptsReturn = true;
            edResult.AcceptsTab = true;
            edResult.AllowDrop = false;
            edResult.MouseWheel += edResult_MouseWheel;
            font_size_default = edResult.Font.SizeInPoints;

            LoadConfig();
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
#if DEBUG
            Console.WriteLine("Result Focus");
#endif
            edResult.Focus();
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

        private void MainForm_InputLanguageChanged(object sender, InputLanguageChangedEventArgs e)
        {
            CurrentInputLanguage = e.InputLanguage;
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
            else if (e.KeyCode == Keys.F1)
            {
                btnTranslate.PerformClick();
            }
            else if (e.KeyCode == Keys.F2)
            {
                btnSpeech.PerformClick();
            }
            else if (e.KeyCode == Keys.F3)
            {
                switch (cbLanguage.SelectedIndex)
                {
                    case 0: cbLanguage.SelectedIndex = 1; break;   //Auto
                    case 1: cbLanguage.SelectedIndex = 2; break;   //ChineseS
                    case 2: cbLanguage.SelectedIndex = 13; break;   //ChineseT
                    case 6: cbLanguage.SelectedIndex = 0; break;   //English
                    case 13: cbLanguage.SelectedIndex = 14; break;  //Japanese
                    case 14: cbLanguage.SelectedIndex = 6; break;  //Korean
                    default: break;
                }
            }
            else if (e.KeyCode == Keys.F4)
            {
                tsmiText2QR.PerformClick();
            }
            else e.Handled = true;
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
                    edResult.Text = await Run_Azure_OCR(src, lang);
                }
                else if (fmts.Contains("Bitmap"))
                {
                    var src = (Bitmap)e.Data.GetData("Bitmap");
                    string lang = cbLanguage.SelectedValue.ToString();
                    edResult.Text = await Run_Azure_OCR(src, lang);
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
                                if (exts_txt.Contains(ext))
                                {
                                    try
                                    {
                                        string[] lines = File.ReadAllLines(fn);
                                        foreach (var l in lines)
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
                                            sb.AppendLine(await Run_Azure_OCR(src, lang));
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

        private void edResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                FontSizeChange(-1);
            }
            else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                FontSizeChange(+1);
            }
            else if (e.Control && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0))
            {
                FontSizeChange(0);
            }
            else e.Handled = true;
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
            else e.Handled = true;
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
            try
            {
                btnOCR.Enabled = false;
                pbar.Style = ProgressBarStyle.Marquee;
                System.Diagnostics.Debug.WriteLine("OCR Starting...");
                Task.Delay(20).GetAwaiter().GetResult();

                var force = ModifierKeys == Keys.Control;
                var lang = ModifierKeys == Keys.Alt ? "unk" : cbLanguage.SelectedValue.ToString();
                var src = force ? null : GetClipboardImage();
                if (!(src is Image)) src = GetCaptureScreen();
                if (src is Image)
                {
                    var qr = new ZXing.BarcodeReader();
                    var qr_result = qr.Decode(new Bitmap(src));
                    if (qr_result == null || string.IsNullOrEmpty(qr_result.Text))
                    {
                        string result = string.Empty;
                        if (tsmiOcrEngineAzure.Checked)
                            result = await Run_Azure_OCR(src, lang);
                        else if (tsmiOcrEngineBaidu.Checked)
                            result = await Run_Baidu_OCR(src, lang);
                        edResult.Text = AutoCorrecting(result, lang);
                        if (tsmiTextAutoSpeech.Checked) btnSpeech.PerformClick();
                        if (tsmiTranslateAuto.Checked) btnTranslate.PerformClick();
                    }
                    else
                    {
                        edResult.Text = qr_result.Text;
                        if (Regex.IsMatch(qr_result.Text, @"^https?://", RegexOptions.IgnoreCase))
                        {
                            var firefox = GetFirefoxPath();
                            if (string.IsNullOrEmpty(firefox)) System.Diagnostics.Process.Start($"\"{qr_result.Text}\"");
                            else System.Diagnostics.Process.Start(firefox, $"\"{qr_result.Text}\"");
                        }
                    }
                }
                if (!string.IsNullOrEmpty(edResult.Text))
                {
                    tsmiShowWindow.PerformClick();
                    if (OCR_HISTORY)
                    {
                        if (ResultHistory.Count >= ResultHistoryLimit) ResultHistory.RemoveAt(0);
                        ResultHistory.Add(new KeyValuePair<string, string>(edResult.Text, Result_Lang));
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"OCR => {ex.Message}"); }
            finally
            {
                if (CLIPBOARD_CLEAR && !string.IsNullOrEmpty(edResult.Text)) Clipboard.Clear();
                ClipboardChanged = false;
                pbar.Style = ProgressBarStyle.Blocks;
                btnOCR.Enabled = true;
                edResult.Focus();
            }
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
            try
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                    Speech.SimpleCultureDetect = false;
                else
                    Speech.SimpleCultureDetect = true;
                if (ModifierKeys.HasFlag(Keys.Shift))
                    Speech.AltPlayMixedCulture = true;
                else
                    Speech.AltPlayMixedCulture = false;

                Speech.AutoChangeSpeechSpeed = tsmiTextAutoSpeechingRate.Checked;

                string lang = cbLanguage.SelectedValue.ToString();
                string culture = string.IsNullOrEmpty(lang) || ModifierKeys == Keys.Alt ? "unk" : lang;

                var slice_words = new List<string>();
                if (edResult.SelectionLength > 0)
                    slice_words.AddRange(Speech.Slice(edResult.SelectedText.Split(split_symbol, StringSplitOptions.RemoveEmptyEntries), culture));
                else
                    slice_words.AddRange(Speech.Slice(edResult.Lines, culture));
                var tip = string.Join(", ", slice_words);
                tip = Regex.Replace(tip, @"((.+?, ){5})", $"$1{Environment.NewLine}", RegexOptions.IgnoreCase);
                if (slice_words.Count > 0)
                {
                    hint.SetToolTip(edResult, null);
                    hint.Show(tip, edResult, edResult.Left, edResult.Bottom, 5000);
                    hint.SetToolTip(edResult, tip);
                }

                if (Speech.State == SynthesizerState.Ready)
                {
                    if (edResult.SelectionLength > 0)
                        Speech.Play(edResult.SelectedText.Split(split_symbol, StringSplitOptions.RemoveEmptyEntries), culture);
                    else
                        Speech.Play(edResult.Lines, culture);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Data.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            edResult.Focus();
        }

        private async void btnTranslate_Click(object sender, EventArgs e)
        {
            try
            {
                pbar.Style = ProgressBarStyle.Marquee;
                int select_s = edResult.SelectionStart;
                int select_l = edResult.SelectionLength;
                string text = edResult.SelectionLength > 0 ? edResult.SelectedText : edResult.Text;
                var result = string.Empty;
                if (tsmiTranslateEngineAzure.Checked)
                    result = await Run_Azure_Translate(text);
                else if (tsmiTranslateEngineBaidu.Checked)
                    result = await Run_Baidu_Translate(text);
                if (!string.IsNullOrEmpty(result))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(edResult.Text.Trim());
                    sb.AppendLine();
                    sb.AppendLine(result);
                    edResult.Text = sb.ToString();
                    edResult.SelectionStart = select_s;
                    edResult.SelectionLength = select_l;
                }
            }
            catch (Exception) { }
            finally
            {
                pbar.Style = ProgressBarStyle.Blocks;
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
            if (sender == tsmiRestart)
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath);
            }
            Application.Exit();
        }

        private void tsmiShowWindow_Click(object sender, EventArgs e)
        {
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
            edResult.SelectAll();
            edResult.Focus();
        }

        private void tsmiTopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = tsmiTopMost.Checked;
            ALWAYS_ON_TOP = tsmiTopMost.Checked;
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
                var dlgResult = MessageBox.Show( $"Text in result box will be saved as {API_TITLE_CV} Key!", "Note", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning );
                if (dlgResult == DialogResult.OK)
                {
                    AzureApi[API_TITLE_CV] = new AzureAPI() { Name = API_TITLE_CV, ApiKey = edResult.Text.Trim() };
                }
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
            if (sender == tsmiTextPlay)
            {
                btnSpeech.PerformClick();
            }
            else if (sender == tsmiTextPause)
            {
                if (Speech.State == SynthesizerState.Paused)
                    Speech.Resume();
                else if (Speech.State == SynthesizerState.Speaking)
                    Speech.Pause();
            }
            else if (sender == tsmiTextStop)
            {
                Speech.Stop();
            }
        }

        private void tsmiTranslate_Click(object sender, EventArgs e)
        {
            btnTranslate.PerformClick();
        }

        private void tsmiOptions_Click(object sender, EventArgs e)
        {
            OptionsForm opt = new OptionsForm()
            {
                Icon = Icon,
                APIKEYTITLE_CV = API_TITLE_CV,
                APIKEYTITLE_TT = API_TITLE_TT,
                APIKEY_CV = AzureApi.ContainsKey(API_TITLE_CV) && !string.IsNullOrEmpty(AzureApi[API_TITLE_CV].ApiKey) ? AzureApi[API_TITLE_CV].ApiKey : string.Empty,
                APIKEY_TT = AzureApi.ContainsKey(API_TITLE_TT) && !string.IsNullOrEmpty(AzureApi[API_TITLE_TT].ApiKey) ? AzureApi[API_TITLE_TT].ApiKey : string.Empty
            };

            if (opt.ShowDialog() == DialogResult.OK)
            {
                AzureApi[API_TITLE_CV] = new AzureAPI() { Name = API_TITLE_CV, ApiKey = opt.APIKEY_CV };
                AzureApi[API_TITLE_TT] = new AzureAPI() { Name = API_TITLE_TT, ApiKey = opt.APIKEY_TT };
                SaveConfig();
            }
            opt.Dispose();
        }

        private void tsmiTranslateLanguage_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                var obj = sender as ToolStripMenuItem;
                if (obj.Name.StartsWith("tsmiTranslateSrc_", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var item in tsmiTranslateSrc.DropDownItems)
                    {
                        if (item is ToolStripMenuItem)
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
            Speech.AutoChangeSpeechSpeed = tsmiTextAutoSpeech.Checked;
        }

        private void tsmiOcrEngine_Click(object sender, EventArgs e)
        {
            if (sender == tsmiOcrEngineAzure)
            {
                tsmiOcrEngineAzure.Checked = true;
                tsmiOcrEngineBaidu.Checked = false;
            }
            else if (sender == tsmiOcrEngineBaidu)
            {
                tsmiOcrEngineAzure.Checked = false;
                tsmiOcrEngineBaidu.Checked = true;
            }
            else if (sender is ToolStripMenuItem)
            {
                foreach (var mi in AzureOCR_Endpoints)
                {
                    if (mi == sender)
                    {
                        var endpoint = mi.Name.Replace("AzureCV_", "");
                        var api = AzureApi.Where(cv => cv.Key.StartsWith(API_TITLE_CV) && cv.Value.EndPointName.Equals(endpoint, StringComparison.CurrentCultureIgnoreCase));
                        if (api.Count() > 0)
                        {
                            var azure = api.First();
                            mi.Checked = true;
                            AzureApi[API_TITLE_CV].ApiKey = azure.Value.ApiKey;
                            AzureApi[API_TITLE_CV].EndPoint = azure.Value.EndPoint;
                        }
                    }
                    else mi.Checked = false;
                }
            }
        }

        private void tsmiTranslateEngine_Click(object sender, EventArgs e)
        {
            if (sender == tsmiTranslateEngineAzure)
            {
                tsmiTranslateEngineAzure.Checked = true;
                tsmiTranslateEngineBaidu.Checked = false;
            }
            else if (sender == tsmiTranslateEngineBaidu)
            {
                tsmiTranslateEngineAzure.Checked = false;
                tsmiTranslateEngineBaidu.Checked = true;
            }
        }

        private void tsmiConvertText_Click(object sender, EventArgs e)
        {
            try
            {
                var HasSelection = edResult.SelectionLength > 0;
                var lines = HasSelection ? edResult.SelectedText.Split(new string[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.None) : edResult.Lines;

                if (sender == tsmiTextV2H)
                {
                    lines = ConvertTextV2H(lines).ToArray();
                }
                else if (sender == tsmiTextH2V)
                {
                    lines = ConvertTextH2V(lines).ToArray();
                }
                else if (sender == tsmiSpaceToFull)
                {
                    lines = ConvertSpaceToFull(lines).ToArray();
                }
                else if (sender == tsmiSpaceToHalf)
                {
                    lines = ConvertSpaceToHalf(lines).ToArray();
                }

                if (HasSelection) edResult.SelectedText = string.Join(Environment.NewLine, lines);
                else edResult.Lines = lines;

            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{ex.Message}{Environment.NewLine}{ex.StackTrace}"); }
        }

        private void tsmiText2QR_Click(object sender, EventArgs e)
        {
            try
            {
                var size_m = 512;
                var size_l = 1024;
                int select_s = edResult.SelectionStart;
                int select_l = edResult.SelectionLength;
                string text = edResult.SelectionLength > 0 ? edResult.SelectedText : edResult.Text;

                var render = new ZXing.Rendering.BitmapRenderer();
                var hint = new Dictionary<ZXing.EncodeHintType, object>() { { ZXing.EncodeHintType.MIN_SIZE, new Size(size_m, size_m) } };
                var qr = new ZXing.BarcodeWriter()
                {
                    Format = ZXing.BarcodeFormat.QR_CODE,
                    Options = new ZXing.QrCode.QrCodeEncodingOptions() { Height = size_l, Width = size_l, CharacterSet = "UTF-8" },
                    Renderer = render
                };
                var qr_matrix = qr.Encode(text);
                var qr_result = render.Render(qr_matrix, ZXing.BarcodeFormat.QR_CODE, text);
                if (qr_result is Bitmap)
                {
                    var picbox = new PictureBox()
                    {
                        Image = qr_result,
                        Width = qr_result.Width,
                        Height = qr_result.Height,
                        BorderStyle = BorderStyle.None,
                        Dock = DockStyle.Fill,
                        SizeMode = PictureBoxSizeMode.Zoom
                    };
                    var win = new Form()
                    {
                        Icon = this.Icon,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly,
                        BackColor = render.Background,
                        ClientSize = new Size(size_m, size_m),
                        StartPosition = FormStartPosition.CenterScreen,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        //MaximizeBox = false,
                        MinimizeBox = false,
                        SizeGripStyle = SizeGripStyle.Hide,
                        DialogResult = DialogResult.OK,
                        CancelButton = null,
                    };
                    win.Controls.Add(picbox);
                    if (DialogResult.Cancel == win.ShowDialog())
                    {
                        if (picbox.Image is Image) picbox.Image.Dispose();
                        picbox.Dispose();
                        win.Dispose();
                    }
                    //win = new System.Windows.
                }
            }
            catch (Exception) { }
        }

        private void tsmiCorrectionTable_Click(object sender, EventArgs e)
        {
            if (sender == tsmiReloadCorrectionTable)
            {
                LoadCorrectionDictionary();
                edResult.Text = AutoCorrecting(edResult.Text, GetLanguageFrom());
            }
            else if (sender == tsmiEditCorrectionTable)
            {
                if (!string.IsNullOrEmpty(CorrectionDictFile))
                {
                    var file = Path.Combine(AppPath, CorrectionDictFile);
                    if (File.Exists(file))
                    {
                        if (string.IsNullOrEmpty(CorrectionDictEditor)) System.Diagnostics.Process.Start(file);
                        else System.Diagnostics.Process.Start(CorrectionDictEditor, file);
                    }
                }
            }
        }

        private void tsmiEditConfig_Click(object sender, EventArgs e)
        {
            if (sender == tsmiReloadConfig)
            {
                LoadConfig();
            }
            else if (sender == tsmiEditConfig)
            {
                if (!string.IsNullOrEmpty(AppConfigFile))
                {
                    var file = Path.Combine(AppPath, AppConfigFile);
                    if (File.Exists(file))
                    {
                        if (string.IsNullOrEmpty(CorrectionDictEditor)) System.Diagnostics.Process.Start(file);
                        else System.Diagnostics.Process.Start(CorrectionDictEditor, file);
                    }
                }
            }
        }

    }

    public class AzureAPI
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("apikey")]
        public string ApiKey { get; set; } = string.Empty;
        [JsonProperty("endpoint")]
        public string EndPoint { get; set; } = string.Empty;
        [JsonProperty("endpointname")]
        public string EndPointName { get; set; } = string.Empty;
    }

    public class BaiduAPI
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("tokenurl")]
        public string TokenURL { get; set; } = string.Empty;
        [JsonProperty("endpoint")]
        public string EndPoint { get; set; } = string.Empty;
        [JsonProperty("appid")]
        public string AppId { get; set; } = string.Empty;
        [JsonProperty("appkey")]
        public string AppKey { get; set; } = string.Empty;
        [JsonProperty("secretkey")]
        public string SecretKey { get; set; } = string.Empty;
    }

    public class CaptureOption
    {
        [JsonProperty("bordercolor")]
        public System.Windows.Media.Color CaptureBorderColor { get; set; } = System.Windows.Media.Colors.CadetBlue;
        [JsonProperty("borderthickness")]
        public int BorderThickness { get; set; } = 2;
        [JsonProperty("backgroundopacity")]
        public double BackgroundOpacity { get; set; } = 0.75;
    }

    public class CorrectionDict
    {
        public string Language { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OrderedDictionary Words { get; set; } = new OrderedDictionary();

        public void Clear()
        {
            if (Words is OrderedDictionary) Words.Clear();
            else Words = new OrderedDictionary();
        }
    }

    public class CorrectionDicts
    {
        public Dictionary<string, CorrectionDict> Dictionaries { get; set; } = new Dictionary<string, CorrectionDict>(StringComparer.OrdinalIgnoreCase);

        public void Clear()
        {
            if (Dictionaries is IList<CorrectionDict>)
            {
                foreach (var dict in Dictionaries) { dict.Value.Clear(); }
                Dictionaries.Clear();
            }
            else Dictionaries = new Dictionary<string, CorrectionDict>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
