using System.Collections.Generic;

namespace Unity.Multiplayer.Tools.NetStats
{
    interface IEventMetric : IMetric
    {
        bool WentOverLimit { get; }
        int Count { get; }
    }

    interface IEventMetric<TValue> : IEventMetric
    {
        IReadOnlyList<TValue> Values { get; }
    }
}