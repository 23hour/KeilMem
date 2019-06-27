using keilMem.uvsock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace keilMem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Uvsock uv = new Uvsock();
        Byte[] sendBuffer;
        SocketFlags flags;

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void BtnConnect_clicked(object sender, RoutedEventArgs e)
        {
            SocketClient.StartClient();

        }

        private void BtnGetVersion_clicked(object sender, RoutedEventArgs e)
        {
            SocketClient.sender.Send(uv.GET_VERSION(), (int)uv.UVSOCK_CMD.m_nTotalLen, flags);

            Console.WriteLine("Send:{0}", BitConverter.ToString(uv.GET_VERSION()));
        }

        private void BtnMemREAD_clicked(object sender, RoutedEventArgs e)
        {
            string addrStr = this.TextAddr.Text;
            string sizeStr = this.TextSize.Text;

            UInt64 addr_64 = (UInt64)Convert.ToInt32(addrStr, 16);
            UInt32 size_32 = (UInt32)Int32.Parse(sizeStr);

            uv.MemRead(addr_64, size_32);
        }
    }
   
}
