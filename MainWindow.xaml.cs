using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using WPFMouseCon.Services;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFMouseCon
{
    public partial class MainWindow : Window
    {
        UdpClient udpClient;
        Thread listenerThread;
        public string? LocalIP { get; set; }

        public string? mouseScale { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            udpClient = new UdpClient(5000);
            listenerThread = new Thread(ListenForSensorData);
            listenerThread.Start();
            var getLocalInInfoServices = new GetLocalIpInfoServices();
            LocalIP = getLocalInInfoServices.GetLocalIPv4();
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private void ListenForSensorData()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string str = Encoding.UTF8.GetString(data);
                    var parts = str.Split(',');

                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double accX) &&
                        double.TryParse(parts[1], out double accY))
                    {
                        MoveMouse(accX, accY);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void MoveMouse(double accX, double accY)
        {
            // 灵敏度调节
            double scale = 50;
            try { scale = int.Parse(mouseScale); }
            catch (Exception ex) { scale = 50; };
            

            GetCursorPos(out POINT currentPos);

            int newX = currentPos.X + (int)(accX * scale);
            int newY = currentPos.Y - (int)(accY * scale); // Y 轴反向

            SetCursorPos(newX, newY);
        }
    }
}