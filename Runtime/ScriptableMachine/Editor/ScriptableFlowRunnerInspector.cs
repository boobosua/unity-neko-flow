#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NekoFlow.Scriptable.Editor
{
    // Custom inspector for any concrete class deriving ScriptableFlowRunner<T>
    [CustomEditor(typeof(ScriptableFlowRunner<>), true)]
    public class ScriptableFlowRunnerInspector : UnityEditor.Editor
    {
        SerializedProperty _initialStateProp;

        // UI state (defaults)
        string _stateName = "New State";
        string _actionName = "New Action";
        string _conditionName = "New Condition";

        bool _assignAsInitial = false;          // default: false
        bool _openScriptAfterCreate = true;

        // Folder paths (strings; folder-only via Browse)
        string _scriptsRootPath;
        string _assetsRootPath;

        Type _controllerType;                 // T in ScriptableFlowRunner<T>
        string _controllerTypeName;             // EnemyController, NPCController, ...

        const string BaseScriptsRoot = "Assets/Plugins/NekoFlow/Scriptables/Scripts";
        const string BaseAssetsRoot = "Assets/Plugins/NekoFlow/Scriptables/Resources";

        void OnEnable()
        {
            _initialStateProp = serializedObject.FindProperty("_initialState");
            _controllerType = ResolveControllerTypeFromRunner(target.GetType());
            _controllerTypeName = _controllerType != null ? _controllerType.Name : "Controller";

            // Default roots
            _scriptsRootPath = EnsureFolderPath($"{BaseScriptsRoot}/{_controllerTypeName}");
            _assetsRootPath = EnsureFolderPath($"{BaseAssetsRoot}/{_controllerTypeName}");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Initial state slot
            EditorGUILayout.PropertyField(_initialStateProp);

            // Runtime box visible in Edit & Play modes
            DrawRuntimeBox();

            EditorGUILayout.Space(10);
            DrawQuickAddState();
            EditorGUILayout.Space(6);
            DrawQuickAddAction();
            EditorGUILayout.Space(6);
            DrawQuickAddCondition();

            serializedObject.ApplyModifiedProperties();
        }

        // -------------------- Runtime --------------------
        void DrawRuntimeBox()
        {
            var runner = target;
            var t = runner.GetType();

            var currentState = t.GetProperty("CurrentState")?.GetValue(runner) as ScriptableObject;
            var context = t.GetProperty("Context")?.GetValue(runner) as Component;

            // Time: show always, compact integer seconds (0s when no state or not playing)
            int seconds = 0;
            var tisProp = t.GetProperty("TimeInCurrentState");
            if (Application.isPlaying && currentState != null && tisProp != null)
            {
                float tis = (float)tisProp.GetValue(runner);
                seconds = Mathf.FloorToInt(tis);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Context", context, typeof(Component), true);
                    EditorGUILayout.ObjectField("Current State", currentState, typeof(ScriptableObject), false);
                    EditorGUILayout.TextField("Time In State", $"{seconds}s");
                }

                // In Play Mode, show jump buttons for available transitions
                if (Application.isPlaying && currentState != null)
                {
                    var so = new SerializedObject(currentState);
                    var trans = so.FindProperty("_transitions");
                    if (trans != null && trans.isArray)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Available Transitions", EditorStyles.boldLabel);

                        var transitionTo = t.GetMethod("TransitionTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        for (int i = 0; i < trans.arraySize; i++)
                        {
                            var elem = trans.GetArrayElementAtIndex(i);
                            var cond = elem.FindPropertyRelative("Condition");
                            var targ = elem.FindPropertyRelative("TargetState");

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.ObjectField(cond.objectReferenceValue, typeof(ScriptableObject), false);
                                EditorGUILayout.ObjectField(targ.objectReferenceValue, typeof(ScriptableObject), false);
                                using (new EditorGUI.DisabledScope(targ.objectReferenceValue == null))
                                {
                                    if (GUILayout.Button("Jump", GUILayout.Width(60)))
                                        transitionTo?.Invoke(runner, new object[] { targ.objectReferenceValue });
                                }
                            }
                        }
                    }
                }
            }
        }

        // -------------------- Quick-Add: State --------------------
        void DrawQuickAddState()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick-Add State", EditorStyles.boldLabel);

                if (_controllerType == null)
                {
                    EditorGUILayout.HelpBox("Could not resolve T from ScriptableFlowRunner<T>.", MessageType.Error);
                    return;
                }

                _stateName = EditorGUILayout.TextField("State Name", _stateName);
                _scriptsRootPath = FolderField("Scripts Root", _scriptsRootPath, $"{BaseScriptsRoot}/{_controllerTypeName}");
                _assetsRootPath = FolderField("Assets Root (Resources)", _assetsRootPath, $"{BaseAssetsRoot}/{_controllerTypeName}");

                _assignAsInitial = EditorGUILayout.ToggleLeft("Assign as Initial State", _assignAsInitial);
                _openScriptAfterCreate = EditorGUILayout.ToggleLeft("Open Script After Create", _openScriptAfterCreate);

                var nameCore = StripSuffixes(Sanitize(_stateName), "State"); // avoid *StateState
                var className = $"{_controllerTypeName}_{nameCore}State";

                var scriptsDir = _scriptsRootPath;
                var assetsDir = _assetsRootPath;
                var scriptPath = Combine(scriptsDir, $"{className}.cs");
                var assetPath = Combine(assetsDir, $"{className}.asset");

                var valid = ValidateNewTypePaths(className, scriptPath, assetPath, out var reason);
                bool playLock = EditorApplication.isPlayingOrWillChangePlaymode;
                if (playLock) EditorGUILayout.HelpBox("Creation is disabled while entering/inside Play Mode.", MessageType.Info);

                using (new EditorGUI.DisabledScope(!valid || playLock))
                {
                    if (GUILayout.Button("Create New State", GUILayout.Height(26)))
                        CreateState(className, scriptPath, assetPath);
                }
                if (!valid) EditorGUILayout.HelpBox(reason, MessageType.Warning);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Script Path", scriptPath);
                    EditorGUILayout.TextField("Asset Path", assetPath);
                }
            }
        }

        // -------------------- Quick-Add: Action --------------------
        void DrawQuickAddAction()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick-Add Action", EditorStyles.boldLabel);

                _actionName = EditorGUILayout.TextField("Action Class Name", _actionName);

                var nameCore = StripSuffixes(Sanitize(_actionName), "Action"); // avoid *ActionAction
                var className = $"{_controllerTypeName}_{nameCore}";
                var scriptPath = Combine(_scriptsRootPath, $"Actions/{className}.cs");
                var assetPath = Combine(_assetsRootPath, $"Actions/{className}.asset");

                var valid = ValidateNewTypePaths(className, scriptPath, assetPath, out var reason);
                bool playLock = EditorApplication.isPlayingOrWillChangePlaymode;
                if (playLock) EditorGUILayout.HelpBox("Creation is disabled while entering/inside Play Mode.", MessageType.Info);

                using (new EditorGUI.DisabledScope(!valid || playLock))
                {
                    if (GUILayout.Button("Create New Action", GUILayout.Height(22)))
                        CreateAction(className, scriptPath, assetPath);
                }
                if (!valid) EditorGUILayout.HelpBox(reason, MessageType.Warning);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Script Path", scriptPath);
                    EditorGUILayout.TextField("Asset Path", assetPath);
                }
            }
        }

        // -------------------- Quick-Add: Condition --------------------
        void DrawQuickAddCondition()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick-Add Condition", EditorStyles.boldLabel);

                _conditionName = EditorGUILayout.TextField("Condition Class Name", _conditionName);

                var nameCore = StripSuffixes(Sanitize(_conditionName), "Condition"); // avoid *ConditionCondition
                var className = $"{_controllerTypeName}_{nameCore}";
                var scriptPath = Combine(_scriptsRootPath, $"Conditions/{className}.cs");
                var assetPath = Combine(_assetsRootPath, $"Conditions/{className}.asset");

                var valid = ValidateNewTypePaths(className, scriptPath, assetPath, out var reason);
                bool playLock = EditorApplication.isPlayingOrWillChangePlaymode;
                if (playLock) EditorGUILayout.HelpBox("Creation is disabled while entering/inside Play Mode.", MessageType.Info);

                using (new EditorGUI.DisabledScope(!valid || playLock))
                {
                    if (GUILayout.Button("Create New Condition", GUILayout.Height(22)))
                        CreateCondition(className, scriptPath, assetPath);
                }
                if (!valid) EditorGUILayout.HelpBox(reason, MessageType.Warning);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Script Path", scriptPath);
                    EditorGUILayout.TextField("Asset Path", assetPath);
                }
            }
        }

        // -------------------- Creation --------------------
        void CreateState(string className, string scriptPath, string assetPath)
        {
            EnsureDirs(scriptPath, assetPath);

            File.WriteAllText(scriptPath, EmitStateScript(className, _controllerType), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var type = mono?.GetClass();
            if (!IsValidStateTypeForRunner(type, _controllerType))
            {
                EditorUtility.DisplayDialog("Compile / Type Check",
                    "Unity hasn’t compiled the new script yet, or it doesn’t derive from the correct ScriptableState<T>.",
                    "OK");
                return;
            }

            var so = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();

            if (_assignAsInitial && _initialStateProp != null)
            {
                Undo.RecordObject(target as UnityEngine.Object, "Assign Initial State");
                _initialStateProp.objectReferenceValue = so;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target as UnityEngine.Object);
            }

            EditorGUIUtility.PingObject(so);
            Selection.activeObject = so;
            if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
        }

        void CreateAction(string className, string scriptPath, string assetPath)
        {
            EnsureDirs(scriptPath, assetPath);

            File.WriteAllText(scriptPath, EmitActionScript(className, _controllerType), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var type = mono?.GetClass();
            if (!IsValidTyped(type, "ScriptableAction`1", _controllerType))
            {
                EditorUtility.DisplayDialog("Compile / Type Check",
                    "Unity hasn’t compiled the new script yet, or it doesn’t derive from ScriptableAction<T> for this runner’s T.",
                    "OK");
                return;
            }

            var so = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(so);
            Selection.activeObject = so;
            if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
        }

        void CreateCondition(string className, string scriptPath, string assetPath)
        {
            EnsureDirs(scriptPath, assetPath);

            File.WriteAllText(scriptPath, EmitConditionScript(className, _controllerType), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var type = mono?.GetClass();
            if (!IsValidTyped(type, "ScriptableCondition`1", _controllerType))
            {
                EditorUtility.DisplayDialog("Compile / Type Check",
                    "Unity hasn’t compiled the new script yet, or it doesn’t derive from ScriptableCondition<T> for this runner’s T.",
                    "OK");
                return;
            }

            var so = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(so);
            Selection.activeObject = so;
            if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
        }

        // -------------------- Helpers --------------------
        // Folder picker UI that only accepts folders within the project (Assets/)
        string FolderField(string label, string current, string defaultPath)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var newPath = EditorGUILayout.TextField(label, current);
                if (GUILayout.Button("Browse…", GUILayout.Width(80)))
                {
                    var startAbs = ToAbsolutePath(string.IsNullOrEmpty(newPath) ? defaultPath : newPath);
                    var chosenAbs = EditorUtility.OpenFolderPanel("Select Folder", string.IsNullOrEmpty(startAbs) ? Application.dataPath : startAbs, "");
                    if (!string.IsNullOrEmpty(chosenAbs))
                    {
                        var rel = ToProjectRelative(chosenAbs);
                        if (!string.IsNullOrEmpty(rel) && rel.StartsWith("Assets/"))
                        {
                            newPath = EnsureFolderPath(rel);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Folder",
                                "Please choose a folder inside your Unity project (under Assets/).",
                                "OK");
                        }
                    }
                }
                return EnsureFolderPath(string.IsNullOrEmpty(newPath) ? defaultPath : newPath);
            }
        }

        static string ToAbsolutePath(string projectRelative)
        {
            if (string.IsNullOrEmpty(projectRelative)) return null;
            projectRelative = projectRelative.Replace("\\", "/");
            if (projectRelative.StartsWith("Assets/") || projectRelative == "Assets")
            {
                var root = Application.dataPath.Replace("\\", "/");
                var abs = Path.GetFullPath(Path.Combine(root, "..", projectRelative)).Replace("\\", "/");
                return abs;
            }
            return null;
        }

        static string ToProjectRelative(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return null;
            absolute = absolute.Replace("\\", "/");
            var assets = Application.dataPath.Replace("\\", "/");
            if (absolute.StartsWith(assets))
            {
                var rel = "Assets" + absolute.Substring(assets.Length);
                return rel.TrimEnd('/');
            }
            return null;
        }

        static string EnsureFolderPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Assets";
            path = path.Replace("\\", "/").TrimEnd('/');

            // Create missing hierarchy as needed
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var cur = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = $"{cur}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }
            return path;
        }

        static void EnsureDirs(string scriptPath, string assetPath)
        {
            var sp = Path.GetDirectoryName(scriptPath);
            var ap = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(sp)) Directory.CreateDirectory(sp);
            if (!string.IsNullOrEmpty(ap)) Directory.CreateDirectory(ap);
        }

        static bool ValidateNewTypePaths(string className, string scriptPath, string assetPath, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(className)) { reason = "Invalid name."; return false; }
            if (File.Exists(scriptPath)) { reason = "A script with the same name already exists at the Scripts Root."; return false; }
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null) { reason = "An asset with the same name already exists at the Assets Root."; return false; }

            // Prevent type name clashes anywhere in the project
            var clash = AssetDatabase.FindAssets($"{className} t:MonoScript");
            if (clash != null && clash.Length > 0) { reason = $"A class named '{className}' already exists in the project."; return false; }

            return true;
        }

        static Type ResolveControllerTypeFromRunner(Type t)
        {
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("ScriptableFlowRunner`"))
                    return t.GetGenericArguments()[0];
                t = t.BaseType;
            }
            return null;
        }

        static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var chars = raw.ToCharArray();
            var sb = new StringBuilder(chars.Length);
            foreach (var ch in chars) if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            var s = sb.ToString();
            if (s.Length > 0 && char.IsDigit(s[0])) s = "_" + s;
            return s;
        }

        static string StripSuffixes(string nameCore, params string[] suffixes)
        {
            foreach (var suf in suffixes)
            {
                var norm = nameCore.TrimEnd('_');
                if (norm.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                    return norm.Substring(0, norm.Length - suf.Length).TrimEnd('_');
            }
            return nameCore;
        }

        static bool IsValidStateTypeForRunner(Type type, Type controllerType)
        {
            if (type == null || controllerType == null) return false;
            return InheritsGeneric(type, "ScriptableState`1", controllerType);
        }

        static bool IsValidTyped(Type type, string openGenericName, Type controllerType)
        {
            if (type == null || controllerType == null) return false;
            return InheritsGeneric(type, openGenericName, controllerType);
        }

        static bool InheritsGeneric(Type type, string openGenericName, Type expectedArg)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition().Name == openGenericName)
                {
                    var arg = type.GetGenericArguments()[0];
                    return arg == expectedArg;
                }
                type = type.BaseType;
            }
            return false;
        }

        // -------------------- Emitters --------------------
        static string EmitStateScript(string className, Type ctrlType)
        {
            var ns = ctrlType.Namespace;
            var ctrl = ctrlType.Name;
            var sb = new StringBuilder();
            sb.AppendLine("using NekoFlow.Scriptable;");
            sb.AppendLine("using UnityEngine;");
            if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"using {ns};");
            sb.AppendLine();
            sb.AppendLine($"[CreateAssetMenu(fileName = \"{className}\", menuName = \"Neko/Scriptable Flow/{ctrl}/State/{className}\")]");
            sb.AppendLine($"public sealed class {className} : ScriptableState<{ctrl}> {{ }}");
            return sb.ToString();
        }

        static string EmitActionScript(string className, Type ctrlType)
        {
            var ns = ctrlType.Namespace;
            var ctrl = ctrlType.Name;
            var sb = new StringBuilder();
            sb.AppendLine("using NekoFlow.Scriptable;");
            sb.AppendLine("using UnityEngine;");
            if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"using {ns};");
            sb.AppendLine();
            sb.AppendLine($"[CreateAssetMenu(fileName = \"{className}\", menuName = \"Neko/Scriptable Flow/{ctrl}/Action/{className}\")]");
            sb.AppendLine($"public sealed class {className} : ScriptableAction<{ctrl}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public override void OnUpdate({ctrl} ctx) {{ }}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string EmitConditionScript(string className, Type ctrlType)
        {
            var ns = ctrlType.Namespace;
            var ctrl = ctrlType.Name;
            var sb = new StringBuilder();
            sb.AppendLine("using NekoFlow.Scriptable;");
            sb.AppendLine("using UnityEngine;");
            if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"using {ns};");
            sb.AppendLine();
            sb.AppendLine($"[CreateAssetMenu(fileName = \"{className}\", menuName = \"Neko/Scriptable Flow/{ctrl}/Condition/{className}\")]");
            sb.AppendLine($"public sealed class {className} : ScriptableCondition<{ctrl}>");
            sb.AppendLine("{");
            sb.AppendLine("    [SerializeField, Min(0f)] private float _seconds = 1.0f;");
            sb.AppendLine($"    public override bool IsMet({ctrl} ctx, float timeInState) => timeInState >= _seconds;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string Combine(string folder, string file) =>
            (folder.EndsWith("/") ? folder : folder + "/") + file;
    }
}
#endif
