using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace easylockforstudy
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainWindow : Window
    {
        private bool running = false;
        private BackgroundWorker BGWorker = new BackgroundWorker();
        private const uint WM_SYSCOMMAND = 0x0112;                   
        private const uint SC_MONITORPOWER = 0xF170;                  
        private const int MonitorPowerOff = 2;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, uint wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

        public MainWindow()
        {
            InitializeComponent();
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey run = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (run == null)
            {
                run = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            }
            run.SetValue("EasyLock", exePath);
            ReadTime();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = running;
        }

        private void LOCK(object sender, DoWorkEventArgs e)
        {
            long target = (long)e.Argument;
            while (GetTimeStampNow() < target)
            {
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MonitorPowerOff);
            }
            File.Delete(@".\\time.dat");
        }

        private long GetTimeStampNow()
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long time = (long)(DateTime.Now - startTime).TotalSeconds;
            return time;
        }

        private void ReadTime()
        {
            string fileName = @".\\time.dat";
            if (File.Exists(fileName))
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                long target = br.ReadInt64();

                fs.Close();
                br.Close();

                if (GetTimeStampNow() < target)
                {
                    Work();
                    BGWorker.DoWork += LOCK;
                    BGWorker.RunWorkerCompleted += Rest;
                    BGWorker.RunWorkerAsync(target);
                }
                return;
            }
            else return;
        }

        private void Rest(object sender, RunWorkerCompletedEventArgs e)
        {
            FakeButton.IsEnabled = !FakeButton.IsEnabled;
            running = false;
            this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
            this.WindowState = System.Windows.WindowState.Normal;

            mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
        }

        private void Work()
        {
            FakeButton.IsEnabled = !FakeButton.IsEnabled;
            running = true;
            this.Topmost = true;
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string fileName = @".\\time.dat";
            if (File.Exists(fileName)) File.Delete(fileName);

            FileStream fs = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            long time = GetTimeStampNow() + (timeChoose.SelectedIndex + 1) * 3;
            bw.Write(time);

            bw.Close();
            fs.Close();

            ReadTime();
        }
    }
}
