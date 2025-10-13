#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NekoFlow.Scriptable.Editor
{
    // Force an IMGUI inspector for all ScriptableState<T> assets to bypass UI Toolkit text jobs
    [CustomEditor(typeof(ScriptableState<>), true)]
    public class ScriptableStateInspector : UnityEditor.Editor
    {
        SerializedProperty _actionsProp;
        SerializedProperty _transitionsProp;

        ReorderableList _actionsList;
        ReorderableList _transitionsList;

        void OnEnable()
        {
            _actionsProp = serializedObject.FindProperty("_actions");
            _transitionsProp = serializedObject.FindProperty("_transitions");

            // Build simple IMGUI lists; keep all work on main thread
            if (_actionsProp != null)
            {
                _actionsList = new ReorderableList(serializedObject, _actionsProp, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "Actions"),
                    drawElementCallback = (r, i, a, f) =>
                    {
                        var el = _actionsProp.GetArrayElementAtIndex(i);
                        EditorGUI.ObjectField(r, el, GUIContent.none);
                    }
                };
            }

            if (_transitionsProp != null)
            {
                _transitionsList = new ReorderableList(serializedObject, _transitionsProp, true, true, true, true)
                {
                    drawHeaderCallback = r => EditorGUI.LabelField(r, "Transitions"),
                    elementHeightCallback = i => EditorGUIUtility.singleLineHeight + 6f,
                    drawElementCallback = (r, i, a, f) =>
                    {
                        var el = _transitionsProp.GetArrayElementAtIndex(i);
                        var cond = el.FindPropertyRelative("Condition");
                        var target = el.FindPropertyRelative("TargetState");
                        var half = r.width * 0.5f;
                        var left = new Rect(r.x, r.y + 2, half - 2, EditorGUIUtility.singleLineHeight);
                        var right = new Rect(r.x + half + 2, r.y + 2, half - 2, EditorGUIUtility.singleLineHeight);
                        EditorGUI.ObjectField(left, cond, GUIContent.none);
                        EditorGUI.ObjectField(right, target, GUIContent.none);
                    }
                };
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_actionsList != null) _actionsList.DoLayoutList();
                else EditorGUILayout.PropertyField(_actionsProp, true);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_transitionsList != null) _transitionsList.DoLayoutList();
                else EditorGUILayout.PropertyField(_transitionsProp, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
