using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WPFMouseCon.Services
{
    public class GetLocalIpInfoServices
    {
        public string GetLocalIPv4()
        {
            string localIP = "未找到内网 IP";
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                var ipProps = networkInterface.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address) &&
                        IsPrivateIP(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }
            return localIP;
        }

        private static bool IsPrivateIP(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            return
                (bytes[0] == 10) ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168);
        }

    }
}
