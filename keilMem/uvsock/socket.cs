﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace keilMem.uvsock
{
    public class SocketClient
    {
        private static MainWindow mainPage = MainWindow.Current;

        public static Socket sender;
        public static SocketFlags socketFlags;

        // Data buffer for incoming data.  
        static byte[] bytes = new byte[1024];

        public static void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // This example uses port 11000 on the local computer.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                //IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 4823);
                //IPEndPoint remoteEP = new IPEndPoint(ipAddress, 4800);

                // Create a TCP/IP  socket.  
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());
                    //更新UI - 连接成功
                    mainPage.connectDone();
                    /*
                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    */
                    //启动接收线程
                    Thread recThread = new Thread(TaskRec);
                    recThread.Start();
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    mainPage.connectError(ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    mainPage.connectError(se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    mainPage.connectError(e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                mainPage.connectError(e.ToString());
            }
        }
        public static void TaskRec()
        {
            while(true)
            {
                if (!SocketConnected(sender))
                {
                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    mainPage.connectBreak();
                    return;
                }
                // Receive the response from the remote device.  
                int bytesRec = sender.Receive(bytes);
                //Console.WriteLine("sender.Connected:{0}", sender.Connected);
                //Thread.Sleep(500);
                Console.WriteLine("Receive = {0}", BitConverter.ToString(bytes, 0, bytesRec));
                Uvsock.rxProcess(bytes, bytesRec);
            }
        }

        private static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }

    }
}
