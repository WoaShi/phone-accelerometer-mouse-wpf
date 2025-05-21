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
        private DateTime lastUpdateTime = DateTime.Now;

        private double velocityX = 0;
        private double velocityY = 0;

        private const double damping = 0.92;              // 衰减系数（乘法方式）
        private const double minSpeedThreshold = 0.005;   // 忽略低于此值的微抖动速度
        private const double maxSpeed = 80;               // 限制最大速度
        private const double zLiftThreshold = 0.2;        // 手机抬起判断阈值（Z轴）

        private double smoothedVelX = 0;
        private double smoothedVelY = 0;

        private const double smoothingFactor = 0.2;

        private bool isRunning = true;

        public string? LocalIP { get; set; }
        public string? mouseScale { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

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
            while (isRunning)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string str = Encoding.UTF8.GetString(data);

                    var parts = str.Split(',');

                    if (parts.Length >= 2 &&
                        double.TryParse(parts[0], out double velX) &&
                        double.TryParse(parts[1], out double velY))
                    {
                        double accZ = 0;
                        if (parts.Length >= 3 && double.TryParse(parts[2], out double parsedZ))
                            accZ = parsedZ;

                        MoveMouse(velX, velY, accZ);
                    }
                }
                catch (SocketException ex)
                {
                    if (isRunning)
                        Console.WriteLine($"Socket Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void MoveMouse(double inputVelX, double inputVelY, double accZ)
        {
            if (Math.Abs(accZ) > zLiftThreshold)
            {
                velocityX = 0;
                velocityY = 0;
                smoothedVelX = 0;
                smoothedVelY = 0;
                return;
            }

            DateTime now = DateTime.Now;
            double deltaTime = (now - lastUpdateTime).TotalSeconds * 10;
            if (deltaTime <= 0)
                return;
            lastUpdateTime = now;

            if (Math.Abs(inputVelX) < 0.001) inputVelX = 0;
            if (Math.Abs(inputVelY) < 0.001) inputVelY = 0;

            double scale = 10000;
            try { scale = int.Parse(mouseScale); } catch { scale = 10000; }

            velocityX = inputVelX;
            velocityY = inputVelY;

            // 滤波和平滑
            smoothedVelX = smoothingFactor * velocityX + (1 - smoothingFactor) * smoothedVelX;
            smoothedVelY = smoothingFactor * velocityY + (1 - smoothingFactor) * smoothedVelY;

            velocityX *= damping;
            velocityY *= damping;

            if (Math.Abs(smoothedVelX) < minSpeedThreshold) smoothedVelX = 0;
            if (Math.Abs(smoothedVelY) < minSpeedThreshold) smoothedVelY = 0;

            GetCursorPos(out POINT currentPos);
            int moveX = (int)(-smoothedVelX * scale * deltaTime * deltaTime * 0.5);
            int moveY = (int)(smoothedVelY * scale * deltaTime * deltaTime * 0.5);

            if (moveX != 0 || moveY != 0)
            {
                SetCursorPos(currentPos.X + moveX, currentPos.Y + moveY);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            isRunning = false;
            udpClient?.Close();

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join();
            }

            Application.Current.Shutdown();
        }
    }
}
