namespace Unity.Multiplayer.Tools.Editor
{
    /// Although this could be a scriptable object in future that stores its own state,
    /// for now all the state that is needed is stored in the existing project settings
    /// in the script compilation define symbols
    class ProjectSettings
    {
        public bool NetStatsMonitorEnabledInDevelop
        {
            // Value is negated here because the define symbol itself is phrased in the negative.
            // The define symbol is phrased in the negative so that the absence of a define symbol
            // provides the correct default: that the RNSM is enabled in develop.
            get => !NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(
                NetStatsMonitorBuildSymbol.DisableInDevelop);
            set => NetStatsMonitorBuildSettings.SetSymbolForAllBuildTargets(
                NetStatsMonitorBuildSymbol.DisableInDevelop, !value);
        }

        public bool NetStatsMonitorEnabledInRelease
        {
            get => NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(
                NetStatsMonitorBuildSymbol.EnableInRelease);
            set => NetStatsMonitorBuildSettings.SetSymbolForAllBuildTargets(
                NetStatsMonitorBuildSymbol.EnableInRelease, value);
        }
    }
}