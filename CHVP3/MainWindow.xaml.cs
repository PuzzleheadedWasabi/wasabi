using System;
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
using CHVP3.Properties;
using System.Collections.ObjectModel;
using System.Threading;
using System.Diagnostics;

using System.Management;
using System.IO;

using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;

namespace CHVP3
{

    public partial class LogViewer : Window
    {

        //private bool controllingProcess = false;
        private VLCInterface vlcInterface = null;

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public LogViewer()
        {
            InitializeComponent();

            DataContext = LogEntries = new ObservableCollection<LogEntry>();
            Log("Started CHVP");

            vlcInterface = new VLCInterface(this);
            vlcInterface.CreateBindings();

            Thread backgroundThread = new Thread(x => BackgroundLoop());
            backgroundThread.IsBackground = true;
            backgroundThread.Start();
            
        }

        private void BackgroundLoop()
        {
            while (true)
            {
                vlcInterface.Connect();
                ControlVLC();

                Log("Re-initiating loop in 2 seconds...");
                Thread.Sleep(2000);
            }
        }

        private void ControlVLC()
        {

            //controllingProcess = true;

            try
            {
                //controllingProcess = vlcInterface.Connect();
                //controllingProcess = process;

                Log("Connected to VLC process!");

                Thread.Sleep(700);
                Log("Example: Seeking to 20 seconds");
                vlcInterface.Send("seek 20");

                Thread.Sleep(700);
                Log("Example: Pausing playback");
                vlcInterface.Send("pause");

                Thread.Sleep(700);
                Log("Example: Resuming playback");
                vlcInterface.Send("pause");
                Log("Example: The current time is " + vlcInterface.GetTime());
                
                Thread.Sleep(700);

                Log("Example: Quitting VLC");
                vlcInterface.Disconnect();
            }
            finally
            {
                //vlcInterface.Kill();
            }

            //Thread.Sleep(2000);
            //controllingProcess.StandardInput.WriteLine("pause");

            //controllingProcess.WaitForExit();

            //Log("Process has exited!");
            //controllingProcess = false;

        }

        public void Log(Exception e)
        {
            Dispatcher.BeginInvoke((Action)(() => LogEntries.Add(new LogEntry(e.ToString())) ));
        }

        public void Log(string message)
        {
            Dispatcher.BeginInvoke((Action)(() => LogEntries.Add(new LogEntry(message))));
        }

    }

}