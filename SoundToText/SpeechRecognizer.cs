using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SoundToText
{
    public static class DefaultStyle
    {
        public static string ENG_Default = "Style: Default,Tahoma,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string ENG_Note = "Style: Note,Times New Roman,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string ENG_Title = "Style: Title,Arial,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";

        public static string CHS_Default = "Style: Default,更纱黑体 SC,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHS_Note = "Style: Note,宋体,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHS_Title = "Style: Title,更纱黑体 SC,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";

        public static string CHT_Default = "Style: Default,Sarasa Gothic TC,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHT_Note = "Style: Note,宋体,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHT_Title = "Style: Title,Sarasa Gothic TC,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
    }

    public class SimpleTitle
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; } = 100.0;
    }

    public class SRT : INotifyPropertyChanged
    {
        public int Count { get; set; } = 0;
        public TimeSpan Start { get; set; } = TimeSpan.FromSeconds(0);
        public TimeSpan End { get; set; } = TimeSpan.FromSeconds(0);

        private string text = string.Empty;
        public string Text
        {
            get { return (text); }
            set { text = value; NotifyPropertyChanged("Text"); }
        }
        public double Confidence { get; set; } = 100.0;

        public List<SimpleTitle> AltTitle { get; set; } = new List<SimpleTitle>();
        internal protected void SetAltText(IEnumerable<RecognizedPhrase> alt)
        {
            if(!(AltTitle is List<SimpleTitle>))
                AltTitle = new List<SimpleTitle>();

            AltTitle.Clear();
            foreach (RecognizedPhrase t in alt)
            {
                var title = new SimpleTitle()
                {
                    Confidence = t.Confidence,
                    Text = t.Text
                };
                AltTitle.Add(title);
            }
        }

        public override string ToString()
        {
            return ($"[{Count}]:{Text}");
        }

        public string Title
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{Count}");
                sb.AppendLine($"{Start.ToString(@"hh\:mm\:ss\,fff")} --> {End.ToString(@"hh\:mm\:ss\,fff")}");
                sb.AppendLine($"{Text}");
                sb.AppendLine();
                return (sb.ToString());
            }
        }

        public string LRC
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{Start.ToString(@"hh\:mm\:ss\.fff")}] {Text}");
                sb.AppendLine($"[{End.ToString(@"hh\:mm\:ss\.fff")}]");
                return (sb.ToString());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SpeechRecognizer
    {
        #region Speech Recognition routines

        #region Recognizer Internal Variable
        private List<RecognizerInfo> Recognizers = SpeechRecognitionEngine.InstalledRecognizers().ToList();
        private SpeechRecognitionEngine _recognizer = null;

        private MemoryStream _recognizerStream = null;

        public Action IsCompleted { get; set; } = null;

        private string appid = string.Empty;
        private string appid_file = "ifly_appid.txt";
        iFly.SpeechRecognizer iflysr = null;
        #endregion

        #region Recognizer Event Handle
        private TimeSpan lastAudioPos = TimeSpan.FromSeconds(0);
        private async void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
                var title = srt.Last();
                title.Start = lastAudioPos + e.Result.Audio.AudioPosition;
                title.End = title.Start + e.Result.Audio.Duration;
                title.Text = e.Result.Text.Trim();
                title.SetAltText(e.Result.Alternates);

                AudioPos = title.Start;
                if (ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                {
                    ProgressHost.Report(Progress);
                }
                if (IsPausing) lastAudioPos = title.End;

                if (iflysr is iFly.SpeechRecognizer)
                {
                    await Task.Run(async () =>
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            e.Result.Audio.WriteToAudioStream(ms);
                            await ms.FlushAsync();

                            byte[] buf = new byte[256];
                            buf = new byte[ms.Length];
                            ms.Seek(0, SeekOrigin.Begin);
                            ms.Read(buf, 0, (int)(ms.Length));
                            var ret = await iflysr.Recognizer(buf);
                        //title.Text += $" [{ret}]";
                        if (!string.IsNullOrEmpty(ret))
                            {
                                if (ret.StartsWith("#"))
                                    title.Text += $" [{ret}]";
                                else
                                    title.Text = ret;
                            }
                        }
                    });
                }

                //using (MemoryStream ms = new MemoryStream())
                //{
                //    e.Result.Audio.WriteToAudioStream(ms);
                //    ms.Flush();

                //    byte[] buf = new byte[256];
                //    buf = new byte[ms.Length];
                //    ms.Seek(0, SeekOrigin.Begin);
                //    ms.Read(buf, 0, (int)(ms.Length));
                //    var ret = await iflysr.Recognizer(buf);
                //    //title.Text += $" [{ret}]";
                //    title.Text = ret;
                //}
            }
        }

        private void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (e.Result is RecognitionResult)
            {
                if (e.Result.Alternates.Count == 0)
                {
#if DEBUG
                    Console.WriteLine("Speech rejected. No candidate phrases found.");
#endif
                    return;
                }
#if DEBUG
                Console.WriteLine("Speech rejected. Did you mean:");
                foreach (RecognizedPhrase r in e.Result.Alternates)
                {
                    Console.WriteLine("    " + r.Text);
                }
#endif
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
            var title = new SRT()
            {
                Count = srt.Count + 1,
                Start = e.AudioPosition,
                End = e.AudioPosition
            };
            srt.Add(title);
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
                    AudioPos = AudioLength;
                    IsRunning = false;
                    IsPausing = false;
                    if (IsCompleted is Action) IsCompleted.Invoke();
                }
                if (ProgressHost is IProgress<Tuple<TimeSpan, TimeSpan>>)
                    ProgressHost.Report(Progress);
            }
            //_recognizerPause.Release();
            //_recognizerRunning.Release();
        }
        #endregion

        #region Recognizer Helper routines
        private async Task<SpeechAudioFormatInfo> LoadAudio(string audiofile)
        {
            //var ext = Path.GetExtension(audiofile).ToLower();
            SpeechAudioFormatInfo audioInfo = null;
            try
            {
                //using (AudioFileReader reader = new AudioFileReader(audiofile))
                using (MediaFoundationReader reader = new MediaFoundationReader(audiofile))
                {
                    var fmt_new = new WaveFormat(16000, 16, 1);
                    //using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                    using (WaveStream pcmStream = new WaveFormatConversionStream(fmt_new, reader))
                    {
                        //WaveFileWriter.CreateWaveFile("_test_.wav", pcmStream);
                        var fmt = pcmStream.WaveFormat;
                        if (iflysr is iFly.SpeechRecognizer) iflysr.SampleRate = fmt.SampleRate;

                        audioInfo = new SpeechAudioFormatInfo(EncodingFormat.Pcm, fmt.SampleRate, fmt.BitsPerSample, fmt.Channels, fmt.AverageBytesPerSecond, fmt.BlockAlign, new byte[fmt.ExtraSize]);

                        _recognizerStream = new MemoryStream();
                        byte[] buf = new byte[pcmStream.Length];
                        pcmStream.Read(buf, 0, buf.Length);
                        await _recognizerStream.WriteAsync(buf, 0, buf.Length);
                        await _recognizerStream.FlushAsync();
                        _recognizerStream.Seek(0, SeekOrigin.Begin);

                        AudioPos = reader.CurrentTime;
                        AudioLength = reader.TotalTime;
                    }
                }
            }
            catch (Exception)
            {
                if(_recognizerStream is MemoryStream)
                    _recognizerStream.Dispose();
                _recognizerStream = null;
            }
            return (audioInfo);
        }

        private async void Recognizer(string audiofile, bool restart = false)
        {
            if (File.Exists(audiofile))
            {
                if (_recognizer is SpeechRecognitionEngine)
                {
                    if (restart || !IsRunning)
                    {
                        //_recognizerPause.Release();
                        //_recognizerRunning.Release();
                        IsRunning = true;
                        IsPausing = false;

                        var audioInfo = await LoadAudio(audiofile);

                        //_recognizer.SetInputToAudioStream();
                        _recognizer.SetInputToAudioStream(_recognizerStream, audioInfo);

                        srt.Clear();
                    }
                    _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                }
            }
        }
        #endregion

        public string AudioFile { get; set; } = string.Empty;

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
                        sb.AppendLine($"{s.Count}");
                        sb.AppendLine($"{s.Start.ToString(@"hh\:mm\:ss\,fff")} --> {s.End.ToString(@"hh\:mm\:ss\,fff")}");
                        sb.AppendLine($"{s.Text}");
                        sb.AppendLine();
                    }
                    return (sb.ToString());
                }
                else return (string.Empty);
            }
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

        #region Recognizer Control routines
        //public bool IsRunning { get { return (_recognizerRunning.CurrentCount > 0); }}
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

        //public bool IsPausing { get { return (_recognizerPause.CurrentCount > 0); } }
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

        #endregion

        public SpeechRecognizer()
        {
            #region Recognition
            try
            {
                if (Recognizers.Count > 0)
                    _recognizer = new SpeechRecognitionEngine(CultureInfo.CurrentCulture);
                else
                    _recognizer = new SpeechRecognitionEngine();

                Grammar dictation = new DictationGrammar();
                dictation.Name = "Dictation Grammar";
                _recognizer.LoadGrammar(dictation);
                //_recognizer.SetInputToDefaultAudioDevice();
                _recognizer.SpeechDetected += Recognizer_SpeechDetected;
                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
                _recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
                _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;

                if (File.Exists(appid_file))
                {
                    appid = File.ReadAllText(appid_file).Trim();
                    iflysr = new iFly.SpeechRecognizer()
                    {
                        APPID = $"{appid}",
                        AppDispather = Application.Current.Dispatcher
                    };
                }
            }
            catch (Exception)
            {
                _recognizer = null;
                MessageBox.Show("Speech Recognizer not INSTALLED!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            #endregion
        }

        public SpeechRecognizer(CultureInfo culture)
        {
            #region Recognition
            if(culture is CultureInfo)
                _recognizer = new SpeechRecognitionEngine(culture);
            else
                _recognizer = new SpeechRecognitionEngine(CultureInfo.CurrentCulture);

            Grammar dictation = new DictationGrammar();
            dictation.Name = "Dictation Grammar";
            _recognizer.LoadGrammar(dictation);
            //_recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechDetected += Recognizer_SpeechDetected;
            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            _recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
            _recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
            _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
            #endregion
        }
    }
}
