using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
using Microsoft.Win32;
using MyCryptLib;

namespace FileKeeperDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TCPManager tcpManager;

        public MainWindow()
        {
            InitializeComponent();
            //Run();
        }

        //async void Run()
        //{
        //    OpenFileDialog diag = new OpenFileDialog();
        //    diag.ShowDialog();
        //    diag.CheckPathExists = true;
        //    if (diag.FileName != null)
        //    {
        //        tcpManager = new TCPManager();
        //        CancellationTokenSource source = new CancellationTokenSource();
        //        tcpManager.FileStatusUpdated += (s, e) =>
        //        {
        //            if (e.Result == SendResult.Success)
        //            {
        //                MessageBox.Show("File has been successfully sent!");
        //                //source.Cancel();
        //            }
        //        };
        //        string Host = System.Net.Dns.GetHostName();
        //        IPAddress[] addresses = System.Net.Dns.GetHostAddresses(Host);
        //        TestLabel.Content = Host;
        //        foreach (IPAddress address in addresses)
        //        {
        //            TestLabel.Content += "\n" + address.ToString();
        //        }
        //        await Task.Run(() => tcpManager.RunEncryptedSendingServer(8005, diag.FileName, "Cypher", source.Token));
                
        //    }
        //}
    }
}
