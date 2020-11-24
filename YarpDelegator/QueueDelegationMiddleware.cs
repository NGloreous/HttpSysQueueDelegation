namespace YarpDelegator
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.ReverseProxy.Middleware;
    using Microsoft.ReverseProxy.RuntimeModel;
    using Microsoft.ReverseProxy.Service;

    public class QueueDelegationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<QueueDelegationMiddleware> logger;
        private readonly QueueDelegationRuleProvider delegationRuleProvider;

        public QueueDelegationMiddleware(
            RequestDelegate next,
            ILogger<QueueDelegationMiddleware> logger,
            QueueDelegationRuleProvider delegationRuleProvider)
        {
            this.next = next;
            this.logger = logger;
            this.delegationRuleProvider = delegationRuleProvider;
        }

        public Task Invoke(HttpContext context)
        {
            var proxyFeature = context.Features.Get<IReverseProxyFeature>();

            // By always selecting the first destination, we prevent the ability to have multiple destinations
            // but Yarp's load balancing decision happens after this middleware is invoked :/
            DestinationInfo destination = proxyFeature.AvailableDestinations[0];

            DelegationRule delegationRule = this.delegationRuleProvider.GetDelegationRule(destination);
            if (delegationRule != null)
            {
                var feature = context.Features.Get<IHttpSysRequestDelegationFeature>();
                feature.DelegateRequest(delegationRule);

                this.logger.LogDebug($"Request '{context.Request.GetDisplayUrl()}' transfered to: {delegationRule.QueueName} - {delegationRule.UrlPrefix}");

                return Task.CompletedTask;
            }

            this.logger.LogDebug($"Proxy request '{context.Request.GetDisplayUrl()}'");

            return this.next(context);
        }
    }

    public static class QueueDelegationMiddlewareExtensions
    {
        public static IApplicationBuilder MapReverseProxyWithQueueDelegation(this IApplicationBuilder app)
        {
            IServerDelegationFeature delegationFeature = app.ServerFeatures.Get<IServerDelegationFeature>();
            if (delegationFeature == null)
            {
                throw new NotSupportedException($"The {nameof(IServerDelegationFeature)} is not supported by the current OS.");
            }

            var delegationRuleProvider = new QueueDelegationRuleProvider(
                delegationFeature,
                app.ApplicationServices.GetRequiredService<IProxyConfigProvider>(),
                app.ApplicationServices.GetRequiredService<ILogger<QueueDelegationRuleProvider>>());

            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopped.Register(() => delegationRuleProvider.Dispose());

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapReverseProxy(proxyPipeline =>
                {
                    proxyPipeline.UseMiddleware<QueueDelegationMiddleware>(delegationRuleProvider);
                });
            });

            return app;
        }
    }
}
