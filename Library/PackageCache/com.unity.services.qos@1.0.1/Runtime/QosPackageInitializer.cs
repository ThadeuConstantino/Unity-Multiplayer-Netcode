using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Qos.Apis.QosDiscovery;

using Unity.Services.Qos.Http;
using Unity.Services.Qos.Internal;
using Unity.Services.Qos.Runner;

namespace Unity.Services.Qos
{
    class QosPackageInitializer : IInitializablePackage
    {
        const string PackageName = "com.unity.services.qos";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            // Pass an instance of this class to Core
            CoreRegistry.Instance.RegisterPackage(new QosPackageInitializer())
                .DependsOn<IAccessToken>()
                .DependsOn<IMetricsFactory>()
                .ProvidesComponent<IQosResults>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            var httpClient = new HttpClient();

            var accessTokenQosDiscovery = registry.GetServiceComponent<IAccessToken>();
            var metricsFactory = registry.GetServiceComponent<IMetricsFactory>();
            var metrics = metricsFactory.Create(PackageName);

            // Set up internal QoS Discovery API client & config
            QosDiscoveryService.Instance = new InternalQosDiscoveryService(httpClient, accessTokenQosDiscovery);

            // Set up public QoS interface
            var wrappedQosService = new WrappedQosService(QosDiscoveryService.Instance.QosDiscoveryApi,
                new BaselibQosRunner(), accessTokenQosDiscovery, metrics);
            QosService.Instance = wrappedQosService;
            registry.RegisterServiceComponent<IQosResults>(new QosResults(wrappedQosService));

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// InternalQosDiscoveryService
    /// </summary>
    class InternalQosDiscoveryService : IQosDiscoveryService
    {
#if AUTHENTICATION_TESTING_STAGING_UAS
        const string QosDiscoveryHost = "https://qos-discovery-stg.services.api.unity.com";
#else
        const string QosDiscoveryHost = "https://qos-discovery.services.api.unity.com";
#endif
        const int RequestTimeout = 10;
        const int NumRetries = 4;

        /// <summary>
        /// Constructor for InternalQosDiscoveryService
        /// </summary>
        /// <param name="httpClient">The HttpClient for InternalQosDiscoveryService.</param>
        /// <param name="accessToken">The Authentication token for the service.</param>
        internal InternalQosDiscoveryService(HttpClient httpClient, IAccessToken accessToken = null)
        {
            Configuration = new Configuration(QosDiscoveryHost, RequestTimeout, NumRetries, null);

            QosDiscoveryApi = new QosDiscoveryApiClient(httpClient, accessToken, Configuration);
        }

        public IQosDiscoveryApiClient QosDiscoveryApi { get; set; }

        /// <summary> Configuration properties for the service.</summary>
        public Configuration Configuration { get; set; }
    }
}
