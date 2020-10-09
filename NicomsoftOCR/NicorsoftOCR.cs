using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSOCR_NameSpace;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;

namespace NicomsoftOCR
{
    class AutoPinner : SafeHandle, IDisposable
    {
        GCHandle _pinnedArray;
        public override bool IsInvalid => handle == IntPtr.Zero;

        public AutoPinner(object obj) : base(IntPtr.Zero, true)
        {
            _pinnedArray = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        public static implicit operator IntPtr(AutoPinner ap)
        {
            return ap._pinnedArray.AddrOfPinnedObject();
        }

        protected override bool ReleaseHandle()
        {
            _pinnedArray.Free();
            return true;
        }
    }

    public class NCOCR
    {
        public string Version { get; set; } = string.Empty;

        public int CfgObj = 0;
        int OcrObj = 0;
        int ImgObj = 0;
        bool Dwn = false;
        Rectangle Frame;
        Image bmp;
        public Graphics g;
        bool NoEvent;

        private IntPtr NicomsoftOCR_Library = IntPtr.Zero;

        const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(uint DirectoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string PathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int AddDllDirectory(string NewDirectory);

        //[DllImport("kernel32.dll")]
        //private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string fileName);
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        private void Initialize(string folder = default(string))
        {
            try
            {
                // Indicate to search libraries in
                // - application directory
                // - paths set using AddDllDirectory or SetDllDirectory
                // - %windows%\system32
                SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);

                // Add the directory of the native dll
                if (string.IsNullOrEmpty(folder))
                    AddDllDirectory(".");
                else
                    AddDllDirectory(folder);

                //var dllFile = Path.Combine(Environment.Is64BitProcess ? "x64" : "x86", TNSOCR.LIBNAME);
                var dllFile = Path.Combine(folder, TNSOCR.LIBNAME);
                NicomsoftOCR_Library = LoadLibrary(dllFile);
            }
            catch(Exception ex)
            {
                ShowError(ex.Message, 0);
            }
        }

        public NCOCR(string folder = default(string))
        {
            try
            {
                Initialize(folder);

                //get NSOCR version
                StringBuilder val;
                val = new StringBuilder(256);
                TNSOCR.Engine_GetVersion(val); //if you get "BadImageFormatException" error here: find and check "LIBNAME" constant in "NSOCR.cs"
                Version = $"[NSOCR version: { val }] ";

                TNSOCR.Engine_SetLicenseKey("AB2A4DD5FF2A"); //required for licensed version only

                //init engine and create ocr-related objects
                TNSOCR.Engine_Initialize();
                TNSOCR.Cfg_Create(out CfgObj);
                TNSOCR.Ocr_Create(CfgObj, out OcrObj);
                TNSOCR.Img_Create(OcrObj, out ImgObj);

                TNSOCR.Cfg_LoadOptions(CfgObj, "Config.dat"); //load options, if path not specified, current folder and folder with NSOCR.dll will be checked

                NoEvent = true;
                NoEvent = false;

                //by default this option is disabled because it takes about 10% of total recognition time
                //enable it to demonstrate this feature
                TNSOCR.Cfg_SetOption(CfgObj, TNSOCR.BT_DEFAULT, "Zoning/FindBarcodes", "1");
                //also enable auto-detection of image inversion
                TNSOCR.Cfg_SetOption(CfgObj, TNSOCR.BT_DEFAULT, "ImgAlizer/Inversion", "2");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, 0);
            }
        }

        ~NCOCR()
        {
            try
            {
                if (ImgObj != 0) TNSOCR.Img_Destroy(ImgObj);
                if (OcrObj != 0) TNSOCR.Ocr_Destroy(OcrObj);
                if (CfgObj != 0) TNSOCR.Cfg_Destroy(CfgObj);
                TNSOCR.Engine_Uninitialize();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, 0);
            }
            finally
            {
                if(NicomsoftOCR_Library != IntPtr.Zero)
                    FreeLibrary(NicomsoftOCR_Library);
            }
        }

        private void ShowError(string api, int err)
        {
            string s;
            s = api + " Error #" + err.ToString("X");
            System.Windows.Forms.MessageBox.Show(s);
        }

        public string Recognize(Image image)
        {
            string result = string.Empty;

            TNSOCR.Img_DeleteAllBlocks(ImgObj);

            int w, h;
            TNSOCR.Img_GetSize(ImgObj, out w, out h);

            return (result);
        }

        public string Recognize(Stream stream)
        {
            string result = string.Empty;

            try
            {
                var count = (int)stream.Length;
                var buffer = new byte[count];
                stream.Read(buffer, 0, count);
                result = Recognize(buffer);
            }
            catch
            {
            }
            finally
            {

            }
            return (result);
        }

        public string Recognize(byte[] bytes)
        {
            string result = string.Empty;

            IntPtr pBuf = IntPtr.Zero;
            try
            {
                TNSOCR.Img_DeleteAllBlocks(ImgObj);

                var count = bytes.Length;
                pBuf = Marshal.AllocHGlobal(count);
                Marshal.Copy(bytes, 0, pBuf, count);

                //GCHandle pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                //pBuf = pinnedArray.AddrOfPinnedObject();
                //// Do your stuff...
                //pinnedArray.Free();

                //using (AutoPinner pBuf = new AutoPinner(bytes))
                //{
                //    // Do your stuff...
                //    int res = TNSOCR.Img_LoadFromMemory(ImgObj, pBuf, count);
                //}

                int res = TNSOCR.Img_LoadFromMemory(ImgObj, pBuf, count);

                int w, h;
                TNSOCR.Img_GetSize(ImgObj, out w, out h);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, 0);
            }
            finally
            {
                Marshal.FreeHGlobal(pBuf);
            }
            return (result);
        }


    }
}
