using NAudio.MediaFoundation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Web.Script.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;

namespace SoundToText
{
    public class AudioSlice
    {
        public bool IsActive { get; set; } = false;
        public TimeSpan Start { get; set; } = default(TimeSpan);
        public TimeSpan End { get; set; } = default(TimeSpan);
        public byte[] Audio { get; set; } = null;
    }

    public class SpeechRecognizer
    {
        #region Recognizer Internal Variable
        public static List<RecognizerInfo> InstalledRecognizers { get; } = SpeechRecognitionEngine.InstalledRecognizers().ToList();
        private Dictionary<string, SpeechRecognitionEngine> _recognizers = new Dictionary<string, SpeechRecognitionEngine>();
        private SpeechRecognitionEngine _defaultRecognizer = null;
        private SpeechRecognitionEngine _recognizer = null;
        private WaveFormat defaultWaveFmt = new WaveFormat(16000, 16, 1);

        private MemoryStream _recognizerStream = null;

        private string APPID_iFlySpeech = string.Empty;
        private string APPID_iFlySpeech_File = "appid_ifly_speech.txt";
        private string APIKEY_iFlySpeech = string.Empty;
        private string APISecret_iFlySpeech = string.Empty;
        private string APIKEY_iFlySpeech_File = "apikey_ifly_speech.txt";
        private iFly.SpeechRecognizer iflysr = null;
        private iFly.iFlySpeechOnline iflyIAT = null;


        private string APIKEY_GoogleSpeech = string.Empty;
        private string APIKEY_GoogleSpeech_File = "apikey_google_speech.txt";

        private string APIKEY_AzureTranslate = string.Empty;
        private string APIKEY_AzureTranslate_File = "apikey_azure_translate.txt";
        private string URL_AzureTranslate = "https://api.cognitive.microsofttranslator.com/translate";

        private string APIKEY_AzureSpeech = string.Empty;
        private string APIKEY_AzureSpeech_File = "apikey_azure_speech.txt";
        private string URL_AzureSpeechToken = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private string URL_AzureSpeechResponse = "https://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
        private string Token_AzureSpeech = string.Empty;
        private DateTime Token_AzureSpeech_Lifetime = DateTime.Now;
        #endregion

        #region Private vars
        private int _ReRecognize = -1;

        private CountdownEvent taskCount = new CountdownEvent(1);
        #endregion

        #region Properties
        private CultureInfo culture = CultureInfo.CurrentCulture;
        public CultureInfo Culture
        {
            get { return (culture); }
            set
            {
                culture = value;
                if (_recognizers.Count == InstalledRecognizers.Count)
                    _recognizer = _recognizers[culture.Name];
                else
                    _recognizer = _defaultRecognizer;
            }
        }

        public Stream AudioStream
        {
            get
            {
                return (_recognizerStream);
            }
            private set
            {
                var pos = value.Position;
                _recognizerStream.SetLength(value.Length);
                _recognizerStream.Seek(0, SeekOrigin.Begin);
                value.Seek(0, SeekOrigin.Begin);
                value.CopyToAsync(_recognizerStream);
                _recognizerStream.FlushAsync();
                _recognizerStream.Seek(pos, SeekOrigin.Begin);
            }
        }

        public Action IsCompleted { get; set; } = null;

        public Action<TimeSpan, TimeSpan> AudioLoaded { get; set; } = null;

        private SpeechAudioFormatInfo _audioInfo = null;
        private string _audiofile = string.Empty;
        public string AudioFile
        {
            get { return (_audiofile); }
            set
            {
                _audiofile = value;
                new Task(async () =>
                {
                    _audioInfo = await LoadAudio(_audiofile);
                }).Start();
            }
        }

        private TimeSpan lastAudioPos = TimeSpan.FromSeconds(0);
        public TimeSpan AudioLength { get; set; } = TimeSpan.FromSeconds(0);
        public TimeSpan AudioPos { get; set; } = TimeSpan.FromSeconds(0);

        public IProgress<Tuple<TimeSpan, TimeSpan>> ProgressHost { get; set; } = null;
        public Tuple<TimeSpan, TimeSpan> Progress
        {
            get
            {
                if (_recognizer is SpeechRecognitionEngine)
                    return (Tuple.Create(AudioPos, AudioLength));
                else
                    return (Tuple.Create(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));
            }
        }

        public string Title
        {
            get { return (Path.GetFileNameWithoutExtension(AudioFile)); }
        }

        private ObservableCollection<SRT> srt = new ObservableCollection<SRT>();
        public ObservableCollection<SRT> Result { get { return (srt); } }
        public string Text
        {
            get
            {
                if (srt is IEnumerable<SRT>)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (SRT s in srt)
                    {
                        sb.AppendLine($"{s.DisplayIndex}");
                        sb.AppendLine($"{s.NewStart.ToString(@"hh\:mm\:ss\,fff")} --> {s.NewEnd.ToString(@"hh\:mm\:ss\,fff")}");
                        sb.AppendLine($"{s.MultiLingoText}");
                        sb.AppendLine();
                    }
                    return (sb.ToString());
                }
                else return (string.Empty);
            }
        }

        public bool SAPIEnabled { get; set; } = true;
        public bool iFlyEnabledSDK { get; set; } = false;
        public bool iFlyEnabledWebAPI { get; set; } = false;
        public bool AzureEnabled { get; set; } = false;
        public bool GoogleEnabled { get; set; } = false;
        #endregion

        #region Common Helper Routines
        private void Log(string text)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        private async void CheckCompleted(bool autoRelease = true)
        {
            if (autoRelease)
                taskCount.Reset(taskCount.CurrentCount - 1);
            if (taskCount.CurrentCount <= 1 && IsCompleted is Action)
            {
                IsRunning = false;
                IsPausing = false;
                //if (iFlyEnabledWebAPI && iflyIAT is iFly.iFlySpeechOnline)
                //    iflyIAT.Disconnect();
                if (iFlyEnabledSDK && iflysr is iFly.SpeechRecognizer)
                    iflysr.iFlytekQuit();
                await IsCompleted.InvokeAsync();
            }
        }
        #endregion

        #region Recognizer Event Handle
        private void Recognizer_AudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
        }

        private void Recognizer_AudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            if (srt.Count <= 0) return;
            SRT title = null;
            if (_ReRecognize == -1 && srt.Count > 0)
                title = srt.Last();
            else
                title = _ReRecognize >= 0 && _ReRecognize < srt.Count ? srt[_ReRecognize] : null;

            ThirdPartyRecognizer(title);
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
                Log($"Processing : {_recognizer.RecognizerAudioPosition.ToString(@"hh\:mm\:ss\.fff")} - {AudioLength.ToString(@"hh\:mm\:ss\.fff")}");

                var index = _ReRecognize >= 0 ? _ReRecognize : IndexOf(e.Result.Audio.AudioPosition, e.Result.Audio.AudioPosition + e.Result.Audio.Duration);
                SRT title = index == -1 ? srt.Last() : srt[index];
                UpdateTitleInfo(title, e.Result);
                if (_ReRecognize < 0 && ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                {
                    ProgressHost.Report(Progress);
                }
                ThirdPartyRecognizer(title);
            }
        }

        private void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
#if DEBUG
                if (e.Result.Alternates.Count == 0)
                {
                    Console.WriteLine("Speech rejected. No candidate phrases found.");
                    return;
                }
                Console.WriteLine("Speech rejected. Did you mean:");
                foreach (RecognizedPhrase r in e.Result.Alternates)
                {
                    Console.WriteLine("    " + r.Text);
                }
#endif
                var index = IndexOf(e.Result.Audio.AudioPosition, e.Result.Audio.AudioPosition + e.Result.Audio.Duration);
                SRT title = index == -1 ? srt.Last() : srt[index];
                if (!string.IsNullOrEmpty(e.Result.Text))
                    UpdateTitleInfo(title, e.Result);
                ThirdPartyRecognizer(title);
            }
        }

        private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
            }
        }

        private void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            if (_ReRecognize < 0 && IndexOf(e.AudioPosition, e.AudioPosition, true) == -1)
            {
                var title = new SRT()
                {
                    Language = culture is CultureInfo ? culture.IetfLanguageTag : CultureInfo.CurrentCulture.IetfLanguageTag,
                    Index = srt.Count,
                    Start = e.AudioPosition,
                    End = e.AudioPosition,
                    NewStart = e.AudioPosition,
                    NewEnd = e.AudioPosition
                };
                srt.Add(title);
            }
        }

        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
                if (IsPausing)
                {
                    lastAudioPos = GetTime(_recognizerStream.Position, defaultWaveFmt);
                }
                else
                {
                    Log($"Completed: {_recognizer.RecognizerAudioPosition.ToString(@"hh\:mm\:ss\.fff")} - {AudioLength.ToString(@"hh\:mm\:ss\.fff")}");

                    if (_recognizerStream.Position >= _recognizerStream.Length)
                        AudioPos = AudioLength;

                    CheckCompleted(true);
                    //IsRunning = false;
                    //IsPausing = false;
                    //if (IsCompleted is Action) IsCompleted.InvokeAsync();
                }
                if (ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                    ProgressHost.Report(Progress);
            }
        }
        #endregion

        #region Third Party Recognizer
        private void ThirdPartyRecognizer(SRT title, bool force = false)
        {
            if (iFlyEnabledSDK && iflysr is iFly.SpeechRecognizer)
            {
                Recognizer_iFly(title, force);
            }
            else if (iFlyEnabledWebAPI && iflyIAT is iFly.iFlySpeechOnline)
            {
                Recognizer_iFlyOnline(title, force);
            }
            else if (AzureEnabled)
            {
                Recognizer_Azure(title, force);
            }
            else if (GoogleEnabled)
            {
                Recognizer_Google(title, force);
            }
            else
            {
                CheckCompleted(false);
            }
            //ThirdPartyTranslate(title, CultureInfo.CurrentCulture.IetfLanguageTag);
        }

        private void ThirdPartyTranslate(SRT title, string langDst = "zh-CN")
        {
            if (iFlyEnabledSDK && iflysr is iFly.SpeechRecognizer)
            {
                //
            }
            else if (iFlyEnabledWebAPI && iflyIAT is iFly.iFlySpeechOnline)
            {
                //
            }
            else if (AzureEnabled)
            {
                Translate_Azure(title, title.Language, langDst);
            }
            else if (GoogleEnabled)
            {
                //
            }
            else
            {
                CheckCompleted(false);
            }
        }

        private async void Recognizer_iFly(SRT title, bool force = false)
        {
            if (string.IsNullOrEmpty(APPID_iFlySpeech)) return;
            if (!(title is SRT)) return;
            if (!culture.IetfLanguageTag.StartsWith("zh", StringComparison.CurrentCultureIgnoreCase)) return;

            //await new Task(async () =>
            await new Action(async () =>
            {
                taskCount.AddCount();
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = force ? title.NewAudio : title.Audio;
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {
                        try
                        {
                            if (iflysr is iFly.SpeechRecognizer)
                            {
                                var ret = await iflysr.Recognizer(BA_AudioFile);
                                if (!string.IsNullOrEmpty(ret))
                                {
                                    if (ret.StartsWith("#"))
                                        title.Text += $" [{ret}]";
                                    else
                                        title.Text = ret;
                                }
                            }
                        }
#if DEBUG
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
                        catch (Exception) { }
#endif
                    }
                }
                CheckCompleted(true);
                //});
            }).InvokeAsync();
        }

        private async void Recognizer_iFlyOnline(SRT title, bool force = false)
        {
            if (string.IsNullOrEmpty(APPID_iFlySpeech)) return;
            if (!(title is SRT)) return;
            if (!culture.IetfLanguageTag.StartsWith("zh", StringComparison.CurrentCultureIgnoreCase)) return;

            if (!(iflyIAT is iFly.iFlySpeechOnline)) iflyIAT = new iFly.iFlySpeechOnline()
            {
                APPID = APPID_iFlySpeech,
                APIKey = APIKEY_iFlySpeech,
                APISecret = APISecret_iFlySpeech
            };

            await Task.Run(async () =>
            {
                taskCount.AddCount();
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = force ? title.NewAudio : title.Audio;
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {
                        try
                        {
                            if (iflyIAT is iFly.iFlySpeechOnline)
                            {
                                var t = await iflyIAT.Recognizer(BA_AudioFile);
                                //await iflyIAT.Disconnect();
                                if (!string.IsNullOrEmpty(t.Trim()))
                                    title.Text = t.Trim();
                                //await iflyIAT.Connect();
                                //title.Text = await iflyIAT.Recognizer(BA_AudioFile);
                                //await iflyIAT.Disconnect();
                            }
                        }
                        catch (Exception) { }
                    }
                }
                CheckCompleted(true);
            });
        }

        private async Task<string> Recognizer_AzureFetchToken(string url)
        {
            string resuilt = Token_AzureSpeech;
            if (string.IsNullOrEmpty(Token_AzureSpeech) || Token_AzureSpeech_Lifetime + TimeSpan.FromSeconds(600) < DateTime.Now)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKEY_AzureSpeech);
                        //client.DefaultRequestHeaders.Add("Content-type", "application/x-www-form-urlencoded");
                        //client.DefaultRequestHeaders.Add("Content-Length", "0");
                        UriBuilder uriToken = new UriBuilder(url);
                        var result = await client.PostAsync(uriToken.Uri.AbsoluteUri, null);
                        resuilt = await result.Content.ReadAsStringAsync();
                        Token_AzureSpeech_Lifetime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message);
                    }
                }
            }
            return (resuilt);
        }

        private async void Recognizer_Azure(SRT title, bool force = false)
        {
            if (string.IsNullOrEmpty(APIKEY_AzureSpeech)) return;
            if (!(title is SRT)) return;

            await Task.Run(async () =>
            {
                taskCount.AddCount();
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = force ? title.NewAudio : title.Audio;
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {
                        await ms.WriteAsync(BA_AudioFile, 0, BA_AudioFile.Length);
                        if (ms.Length > 0)
                        {
                            await ms.FlushAsync();
                            ms.Seek(0, SeekOrigin.Begin);
                            using (var client = new HttpClient())
                            {
                                try
                                {
                                    Token_AzureSpeech = await Recognizer_AzureFetchToken(URL_AzureSpeechToken);

                                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKEY_AzureSpeech);
                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(@"application/json"));
                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(@"text/xml"));
                                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token_AzureSpeech);

                                    var lang = Culture.IetfLanguageTag;
                                    var content = new StreamContent(ms);
                                    var contentType = new string[] { @"audio/wav", @"codecs=audio/pcm", @"samplerate=16000" };
                                    var ok = content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                                    var response = await client.PostAsync($"{URL_AzureSpeechResponse}?language={lang}", content);
                                    string stt_rest = await response.Content.ReadAsStringAsync();

                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        try
                                        {
                                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                                            var stt = serializer.Deserialize<AzureSpeechRecognizResult>(stt_rest);

                                            var text = stt.DisplayText;
                                            if (stt.RecognitionStatus.Equals("Success", StringComparison.CurrentCultureIgnoreCase))
                                                title.Text = text;
                                            else
                                                title.Text += $" [{text}]";
                                            Log(text);
                                            Translate_Azure(title, title.Language, CultureInfo.CurrentCulture.IetfLanguageTag);
                                        }
                                        catch (Exception) { }
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
                //if (_ReRecognize >= 0 && IsCompleted is Action)
                //{
                //    IsRunning = false;
                //    IsPausing = false;
                //    IsCompleted.InvokeAsync();
                //}
                CheckCompleted(true);
            });
        }

        private async Task<string> Translate_Azure(string text, string langSrc, string langDst = "zh-CN")
        {
            string result = "";

            if (string.IsNullOrEmpty(APIKEY_AzureTranslate) || langSrc.Equals(langDst) || string.IsNullOrEmpty(text))
            {
                //if (_ReRecognize >= 0 && IsCompleted is Action)
                //{
                //    IsRunning = false;
                //    IsPausing = false;
                //    IsCompleted.InvokeAsync();
                //}
                CheckCompleted(false);
                return (result);
            }

            try
            {
                taskCount.AddCount();

                var queryString = HttpUtility.ParseQueryString(string.Empty);
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
                var uri = $"{URL_AzureTranslate}?{queryString}";

                var content = new StringContent($"[{{'Text':'{text.Replace("'", "\\'").Replace("\"", "\\\"")}'}}]");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var client = new HttpClient();
                // Request headers
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKEY_AzureTranslate);

                HttpResponseMessage response = await client.PostAsync(uri, content);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string translate_result = await response.Content.ReadAsStringAsync();

                    JToken token = JToken.Parse( translate_result);
                    var Result_JSON = JsonConvert.SerializeObject(token, Formatting.Indented);
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
            catch (Exception) { }
            finally
            {
                //if (_ReRecognize >= 0 && IsCompleted is Action)
                //{
                //    IsRunning = false;
                //    IsPausing = false;
                //}
                CheckCompleted(true);
            }
            return (result);
        }

        private async void Translate_Azure(SRT title, string langSrc, string langDst = "zh-CN")
        {
            title.TranslatedText = await Translate_Azure(title.Text, langSrc, langDst);
        }

        private async void Recognizer_Google(SRT title, bool force = false)
        {
            if (string.IsNullOrEmpty(APIKEY_GoogleSpeech)) return;
            if (!(title is SRT)) return;

            taskCount.AddCount();

            await Task.Run(async () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = force ? title.NewAudio : title.Audio;
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {
                        WebRequest _S2T = null;
                        _S2T = WebRequest.Create($"https://www.google.com/speech-api/v2/recognize?output=json&lang=en-us&key={APIKEY_GoogleSpeech}");
                        _S2T.Credentials = CredentialCache.DefaultCredentials;
                        _S2T.Method = "POST";
                        _S2T.ContentType = "audio/wav; rate=16000";
                        _S2T.ContentLength = BA_AudioFile.Length;
                        using (Stream stream = await _S2T.GetRequestStreamAsync())
                        {
                            stream.Write(BA_AudioFile, 0, BA_AudioFile.Length);
                        }

                        HttpWebResponse _S2T_Response = (HttpWebResponse)_S2T.GetResponse();
                        if (_S2T_Response.StatusCode == HttpStatusCode.OK)
                        {
                            StreamReader SR_Response = new StreamReader(_S2T_Response.GetResponseStream());
                            var ret = await SR_Response.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(ret))
                            {
                                if (ret.StartsWith("#"))
                                    title.Text += $" [{ret}]";
                                else
                                    title.Text = ret;
                            }
                            Log(ret);
                        }
                    }
                }
                //if (_ReRecognize >= 0 && IsCompleted is Action)
                //{
                //    IsRunning = false;
                //    IsPausing = false;
                //    IsCompleted.InvokeAsync();
                //}
                CheckCompleted(true);
            });
        }
        #endregion

        #region Audio Converting Helper Routines
        private async Task<byte[]> GetBytes(RecognizedAudio audio)
        {
            byte[] result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (WaveFileWriter ws = new WaveFileWriter(ms, defaultWaveFmt))
                {
                    audio.WriteToWaveStream(ws);
                    result = new byte[ms.Length];
                    ms.Seek(0, SeekOrigin.Begin);
                    await ms.ReadAsync(result, 0, result.Length);
                    await ms.FlushAsync();
                }
            }
            return (result);
        }

        private async Task<byte[]> GetAudio(Stream stream, TimeSpan start, TimeSpan end)
        {
            byte[] result = null;

            using (MemoryStream ms = new MemoryStream())
            {
                if (stream == null) stream = _recognizerStream;

                var pos = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);
                using (WaveStream rws = new RawSourceWaveStream(stream, defaultWaveFmt))
                {
                    try
                    {
                        rws.CurrentTime = end;
                        var idx_e = rws.Position;
                        rws.CurrentTime = start;
                        var idx_s = rws.Position;
                        var len = idx_e - idx_s;

                        var ws_buf = new byte[len];
                        await rws.ReadAsync(ws_buf, 0, (int)len);

                        ms.Seek(0, SeekOrigin.Begin);
                        using (WaveFileWriter wws = new WaveFileWriter(ms, defaultWaveFmt))
                        {
                            await wws.WriteAsync(ws_buf, 0, ws_buf.Length);
                            await wws.FlushAsync();
                            ms.Seek(0, SeekOrigin.Begin);
                            result = new byte[ms.Length];
                            await ms.ReadAsync(result, 0, result.Length);
                            await ms.FlushAsync();
                        }
                    }
#if DEBUG
                    catch (Exception ex) { Log(ex.Message); }
#else
                        catch (Exception) { }
#endif
                    finally { stream.Seek(pos, SeekOrigin.Begin); }
                }
            }

            return (result);
        }

        private async Task<byte[]> GetPcmBytes(byte[] audio, WaveFormat fmt)
        {
            byte[] result = null;
            if (audio is byte[])
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    await ms.WriteAsync(audio, 0, audio.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (WaveFileReader reader = new WaveFileReader(ms))
                    {
                        var wave = new WaveFormatConversionStream(defaultWaveFmt, reader);
                        wave.CurrentTime = TimeSpan.FromSeconds(0);
                        wave.Seek(0, SeekOrigin.Begin);
                        result = new byte[wave.Length];
                        await wave.ReadAsync(result, 0, result.Length);
                        await wave.FlushAsync();
                    }
                }
            }
            return (result);
        }

        private async Task<Stream> GetStream(byte[] bytes)
        {
            MemoryStream result = new MemoryStream();
            if (bytes is byte[])
            {
                await result.WriteAsync(bytes, 0, bytes.Length);
                await result.FlushAsync();
                result.Seek(0, SeekOrigin.Begin);
            }
            return (result);
        }

        private async Task<WaveStream> GetWaveStreamFromPcmBytes(byte[] audio, WaveFormat fmt)
        {
            WaveStream result = null;

            MemoryStream mso = new MemoryStream();
            WaveFileWriter writer = new WaveFileWriter(mso, fmt);
            await writer.WriteAsync(audio, 0, audio.Length);
            await writer.FlushAsync();
            mso.Seek(0, SeekOrigin.Begin);
            WaveFileReader reader = new WaveFileReader(mso);
            result = new WaveFormatConversionStream(fmt, reader);
            result.CurrentTime = TimeSpan.FromSeconds(0);
            result.Seek(0, SeekOrigin.Begin);
            await result.FlushAsync();

            return (result);
        }

        private async Task<WaveStream> GetWaveStream(MemoryStream audio, WaveFormat fmt)
        {
            WaveStream result = null;
            var pos = audio.Position;
            var bytes = new byte[audio.Length];
            await audio.ReadAsync(bytes, 0, bytes.Length);
            await audio.FlushAsync();
            result = await GetWaveStreamFromPcmBytes(bytes, fmt);
            audio.Seek(pos, SeekOrigin.Begin);
            return (result);
        }

        private async Task<MemoryStream> GetMemoryStream(WaveStream stream)
        {
            MemoryStream result = new MemoryStream();
            try
            {
                byte[] buf = new byte[stream.Length];
                //stream.Seek(0, SeekOrigin.Begin);
                await stream.ReadAsync(buf, 0, buf.Length);
                await result.WriteAsync(buf, 0, buf.Length);
                await result.FlushAsync();
                result.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception) { }
            return (result);
        }
        #endregion

        #region Audio Time Index Helper Routines
        private TimeSpan GetTime(long pos, WaveFormat fmt)
        {
            TimeSpan result = TimeSpan.FromSeconds(0);

            if (_recognizerStream is MemoryStream && _recognizerStream.Length > 0)
            {
                var sBytes = fmt.SampleRate * fmt.BitsPerSample * fmt.Channels / 8;
                result = TimeSpan.FromSeconds((double)pos / (double)sBytes);
            }

            return (result);
        }

        private int IndexOf(TimeSpan start, TimeSpan end, bool fuzzy = false)
        {
            int result = -1;

            foreach (var s in srt)
            {
                if (s.Start == start && (fuzzy || s.End == end))
                {
                    result = s.Index;
                    break;
                }
            }
            return (result);
        }

        private SRT FindSRT(TimeSpan start, TimeSpan end, bool fuzzy = false)
        {
            SRT result = null;
            var idx = IndexOf(start, end, fuzzy);
            if (idx >= 0 && idx < srt.Count) result = srt[idx];
            return (result);
        }

        private async void UpdateTitleInfo(SRT title, RecognitionResult result)
        {
            if (title is SRT)
            {
                title.Start = lastAudioPos + result.Audio.AudioPosition;
                title.End = title.Start + result.Audio.Duration;
                title.Text = result.Text.Trim();
                title.SetAltText(result.Alternates);
                title.Audio = await GetBytes(result.Audio);
                title.NewStart = title.Start;
                title.NewEnd = title.End;

                AudioPos = title.End;
            }
        }
        #endregion

        #region SubTitles I/O Helper Routines
        public string ToSRT()
        {
            return (Text);
        }

        public string ToASS()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[Script Info]");
            sb.AppendLine($"Title: {Title}");
            sb.AppendLine();
            sb.AppendLine($"[V4+ Styles]");
            sb.AppendLine($"Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            //sb.AppendLine($"Style: Default,Tahoma,24,&H00FFFFFF,&HF0000000,&H00000000,&HF0000000,1,0,0,0,100,100,0,0.00,1,1,0,2,30,30,10,1");
            sb.AppendLine($"{DefaultStyle.CHS_Default}");
            sb.AppendLine($"{DefaultStyle.CHS_Note}");
            sb.AppendLine($"{DefaultStyle.CHS_Title}");
            sb.AppendLine();
            sb.AppendLine($"[Events]");
            sb.AppendLine($"Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var t in srt)
            {
                var text = t.MultiLingoText.Replace("\r\n", "\\N").Replace("\n\r", "\\N").Replace("\r", "\\N").Replace("\n", "\\N");
                sb.AppendLine($"Dialogue: 0,{t.NewStart.ToString(@"hh\:mm\:ss\.ff")},{t.NewEnd.ToString(@"hh\:mm\:ss\.ff")},Default,NTP,0000,0000,0000,,{text}");
            }

            return (sb.ToString());
        }

        public string ToSSA()
        {
            return (ToASS());
        }

        public string ToLRC()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[ti]{Title}");
            sb.AppendLine();
            foreach (var t in srt)
            {

                sb.AppendLine($"[{t.NewStart.ToString(@"hh\:mm\:ss\.fff")}] {t.MultiLingoText}");
                sb.AppendLine($"[{t.NewEnd.ToString(@"hh\:mm\:ss\.fff")}]");
            }

            return (sb.ToString());
        }

        private SRT MakeSRT(AudioSlice slice, int idx = 0)
        {
            var title = new SRT()
            {
                Index = idx,
                Language = culture.IetfLanguageTag,
                Audio = null,
                Start = slice.Start,
                End = slice.End,
                NewAudio = null,
                NewStart = slice.Start,
                NewEnd = slice.End
            };
            return (title);
        }

        public void LoadSlice(IEnumerable<AudioSlice> slices, bool active = true)
        {
            var selected = slices.Where(o => o.IsActive == active);
            srt.Clear();
            foreach (var s in selected)
            {
                srt.Add(MakeSRT(s, srt.Count));
            }

        }

        public async void LoadSRT(string file)
        {
            if (File.Exists(file))
            {
                var contents = File.ReadAllLines(file);
                if (contents.Length > 3)
                {
                    srt.Clear();
                    var index = -1;
                    var text = string.Empty;
                    TimeSpan start = default(TimeSpan);
                    TimeSpan end = default(TimeSpan);
                    string line = string.Empty;

                    for (var i = 0; i < contents.Length; i++)
                    {
                        try
                        {
                            if (Regex.IsMatch(contents[i], @"^\d+$"))
                            {
                                line = contents[i];
                                index = int.Parse((line.Trim())) - 1;
                                i++;

                                line = contents[i];
                                var time = Regex.Replace(line, @"-->", "-", RegexOptions.IgnoreCase).Split('-');
                                start = TimeSpan.Parse(time[0].Trim().Replace(",", "."));
                                end = TimeSpan.Parse(time[1].Trim().Replace(",", "."));
                            }
                            else if (index >= 0 && string.IsNullOrEmpty(contents[i].Trim()))
                            {
                                var title = new SRT()
                                {
                                    Index = index,
                                    Language = culture.IetfLanguageTag,
                                    Audio = null,
                                    Start = start,
                                    End = end,
                                    NewAudio = null,
                                    NewStart = start,
                                    NewEnd = end,
                                    Text = text.Trim()
                                };
                                srt.Add(title);
                                index = -1;
                                text = string.Empty;
                                start = default(TimeSpan);
                                end = default(TimeSpan);
                            }
                            else if (index >= 0)
                            {
                                text += $"\r\n{contents[i].Trim()}";
                            }
                        }
                        catch (Exception) { }
                        if (srt.Count < 100) await Task.Delay(1);
                    }
                }
            }
        }

        public async void LoadCSV(string file)
        {
            if (File.Exists(file))
            {
                var contents = File.ReadAllLines(file);
                if (contents.Length > 2)
                {
                    srt.Clear();
                    foreach (var line in contents.Skip(1))
                    {
                        try
                        {
                            var content = line.Split(',');
                            if (content.Length >= 4)
                            {
                                var title = new SRT()
                                {
                                    Index = int.Parse(content[0].Trim()) - 1,
                                    Language = culture.IetfLanguageTag,
                                    Text = content[3].Trim(),
                                    Audio = null,
                                    Start = TimeSpan.Parse(content[1].Trim()),
                                    End = TimeSpan.Parse(content[2].Trim()),
                                    NewAudio = null,
                                    NewStart = TimeSpan.Parse(content[1].Trim()),
                                    NewEnd = TimeSpan.Parse(content[2].Trim())
                                };
                                srt.Add(title);
                            }
                        }
                        catch (Exception) { }
                        if (srt.Count < 100) await Task.Delay(1);
                    }
                }
            }
        }
        #endregion

        #region Audio Processing Helper Routines
        private async Task<SpeechAudioFormatInfo> LoadAudio(string audiofile)
        {
            SpeechAudioFormatInfo audioInfo = null;
            try
            {
                using (MediaFoundationReader reader = new MediaFoundationReader(audiofile))
                {
                    //using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                    using (WaveStream pcmStream = new WaveFormatConversionStream(defaultWaveFmt, reader))
                    {
                        //WaveFileWriter.CreateWaveFile("_test_.wav", pcmStream);

                        IsRunning = false;
                        IsPausing = false;

                        var fmt = pcmStream.WaveFormat;
                        if (iflysr is iFly.SpeechRecognizer)
                        {
                            iflysr.SampleRate = fmt.SampleRate;
                            iflysr.BitsPerSample = fmt.BitsPerSample;
                        }

                        AudioPos = TimeSpan.FromSeconds(0);
                        AudioLength = reader.TotalTime;
                        lastAudioPos = AudioPos;

                        audioInfo = new SpeechAudioFormatInfo(EncodingFormat.Pcm, fmt.SampleRate, fmt.BitsPerSample, fmt.Channels, fmt.AverageBytesPerSecond, fmt.BlockAlign, new byte[fmt.ExtraSize]);

                        _recognizerStream = new MemoryStream();
                        byte[] buf = new byte[pcmStream.Length];
                        pcmStream.Read(buf, 0, buf.Length);
                        await _recognizerStream.WriteAsync(buf, 0, buf.Length);
                        await _recognizerStream.FlushAsync();
                        _recognizerStream.Seek(0, SeekOrigin.Begin);

                        ProgressHost.Report(Progress);
                    }
                }
            }
            catch (Exception)
            {
                if (_recognizerStream is MemoryStream)
                    _recognizerStream.Dispose();
                _recognizerStream = null;
            }
            return (audioInfo);
        }

        private void InitAudio()
        {
            if (_recognizerStream is MemoryStream)
            {
                if (_audioInfo is SpeechAudioFormatInfo && _recognizerStream.Length > 0)
                {
                    AudioPos = TimeSpan.FromSeconds(0);
                    lastAudioPos = AudioPos;

                    _recognizerStream.Seek(0, SeekOrigin.Begin);
                    //_recognizer.SetInputToAudioStream();
                    //_recognizer.SetInputToAudioStream(_recognizerStream, _audioInfo);
                    srt.Clear();
                }
            }
        }

        private static bool IsSilence(float amplitude, sbyte threshold)
        {
            double dB = 20 * Math.Log10(Math.Abs(amplitude));
            return dB < threshold;
        }

        private static bool IsSilence(byte[] amplitude, sbyte threshold, WaveFormat fmt)
        {
            double amp = 0;
            var step = fmt.BitsPerSample / 8;
            for (var i = 0; i < amplitude.Length; i += step)
            {
                double a = BitConverter.ToInt16(amplitude, i) / 32768.0;
                amp += Math.Abs(a);
            }
            amp = amp * step / amplitude.Length;
            if (amp == 0) amp = 0.000001;
            double dB = 20 * Math.Log10(Math.Abs(amp));
            return dB < threshold;
        }

        private static bool IsSilence(byte[] amplitude, int index, int count, int step, sbyte threshold)
        {
            double amp = 0;
            for (var i = index; i < index + count; i += step)
            {
                double a = BitConverter.ToInt16(amplitude, i) / 32768.0;
                amp += Math.Abs(a);
            }
            amp = amp * step / amplitude.Length;
            if (amp == 0) amp = 0.000001;
            double dB = 20 * Math.Log10(Math.Abs(amp));
            return dB < threshold;
        }

        public async Task<List<AudioSlice>> SliceAudio(sbyte Threshold = -40, double activeDuration = 0.7500, double silenceDuration = 0.7500, bool? active = null)
        {
            List<AudioSlice> result = new List<AudioSlice>();
            try
            {
                await new Action(async () =>
                {
                    IsRunning = true;
                    IsPausing = false;
                    taskCount.AddCount();

                    if (active != null) srt.Clear();
                    var wave = _recognizerStream.ToArray();
                    var TotalTime = GetTime(wave.Length - 1, defaultWaveFmt);
                    AudioLength = TotalTime;
                    activeDuration = Math.Round(activeDuration, 3);
                    TimeSpan activeDurationTime = TimeSpan.FromSeconds(activeDuration);
                    TimeSpan silenceDurationTime = TimeSpan.FromSeconds(silenceDuration);
                    bool silence = false;
                    int sample = defaultWaveFmt.SampleRate * defaultWaveFmt.BitsPerSample * defaultWaveFmt.Channels / 8 / 10;
                    int step = defaultWaveFmt.BitsPerSample / 8;
                    AudioSlice slice = new AudioSlice();
                    for (var i = 0; i < wave.Length; i += sample)
                    {
                        var CurrentTime = GetTime(i, defaultWaveFmt);
                        var count = i + sample > wave.Length ? wave.Length - i : sample;
                        //var iss = IsSilence(wave, i, count, step, Threshold);
                        byte[] samples = new byte[sample];
                        Array.Copy(wave, i, samples, 0, count);
                        var iss = IsSilence(samples, Threshold, defaultWaveFmt);
                        //var iss = IsSilence(wave.Skip(i).Take(sample).ToArray(), Threshold, defaultWaveFmt);
                        if (result.Count == 0) silence = !iss;
                        if (silence != iss && (slice.End - slice.Start >= (silence ? silenceDurationTime : activeDurationTime)))
                        {
                            slice.IsActive = !silence;
                            result.Add(slice);
                            if (active != null && active.Value == slice.IsActive)
                                srt.Add(MakeSRT(slice, srt.Count));
                            AudioPos = slice.Start;
                            ProgressHost.Report(Progress);
                            Log($"Processing : {slice.Start.ToString(@"hh\:mm\:ss\.fff")} - {slice.End.ToString(@"hh\:mm\:ss\.fff")}");
                            await Task.Delay(1);

                            slice = new AudioSlice()
                            {
                                Start = CurrentTime,
                                End = CurrentTime
                            };
                            silence = iss;
                        }
                        slice.End = CurrentTime;
                        //Log($"Processing : {slice.Start.ToString(@"hh\:mm\:ss\.fff")} - {slice.End.ToString(@"hh\:mm\:ss\.fff")}");
                    }

                    if (slice is AudioSlice)
                    {
                        slice.End = GetTime(wave.Length - 1, defaultWaveFmt);
                        result.Add(slice);
                        if (active != null && active.Value == slice.IsActive)
                            srt.Add(MakeSRT(slice, srt.Count));
                        AudioPos = TotalTime;
                        ProgressHost.Report(Progress);
                        Log($"Processing : {slice.Start.ToString(@"hh\:mm\:ss\.fff")} - {slice.End.ToString(@"hh\:mm\:ss\.fff")}");
                        await Task.Delay(1);
                    }
                    CheckCompleted(true);
                }).InvokeAsync();
            }
#if DEBUG
            catch (Exception ex) { Log(ex.Message); }
#else
            catch (Exception) { }
#endif
            return (result);
        }
        #endregion

        #region Recognizer Helper routines
        private async void Recognizer(string audiofile, bool restart = false)
        {
            _ReRecognize = -1;
            if (_recognizer is SpeechRecognitionEngine)
            {
                if (File.Exists(audiofile))
                {
                    taskCount.AddCount();

                    if (restart || !IsRunning)
                    {
                        IsRunning = true;
                        IsPausing = false;

                        if (_recognizerStream == null || _recognizerStream.Length <= 0)
                            _audioInfo = await LoadAudio(audiofile);

                        InitAudio();
                    }
                    if (_recognizerStream is MemoryStream)
                    {
                        if (_recognizerStream.Position >= _recognizerStream.Length)
                        {
                            InitAudio();
                        }
                        //if(iFlyEnabledSDK && iflyIAT is iFly.iFlySpeechOnline)
                        //{
                        //    iflyIAT.Results.Clear();
                        //    await iflyIAT.Connect();
                        //}
                        _recognizer.SetInputToAudioStream(_recognizerStream, _audioInfo);
                        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    }
                }
            }
        }

        private async void Recognizer(SRT title, bool force = false)
        {
            if (_recognizer is SpeechRecognitionEngine && title is SRT)
            {
                IsRunning = true;
                IsPausing = false;

                if (force)
                {
                    _ReRecognize = title.Index;
                    title.NewAudio = await GetAudio(_recognizerStream, title.NewStart, title.NewEnd);
                    ThirdPartyRecognizer(title, force);
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            taskCount.AddCount();
                            _ReRecognize = title.Index;
                            var audio = title.Audio == null ? await GetAudio(_recognizerStream, title.NewStart, title.NewEnd) : title.Audio;
                            var bytes = await GetPcmBytes(audio, defaultWaveFmt);
                            var pcm = await GetStream(bytes);
                            if (pcm is Stream && pcm.Length > 0)
                            {
                                _recognizer.SetInputToAudioStream(pcm, _audioInfo);
                                _recognizer.Recognize();
                            }
                            ThirdPartyRecognizer(title);
                            CheckCompleted(true);
                        }
                        catch (Exception) { }
                    });
                }
            }
        }

        public void Translate(SRT title, string langDst = "zh-CN")
        {
            _ReRecognize = title.Index;
            ThirdPartyTranslate(title, langDst);
        }

        public void Translate(IEnumerable<SRT> titles, string langDst = "zh-CN")
        {
            _ReRecognize = -1;
            foreach (var srt in titles)
            {
                Translate(srt, langDst);
            }
            CheckCompleted(false);
        }
        #endregion

        #region Recognizer Control routines
        public bool IsRunning { get; private set; } = false;
        public void Start(string audiofile = default(string))
        {
            if (IsRunning && IsPausing)
            {
                Resume();
            }
            else
            {
                if (!string.IsNullOrEmpty(audiofile))
                    AudioFile = audiofile;

                if (!string.IsNullOrEmpty(AudioFile))
                    Recognizer(AudioFile);
            }
        }

        public void Start(SRT title, bool force = false)
        {
            if (IsRunning) return;
            Recognizer(title, force);
        }

        private bool is_pausing = false;
        public bool IsPausing
        {
            get { return (is_pausing && IsRunning); }
            private set { is_pausing = IsRunning && value; }
        }
        public void Pause()
        {
            if (_recognizer is SpeechRecognitionEngine)
            {
                if (IsRunning && !IsPausing)
                {
                    IsPausing = true;
                    _recognizer.RecognizeAsyncStop();
                }
            }
        }

        public void Resume()
        {
            if (_recognizer is SpeechRecognitionEngine)
            {
                if (IsRunning && IsPausing)
                {
                    IsPausing = false;
                    Recognizer(AudioFile);
                }
            }
        }

        public void Stop()
        {
            if (_recognizer is SpeechRecognitionEngine)
            {
                IsRunning = false;
                IsPausing = false;
                _recognizer.RecognizeAsyncStop();
            }
            CheckCompleted(false);
        }
        #endregion

        #region SpeechRecognizer routines
        public SpeechRecognizer(CultureInfo culture = default(CultureInfo))
        {
            try
            {
                MediaFoundationApi.Startup();

                taskCount.Reset(1);

                #region Microsoft SAPI Recognition
                foreach (var ri in InstalledRecognizers)
                {
                    var r = new SpeechRecognitionEngine(ri.Culture);
                    r.InitialSilenceTimeout = TimeSpan.FromMilliseconds(50);
                    r.EndSilenceTimeout = TimeSpan.FromMilliseconds(50);
                    r.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(50);
                    r.BabbleTimeout = TimeSpan.FromMilliseconds(50);
                    r.LoadGrammar(new DictationGrammar() { Name = "Dictation Grammar" });
                    r.SpeechDetected += Recognizer_SpeechDetected;
                    r.SpeechRecognized += Recognizer_SpeechRecognized;
                    r.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
                    r.SpeechHypothesized += Recognizer_SpeechHypothesized;
                    r.RecognizeCompleted += Recognizer_RecognizeCompleted;
                    r.AudioSignalProblemOccurred += Recognizer_AudioSignalProblemOccurred;
                    r.AudioStateChanged += Recognizer_AudioStateChanged;
                    _recognizers.Add(ri.Culture.Name, r);
                }

                if (culture is CultureInfo)
                    _defaultRecognizer = new SpeechRecognitionEngine(culture);
                else if (InstalledRecognizers.Count > 0)
                    _defaultRecognizer = new SpeechRecognitionEngine(CultureInfo.CurrentCulture);
                else
                    _defaultRecognizer = new SpeechRecognitionEngine();
                _defaultRecognizer.InitialSilenceTimeout = TimeSpan.FromMilliseconds(50);
                _defaultRecognizer.BabbleTimeout = TimeSpan.FromMilliseconds(50);

                Grammar dictation = new DictationGrammar();
                dictation.Name = "Dictation Grammar";
                _defaultRecognizer.LoadGrammar(dictation);
                //_recognizer.SetInputToDefaultAudioDevice();
                _defaultRecognizer.SpeechDetected += Recognizer_SpeechDetected;
                _defaultRecognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _defaultRecognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
                _defaultRecognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
                _defaultRecognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
                _defaultRecognizer.AudioSignalProblemOccurred += Recognizer_AudioSignalProblemOccurred;
                _defaultRecognizer.AudioStateChanged += Recognizer_AudioStateChanged;

                _recognizer = _defaultRecognizer;
                #endregion

                #region iFlyTek Speech Recognition
                APPID_iFlySpeech = File.Exists(APPID_iFlySpeech_File) ? File.ReadAllText(APPID_iFlySpeech_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APPID_iFlySpeech))
                {
                    if (File.Exists(iFly.MscDLL.DllName))
                    {
                        iflysr = new iFly.SpeechRecognizer()
                        {
                            APPID = $"{APPID_iFlySpeech}",
                            AppDispather = Application.Current.Dispatcher,
                            iFlyIsCompleted = new Action(() =>
                            {
                                //if (IsCompleted is Action) IsCompleted.InvokeAsync();
                                CheckCompleted(false);
                            })
                        };
                    }
                }
                APIKEY_iFlySpeech = File.Exists(APIKEY_iFlySpeech_File) ? File.ReadAllText(APIKEY_iFlySpeech_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APIKEY_iFlySpeech))
                {
                    var kv = APIKEY_iFlySpeech.Split('=');
                    if (kv.Length >= 2)
                    {
                        APIKEY_iFlySpeech = kv[0].Trim();
                        APISecret_iFlySpeech = kv[1].Trim();
                    }
                    if (!string.IsNullOrEmpty(APPID_iFlySpeech) && !string.IsNullOrEmpty(APIKEY_iFlySpeech) && !string.IsNullOrEmpty(APISecret_iFlySpeech))
                    {
                        iflyIAT = new iFly.iFlySpeechOnline()
                        {
                            APPID = APPID_iFlySpeech,
                            APIKey = APIKEY_iFlySpeech,
                            APISecret = APISecret_iFlySpeech
                        };
                    }
                }
                #endregion

                #region Azure Cognitive Service
                APIKEY_AzureSpeech = File.Exists(APIKEY_AzureSpeech_File) ? File.ReadAllText(APIKEY_AzureSpeech_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APIKEY_AzureSpeech))
                {
                    var kv = APIKEY_AzureSpeech.Split('=');
                    if (kv.Length >= 2)
                    {
                        APIKEY_AzureSpeech = kv[0].Trim();
                        URL_AzureSpeechToken = string.Join("=", kv.Skip(1)).Trim();
                        UriBuilder ubt = new UriBuilder(URL_AzureSpeechToken);
                        UriBuilder ubr = new UriBuilder(URL_AzureSpeechResponse);
                        ubr.Host = ubt.Host.Replace("api.cognitive.microsoft.com", "stt.speech.microsoft.com");
                        URL_AzureSpeechResponse = ubr.Uri.AbsoluteUri;
                    }
                    Task.Run(async () =>
                    {
                        Token_AzureSpeech = await Recognizer_AzureFetchToken(URL_AzureSpeechToken);
                    });
                }

                APIKEY_AzureTranslate = File.Exists(APIKEY_AzureTranslate_File) ? File.ReadAllText(APIKEY_AzureTranslate_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APIKEY_AzureTranslate))
                {
                    var kv = APIKEY_AzureTranslate.Split('=');
                    if (kv.Length >= 2)
                    {
                        APIKEY_AzureTranslate = kv[0];
                        URL_AzureTranslate = string.Join("", kv.Skip(1));
                    }
                }
                #endregion

                #region Google Speech Recognize
                APIKEY_GoogleSpeech = File.Exists(APIKEY_GoogleSpeech_File) ? File.ReadAllText(APIKEY_GoogleSpeech_File).Trim() : string.Empty;
                #endregion
            }
            catch (Exception)
            {
                _recognizer = null;
                MessageBox.Show("Speech Recognizer not INSTALLED!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        ~SpeechRecognizer()
        {
            if (_recognizer is SpeechRecognitionEngine) _recognizer.Dispose();
        }
        #endregion
    }

    public class AzureSpeechRecognizResult
    {
        public string RecognitionStatus;
        public string DisplayText;
        public string Offset;
        public string Duration;
    }
}
