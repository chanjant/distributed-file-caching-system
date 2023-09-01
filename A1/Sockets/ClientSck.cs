using Newtonsoft.Json;
using System; 
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace A1.Sockets
{
    public class ClientSck
    {
        IPAddress ipAddr;
        int port;
        public event Action<string> ImageUpdate;
        public event Action<string> FileUpdate;
        string folderPath;

        public ClientSck()
        {
            ipAddr = IPAddress.Loopback;
            port = 8081;
            folderPath = @"download\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public void Connect()
        {
            Console.WriteLine("Client: Request file list");
            FileListReq();
        }
        private void FileListReq()
        {
            try
            {
                TcpClient client = new TcpClient(ipAddr.ToString(), port);

                // send 0 to cache
                byte command = 0;
                using (NetworkStream stream = client.GetStream())
                {
                    stream.WriteByte(command);
                    stream.Flush();

                    byte[] data = new byte[4];
                    stream.Read(data, 0, data.Length);

                    int fileListBytesLength = BitConverter.ToInt32(data, 0);
                    data = new byte[fileListBytesLength];
                    stream.Read(data, 0, fileListBytesLength);
                    
                    stream.Close();

                    // process data
                    string fileListStr = Encoding.UTF8.GetString(data, 0, data.Length);
                    List<string> fileList = JsonConvert.DeserializeObject<List<string>>(fileListStr);
                    foreach (string fileName in fileList)
                    {
                        Task.Run(() =>
                        {
                            UpdateFile(fileName);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void DownloadReq(string fileName)
        {
            try
            {
                Console.WriteLine($"Client: Reqest download {fileName}");
                TcpClient client = new TcpClient(ipAddr.ToString(), port);

                // send 1 to cache
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
                    byte[] buffer = new byte[2048];
                    MemoryStream ms = new MemoryStream();

                    while (true)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        ms.Write(buffer, 0, bytesRead);
                    }

                    byte[] imageData = ms.ToArray();
                    if(imageData.Length == 0)
                    {
                        MessageBox.Show("Download failed\nCheck your connection");
                        return;
                    }
                    // Process data
                    string filePath = Path.Combine(folderPath, fileName);
                    File.WriteAllBytes(filePath, imageData);
                    Task.Run(() =>
                    {
                        UpdateImage(fileName);
                    });
                }
               
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void UpdateImage(string filename)
        {
            ImageUpdate?.Invoke(filename);
        }

        private void UpdateFile(string filename)
        {
            FileUpdate?.Invoke(filename);
        }
    }
}
