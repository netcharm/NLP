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
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SoundToText
{
    public class SpeechRecognizer
    {
        #region Recognizer Internal Variable
        public static List<RecognizerInfo> InstalledRecognizers { get; } = SpeechRecognitionEngine.InstalledRecognizers().ToList();
        private Dictionary<string, SpeechRecognitionEngine> _recognizers = new Dictionary<string, SpeechRecognitionEngine>();
        private SpeechRecognitionEngine _defaultRecognizer = null;
        private SpeechRecognitionEngine _recognizer = null;
        private WaveFormat defaultWaveFmt = new WaveFormat(16000, 16, 1);

        private MemoryStream _recognizerStream = null;
        private byte[] _recognizerBuffer = null;

        private string APPID_iFly = string.Empty;
        private string APPID_iFly_File = "appid_ifly.txt";
        iFly.SpeechRecognizer iflysr = null;

        private string APIKEY_Google = string.Empty;
        private string APIKEY_Google_File = "apikey_google.txt";

        private string APIKEY_Azure = string.Empty;
        private string APIKEY_Azure_File = "apikey_azure.txt";
        //private string URL_AzureToken = "https://eastasia.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        //private string URL_AzureResponse = "https://eastasia.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
        private string URL_AzureToken = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private string URL_AzureResponse = "https://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
        private string Token_Azure = string.Empty;
        private DateTime Token_Lifetime = DateTime.Now;
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
                        sb.AppendLine($"{s.Index}");
                        sb.AppendLine($"{s.Start.ToString(@"hh\:mm\:ss\,fff")} --> {s.End.ToString(@"hh\:mm\:ss\,fff")}");
                        sb.AppendLine($"{s.Text}");
                        sb.AppendLine();
                    }
                    return (sb.ToString());
                }
                else return (string.Empty);
            }
        }

        private int _ReRecognize = -1;
        private int _ForceRecognize = -1;

        public bool SAPIEnabled { get; set; } = true;
        public bool iFlyEnabled { get; set; } = false;
        public bool AzureEnabled { get; set; } = false;
        public bool GoogleEnabled { get; set; } = false;
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

            _recognizerBuffer = title.Audio;
            ThirdPartyRecognizer(title, null);
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
                Log($"Processing : {_recognizer.RecognizerAudioPosition.ToString(@"hh\:mm\:ss\.fff")} - {AudioLength.ToString(@"hh\:mm\:ss\.fff")}");

                SRT title = null;
                if (_ReRecognize == -1)
                {
                    title = srt.Last();
                    UpdateTitleInfo(title, e.Result);

                    AudioPos = title.Start;
                    if (ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                    {
                        ProgressHost.Report(Progress);
                    }
                }
                else
                    title = _ReRecognize >= 0 && _ReRecognize < srt.Count ? srt[_ReRecognize] : null;

                ThirdPartyRecognizer(title, e);
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
                SRT title = null;
                if (_ReRecognize == -1 && srt.Count > 0)
                    title = srt.Last();
                else
                    title = _ReRecognize >= 0 && _ReRecognize < srt.Count ? srt[_ReRecognize] : null;

                UpdateTitleInfo(title, e.Result);
                _recognizerBuffer = title.Audio;
                ThirdPartyRecognizer(title, null);
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
            if (_ReRecognize == -1)
            {
                var title = new SRT()
                {
                    Index = srt.Count + 1,
                    Start = e.AudioPosition,
                    End = e.AudioPosition
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
                    AudioPos = lastAudioPos + e.Result.Audio.AudioPosition + e.Result.Audio.Duration;
                    lastAudioPos = AudioPos;
                }
                else
                {
                    Log($"Completed: {_recognizer.RecognizerAudioPosition.ToString(@"hh\:mm\:ss\.fff")} - {AudioLength.ToString(@"hh\:mm\:ss\.fff")}");

                    if (_recognizerStream.Position >= _recognizerStream.Length)
                        AudioPos = AudioLength;

                    IsRunning = false;
                    IsPausing = false;
                    if (IsCompleted is Action) IsCompleted.InvokeAsync();
                }
                if (ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                    ProgressHost.Report(Progress);
            }
            //_recognizerPause.Release();
            //_recognizerRunning.Release();
        }
        #endregion

        #region Third Party Recognizer
        private void ThirdPartyRecognizer(SRT title, SpeechRecognizedEventArgs e)
        {
            if (iFlyEnabled && iflysr is iFly.SpeechRecognizer)
            {
                Recognizer_iFly(title, e);
            }
            else if (AzureEnabled)
            {
                Recognizer_Azure(title, e);
            }
            else if (GoogleEnabled)
            {
                Recognizer_Google(title, e);
            }
            else
            {
                if (IsCompleted is Action) IsCompleted.InvokeAsync();
            }            
        }

        private async void Recognizer_iFly(SRT title, SpeechRecognizedEventArgs e)
        {
            if (string.IsNullOrEmpty(APPID_iFly)) return;
            if (!(title is SRT)) return;
            if (!culture.IetfLanguageTag.StartsWith("zh", StringComparison.CurrentCultureIgnoreCase)) return;

            await Task.Run(async () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = _recognizerBuffer;
                    if (e is SpeechRecognizedEventArgs)
                    {
                        e.Result.Audio.WriteToWaveStream(ms);
                        await ms.FlushAsync();
                        ms.Seek(0, SeekOrigin.Begin);
                        BA_AudioFile = ms.GetBuffer();
                    }
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {
                        var ret = await iflysr.Recognizer(BA_AudioFile);
                        //title.Text += $" [{ret}]";
                        if (!string.IsNullOrEmpty(ret))
                        {
                            if (ret.StartsWith("#"))
                                title.Text += $" [{ret}]";
                            else
                                title.Text = ret;
                        }
                    }
                }
                if (IsCompleted is Action) IsCompleted.InvokeAsync(); ;
            });
        }

        private async Task<string> Recognizer_AzureFetchToken(string url)
        {
            string resuilt = Token_Azure;
            if (string.IsNullOrEmpty(Token_Azure) || Token_Lifetime + TimeSpan.FromSeconds(600) < DateTime.Now)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKEY_Azure);
                        //client.DefaultRequestHeaders.Add("Content-type", "application/x-www-form-urlencoded");
                        //client.DefaultRequestHeaders.Add("Content-Length", "0");
                        UriBuilder uriToken = new UriBuilder(url);
                        var result = await client.PostAsync(uriToken.Uri.AbsoluteUri, null);
                        resuilt = await result.Content.ReadAsStringAsync();
                        Token_Lifetime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message);
                    }
                }
            }
            return (resuilt);
        }

        private async void Recognizer_Azure(SRT title, SpeechRecognizedEventArgs e)
        {
            if (string.IsNullOrEmpty(APIKEY_Azure)) return;
            if (!(title is SRT)) return;

            await Task.Run(async () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    if (e is SpeechRecognizedEventArgs)
                    {
                        e.Result.Audio.WriteToWaveStream(ms);
                    }
                    else if (_recognizerBuffer is byte[] && _recognizerBuffer.Length > 0)
                    {
                        await ms.WriteAsync(_recognizerBuffer, 0, _recognizerBuffer.Length);
                    }
                    if (ms.Length > 0)
                    {
                        await ms.FlushAsync();
                        ms.Seek(0, SeekOrigin.Begin);
                        using (var client = new HttpClient())
                        {
                            try
                            {
                                Token_Azure = await Recognizer_AzureFetchToken(URL_AzureToken);

                                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIKEY_Azure);
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(@"application/json"));
                                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(@"text/xml"));
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token_Azure);

                                var lang = Culture.IetfLanguageTag;
                                var content = new StreamContent(ms);
                                var contentType = new string[] { @"audio/wav", @"codecs=audio/pcm", @"samplerate=16000" };
                                var ok = content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                                var response = await client.PostAsync($"{URL_AzureResponse}?language={lang}", content);
                                string stt_rest = await response.Content.ReadAsStringAsync();

                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    try
                                    {
                                        System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                                        var stt = serializer.Deserialize<AzureSpeechRecognizResult>(stt_rest);

                                        var text = stt.DisplayText;
                                        if (stt.RecognitionStatus.Equals("Success", StringComparison.CurrentCultureIgnoreCase))
                                            title.Text = text;
                                        else
                                            title.Text += $" [{text}]";
                                        Log(text);
                                    }
                                    catch (Exception) { }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                if (IsCompleted is Action) IsCompleted.InvokeAsync();
            });
        }

        private async void Recognizer_Google(SRT title, SpeechRecognizedEventArgs e)
        {
            if (string.IsNullOrEmpty(APIKEY_Google)) return;
            if (!(title is SRT)) return;

            await Task.Run(async () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] BA_AudioFile = _recognizerBuffer;
                    if (e is SpeechRecognizedEventArgs)
                    {
                        e.Result.Audio.WriteToWaveStream(ms);
                        await ms.FlushAsync();
                        ms.Seek(0, SeekOrigin.Begin);
                        BA_AudioFile = ms.GetBuffer();
                    }
                    if (BA_AudioFile is byte[] && BA_AudioFile.Length > 0)
                    {

                        WebRequest _S2T = null;
                        _S2T = WebRequest.Create($"https://www.google.com/speech-api/v2/recognize?output=json&lang=en-us&key={APIKEY_Google}");
                        _S2T.Credentials = CredentialCache.DefaultCredentials;
                        _S2T.Method = "POST";
                        _S2T.ContentType = "audio/wav; rate=16000";
                        _S2T.ContentLength = BA_AudioFile.Length;
                        using (Stream stream = _S2T.GetRequestStream())
                        {
                            stream.Write(BA_AudioFile, 0, BA_AudioFile.Length);
                        }

                        HttpWebResponse _S2T_Response = (HttpWebResponse)_S2T.GetResponse();
                        if (_S2T_Response.StatusCode == HttpStatusCode.OK)
                        {
                            StreamReader SR_Response = new StreamReader(_S2T_Response.GetResponseStream());
                            var ret = SR_Response.ReadToEnd();
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
                if (IsCompleted is Action) IsCompleted.InvokeAsync();
            });
        }
        #endregion

        #region Recognizer Helper routines
        private async Task<byte[]> ToBytes(RecognizedAudio audio)
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

        private async void UpdateTitleInfo(SRT title, RecognitionResult result)
        {
            if (title is SRT)
            {
                title.Start = lastAudioPos + result.Audio.AudioPosition;
                title.End = title.Start + result.Audio.Duration;
                title.Text = result.Text.Trim();
                title.SetAltText(result.Alternates);
                title.Audio = await ToBytes(result.Audio);
            }
        }

        private void Log(string text)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        public string ToSSA()
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
                sb.AppendLine($"Dialogue: 0,{t.Start.ToString(@"hh\:mm\:ss\.ff")},{t.End.ToString(@"hh\:mm\:ss\.ff")},Default,NTP,0000,0000,0000,,{t.Text}");
            }

            return (sb.ToString());
        }

        public string ToASS()
        {
            return (ToSSA());
        }

        public string ToLRC()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[ti]{Title}");
            sb.AppendLine();
            foreach (var t in srt)
            {
                sb.AppendLine($"[{t.Start.ToString(@"hh\:mm\:ss\.fff")}] {t.Text}");
                sb.AppendLine($"[{t.End.ToString(@"hh\:mm\:ss\.fff")}]");
            }

            return (sb.ToString());
        }

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

        private async void Recognizer(string audiofile, bool restart = false)
        {
            _ReRecognize = -1;
            if (_recognizer is SpeechRecognitionEngine)
            {
                if (File.Exists(audiofile))
                {
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
                        _recognizer.SetInputToAudioStream(_recognizerStream, _audioInfo);
                        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    }
                }
            }
        }

        private void Recognizer(int index)
        {
            if (srt.Count > 0 && index >= 0 && index < srt.Count)
            {
                _ReRecognize = index;
                var buf = srt[index].Audio;
                Recognizer(index, buf);
            }
        }

        private async void Recognizer(int index, byte[] buffer, bool force = false, TimeSpan start = default(TimeSpan), TimeSpan end = default(TimeSpan))
        {
            if (buffer is byte[] && buffer.Length > 0)
            {
                if (_recognizer is SpeechRecognitionEngine)
                {
                    if (force) _ForceRecognize = index;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await ms.WriteAsync(buffer, 0, buffer.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        using (WaveFileReader reader = new WaveFileReader(ms))
                        {
                            using (WaveStream pcmStream = new WaveFormatConversionStream(defaultWaveFmt, reader))
                            {
                                if (force)
                                {
                                    SRT title = _ForceRecognize >= 0 && _ForceRecognize < srt.Count ? srt[_ForceRecognize] : null;
                                    if (title is SRT)
                                    {
                                        if (start != default(TimeSpan)) title.Start = start;
                                        if (end != default(TimeSpan)) title.End = end;
                                    }
                                    ThirdPartyRecognizer(title, null);
                                }
                                else
                                {
                                    _recognizer.SetInputToAudioStream(pcmStream, _audioInfo);
                                    _recognizer.Recognize();
                                    //_recognizer.RecognizeAsync(RecognizeMode.Single);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Recognizer Control routines
        public bool IsRunning { get; private set; } = false;
        public void Start(string audiofile = default(string))
        {
            if (IsRunning && IsPausing)
            {
                IsPausing = false;
            }
            else
            {
                if (!string.IsNullOrEmpty(audiofile))
                    AudioFile = audiofile;

                if (!string.IsNullOrEmpty(AudioFile))
                    Recognizer(AudioFile);
            }
        }

        public void Start(int index)
        {
            if (IsRunning) return;

            Recognizer(index);
        }

        public void Start(int index, byte[] buffer)
        {
            if (IsRunning) return;

            Recognizer(index, buffer);
        }

        public async void Start(int index, TimeSpan start, TimeSpan end)
        {
            if (IsRunning) return;

            if (_recognizerStream is MemoryStream && _recognizerStream.Length > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var pos = _recognizerStream.Position;
                    _recognizerStream.Seek(0, SeekOrigin.Begin);
                    using (WaveStream rws = new RawSourceWaveStream(_recognizerStream, defaultWaveFmt))
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
                                var buf = new byte[ms.Length];
                                await ms.ReadAsync(buf, 0, buf.Length);
                                await ms.FlushAsync();
                                _recognizerBuffer = buf;
                                Recognizer(index, buf, true, start, end);
                            }
                        }
#if DEBUG
                        catch (Exception ex) { Log(ex.Message); }
#else
                        catch (Exception) { }
#endif
                        finally { _recognizerStream.Seek(pos, SeekOrigin.Begin); }
                    }
                }
            }
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
                //_recognizer.RecognizeAsyncCancel();
                _recognizer.RecognizeAsyncStop();
            }
        }
        #endregion

        #region SpeechRecognizer routines
        public SpeechRecognizer(CultureInfo culture = default(CultureInfo))
        {
            try
            {
                MediaFoundationApi.Startup();

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
                APPID_iFly = File.Exists(APPID_iFly_File) ? File.ReadAllText(APPID_iFly_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APPID_iFly))
                {
                    iflysr = new iFly.SpeechRecognizer()
                    {
                        APPID = $"{APPID_iFly}",
                        AppDispather = Application.Current.Dispatcher,
                        iFlyIsCompleted = new Action(() => {
                            if (IsCompleted is Action) IsCompleted.InvokeAsync();
                        })
                    };
                }
                #endregion

                #region Azure Cognitive Service
                APIKEY_Azure = File.Exists(APIKEY_Azure_File) ? File.ReadAllText(APIKEY_Azure_File).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(APIKEY_Azure))
                {
                    var kv = APIKEY_Azure.Split('=');
                    if (kv.Length >= 2)
                    {
                        APIKEY_Azure = kv[0];
                        URL_AzureToken = string.Join("", kv.Skip(1));
                        UriBuilder ubt = new UriBuilder(URL_AzureToken);
                        UriBuilder ubr = new UriBuilder(URL_AzureResponse);
                        ubr.Host = ubt.Host.Replace("api.cognitive.microsoft.com", "stt.speech.microsoft.com");
                        URL_AzureResponse = ubr.Uri.AbsoluteUri;
                    }
                    Task.Run(async () =>
                    {
                        Token_Azure = await Recognizer_AzureFetchToken(URL_AzureToken);
                    });
                }
                #endregion

                #region Google Speech Recognize
                APIKEY_Google = File.Exists(APIKEY_Google_File) ? File.ReadAllText(APIKEY_Google_File).Trim() : string.Empty;
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
