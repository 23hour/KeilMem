using InteractiveDataDisplay.WPF;
using keilMem.uvsock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace keilMem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Current;

        Uvsock uv = new Uvsock();
        Byte[] sendBuffer;
        SocketFlags flags;

        const int dataSize = 500;
        private Queue<double> dataQueue0 = new Queue<double>(dataSize);
        LineGraph linegraph0 = new LineGraph();

        int counter = 0;

        DispatcherTimer timer = new DispatcherTimer();

        UInt64 addr_64; //地址
        UInt32 size_32; //总大小 字节
        int interval; //读取间隔时间 ms
        int dataTypeIndex; //数据类型索引


        public MainWindow()
        {
            InitializeComponent();
            Current = this;

            lines.Children.Add(linegraph0);
            linegraph0.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 155, 0));
            //linegraph0.Description = String.Format("Channel 'open'");
            linegraph0.StrokeThickness = 2;
        }

        private void BtnConnect_clicked(object sender, RoutedEventArgs e)
        {
            this.ConnectStatus.Visibility = Visibility.Hidden;
            this.ConnectImg.Source = new BitmapImage(new Uri(@"Image/waitting.png", UriKind.Relative));
            this.ConnectImg.Visibility = Visibility.Visible;
            ((Storyboard)FindResource("ConnectStoryboard")).Begin();

            //SocketClient.StartClient();

            //启动连接线程
            Thread connThread = new Thread(SocketClient.StartClient);
            connThread.Start();

            this.BtnConnect.IsEnabled = false;
        }

        //更新UI - 连接成功
        public async void connectDone()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ((Storyboard)FindResource("ConnectStoryboard")).Stop();
                this.ConnectImg.Source = new BitmapImage(new Uri(@"Image/done2.png", UriKind.Relative));
                this.BtnMemREAD.IsEnabled = true;
            });
        }
        //更新UI - 连接失败
        public async void connectError(string errStr)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                this.ConnectStatus.Text = "Error Message:"+errStr;
                this.ConnectStatus.Visibility = Visibility.Visible;

                this.BtnConnect.IsEnabled = true; //可重新连接

                ((Storyboard)FindResource("ConnectStoryboard")).Stop();
                this.ConnectImg.Source = new BitmapImage(new Uri(@"Image/error.png", UriKind.Relative));
            });
        }
        //更新UI - 连接断开
        public async void connectBreak()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                this.BtnConnect.IsEnabled = true; //可重新连接
                this.ConnectImg.Source = new BitmapImage(new Uri(@"Image/break2.png", UriKind.Relative));
                this.BtnMemREAD.IsEnabled = false;
            });
        }

        private void BtnGetVersion_clicked(object sender, RoutedEventArgs e)
        {
            SocketClient.sender.Send(uv.GET_VERSION(), (int)uv.UVSOCK_CMD.m_nTotalLen, flags);

            Console.WriteLine("Send:{0}", BitConverter.ToString(uv.GET_VERSION()));
        }

        private void BtnMemREAD_clicked(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                this.BtnMemREAD.Content = "Memery Read";
                return;
            }

            string addrStr = this.TextAddr.Text;
            string sizeStr = this.TextSize.Text;
            string tInterval = this.TextUpdateInterval.Text;

            linegraph0.Description = String.Format(addrStr);

            addr_64 = (UInt64)Convert.ToInt32(addrStr, 16);
            size_32 = (UInt32)Int32.Parse(sizeStr);
            interval = int.Parse(tInterval); //interval time 

            dataTypeIndex = DataType.SelectedIndex;

            if (interval > 0)
            {
                timer = new DispatcherTimer();
                timer.Tick += TimerTick;
                timer.Interval = TimeSpan.FromMilliseconds(interval);
                timer.Start();

                this.BtnMemREAD.Content = "STOP";
            }
            else {
                uv.MemRead(addr_64, size_32, dataTypeIndex);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            uv.MemRead(addr_64, size_32, dataTypeIndex);
        }

        public async void UpdateChart(double value0)
        {
            counter++;

            if (dataQueue0.Count > (dataSize - 1))
            {
                dataQueue0.Dequeue();
            }
            dataQueue0.Enqueue(value0);

            await Dispatcher.InvokeAsync(() =>
            {
                linegraph0.PlotY(dataQueue0.ToArray());
            });
        }

    }
}
