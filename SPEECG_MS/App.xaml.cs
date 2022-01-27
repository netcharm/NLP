using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SPEECH_MS
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public Common.Setting Setting { get; set; } = new Common.Setting();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var config_file = Path.Combine(Common.Setting.APP_ROOT, Path.ChangeExtension(Common.Setting.APP_NAME, ".json"));
            if (File.Exists(config_file))
            {
                var config = File.ReadAllText(config_file);
                Setting = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Setting>(config);
            }
        }
    }
}
