#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoFlow.FSM
{
    [CustomEditor(typeof(FlowBehaviour), true)]
    public class FlowBehaviourInspector : Editor
    {
        private IState _lastState;
        private float _stateStartTime;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script field
            DrawScriptField();

            // Runtime box (visible in Edit & Play)
            DrawRuntimeBox();

            EditorGUILayout.Space();

            // Derived class properties
            DrawDerivedClassProperties();

            serializedObject.ApplyModifiedProperties();

            // Live updates in Play Mode
            if (Application.isPlaying)
                Repaint();
        }

        // (Removed obsolete DrawFlowBehaviourSection)

        private void DrawRuntimeBox()
        {
            var flowBehaviour = (FlowBehaviour)target;
            var currentState = flowBehaviour.GetCurrentState();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    // Context is the component itself
                    EditorGUILayout.ObjectField("Context", flowBehaviour as Component, typeof(Component), true);

                    // Current State name (text only; not a UnityEngine.Object)
                    string stateName = currentState != null
                        ? System.Text.RegularExpressions.Regex.Replace(currentState.GetType().Name, "(\\B[A-Z])", " $1")
                        : "None";
                    EditorGUILayout.TextField("Current State", stateName);

                    // Time In State as integer seconds (0 when not applicable)
                    int seconds = GetTimeInCurrentStateSeconds(currentState);
                    EditorGUILayout.TextField("Time In State", $"{seconds}s");
                }

                // In Play Mode, list potential transitions with Jump buttons
                if (Application.isPlaying)
                {
                    var flowMachine = flowBehaviour.GetFlowMachine();
                    if (flowMachine != null)
                    {
                        var potentialStates = flowMachine.GetPotentialTransitions();
                        if (potentialStates != null && potentialStates.Count > 0)
                        {
                            EditorGUILayout.Space(4);
                            EditorGUILayout.LabelField("Available Transitions", EditorStyles.boldLabel);

                            for (int i = 0; i < potentialStates.Count; i++)
                            {
                                var to = potentialStates[i];
                                string toName = to != null
                                    ? System.Text.RegularExpressions.Regex.Replace(to.GetType().Name, "(\\B[A-Z])", " $1")
                                    : "None";

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    using (new EditorGUI.DisabledScope(true))
                                    {
                                        EditorGUILayout.TextField(toName);
                                    }
                                    using (new EditorGUI.DisabledScope(to == null))
                                    {
                                        if (GUILayout.Button("Jump", GUILayout.Width(60)))
                                        {
                                            flowMachine.SetState(to);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private int GetTimeInCurrentStateSeconds(IState currentState)
        {
            if (!Application.isPlaying || currentState == null)
                return 0;

            // Track state changes
            if (_lastState != currentState)
            {
                _lastState = currentState;
                _stateStartTime = (float)EditorApplication.timeSinceStartup;
            }

            float timeInState = (float)EditorApplication.timeSinceStartup - _stateStartTime;
            return Mathf.FloorToInt(timeInState);
        }

        // Removed old array-style transitions list in favor of the Runtime box above

        private void DrawScriptField()
        {
            SerializedProperty script = serializedObject.FindProperty("m_Script");
            if (script != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(script);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawDerivedClassProperties()
        {
            // Draw all other properties except FlowBehaviour base class properties
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Skip script reference and FlowBehaviour properties
                if (iterator.propertyPath == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }
}
#endif