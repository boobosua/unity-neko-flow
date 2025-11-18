#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace NekoFlow.FSM
{
    [CustomEditor(typeof(FlowBehaviour), true)]
    public class FlowBehaviourInspector :
#if ODIN_INSPECTOR
        OdinEditor          // Use Odin when available
#else
        Editor              // Fallback to normal Unity inspector
#endif
    {
        private IState _lastState;
        private float _stateStartTime;

        public override void OnInspectorGUI()
        {
#if ODIN_INSPECTOR
            // --- ODIN VERSION ---
            // Odin handles the serializedObject life cycle internally,
            // so don't wrap base.OnInspectorGUI() in Update/Apply.

            // Optional: if you DON'T want the default script field Odin shows,
            // you can comment this out.
            // DrawScriptField();

            // Your runtime debug box (same for both Odin / non-Odin)
            DrawRuntimeBox();

            EditorGUILayout.Space();

            // Let Odin draw all fields with attributes, groups, etc.
            base.OnInspectorGUI();

            if (Application.isPlaying)
                Repaint();

#else
            // --- NON-ODIN VERSION ---
            serializedObject.Update();

            DrawScriptField();
            DrawRuntimeBox();

            EditorGUILayout.Space();

            // Old behaviour: manually draw all other properties
            DrawDerivedClassProperties();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                Repaint();
#endif
        }

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
                    EditorGUILayout.ObjectField("Context",
                        flowBehaviour as Component, typeof(Component), true);

                    // Current State name (text only; not a UnityEngine.Object)
                    string stateName = currentState != null
                        ? System.Text.RegularExpressions.Regex.Replace(
                            currentState.GetType().Name, "(\\B[A-Z])", " $1")
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
                            EditorGUILayout.LabelField(
                                "Available Transitions", EditorStyles.boldLabel);

                            for (int i = 0; i < potentialStates.Count; i++)
                            {
                                var to = potentialStates[i];
                                string toName = to != null
                                    ? System.Text.RegularExpressions.Regex.Replace(
                                        to.GetType().Name, "(\\B[A-Z])", " $1")
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

#if !ODIN_INSPECTOR
        // Only used in non-Odin mode
        private void DrawDerivedClassProperties()
        {
            // Draw all other properties except FlowBehaviour base class properties
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Skip script reference
                if (iterator.propertyPath == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }
#endif
    }
}
#endif
