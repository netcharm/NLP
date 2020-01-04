using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Windows;
using System.Windows.Threading;

namespace iFly
{
    public class SpeechRecognizer
    {
        /// <summary>
        /// code from : 
        /// 1. https://github.com/ilovehotmilk/SmartFactory/blob/master/iFlyDotNet/IFlyAsr.cs
        /// 2. https://www.shijunzh.com/archives/1229
        /// </summary>

        public Dispatcher AppDispather { get; set; } = null;

        public string APPID { get; set; } = string.Empty;

        public int SampleRate { get; set; } = 16000;
        public int BitsPerSample { get; set; } = 16;

        public string Text { get; set; } = string.Empty;
        public Action<string> RecognizerResult = null;
        //public delegate void RecognizerResult(string text);

        public Action iFlyIsCompleted = null;

        private bool IsLogin = false;

        private static string session_begin_params = $"sub=iat,domain=iat,language=zh_cn,accent=mandarin,sample_rate=16000,result_type=plain,result_encoding=UNICODE";

        private void Log(string text)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        private object ExitFrame(object state)
        {
            ((DispatcherFrame)state).Continue = false;
            return null;
        }

        public async void DoEvents()
        {
            try
            {
                if (AppDispather.CheckAccess())
                {
                    //Dispatcher.Yield();
                    DispatcherFrame frame = new DispatcherFrame();
                    //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(ExitFrame), frame);
                    //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(ExitFrame), frame);
                    //Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
                    await AppDispather.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
                    Dispatcher.PushFrame(frame);
                }
            }
            catch (Exception)
            {
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                {
                    await Dispatcher.Yield(DispatcherPriority.Background);
                    //DispatcherFrame frame = new DispatcherFrame();
                    ////await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(ExitFrame), frame);
                    ////await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(ExitFrame), frame);
                    //AppDispather.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
                    //Dispatcher.PushFrame(frame);
                }
            }
        }

        public void Sleep(int ms)
        {
            for (int i = 0; i < ms; i += 10)
            {
                Thread.Sleep(10);
                DoEvents();
            }
        }

        public SpeechRecognizer()
        {
            RecognizerResult = new Action<string>((text) =>
            {
                iFlytekResult(text);
            });
        }

        ~SpeechRecognizer()
        {
            iFlytekQuit();
        }

        public bool iFlytekInit()
        {
            if (IsLogin) return (IsLogin);

            //RecognizerResult += iFlytekResult;

            int res = MscDLL.MSPLogin(null, null, $"appid={APPID}");//用户名，密码，登陆信息，前两个均为空
            if (res != (int)Errors.MSP_SUCCESS)
            {
                //说明登陆失败
                Log("登陆失败！");
                Log("错误编号:" + res);
                IsLogin = false;
                return false;
            }
            IsLogin = true;
            Log("登陆成功！");

            #region 参数
            /*
            *sub:本次识别请求的类型  iat 连续语音识别;   asr 语法、关键词识别,默认为iat
            *domain:领域      iat：连续语音识别  asr：语法、关键词识别    search：热词   video：视频    poi：地名  music：音乐    默认为iat。 注意：sub=asr时，domain只能为asr
            *language:语言    zh_cn：简体中文  zh_tw：繁体中文  en_us：英文    默认值：zh_cn
            *accent:语言区域    mandarin：普通话    cantonese：粤语    lmz：四川话 默认值：mandarin
            *sample_rate:音频采样率  可取值：16000，8000  默认值：16000   离线识别不支持8000采样率音频
            *result_type:结果格式   可取值：plain，json  默认值：plain
            *result_encoding:识别结果字符串所用编码格式  GB2312;UTF-8;UNICODE    不同的格式支持不同的编码：   plain:UTF-8,GB2312  json:UTF-8
            */
            session_begin_params = $"sub=iat,domain=iat,language=zh_cn,accent=mandarin,sample_rate={SampleRate},result_type=plain,result_encoding=UNICODE,asr_res_path=fo|iat/common.jet,grm_build_path=asr/GrmBuilld,engine_type=mixed,mixed_type=realtime";
            #endregion
            return (IsLogin);
        }

        public bool iFlytekQuit()
        {
            try
            {
                //if (IsLogin)
                {
                    int res = MscDLL.MSPLogout();
                    if (res != (int)Errors.MSP_SUCCESS)
                    {
                        //说明登陆失败
                        Log("#退出登录失败！");
                        Log($"#错误编号:{res} : {Enum.GetName(typeof(Errors), res)}");
                        return false;
                    }
                    IsLogin = false;
                    Log("#退出登录成功！");
                }
            }
            catch (Exception) { }
            return true;
        }

        public void iFlySendData(byte[] data)
        {
            string hints = "hiahiahia";
            IntPtr session_id;
            StringBuilder result = new StringBuilder();//存储最终识别的结果
            var aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;//音频状态
            var ep_stat = EpStatus.MSP_EP_LOOKING_FOR_SPEECH;//端点状态
            var rec_stat = RecogStatus.MSP_REC_STATUS_SUCCESS;//识别状态
            int errcode = (int)Errors.MSP_SUCCESS;
            byte[] audio_content = data;  //用来存储音频文件的二进制数据
            int totalLength = 0;//用来记录总的识别后的结果的长度，判断是否超过缓存最大值

            #region Start Recording
            if (audio_content == null)
            {
                var e = "#没有读取到任何内容";
                Log(e);
                iFlytekQuit();//退出登录
                result.Append(e);
            }
            else
            {
                #region QISRSessionBegin
                Log("#开始进行语音听写.......");

                /*
                 * QISRSessionBegin（）；
                 * 功能：开始一次语音识别
                 * 参数一：定义关键词识别||语法识别||连续语音识别（null）
                 * 参数2：设置识别的参数：语言、领域、语言区域。。。。
                 * 参数3：带回语音识别的结果，成功||错误代码
                 * 返回值intPtr类型,后面会用到这个返回值
                 */
                session_id = MscDLL.QISRSessionBegin(null, session_begin_params, ref errcode);
                if (errcode != (int)Errors.MSP_SUCCESS)
                {
                    var e = "#开始一次语音识别失败！";
                    Log(e);
                    iFlytekQuit();
                    result.Append(e);
                }
                else
                {
                    #region QISRAudioWrite
                    /*
                     QISRAudioWrite（）；
                     功能：写入本次识别的音频
                     参数1：之前已经得到的sessionID
                     参数2：音频数据缓冲区起始地址
                     参数3：音频数据长度,单位字节。
                     参数4：用来告知MSC音频发送是否完成     
                           MSP_AUDIO_SAMPLE_FIRST = 1	第一块音频
                           MSP_AUDIO_SAMPLE_CONTINUE = 2	还有后继音频
                           MSP_AUDIO_SAMPLE_LAST = 4	最后一块音频
                     参数5：端点检测器（End-point detected）所处的状态
                           MSP_EP_LOOKING_FOR_SPEECH = 0	还没有检测到音频的前端点。
                           MSP_EP_IN_SPEECH = 1	已经检测到了音频前端点，正在进行正常的音频处理。
                           MSP_EP_AFTER_SPEECH = 3	检测到音频的后端点，后继的音频会被MSC忽略。
                           MSP_EP_TIMEOUT = 4	超时。
                           MSP_EP_ERROR = 5	出现错误。
                           MSP_EP_MAX_SPEECH = 6	音频过大。
                     参数6：识别器返回的状态，提醒用户及时开始\停止获取识别结果
                           MSP_REC_STATUS_SUCCESS = 0	识别成功，此时用户可以调用QISRGetResult来获取（部分）结果。
                           MSP_REC_STATUS_NO_MATCH = 1	识别结束，没有识别结果。
                           MSP_REC_STATUS_INCOMPLETE = 2	正在识别中。
                           MSP_REC_STATUS_COMPLETE = 5	识别结束。
                     返回值：函数调用成功则其值为MSP_SUCCESS，否则返回错误代码。
                     本接口需不断调用，直到音频全部写入为止。上传音频时，需更新audioStatus的值。具体来说:
                     当写入首块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_FIRST
                     当写入最后一块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_LAST
                     其余情况下,将audioStatus置为MSP_AUDIO_SAMPLE_CONTINUE
                     同时，需定时检查两个变量：epStatus和rsltStatus。具体来说:
                     当epStatus显示已检测到后端点时，MSC已不再接收音频，应及时停止音频写入
                     当rsltStatus显示有识别结果返回时，即可从MSC缓存中获取结果
                    */
                    var seg = SampleRate*BitsPerSample/8/5;
                    for (int i = 0; i < audio_content.Length; i += seg)
                    {
                        if (i == 0)
                            aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_FIRST;
                        //else if (i + seg >= audio_content.Length - 1)
                        //    aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_LAST;
                        else
                            aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE;

                        var d = audio_content.Skip(i).Take(seg).ToArray();
                        int r = MscDLL.QISRAudioWrite(session_id, d, (uint)d.Length, aud_stat, ref ep_stat, ref rec_stat);
                        if (r != (int)Errors.MSP_SUCCESS)
                        {
                            var e = $"#写入识别的音频失败: {r} : {Enum.GetName(typeof(Errors), r)}";
                            Log(e);
                            MscDLL.QISRSessionEnd(session_id, hints);
                            iFlytekQuit();
                            result.Append(e);
                            break;
                        }
                    }
                    aud_stat = AudioStatus.MSP_AUDIO_SAMPLE_LAST;
                    int res = MscDLL.QISRAudioWrite(session_id, null, 0, aud_stat, ref ep_stat, ref rec_stat);
                    if (res != (int)Errors.MSP_SUCCESS)
                    {
                        var e = $"#写入识别的音频失败: {res} : {Enum.GetName(typeof(Errors), res)}";
                        Log(e);
                        MscDLL.QISRSessionEnd(session_id, hints);
                        iFlytekQuit();
                        result.Append(e);
                    }
                    #endregion
                    while (IsLogin && RecogStatus.MSP_REC_STATUS_COMPLETE != rec_stat)
                    {
                        #region Get Result
                        //如果没有完成就一直继续获取结果
                        /*
                         QISRGetResult（）；
                         功能：获取识别结果
                         参数1：session，之前已获得
                         参数2：识别结果的状态
                         参数3：waitTime[in]	此参数做保留用
                         参数4：错误编码||成功
                         返回值：函数执行成功且有识别结果时，返回结果字符串指针；其他情况(失败或无结果)返回NULL。
                        */
                        IntPtr now_result = MscDLL.QISRGetResult(session_id, ref rec_stat, 0, ref errcode);
                        if (errcode != (int)Errors.MSP_SUCCESS)
                        {
                            var e = $"#获取结果失败：{errcode} : {Enum.GetName(typeof(Errors), errcode)}";
                            Log(e);
                            result.Append(e);
                            break;
                        }
                        if (now_result != IntPtr.Zero)
                        {
                            var s = Marshal.PtrToStringAnsi(now_result);
                            int length = s.Length;
                            totalLength += length;
                            if (totalLength > 4096)
                            {
                                var e = $"#缓存空间不够 {totalLength}";
                                Log(e);
                                result.Append(e);
                                break;
                            }
                            result.Append(s);
                        }
                        //Thread.Sleep(150);//防止频繁占用cpu
                        Sleep(150);
                        #endregion
                    }

                    if (IsLogin)
                    {
                        #region QISRSessionEnd
                        res = MscDLL.QISRSessionEnd(session_id, hints);
                        if (res != (int)Errors.MSP_SUCCESS)
                        {
                            iFlytekQuit();
                            var e = $"#会话结束失败: {res} : {Enum.GetName(typeof(Errors), res)}";
                            Log(e);
                            result.Append(e);
                        }
                        else
                        {
                            Log("#成功结束会话！");
                        }
                        #endregion
                    }
                }
                Log("#语音听写结束");
                #endregion
            }
            #endregion

            if (RecognizerResult != null)
            {
                RecognizerResult.Invoke(result.ToString());
            }
        }

        // Start is called before the first frame update
        public void iFlytekResult(string result)
        {
            Log(result);
            Text = result;
        }

        //public async Task<string> Recognizer(byte[] buf)
        public async Task<string> Recognizer(byte[] buf)
        {
            string result = string.Empty;

            //await Task.Run(() =>
            var action = new Action(() =>
            {
                if (!string.IsNullOrEmpty(APPID))
                {
                    try
                    {
                        iFlytekInit();
                        iFlySendData(buf);
                        result = Text;
                    }
                    catch (Exception) { }
                }
            });
            await Task.Delay(1);
            await Application.Current.Dispatcher.BeginInvoke(action);

            return (result);
        }
    }

}
