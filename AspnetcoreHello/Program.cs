namespace AspnetcoreHello
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_POOL_ID")))
                    {
                        // Not hosted in IIS, use HttpSys
                        Debug.Assert(OperatingSystem.IsWindows());
                        webBuilder.UseHttpSys(options =>
                        {
                            options.RequestQueueName = "AspnetCoreHelloSelfhost";
                            options.RequestQueueMode = RequestQueueMode.Create;
                        });
                    }

                    webBuilder.UseStartup<Startup>();
                });
    }
}
