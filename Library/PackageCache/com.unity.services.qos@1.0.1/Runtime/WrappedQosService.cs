using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Qos.Apis.QosDiscovery;
using Unity.Services.Qos.QosDiscovery;
using Unity.Services.Qos.Runner;

namespace Unity.Services.Qos
{
    class WrappedQosService : IQosService
    {
        const string ResultLatencyMetricName = "qos_result_latency_ms";
        const string ResultPacketLossMetricName = "qos_result_packet_loss";

        const string MetricServiceNameLabelName = "service_name";
        const string MetricServiceRegionLabelName = "service_region";
        const string MetricClientCountryLabelName = "client_country";
        const string MetricClientRegionLabelName = "client_region";
        const string MetricClientBestResultLabelName = "best_result";
        const string MetricClientBestResultLabelTrueValue = "true";

        IQosDiscoveryApiClient _qosDiscoveryApiClient;

        IQosRunner _qosRunner;

        IAccessToken _accessToken;

        IMetrics _metrics;

        internal WrappedQosService(IQosDiscoveryApiClient qosDiscoveryApiClient, IQosRunner qosRunner,
                                   IAccessToken accessToken, IMetrics metrics)
        {
            _qosDiscoveryApiClient = qosDiscoveryApiClient;
            _qosRunner = qosRunner;
            _accessToken = accessToken;
            _metrics = metrics;
        }

        /// <inheritdoc/>
        /// Implementation of GetSortedQosResultsAsync, where QoS results are sorted by packet latency and _then_ by
        /// packet loss. E.g. Regions for the following pairs of (Latency,PL): (1,0) (1,1) (1,3) (2,0) (2,1) they will
        /// be sorted in the following manner:
        /// Lat P/L
        /// 1   0
        /// 1   1
        /// 1   3
        /// 2   0
        /// 2   1
        ///
        /// Notice that the third entry (1,3) is put before the (2,1) because it has less latency, even if it has more
        /// packet loss.
        /// In case where no QoS servers can be found, no QoS is performed and an empty list is returned.
        public async Task<IList<IQosResult>> GetSortedQosResultsAsync(string service, IList<string> regions)
        {
            return (await GetSortedInternalQosResultsAsync(service, regions))
                .Select(MapToPublicQosResult)
                .ToList();
        }

        internal async Task<IList<Internal.QosResult>> GetSortedInternalQosResultsAsync(string service, IList<string> regions)
        {
#if UGS_QOS_SUPPORTED
            if (string.IsNullOrEmpty(_accessToken.AccessToken))
            {
                throw new Exception("Access token not available, please sign in with the Authentication Service.");
            }

            // Code-generated API client requires a (concrete) List but in our interface we only require an IList interface
            // Try to cast IList to List
            var regionsList = regions as List<string>;
            // Else create a List from the IList
            if (regionsList == null && regions != null)
            {
                regionsList = new List<string>(regions);
            }
            var httpResp = await _qosDiscoveryApiClient.GetServersAsync(
                new GetServersRequest(regionsList, service));
            var servers = httpResp.Result.Data.Servers;

            // empty response, return no results
            if (!servers.Any())
            {
                return new List<Internal.QosResult>();
            }

            var qosResults = await _qosRunner.MeasureQosAsync(servers);
            var sortedResults = SortResults(qosResults);

            SendResultMetrics(sortedResults, service, httpResp);
            return sortedResults;
#else
            throw new UnsupportedEditorVersionException(
                "QoS SDK does not support this version of Unity, please upgrade to 2020.3.34f1+, 2021.3.2f1+, 2022.1.0f1+, 2022.2.0a10+, or newer.");
#endif
        }

        List<Internal.QosResult> SortResults(IList<Internal.QosResult> results)
        {
            return results
                .OrderBy(q => q.AverageLatencyMs)
                .ThenBy(q => q.PacketLossPercent)
                .ToList();
        }

        void SendResultMetrics(IList<Internal.QosResult> sortedResults, string service, Response discoveryResponse)
        {
            // don't send metrics in the rare case that there are no results
            if (sortedResults.Count == 0)
            {
                return;
            }

            // we add a special tag to the "best" result (first in the sorted list) so we can query only on
            // "best" results when needed
            var bestResult = true;
            foreach (var result in sortedResults)
            {
                IDictionary<string, string> metricTags = new Dictionary<string, string>();

                metricTags.Add(MetricServiceNameLabelName, service);
                metricTags.Add(MetricServiceRegionLabelName, result.Region);
                if (discoveryResponse.Headers.TryGetValue("X-Client-Country", out var clientCountry))
                {
                    metricTags.Add(MetricClientCountryLabelName, clientCountry);
                }
                if (discoveryResponse.Headers.TryGetValue("X-Client-Region", out var clientRegion))
                {
                    metricTags.Add(MetricClientRegionLabelName, clientRegion);
                }

                // only add the "best_result" tag when processing the first (best) result
                if (bestResult)
                {
                    metricTags.Add(MetricClientBestResultLabelName, MetricClientBestResultLabelTrueValue);
                    bestResult = false;
                }

                _metrics.SendHistogramMetric(ResultLatencyMetricName, result.AverageLatencyMs, metricTags);
                _metrics.SendHistogramMetric(ResultPacketLossMetricName, result.PacketLossPercent, metricTags);
            }
        }

        IQosResult MapToPublicQosResult(Internal.QosResult internalQosResult)
        {
            return new QosResult(internalQosResult.Region, internalQosResult.AverageLatencyMs,
                internalQosResult.PacketLossPercent);
        }
    }

    class QosResult : IQosResult
    {
        public QosResult(string region, int averageLatencyMs, float packetLossPercent)
        {
            Region = region;
            AverageLatencyMs = averageLatencyMs;
            PacketLossPercent = packetLossPercent;
        }

        public string Region { get; }
        public int AverageLatencyMs { get; }
        public float PacketLossPercent { get; }
    }

    class UnsupportedEditorVersionException : Exception
    {
        public UnsupportedEditorVersionException()
        {
        }

        public UnsupportedEditorVersionException(string message) : base(message)
        {
        }
    }
}
