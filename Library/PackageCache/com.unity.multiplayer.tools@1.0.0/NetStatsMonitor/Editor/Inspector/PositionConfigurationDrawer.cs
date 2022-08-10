using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Tools.NetStatsMonitor.Editor
{
    [CustomPropertyDrawer(typeof(PositionConfiguration))]
    internal class PositionConfigurationDrawer : PropertyDrawer
    {
        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty configurationProp)
        {
            return new PositionConfigurationInspector(configurationProp);
        }
    }

    class PositionConfigurationInspector : VisualElement
    {
        static readonly string k_OverrideFieldName;
        static readonly string k_PositionLeftFieldName;
        static readonly string k_PositionTopFieldName;

        static PositionConfigurationInspector()
        {
            var fields = typeof(PositionConfiguration)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            string GetFieldName(string propertyName)
                => fields.First(field => field.Name.Contains(propertyName)).Name;

            k_OverrideFieldName = GetFieldName(nameof(PositionConfiguration.OverridePosition));

            k_PositionLeftFieldName = GetFieldName(nameof(PositionConfiguration.PositionLeftToRight));
            k_PositionTopFieldName = GetFieldName(nameof(PositionConfiguration.PositionTopToBottom));
        }

        readonly Foldout m_Foldout;
        VisualElement Content => m_Foldout.contentContainer;


        internal PositionConfigurationInspector(SerializedProperty property)
        {
            m_Foldout = new Foldout();
            m_Foldout.text = property.displayName;
            Add(m_Foldout);

            var (overrideProp, overrideField) = Content.AddFieldForProperty(property, k_OverrideFieldName);

            var (posLeftProp, posLeftField) = property.CreatePropertyAndFieldRelative(k_PositionLeftFieldName);
            var (posTopProp, posTopField) = property.CreatePropertyAndFieldRelative(k_PositionTopFieldName);

            void UpdateFieldVisibility()
            {
                var overrideEnabled = overrideProp.boolValue;

                Content.AddOrRemovePropertyField(posLeftProp, posLeftField, overrideEnabled);
                Content.AddOrRemovePropertyField(posTopProp, posTopField, overrideEnabled);
            }

            UpdateFieldVisibility();
            overrideField.RegisterValueChangeCallback(evt =>
            {
                UpdateFieldVisibility();
            });
        }
    }
}