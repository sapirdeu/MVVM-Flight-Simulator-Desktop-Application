using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FlightSimulatorApp.Model
{
    public class TelnetClient : ITelnetClient
    {
        private bool isConnected;
        private int buffSize;
        private TcpClient tcpClient;
        private NetworkStream networkworkStream;
        private byte[] writeBuff;
        private byte[] readBuff;
        private int usedCapacity;

        // Ctor.
        public TelnetClient()
        {
            isConnected = false;
            buffSize = 1024;
            writeBuff = new byte[buffSize];
            readBuff = new byte[buffSize];
            usedCapacity = 0;
        }

        // Open socket and connect to the simulator.
        public string Connect(string ip, string port)
        {
            try
            {
                int portNum = int.Parse(port);
                tcpClient = new TcpClient(ip, portNum);
                networkworkStream = tcpClient.GetStream();
                isConnected = true;
                return "Status: Connected to server";
            }
            catch (Exception ex)
            {
                string msgerr = ex.Message;
                return "Status: Cannot connect (invalid ip or port)";
            }
        }

        // Write data to the simulator.
        public string Write(string command)
        {
            string status = "";
            try
            {
                if (isConnected && (command != null && command != ""))
                {
                    byte[] data = Encoding.ASCII.GetBytes(command);

                    // Writing data to the write buffer and sending it to the simulator.
                    if (writeBuff.Length < data.Length + usedCapacity)
                    {
                        networkworkStream.Write(writeBuff, 0, usedCapacity);
                        networkworkStream.Flush();
                        usedCapacity = 0;
                    }
                    Array.ConstrainedCopy(data, 0, writeBuff, usedCapacity, data.Length);
                    usedCapacity += data.Length;
                    networkworkStream.Write(writeBuff, 0, usedCapacity);
                    networkworkStream.Flush();
                    usedCapacity = 0;
                }
            }
            catch (Exception ex)
            {
                string msgerr = ex.Message;
                byte[] blank = Encoding.ASCII.GetBytes("");
                Array.ConstrainedCopy(blank, 0, readBuff, usedCapacity, blank.Length);
                usedCapacity = 0;
                status = "Status: Server has disconnected";
            }
            return status;
        }

        // Read data from the simulator.
        public string Read()
        {
            string status = "";
            try
            {
                // Timeout notify when server didn't respond for 10 seconds.
                tcpClient.ReceiveTimeout = 10000;

                // Reading data from the buffer.
                int bytesRead = networkworkStream.Read(readBuff, 0, buffSize);
                return System.Text.Encoding.UTF8.GetString(readBuff, 0, bytesRead);
            }
            catch (Exception ex)
            {
                string errmsg = ex.Message;
                status = "Status: Server timeout";
            }

            isConnected = false;
            return status;
        }

        // Disconnect from the simulator.
        public string Disconnect()
        {
            if (tcpClient == null)
            {
                return "";
            }
            tcpClient.Close();
            isConnected = false;
            return "Status: Disconnected from server";
        }
    }
}
