using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WPFMouseCon.Services
{
    public class GetLocalIpInfoServices
    {
        public string GetLocalIPv4()
        {
            string localIP = "未找到 IPv4 地址";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ip != null)
            {
                localIP = ip.ToString();
            }
            return localIP;
        }
    }
}
