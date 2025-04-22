//LT OCM芯片烧写工具DLL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        IntPtr mainHandle = IntPtr.Zero; // 主窗口句柄
        static bool isTimeOut = false; // 是否超时标志
        string burnFilePath = "";//烧录文件路径
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
        private void InitializeChip(IntPtr comboBoxHandle, string defaultSelect)
        {
            //默认选中指定的内容
            if (!string.IsNullOrEmpty(defaultSelect))
            {
                //选择字符串
                SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(defaultSelect));
            }
        }

        /// <summary>
        /// 获取控件handle
        /// </summary>
        /// <param name="CompareCtrlId">指定控件ID</param>
        /// <returns></returns>
        private IntPtr SetControlHandles(int CompareCtrlId)
        {
            foreach (var handle in controlHandles)
            {
                int ctrlId = GetDlgCtrlID(handle);//获取控件ID
                if (ctrlId == CompareCtrlId) //判断控件是否为烧录按钮
                {
                    return handle;
                    break;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 清除Log记录
        /// </summary>
        /// <returns>如果Log内容长度小于2则返回true</returns>
        private bool ClearLog()
        {
            IntPtr clearBtnHandle = SetControlHandles(1014);//获取清除按钮控件句柄
            ClickButton(clearBtnHandle);//模拟点击清除按钮
            Thread.Sleep(100);//等待清除完成
            string log = ReadLog();//读取Log记录
            if (log.Length < 2) return true;
            return false;
        }

        /// <summary>
        /// 判断动作是否完成
        /// </summary>
        /// <param name="timeOut">超时设置</param>
        private bool ActionIsDone(out string logStr, int timeOut)
        {
            //判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到timeOut则表示完成
            int count = 0;
            while (true)
            {
                logStr = ReadLog();
                if (logStr.Length > 3 || count >= timeOut*1000)
                {
                    isTimeOut = true;
                    return true;
                }
                Thread.Sleep(100);
                count += 100;
            }
            return false;
        }

        #endregion


        #region 声明调用方法

        #region 打开烧录软件exe
        /// <summary>
        /// 打开烧录软件exe
        /// </summary>
        /// <param name="exePath">exe路径</param>
        /// <param name="waitForOpenDone">等待exe打开完成时间</param>
        public void OpenUpgradeFlash(string exePath, int waitForOpenDone,string chipSelection)
        {
            //启动烧录软件exe
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);//设置工作目录
            process.StartInfo.UseShellExecute = true;
            process.Start();

            //等待进程加载完成
            Thread.Sleep(waitForOpenDone);
            process.WaitForInputIdle(); // 等待进程进入空闲状态

            //获取主窗口句柄
            mainHandle = process.MainWindowHandle;
            //如果主窗口句柄为空，则等待一段时间再次获取
            if(mainHandle == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                mainHandle = process.MainWindowHandle;
            }

            //获取所有子控件句柄
            EnumChildWindows(mainHandle,EnumChildCallback, IntPtr.Zero);

            //初始化Chip控件
            IntPtr comboBoxHandle = FindWindowEx(mainHandle, IntPtr.Zero, "ComboBox", null);
            InitializeChip(comboBoxHandle,  chipSelection);//加载XML并初始化ComboBox控件Chip
        }
        #endregion

        #region 选择芯片型号
        public bool SelectChip(string chipName)
        {
            IntPtr comboBoxHandle = FindWindowEx(mainHandle, IntPtr.Zero, "ComboBox", null);
            SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(chipName));
            StringBuilder chipContent = null;
            int comboBoxContentLength = SendMessage(comboBoxHandle, WM_GETTEXTLENGTH, 0, chipContent);
            if(comboBoxContentLength > 0)
            {
                chipContent = new StringBuilder(comboBoxContentLength + 1);
                //获取文本内容
                SendMessage(comboBoxHandle, WM_GETTEXT, comboBoxContentLength + 1, chipContent);
                if (chipContent.ToString() == chipName)
                {
                    return true;//判断是否设置成功
                }
            }
            return false;
        }
        #endregion

        #region 烧录
        /// <summary>
        /// 烧录方法
        /// </summary>
        /// <param name="burnFilePath">烧录文件</param>
        /// <param name="waitForDone">烧录完成等待时间</param>
        /// <returns></returns>
        public bool Prog(out string resultStr, int timeOut=3000)
        {
            try
            {
                ClearLog();//清除Log记录
                IntPtr progHandle = SetControlHandles(1001);//获取烧录按钮句柄
                ClickButton(progHandle);//模拟点击烧录按钮
                resultStr = "";
                ActionIsDone(out resultStr, timeOut);//判断烧录是否完成
                string pattern = @".*Program Flash Data.*Succeed.*";
                return Regex.IsMatch(resultStr, pattern);
            }
            catch(Exception ex)
            {
                resultStr = "烧录失败，检查芯片连接";
                return false;
            }
        }
        #endregion

        #region 读取按钮事件
        public bool Read(out string resultStr,int timeOut=3000)
        {
            try
            {
                ClearLog();//清除Log记录
                IntPtr readHandle = SetControlHandles(1002);//获取读取按钮句柄
                ClickButton(readHandle);//模拟点击读取按钮
                resultStr = "";
                ActionIsDone(out resultStr, timeOut);//判断读取是否完成
                resultStr = ReadLog();//读取烧录结果
                string pattern = @".*Read Flash Data.*Succeed.*";//判断烧录结果是否成功的正则表达式
                return Regex.IsMatch(resultStr, pattern);
            }
            catch (Exception ex) {
                resultStr = "读取失败,检查芯片连接";
                return false; 
            }

        }
        #endregion

        #region 擦除按钮事件
        public bool Erase(out string resultStr,int timeOut = 3000)
        {
            try
            {
                ClearLog();//清除Log记录
                IntPtr eraseHandle = SetControlHandles(1021);//获取擦除按钮句柄
                ClickButton(eraseHandle);//模拟点击擦除按钮
                ActionIsDone(out resultStr, timeOut);//判断擦除是否完成
                resultStr = ReadLog();//读取烧录结果
                string pattern = @".*Erase.*Done.*";
                return Regex.IsMatch(resultStr,pattern);
            }
            catch (Exception ex) {
                resultStr = "擦除失败，检查芯片连接";
                return false;
            }


            //判断烧录结果是否成功的正则表达式
            //string pattern = @".*Erase Flash Data.*Succeed.*";
            //return Regex.IsMatch(resultStr, pattern);
        }
        #endregion

        #region 设置烧录文件
        public bool SetBurnFile(string burnFilePath)
        {
            foreach (var handle in controlHandles)
            {
                int ctrlId = GetDlgCtrlID(handle);//获取控件ID
                if (ctrlId == 1004) //判断控件是否为加载HEX文件的控件
                {
                    SendMessage(handle, WM_SETTEXT, 0, burnFilePath);//设置HEX文件路径
                    Thread.Sleep(100);//等待设置完成
                    StringBuilder sb = null;
                    // 获取文本框内容的长度
                    int textLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, sb);
                    if (textLength > 0)
                    {
                        // 创建一个StringBuilder对象来存储文本内容
                        StringBuilder textContent = new StringBuilder(textLength + 1);
                        // 获取文本框内容
                        SendMessage(handle, WM_GETTEXT, textLength + 1, textContent);
                        if(textContent.ToString() == burnFilePath)return true;//判断是否设置成功
                    }
                    break;
                }
            }
            return false;
        }
        #endregion

        #region 清除Log记录
        public bool ClearLogRecord()
        {
            return ClearLog();
        }
        #endregion

        #region 读取Log记录
        public string ReadLog()
        {
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1010)
                {
                    StringBuilder sb = null;
                    // 获取文本框内容的长度
                    int textLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, sb);
                    if (textLength > 0)
                    {
                        // 创建一个StringBuilder对象来存储文本内容
                        StringBuilder textContent = new StringBuilder(textLength + 1);
                        // 获取文本框内容
                        SendMessage(handle, WM_GETTEXT, textLength + 1, textContent);
                        // 将文本内容显示在UI的TextBlock上
                        return textContent.ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            return "";
        }
        #endregion

        #region 关闭烧录软件exe
        public void CloseUpgradeFlash()
        {
            //关闭烧录软件exe
            Process[] processes = Process.GetProcessesByName("Upgrade_Flash_For_Application");
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }
        #endregion


        #endregion
    }
}
