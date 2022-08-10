// RNSM Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NET_STATS_MONITOR_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_2021_2_OR_NEWER && UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED
#define UNITY_MP_TOOLS_RNSM_IMPLEMENTATION_INCLUDED
#else
#undef UNITY_MP_TOOLS_RNSM_IMPLEMENTATION_INCLUDED
#endif

using Unity.Multiplayer.Tools.NetStats;
#if UNITY_MP_TOOLS_RNSM_IMPLEMENTATION_INCLUDED
using Unity.Multiplayer.Tools.NetStatsMonitor.Implementation;
#endif

namespace Unity.Multiplayer.Tools
{
    internal class NoOpMetricObserver : IMetricObserver
    {
        /// No sense in having more than one instance if none of them do anything or have any data
        public static NoOpMetricObserver Instance { get; } = new NoOpMetricObserver();
        private NoOpMetricObserver() {}
        public void Observe(MetricCollection collection) {}
    }

    /// This can't be included in the RNSM assembly, as that entire assembly is excluded in
    /// Unity versions less than 2021.2
    internal static class RnsmMetricObserverFactory
    {
        public static IMetricObserver Construct()
        {
#if UNITY_MP_TOOLS_RNSM_IMPLEMENTATION_INCLUDED
            return RnsmMetricObserver.Instance;
#else
            return NoOpMetricObserver.Instance;
#endif
        }
    }
}
