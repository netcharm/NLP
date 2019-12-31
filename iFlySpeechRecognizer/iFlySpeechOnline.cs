using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
//using WebSocketSharp;

namespace iFly
{
    public enum RequestStatus { First = 0, Middle = 1, Last = 2 };
    public class DataFirst
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

        public DataFirst(string appid)
        {
            common.app_id = appid;                
        }
    }

    public class DataMiddle
    {
        public class Data
        {
            public int status { get; set; } = 0;
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

    /// <summary>
    /// Coding Source: https://my.oschina.net/u/1020719/blog/3104193
    /// </summary>
    public class EncryptHelper
    {
        /// <summary>
        /// HMACSHA1算法加密并返回ToBase64String
        /// </summary>
        /// <param name="strText">签名参数字符串</param>
        /// <param name="strKey">密钥参数</param>
        /// <returns>返回一个签名值(即哈希值)</returns>
        public static string ToBase64hmac(string strText, string strKey)
        {
            HMACSHA1 myHMACSHA1 = new HMACSHA1(Encoding.UTF8.GetBytes(strKey));
            byte[] byteText = myHMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(strText));
            return Convert.ToBase64String(byteText);
        }


        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static String EncryptWithMD5(String source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                //加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位
                strbul.Append(result[i].ToString("x2"));
            }
            return strbul.ToString();
        }

        /// <summary> 
        /// 获取时间戳 
        /// </summary> 
        /// <returns></returns> 
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// 将16进制的字符串转为byte[]
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    }

    public class iFlySpeechOnline
    {
        //public delegate void message_Event(string message);

        //public event message_Event messageUpdate_Event;

        /// <summary>
        /// baseURL
        /// </summary>
        private string baseUrl = "wss://iat-api.xfyun.cn/v2/iat";

        /// <summary>
        /// 源地址
        /// </summary>
        //private string originStr = "https://rtasr.xfyun.cn";

        public string APPID { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;
        public string APISecret { get; set; } = string.Empty;
        public Dictionary<string, string> Results { get; set; } = new Dictionary<string, string>();
        private SemaphoreSlim sem = new SemaphoreSlim(1);
        private Task _wsReceive = null;

        /// <summary>
        /// 建议音频流每40ms发送1280字节，发送过快可能导致引擎出错； 2.音频发送间隔超时时间为15秒，超时服务端报错并主动断开连接。
        /// </summary>
        private int sendSize = 1280;
        private int sendDelay = 40;

        //private WebSocketSharp.WebSocket _ws;
        private ClientWebSocket _ws;
        //private CancellationToken _wsCancellation = new CancellationToken();

        private HMACSHA256 hash = null;

        private string ComputeHash(string src, string apisecret)
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
            string signature = ComputeHash(signatureOrigin, apisecret);
            string authUrl = $"api_key=\"{apikey}\", algorithm=\"hmac-sha256\", headers=\"host date request-line\", signature=\"{signature}\"";
            string authorization = BASE64(authUrl);

            var query = $"{uri.AbsoluteUri}?host={uri.Host}&date={HttpUtility.UrlEncode(date).Replace("+", "%20")}&authorization={authorization}";
            return (query);
        }

        public async Task<bool> Connect()
        {
            if (!(_ws is ClientWebSocket)) _ws = new ClientWebSocket();
            var auths = AssembleAuthUrl(baseUrl, APIKey, APISecret);
            if (_ws.State != WebSocketState.Open)
                await _ws.ConnectAsync(new Uri(auths), CancellationToken.None);
            if (!(_wsReceive is Task) || _wsReceive.Status != TaskStatus.Running)
                _wsReceive = Task.Run(async () => { await Receive(_ws); });
            return (_ws.State == WebSocketState.Open ? true : false);
        }

        public async Task<bool> Disconnect()
        {
            if (!(_ws is ClientWebSocket)) return (true);
            //await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "END", CancellationToken.None);
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "END", CancellationToken.None);
            return (_ws.State == WebSocketState.Closed ? true : false);
        }

        public async Task<bool> Send(byte[] buffer)
        {
            bool result = false;

            try
            {
                var pos = 0;
                var param = new DataFirst(APPID);
                var tail = new ArraySegment<byte>(ToBytes("{\"data\":{\"status\":2}}"));
                //while (pos < buffer.Length)
                //{
                //    ArraySegment<byte> seg = new ArraySegment<byte>();
                //    if (pos + sendSize >= buffer.Length)
                //    {
                //        param.data.status = pos == 0 ? 0 : 1;
                //        param.data.audio = BASE64(buffer.Take(sendSize).ToArray());
                //        var data = JsonConvert.SerializeObject(param);
                //        await _ws.SendAsync(new ArraySegment<byte>(ToBytes(data)), WebSocketMessageType.Text, true, CancellationToken.None);
                //        await Task.Delay(sendDelay);
                //        await _ws.SendAsync(tail, WebSocketMessageType.Text, true, CancellationToken.None);
                //        break;
                //    }
                //    else if (pos == 0)
                //    {
                //        param.data.status = 0;
                //        param.data.audio = BASE64(buffer.Take(sendSize).ToArray());
                //        var data = JsonConvert.SerializeObject(param);
                //        await _ws.SendAsync(new ArraySegment<byte>(ToBytes(data)), WebSocketMessageType.Text, true, CancellationToken.None);
                //        await Task.Delay(sendDelay);
                //    }
                //    else
                //    {
                //        param.data.status = 1;
                //        param.data.audio = BASE64(buffer.Skip(pos).Take(sendSize).ToArray());
                //        var data = JsonConvert.SerializeObject(param);
                //        await _ws.SendAsync(new ArraySegment<byte>(ToBytes(data)), WebSocketMessageType.Text, IsTail, CancellationToken.None);
                //        await Task.Delay(sendDelay);
                //    }
                //    pos += sendSize;
                //}

                while (pos < buffer.Length)
                {
                    var seg = buffer.Skip(pos).Take(sendSize);
                    param.data.status = pos == 0 ? 0 : 1;
                    param.data.audio = BASE64(buffer.Take(sendSize).ToArray());
                    var data = JsonConvert.SerializeObject(param);
                    await _ws.SendAsync(new ArraySegment<byte>(ToBytes(data)), WebSocketMessageType.Text, true, CancellationToken.None);
                    await Task.Delay(sendDelay);
                    pos += sendSize;
                }
                await _ws.SendAsync(tail, WebSocketMessageType.Text, true, CancellationToken.None);
                result = true;
            }
#if DEBUG
            catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
            return (result);
        }

        public async Task Receive()
        {
            try
            {
                while (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseSent || _ws.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        var bytes = new byte[4096];
                        var buffer = new ArraySegment<byte>(bytes);
                        var wsRet = await _ws.ReceiveAsync(buffer, CancellationToken.None);
                        if (wsRet.Count > 0)
                        {
                            var ret = JsonConvert.DeserializeObject<ResultParameter>(ToString(buffer.Array).Substring(0, wsRet.Count));
                            if (Results.ContainsKey(ret.sid)) Results[ret.sid] += ret.data.result.ws.ToString();
                            else Results[ret.sid] = ret.data.result.ws.ToString();
                            await Task.Delay(50);
                        }
                        //wsRet.
                        //if (wsRet.EndOfMessage) break;
                    }
#if DEBUG
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
                }
            }
#if DEBUG
            catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
        }

        public async Task Receive(ClientWebSocket ws)
        {
            try
            {
                if (Results != null)
                {
                    Results.Clear();
                }
                while (true)
                {
                    try
                    {
                        if (ws.CloseStatus == WebSocketCloseStatus.EndpointUnavailable ||
                            ws.CloseStatus == WebSocketCloseStatus.InternalServerError ||
                            ws.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                        {
                            return;
                        }

                        var array = new byte[4096];
                        var receive = await ws.ReceiveAsync(new ArraySegment<byte>(array), CancellationToken.None);
                        if (receive.MessageType == WebSocketMessageType.Text)
                        {
                            if (receive.Count <= 0)
                            {
                                continue;
                            }

                            string msg = Encoding.UTF8.GetString(array, 0, receive.Count);
                            ResultParameter result = JsonConvert.DeserializeObject<ResultParameter>(msg);
                            if (result.code != 0)
                            {
                                throw new Exception($"Result error({result.code}): {result.message}");
                            }
                            if (result.data == null
                                || result.data.result == null
                                || result.data.result.ws == null)
                            {
                                return;
                            }
                            //分析数据
                            StringBuilder itemStringBuilder = new StringBuilder();
                            foreach (var item in result.data.result.ws)
                            {
                                foreach (var child in item.cw)
                                {
                                    if (string.IsNullOrEmpty(child.w))
                                    {
                                        continue;
                                    }
                                    itemStringBuilder.Append(child.w);
                                }
                            }
                            //if (result.data.result.pgs == "apd")
                            //{
                            //    Results.Add(new ResultWPGSInfo()
                            //    {
                            //        sn = result.data.result.sn,
                            //        data = itemStringBuilder.ToString()
                            //    });
                            //}
                            //else if (result.data.result.pgs == "rpl")
                            //{
                            //    if (result.data.result.rg == null || result.data.result.rg.Count != 2)
                            //    {
                            //        continue;
                            //    }
                            //    int first = result.Data.result.rg[0];
                            //    int end = result.Data.result.rg[1];
                            //    try
                            //    {
                            //        ResultWPGSInfo item = _result.Where(p => p.sn >= first && p.sn <= end).SingleOrDefault();
                            //        if (item == null)
                            //        {
                            //            continue;
                            //        }
                            //        else
                            //        {
                            //            item.sn = result.Data.result.sn;
                            //            item.data = itemStringBuilder.ToString();
                            //        }
                            //    }
                            //    catch
                            //    {
                            //        continue;
                            //    }
                            //}

                            if (Results.ContainsKey(result.sid)) Results[result.sid] += itemStringBuilder.ToString();
                            else Results[result.sid] = itemStringBuilder.ToString();

                            //StringBuilder totalStringBuilder = new StringBuilder();
                            //foreach (var item in _result)
                            //{
                            //    totalStringBuilder.Append(item.data);
                            //}

                            //OnMessage?.Invoke(this, totalStringBuilder.ToString());
                            //最后一帧，结束
                            if (result.data.status == 2)
                            {
                                return;
                            }
                        }
                    }
                    catch (WebSocketException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        //服务器主动断开连接
                        if (!ex.Message.ToLower().Contains("unable to read data from the transport connection"))
                        {
                            //OnError?.Invoke(this, new ErrorEventArgs()
                            //{
                            //    Code = ResultCode.Error,
                            //    Message = ex.Message,
                            //    Exception = ex,
                            //});
                        }
                        return;
                    }
                }
            }
#if DEBUG
            catch (Exception ex) { Console.WriteLine(ex.Message); }
#else
            catch (Exception) { }
#endif
        }    

        /// <summary>
        /// 调用讯飞接口
        /// </summary>
        /// <param name="voice"></param>
        public async Task<string> GetSocketValue(byte[] voice)
        {
            string result = string.Empty;
            try
            {
                if (_ws.State == WebSocketState.Open)
                {
                    await Send(voice);
                    //result = await Receive();
                }
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
                if (_ws.State == WebSocketState.Open)
                {
                    await sem.WaitAsync();
                    await Send(voice);
                    //await Receive();
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
