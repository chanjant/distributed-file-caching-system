using A1.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace A1.Sockets
{
    public class CacheSck
    {
        IPAddress ipAddr;
        int clientPort;
        int serverPort;
        RabinFingerprintCache cache;
        RabinFingerprint rabin;
        public event Action<string> LogUpdate;
        public event Action<string> FragmentUpdate;

        public CacheSck(RabinFingerprintCache cache) 
        {
            ipAddr = IPAddress.Loopback;
            clientPort = 8081;
            serverPort = 8082;
            this.cache = cache;
            rabin = new RabinFingerprint();
        }
        public void Connect()
        {
            Console.WriteLine("Cache: Request file list from server");
            byte[] fileListBytes = FileListReq();


            Console.WriteLine("Cache: Send fingerprints to server");
            SendFingerprint();
            
            Console.WriteLine("Cache: Start listening");

            TcpListener listener = new TcpListener(ipAddr, clientPort);
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
                        
                        byte[] fileListLengthBytes = BitConverter.GetBytes(fileListBytes.Length);

                        byte[] fileListdata = new byte[5 + fileListBytes.Length];

                        fileListdata[0] = command;
                        Array.Copy(fileListLengthBytes, 0, fileListdata, 1, fileListLengthBytes.Length);
                        Array.Copy(fileListBytes, 0, fileListdata, 5, fileListBytes.Length);
                        
                        stream.Write(fileListdata, 0, fileListdata.Length);
                        stream.Flush();
                        stream.Close();
                        break;
                    case 1:
                        // request download file
                        stream.Read(data, 0, 4);

                        int fileNameBytesLength = BitConverter.ToInt32(data, 0);
                        byte[] fileNameBytes = new byte[fileNameBytesLength];
                        stream.Read(fileNameBytes, 0,fileNameBytesLength);

                        string fileName = Encoding.UTF8.GetString(fileNameBytes);
                        string currentTime = DateTime.Now.ToString();
                        string record = $"User request: {fileName} at {currentTime}\n";
                        Task.Run(() =>
                        {
                            UpdateLog(record);
                        });

                        // forward request to server
                        byte[] response = DownloadReq(fileName);
                        
                        // forward response to client

                        stream.Write(response, 0, response.Length);
                        stream.Flush();
                        stream.Close();

                        break;
                        default: break;
                }
            }
        }

        private byte[] FileListReq()
        {
            try
            {
                TcpClient client = new TcpClient(ipAddr.ToString(), serverPort);
 
                // send 0 to server
                byte command = 0;
                using (NetworkStream stream = client.GetStream())
                {
                    stream.WriteByte(command);
                    stream.Flush();

                    // Read response
                    byte[] data = new byte[4];
                    stream.Read(data, 0, data.Length);

                    int fileListBytesLength = BitConverter.ToInt32(data, 0);
                    data = new byte[fileListBytesLength];
                    stream.Read(data,0, fileListBytesLength);
   
                    stream.Close();
                    return data;
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);  
            }
            return null;
        }

        private void SendFingerprint()
        {
            try
            {
                TcpClient client= new TcpClient(ipAddr.ToString(), serverPort);
                // send 2 to server
                byte command = 2;

                // Retrieve fingerprints
                List<uint> fingerprints = cache.GetFingerprints();
                string fingerprintsStr = JsonConvert.SerializeObject(fingerprints);
                byte[] fingerprintsBytes = Encoding.UTF8.GetBytes(fingerprintsStr);
                
                byte[] fingerprintsLengthBytes = BitConverter.GetBytes(fingerprintsBytes.Length);

                byte[] fingerprintsData = new byte[5 + fingerprintsBytes.Length];

                fingerprintsData[0] = command;
                Array.Copy(fingerprintsLengthBytes, 0, fingerprintsData, 1, fingerprintsLengthBytes.Length);
                Array.Copy(fingerprintsBytes, 0, fingerprintsData, 5, fingerprintsBytes.Length);
                
                using(NetworkStream stream = client.GetStream())
                {
                    stream.Write(fingerprintsData, 0, fingerprintsData.Length);
                    stream.Flush();
                    stream.Close();
                }
            } catch(Exception ex) 
            { 
                Console.WriteLine(ex.Message);
            }
        }

        private byte[] DownloadReq(string fileName)
        {
            List<byte[]> dataBlocks = new List<byte[]>();
            try
            {
                TcpClient client = new TcpClient(ipAddr.ToString(), serverPort);

                // send 1 to server
                byte command = 1;

                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);

                byte[] fileNameData = new byte[5 + fileNameBytes.Length];

                fileNameData[0] = command;
                Array.Copy(fileNameLengthBytes, 0, fileNameData, 1, fileNameLengthBytes.Length);
                Array.Copy(fileNameBytes, 0, fileNameData, 5, fileNameBytes.Length);
                
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(fileNameData, 0, fileNameData.Length);
                    stream.Flush();

                    // Read response
                    byte[] resData = new byte[4];
                    stream.Read(resData, 0, 4);
                    
                    int fileBytesLength = BitConverter.ToInt32(resData, 0);
                    resData = new byte[fileBytesLength];
                    stream.Read(resData, 0, fileBytesLength);

                    // process data
                    string fileStr = Encoding.ASCII.GetString(resData, 0, resData.Length);
                    int cacheCount = 0;
                    int serverCount = 0;
                    
                    List<byte[]> fileList = JsonConvert.DeserializeObject<List<byte[]>>(fileStr);
                  
                    foreach (byte[] f in fileList)
                    {
                        byte type = f[0];
                       
                        byte[] content = f.Skip(1).ToArray();
                        if (type == 0)
                        {
                            // fingerprint
                            uint hash = BitConverter.ToUInt32(content, 0);
                            byte[] cachedDataBlock = cache.GetDataBlock(hash);
                            dataBlocks.Add(cachedDataBlock);
                            cacheCount += cachedDataBlock.Length;
                        }else if (type == 1)
                        {
                            // datablock
                            int bytesToRead = 2048 - 32;
                            uint fingerprint = rabin.Fingerprint(content, 0, bytesToRead);
                            dataBlocks.Add(content);
                            serverCount += content.Length;
                            Task.Run(() =>
                            {
                                cache.AddDataBlock(fingerprint, content);
                                UpdateFragment(fingerprint.ToString());
                            });
                        }
                    }
                    int totalCount = cacheCount + serverCount;
                    double percentage = (double)cacheCount / totalCount * 100;
                    string record = $"Response: {percentage}% of {fileName} was constructed with the cached data\n+++++\n";
                    Task.Run(() =>
                    {
                        UpdateLog(record);
                    });
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            byte[] file = dataBlocks.SelectMany(block => block).ToArray();
            return file;
        }
 
        public Boolean Clear()
        {
            try
            {
                TcpClient client = new TcpClient(ipAddr.ToString(), serverPort);

                // send 3 to server
                byte command = 3;
                using (NetworkStream stream = client.GetStream())
                {
                    stream.WriteByte(command);
                    stream.Flush();

                    // Read response
                   byte response = (byte)stream.ReadByte();
                   if(response == 0)
                    {
                        
                        Task.Run(() =>
                        {
                            UpdateLog("Clearing cache ...\n");
                            cache.ClearCache();
                            UpdateLog("Cache cleared\n+++++\n");
                        });
                        return true;
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        private void UpdateLog(string message)
        {
            LogUpdate?.Invoke(message);
        }

        private void UpdateFragment(string fingerprint)
        {
            FragmentUpdate?.Invoke(fingerprint);
        }

    }
}
