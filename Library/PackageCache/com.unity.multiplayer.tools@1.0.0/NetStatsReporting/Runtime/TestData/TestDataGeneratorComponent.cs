#if UNITY_EDITOR

using UnityEngine;

namespace Unity.Multiplayer.Tools.NetStatsReporting
{
    internal enum TestDataGeneratorType
    {
        Client,
        Server,
    }

#if UNITY_MP_TOOLS_DEV
    [AddComponentMenu("Multiplayer Tools/" + "Test Data Generator", 1000)]
#else
    [AddComponentMenu("")] // Prevent the component from being instantiated in editor
#endif
    public class TestDataGeneratorComponent : MonoBehaviour
    {
        [SerializeField] internal TestDataGeneratorType m_type;

        TestDataDispatcher m_Dispatcher;

        void OnValidate()
        {
            m_Dispatcher ??= new TestDataDispatcher();
        }

        void Start()
        {
            m_Dispatcher ??= new TestDataDispatcher();
        }

        internal void Update()
        {
            switch (m_type)
            {
                case TestDataGeneratorType.Client:
                {
                    m_Dispatcher.DispatchClientFrame();
                    break;
                }
                case TestDataGeneratorType.Server:
                {
                    m_Dispatcher.DispatchServerFrame();
                    break;
                }
            }
        }
    }
}

#endif