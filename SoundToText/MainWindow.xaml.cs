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
        private static string[] exts_snd = new string[] { ".wav", ".mp3", ".mp4", ".m3a", ".m4a", ".aac" };
        private static string[] exts_srt = new string[] { ".srt", ".ass", ".ssa", ".lrc", ".csv" };

        private string lastSaveFile = string.Empty;

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
                        btnOpen.IsEnabled = true;
                        break;
                    case MediaButtonState.Running:
                        btnConvertPlay.IsEnabled = Valid && false;
                        btnConvertPause.IsEnabled = Valid && true;
                        btnConvertStop.IsEnabled = Valid && true;
                        btnOpen.IsEnabled = false;
                        break;
                    case MediaButtonState.Pausing:
                        btnConvertPlay.IsEnabled = Valid && true;
                        btnConvertPause.IsEnabled = Valid && false;
                        btnConvertStop.IsEnabled = Valid && true;
                        btnOpen.IsEnabled = false;
                        break;
                    case MediaButtonState.Completed:
                        btnConvertPlay.IsEnabled = Valid && true;
                        btnConvertPause.IsEnabled = false;
                        btnConvertStop.IsEnabled = false;
                        btnOpen.IsEnabled = true;
                        break;
                    default:
                        btnConvertPlay.IsEnabled = true;
                        btnConvertPause.IsEnabled = false;
                        btnConvertStop.IsEnabled = false;
                        btnOpen.IsEnabled = true;
                        break;
                }
                btnSlice.IsEnabled = btnConvertPlay.IsEnabled;
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
                btnOpen.IsEnabled = true;
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
                    btnPrevTitle.IsEnabled = false;
                    btnNextTitle.IsEnabled = false;
                    edTitle.IsEnabled = false;
                    edTranslated.IsEnabled = false;
                    TtsPanel.IsEnabled = false;
                }
                else
                {
                    btnCommit.IsEnabled = true;
                    btnPrevTitle.IsEnabled = true;
                    btnNextTitle.IsEnabled = true;
                    edTitle.IsEnabled = true;
                    edTranslated.IsEnabled = true;
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
                if (s2t is SpeechRecognizer)
                {
                    if (s2t.IsRunning) return;

                    SetMediaButtonState(MediaButtonState.Running);
                    s2t.AudioFile = file;
                    NAudioEngine.Instance.OpenFile(file);
                    s2t.Result.Clear();
                    lblTitle.Text = string.Empty;
                    edTitle.Text = string.Empty;
                    edStartTime.Value = TimeSpan.FromSeconds(0);
                    edEndTime.Value = TimeSpan.FromSeconds(0);
                    SetMediaButtonState(MediaButtonState.Idle);
                }
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
                #region Update ProgressBar
                var percent = total.TotalSeconds > 0 ? received.TotalSeconds / total.TotalSeconds : 0;
                progressBar.Value = percent * 100;
                #endregion
                #region Update Progress Info Text
                var cs = received.ToString(@"hh\:mm\:ss\.fff");
                var ts = total.ToString(@"hh\:mm\:ss\.fff");
                progressInfo.Text = $"{cs}/{ts} ({progressBar.Value:0.0}%)";
                #endregion
                #region Update Progress Info Text Color Gradient
                var factor = progressBar.ActualWidth / progressInfo.ActualWidth;
                var offset = Math.Abs((factor - 1) / 2);
                progressInfoLinear.StartPoint = new Point(0 - offset, 0);
                progressInfoLinear.EndPoint = new Point(1 + offset, 0);
                progressInfoLeft.Offset = percent;
                progressInfoRight.Offset = percent;
                #endregion
            });

            s2t = new SpeechRecognizer()
            {
                ProgressHost = progress,
                IsCompleted = new Action(() =>
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        SetMediaButtonState(MediaButtonState.Completed);
                        if (lstResult.Items.Count > 0 && lstResult.SelectedItem == null) lstResult.SelectedIndex = 0;
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
            miOptEngineiFlySDK.IsChecked = s2t.iFlyEnabledSDK;
            miOptEngineAzure.IsChecked = s2t.AzureEnabled;

            lstResult.ItemsSource = s2t.Result;
            lstResult.UpdateLayout();
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

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.S)
                {
                    if (string.IsNullOrEmpty(lastSaveFile) || !File.Exists(lastSaveFile))
                    {
                        btnSave_Click(btnSave, e);
                    }
                    else
                    {
                        var ext = Path.GetExtension(lastSaveFile).ToLower();
                        if (ext.Equals(".ssa"))
                            File.WriteAllText(lastSaveFile, s2t.ToSSA(), Encoding.UTF8);
                        else if (ext.Equals(".ass"))
                            File.WriteAllText(lastSaveFile, s2t.ToASS(), Encoding.UTF8);
                        else if (ext.Equals(".lrc"))
                            File.WriteAllText(lastSaveFile, s2t.ToLRC(), Encoding.UTF8);
                        else
                            File.WriteAllText(lastSaveFile, s2t.ToSRT(), Encoding.UTF8);
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.O)
                {
                    btnOpen_Click(btnOpen, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    btnCommit_Click(btnCommit, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.R)
                {
                    btnConvertPlay_Click(miRecognizing, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.T)
                {
                    btnConvertPlay_Click(miTranslating, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.P)
                {
                    btnTtsPlay_Click(btnTtsPlay, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    btnPrevTitle_Click(btnPrevTitle, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    btnNextTitle_Click(btnNextTitle, e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Divide)
                {
                    btnSlice_Click(btnSlice, e);
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
                    edTranslated.Text = srt.TranslatedText;
                    edStartTime.Value = srt.NewStart;
                    edEndTime.Value = srt.NewEnd;

                    NAudioEngine.Instance.ChannelPosition = srt.NewStart.TotalSeconds;
                    NAudioEngine.Instance.SelectionBegin = srt.NewStart;
                    NAudioEngine.Instance.SelectionEnd = srt.NewEnd;

                    titleIndex.Time = TimeSpan.FromSeconds((srt.DisplayIndex / 100 * 60) + (srt.DisplayIndex % 100));
                    titleIndex.ToolTip = $"Duration: {(srt.NewEnd - srt.NewStart).ToString(@"hh\:mm\:ss\.fff")}";

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
            if ((e.Property.Name.Equals("Text") || e.Property.Name.Equals("TranslatedText")) && e.TargetObject is TextBlock)
            {
                var tb = e.TargetObject as TextBlock;
                if (lstResult.SelectedItem != null)
                {
                    var srt = lstResult.SelectedItem as SRT;
                    lblTitle.Text = srt.Title;
                    edTitle.Text = srt.Text;
                    edTranslated.Text = srt.TranslatedText;
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
                else if (sender == miOptEngineiFlySDK)
                {
                    s2t.iFlyEnabledSDK = !s2t.iFlyEnabledSDK;
                    if (s2t.iFlyEnabledSDK)
                    {
                        s2t.iFlyEnabledWebAPI = false;
                        s2t.AzureEnabled = false;
                        s2t.GoogleEnabled = false;
                    }
                }
                else if (sender == miOptEngineiFlyWebAPI)
                {
                    s2t.iFlyEnabledWebAPI = !s2t.iFlyEnabledWebAPI;
                    if (s2t.iFlyEnabledWebAPI)
                    {
                        s2t.iFlyEnabledSDK = false;
                        s2t.AzureEnabled = false;
                        s2t.GoogleEnabled = false;
                    }
                }
                else if (sender == miOptEngineAzure)
                {
                    s2t.AzureEnabled = !s2t.AzureEnabled;
                    if (s2t.AzureEnabled)
                    {
                        s2t.iFlyEnabledSDK = false;
                        s2t.iFlyEnabledWebAPI = false;
                        s2t.GoogleEnabled = false;
                    }
                }
                else if (sender == miOptEngineGoogle)
                {
                    s2t.GoogleEnabled = !s2t.GoogleEnabled;
                    if (s2t.GoogleEnabled)
                    {
                        s2t.iFlyEnabledSDK = false;
                        s2t.iFlyEnabledWebAPI = false;
                        s2t.AzureEnabled = false;
                    }
                }
                miOptEngineiFlySDK.IsChecked = s2t.iFlyEnabledSDK;
                miOptEngineiFlyWebAPI.IsChecked = s2t.iFlyEnabledWebAPI;
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
                if (s2t.IsRunning) return;

                OpenFileDialog dlgOpen = new OpenFileDialog();
                dlgOpen.DefaultExt = ".mp3";
                //dlgOpen.Filter = "All Supported Audio Files|*.mp1;*.mp2;*.mp3;*.mp4;*.m4a;*.aac;*.ogg;*.oga;*.flac;*.wav;*.wma";
                dlgOpen.Filter = "All Supported Audio Files|*.mp3;*.mp4;*.m3a;*.m4a;*.wav;*.aac;*.srt;*.csv";
                if (dlgOpen.ShowDialog(this).Value)
                {
                    var fn = dlgOpen.FileName;
                    var ext = Path.GetExtension(fn).ToLower();
                    if (exts_snd.Contains(ext))
                    {
                        LoadAudio(fn);
                    }
                    else if (exts_srt.Contains(ext))
                    {
                        var ret = MessageBoxResult.Yes;
                        if(s2t.Result.Count>0)
                            ret = MessageBox.Show("Will lost current subtitles, continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
                        if (ret == MessageBoxResult.Yes)
                        {
                            if (ext.Equals(".csv"))
                                s2t.LoadCSV(fn);
                            else if (ext.Equals(".srt"))
                                s2t.LoadSRT(fn);
                        }                        
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
                        File.WriteAllText(fn, s2t.ToSRT(), Encoding.UTF8);

                    lastSaveFile = fn;
                }
            }
        }

        private async void btnSlice_Click(object sender, RoutedEventArgs e)
        {
            if (s2t.IsRunning) return;
            SetMediaButtonState(MediaButtonState.Running);
            var results = await s2t.SliceAudio(-50, 2, 0.5, true);
            //s2t.LoadSlice(results, true);
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

                    if (sender == miRecognizing || sender == btnReConvertPlay || (sender == btnConvertPlay && Keyboard.Modifiers == ModifierKeys.Control))
                    {
                        foreach (var item in lstResult.SelectedItems)
                        {
                            if (item is SRT)
                            {
                                var srt = item as SRT;
                                srt.NewStart = srt.Start;
                                srt.NewEnd = srt.End;
                                s2t.Start(srt, srt.Audio == null ? true : false);
                            }
                        }
                    }
                    else if (sender == miTranslating || sender == btnTranslateSelected)
                    {
                        foreach (var item in lstResult.SelectedItems)
                        {
                            if (item is SRT)
                            {
                                var srt = item as SRT;
                                s2t.Translate(srt);
                            }
                        }
                    }
                    else if (sender == btnTranslateAll)
                    {

                    }
                    else if (sender == btnForceConvertPlay || (sender == btnConvertPlay && Keyboard.Modifiers == ModifierKeys.Control))
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
                    else if (sender == btnConvertPlay && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        if (s2t.Result.Count > 0)
                        {
                            var result = MessageBox.Show("Current subtitle results will be lost, continue?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                            if (result == MessageBoxResult.Cancel) return;
                        }

                        lstResult.ItemsSource = s2t.Result;
                        lstResult.UpdateLayout();

                        lblTitle.Text = string.Empty;
                        edTitle.Text = string.Empty;
                        edTranslated.Text = string.Empty;
                        edStartTime.Value = default(TimeSpan);
                        edEndTime.Value = default(TimeSpan);                        

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

        private void btnPrevTitle_Click(object sender, RoutedEventArgs e)
        {
            if (s2t.IsRunning) return;
            if (lstResult.SelectedIndex > 0)
                lstResult.SelectedIndex--;
        }

        private void btnNextTitle_Click(object sender, RoutedEventArgs e)
        {
            if (s2t.IsRunning) return;
            if (lstResult.SelectedIndex < lstResult.Items.Count - 1)
                lstResult.SelectedIndex++;
        }
    }
}
