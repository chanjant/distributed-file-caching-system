using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using A1.Tools;

namespace A1.Sockets
{
    public class ServerSck
    {
        IPAddress ipAddr;
        int port;
        private List<string> fileList;
        RabinFingerprint rabin;

        public ServerSck(List<string> fileList)
        {
            ipAddr = IPAddress.Loopback;
            port = 8082;
            this.fileList = fileList;
            rabin = new RabinFingerprint();
        }

        public void Connect()
        {
            Console.WriteLine("Server: Start listening");

            TcpListener listener = new TcpListener(ipAddr, port);
            listener.Start();

            // keep running
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                NetworkStream stream = client.GetStream();

                byte command = (byte)stream.ReadByte();
                byte[] data = new byte[4];

                switch (command)
                {
                    case 0:
                        // request file list
                        string fileListStr = JsonConvert.SerializeObject(fileList);
                        byte[] fileListBytes = Encoding.UTF8.GetBytes(fileListStr);
                        
                        byte[] fileListLengthBytes = BitConverter.GetBytes(fileListBytes.Length);

                        byte[] fileListData = new byte[4 + fileListBytes.Length];

                        Array.Copy(fileListLengthBytes, 0, fileListData, 0, fileListLengthBytes.Length);
                        Array.Copy(fileListBytes, 0, fileListData, 4, fileListBytes.Length);
                        
                        stream.Write(fileListData, 0, fileListData.Length);
                        stream.Flush();
                        stream.Close();

                        break;
                    case 1:
                        // request download file
                        stream.Read(data, 0, 4);

                        int fileNameBytesLength = BitConverter.ToInt32(data, 0);
                        byte[] fileNameBytes = new byte[fileNameBytesLength];
                        stream.Read(fileNameBytes, 0, fileNameBytesLength);

                        string fileName = Encoding.UTF8.GetString(fileNameBytes);

                        // retrieve file

                        List<byte[]> file = rabin.GetFingerprint(fileName);

                        string fileStr = JsonConvert.SerializeObject(file);
                        byte[] fileBytes = Encoding.ASCII.GetBytes(fileStr);
                        
                        byte[] fileLengthBytes = BitConverter.GetBytes(fileBytes.Length);
                        byte[] fileData = new byte[4 + fileBytes.Length];
                        Array.Copy(fileLengthBytes, 0, fileData, 0, fileLengthBytes.Length);
                        Array.Copy(fileBytes, 0, fileData, 4, fileBytes.Length);
                         
                        stream.Write(fileData, 0, fileData.Length);
                        stream.Flush();
                        stream.Close();

                        break;
                    case 2:
                        // receive fingerprints from cache
                        stream.Read(data, 0, 4);

                        int fingerprintsBytesLength = BitConverter.ToInt32(data, 0);
                        data = new byte[fingerprintsBytesLength];
                        stream.Read(data, 0, fingerprintsBytesLength);

                        string fingerprintStr = Encoding.UTF8.GetString(data);
                        List<uint> fingerprints = JsonConvert.DeserializeObject<List<uint>>(fingerprintStr);

                        int count = 0;
                        foreach (uint fp in fingerprints)
                        {
                            rabin.AddFingerprint(fp);
                            count++;
                        }
                        stream.Close();
                        break;
                    case 3:
                        // clear cache
                        rabin.ClearFingerprint();
                        stream.WriteByte(0);
                        stream.Flush();
                        stream.Close();
                        break;
                    default: break;
                }
            }
        }
        public void UpdateFileList(string fileName)
        {
            fileList.Add(fileName);
        }

    }
}

