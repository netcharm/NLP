using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SoundToText
{
    static class Common
    {
        private static object ExitFrame(object state)
        {
            ((DispatcherFrame)state).Continue = false;
            return null;
        }

        public static void DoEvents(this object obj)
        {
            try
            {
                //Dispatcher.Yield();
                DispatcherFrame frame = new DispatcherFrame();
                //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(ExitFrame), frame);
                //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);
            }
            catch (Exception)
            {
                //Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));
                //Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Send, new Action(delegate { }));
                //Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate { }));
                //Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(delegate { }));

                DispatcherFrame frame = new DispatcherFrame();
                //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(ExitFrame), frame);
                //await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);
            }
        }

        public static void Sleep(this int ms)
        {
            for (int i = 0; i < ms; i += 10)
            {
                System.Threading.Thread.Sleep(5);
                DoEvents(null);
                //Dispatcher.Yield();
            }
        }


        public static void Log(this string text)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
        }

    }
}
