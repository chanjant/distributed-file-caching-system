using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using A1.Sockets;
using A1.Tools;

namespace A1.GUI
{

    public partial class Cache : Window
    {
        CacheSck cacheSck;
        RabinFingerprintCache cache;
        List<uint> fingerprints;
        public Cache()
        {
            InitializeComponent();
            cache = new RabinFingerprintCache();
            fingerprints = cache.GetFingerprints();
            cacheSck = new CacheSck(cache);
            cacheSck.LogUpdate += OnLogUpdate;
            cacheSck.FragmentUpdate += OnFragmentUpdate;
            Task.Run(() =>
            {
                Start();
            });
            
        }

        private void Start()
        {
            foreach(uint fingerprint in fingerprints)
            {
                OnFragmentUpdate(fingerprint.ToString());
            }
            try
            {
                cacheSck.Connect();
            } catch(Exception ex)
            {
                MessageBox.Show($"Cache: Connection failed\n{ex.Message}");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if(cacheSck.Clear())
            {
                FragmentContent.Clear();
                FragmentsList.Items.Clear();
            }
        }

        public void OnLogUpdate(string record)
        {
            Dispatcher.Invoke(() =>
            {
                logEntry.Text += record;
            });
        }

        public void OnFragmentUpdate(string fingerprint)
        {
            
            Dispatcher.Invoke(() =>
            {
                FragmentsList.Items.Add(fingerprint);
            });
        }

        private void FragmentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFragment = (string)FragmentsList.SelectedItem;
            try
            {
                uint fingerprint = uint.Parse(selectedFragment);
                byte[] dataBlock = cache.GetDataBlock(fingerprint);
                string hexString = BitConverter.ToString(dataBlock).Replace("-", "");
                FragmentContent.Text = hexString;
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }
    }
}
