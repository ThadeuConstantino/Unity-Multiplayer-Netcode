using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Multiplayer.Tools.Editor;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.Multiplayer.Tools.Tests.Editor
{
    [TestFixture]
    [Explicit("Running these tests modifies compile flags and can trigger a recompile, " +
              "so should only be run when needed. " +
              "They are also pretty simple so don't need to be run at a all times.")]
    public class NetStatsMonitorBuildSettingsTests
    {
        readonly IReadOnlyList<NetStatsMonitorBuildSymbol> k_AllBuildSymbols = new List<NetStatsMonitorBuildSymbol>
        {
            NetStatsMonitorBuildSymbol.DisableInDevelop,
            NetStatsMonitorBuildSymbol.EnableInRelease,
        };

        readonly Dictionary<NamedBuildTarget, string[]> m_BuildSettingsPerTarget
            = new Dictionary<NamedBuildTarget, string[]>();

        [OneTimeSetUp]
        public void SaveScriptingDefineSymbols()
        {
            m_BuildSettingsPerTarget.Clear();
            foreach (var target in NetStatsMonitorBuildSettings.k_AllBuildTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols);
                m_BuildSettingsPerTarget[target] = symbols;
            }
        }

        [OneTimeTearDown]
        public void RestoreScriptingDefineSymbols()
        {
            foreach (var (target, symbols) in m_BuildSettingsPerTarget)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, symbols);
            }
        }

        [Test]
        public void When_NetsStatsMonitorSymbolIsEnabled_ItIsEnabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetStatsMonitorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                Assert.IsTrue(NetStatsMonitorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsTrue(NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetSatsMonitorSymbolIsDisabled_ItIsDisabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetStatsMonitorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);
                Assert.IsFalse(NetStatsMonitorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsFalse(NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetSatsMonitorIsEnabledThenDisabled_ItIsDisabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetStatsMonitorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                NetStatsMonitorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);

                Assert.IsFalse(NetStatsMonitorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsFalse(NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetSatsMonitorIsEnabledThenDisabledThenEnabled_ItIsEnabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetStatsMonitorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                NetStatsMonitorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);
                NetStatsMonitorBuildSettings.AddSymbolToAllBuildTargets(symbol);

                Assert.IsTrue(NetStatsMonitorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsTrue(NetStatsMonitorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }
    }
}
