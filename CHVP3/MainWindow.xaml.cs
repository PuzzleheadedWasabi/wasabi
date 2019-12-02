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

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public LogViewer()
        {
            InitializeComponent();

            DataContext = LogEntries = new ObservableCollection<LogEntry>();

            Log("Started CHVP");

            Timer = new Timer(x => CheckIfVLCRunning(), null, 0, 300);
        }

        private Process controllingProcess = null;
        private Timer Timer;
        private Random random = new Random();

        // Returns the file, or null if no file
        private string[] GetVLCFile(Process process)
        {
            string cli = GetCommandLine(process);

            // First get the path to the vlc executable
            string[] chunks = cli.Split('\"');

            if (chunks.Length <= 1) return null;

            string pathToVLC = chunks[1];
            Log("Path to VLC: " + pathToVLC);

            string check = "--started-from-file";
            int index = cli.IndexOf(check);

            if (index == -1) return null;

            cli = cli.Substring(index + check.Length);
            cli = cli.TrimStart();
            Debug.WriteLine(cli);

            chunks = cli.Split('\"');

            if (chunks.Length <= 1) return null;

            // The first index should be empty or an equals sign or something, cbs checking
            string filePath = chunks[1];
            Debug.WriteLine(filePath);

            bool exists = File.Exists(filePath);
            Debug.WriteLine(exists);

            if (!exists) return null;

            return new string[] { pathToVLC, filePath };
        }
        
        private void ControlVLC(Process process, string[] vlcDetails)
        {
            process.Kill();

            VLCInterface vlcInterface = new VLCInterface(vlcDetails[0], vlcDetails[1], Log);

            try
            {
                controllingProcess = vlcInterface.Connect();

                Log("Connected to VLC process!");

                Thread.Sleep(700);
                vlcInterface.Send("seek 20");

                Thread.Sleep(700);
                vlcInterface.Send("pause");

                Thread.Sleep(700);
                vlcInterface.Send("pause");
                Log("Time is " + vlcInterface.GetTime());

                Thread.Sleep(700);

                vlcInterface.Send("quit"); // close vlc rc interface and disconnect
                vlcInterface.Disconnect();
            }
            finally
            {
                vlcInterface.Kill();
            }

            //Thread.Sleep(2000);
            //controllingProcess.StandardInput.WriteLine("pause");

            controllingProcess.WaitForExit();

            Log("Process has exited!");
            controllingProcess = null;

        }

        private void CheckIfVLCRunning()
        {
            Log("CheckIfVLCRunning");

            Process vlc = GetVLCProcess();
            if (vlc == null) return;

            string[] vlcDetails = GetVLCFile(vlc);
            if (vlcDetails == null) return;

            ControlVLC(vlc, vlcDetails);

        }

        private Process GetVLCProcess()
        {

            // If we are already controlling a process, then just exit
            if (controllingProcess != null) return null;

            Process[] processes = Process.GetProcesses();

            // Only support single VLC process
            foreach (Process process in processes)
            {
                if (!process.ProcessName.ToLower().Equals("vlc")) continue;
                String cli = GetCommandLine(process);
                if (cli == null) continue;
                if (cli.ToLower().Contains("--intf rc")) continue;
                Debug.WriteLine("Found VLC, ID: {0}, CLI: {1}", process.Id, GetCommandLine(process));
                return process;
            }

            return null;
        }

        private void Log(string message)
        {
            Dispatcher.BeginInvoke((Action)(() => LogEntries.Add(new LogEntry(message))));
        }

        private static string GetCommandLine(Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }

        }

    }

}