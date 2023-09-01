using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using A1.GUI;

namespace A1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            Server serverWin = new Server();
            serverWin.Show();

        }

        private void CacheButton_Click(object sender, RoutedEventArgs e)
        {
            Cache cacheWin = new Cache();
            cacheWin.Show();
        }

        private void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            Client clientWin = new Client();
            clientWin.Show();
        }
    }
}
