using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LT_UpgradeFlash_dll
{
    public class LT_Class
    {
        #region 引用user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);//查找窗口句柄
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);//阻塞发送消息
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);//不阻塞发送消息
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder caption, int count);//获取窗口标题
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);//获取窗口标题长度
        [DllImport("user32.dll")]
        static extern int EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);//枚举子窗口
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);//获取控件类名
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetDlgCtrlID(IntPtr hWnd);//获取控件ID
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SetForegroundWindow(IntPtr hWnd);//设置窗口为前景窗口
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);//查找子窗口句柄

        #endregion

        #region 变量声明
        public delegate bool EnumChildProc(IntPtr hwnd, int lParam);//定义枚举子句柄委托
        List<IntPtr> DialogControlHandles = new List<IntPtr>();//文件对话框控件handle集合
        IntPtr exeHandle = IntPtr.Zero; // 主窗口句柄
        List<IntPtr> exeControlHandles = new List<IntPtr>();//exe控件句柄集合
        #endregion

        #region 定义消息常量
        const int BM_CLICK = 0x00F5;//模拟按钮点击的消息
        const int WM_GETTEXT = 0x000D; // 获取文本的消息
        const int WM_GETTEXTLENGTH = 0x000E; // 获取文本长度的消息
        const int CB_SHOWDROPDOWN = 0x014F;//下拉列表框显示下拉列表的消息
        const int CB_SETCURSEL = 0x014E;//设置下拉列表框当前选中项的消息
        const int WM_COMMAND = 0x0111;//发送命令消息
        const int CBN_SELCHANGE = 1;//下拉列表框选中项改变的消息
        const int CB_SELECTSTRING = 0x014D; // ComboBox选择字符串消息
        const int WM_SETTEXT = 0x000C;//设置文本消息
        #endregion

        #region 子方法

        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="dialogTitle">对话框标题</param>
        /// <param name="captionName"></param>
        private bool CloseDialog(string dialogTitle,string captionName)
        {
            //获取对话框句柄
            IntPtr dialogHandle = new IntPtr();
            dialogHandle = IntPtr.Zero;
            string title = string.Empty;
            StringBuilder sb  = new StringBuilder(256);
            int lenth = 0;

            DateTime start = DateTime.Now;
            while((DateTime.Now - start).TotalSeconds <1.5)
            {
                dialogHandle = FindWindow("#32770",null);
                title  = GetWindowTitle(dialogHandle);
                if ((dialogHandle != IntPtr.Zero) && (title == dialogTitle))
                {
                    SetForegroundWindow(dialogHandle);
                    //枚举对话框获取对话框所有控件句柄
                    EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0);
                    StringBuilder className = new StringBuilder(256);

                    //遍历控件句柄，查找并点击指定按钮
                    foreach (var handle in DialogControlHandles)
                    {
                        //获取控件类型
                        GetClassName(handle, className, className.Capacity);
                        string classNameStr = className.ToString();

                        //判断控件类型是否为按钮并且按钮文本是否为指定文本，是则点击
                        if (classNameStr == "Button" && GetWindowTitle(handle) == captionName)
                        {
                            //点击按钮
                            SendMessage(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                            return true;
                        }
                    }
                    break;
                }
                Thread.Sleep(100);
            }
            return false;
        }
        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="captionName">控件标题</param>
        private bool CloseDialog(string captionName)
        {
            //获取对话框句柄
            IntPtr dialogHandle = new IntPtr();
            dialogHandle = IntPtr.Zero;
            string title = string.Empty;
            StringBuilder sb = new StringBuilder(256);
            int lenth = 0;

            for (int count = 0; count < 2000; count += 100)
            {
                dialogHandle = FindWindow("#32770", null);
                title = GetWindowTitle(dialogHandle);
                if ((dialogHandle != IntPtr.Zero))
                {
                    //枚举对话框获取对话框所有控件句柄
                    EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0);
                    StringBuilder className = new StringBuilder(256);

                    //遍历控件句柄，查找并点击指定按钮
                    foreach (var handle in DialogControlHandles)
                    {
                        //获取控件类型
                        GetClassName(handle, className, className.Capacity);
                        string classNameStr = className.ToString();

                        //判断控件类型是否为按钮并且按钮文本是否为指定文本，是则点击
                        if (classNameStr == "Button" && GetWindowTitle(handle) == captionName)
                        {
                            //点击按钮
                            //PostMessage(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                            //点击按钮
                            SendMessage(handle, BM_CLICK, (IntPtr)0, (IntPtr)0);
                            return true;
                        }
                    }
                    break;
                }
            }

            //DateTime start = DateTime.Now;
            //while ((DateTime.Now - start).TotalSeconds < 3)
            //{
            //    dialogHandle = FindWindow("#32770", null);
            //    title = GetWindowTitle(dialogHandle);
            //    if ((dialogHandle != IntPtr.Zero))
            //    {
            //        //枚举对话框获取对话框所有控件句柄
            //        EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0);
            //        StringBuilder className = new StringBuilder(256);

            //        //遍历控件句柄，查找并点击指定按钮
            //        foreach (var handle in DialogControlHandles)
            //        {
            //            //获取控件类型
            //            GetClassName(handle, className, className.Capacity);
            //            string classNameStr = className.ToString();

            //            //判断控件类型是否为按钮并且按钮文本是否为指定文本，是则点击
            //            if (classNameStr == "Button" && GetWindowTitle(handle) == captionName)
            //            {
            //                //点击按钮
            //                //PostMessage(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            //                //点击按钮
            //                SendMessage(handle,BM_CLICK,(IntPtr)0, (IntPtr)0);
            //                return true;
            //            }
            //        }
            //        break;
            //    }
            //    Thread.Sleep(100);
            //}
            return false;
        }
        /// <summary>
        /// 枚举子窗口回调函数
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            exeControlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        /// <summary>
        /// 获取对话框控件句柄
        /// </summary>
        /// <param name="hwnd">文件对话框窗口handle</param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumDialogChildCallback(IntPtr hwnd, int lParam)
        {
            DialogControlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        /// <summary>
        /// 获取对话框标题
        /// </summary>
        /// <param name="intPtr"></param>
        /// <returns></returns>
        private string GetWindowTitle(IntPtr intPtr)
        {
            int length = GetWindowTextLength(intPtr);
            if (length == 0)
            {
                return string.Empty;
            }
            StringBuilder stringBuilder = new StringBuilder(length + 1);
            GetWindowText(intPtr, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 通过控件ID模拟点击按钮
        /// </summary>
        /// <param name="handle">按钮控件句柄</param>
        /// <param name="controlID">控件ID</param>
        private void ClickButton(int controlID)
        {
            foreach (var handle in exeControlHandles)
            {
                //获取控件ID
                int id = GetDlgCtrlID(handle);
                if (id == controlID)
                {
                    //模拟点击按钮
                    PostMessage(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                    break;
                }
            }
        }

        /// <summary>
        /// 通过控件handle模拟外部进程按钮点击
        /// </summary>
        /// <param name="hwnd">按钮句柄</param>
        private void ClickButton(IntPtr hwnd)
        {
            PostMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// 读取日志
        /// </summary>
        /// <returns></returns>
        private string ReadLog()
        {
            foreach (var handle in exeControlHandles)
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

        /// <summary>
        /// 判断动作是否完成
        /// </summary>
        /// <param name="logStr"></param>
        /// <param name="pattern"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        private bool ActionIsDone(out string logStr, string pattern, int timeOut)
        {
            //判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到timeOut则表示完成
            int count = 0;
            bool isTimeOut = false;
            while (true)
            {
                logStr = ReadLog();
                if (Regex.IsMatch(logStr, pattern) || count >= timeOut)
                {
                    isTimeOut = true;
                    return Regex.IsMatch(logStr, pattern);
                }
                Thread.Sleep(100);
                count += 100;
            }
            return false;
        }

        /// <summary>
        /// 清除Log记录
        /// </summary>
        /// <returns>如果Log内容长度小于2则返回true</returns>
        private bool ClearLog()
        {
            ClickButton(1014);//模拟点击清除按钮
            Thread.Sleep(100);//等待清除完成
            string log = ReadLog();//读取Log记录
            if (log.Length < 2) return true;
            return false;
        }

        /// <summary>
        /// 设置烧录文件路径为空
        /// 单独设置烧录文件路径无法触发exe加载烧录文件需要通过Prog按钮的点击来触发加载文件对话框才可正常加载
        /// 所以每次点击烧录按钮前需要先置空烧录文件路径
        /// </summary>
        /// <returns></returns>
        private bool SetBurnFileEmpty()
        {
            foreach (var handle in exeControlHandles)
            {
                int ctrlId = GetDlgCtrlID(handle);//获取控件ID
                if (ctrlId == 1004) //判断控件是否为加载HEX文件的控件
                {
                    SendMessage(handle, WM_SETTEXT, 0, "");//设置HEX文件路径
                    Thread.Sleep(100);//等待设置完成
                    StringBuilder sb = null;
                    // 获取文本框内容的长度
                    int textLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, sb);
                    // 创建一个StringBuilder对象来存储文本内容
                    StringBuilder textContent = new StringBuilder(textLength + 1);
                    // 获取文本框内容
                    SendMessage(handle, WM_GETTEXT, textLength + 1, textContent);
                    if (textContent.ToString() == "") return true;//判断是否设置成功
                }
            }
            return false;
        }

        #endregion

        #region dll调用方法

        #region 打开exe
        public bool OpenExe(string exePath,int waitForOpenDone)
        {
            #region 打开exe
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);//设置工作目录
            process.StartInfo.UseShellExecute = true;
            process.Start();
            #endregion

            //判断是否弹出Error提示框，如果弹出则点击确定按钮关闭对话框
            CloseDialog("Error", "确定");

            process.WaitForInputIdle(waitForOpenDone);//等待exe进程进入空闲状态
            //获取exe窗口句柄
            exeHandle = process.MainWindowHandle;
            //如果exe窗口句柄为IntPtr.Zero，则等待1秒后再次获取
            if (exeHandle == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                exeHandle = process.MainWindowHandle;
            }

            //获取exe窗口所有控件句柄
            exeControlHandles.Clear();
            EnumChildWindows(exeHandle, EnumChildCallback, (IntPtr)0);

            //判断exe打开是否成功
            if((exeHandle != IntPtr.Zero)&&(GetWindowTitle(exeHandle)=="UpgradeFlash")) return true;
            else return false;
        }
        #endregion

        #region 关闭exe
        public void CloseExe()
        {
            //关闭exe
            if (exeHandle != IntPtr.Zero)
            {
                Process[] processes = Process.GetProcessesByName("Upgrade_Flash_For_Application");
                foreach (Process process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
                
            }
        }
        #endregion

        #region 点击Read按钮
        public bool Read(out string resultString,int timeOut)
        {
            ClearLog();//清除Log记录
            ClickButton(1002);//点击Read按钮
            CloseDialog("Error", "确定");//判断是否弹出Error提示框，如果弹出则点击确定按钮关闭对话框

            //判断Read按钮是否被点击成功
            string pattern = @".*Read FLASH Data.*Done.*";
            resultString = ReadLog();//读取日志
            return ActionIsDone(out resultString, pattern, timeOut);//判断Read按钮是否被点击成功
        }
        #endregion

        #region 点击Erase按钮
        public bool Erase(out string resultString, int timeOut)
        {
            ClearLog();//清除Log记录
            ClickButton(1021);//点击Erase按钮
            Thread.Sleep(1000);
            for(int i=0;i<30;i++)
            {
                if (CloseDialog("Info","确定")) break;
            }
            for(int i = 0; i < 3; i++)
            {
                if (CloseDialog("Error", "确定")) break;
            }
            //判断Erase按钮是否被点击成功
            string pattern = @".*Done.*";
            resultString = ReadLog();//读取日志
            return ActionIsDone(out resultString, pattern, timeOut);//判断Erase按钮是否被点击成功
        }
        #endregion

        #region 设置芯片类型
        public bool SelectChip(string chipName)
        {
            IntPtr comboBoxHandle = FindWindowEx(exeHandle, IntPtr.Zero, "ComboBox", null);//获取芯片型号下拉框handle
            SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(chipName));

            if (comboBoxHandle != IntPtr.Zero)
            {
                //展开下拉列表
                SendMessage(comboBoxHandle, CB_SHOWDROPDOWN, (IntPtr)1, IntPtr.Zero);
                System.Threading.Thread.Sleep(500); //等待下拉动画
                switch (chipName)
                {
                    case "LT6911":
                        SendMessage(comboBoxHandle, CB_SETCURSEL, (IntPtr)13, IntPtr.Zero);
                        break;
                    case "LT9611":
                        SendMessage(comboBoxHandle, CB_SETCURSEL, (IntPtr)14, IntPtr.Zero);
                        break;
                }

                // 关闭下拉框
                SendMessage(comboBoxHandle, CB_SHOWDROPDOWN, (IntPtr)0, IntPtr.Zero);
                System.Threading.Thread.Sleep(100);
                // 4. 触发事件
                int comboBoxId = GetDlgCtrlID(comboBoxHandle);
                IntPtr wParam = (IntPtr)((CBN_SELCHANGE << 16) | (comboBoxId & 0xFFFF));
                PostMessage(exeHandle, WM_COMMAND, wParam, comboBoxHandle);
                CloseDialog("Error","确定");
            }

            StringBuilder address = new StringBuilder();
            foreach (var handle in exeControlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1009)
                {
                    int length = SendMessage(handle, WM_GETTEXTLENGTH, (IntPtr)0, (IntPtr)0);
                    SendMessage(handle, WM_GETTEXT, address.Capacity, address);
                    break;
                }
            }

            if (address.ToString() != "") return true;
            else return false;
        }
        #endregion

        #region 烧录
        public bool Prog(out string resultStr,string progFilePath,int timeOut)
        {
            SetBurnFileEmpty();//设置烧录文件路径为空
            ClearLog();
            ClickButton(1001);//模拟点击烧录按钮
            IntPtr dialogHandle = IntPtr.Zero;//文件对话框Handle

            resultStr = "";
            #region 获取烧录文件对话框句柄
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 1)
            {
                // 标准文件对话框类名为#32770
                dialogHandle = FindWindow("#32770", null);
                Thread.Sleep(100);
            }
            #endregion

            DialogControlHandles.Clear();
            #region 获取文件选择框handle信息
            EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0); // 枚举子窗口
            for (int i = 0; i < DialogControlHandles.Count; i++)
            {
                IntPtr subHandle = DialogControlHandles[i]; // 获取句柄
                StringBuilder className = new StringBuilder(256);
                StringBuilder text = new StringBuilder(256); // 创建StringBuilder对象用于存储类名和标题
                GetClassName(subHandle, className, className.Capacity); // 获取类名
                string controlType = className.ToString(); // 获取控件类型

                //判断是否为文件对话框的文件名输入框
                if (controlType == "Edit")
                {
                    SendMessage(subHandle, WM_SETTEXT, 0, progFilePath); // 设置文件名
                    break;
                }
            }
            #endregion

            #region 模拟点击文件对话框打开(O)按钮
            // 多语言适配按钮标题

            IntPtr openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "打开(&O)");
            if (openButton == IntPtr.Zero)
                openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "Open(&O)");
            ClickButton(openButton);
            #endregion
            CloseDialog("Error", "确定");//判断是否弹出Error提示框，如果弹出则点击确定按钮关闭对话框
            string pattern = @".*Prog Flash Data.*Succeed.*";
            return ActionIsDone(out resultStr, pattern, timeOut);//判断烧录是否完成 ;
        }
        #endregion

        #endregion
    }
}
