using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;

            MediaPanel.IsEnabled = false;

            progress = new Progress<Tuple<TimeSpan, TimeSpan>>(i =>
            {
                var received = i.Item1;
                var total = i.Item2;
                progressBar.Value = total.TotalSeconds > 0 ? received.TotalSeconds / total.TotalSeconds * 100 : 0;
                var cs = received.ToString(@"hh\:mm\:ss\,fff");
                var ts = total.ToString(@"hh\:mm\:ss\,fff");
                progressInfo.Text = $"{cs} / {ts} ({progressBar.Value:0.0}%)";
            });

            s2t = new SpeechRecognizer()
            {
                ProgressHost = progress,
                IsCompleted = new Action(() => { btnPlay.IsChecked = false; btnPause.IsChecked = false; btnStop.IsChecked = true; })
            };
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
            try
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
                        foreach (var fn in fns)
                        {
                            var ext = Path.GetExtension(fn).ToLower();

                            if (exts_snd.Contains(ext))
                            {
                                try
                                {
                                    if (s2t is SpeechRecognizer)
                                    {
                                        MediaPanel.IsEnabled = true;
                                        s2t.Start(fn);
                                    }
                                }
                                catch (Exception) { }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                lstResult.ItemsSource = s2t.Result;
                lstResult.UpdateLayout();

                edTitle.Text = string.Empty;

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progress.Report(s2t.Progress);
                s2t.Start();

                btnPlay.IsChecked = s2t.IsRunning;
                btnPause.IsChecked = s2t.IsPausing;
                btnStop.IsChecked = !s2t.IsRunning;
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (s2t.IsPausing)
                    s2t.Resume();
                else
                    s2t.Pause();

                btnPlay.IsChecked = s2t.IsRunning;
                btnPause.IsChecked = s2t.IsPausing;
                btnStop.IsChecked = !s2t.IsRunning;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                s2t.Stop();
                btnPlay.IsChecked = s2t.IsRunning;
                btnPause.IsChecked = s2t.IsPausing;
                btnStop.IsChecked = !s2t.IsRunning;
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.Items.Count > 0) lstResult.SelectedIndex = 0;
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.SelectedIndex > 0) lstResult.SelectedIndex -= 1;
                else lstResult.SelectedIndex = 0;
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                lstResult.SelectedIndex += 1;
                if (lstResult.SelectedIndex < lstResult.Items.Count - 1) lstResult.SelectedIndex += 1;
                else lstResult.SelectedIndex = lstResult.Items.Count - 1;
            }
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.Items.Count > 0) lstResult.SelectedIndex = lstResult.Items.Count - 1;
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
                        MediaPanel.IsEnabled = true;
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

        private void lstResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (s2t is SpeechRecognizer)
            {
                if (lstResult.SelectedItem != null)
                {
                    var srt = lstResult.SelectedItem as SRT;
                    edTitle.Text = srt.Title;
                }
            }
        }

    }
}
