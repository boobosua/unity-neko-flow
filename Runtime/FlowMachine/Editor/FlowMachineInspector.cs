#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NekoFlow
{
    [CustomEditor(typeof(FlowBehaviour), true)]
    public class FlowBehaviourInspector : Editor
    {
        private SerializedProperty _enableFixedTick;
        private SerializedProperty _enableLateTick;
        private IState _lastState;
        private float _stateStartTime;

        private void OnEnable()
        {
            _enableFixedTick = serializedObject.FindProperty("_enableFixedTick");
            _enableLateTick = serializedObject.FindProperty("_enableLateTick");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw script field first (like normal components)
            DrawScriptField();

            // Draw FlowBehaviour base properties
            DrawFlowBehaviourSection();

            // Draw Flow Machine debug info if playing
            if (Application.isPlaying)
            {
                DrawFlowMachineDebug();
            }

            EditorGUILayout.Space();

            // Draw derived class properties
            DrawDerivedClassProperties();

            serializedObject.ApplyModifiedProperties();

            // Force repaint during play mode for real-time updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawFlowBehaviourSection()
        {
            EditorGUILayout.PropertyField(_enableFixedTick, new GUIContent("Enable Fixed Tick"));
            EditorGUILayout.PropertyField(_enableLateTick, new GUIContent("Enable Late Tick"));
        }

        private void DrawFlowMachineDebug()
        {
            var flowBehaviour = (FlowBehaviour)target;
            var currentState = flowBehaviour.GetCurrentState();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Flow Machine Debug", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);

            // Current State
            string stateName = currentState != null
                ? System.Text.RegularExpressions.Regex.Replace(currentState.GetType().Name, "(\\B[A-Z])", " $1")
                : "None";
            EditorGUILayout.TextField("Current State", stateName);

            // Time in current state (moved above transitions)
            string timeInState = GetTimeInCurrentState(currentState);
            EditorGUILayout.TextField("Time", timeInState);

            // Potential Transitions in Unity list style
            DrawPotentialTransitionsAsArray(flowBehaviour);

            EditorGUI.EndDisabledGroup();
        }

        private string GetTimeInCurrentState(IState currentState)
        {
            if (currentState == null)
                return "0.00s";

            // Track state changes
            if (_lastState != currentState)
            {
                _lastState = currentState;
                _stateStartTime = (float)EditorApplication.timeSinceStartup;
            }

            float timeInState = (float)EditorApplication.timeSinceStartup - _stateStartTime;
            return $"{timeInState:F2}s";
        }

        private void DrawPotentialTransitionsAsArray(FlowBehaviour flowBehaviour)
        {
            var flowMachine = flowBehaviour.GetFlowMachine();
            if (flowMachine == null)
            {
                EditorGUILayout.LabelField("Transitions", "None");
                return;
            }

            var potentialStates = flowMachine.GetPotentialTransitions();

            // Draw array-style header with size
            var headerRect = EditorGUILayout.GetControlRect();
            EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, EditorGUIUtility.labelWidth, headerRect.height),
                $"Transitions ({potentialStates.Count})");

            if (potentialStates.Count == 0)
                return;

            // Draw each transition as array element
            EditorGUI.indentLevel++;
            for (int i = 0; i < potentialStates.Count; i++)
            {
                string stateName = System.Text.RegularExpressions.Regex.Replace(potentialStates[i].GetType().Name, "(\\B[A-Z])", " $1");
                EditorGUILayout.TextField($"Element {i}", stateName);
            }
            EditorGUI.indentLevel--;
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

        private void DrawDerivedClassProperties()
        {
            // Draw all other properties except FlowBehaviour base class properties
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Skip script reference and FlowBehaviour properties
                if (iterator.propertyPath == "m_Script" ||
                    iterator.name == "_enableFixedTick" ||
                    iterator.name == "_enableLateTick")
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }
}
#endif