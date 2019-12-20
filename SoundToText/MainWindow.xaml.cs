using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPFSoundVisualizationLib;

namespace SoundToText
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static string[] exts_snd = new string[] { ".wav", ".mp1", ".mp2", ".mp3", ".mp4", ".aac", ".ogg", ".m4a", ".flac", ".wma", ".amr" };
        private static string[] exts_snd = new string[] { ".wav", ".mp3", ".mp4", ".m3a", ".m4a" };

        private IProgress<Tuple<TimeSpan, TimeSpan>> progress = null;

        private SpeechRecognizer s2t = null;
        private SpeechTTS t2s = null;

        private enum MediaButtonState { Invalid, Idle, Running, Pausing, Completed };
        private void SetMediaButtonState(MediaButtonState state)
        {
            try
            {
                bool Valid = s2t is SpeechRecognizer;
                switch (state)
                {
                    case MediaButtonState.Idle:
                        btnConvertPlay.IsEnabled = Valid && true;
                        btnConvertPause.IsEnabled = Valid && false;
                        btnConvertStop.IsEnabled = Valid && false;
                        break;
                    case MediaButtonState.Running:
                        btnConvertPlay.IsEnabled = Valid && false;
                        btnConvertPause.IsEnabled = Valid && true;
                        btnConvertStop.IsEnabled = Valid && true;
                        break;
                    case MediaButtonState.Pausing:
                        btnConvertPlay.IsEnabled = Valid && true;
                        btnConvertPause.IsEnabled = Valid && false;
                        btnConvertStop.IsEnabled = Valid && true;
                        break;
                    case MediaButtonState.Completed:
                        btnConvertPlay.IsEnabled = Valid && true;
                        btnConvertPause.IsEnabled = false;
                        btnConvertStop.IsEnabled = false;
                        break;
                    default:
                        btnConvertPlay.IsEnabled = true;
                        btnConvertPause.IsEnabled = false;
                        btnConvertStop.IsEnabled = false;
                        break;
                }
                if (state == MediaButtonState.Invalid)
                {
                    MediaPanel.IsEnabled = false;
                    WaveformPanel.IsEnabled = false;
                }
                else
                {
                    MediaPanel.IsEnabled = true;
                    WaveformPanel.IsEnabled = true;
                }
            }
            catch (Exception)
            {
                btnConvertPlay.IsEnabled = true;
                btnConvertPause.IsEnabled = false;
                btnConvertStop.IsEnabled = false;
                MediaPanel.IsEnabled = true;
                WaveformPanel.IsEnabled = true;
            }
        }

        private void SetTtsButtonState(MediaButtonState state)
        {
            try
            {
                switch (state)
                {
                    case MediaButtonState.Idle:
                        btnTtsPlay.IsEnabled = true;
                        btnTtsPause.IsEnabled = false;
                        btnTtsStop.IsEnabled = false;
                        break;
                    case MediaButtonState.Running:
                        btnTtsPlay.IsEnabled = false;
                        btnTtsPause.IsEnabled = true;
                        btnTtsStop.IsEnabled = true;
                        break;
                    case MediaButtonState.Pausing:
                        btnTtsPlay.IsEnabled = true;
                        btnTtsPause.IsEnabled = false;
                        btnTtsStop.IsEnabled = true;
                        break;
                    case MediaButtonState.Completed:
                        btnTtsPlay.IsEnabled = true;
                        btnTtsPause.IsEnabled = false;
                        btnTtsStop.IsEnabled = false;
                        break;
                    default:
                        btnTtsPlay.IsEnabled = false;
                        btnTtsPause.IsEnabled = false;
                        btnTtsStop.IsEnabled = false;
                        break;
                }
                if (state == MediaButtonState.Invalid)
                {
                    btnCommit.IsEnabled = false;
                    edTitle.IsEnabled = false;
                    TtsPanel.IsEnabled = false;
                }
                else
                {
                    btnCommit.IsEnabled = true;
                    edTitle.IsEnabled = true;
                    TtsPanel.IsEnabled = true;
                }
            }
            catch (Exception)
            {
                btnTtsPlay.IsEnabled = true;
                btnTtsPause.IsEnabled = false;
                btnTtsStop.IsEnabled = false;
            }
        }

        private void LoadAudio(string file)
        {
            if (File.Exists(file))
            {
                s2t.AudioFile = file;
                SetMediaButtonState(MediaButtonState.Idle);
                NAudioEngine.Instance.OpenFile(file);
            }
        }

        #region NAudio Engine Events
        private void NAudioEngine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NAudioEngine engine = NAudioEngine.Instance;
            switch (e.PropertyName)
            {
                case "FileTag":
                    if (engine.FileTag != null)
                    {
                        TagLib.Tag tag = engine.FileTag.Tag;
                    }
                    else
                    {
                        //albumArtPanel.AlbumArtImage = null;
                    }
                    break;
                case "ChannelPosition":
                    WaveTime.Time = TimeSpan.FromSeconds(engine.ChannelPosition);
                    break;
                default:
                    // Do Nothing
                    break;
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            NAudioEngine soundEngine = NAudioEngine.Instance;
            soundEngine.PropertyChanged += NAudioEngine_PropertyChanged;
            WaveformViewer.RegisterSoundPlayer(soundEngine);
            WaveformViewer.AutoScaleWaveformCache = true;
            WaveformViewer.AllowRepeatRegions = true;

            UIHelper.Bind(soundEngine, "CanPlay", btnPlay, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanPause", btnPause, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanStop", btnStop, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "SelectionBegin", repeatStartTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);
            UIHelper.Bind(soundEngine, "SelectionEnd", repeatStopTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);

            //Resources.MergedDictionaries.Clear();
            ResourceDictionary themeResources = Application.LoadComponent(new Uri("ExpressionDark.xaml", UriKind.Relative)) as ResourceDictionary;
            Resources.MergedDictionaries.Add(themeResources);

            SetMediaButtonState(MediaButtonState.Invalid);
            SetTtsButtonState(MediaButtonState.Invalid);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPanel.IsEnabled = false;

            progress = new Progress<Tuple<TimeSpan, TimeSpan>>(i =>
            {
                var received = i.Item1;
                var total = i.Item2;
                progressBar.Value = total.TotalSeconds > 0 ? received.TotalSeconds / total.TotalSeconds * 100 : 0;
                var cs = received.ToString(@"hh\:mm\:ss\.fff");
                var ts = total.ToString(@"hh\:mm\:ss\.fff");
                progressInfo.Text = $"{cs}/{ts} ({progressBar.Value:0.0}%)";
            });

            s2t = new SpeechRecognizer()
            {
                ProgressHost = progress,
                IsCompleted = new Action(() =>
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        SetMediaButtonState(MediaButtonState.Completed);
                    }
                })
            };

            t2s = new SpeechTTS()
            {
                IsCompleted = new Action(() => {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        SetTtsButtonState(MediaButtonState.Completed);
                    }
                })
            };

            cbLanguage.Items.Clear();
            foreach (var r in SpeechRecognizer.InstalledRecognizers)
            {
                cbLanguage.Items.Add(r.Culture.DisplayName);
            }
            if (cbLanguage.Items.Count > 0) cbLanguage.SelectedIndex = 0;

            miOptEngineSAPI.IsChecked = s2t.SAPIEnabled;
            miOptEngineiFly.IsChecked = s2t.iFlyEnabled;
            miOptEngineAzure.IsChecked = s2t.AzureEnabled;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            NAudioEngine.Instance.Dispose();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                var fmts = e.Data.GetFormats();
#if DEBUG
                Console.WriteLine($"{string.Join(", ", fmts)}");
#endif
                if (fmts.Contains("FileName"))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception) { }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var fmts = e.Data.GetFormats();
#if DEBUG
            Console.WriteLine($"{string.Join(", ", fmts)}");
#endif
            if (fmts.Contains("FileName"))
            {
                var fns = (string[])e.Data.GetData("FileName");
                if (fns.Length > 0)
                {
                    var fn = fns[0];
                    var ext = Path.GetExtension(fn).ToLower();

                    if (s2t is SpeechRecognizer)
                    {
                        if (exts_snd.Contains(ext))
                        {
                            try
                            {
                                LoadAudio(fn);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
        }

        private void WaveformViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (e.Delta < 0) NAudioEngine.Instance.ChannelPosition -= 0.01;
                else if (e.Delta > 0) NAudioEngine.Instance.ChannelPosition += 0.01;
            }
        }

        private void lstResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.SelectedItem is SRT)
                {
                    var srt = lstResult.SelectedItem as SRT;
                    lblTitle.Text = srt.Title;
                    edTitle.Text = srt.Text;
                    edStartTime.Value = srt.Start;
                    edEndTime.Value = srt.End;

                    NAudioEngine.Instance.ChannelPosition = srt.Start.TotalSeconds;
                    NAudioEngine.Instance.SelectionBegin = srt.Start;
                    NAudioEngine.Instance.SelectionEnd = srt.End;

                    titleIndex.Time = TimeSpan.FromSeconds((srt.DisplayIndex / 100 * 60) + (srt.DisplayIndex % 100));

                    SetTtsButtonState(MediaButtonState.Idle);
                }
                else
                {
                    SetTtsButtonState(MediaButtonState.Invalid);
                }
            }
        }

        private void TitleContent_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property.Name == "Text" && e.TargetObject is TextBlock)
            {
                var tb = e.TargetObject as TextBlock;
                if (lstResult.SelectedItem != null)
                {
                    var srt = lstResult.SelectedItem as SRT;
                    lblTitle.Text = srt.Title;
                    edTitle.Text = srt.Text;
                    edStartTime.Value = srt.NewStart;
                    edEndTime.Value = srt.NewEnd;

                    $"{srt.Index}:{srt.Text}, {tb.Text}".Log();
                }
            }
        }

        private void cbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx = cbLanguage.SelectedIndex;
            if (SpeechRecognizer.InstalledRecognizers.Count > 0 && idx >= 0 && idx < SpeechRecognizer.InstalledRecognizers.Count)
            {
                if (s2t is SpeechRecognizer)
                {
                    s2t.Culture = SpeechRecognizer.InstalledRecognizers[idx].Culture;                    
                }
                if (t2s is SpeechTTS)
                {
                    t2s.Culture = s2t.Culture;
                }
            }
        }

        private void miOptEngine_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (sender == miOptEngineSAPI)
                {
                    miOptEngineSAPI.IsChecked = s2t.SAPIEnabled;
                }
                else if (sender == miOptEngineiFly)
                {
                    s2t.iFlyEnabled = !s2t.iFlyEnabled;
                    if (s2t.iFlyEnabled)
                    {
                        s2t.AzureEnabled = false;
                        s2t.GoogleEnabled = false;
                    }
                }
                else if (sender == miOptEngineAzure)
                {
                    s2t.AzureEnabled = !s2t.AzureEnabled;
                    if (s2t.AzureEnabled)
                    {
                        s2t.iFlyEnabled = false;
                        s2t.GoogleEnabled = false;
                    }
                }
                else if (sender == miOptEngineGoogle)
                {
                    s2t.GoogleEnabled = !s2t.GoogleEnabled;
                    if (s2t.GoogleEnabled)
                    {
                        s2t.iFlyEnabled = false;
                        s2t.AzureEnabled = false;
                    }
                }
                miOptEngineiFly.IsChecked = s2t.iFlyEnabled;
                miOptEngineAzure.IsChecked = s2t.AzureEnabled;
                miOptEngineGoogle.IsChecked = s2t.GoogleEnabled;
            }
        }

        private void btnOption_Click(object sender, RoutedEventArgs e)
        {
            if (btnOption.ContextMenu is ContextMenu)
            {
                btnOption.ContextMenu.IsOpen = true;
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                OpenFileDialog dlgOpen = new OpenFileDialog();
                dlgOpen.DefaultExt = ".mp3";
                //dlgOpen.Filter = "All Supported Audio Files|*.mp1;*.mp2;*.mp3;*.mp4;*.m4a;*.aac;*.ogg;*.oga;*.flac;*.wav;*.wma";
                dlgOpen.Filter = "All Supported Audio Files|*.mp3;*.mp4;*.m3a,*.m4a;*.wav";
                if (dlgOpen.ShowDialog(this).Value)
                {
                    var fn = dlgOpen.FileName;
                    var ext = Path.GetExtension(fn).ToLower();
                    if (exts_snd.Contains(ext))
                    {
                        LoadAudio(fn);
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                SaveFileDialog dlgSave = new SaveFileDialog();
                dlgSave.FileName = Path.ChangeExtension(s2t.AudioFile, ".srt");
                dlgSave.DefaultExt = ".srt";
                dlgSave.Filter = "All Supported Audio Files|*.srt;*.lrc;*.ass;*.ssa;*.txt|SubRip/SRT File|*.srt|Advanced Sub Station Alpha File|*.ass|Lyric File|*.lrc";
                if (dlgSave.ShowDialog(this).Value)
                {
                    var fn = dlgSave.FileName;
                    var ext = Path.GetExtension(fn).ToLower();
                    if (ext.Equals(".ssa"))
                        File.WriteAllText(fn, s2t.ToSSA(), Encoding.UTF8);
                    else if (ext.Equals(".ass"))
                        File.WriteAllText(fn, s2t.ToASS(), Encoding.UTF8);
                    else if (ext.Equals(".lrc"))
                        File.WriteAllText(fn, s2t.ToLRC(), Encoding.UTF8);
                    else
                        File.WriteAllText(fn, s2t.Text, Encoding.UTF8);
                }
            }
        }

        private void btnConvertPlay_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (s2t.IsPausing)
                {
                    s2t.Resume();
                }
                else
                {
                    SetMediaButtonState(MediaButtonState.Running);

                    if (Keyboard.Modifiers == ModifierKeys.Control || sender == btnReConvertPlay)
                    {
                        foreach (var item in lstResult.SelectedItems)
                        {
                            if (item is SRT)
                            {
                                var srt = item as SRT;
                                srt.NewStart = srt.Start;
                                srt.NewEnd = srt.End;
                                s2t.Start(srt);
                            }
                        }
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift || sender == btnForceConvertPlay)
                    {
                        var engine = NAudioEngine.Instance;
                        foreach (var item in lstResult.SelectedItems)
                        {
                            if (item is SRT)
                            {
                                var srt = item as SRT;
                                var start = srt == lstResult.SelectedItem ? engine.SelectionBegin : srt.Start;
                                var end = srt == lstResult.SelectedItem ? engine.SelectionEnd : srt.End;
                                if (start.TotalSeconds < 0)
                                    start = TimeSpan.FromSeconds(0);
                                if (end.TotalSeconds > engine.ActiveStream.TotalTime.TotalSeconds)
                                    end = engine.ActiveStream.TotalTime;
                                srt.NewStart = start;
                                srt.NewEnd = end;
                                s2t.Start(srt, true);
                                //s2t.Start(srt.Index, start, end);
                            }
                        }
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        lstResult.ItemsSource = s2t.Result;
                        lstResult.UpdateLayout();

                        lblTitle.Text = string.Empty;

                        progressBar.Minimum = 0;
                        progressBar.Maximum = 100;
                        progress.Report(s2t.Progress);
                        s2t.Start();
                    }
                }
            }
        }

        private void btnConvertPause_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (s2t.IsPausing)
                    s2t.Resume();
                else
                    s2t.Pause();

                SetMediaButtonState(MediaButtonState.Pausing);
            }
        }

        private void btnConvertStop_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                s2t.Stop();
                SetMediaButtonState(MediaButtonState.Idle);
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                NAudioEngine.Instance.ChannelPosition = 0;
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                NAudioEngine.Instance.ChannelPosition -= 0.01;
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                NAudioEngine.Instance.ChannelPosition += 0.01;
            }
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                NAudioEngine.Instance.ChannelPosition = NAudioEngine.Instance.ChannelLength;
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (NAudioEngine.Instance.CanPlay)
                    NAudioEngine.Instance.Play();
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (NAudioEngine.Instance.CanPause)
                    NAudioEngine.Instance.Pause();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (NAudioEngine.Instance.CanStop)
                    NAudioEngine.Instance.Stop();
            }
        }

        private void btnTtsPlay_Click(object sender, RoutedEventArgs e)
        {
            if(t2s is SpeechTTS)
            {
                SetTtsButtonState(MediaButtonState.Running);
                t2s.Play(edTitle.Text);
            }
        }

        private void btnTtsPause_Click(object sender, RoutedEventArgs e)
        {
            if (t2s is SpeechTTS)
            {
                SetTtsButtonState(MediaButtonState.Pausing);
                t2s.Pause();
            }
        }

        private void btnTtsStop_Click(object sender, RoutedEventArgs e)
        {
            if (t2s is SpeechTTS)
            {
                SetTtsButtonState(MediaButtonState.Invalid);
                t2s.Stop();
            }
        }

        private void btnCommit_Click(object sender, RoutedEventArgs e)
        {
            if (lstResult.SelectedItem is SRT)
            {
                var srt = lstResult.SelectedItem as SRT;
                if (!edTitle.Text.Equals(srt.Text))
                {
                    srt.Text = edTitle.Text;
                }
            }
        }

    }
}
