using System.Linq;
using Unity.Multiplayer.Tools.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Multiplayer.Tools.NetStatsMonitor.Implementation
{
    class NetStatsMonitorBuildLogEntry : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;

            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var symbols);

            var isDevelopBuild = (report.summary.options & BuildOptions.Development) != 0;
            var isReleaseBuild = !isDevelopBuild;

            var enabledInDevelop = !symbols.Contains(NetStatsMonitorBuildSymbolStrings.k_DisableInDevelop);
            var enabledInRelease =  symbols.Contains(NetStatsMonitorBuildSymbolStrings.k_EnableInRelease);
            var overrideEnabled  =  symbols.Contains(NetStatsMonitorBuildSymbolStrings.k_OverrideEnabled);

            // This logic needs to match the preprocessor logic for the inclusion of the RNSM
            // implementation in the RNSM implementation source files
            var rnsmImplementationEnabled = overrideEnabled ||
                                            (isDevelopBuild && enabledInDevelop) ||
                                            (isReleaseBuild && enabledInRelease);

            var buildType = isDevelopBuild ? "development" : "release";
            var enabled = rnsmImplementationEnabled ? "enabled" : "disabled";

            Debug.Log($"Runtime Net Stats Monitor (RNSM) implementation {enabled} in {buildType} build targeting {target}");
        }
    }
}
