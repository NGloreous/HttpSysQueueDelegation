namespace Delegator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DelegationConfig>(this.Configuration.GetSection("DelegationConfig"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ILogger<Startup> logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            IServerDelegationFeature delegator = app.ServerFeatures.Get<IServerDelegationFeature>();
            if (delegator == null)
            {
                throw new NotSupportedException($"The {nameof(IServerDelegationFeature)} is not supported by the current OS.");
            }

            DelegationRule[] rules = app.ApplicationServices.GetRequiredService<IOptions<DelegationConfig>>().Value.Rules
                .Where(ruleConfig => ruleConfig.Enabled)
                .Select(ruleConfig =>
                {
                    var rule = delegator.CreateDelegationRule(ruleConfig.QueueName, ruleConfig.Uri);
                    lifetime.ApplicationStopped.Register(() =>
                    {
                        try
                        {
                            // Note that dispose doesn't properly cleanup everything.
                            // This is only an issue if you want to create the same rule again in the same process.
                            // Shutdown of the process properly cleans up everything.
                            // https://github.com/dotnet/aspnetcore/issues/27126
                            rule.Dispose();
                        }
                        catch (ArgumentNullException ex)
                        {
                            // There is a bug in rule cleanup that causes a failure
                            // https://github.com/dotnet/aspnetcore/issues/26989
                            // This failure then causes a null ref bug to get hit
                            // https://github.com/dotnet/aspnetcore/issues/26982
                            logger.LogWarning(ex, "Known issue with disposing delegation rules");
                        }
                    });

                    logger.LogInformation($"Added delegation rule: {ruleConfig.QueueName} - {ruleConfig.Uri}");

                    return rule;
                })
                .ToArray();

            uint ruleIndex = uint.MaxValue;
            app.Run(context =>
            {
                int index = (int)(Interlocked.Increment(ref ruleIndex) % rules.Length);
                DelegationRule rule = rules[index];

                IHttpSysRequestDelegationFeature transferFeature = context.Features.Get<IHttpSysRequestDelegationFeature>();
                transferFeature.DelegateRequest(rule);

                logger.LogDebug($"Request '{context.Request.GetDisplayUrl()}' transfered to: {rule.QueueName} - {rule.UrlPrefix}");

                return Task.CompletedTask;
            });
        }

        private class DelegationConfig
        {
            public List<DelegationRuleConfig> Rules { get; set; }
        }

        private class DelegationRuleConfig
        {
            public string QueueName { get; set; }

            public string Uri { get; set; }

            public bool Enabled { get; set; }
        }
    }
}
