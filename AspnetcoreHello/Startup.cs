namespace AspnetcoreHello
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ILogger<Startup> logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();

            int pid = Process.GetCurrentProcess().Id;
            logger.LogInformation($"PID: {pid}");

            app.Run(async context =>
            {
                logger.LogDebug("We said hello");
                await context.Response.WriteAsync($"Hello world from ASP.NET Core ({pid})");
            });
        }
    }
}
