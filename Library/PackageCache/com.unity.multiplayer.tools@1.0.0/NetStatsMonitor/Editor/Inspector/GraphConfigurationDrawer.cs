using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Tools.NetStatsMonitor.Editor
{
    [CustomPropertyDrawer(typeof(GraphConfiguration))]
    internal class GraphConfigurationDrawer : PropertyDrawer
    {
        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty configurationProp)
        {
            return new GraphConfigurationInspector(configurationProp);
        }
    }

    class GraphConfigurationInspector : VisualElement
    {
        static readonly string k_SampleCountFieldName;
        static readonly string k_XAxisTypeFieldName;
        static readonly string k_VariableColorsFieldName;
        static readonly string k_LineGraphConfigurationFieldName;

        static GraphConfigurationInspector()
        {
            var fields = typeof(GraphConfiguration)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            string GetFieldName(string propertyName)
                => fields.First(field => field.Name.Contains(propertyName)).Name;

            k_SampleCountFieldName = GetFieldName(nameof(GraphConfiguration.SampleCount));
            k_XAxisTypeFieldName = GetFieldName(nameof(GraphConfiguration.XAxisType));
            k_VariableColorsFieldName = GetFieldName(nameof(GraphConfiguration.VariableColors));
            k_LineGraphConfigurationFieldName = GetFieldName(nameof(GraphConfiguration.LineGraphConfiguration));
        }

        readonly Foldout m_Foldout;
        VisualElement Content => m_Foldout.contentContainer;

        readonly SerializedProperty m_LineGraphProp;
        readonly PropertyField m_LineGraphField;

        internal GraphConfigurationInspector(
            SerializedProperty configurationProp,
            PropertyField typeField = null)
        {
            m_Foldout = new Foldout();
            m_Foldout.text = configurationProp.displayName;
            Add(m_Foldout);


            var help = new HelpBox(ConfigurationLimits.k_GraphMaxSampleWarningMessage, HelpBoxMessageType.Warning);
            Content.Add(help);

            var (samplesProperty, samplesField) = Content.AddFieldForProperty(configurationProp, k_SampleCountFieldName);
            samplesField.RegisterValueChangeCallback(_ =>
            {
                help.style.display = GetWarningBoxDisplayStyle(samplesProperty.intValue);
            });
            help.style.display = GetWarningBoxDisplayStyle(samplesProperty.intValue);

            Content.AddFieldForProperty(configurationProp, k_XAxisTypeFieldName);
            Content.AddFieldForProperty(configurationProp, k_VariableColorsFieldName);

            (m_LineGraphProp, m_LineGraphField) =
                configurationProp.CreatePropertyAndFieldRelative(k_LineGraphConfigurationFieldName);

            var displayElementType = (DisplayElementType)configurationProp
                .GetParent()
                .FindPropertyRelative(DisplayElementConfigurationInspector.k_TypeFieldName)
                .enumValueIndex;
            OnTypeChanged(displayElementType);
        }

        public void OnTypeChanged(DisplayElementType type)
        {
            var lineGraphOptionsVisible = type == DisplayElementType.LineGraph;
            Content.AddOrRemovePropertyField(m_LineGraphProp, m_LineGraphField, lineGraphOptionsVisible);
        }

        DisplayStyle GetWarningBoxDisplayStyle(int value)
        {
            return ConfigurationLimits.k_GraphSampleMax < value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}