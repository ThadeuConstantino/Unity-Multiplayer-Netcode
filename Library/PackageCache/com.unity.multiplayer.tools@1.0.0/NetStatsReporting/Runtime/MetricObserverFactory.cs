using Unity.Multiplayer.Tools.NetStats;
using Unity.Multiplayer.Tools.NetworkProfiler.Runtime;

namespace Unity.Multiplayer.Tools
{
    static class MetricObserverFactory
    {
        internal static IMetricObserver Construct() => new MetricObserver();
    }

    class MetricObserver : IMetricObserver
    {
        private IMetricObserver m_profilerMetricObserver = ProfilerMetricObserverFactory.Construct();
        private IMetricObserver m_rnsmMetricObserver = RnsmMetricObserverFactory.Construct();
        public void Observe(MetricCollection collection)
        {
            m_profilerMetricObserver.Observe(collection);
            m_rnsmMetricObserver.Observe(collection);
        }
    }
}
