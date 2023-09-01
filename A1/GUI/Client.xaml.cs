using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using A1.Sockets;

namespace A1.GUI
{
    public partial class Client : Window
    {
        ClientSck client;
        string folderPath;
        public Client()
        {
            InitializeComponent();
            client = new ClientSck();
            folderPath = @"download\";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            client.FileUpdate += OnFileUpdate;
            client.ImageUpdate += OnImageUpdate;
            Start();

            
        }

        private void Start()
        {
            try
            {
              client.Connect();
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedFile = (string)FileList.SelectedItem;
            client.DownloadReq(selectedFile);
        }

        public void OnImageUpdate(string fileName)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Image img = new Image();
                    img.Width = 200;
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    string filePath = System.IO.Path.Combine(folderPath, fileName);
                    string uriString = "file:///" + System.IO.Path.GetFullPath(filePath).Replace("\\", "/");
                    bitmap.UriSource = new Uri(uriString);
                    bitmap.DecodePixelHeight = 200;
                    bitmap.EndInit();
                    DownloadedImage.Source = bitmap;
                    ImageName.Text = fileName;
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            });
        }

        public void OnFileUpdate(string filename)
        {
            Dispatcher.Invoke(() =>
            {
                FileList.Items.Add(filename);
            });
        }
    }
}
