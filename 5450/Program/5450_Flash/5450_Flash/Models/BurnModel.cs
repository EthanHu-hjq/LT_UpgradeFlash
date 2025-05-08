using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using _5450_Flash.Base;
using Microsoft.Win32;

namespace _5450_Flash.Models
{
    public class BurnModel:NotifyBase
    {
        #region windows应用接口
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
        string lpszClass, string lpszWindow);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);//通过窗口句柄获取进程ID（PID）
        [DllImport("user32.dll")]
        static extern int EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);//枚举获取所有控件句柄
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);//获取控件类名
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetDlgCtrlID(IntPtr hWnd);//获取控件ID
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);//发送消息
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder caption, int count);//获取窗口标题[DllImport("user32.dll", CharSet = CharSet.Auto)]
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);//不阻塞发送消息
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);//阻塞发送消息
                                                                                          // 导入 SendMessage 函数
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        #endregion


        #region 界面按钮响应命令
        public RelayCommand OpenCmd { get; set; }//打开exe命令
        public RelayCommand CloseCmd { get;set; }//关闭exe命令
        public RelayCommand BrowseCmd { get; set; }//浏览exe命令
        public RelayCommand ReadVerCmd { get; set; }//读取版本命令
        public RelayCommand EraseCmd { get; set; }//擦除命令
        public RelayCommand LoadBurnFileCmd { get; set; }//加载烧录文件命令
        public RelayCommand UpdateCmd { get; set; }//更新命令
        #endregion

        #region 属性
        public delegate bool EnumChildProc(IntPtr hwnd, int lParam);//枚举方法委托
        List<IntPtr> controlHandles = new List<IntPtr>();
        /// <summary>
        /// exe窗口句柄
        /// </summary>
        private IntPtr MainHandle=IntPtr.Zero;

        /// <summary>
        /// exe窗口句柄字符串
        /// </summary>
        private string _mainHandleStr;
        /// <summary>
        /// exe窗口句柄字符串
        /// </summary>
        public string MainHandleStr
        {
            get { return _mainHandleStr; }
            set { 
                _mainHandleStr = value;
                RaisePropertyChanged();
            }
        }

        private uint hId = 0;//exe进程ID

        /// <summary>
        /// 控件句柄列表
        /// </summary>
        private List<IntPtr> ControlHandles;

        /// <summary>
        /// 控件句柄字符串列表
        /// </summary>
        private string[] _controlStrs;
        /// <summary>
        /// 控件句柄字符串列表
        /// </summary>
        public string[] ControlStrs
        {
            get { return _controlStrs; }
            set { 
                _controlStrs = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// exe路径
        /// </summary>
        private string _exePath;
        /// <summary>
        /// exe路径
        /// </summary>
        public string ExePath
        {
            get { return _exePath; }
            set
            {
                _exePath = value;
                RaisePropertyChanged();
            }
        }

        public string[] BusType = new string[7];

        #endregion

        #region 消息常量
        const int WM_SETTEXT = 0x000C;//设置文本消息
        private const int BM_CLICK = 0x00F5;//模拟按钮点击的消息
        const int WM_ENABLE = 0x000A;
        #endregion

        #region 方法
        /// <summary>
        /// 枚举子窗口回调函数
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            controlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        private void ClickButton(string btnName)
        {
            try
            {
                // 查找目标窗口
                AutomationElement targetWindow = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Realtek TypeC Dump 1.203"));
                if (targetWindow != null)
                {
                    // 查找按钮
                    System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.NameProperty, btnName);
                    AutomationElement button = targetWindow.FindFirst(TreeScope.Children, condition);
                    if (button != null)
                    {
                        InvokePattern invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        if (invokePattern != null)
                        {
                            invokePattern.Invoke();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过ID和caption获取控件句柄
        /// </summary>
        /// <param name="controlID"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private IntPtr GetHandle(int controlID, string caption)
        {
            StringBuilder captionName = new StringBuilder(256);
            const int maxLength = 256;
            int ID = 0;
            foreach (var handle in ControlHandles)
            {
                ID = GetDlgCtrlID(handle); // 获取控件ID
                if (ID == controlID)
                {
                    int lenghth = GetWindowText(handle, captionName, maxLength); // 获取控件标题
                    if (captionName.ToString() == caption)
                    {
                        return handle;
                        break;
                    }
                }
            }
            return IntPtr.Zero;
        }


        /// <summary>
        /// 打开exe
        /// </summary>
        public void OpenExe()
        {
            StringBuilder className = new StringBuilder(256);// 创建StringBuilder对象用于存储类名
            StringBuilder caption = new StringBuilder(256); // 创建StringBuilder对象用于存储标题
            IntPtr handle = IntPtr.Zero;
            int controlID = 0;

            Process process = new Process();
            process.StartInfo.FileName = _exePath;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(_exePath);
            process.StartInfo.UseShellExecute = true;
            process.Start();

            Thread.Sleep(3000);

            process.WaitForInputIdle(); // 等待进程进入空闲状态
            IntPtr hWnd = process.MainWindowHandle; // 获取主窗口句柄
            if (hWnd == IntPtr.Zero)
            {
                Thread.Sleep(1000); // 等待1秒
                hWnd = process.MainWindowHandle; // 再次获取主窗口句柄
            }
            MainHandle = hWnd; // 保存主窗口句柄
            MainHandleStr = hWnd.ToString("X"); // 将句柄转换为十六进制字符串

            EnumChildWindows(hWnd, EnumChildCallback, 0); // 枚举子窗口
            ControlHandles = controlHandles; // 保存控件句柄列表
            string[] controlHandleStrs = new string[ControlHandles.Count]; // 控件句柄字符串列表
            for (int i = 0; i < ControlHandles.Count; i++)
            {
                handle = controlHandles[i]; // 获取句柄
                
                GetClassName(handle, className, className.Capacity); // 获取类名
                controlID = GetDlgCtrlID(handle); // 获取控件ID
                GetWindowText(handle, caption, caption.Capacity); // 获取窗口标题
                // 将类名和标题转换为字符串
                string str = string.Format("handle:{0,8}  ClassType:{1,-20}  Caption:{2,-30}  CtrlID:{3,4}",
                             ControlHandles[i].ToString("X8"),    // 强制8位十六进制[2,4](@ref)
                             className.ToString(),  // 限制最大20字符[6](@ref)
                             caption.ToString(),    // 限制最大30字符
                             controlID.ToString().PadLeft(4, '0'));     // 4位数字补零[4](@ref)
                controlHandleStrs[i] = str; // 将句柄转换为字符串
            }
            ControlStrs = controlHandleStrs; // 保存控件句柄字符串列表
            GetWindowThreadProcessId(hWnd, out hId);
        }

        /// <summary>
        /// 关闭exe
        /// </summary>
        public void CloseExe()
		{
			Process processes= Process.GetProcessById((int)hId);
			processes.Kill();
        }

        /// <summary>
        /// 加载exe路径
        /// </summary>
        public void Browse()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "exe文件 (*.exe)|*.exe|所有文件(*.*)|*.*";

            bool? res = openFileDialog.ShowDialog();
            if (res == true)
            {
                string filePath = openFileDialog.FileName;
                ExePath = filePath;
            }
        }

        /// <summary>
        /// 读取版本
        /// </summary>
        public void ReadVersion()
        {
            PostMessage(MainHandle,BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// 擦除
        /// </summary>
        public void Erase()
        {
            //ClickButton("Erase");
            IntPtr handle = GetHandle(1021, "Erase");
            if (handle != IntPtr.Zero)
            {
                SendMessage(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            }
        }


        #endregion
    }
}
