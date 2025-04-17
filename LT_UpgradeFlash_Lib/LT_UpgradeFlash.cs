//LT OCM芯片烧写工具DLL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LT_UpgradeFlash_Lib
{
    public class LT_UpgradeFlash
    {
        public delegate bool EnumChildProc(IntPtr hwnd, int lParam);//定义枚举子句柄委托

        #region 引入user32.dll
        [DllImport("user32.dll")]
        static extern int EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder caption, int count);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetDlgCtrlID(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SetWindowText(IntPtr hWnd, string lpString);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern int IsWindowEnabled(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);
        #endregion

        #region 定义消息常量
        const int WM_SETTEXT = 0x000C;//设置文本消息
        const int BM_CLICK = 0x00F5;//模拟按钮点击的消息
        const int BM_SETCHECK = 0x00F1; // 设置按钮选中状态
        const int BM_CHECKED = 0x0001; // 按钮选中
        const int BM_UNCHECKED = 0x0000; // 按钮未选中
        const int CB_ADDSTRING = 0x0143; // ComboBox添加字符串消息
        const int CB_SELECTSTRING = 0x014D; // ComboBox选择字符串消息
        const int WM_GETTEXT = 0x000D; // 获取文本的消息
        const int WM_GETTEXTLENGTH = 0x000E; // 获取文本长度的消息
        #endregion

        #region 定义变量
        List<IntPtr> controlHandles = new List<IntPtr>();//存储控件句柄的集合
        static string rootPath = null;//烧录软件exe路径
        IntPtr mainHnadle = IntPtr.Zero; // 主窗口句柄
        #endregion

        
        #region 声明子方法
        /// <summary>
        /// 枚举控件句柄
        /// </summary>
        /// <param name="hwnd">控件句柄</param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            controlHandles.Add(hwnd);
            return true; // 继续枚举
        }
        /// <summary>
        /// 模拟外部进程按钮点击
        /// </summary>
        /// <param name="hwnd">按钮句柄</param>
        private void ClickButton(IntPtr hwnd)
        {
            PostMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// 加载XML并初始化ComboBox控件Chip
        /// </summary>
        /// <param name="comboBoxHandle">控件句柄</param>
        /// <param name="xmlFilePath">xml文件路径</param>
        /// <param name="defaultSelect">下拉框默认选择</param>
        private void InitializeChip(IntPtr comboBoxHandle, string xmlFilePath, string defaultSelect)
        {
            //加载XML文件
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            //遍历所有ChipType节点
            foreach (var chipType in xmlDoc.Descendants("ChipType"))
            {
                string chipName = chipType.Attribute("ChipName")?.Value;
                if (!string.IsNullOrEmpty(chipName))
                {
                    //向ComboBox控件添加字符串
                    SendMessage(comboBoxHandle, CB_ADDSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(chipName));
                }
            }
            //默认选中指定的内容
            if (!string.IsNullOrEmpty(defaultSelect))
            {
                //选择字符串
                SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(defaultSelect));
            }
        }

        #endregion


        #region 声明调用方法

        #region 打开烧录软件exe
        /// <summary>
        /// 打开烧录软件exe
        /// </summary>
        /// <param name="exePath">exe路径</param>
        /// <param name="waitForOpenDone">等待exe打开完成时间</param>
        public void OpenUpgradeFlash(string exePath, int waitForOpenDone,string xmlFilePath,string chipSelection)
        {
            rootPath = exePath;
            //启动烧录软件exe
            Process process = new Process();
            process.StartInfo.FileName = rootPath;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(rootPath);//设置工作目录
            process.StartInfo.UseShellExecute = true;
            process.Start();

            //等待进程加载完成
            Thread.Sleep(waitForOpenDone);
            process.WaitForInputIdle(); // 等待进程进入空闲状态

            //获取主窗口句柄
            mainHnadle = process.MainWindowHandle;
            //如果主窗口句柄为空，则等待一段时间再次获取
            if(mainHnadle == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                mainHnadle = process.MainWindowHandle;
            }

            //获取所有子控件句柄
            EnumChildWindows(mainHnadle,EnumChildCallback, IntPtr.Zero);

            //初始化Chip控件
            IntPtr comboBoxHandle = FindWindowEx(mainHnadle, IntPtr.Zero, "ComboBox", null);
            InitializeChip(comboBoxHandle, xmlFilePath, chipSelection);//加载XML并初始化ComboBox控件Chip
        }
        #endregion

        

        #region 选择芯片型号
        public void SelectChip(string chipName)
        {
            IntPtr comboBoxHandle = FindWindowEx(mainHnadle, IntPtr.Zero, "ComboBox", null);
            SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(chipName));
        }
        #endregion

        #region 烧录
        /// <summary>
        /// 烧录方法
        /// </summary>
        /// <param name="hexFilePath">烧录文件</param>
        public string Prog(string hexFilePath,int waitForDone)
        {
            foreach(var handle in controlHandles)
            {
                int ctrlId = GetDlgCtrlID(handle);//获取控件ID
                if(ctrlId == 1004) //判断控件是否为加载HEX文件的控件
                {
                    SendMessage(handle, WM_SETTEXT, 0, hexFilePath);//设置HEX文件路径
                }
                if(ctrlId == 1001)//判断控件是否为烧录按钮
                {
                    //设置焦点
                    //SetForegroundWindow(handle);
                    //SetFocus(handle);
                    //模拟点击
                    ClickButton(handle);
                    break;
                }
            }
            //等待烧录完成
            Thread.Sleep(waitForDone);//等待烧录完成
            string result = ReadLog();//读取烧录结果


            return "";
        }
        #endregion

        #region 读取烧录结果
        
        #endregion


        #region 读取Log记录
        public string ReadLog()
        {
            StringBuilder result = new StringBuilder(256);
            foreach (var handle in controlHandles)
            {
                int ctrlId = GetDlgCtrlID(handle);//获取控件ID
                if (ctrlId == 1010) //判断控件是否为结果显示控件
                {
                    StringBuilder textContent = null;
                    //获取文本长度
                    int length = SendMessage(handle, WM_GETTEXTLENGTH, 0, textContent);
                    if(length > 0)
                    {
                        //获取文本内容
                        textContent = new StringBuilder(length + 1);
                        //获取文本内容
                        SendMessage(handle, WM_GETTEXT, textContent.Capacity, textContent);
                        result.Append(textContent.ToString());
                    }
                    break;
                }
            }
            return result.ToString();
        }
        #endregion

        #region 关闭烧录软件exe
        public void CloseUpgradeFlash()
        {
            //关闭烧录软件exe
            Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(rootPath));
            foreach (Process process in processes)
            {
                process.Kill();
            }
        }
        #endregion


        #endregion
    }
}
