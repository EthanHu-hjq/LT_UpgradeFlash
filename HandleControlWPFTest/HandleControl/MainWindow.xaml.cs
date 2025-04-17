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
        static extern int GetWindowText(IntPtr hWnd,StringBuilder caption, int count);
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

        //枚举回调函数1
        private bool EnumChildCallback(IntPtr hwnd, int lParam)
        {
            controlHandles.Add(hwnd);
            return true; // 继续枚举
        }

        //定义exe路径1
        static string rootPath = @"D:\SoftWare\Microsoft Visual Studio\Project\Tymphany\LT_UpgradeFlash\HDMI Board\LT Upgrade Flash\Upgrade_Flash_For_Application.exe";
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
            SendMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero); // 阻塞模拟按钮点击
            //PostMessage(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);//不阻塞模拟按钮点击
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

        IntPtr mainHnadle = IntPtr.Zero; // 主窗口句柄11
        /// <summary>
        /// 打开exe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            #region 打开exe
            Process process = new Process();
            process.StartInfo.FileName = rootPath;
            process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(rootPath);//设置工作目录
            process.StartInfo.UseShellExecute = true;
            process.Start();
            #endregion

            Thread.Sleep(3000);
            #region 获取窗口句柄
            process.WaitForInputIdle(); // 等待进程进入空闲状态
            IntPtr hWnd = process.MainWindowHandle; // 获取主窗口句柄
            if(hWnd == IntPtr.Zero)
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
                    SendMessage(handle,WM_SETTEXT,0, @"0001"); // 设置控件标题
                }

                string handleStr = controlHandles[i].ToString("X");
                string controlType = className.ToString(); // 获取控件类型

                #region 获取控件caption
                const int maxLength = 256;
                StringBuilder caption = new StringBuilder(maxLength);
                int lenghth = GetWindowText(handle, caption, maxLength); // 获取控件标题
                #endregion

                this.itemsControl.Items.Add($"Handle: {handleStr}__Type: {controlType}__caption: {caption}__ControlID:{controlID}"); // 将句柄添加到ItemsControl
                
                ////初始化ComboBox控件Chip
                //if(controlType == "ComboBox")
                //{
                //    //设置Chip控件的XML文件路径
                //    string xmlFilePath = @"D:\Project\Tymphany\HDMI Board_2025-04-15\HDMI Board\LT Upgrade Flash\UpgradeFlash.xml";
                //    string defaultSelection = "LT6911(UX/UXB/UXC)";
                //    InitializeComboBox(handle, xmlFilePath,defaultSelection); // 初始化ComboBox控件
                //}
            }
            #endregion

            //初始化Chip
            IntPtr comboBoxHandle = FindWindowEx(mainHnadle, IntPtr.Zero, "ComboBox", null);
            //设置Chip控件的XML文件路径
            string xmlFilePath = @"D:\Project\Tymphany\HDMI Board_2025-04-15\HDMI Board\LT Upgrade Flash\UpgradeFlash.xml";
            string defaultSelection = "LT6911(UX/UXB/UXC)";
            //InitializeComboBox(comboBoxHandle, xmlFilePath, defaultSelection); // 初始化ComboBox控件

        }

        /// <summary>
        /// 关闭exe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("Upgrade_Flash_For_Application");
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
        private void Button_Click_2(object sender, RoutedEventArgs e)
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
                        SendMessage(handle,WM_SETTEXT,0, @"D:\Project\Tymphany\HDMI Board_2025-04-15\HDMI Board\LT Upgrade Flash\text.hex");
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


        private void Button_Click_3(object sender, RoutedEventArgs e)
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
                        break;
                    }
                    else
                    {
                        this.Log.Text = "TextBox is empty.";
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

        //选择芯片型号
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            IntPtr comboBoxHandle = FindWindowEx(mainHnadle, IntPtr.Zero, "ComboBox", null);
            SendMessage(comboBoxHandle, CB_SELECTSTRING, IntPtr.Zero, Marshal.StringToHGlobalAuto(this.chip.Text));
            //foreach (var handle in controlHandles)
            //{
            //    StringBuilder className = new StringBuilder(256);
            //    GetClassName(handle, className, className.Capacity); // 获取类名
            //    string controlType = className.ToString(); // 获取控件类型
            //    if (controlType == "ComboBox")
            //    {
            //        SelectChip(handle, "LT9611(UX/UXC)"); // 选择芯片型号
            //        break;
            //    }
            //}
        }

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
        private bool ActionIsDone(int timeOut)
        {
            //判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到timeOut则表示完成
            int count = 0;
            while (true)
            {
                string log = ReadLog();
                if (log.Length > 3 || count >= timeOut * 1000)
                {
                    isTimeOut = true;
                    return true;
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
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            ClearLog();   
            //从controlHandles中获取Prog控件的句柄
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1001)
                {
                    ClickButton(handle); // 模拟点击Prog按钮
                    ActionIsDone(4); // 判断动作是否完成，如果反复调用ReadLog()返回的字符串的长度大于2或时间达到4秒则表示完成
                    break;
                }
            }
            string pattern = @".*Prog Flash Data.*Succeed.*";
            string multiLineString = @"Some other lines
More lines here
Prog File Data and some text
Some more text Succeed!";

            //从controlHandles中获取Log控件的句柄
            //foreach (var handle in controlHandles)
            //{
            //    int controlID = GetDlgCtrlID(handle); // 获取控件ID
            //    if (controlID == 1010)
            //    {
            //        string logContent = ReadLogContent(handle); // 读取Log控件的内容
            //        MessageBox.Show(logContent);

            //        string[] lines = multiLineString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //        MessageBox.Show(lines[lines.Length - 1]); // 显示最后一行内容

            //        if (Regex.IsMatch(lines[lines.Length-1], pattern))
            //        {
            //            // 匹配成功，执行相应操作
            //            MessageBox.Show("匹配成功！");
            //        }
            //        else
            //        {
            //            // 匹配失败，执行其他操作
            //            MessageBox.Show("匹配失败！");
            //        }
            //        this.Log.Text = logContent; // 显示在UI的TextBlock上
            //        break;
            //    }

            //}
        }

        private void ClearLog()
        {
            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1014)
                {
                    ClickButton(handle); // 模拟点击Clear按钮
                    break;
                }
            }
        }
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {

            foreach (var handle in controlHandles)
            {
                int controlID = GetDlgCtrlID(handle); // 获取控件ID
                if (controlID == 1014)
                {
                    ClickButton(handle); // 模拟点击Clear按钮
                    break;
                }
            }
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
                        MessageBox.Show(textContent.ToString());
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
}