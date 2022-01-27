using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEECH_MS.Common
{
    public class API
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Providor { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Security { get; set; } = string.Empty;
    }

    public class Setting
    {
        // System.Environment.GetCommandLineArgs()[0];
        // System.AppDomain.CurrentDomain.BaseDirectory
        // System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
        // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName)
        static public string APP_ROOT { get; } = AppDomain.CurrentDomain.BaseDirectory;
        static public string APP_NAME { get; } = AppDomain.CurrentDomain.FriendlyName;

        public List<API> API_List { get; set; } = new List<API>();
    }
}
