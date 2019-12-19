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

        private enum MediaButtonState { Idle, Running, Pausing, Completed };
        private void SetMediaButtonState(MediaButtonState state)
        {
            bool Valid = s2t is SpeechRecognizer;
            switch (state)
            {
                case MediaButtonState.Idle:
                    btnConvertPlay.IsChecked = Valid && s2t.IsRunning;
                    btnConvertPause.IsChecked = Valid && s2t.IsPausing;
                    //btnConvertStop.IsChecked = true;
                    break;
                case MediaButtonState.Running:
                    btnConvertPlay.IsChecked = Valid && s2t.IsRunning;
                    btnConvertPause.IsChecked = Valid && s2t.IsPausing;
                    //btnConvertStop.IsChecked = false;
                    break;
                case MediaButtonState.Pausing:
                    btnConvertPlay.IsChecked = Valid && s2t.IsRunning;
                    btnConvertPause.IsChecked = Valid && s2t.IsPausing;
                    //btnConvertStop.IsChecked = false;
                    break;
                case MediaButtonState.Completed:
                    btnConvertPlay.IsChecked = Valid && s2t.IsRunning;
                    btnConvertPause.IsChecked = Valid && s2t.IsPausing;
                    //btnConvertStop.IsChecked = true;
                    break;
                default:
                    btnConvertPlay.IsChecked = false;
                    btnConvertPause.IsChecked = false;
                    //btnConvertStop.IsChecked = false;
                    break;
            }
            MediaPanel.IsEnabled = true;
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
            //WaveformViewer.AutoScaleWaveformCache = true;
            //WaveformViewer.AllowRepeatRegions = false;

            UIHelper.Bind(soundEngine, "CanPlay", btnPlay, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanPause", btnPause, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "CanStop", btnStop, IsEnabledProperty);
            UIHelper.Bind(soundEngine, "SelectionBegin", repeatStartTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);
            UIHelper.Bind(soundEngine, "SelectionEnd", repeatStopTimeEdit, TimeEditor.ValueProperty, BindingMode.TwoWay);

            //Resources.MergedDictionaries.Clear();
            ResourceDictionary themeResources = Application.LoadComponent(new Uri("ExpressionDark.xaml", UriKind.Relative)) as ResourceDictionary;
            Resources.MergedDictionaries.Add(themeResources);
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
                    SetMediaButtonState(MediaButtonState.Completed);
                })
            };

            cbLanguage.Items.Clear();
            foreach (var r in SpeechRecognizer.InstalledRecognizers)
            {
                cbLanguage.Items.Add(r.Culture.DisplayName);
                //cbLanguage.Items[cbLanguage.Items.Count-1]
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
                                s2t.AudioFile = fn;
                                SetMediaButtonState(MediaButtonState.Idle);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
        }

        private void lstResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.SelectedItem is SRT)
                {
                    var srt = lstResult.SelectedItem as SRT;
                    edTitle.Text = srt.Title;

                    NAudioEngine.Instance.ChannelPosition = srt.Start.TotalSeconds;
                    NAudioEngine.Instance.SelectionBegin = srt.Start;
                    NAudioEngine.Instance.SelectionEnd = srt.End;
                    //repeatStartTimeEdit.Value = srt.Start;
                    //repeatStopTimeEdit.Value = srt.End;
                }
            }
        }

        private void TitleContent_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (e.Property.Name == "Text" && e.TargetObject is TextBlock)
            {
                var tb = e.TargetObject as TextBlock;
                //                if (tb.Tag is int && (int)(tb.Tag) == lstResult.SelectedIndex + 1)
                {
                    if (lstResult.SelectedItem != null)
                    {
                        var srt = lstResult.SelectedItem as SRT;
                        edTitle.Text = srt.Title;
                        $"{srt.Index}:{srt.Text}, {tb.Text}".Log();
                    }
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
                        s2t.AudioFile = fn;
                        SetMediaButtonState(MediaButtonState.Idle);
                        NAudioEngine.Instance.OpenFile(fn);
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
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        s2t.Start(lstResult.SelectedIndex);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        if (lstResult.SelectedItem is SRT)
                        {
                            var start = NAudioEngine.Instance.SelectionBegin;
                            var end = NAudioEngine.Instance.SelectionEnd;
                            if (start.TotalSeconds < 0)
                                start = TimeSpan.FromSeconds(0);
                            if (end.TotalSeconds > NAudioEngine.Instance.ActiveStream.TotalTime.TotalSeconds)
                                end = NAudioEngine.Instance.ActiveStream.TotalTime;
                            s2t.Start(lstResult.SelectedIndex, start, end);
                        }
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        lstResult.ItemsSource = s2t.Result;
                        lstResult.UpdateLayout();

                        edTitle.Text = string.Empty;

                        progressBar.Minimum = 0;
                        progressBar.Maximum = 100;
                        progress.Report(s2t.Progress);
                        s2t.Start();
                    }
                }
                SetMediaButtonState(MediaButtonState.Running);
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
                //if (lstResult.Items.Count > 0) lstResult.SelectedIndex = 0;
                NAudioEngine.Instance.ChannelPosition = 0;
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                //if (lstResult.SelectedIndex > 0) lstResult.SelectedIndex -= 1;
                //else lstResult.SelectedIndex = 0;
                NAudioEngine.Instance.ChannelPosition -= 0.01;
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

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                //lstResult.SelectedIndex += 1;
                //if (lstResult.SelectedIndex < lstResult.Items.Count - 1) lstResult.SelectedIndex += 1;
                //else lstResult.SelectedIndex = lstResult.Items.Count - 1;
                NAudioEngine.Instance.ChannelPosition += 0.01;
            }
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                //if (lstResult.Items.Count > 0) lstResult.SelectedIndex = lstResult.Items.Count - 1;
                NAudioEngine.Instance.ChannelPosition = NAudioEngine.Instance.ChannelLength;
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (NAudioEngine.Instance.CanPlay)
                    NAudioEngine.Instance.Play();

                //if (lstResult.SelectedIndex >= 0 && lstResult.SelectedIndex < s2t.Result.Count)
                //{
                //    var bs = s2t.Result[lstResult.SelectedIndex].Audio;
                //    if(bs is byte[] && bs.Length > 0)
                //    {
                //        try
                //        {
                //            btnPlay.IsEnabled = false;
                //            using (MemoryStream ms = new MemoryStream())
                //            {
                //                await ms.WriteAsync(bs, 0, bs.Length);
                //                await ms.FlushAsync();
                //                ms.Seek(0, SeekOrigin.Begin);
                //                using (WaveStream ws = new WaveFileReader(ms))
                //                {
                //                    //WaveFileWriter.CreateWaveFile("_test_.wav", ws);
                //                    using (WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                //                    {
                //                        waveOut.Init(ws);
                //                        waveOut.Play();
                //                        while (waveOut.PlaybackState == PlaybackState.Playing)
                //                        {
                //                            100.Sleep();
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //        finally
                //        {
                //            btnPlay.IsEnabled = true;
                //        }
                //    }
                //}
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

    }
}
