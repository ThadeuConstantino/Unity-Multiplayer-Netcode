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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

using Unity.Multiplayer.Tools.Common;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Multiplayer.Tools.NetStatsMonitor.Implementation
{
    /// A class for storing multiple RNSM stat histories over multiple frames,
    /// to facilitate moving averages and graphs over time
    class MultiStatHistory
    {
        [NotNull]
        readonly Dictionary<MetricId, StatHistory> m_Data = new();

        [NotNull]
        public IReadOnlyDictionary<MetricId, StatHistory> Data => m_Data;

        /// Record sample time-stamps for graphs and counters, so that
        /// we can gracefully handle irregularly-timed metric dispatches.
        [NotNull]
        public RingBuffer<double> TimeStamps { get; } = new(0);

        public void Clear()
        {
            m_Data.Clear();
        }

        public void Collect(StatsAccumulator statsAccumulator, double time)
        {
            TimeStamps.PushBack(time);
            foreach (var (metricId, history) in m_Data)
            {
                var collectedValue = statsAccumulator.Collect(metricId);
                history.Update(metricId, collectedValue, time);
            }
            statsAccumulator.LastCollectionTime = time;
        }

        /// Updates the requirements, while preserving all existing data that is still required.
        internal void UpdateRequirements(MultiStatHistoryRequirements requirements)
        {
            var allStatRequirements = requirements.Data;

            // Remove existing data that is no longer required
            var statsToRemove = m_Data.Keys
                .Where(metricId => !allStatRequirements.ContainsKey(metricId))
                .ToList();
            foreach (var statName in statsToRemove)
            {
                m_Data.Remove(statName);
            }

            // Add and update stats according to the requirements
            var maxSampleCount = 0;
            foreach (var (statName, statRequirements) in allStatRequirements)
            {
                maxSampleCount = Math.Max(maxSampleCount, statRequirements.SampleCount);
                if (m_Data.ContainsKey(statName))
                {
                    m_Data[statName].UpdateRequirements(statRequirements);
                }
                else
                {
                    m_Data[statName] = new StatHistory(statRequirements);
                }
            }

            // Include a surplus of one timestamp so that the duration of the oldest sample
            // can still be computed
            const int k_TimeStampSurplus = 1;
            TimeStamps.Capacity = maxSampleCount + k_TimeStampSurplus;
            for (int i = 0; i < k_TimeStampSurplus; ++i)
            {
                TimeStamps.PushBack(0);
            }
        }

        internal double? GetSimpleMovingAverageForCounter(MetricId metricId, int maxSampleCount, double time)
        {
            if (!Data.TryGetValue(metricId, out StatHistory statHistory))
            {
                return null;
            }

            var sampleCount = Math.Min(maxSampleCount, statHistory.RecentValues.Length);
            if (sampleCount <= 1)
            {
                return null;
            }
            var sampleSum = statHistory.RecentValues.SumLastN(sampleCount);

            var startTime = TimeStamps[^(sampleCount - 1)];

            var timeSpan = time - startTime;

            var rate = sampleSum / timeSpan;
            return rate;
        }

        public double? GetSimpleMovingAverageForGauge(MetricId metricId, int maxSampleCount)
        {
            if (!Data.TryGetValue(metricId, out StatHistory statHistory))
            {
                return null;
            }

            var sampleCount = Math.Min(maxSampleCount, statHistory.RecentValues.Length);
            if (sampleCount <= 0)
            {
                return null;
            }
            var sampleSum = statHistory.RecentValues.SumLastN(sampleCount);

            var average = sampleSum / sampleCount;
            return average;
        }

        public double? GetSimpleMovingAverage(MetricId metricId, int maxSampleCount, double time)
        {
            var metricKind = metricId.MetricKind;
            switch (metricKind)
            {
                case MetricKind.Counter:
                    return GetSimpleMovingAverageForCounter(metricId, maxSampleCount, time);
                case MetricKind.Gauge:
                    return GetSimpleMovingAverageForGauge(metricId, maxSampleCount);
                default:
                    throw new NotSupportedException($"Unhandled {nameof(MetricKind)} {metricKind}");
            }
        }

        /// The length of the history in seconds
        internal double TimeSpanOfLastNSamples(int sampleCount)
        {
            var validSampleCount = Math.Min(sampleCount, TimeStamps.Length);
            if (validSampleCount <= 1)
            {
                return 0;
            }
            var firstTimeStamp = TimeStamps[^(validSampleCount - 1)];
            var lastTimeStamp = TimeStamps[^1];
            return lastTimeStamp - firstTimeStamp;
        }

#if UNITY_INCLUDE_TESTS
        public static MultiStatHistory CreateMockMultiStatHistoryForTest(
            MetricId metricId,
            StatHistory statHistory,
            double[] timeStamps = null)
        {
            var history = new MultiStatHistory();
            history.m_Data[metricId] = statHistory;

            if (timeStamps != null)
            {
                var timeStampCount = timeStamps.Length;
                history.TimeStamps.Capacity = timeStampCount;
                foreach (var timeStamp in timeStamps)
                {
                    history.TimeStamps.PushBack(timeStamp);
                }
            }
            else
            {
                var sampleCapacity = statHistory.RecentValues.Capacity;
                var sampleCount = statHistory.RecentValues.Length;
                history.TimeStamps.Capacity = sampleCapacity;
                for (var i = 0; i < sampleCount; ++i)
                {
                    history.TimeStamps.PushBack(i);
                }
            }
            return history;
        }
#endif
    }
}
#endif