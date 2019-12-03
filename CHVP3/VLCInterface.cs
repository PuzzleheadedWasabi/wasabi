using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CHVP3
{
    class VLCInterface
    {

        //private readonly string VLCPath;
        //private readonly string FilePath;
        private readonly LogViewer LogViewer;

        //private Process controllingProcess;
        private Socket vlcRcSocket;

        private BlockingCollection<string> Output = new BlockingCollection<string>();

        public VLCInterface(LogViewer logViewer)
        {
            this.LogViewer = logViewer;
        }

        //public VLCInterface(string vlcPath, string filePath, LogViewer logViewer)
        //{
        //    this.VLCPath = vlcPath;
        //    this.FilePath = filePath;
        //    this.LogViewer = logViewer;
        //}

        public bool CreateBindings()
        {
            try
            {
                string vlcConfigFile =          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vlc\\vlcrc");
                string vlcConfigFileBackup1 =   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vlc\\vlcrc-chvp-1.bak");
                string vlcConfigFileBackup2 =   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vlc\\vlcrc-chvp-2.bak");

                bool exists = File.Exists(vlcConfigFile);
                if (!exists)
                {
                    LogViewer.Log("VLC Config file not found at path " + vlcConfigFile);
                    return false;
                }

                if (!File.Exists(vlcConfigFileBackup1)) File.Copy(vlcConfigFile, vlcConfigFileBackup1);
                if (File.Exists(vlcConfigFileBackup2)) File.Delete(vlcConfigFileBackup2);
                File.Copy(vlcConfigFile, vlcConfigFileBackup2);

                string[] lines = File.ReadAllLines(vlcConfigFile);
                List<string> newLines = new List<string>();

                //for (int i = 0; i < lines.Length; i++)     
                //string line = lines[i];

                // Copy all lines that don't have our config items
                string[] importantLines = new string[] { "extraintf", "rc-quiet", "rc-host" };
                string[] newConfigLines = new string[] {
                    "extraintf=oldrc",
                    "rc-quiet=1",
                    "rc-host=127.0.0.1:54174"
                };

                // Copy all good lines
                foreach (string line in lines)
                {

                    // 0 = line is fine, 1 = line is bad and needs to be disabled, 2 = line is chvp line
                    int lineStatus = 0;
                    //Debug.WriteLine(line);

                    if (line.Contains("#CHVP") && !line.Contains("#CHVP-DISABLED"))
                    {
                        //lineStatus = 2;
                        continue;
                    }
                    else
                    {
                        foreach (string importantline in importantLines)
                        {

                            if (line.ToLower().StartsWith(importantline))
                            {
                                lineStatus = 1;
                                break;
                            }
                        }
                    }

                    if (lineStatus == 0) newLines.Add(line);
                    //else if (lineStatus == 1) newLines.Add("#CHVP-DISABLED " + line);

                }

                // TODO: This operation forcibly changes new lines from unix to windows (LF to CRLF)
                newLines.Add("#CHVP");
                newLines.Add("#CHVP These lines were added by CHVP. Feel free to remove these.");
                newLines.AddRange(newConfigLines);
                File.WriteAllLines(vlcConfigFile, newLines.ToArray(), Encoding.UTF8);

                LogViewer.Log("Succesfully added VLC hook");
                return true;

            } catch (Exception e)
            {
                LogViewer.Log("An exception occured while trying to edit VLC config file");
                LogViewer.Log(e);
                return false;
            }
        }






        public void Disconnect()
        {
            Send("quit"); // close vlc rc interface and disconnect
            vlcRcSocket.Disconnect(false);
        }

        //public void Kill()
        //{
        //    //controllingProcess.Kill();
        //}

        public void Connect()
        {
            LogViewer.Log("Waiting for VLC to start");

            IPEndPoint socketAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54174);
            vlcRcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // TODO: some mechanism to tell it to stop listening if another api succeeds connection first
            bool connected = false;
            while (!connected)
            {
                try
                {
                    vlcRcSocket.Connect(socketAddress);
                    connected = true;
                } catch (SocketException) {}

                Thread.Sleep(500);
            }

            Task listener = Task.Factory.StartNew(() => Receive());

            LogViewer.Log("Connected to VLC! Waiting for file to be opened");

            BlockPlaying();

            LogViewer.Log("Valid file detected. Initiating VLC control.");

            //return controllingProcess;
        }

        public bool BlockPlaying()
        {
            while(true)
            {
                string title = SendAndGet("get_title");
                int duration = Int32.Parse(SendAndGet("get_length"));

                LogViewer.Log("Title: " + title + ", Duration: " + duration);

                // Check if the video is valid. This is an example
                if (title.ToLower().Contains("wasabi")) return true;

                Thread.Sleep(1000);
            }
            
        }

        public String GetTime()
        {
            return SendAndGet("get_time");
        }

        // TODO: This is clearly terrible, I'm sure there's a better way to do this
        public String SendAndGet(string command)
        {
            // Flush the thing first - forces EOL
            Send("get_time");

            Thread.Sleep(1000);
            
            // Now recreate the blocking collection and send the command
            Output = new BlockingCollection<string>();
            Send(command);
            return Output.Take().Trim();
        }

        public void Send(string command)
        {
            try
            {
                // send command to vlc socket, note \n is important
                byte[] commandData = UTF8Encoding.UTF8.GetBytes(String.Format("{0}\n", command));
                int sent = vlcRcSocket.Send(commandData);
            }
            catch (SocketException)
            {
                LogViewer.Log("Socket exception occured during SEND, VLC was probably closed. You'll have to restart the app to continue. TODO: handle this gracefully (see SendAndGet)");
            }
        }

        public void Receive()
        {
            try
            {
                do
                {
                    if (vlcRcSocket.Connected == false) break;
                    // check if there is any data
                    bool haveData = vlcRcSocket.Poll(100000, SelectMode.SelectRead); // 10 times a second

                    if (haveData == false) continue;
                    byte[] buffer = new byte[vlcRcSocket.ReceiveBufferSize];
                    using (MemoryStream mem = new MemoryStream())
                    {
                        while (haveData)
                        {
                            int received = vlcRcSocket.Receive(buffer);
                            mem.Write(buffer, 0, received);
                            haveData = vlcRcSocket.Poll(100000, SelectMode.SelectRead); // 10 times a second
                        }

                        Output.Add(Encoding.UTF8.GetString(mem.ToArray()));
                        Debug.WriteLine(Encoding.UTF8.GetString(mem.ToArray()));
                    }

                } while (true);
            } catch (SocketException)
            {
                LogViewer.Log("Socket exception occured during Receive, VLC was probably closed. You'll have to restart the app to continue. TODO: handle this gracefully (see SendAndGet)");
            }
        }

    }
}
