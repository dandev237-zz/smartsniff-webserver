using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace smartsniff_api.Models
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string[] urls = new string[] { "http://localhost:5000", "http://192.168.1.199:5000" };

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls(urls)
                .Build();

            host.Run();
        }
    }
}