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

        private readonly string VLCPath;
        private readonly string FilePath;
        private readonly Action<string> Log;

        private Process controllingProcess;
        private Socket vlcRcSocket;

        private BlockingCollection<string> Output = new BlockingCollection<string>();

        public VLCInterface(string vlcPath, string filePath, Action<string> log)
        {
            this.VLCPath = vlcPath;
            this.FilePath = filePath;
            this.Log = log;
        }

        public void Disconnect()
        {
            vlcRcSocket.Disconnect(false);
        }

        public void Kill()
        {
            controllingProcess.Kill();
        }

        public Process Connect()
        {
            IPEndPoint socketAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 54174);
            
            string args = "--fullscreen -I rc --rc-host " + socketAddress.ToString() + $" \"{FilePath}\"";
            //string args = "-I rc --rc-host " + socketAddress.ToString() + $" \"{FilePath}\"";

            Log("Running command: " + args);

            controllingProcess = new Process();
            controllingProcess.StartInfo.UseShellExecute = false;
            controllingProcess.StartInfo.FileName = VLCPath;
            controllingProcess.StartInfo.Arguments = args;
            controllingProcess.Start();

            Log("PID of controllingProcess is " + controllingProcess.Id);

            vlcRcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            vlcRcSocket.Connect(socketAddress);

            Task listener = Task.Factory.StartNew(() => Receive());

            return controllingProcess;
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
            // send command to vlc socket, note \n is important
            byte[] commandData = UTF8Encoding.UTF8.GetBytes(String.Format("{0}\n", command));
            int sent = vlcRcSocket.Send(commandData);
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
                Debug.WriteLine("Socket exception occured, VLC was probably closed");
            }
        }

    }
}
