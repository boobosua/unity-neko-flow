#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace NekoFlow.FSM
{
    [CustomEditor(typeof(StateBehaviour), true)]
    public class StateBehaviourInspector :
#if ODIN_INSPECTOR
        OdinEditor          // Use Odin when available
#else
        Editor              // Fallback to normal Unity inspector
#endif
    {
        private const string RuntimeFoldoutStateKeyPrefix = "NekoFlow.StateBehaviourInspector.RuntimeFoldout";

        private static GUIStyle _runtimeBoxStyle;

        private IState _lastState;
        private float _stateStartTime;
        private readonly List<IState> _transitionsBuffer = new(8);
        private readonly Dictionary<System.Type, string> _typeNameCache = new(8);

        public override void OnInspectorGUI()
        {
#if ODIN_INSPECTOR
            // --- ODIN VERSION ---
            // Odin handles the serializedObject life cycle internally,
            // so don't wrap base.OnInspectorGUI() in Update/Apply.

            // Optional: if you DON'T want the default script field Odin shows,
            // you can comment this out.
            // DrawScriptField();

            DrawRuntimeFoldout();

            EditorGUILayout.Space();

            // Let Odin draw all fields with attributes, groups, etc.
            base.OnInspectorGUI();

            if (Application.isPlaying)
                Repaint();

#else
            // --- NON-ODIN VERSION ---
            serializedObject.Update();

            DrawScriptField();
            DrawRuntimeFoldout();

            EditorGUILayout.Space();

            // Old behaviour: manually draw all other properties
            DrawDerivedClassProperties();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                Repaint();
#endif
        }

        private void DrawRuntimeFoldout()
        {
            bool expanded = SessionState.GetBool(GetRuntimeFoldoutStateKey(), true);
            bool expandedBefore = expanded;
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "State Machine Runtime");
            if (expanded != expandedBefore)
                SessionState.SetBool(GetRuntimeFoldoutStateKey(), expanded);

            if (expanded)
            {
                DrawRuntimeBox();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private string GetRuntimeFoldoutStateKey()
        {
            // Match Unity's typical inspector behavior: state is per-object, editor-only, and not serialized into scenes/prefabs.
            // We keep it project-local (Library) via SessionState.
#if UNITY_2020_2_OR_NEWER
            GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return $"{RuntimeFoldoutStateKeyPrefix}.{globalId}";
#else
            return $"{RuntimeFoldoutStateKeyPrefix}.{target.GetInstanceID()}";
#endif
        }

        private static GUIStyle GetRuntimeBoxStyle()
        {
            if (_runtimeBoxStyle != null)
                return _runtimeBoxStyle;

            _runtimeBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(
                    EditorStyles.helpBox.padding.left + 6,
                    EditorStyles.helpBox.padding.right + 6,
                    EditorStyles.helpBox.padding.top + 4,
                    EditorStyles.helpBox.padding.bottom + 4)
            };
            return _runtimeBoxStyle;
        }

        private void DrawRuntimeBox()
        {
            var stateBehaviour = (StateBehaviour)target;
            var currentState = stateBehaviour.GetCurrentState();

            using (new EditorGUILayout.VerticalScope(GetRuntimeBoxStyle()))
            {
                EditorGUILayout.Space(2);
                using (new EditorGUI.DisabledScope(true))
                {
                    // Current State name (text only; not a UnityEngine.Object)
                    string stateName = GetPrettyTypeName(currentState?.GetType());
                    EditorGUILayout.TextField("Current State", stateName);

                    // Time In State as integer seconds (0 when not applicable)
                    int seconds = GetTimeInCurrentStateSeconds(currentState, stateBehaviour);
                    EditorGUILayout.TextField("Time In State", $"{seconds}s");
                }

                // In Play Mode, list potential transitions with Jump buttons
                if (Application.isPlaying)
                {
                    var stateMachine = stateBehaviour.GetStateMachine();
                    if (stateMachine != null)
                    {
                        stateMachine.GetPotentialTransitionsNonAlloc(_transitionsBuffer);
                        if (_transitionsBuffer.Count > 0)
                        {
                            EditorGUILayout.Space(4);
                            EditorGUILayout.LabelField(
                                "Available Transitions", EditorStyles.boldLabel);

                            for (int i = 0; i < _transitionsBuffer.Count; i++)
                            {
                                var to = _transitionsBuffer[i];
                                string toName = GetPrettyTypeName(to?.GetType());

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
                                            stateMachine.SetState(to);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space(2);
            }
        }

        private string GetPrettyTypeName(System.Type type)
        {
            if (type == null) return "None";
            if (_typeNameCache.TryGetValue(type, out var cached))
                return cached;
            string pretty = Regex.Replace(type.Name, "(\\B[A-Z])", " $1");
            _typeNameCache[type] = pretty;
            return pretty;
        }

        private int GetTimeInCurrentStateSeconds(IState currentState, StateBehaviour component)
        {
            if (!Application.isPlaying || currentState == null)
                return 0;

            // Prefer runtime-tracked time from the FSM when available
            var sm = component != null ? component.GetStateMachine() : null;
            if (sm != null)
            {
                return Mathf.FloorToInt(sm.TimeInState);
            }

            // Fallback to editor timer if FSM not available
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
