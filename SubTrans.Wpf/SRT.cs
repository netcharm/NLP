﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubTitles
{
    public static class DefaultAssStyle
    {
        public static string ENG_Default { get; } = "Style: Default,Tahoma,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string ENG_Note { get; } = "Style: Note,Times New Roman,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string ENG_Title { get; } = "Style: Title,Arial,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";

        public static string CHS_Default { get; } = "Style: Default,更纱黑体 SC,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHS_Note { get; } = "Style: Note,宋体,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHS_Title { get; } = "Style: Title,更纱黑体 SC,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";

        public static string CHT_Default { get; } = "Style: Default,Sarasa Gothic TC,20,&H19000000,&H19843815,&H37A4F2F7,&HA0A6A6A8,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHT_Note { get; } = "Style: Note,宋体,22,&H19FFF907,&H19DC16C8,&H371E4454,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
        public static string CHT_Title { get; } = "Style: Title,Sarasa Gothic TC,28,&H190055FF,&H1948560E,&H37EAF196,&HA0969696,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1";
    }

    public static class DefaultVttHeader
    {
        public static string Style { get; } = @"video::cue {
  background-color: transparent;
  color: yellow;
}";

    }

    public class SimpleTitle
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; } = 100.0;
    }

    public class SrtBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TimeSpan Start { get; set; } = TimeSpan.FromSeconds(0);
        public TimeSpan End { get; set; } = TimeSpan.FromSeconds(0);

        public TimeSpan NewStart { get; set; } = TimeSpan.FromSeconds(0);
        public TimeSpan NewEnd { get; set; } = TimeSpan.FromSeconds(0);

        public int Index { get; set; } = 0;
        public int DisplayIndex { get { return (Index + 1); } }

        private string text = string.Empty;
        public string Text
        {
            get { return (text); }
            set
            {
                text = value;
                NotifyPropertyChanged("Text");
                NotifyPropertyChanged("MultiLingoText");
            }
        }
        public double Confidence { get; set; } = 100.0;

        private string translated = string.Empty;
        public string TranslatedText
        {
            get { return (translated); }
            set
            {
                translated = value;
                NotifyPropertyChanged("Text");
                NotifyPropertyChanged("TranslatedText");
            }
        }

        public string MultiLingoText
        {
            get
            {
                var translated = string.IsNullOrEmpty(TranslatedText) ? string.Empty : $"\\n{TranslatedText}";
                return ($"{Text}{translated}");
            }
        }
        public List<SimpleTitle> AltTitle { get; set; } = new List<SimpleTitle>();

        public override string ToString()
        {
            var translated = string.IsNullOrEmpty(TranslatedText) ? string.Empty : $"\n{TranslatedText}";
            return ($"[{DisplayIndex}]:{Text}{translated}");
        }

        public string Title
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{DisplayIndex}");
                sb.AppendLine($"{NewStart.ToString(@"hh\:mm\:ss\,fff")} --> {NewEnd.ToString(@"hh\:mm\:ss\,fff")}");
                sb.AppendLine($"{Text}");
                sb.AppendLine();
                return (sb.ToString());
            }
        }

        public string TranslatedTitle
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{DisplayIndex}");
                sb.AppendLine($"{NewStart.ToString(@"hh\:mm\:ss\,fff")} --> {NewEnd.ToString(@"hh\:mm\:ss\,fff")}");
                sb.AppendLine($"{MultiLingoText}");
                sb.AppendLine();
                return (sb.ToString());
            }
        }

        public string Language { get; set; }

        public string LRC
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{NewStart.ToString(@"hh\:mm\:ss\.fff")}] {MultiLingoText}");
                sb.AppendLine($"[{NewEnd.ToString(@"hh\:mm\:ss\.fff")}]");
                return (sb.ToString());
            }
        }
    }

    public class SrtCollection
    {
        public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
        public List<SrtBase> Contents { get; } = new List<SrtBase>();

        private string vtt_header = string.Empty;

        public async void Load(string file)
        {
            if (File.Exists(file))
            {
                var contents = File.ReadAllLines(file);
                await Load(contents);
            }
        }

        public async Task Load(IEnumerable<string> lines)
        {
            var contents = lines.ToList();
            if (contents.Count > 3)
            {
                Contents.Clear();
                var index = -1;
                var text = string.Empty;
                TimeSpan start = default(TimeSpan);
                TimeSpan end = default(TimeSpan);
                string line = string.Empty;

                for (var i = 0; i < contents.Count; i++)
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

                            if (index == 0)
                            {
                                vtt_header = string.Join(Environment.NewLine, contents.Take(i - 1)).Trim();
                            }
                        }
                        else if (index >= 0 && string.IsNullOrEmpty(contents[i].Trim()))
                        {
                            var title = new SrtBase()
                            {
                                Index = index,
                                Language = Culture.IetfLanguageTag,
                                Start = start,
                                End = end,
                                NewStart = start,
                                NewEnd = end,
                                Text = text.Trim()
                            };
                            Contents.Add(title);
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
                    if (Contents.Count < 100) await Task.Delay(1);
                }
            }
        }

        public string ToAssText(string title = "Untitled")
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[Script Info]");
            sb.AppendLine($"Title: {title}");
            sb.AppendLine();
            sb.AppendLine($"[V4+ Styles]");
            sb.AppendLine($"Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            //sb.AppendLine($"Style: Default,Tahoma,24,&H00FFFFFF,&HF0000000,&H00000000,&HF0000000,1,0,0,0,100,100,0,0.00,1,1,0,2,30,30,10,1");
            sb.AppendLine($"{DefaultAssStyle.CHS_Default}");
            sb.AppendLine($"{DefaultAssStyle.CHS_Note}");
            sb.AppendLine($"{DefaultAssStyle.CHS_Title}");
            sb.AppendLine();
            sb.AppendLine($"[Events]");
            sb.AppendLine($"Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var t in Contents)
            {
                sb.AppendLine($"Dialogue: 0,{t.NewStart.ToString(@"hh\:mm\:ss\.ff")},{t.NewEnd.ToString(@"hh\:mm\:ss\.ff")},Default,NTP,0000,0000,0000,,{t.MultiLingoText}");
            }

            return (sb.ToString());
        }

        public List<string> ToAss(string title = "Untitled")
        {
            List<string> sb = new List<string>();

            sb.Add($"[Script Info]");
            sb.Add($"Title: {title}");
            sb.Add("");
            sb.Add($"[V4+ Styles]");
            sb.Add($"Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            //sb.AppendLine($"Style: Default,Tahoma,24,&H00FFFFFF,&HF0000000,&H00000000,&HF0000000,1,0,0,0,100,100,0,0.00,1,1,0,2,30,30,10,1");
            sb.Add($"{DefaultAssStyle.CHS_Default}");
            sb.Add($"{DefaultAssStyle.CHS_Note}");
            sb.Add($"{DefaultAssStyle.CHS_Title}");
            sb.Add("");
            sb.Add($"[Events]");
            sb.Add($"Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var t in Contents)
            {
                var text = t.MultiLingoText.Replace("\r\n", "\\N").Replace("\n\r", "\\N").Replace("\r", "\\N").Replace("\n", "\\N");
                sb.Add($"Dialogue: 0,{t.NewStart.ToString(@"hh\:mm\:ss\.ff")},{t.NewEnd.ToString(@"hh\:mm\:ss\.ff")},Default,NTP,0000,0000,0000,,{text}");
            }

            return (sb);
        }

        public string ToSRT()
        {
            string result = string.Empty;
            if (Contents is IEnumerable<SrtBase>)
            {
                StringBuilder sb = new StringBuilder();
                foreach (SrtBase s in Contents)
                {
                    sb.AppendLine($"{s.DisplayIndex}");
                    sb.AppendLine($"{s.NewStart.ToString(@"hh\:mm\:ss\,fff")} --> {s.NewEnd.ToString(@"hh\:mm\:ss\,fff")}");
                    sb.AppendLine($"{s.MultiLingoText}");
                    sb.AppendLine();
                }
                result = sb.ToString();
            }
            return (result);
        }

        public string ToVtt()
        {
            if (string.IsNullOrEmpty(vtt_header) || vtt_header.StartsWith("WEBVTT", StringComparison.CurrentCultureIgnoreCase))
                return ($"WEBVTT{Environment.NewLine}{DefaultVttHeader.Style}{Environment.NewLine}{ToSRT()}");
            else
                return ($"{vtt_header}{Environment.NewLine}{ToSRT()}");
        }
    }
}
