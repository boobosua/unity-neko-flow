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

        // Invalidated when the editor skin changes (e.g. Light ↔ Dark mode switch).
        private static GUISkin _lastSkin;
        private static GUIStyle _runtimeBoxStyle;

        private string _foldoutStateKey;
        private readonly List<IState> _transitionsBuffer = new(8);
        private readonly Dictionary<System.Type, string> _typeNameCache = new(8);

        protected override void OnEnable()
        {
            base.OnEnable();
            // Cache once per inspector instance — target is valid from OnEnable onward.
            _foldoutStateKey = ComputeFoldoutStateKey();
        }

        public override void OnInspectorGUI()
        {
#if ODIN_INSPECTOR
            DrawRuntimeFoldout();

            EditorGUILayout.Space();

            // Let Odin draw all fields with attributes, groups, etc.
            base.OnInspectorGUI();

            if (Application.isPlaying)
                Repaint();

#else
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
            bool expanded = SessionState.GetBool(_foldoutStateKey, true);
            bool expandedBefore = expanded;
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, "State Machine Runtime");
            if (expanded != expandedBefore)
                SessionState.SetBool(_foldoutStateKey, expanded);

            if (expanded)
            {
                DrawRuntimeBox();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private string ComputeFoldoutStateKey()
        {
            // State is per-object, editor-only, and kept in Library (not serialized into scenes/prefabs).
#if UNITY_2020_2_OR_NEWER
            GlobalObjectId globalId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            return $"{RuntimeFoldoutStateKeyPrefix}.{globalId}";
#else
            return $"{RuntimeFoldoutStateKeyPrefix}.{target.GetInstanceID()}";
#endif
        }

        private static GUIStyle GetRuntimeBoxStyle()
        {
            // Recreate the style whenever the editor skin changes (e.g. Light ↔ Dark mode).
            if (_runtimeBoxStyle != null && GUI.skin == _lastSkin)
                return _runtimeBoxStyle;

            _lastSkin = GUI.skin;
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

        private static int GetTimeInCurrentStateSeconds(IState currentState, StateBehaviour component)
        {
            if (!Application.isPlaying || currentState == null)
                return 0;

            return Mathf.FloorToInt(component.GetStateMachine().TimeInState);
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
