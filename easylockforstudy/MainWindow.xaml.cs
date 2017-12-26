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
        private const uint WM_SYSCOMMAND = 0x112;                   
        private const int SC_MONITORPOWER = 0xF170;                  
        private const int MonitorPowerOff = 2;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

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
            running = true;
            while (GetTimeStampNow() < target)
            {
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MonitorPowerOff);
            }
            running = false;
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
                    MaxScreen();
                    BGWorker.DoWork += LOCK;
                    BGWorker.RunWorkerCompleted += Restore;
                    BGWorker.RunWorkerAsync(target);
                }
                return;
            }
            else return;
        }

        private void Restore(object sender, RunWorkerCompletedEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void MaxScreen()
        {
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
