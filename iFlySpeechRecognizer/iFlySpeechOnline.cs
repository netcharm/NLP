using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebSocketSharp;

namespace iFly
{
    public enum RequestStatus { First = 0, Middle = 1, Last = 2 };
    public class DataFirstFrame
    {
        public class Common
        {
            public string app_id { get; set; } = string.Empty;
        }

        public class Business
        {
            public string language { get; set; } = "zh-cn";
            public string domain { get; set; } = "iat";
            public string accent { get; set; } = "mandarin";
        }

        public class Data
        {
            public int status { get; set; } = 0;
            public string format { get; set; } = "audio/L16;rate=16000";
            public string encoding { get; set; } = "raw";
            public string audio { get; set; } = string.Empty;
        }

        public Common common { get; set; } = new Common();
        public Business business { get; set; } = new Business();
        public Data data { get; set; } = new Data();

        public DataFirstFrame(string appid)
        {
            common.app_id = appid;                
        }
    }

    public class DataContinueFrame
    {
        public class Data
        {
            public int status { get; set; } = 1;
            public string format { get; set; } = "audio/L16;rate=16000";
            public string encoding { get; set; } = "raw";
            public string audio { get; set; } = string.Empty;
        }

        public Data data { get; set; } = new Data();
    }

    public class DataLastFrame
    {
        public class Data
        {
            public int status { get; set; } = 2;
            public string format { get; set; } = "audio/L16;rate=16000";
            public string encoding { get; set; } = "raw";
            public string audio { get; set; } = string.Empty;
        }

        public Data data { get; set; } = new Data();
    }

    public class ResultParameter
    {
        public class Data
        {
            public class Result
            {
                public class WS
                {
                    public class CW
                    {
                        public string w { get; set; } = string.Empty;
                        public string sc { get; set; } = string.Empty;
                        public string wb { get; set; } = string.Empty;
                        public string wc { get; set; } = string.Empty;
                        public string we { get; set; } = string.Empty;
                        public string wp { get; set; } = string.Empty;
                    }
                    public int bg { get; set; } = -1;
                    public List<CW> cw { get; set; } = new List<CW>();
                }

                public int sn { get; set; } = -1;
                public bool ls { get; set; } = false;
                public int bg { get; set; } = -1;
                public int ed { get; set; } = -1;
                public string pgs { get; set; } = string.Empty;
                public List<int> rg { get; set; } = new List<int>();
                public List<WS> ws { get; set; } = new List<WS>();
            }

            public int status { get; set; } = -1;
            public Result result { get; set; } = new Result();
        }

        public string sid { get; set; } = string.Empty;
        public int code { get; set; } = -1;
        public string message { get; set; } = string.Empty;
        public Data data { get; set; } = new Data();
    }

    public class iFlySpeechOnline
    {
        //public delegate void message_Event(string message);

        //public event message_Event messageUpdate_Event;

        /// <summary>
        /// baseURL
        /// </summary>
        private string baseUrl = "wss://iat-api.xfyun.cn/v2/iat";

        public string APPID { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;
        public string APISecret { get; set; } = string.Empty;
        public Dictionary<string, string> Results { get; set; } = new Dictionary<string, string>();
        private SemaphoreSlim sem = new SemaphoreSlim(1);
        ///private Task _wsReceive = null;

        /// <summary>
        /// 1. 建议音频流每40ms发送1280字节，发送过快可能导致引擎出错；
        /// 2. 音频发送间隔超时时间为15秒，超时服务端报错并主动断开连接。
        /// </summary>
        private int sendSize = 1280;
        private int sendDelay = 40;

        private WebSocketSharp.WebSocket _ws;
        //private ClientWebSocket _ws;
        //private CancellationToken _wsCancellation = new CancellationToken();

        private HMACSHA256 hash = null;

        private string ComputeHashBase64(string src, string apisecret)
        {
            if (!(hash is HMACSHA256)) hash = new HMACSHA256(Encoding.Default.GetBytes(apisecret ?? APISecret ?? string.Empty));
            return (Convert.ToBase64String(hash.ComputeHash(Encoding.Default.GetBytes(src))));
        }

        private string BASE64(string src)
        {
            return (Convert.ToBase64String(Encoding.Default.GetBytes(src)));
        }

        private string BASE64(byte[] src)
        {
            return (Convert.ToBase64String(src));
        }

        private byte[] ToBytes(string src)
        {
            return (Encoding.Default.GetBytes(src));
        }

        private string ToString(byte[] src)
        {
            return (Encoding.Default.GetString(src));
        }

        public string AssembleAuthUrl(string url, string apikey, string apisecret)
        {
            Uri uri = new Uri(url);
            string date = DateTime.UtcNow.ToString("r");
            string signatureOrigin = $"host: {uri.Host}\ndate: {date}\nGET {uri.LocalPath} HTTP/1.1";
            string signature = ComputeHashBase64(signatureOrigin, apisecret);
            string authUrl = $"api_key=\"{apikey}\", algorithm=\"hmac-sha256\", headers=\"host date request-line\", signature=\"{signature}\"";
            string authorization = BASE64(authUrl);

            var query = $"{uri.AbsoluteUri}?host={uri.Host}&date={HttpUtility.UrlEncode(date).Replace("+", "%20")}&authorization={authorization}";
            return (query);
        }

        private void WebSocket_Open(object sender, EventArgs e)
        {

        }

        private void WebSocket_Close(object sender, CloseEventArgs e)
        {

        }

        private void WebSocket_Error(object sender, ErrorEventArgs e)
        {

        }

        private void WebSocket_Message(object sender, MessageEventArgs e)
        {
            if (e.IsText)
            {
                var ret = JsonConvert.DeserializeObject<ResultParameter>(e.Data);
                StringBuilder words = new StringBuilder();
                foreach (var item in ret.data.result.ws)
                {
                    foreach (var child in item.cw)
                    {
                        if (string.IsNullOrEmpty(child.w))
                        {
                            continue;
                        }
                        words.Append(child.w);
                    }
                }
                if (Results.ContainsKey(ret.sid)) Results[ret.sid] += words.ToString();
                else Results[ret.sid] = words.ToString();
            }            
        }

        public bool Connect()
        {
            var auths = AssembleAuthUrl(baseUrl, APIKey, APISecret);
            if (!(_ws is WebSocket))
            {
                _ws = new WebSocket(auths);
                _ws.OnOpen += WebSocket_Open;
                _ws.OnClose += WebSocket_Close;
                _ws.OnMessage += WebSocket_Message;
                _ws.OnError += WebSocket_Error;
            }
            if (_ws.ReadyState != WebSocketState.Open)
            {
                _ws.Connect();
                //_ws.ConnectAsync();
            }
            return (_ws.ReadyState == WebSocketState.Open ? true : false);
        }

        public async Task<string> Disconnect()
        {
            var result = string.Empty;
            await Task.Run(() => {
                if (!(_ws is WebSocket))
                {
                    _ws.CloseAsync();
                }
                while (true)
                {
                    if (_ws.ReadyState == WebSocketState.Closed)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var kv in Results)
                        {
                            sb.AppendLine(kv.Value);
                        }
                        result = sb.ToString();
                        break;
                    }
                }
            });
            return (result);
        }

        public bool Send(byte[] buffer)
        {
            bool result = false;
            if (_ws.ReadyState != WebSocketState.Open) return(result);

            try
            {
                var pos = 0;
                //var tail = new ArraySegment<byte>(ToBytes("{\"data\":{\"status\":2}}"));
                //var tail = ToBytes("{\"data\":{\"status\":2}}");
                var tail = new DataLastFrame();
                dynamic param;

                while (pos < buffer.Length)
                {
                    var seg = buffer.Skip(pos).Take(sendSize);
                    if (pos == 0)
                    {
                        param = new DataFirstFrame(APPID);
                    }
                    else
                    {
                        param = new DataContinueFrame();
                    }
                    param.data.audio = BASE64(buffer.Take(sendSize).ToArray());
                    var data = JsonConvert.SerializeObject(param);
                    _ws.SendAsync(data, new Action<bool>(async (ret)=> {
                        if (ret)
                        {
                            Console.WriteLine("#Send OK");
                            await Task.Delay(sendDelay);
                        }
                    }));
                    
                    pos += sendSize;
                }
                _ws.SendAsync(JsonConvert.SerializeObject(tail), new Action<bool>(async (ret) => {
                    if (ret)
                    {
                        Console.WriteLine("#Send Finished");
                        await Task.Delay(sendDelay);
                    }
                }));

                result = true;
            }
#if DEBUG
            catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
            return (result);
        }

        public async Task<string> Recognizer(byte[] voice)
        {
            var result = string.Empty;

            try
            {
                Connect();
                if (_ws.ReadyState == WebSocketState.Open)
                {
                    await sem.WaitAsync();
                    Send(voice);
                    result = await Disconnect();
                    sem.Release();
                }
            }
#if DEBUG
            catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
            return (result);
        }
    }
}
