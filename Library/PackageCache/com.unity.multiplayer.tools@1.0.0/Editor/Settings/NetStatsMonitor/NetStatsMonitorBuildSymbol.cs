using System;

namespace Unity.Multiplayer.Tools.Editor
{
    internal enum NetStatsMonitorBuildSymbol
    {
        None,

        /// This is phrased as a negative so that the default state (not defined) matches the
        /// desired default behaviour (inclusion in develop builds)
        DisableInDevelop,

        EnableInRelease,

        /// By adding this scripting define symbol users can override our build logic and
        /// forcibly enable the RNSM in both development and release. This option takes
        /// precedence over DisableInDevelop
        OverrideEnabled,
    }

    internal static class NetStatsMonitorBuildSymbolStrings
    {
        public const string k_DisableInDevelop = "UNITY_MP_TOOLS_NET_STATS_MONITOR_DISABLED_IN_DEVELOP";
        public const string k_EnableInRelease  = "UNITY_MP_TOOLS_NET_STATS_MONITOR_ENABLED_IN_RELEASE";
        public const string k_OverrideEnabled  = "UNITY_MP_TOOLS_NET_STATS_MONITOR_IMPLEMENTATION_ENABLED";
    }

    internal static class NetStatsMonitorBuildSymbolExtensions
    {
        public static string GetBuildSymbolString(this NetStatsMonitorBuildSymbol symbol)
        {
            switch (symbol)
            {
                case NetStatsMonitorBuildSymbol.None:
                    return "";
                case NetStatsMonitorBuildSymbol.DisableInDevelop:
                    return NetStatsMonitorBuildSymbolStrings.k_DisableInDevelop;
                case NetStatsMonitorBuildSymbol.EnableInRelease:
                    return NetStatsMonitorBuildSymbolStrings.k_EnableInRelease;
                case NetStatsMonitorBuildSymbol.OverrideEnabled:
                    return NetStatsMonitorBuildSymbolStrings.k_OverrideEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
            }
        }
    }
}