using UnityEngine.UIElements;

namespace Unity.Multiplayer.Tools.Editor
{
    internal class NetStatsMonitorSettingsMenu : VisualElement
    {
        internal NetStatsMonitorSettingsMenu(ProjectSettings settings)
        {
            var foldout = new Foldout
            {
                text = "Net Stats Monitor",
            };
            foldout.Q<Label>()?.AddToClassList(UssClassNames.k_SettingsSubMenuTitle);
            foldout.contentContainer.AddToClassList(UssClassNames.k_SettingsSubMenuContents);
            Add(foldout);
            {
                var includeInDevelop = new Toggle
                {
                    label = "Include in Develop Builds",
                    value = settings.NetStatsMonitorEnabledInDevelop,
                };
                includeInDevelop.AddToClassList(UssClassNames.k_Setting);
                includeInDevelop.RegisterValueChangedCallback(evt =>
                {
                    settings.NetStatsMonitorEnabledInDevelop = evt.newValue;
                });
                foldout.Add(includeInDevelop);
            }
            {
                var includeInRelease = new Toggle
                {
                    label = "Include in Release Builds",
                    value = settings.NetStatsMonitorEnabledInRelease,
                };
                includeInRelease.AddToClassList(UssClassNames.k_Setting);
                includeInRelease.RegisterValueChangedCallback(evt =>
                {
                    settings.NetStatsMonitorEnabledInRelease = evt.newValue;
                });
                foldout.Add(includeInRelease);
            }
        }

    }
}