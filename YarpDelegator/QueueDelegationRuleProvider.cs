namespace YarpDelegator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.Logging;
    using Microsoft.ReverseProxy.Abstractions;
    using Microsoft.ReverseProxy.RuntimeModel;
    using Microsoft.ReverseProxy.Service;

    public sealed class QueueDelegationRuleProvider : IDisposable
    {
        private readonly IServerDelegationFeature delegationFeature;
        private readonly IProxyConfigProvider proxyConfigProvider;
        private readonly ILogger<QueueDelegationRuleProvider> logger;

        private bool disposed;
        private IDisposable proxyConfigSubscription;
        private IProxyConfig proxyConfig;
        private Dictionary<string, DelegationRule> rules;

        public QueueDelegationRuleProvider(
            IServerDelegationFeature delegationFeature,
            IProxyConfigProvider proxyConfigProvider,
            ILogger<QueueDelegationRuleProvider> logger)
        {
            this.delegationFeature = delegationFeature;
            this.proxyConfigProvider = proxyConfigProvider;
            this.logger = logger;
            this.rules = new Dictionary<string, DelegationRule>();

            this.UpdateRules();
        }

        public DelegationRule GetDelegationRule(DestinationInfo destination)
        {
            this.UpdateRules();
            return this.rules.GetValueOrDefault(destination.DestinationId);
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.proxyConfigSubscription?.Dispose();

                foreach (DelegationRule rule in this.rules.Values)
                {
                    rule.Dispose();
                }
            }
        }

        private static bool ShouldUseQueueDelegation(Destination destination)
        {
            return destination.Metadata != null
                && destination.Metadata.TryGetValue("Delegate", out string value)
                && bool.TryParse(value, out bool useQueueDelegation)
                && useQueueDelegation;
        }

        private void UpdateRules()
        {
            if (this.proxyConfig?.ChangeToken.HasChanged ?? true)
            {
                lock (this.rules)
                {
                    if (this.proxyConfig?.ChangeToken.HasChanged ?? true)
                    {
                        this.proxyConfigSubscription?.Dispose();

                        var newRules = new Dictionary<string, DelegationRule>();
                        var newProxyConfig = this.proxyConfigProvider.GetConfig();
                        var queueDelegationRules = newProxyConfig.Clusters
                            .SelectMany(c => c.Destinations)
                            .Where(d => ShouldUseQueueDelegation(d.Value));

                        foreach (KeyValuePair<string, Destination> destination in queueDelegationRules)
                        {
                            if (this.rules.Remove(destination.Key, out DelegationRule rule))
                            {
                                newRules.Add(destination.Key, rule);
                            }
                            else
                            {
                                try
                                {
                                    rule = this.delegationFeature.CreateDelegationRule(destination.Key, destination.Value.Address);
                                    newRules.Add(destination.Key, rule);

                                    this.logger.LogDebug($"Added delegation rule: {rule.QueueName} - {rule.UrlPrefix}");
                                }
                                catch (Exception ex)
                                {
                                    this.logger.LogError(ex, "Failed to create delegation rule");
                                }
                            }
                        }

                        foreach (DelegationRule rule in this.rules.Values)
                        {
                            this.logger.LogDebug($"Removing delegation rule: {rule.QueueName} - {rule.UrlPrefix}");
                            rule.Dispose();
                        }

                        this.proxyConfigSubscription = newProxyConfig.ChangeToken.RegisterChangeCallback(s => this.UpdateRules(), null);
                        this.rules = newRules;
                        this.proxyConfig = newProxyConfig;
                    }
                }
            }
        }
    }
}
