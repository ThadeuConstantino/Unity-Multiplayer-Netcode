using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Tools.NetStats
{
    class MetricDispatcher : IMetricDispatcher
    {
        internal const string k_ThrottlingWarning = "Some metrics in the collection went over the configured values limit. Some values were ignored.";

        readonly MetricCollection m_Collection;
        readonly IReadOnlyList<IResettable> m_Resettables;
        readonly IReadOnlyList<IEventMetric> m_EventMetrics;

        readonly IList<IMetricObserver> m_Observers = new List<IMetricObserver>();

        internal MetricDispatcher(
            MetricCollection collection,
            IReadOnlyList<IResettable> resettables,
            IReadOnlyList<IEventMetric> eventMetrics)
        {
            m_Collection = collection;
            m_Resettables = resettables;
            m_EventMetrics = eventMetrics;
        }

        public void RegisterObserver(IMetricObserver observer)
        {
            m_Observers.Add(observer);
        }

        public void SetConnectionId(ulong connectionId)
        {
            m_Collection.ConnectionId = connectionId;
        }

        public void Dispatch()
        {
            var wentOverLimit = false;
            for (var i = 0; i < m_EventMetrics.Count; i++)
            {
                if (m_EventMetrics[i].WentOverLimit)
                {
                    wentOverLimit = true;
                    break;
                }
            }

            if (wentOverLimit)
            {
                Debug.LogWarning(k_ThrottlingWarning);
            }

            for (var i = 0; i < m_Observers.Count; i++)
            {
                var snapshotObserver = m_Observers[i];
                snapshotObserver.Observe(m_Collection);
            }

            for (var i = 0; i < m_Resettables.Count; i++)
            {
                var resettable = m_Resettables[i];
                if (resettable.ShouldResetOnDispatch)
                {
                    resettable.Reset();
                }
            }
        }
    }
}