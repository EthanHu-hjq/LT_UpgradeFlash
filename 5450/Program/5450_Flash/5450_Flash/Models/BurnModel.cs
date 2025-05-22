using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using _5450_Flash.Base;
using Microsoft.Win32;

namespace _5450_Flash.Models
{
    public class BurnModel:NotifyBase
    {
        #region win32 API
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
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
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool OpenIcon(IntPtr hWnd);

        #endregion

        #region 界面按钮响应命令
        public RelayCommand? OpenCmd { get; set; }//打开exe命令
        public RelayCommand? CloseCmd { get;set; }//关闭exe命令
        public RelayCommand? BrowseCmd { get; set; }//浏览exe命令
        public RelayCommand? ReadVerCmd { get; set; }//读取版本命令
        public RelayCommand? EraseCmd { get; set; }//擦除命令
        public RelayCommand? LoadBurnFileCmd { get; set; }//加载烧录文件命令
        public RelayCommand? UpdateCmd { get; set; }//更新命令
        public RelayCommand? SetBusTypeCmd { get; set; }
        public RelayCommand? SetDev1 { get; set; }

        public RelayCommand? ShowExeCmd { get; set; }//显示exe命令
        #endregion

        #region 属性
        public delegate bool EnumChildProc(IntPtr hwnd, int lParam);//枚举方法委托
        List<IntPtr> controlHandles = new List<IntPtr>();//主窗口控件句柄列表
        List<IntPtr> DialogControlHandles = new List<IntPtr>();//对话框窗口控件句柄列表

        /// <summary>
        /// exe窗口句柄
        /// </summary>
        private IntPtr MainHandle=IntPtr.Zero;

        /// <summary>
        /// exe窗口句柄字符串
        /// </summary>
        private string? _mainHandleStr;
        /// <summary>
        /// exe窗口句柄字符串
        /// </summary>
        public string MainHandleStr
        {
            get { return _mainHandleStr ?? ""; }
            set { 
                _mainHandleStr = value;
                RaisePropertyChanged();
            }
        }

        private uint hId = 0;//exe进程ID

        /// <summary>
        /// 控件句柄列表
        /// </summary>
        private List<IntPtr>? ControlHandles;

        /// <summary>
        /// 控件句柄字符串列表
        /// </summary>
        private string[]? _controlStrs;
        /// <summary>
        /// 控件句柄字符串列表
        /// </summary>
        public string[] ControlStrs
        {
            get { return _controlStrs ?? new string[] { }; }
            set { 
                _controlStrs = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// exe路径
        /// </summary>
        private string? _exePath;
        /// <summary>
        /// exe路径
        /// </summary>
        public string ExePath
        {
            get { return _exePath ?? ""; }
            set
            {
                _exePath = value;
                RaisePropertyChanged();
            }
        }

        public ItemModel itemModel = new ItemModel();

        private ItemModel? _selectedItem;

        public ItemModel SelectedItem
        {
            get { return _selectedItem ?? new ItemModel(); }
            set { 
                _selectedItem = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ItemModel>? _items;

        public ObservableCollection<ItemModel> Items
        {
            get { return _items ?? new ObservableCollection<ItemModel>(); }
            set {  
                _items = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 消息常量
        const int WM_SETTEXT = 0x000C;//设置文本消息
        private const int BM_CLICK = 0x00F5;//模拟按钮点击的消息
        const int WM_ENABLE = 0x000A;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;
        const int CB_SETCURSEL = 0x014E;//设置下拉列表框当前选中项的消息
        const int CBN_SELCHANGE = 1;//下拉列表框选中项改变的消息
        const int CB_SELECTSTRING = 0x014D; // ComboBox选择字符串消息
        const int WM_COMMAND = 0x0111;//发送命令消息
        #endregion

        #region 方法

        /// <summary>
        /// 枚举窗口所有控件句柄回调函数
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            controlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        /// <summary>
        /// 枚举对话框窗口所有控件句柄回调函数
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumDialogCallback(IntPtr hwnd, int lParam)
        {
            DialogControlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        /// <summary>
        /// 设置烧录文件
        /// </summary>
        /// <param name="path">烧录文件路径</param>
        private void CloseFileDialog(string FilePath)
        {
            IntPtr fileDialogHandle = IntPtr.Zero;

            //查找文件对话框句柄
            for(int count = 0;count < 2000; count+=100)
            {
                fileDialogHandle = FindWindow("#32770", "打开");
                if (fileDialogHandle != IntPtr.Zero) break;
            }

            //获取文件名输入框句柄
            EnumChildWindows(fileDialogHandle, EnumDialogCallback, (IntPtr)0);
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
                    SendMessage(subHandle, WM_SETTEXT, 0, FilePath); // 设置文件名
                    break;
                }
            }

            // 多语言适配按钮标题
            IntPtr openButton = FindWindowEx(fileDialogHandle, IntPtr.Zero, "Button", "打开(&O)");
            if (openButton == IntPtr.Zero)
                openButton = FindWindowEx(fileDialogHandle, IntPtr.Zero, "Button", "Open(&O)");
            SendMessage(openButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//模拟点击打开按钮
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        private bool CloseDialog(string caption,string btnContent)
        {
            IntPtr dialogHandle = IntPtr.Zero;
            //查找对话框句柄
            for(int count=0;count<3000;count+=100)
            {
                dialogHandle = FindWindow("#32770", caption);
                Thread.Sleep(100);
                if (dialogHandle != IntPtr.Zero)
                {
                    IntPtr btnHandle = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", btnContent);
                    SendMessage(btnHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//模拟点击确定按钮
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 从对话框获取动作结果字符串并关闭弹框
        /// </summary>
        /// <param name="btnContent">弹框关闭按钮标题</param>
        /// <returns></returns>
        private string GetActionResult(string btnContent)
        {
            IntPtr dialogHandle = IntPtr.Zero;
            const int maxLength = 256;
            StringBuilder caption = new StringBuilder(maxLength);
            string? fw = null;

            //查找对话框句柄
            for (int count = 0; count < 3000; count += 100)
            {
                dialogHandle = FindWindow("#32770", fw ?? "");
                Thread.Sleep(100);
                if (dialogHandle != IntPtr.Zero)
                {
                    break;
                }
            }
            Int32 id = 0;
            EnumChildWindows(dialogHandle, EnumDialogCallback, (IntPtr)0);
            for (int i = 0; i < DialogControlHandles.Count; i++)
            {
                IntPtr hWnd = DialogControlHandles[i];
                id = GetDlgCtrlID(hWnd);
                if (id == 65535)
                {
                    GetWindowText(hWnd, caption, maxLength);
                    break;
                } 
            }


            IntPtr btnHandle = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", btnContent);
            SendMessage(btnHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//模拟点击确定按钮

            return caption.ToString() ;
        }
        
        /// <summary>
        /// 模拟点击按钮
        /// </summary>
        /// <param name="caption"></param>
        private void ClickBtn(string caption)
        {
            IntPtr hWnd = GetHandle(caption);
            if (hWnd != IntPtr.Zero)
            {
                PostMessage(hWnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//不阻塞模拟点击按钮
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
            foreach (var handle in ControlHandles ?? new List<nint>())
            {
                ID = GetDlgCtrlID(handle); // 获取控件ID
                if (ID == controlID)
                {
                    int lenghth = GetWindowText(handle, captionName, maxLength); // 获取控件标题
                    if (captionName.ToString() == caption)
                    {
                        return handle;
                    }
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 通过caption获取控件句柄
        /// </summary>
        /// <param name="caption"></param>
        /// <returns></returns>
        private IntPtr GetHandle(string caption)
        {
            StringBuilder captionName = new StringBuilder(256);
            const int maxLength = 256;
            foreach (var handle in ControlHandles ?? new List<nint>())
            {
                GetWindowText(handle, captionName, maxLength); // 获取控件标题
                if (captionName.ToString() == caption)
                {
                    return handle;
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
            ControlStrs = controlHandleStrs; // 将控件句柄字符串列表赋值给属性
            GetWindowThreadProcessId(hWnd, out hId);
        }

        /// <summary>
        /// 关闭exe
        /// </summary>
        public void CloseExe()
		{
            //Process processes= Process.GetProcessById((int)hId);
            //processes.Kill();
            MessageBox.Show(MainHandle.ToString("X"));
            Process[] processes = Process.GetProcessesByName("TypeCDump");
            MessageBox.Show(processes.Length.ToString());
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        // 控制窗口显示状态
        private static void SetWindowVisibility(IntPtr hWnd, bool isVisible)
        {
            if (hWnd == IntPtr.Zero) return;

            if (isVisible)
            {
                if (IsIconic(hWnd)) OpenIcon(hWnd); // 恢复最小化
                ShowWindow(hWnd, 5); // SW_SHOW
            }
            else
            {
                ShowWindow(hWnd, 0); // SW_HIDE
            }
        }


        ///<summary>
        ///显示exe
        /// </summary>
        public void ShowExe()
        {
            if (MainHandle != IntPtr.Zero)
            {
                SetWindowVisibility(MainHandle, false);
                Thread.Sleep(2000);
                SetWindowVisibility(MainHandle, true);
            }
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
            ClickBtn("Read Version");//点击读取版本按钮
            Thread.Sleep(1500);
            string resultStr = GetActionResult("确定");
            bool failedFlag = resultStr.Contains("Please select at least one device as the target device");
            if(!failedFlag)
            {
                resultStr = resultStr.Trim('{', '}');
                string[] result = resultStr.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string pattern = @":\s*([^\s]+)";
                Match match = Regex.Match(result[0], pattern);
                Match match1 = Regex.Match(result[1], pattern);

                string FWVersion = match.Groups[1].Value;
                string HWVersion = match1.Groups[1].Value;

                bool isNA = Regex.IsMatch("N/A", pattern);
                MessageBox.Show(isNA ? "读取版本失败" : "读取版本成功");
            }
          
        }

        /// <summary>
        /// 设置烧录文件路径
        /// </summary>
        public void FW1_Path()
        {
            ClickBtn("FW1 Path ...");
            Thread.Sleep(1000);
            CloseFileDialog(@"D:\Project\Tymphany\LT_UpgradeFlash\5450\RTS5450 FW File\RTS 5450 U1800\UC_WALL_U1800UC_UFP_V4.2_20240328.bin");
        }

        public void SelectBusType()
        {
            IntPtr comboBoxHandle = IntPtr.Zero;
            int ctrlID = 0;
            foreach(var hWnd in ControlHandles ?? new List<nint>())
            {
                ctrlID = GetDlgCtrlID(hWnd);
                if (ctrlID == 1768) {
                    comboBoxHandle = hWnd;
                    break;
                } 
            }

            int selectedId = SelectedItem.Id;
            SendMessage(comboBoxHandle, CB_SETCURSEL, (IntPtr)selectedId, IntPtr.Zero);//设置ComboBox选中项
            //SendMessage(comboBoxHandle, CB_SELECTSTRING, 2, IntPtr.Zero);
            IntPtr wParam = (IntPtr)((CBN_SELCHANGE << 16) | (ushort)ctrlID);
            IntPtr parentHandle = GetParent(comboBoxHandle);
            PostMessage(parentHandle, WM_COMMAND, wParam, comboBoxHandle);

        }

        /// <summary>
        /// Read方法
        /// </summary>
        public void Read()
        {
            ClickBtn("Read");//点击Read按钮
            CloseDialog("TypeCFF", "确定");//如果有报错弹框默认点击“确定”按钮
            CloseDialog("TypeCFF", "确定");//如果有报错弹框默认点击“确定”按钮
        }

        /// <summary>
        /// 擦除
        /// </summary>
        public void Erase()
        {
            ClickBtn("Erase");//点击擦除按钮
            bool earseOk = CloseDialog("TypeCFF", "确定");//如果有报错弹框默认点击“确定”按钮
            MessageBox.Show(earseOk ? "Erase failed" : "Erase successed");
        }

        /// <summary>
        /// 烧录
        /// </summary>
        public void Update()
        {
            bool exactMatch = false;
            ClickBtn("Update");//点击Update按钮
            CloseDialog("TypeCFF", "确定");//如果有报错弹框默认点击“确定”按钮
            if(!CloseDialog("TypeCFF","确定"))
            {
                Thread.Sleep(22000);
                string result = GetActionResult("确定");
                exactMatch = Regex.IsMatch(result, @"^Update Flash Code Succeed\.$");
            }
            MessageBox.Show(exactMatch ? "Update successed" : "Update failed");
        }

        public void SetDev1Btn()
        {
            ClickBtn("Dev1");
            SetDev1Addr();
        }

        public void SetDev1Addr()
        {
            IntPtr devHandle = GetHandle(1774,"");
            SendMessage(devHandle, WM_SETTEXT, 0, "B0");
        }

        #endregion
    }
}
