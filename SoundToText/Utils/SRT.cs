﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

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
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public byte[] Audio { get; set; }
        public TimeSpan Start { get; set; } = TimeSpan.FromSeconds(0);
        public TimeSpan End { get; set; } = TimeSpan.FromSeconds(0);

        public byte[] NewAudio { get; set; }
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
        internal protected void SetAltText(IEnumerable<RecognizedPhrase> alt)
        {
            if (!(AltTitle is List<SimpleTitle>))
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

}
