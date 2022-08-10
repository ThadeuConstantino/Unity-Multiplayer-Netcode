// RNSM Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NET_STATS_MONITOR_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED

using System;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Multiplayer.Tools.NetStatsMonitor.Implementation
{
    internal class RnsmMetricObserver : IMetricObserver
    {
        private RnsmMetricObserver(){}
        public static RnsmMetricObserver Instance { get; } = new RnsmMetricObserver();

        public event Action<MetricCollection> OnMetricsReceived;
        public void Observe(MetricCollection collection)
        {
            OnMetricsReceived?.Invoke(collection);
        }
    }
}
#endif