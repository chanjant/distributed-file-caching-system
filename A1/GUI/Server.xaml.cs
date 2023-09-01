using A1.Sockets;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace A1.GUI
{
    public partial class Server : Window
    {

        List<string> fileNames;
        ServerSck serverSck;
        string folderPath;
        public Server()
        {
            InitializeComponent();
            fileNames = new List<string>();
            folderPath = @"UploadedFiles\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            Start();
        }

        private void Start()
        {
            string[] filePaths = Directory.GetFiles(folderPath);
            if(filePaths != null)
            {
                foreach(string filePath in filePaths)
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    fileNames.Add(fileName);
                    UploadedFilesList.Items.Add(fileName);
                } 
            }
            serverSck = new ServerSck(fileNames);
            Task.Run(() =>
            {
                serverSck.Connect();
            });
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            bool? response = openFileDialog.ShowDialog();

            if (response == true)
            {
                string filepath = openFileDialog.FileName;
                string filename = System.IO.Path.GetFileName(filepath);

                string uploadPath = System.IO.Path.Combine(folderPath);
                bool exists = Directory.Exists(uploadPath);
                if (!exists)
                {
                    Directory.CreateDirectory(uploadPath);
                }
                uploadPath += filename;
                try
                {
                    File.Copy(filepath, uploadPath);
                    UploadedFilesList.Items.Add(filename);
                    serverSck.UpdateFileList(filename);
                    
                    MessageBox.Show("Uploaded!");
                }
                catch (IOException)
                {
                    MessageBox.Show("File already exists!");
                }

            }
        }

     
    }

}
