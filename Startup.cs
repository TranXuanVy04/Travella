using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

// DÒNG QUAN TRỌNG NHẤT: Đánh dấu đây là file khởi động
[assembly: OwinStartup(typeof(Trave.Startup))]

namespace Trave
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Kích hoạt SignalR
            app.MapSignalR();
        }
    }
}