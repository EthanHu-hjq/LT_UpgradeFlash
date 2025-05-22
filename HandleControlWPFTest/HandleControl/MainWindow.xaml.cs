using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32;
using System.Windows.Automation;
using WindowsInput;
using WindowsInput.Native;

namespace HandleControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 引入user32.dll中的EnumChildWindows函数 已拷贝
        [DllImport("user32.dll")]
        static extern int EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        //窗口控件类名获取
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SendMessage(IntPtr hWnd, int msg, int index, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd,StringBuilder caption, int count);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);//获取窗口标题长度
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetDlgCtrlID(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SetWindowText(IntPtr hWnd, string lpString);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll",CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern int IsWindowEnabled(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, string lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);


        // 定义消息常量1
        const int WM_SETTEXT = 0x000C;

        #endregion

        public delegate bool EnumChildProc(IntPtr hwnd, int lParam);//1

        //声明一个返回值为Dictionary<IntPtr, string>的方法GetControlContents，有一个IntPtr类型的参数hwnd
        public Dictionary<IntPtr, string> GetControlContents(IntPtr hwnd)
        {
            var controlContents = new Dictionary<IntPtr, string>();
            //枚举所有子控件
            return controlContents;
        }

        //存储所有控件句柄的集合1
        List<IntPtr> controlHandles = new List<IntPtr>();
        List<IntPtr> DialogControlHandles = new List<IntPtr>();//文件对话框控件handle集合

        //枚举回调函数1
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            controlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        #region 设置Pro按键状态的方法1
        //设置Pro按键状态
        const int BM_SETCHECK = 0x00F1; // 设置选中状态
        const int BM_CHECKED = 0x0001; // 选中
        const int BM_UNCHECKED = 0x0000; // 未选中
        const int BM_CLICK = 0x00F5;//模拟按钮点击的消息

        //通过句柄想按钮发送BM_SETCHECK消息来设置按钮的选中状态1
        public void SetButtonCheck(IntPtr hwnd, bool isChecked)
        {
            int checkState = isChecked ? BM_CHECKED : BM_UNCHECKED;
            SendMessage(hwnd, BM_SETCHECK, (IntPtr)checkState, IntPtr.Zero);
        }
        //通过句柄想按钮发送BM_CLICK消息来模拟按钮点击1
        public void ClickButton(IntPtr hwnd)
        {
            //SendMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero); // 阻塞模拟按钮点击
            bool isOk = PostMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//不阻塞模拟按钮点击
        }
        #endregion

        #region 初始化Chip控件的方法11
        //向Chip控件添加内容消息
        const int CB_ADDSTRING = 0x0143; // 添加字符串消息1
        const int CB_SELECTSTRING = 0x014D; // 选择字符串消息1

        //加载XML并初始化ComboBox控件Chip
        public void InitializeComboBox(IntPtr comboBoxHandle,string xmlFilePath,string defaultSelect)
        {
            //加载XML文件
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            //遍历所有ChipType节点
            foreach(var chipType in xmlDoc.Descendants("ChipType"))
            {
                string chipName = chipType.Attribute("ChipName")?.Value;
                if(!string.IsNullOrEmpty(chipName))
                {
                    //向ComboBox控件添加字符串
                    SendMessage(comboBoxHandle, CB_ADDSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(chipName));
                }
            }
            //默认选中指定的内容
            if(!string.IsNullOrEmpty(defaultSelect))
            {
                //选择字符串
                SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(defaultSelect));
            }
        }
        #endregion

        #region 子方法
        /// <summary>
        /// 点击对话框确定按钮
        /// </summary>
        private bool ClickDialogButton(string caption)
        {
            #region 获取Error对话框句柄
            IntPtr dialogHandle = new IntPtr();
            dialogHandle = IntPtr.Zero;
            for(int count=0;count<2000;count+=100)
            {
                // 标准文件对话框类名为#32770
                dialogHandle = FindWindow("#32770", null);
                if (dialogHandle != IntPtr.Zero)
                {
                    #region 获取Error对话框handle信息
                    EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0); // 枚举子窗口
                    for (int i = 0; i < DialogControlHandle.Count; i++)
                    {
                        IntPtr subHandle = DialogControlHandle[i]; // 获取句柄
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(subHandle, className, className.Capacity); // 获取类名
                        string controlType = className.ToString(); // 获取控件类型
                        StringBuilder captionNmae = new StringBuilder();
                        int length = SendMessage(subHandle, WM_GETTEXTLENGTH, (IntPtr)0, (IntPtr)0);
                        SendMessage(subHandle, WM_GETTEXT, captionNmae.Capacity, captionNmae);
                        //GetWindowText(subHandle, captionNmae, 0);

                        //判断是否为文件对话框的文件名输入框
                        if ((controlType == "Button") && (captionNmae.ToString() == caption))
                        {
                            ClickButton(subHandle);
                            break;
                        }
                    }
                    #endregion
                    return true;//如果获取到对话框句柄返回true，否则返回false
                    
                }
                Thread.Sleep(100);
            }

            return false;

            #endregion

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
        /// 关闭对话框
        /// </summary>
        /// <param name="dialogTitle">对话框标题</param>
        /// <param name="captionName"></param>
        private bool CloseDialog(string dialogTitle, string captionName)
        {
            //获取对话框句柄
            IntPtr dialogHandle = new IntPtr();
            dialogHandle = IntPtr.Zero;
            string title = string.Empty;
            StringBuilder sb = new StringBuilder(256);
            int lenth = 0;

            for (int count=0;count<2000;count+=100)
            {
                dialogHandle = FindWindow("#32770", dialogTitle);
                title = GetWindowTitle(dialogHandle);
                if ((dialogHandle != IntPtr.Zero) && (title == dialogTitle))
                {
                    SetForegroundWindow(dialogHandle);
                    //枚举对话框获取对话框所有控件句柄
                    EnumChildWindows(dialogHandle, EnumDialogChildCallback, (IntPtr)0);
                    StringBuilder className = new StringBuilder(256);

                    //遍历控件句柄，查找并点击指定按钮
                    foreach (var handle in DialogControlHandle)
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

        #endregion
        IntPtr mainHnadle = IntPtr.Zero; // 主窗口句柄11
        /// <summary>
        /// 打开exe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenExe_Click(object sender, RoutedEventArgs e)
        {
            #region 打开exe
            Process process = new Process();
            process.StartInfo.FileName = exePath.Text;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath.Text);//设置工作目录
            process.StartInfo.UseShellExecute = true;
            process.Start();
            #endregion
            Thread.Sleep(2000);
            CloseDialog("Error", "确定"); // 关闭错误对话框

            Thread.Sleep(3000);
            #region 获取窗口句柄
            process.WaitForInputIdle(); // 等待进程进入空闲状态
            IntPtr hWnd = process.MainWindowHandle; // 获取主窗口句柄
            if (hWnd == IntPtr.Zero)
            {
                Thread.Sleep(1000); // 等待1秒
                hWnd = process.MainWindowHandle; // 再次获取主窗口句柄
            }
            //将句柄显示到UI的TextBlock上
            if (hWnd != IntPtr.Zero)
            {
                string handle = hWnd.ToString("X");
                this.showhandle.Text = "Handle: " + handle;
            }
            else
            {
                this.showhandle.Text = "Handle: Not Found";
            }
            mainHnadle = hWnd; // 保存主窗口句柄
            #endregion

            #region 获取handle信息
            EnumChildWindows(hWnd, EnumChildCallback, 0); // 枚举子窗口
            for (int i = 0; i < controlHandles.Count; i++)
            {
                IntPtr handle = controlHandles[i]; // 获取句柄
                StringBuilder className = new StringBuilder(256);
                StringBuilder text = new StringBuilder(256); // 创建StringBuilder对象用于存储类名和标题

                GetClassName(handle, className, className.Capacity); // 获取类名
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1006)
                {
                    SendMessage(handle, WM_SETTEXT, 0, @"0001"); // 设置控件标题
                }

                string handleStr = controlHandles[i].ToString("X");
                string controlType = className.ToString(); // 获取控件类型

                #region 获取控件caption
                const int maxLength = 256;
                StringBuilder caption = new StringBuilder(maxLength);
                int lenghth = GetWindowText(handle, caption, maxLength); // 获取控件标题
                #endregion

                this.itemsControl.Items.Add($"Handle: {handleStr}__Type: {controlType}__caption: {caption}__ControlID:{controlID}"); // 将句柄添加到ItemsControl

            }
            #endregion
        }

        /// <summary>
        /// 关闭exe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseExe_Click(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("LT Programmer");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void HandleFileDialog(string defaultFilePath)
        {
            // 循环等待文件选择框出现
            IntPtr fileDialogHandle = IntPtr.Zero;
            Thread.Sleep(4);
            for (int i = 0; i < 10; i++) // 尝试 10 次，每次间隔 500ms
            {
                fileDialogHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "#32770", null); // "#32770" 是文件选择框的类名
                if (fileDialogHandle != IntPtr.Zero)
                {
                    break;
                }
                Thread.Sleep(500); // 等待 500ms
            }

            if (fileDialogHandle != IntPtr.Zero)
            {
                // 获取文件选择框中的文件路径输入框句柄
                //IntPtr editHandle = FindWindowEx(fileDialogHandle, IntPtr.Zero, "Edit", null);
                IntPtr editHandle = FindEditControl(fileDialogHandle);
                if (editHandle != IntPtr.Zero)
                {
                    // 设置默认路径和文件名
                    SetWindowText(editHandle, defaultFilePath);
                }

                // 获取“打开”按钮的句柄
                IntPtr openButtonHandle = FindWindowEx(fileDialogHandle, IntPtr.Zero, "Button", "打开(&O)");
                if (openButtonHandle != IntPtr.Zero)
                {
                    // 模拟点击“打开”按钮
                    ClickButton(openButtonHandle);
                }
            }
            else
            {
                MessageBox.Show("未找到文件选择框！");
            }
        }
        private void FocusOnControl(IntPtr mainWindowHandle, IntPtr controlHandle)
        {
            // 将主窗口置于前台
            if (SetForegroundWindow(mainWindowHandle))
            {
                // 设置焦点到指定控件
                SetFocus(controlHandle);
            }
            else
            {
                MessageBox.Show("无法将窗口置于前台！");
            }
        }

        /// <summary>
        /// 查找文件选择框中的编辑控件
        /// </summary>
        /// <param name="parentHandle"></param>
        /// <returns></returns>
        private IntPtr FindEditControl(IntPtr parentHandle)
        {
            IntPtr editHandle = IntPtr.Zero;
            EnumChildWindows(parentHandle, (hwnd, lParam) =>
            {
                StringBuilder className = new StringBuilder(256);
                GetClassName(hwnd, className, className.Capacity);
                if (className.ToString() == "Edit")
                {
                    editHandle = hwnd;
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            }, IntPtr.Zero);
            return editHandle;
        }
        /// <summary>
        /// 模拟点击Prog按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetProg_Click(object sender, RoutedEventArgs e)
        {
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1004)
                {
                    //SetWindowText(handle, "00001"); // 设置控件标题
                    bool isEnabled = IsWindowEnabled(handle) != 0;
                    if (!isEnabled)
                    {
                        MessageBox.Show("控件未启用！");
                        break;
                    }
                    else
                    {
                        SendMessage(handle,WM_SETTEXT,0, "");
                        StringBuilder sb = null;
                        // 获取文本框内容的长度
                        int textLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, sb);
                        if (textLength > 0)
                        {
                            // 创建一个StringBuilder对象来存储文本内容
                            StringBuilder textContent = new StringBuilder(textLength + 1);
                            // 获取文本框内容
                            SendMessage(handle, WM_GETTEXT, textLength + 1, textContent);
                            MessageBox.Show(textContent.ToString());
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 读取Log控件的内容
        /// </summary>
        private string ReadLogContent(IntPtr logHandle)
        {
            StringBuilder sb = null;
            // 获取文本框内容的长度
            int textLength = SendMessage(logHandle, WM_GETTEXTLENGTH, 0, sb);
            if (textLength > 0)
            {
                // 创建一个StringBuilder对象来存储文本内容
                StringBuilder textContent = new StringBuilder(textLength + 1);
                // 获取文本框内容
                SendMessage(logHandle, WM_GETTEXT, textLength + 1, textContent);
                // 将文本内容显示在UI的TextBlock上
                return textContent.ToString();
            }
            else
            {
                return null;
            }
        }

        const int WM_GETTEXT = 0x000D; // 获取文本的消息
        const int WM_GETTEXTLENGTH = 0x000E; // 获取文本长度的消息


        /// <summary>
        /// 读取Log事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetLog_Click(object sender, RoutedEventArgs e)
        {
            int flag = 0;
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1010)
                {
                    flag += 1;
                    if(flag == 4)
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
                            this.Log.Text = textContent.ToString();
                            break;
                        }
                        else
                        {
                            this.Log.Text = "TextBox is empty.";
                        }
                    }
                    
                }
            }
        }

        //修改Chip控件的选中项
        private void SelectChip(IntPtr comboBoxHandle, string chipName)
        {
            //选择字符串
            SendMessage(comboBoxHandle, CB_SELECTSTRING, 0, Marshal.StringToHGlobalAuto(chipName));
        }

        #region 选择芯片型号
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool UpdateWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
    IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
    uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        const int WM_COMMAND = 0x0111;
        const int CBN_SELCHANGE = 1;
        const int CBN_CLOSEUP = 8;
        const int CB_SHOWDROPDOWN = 0x014F;
        const int CB_SETCURSEL = 0x014E;
        const int CB_GETDROPPEDSTATE = 0x0157;

        //const int CB_SHOWDROPDOWN = 0x014F;
        //const int CB_SETCURSEL = 0x014E;
        //const int WM_COMMAND = 0x0111;
        //const int CBN_SELCHANGE = 1;

        private void SelectChip_Click(object sender, RoutedEventArgs e)
        {
            int selectItem = int.Parse(this.chip.Text) - 1;
            
            #region 实现方法1
            IntPtr comboBoxHandle = IntPtr.Zero;
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1073)
                {
                    StringBuilder className = new StringBuilder(256);

                    GetClassName(handle, className, className.Capacity); // 获取类名

                    if (className.ToString() == "ComboBox")
                    {
                        comboBoxHandle = handle; // 获取ComboBox控件的句柄
                                                 // 设置选中项（索引从0开始）
                        PostMessage(comboBoxHandle, CB_SETCURSEL, (IntPtr)selectItem, IntPtr.Zero);
                        Thread.Sleep(1000);
                        CloseDialog("Error", "确定");
                        // 触发值改变事件
                        int ctrlID = GetDlgCtrlID(comboBoxHandle);  // 需补充此API声明 
                        IntPtr wParam = (IntPtr)((CBN_SELCHANGE << 16) | (ushort)ctrlID);
                        IntPtr parentHandle = GetParent(comboBoxHandle);
                        PostMessage(parentHandle, WM_COMMAND, wParam, comboBoxHandle);
                        CloseDialog("Error", "确定");
                        break;
                    }
                }
            }
            #endregion
        }
        #endregion

        private bool ReadBtnResult([CallerMemberName]string caller="",string compareStr="")
        {
            string pattern = null;
            switch(caller)
            {
                case "Button_Click_5":
                    pattern = @".*Prog Flash Data.*Succeed.*";
                    break;
                case "Button_Click_6":
                    pattern = @".*Load Prog File.*Done.*";
                    break;
            }
            return Regex.IsMatch(compareStr, pattern);
        }

        bool isTimeOut = false;
        private bool ActionIsDone(string pattern, int timeOut)
        {
            //判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到timeOut则表示完成
            int count = 0;
            while (true)
            {
                string log = ReadLog();
                if (Regex.IsMatch(log,pattern) || count >= timeOut * 1000)
                {
                    return Regex.IsMatch(log, pattern);
                }
                Thread.Sleep(100);
                count += 100;
            }
            return false;
        }
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
                        this.Log.Text = textContent.ToString();
                        return textContent.ToString();
                    }
                    else
                    {
                        this.Log.Text = "TextBox is empty.";
                    }
                }
            }
            return null;
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

        private void SetProgFIleEmpty()
        {
            int flag = 0;
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1004)
                {
                    flag += 1;
                    if(flag == 4)
                    {
                        //SetWindowText(handle, "00001"); // 设置控件标题
                        bool isEnabled = IsWindowEnabled(handle) != 0;
                        if (!isEnabled)
                        {
                            MessageBox.Show("控件未启用！");
                            break;
                        }
                        else
                        {
                            SendMessage(handle, WM_SETTEXT, 0, "");
                            break;
                        }
                    }
                }
            }
        }

        #region 点击烧录按钮
        private const int MaxWaitSeconds = 5;
        List<IntPtr> DialogControlHandle = new List<IntPtr>();

        //枚举文件选择框回调函数
        private bool EnumDialogChildCallback(IntPtr hwnd, int lParam)
        {
            DialogControlHandle.Add(hwnd);
            return true; // 继续枚举
        }

        /// <summary>
        /// 点击烧录按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgBtn_Click(object sender, RoutedEventArgs e)
        {
            SetProgFIleEmpty();//清空烧录文件
            ClearLog();   
            IntPtr dialogHandle = IntPtr.Zero;//文件对话框Handle
            IntPtr fileHandle = IntPtr.Zero;//文件对话框中文本选择框Handle
            //从controlHandles中获取Prog控件的句柄
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1001)
                {
                    #region 获取控件caption
                    const int maxLength = 256;
                    StringBuilder caption = new StringBuilder(maxLength);
                    int lenghth = GetWindowText(handle, caption, maxLength); // 获取控件标题
                    #endregion
                    if(caption.ToString()=="Prog")
                    {
                        ClickButton(handle); // 模拟点击Prog按钮
                        DateTime start = DateTime.Now;
                        while ((DateTime.Now - start).TotalSeconds < 1)
                        {
                            // 标准文件对话框类名为#32770
                            dialogHandle = FindWindow("#32770", null);
                            if (dialogHandle != IntPtr.Zero)
                            {
                                StringBuilder title = new StringBuilder(256);
                                GetWindowText(dialogHandle, title, 256);
                            }
                            Thread.Sleep(100);
                        }

                        #region 获取文件选择框handle信息
                        EnumChildWindows(dialogHandle, EnumDialogChildCallback, 0); // 枚举子窗口
                        for (int i = 0; i < DialogControlHandle.Count; i++)
                        {
                            IntPtr subHandle = DialogControlHandle[i]; // 获取句柄
                            StringBuilder className = new StringBuilder(256);
                            StringBuilder text = new StringBuilder(256); // 创建StringBuilder对象用于存储类名和标题
                            GetClassName(subHandle, className, className.Capacity); // 获取类名
                            string controlType = className.ToString(); // 获取控件类型

                            string handleStr = DialogControlHandle[i].ToString("X");
                            //判断是否为文件对话框的文件名输入框
                            if (controlType == "Edit")
                            {
                                
                                if(this.chip.Text=="17")
                                    SendMessage(subHandle, WM_SETTEXT, 0, @"D:\Project\Tymphany\LT_UpgradeFlash\LT Programmer\LT FW  Files\LT6911 UXE\lt6911_fw.bin"); // 设置控件标题
                                else if(this.chip.Text=="9")
                                    SendMessage(subHandle, WM_SETTEXT, 0, @"D:\Project\Tymphany\LT_UpgradeFlash\LT Programmer\LT FW  Files\LT9611 UXC\lt9611uxc_fw.bin"); // 设置控件标题

                                //break;
                            }
                        }
                        #endregion

                        // 多语言适配按钮标题
                        IntPtr openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "打开(&O)");
                        if (openButton == IntPtr.Zero)
                            openButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "Open(&O)");
                        SendMessage(openButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero); // 触发点击[1,6](@ref)
                        CloseDialog("Error", "确定"); // 关闭错误对话框
                        CloseDialog("Waring", "确定");
                        string pattern = @".*Prog Flash Data.*Succeed.*";
                        bool isOk = ActionIsDone(pattern, 10); // 判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到4秒则表示完成
                        break;
                    }
                    
                }
            }
        }

        #endregion

        private void ClearLog()
        {
            int flag = 0;
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1014)
                {
                    flag += 1;
                    if (flag == 5)
                    {
                        ClickButton(handle); // 模拟点击Clear按钮
                        break;
                    }
                }
            }
        }
        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }

        /// <summary>
        /// 选择exe路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowseExePath_Click(object sender, RoutedEventArgs e)
        {
            //创建对话框实例
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //设置文件类型过滤器
            openFileDialog.Filter = "exe文件 (*.exe)|*.exe|所有文件(*.*)|*.*";
            //设置默认文件路径
            openFileDialog.InitialDirectory = @"D:\SoftWare\Microsoft Visual Studio\Project\Tymphany\LT_UpgradeFlash\HDMI Board\LT Upgrade Flash";
            //设置默认文件名
            openFileDialog.FileName = "Upgrade_Flash_For_Application.exe";

            //显示对话框
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                //获取选择的文件路径
                string filePath = openFileDialog.FileName;
                //设置exe路径文本框的值
                this.exePath.Text = filePath;
            }
        }

        private void Erase_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
            int controlID = 0;

            IntPtr dialogHandle = new IntPtr();

            foreach(var handle in controlHandles)
            {
                controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID==1021)
                {
                    #region 获取控件caption
                    const int maxLength = 256;
                    StringBuilder caption = new StringBuilder(maxLength);
                    int lenghth = GetWindowText(handle, caption, maxLength); // 获取控件标题
                    #endregion
                    if(caption.ToString()=="Erase")
                    {
                        ClickButton(handle);
                        break;
                    }
                }
            }

            //ClickDialogButton("确定");

            CloseDialog("Info", "确定");
            CloseDialog("Error", "确定");


            foreach (var handle in controlHandles)
            {
                controlID = GetDlgCtrlID(handle); // 获取控件ID
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
                        //MessageBox.Show(textContent.ToString());
                        this.Log.Text = textContent.ToString();
                        break;
                    }
                    else
                    {
                        this.Log.Text = "TextBox is empty.";
                    }
                }
            }
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            foreach(var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1002)
                {
                    ClickButton(handle);
                    break;
                }
            }

            ClickDialogButton("确定");
        }
    }
}