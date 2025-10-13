#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
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

        bool _openScriptAfterCreate = true;

        // Folder paths (strings; folder-only via Browse)
        string _scriptsRootPath;
        string _assetsRootPath;

        Type _controllerType;                 // T in ScriptableFlowRunner<T>
        string _controllerTypeName;             // EnemyController, NPCController, ...

        // Use non-firstpass folders so generated scripts can reference user types (e.g., Enemy in Assembly-CSharp)
        const string BaseScriptsRoot = "Assets/NekoFlow/Scriptables/Scripts";   // Fallback when context script folder not found
        const string BaseAssetsRoot = "Assets/NekoFlow/Scriptables/Resources";  // Fallback when context script folder not found

        void OnEnable()
        {
            _initialStateProp = serializedObject.FindProperty("_initialState");
            _controllerType = ResolveControllerTypeFromRunner(target.GetType());
            _controllerTypeName = _controllerType != null ? _controllerType.Name : "Controller";

            // Default roots: under the context (controller) script folder
            // e.g., <ContextFolder>/Scriptable and <ContextFolder>/Scriptable/Resources
            var ctxFolder = FindScriptFolderOfType(_controllerType);
            if (!string.IsNullOrEmpty(ctxFolder))
            {
                _scriptsRootPath = EnsureFolderPath($"{ctxFolder}/Scriptable");
                _assetsRootPath = EnsureFolderPath($"{ctxFolder}/Scriptable/Resources");
            }
            else
            {
                // Fallback: project-level defaults
                _scriptsRootPath = EnsureFolderPath($"{BaseScriptsRoot}/{_controllerTypeName}");
                _assetsRootPath = EnsureFolderPath($"{BaseAssetsRoot}/{_controllerTypeName}");
            }
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

                // Warn when selecting a first-pass folder where user types (Assembly-CSharp) are not visible
                if (IsFirstPassFolder(_scriptsRootPath))
                {
                    EditorGUILayout.HelpBox(
                        "Selected Scripts Root is under a first-pass folder (Plugins/Standard Assets). " +
                        "Scripts there cannot reference regular project types like your Enemy MonoBehaviour. " +
                        "Choose a folder outside those special directories (e.g., Assets/NekoFlow/...).",
                        MessageType.Warning);
                }

                _openScriptAfterCreate = EditorGUILayout.ToggleLeft("Open Script After Create", _openScriptAfterCreate);

                var nameCore = StripSuffixes(Sanitize(_stateName), "State"); // avoid *StateState
                var className = $"{_controllerTypeName}{nameCore}State"; // no underscore

                var scriptsDir = _scriptsRootPath;
                var assetsDir = _assetsRootPath;
                var scriptPath = Combine(scriptsDir, $"States/{className}.cs");
                var assetPath = Combine(assetsDir, $"States/{className}.asset");

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
                var className = $"{_controllerTypeName}{nameCore}"; // no underscore
                var scriptPath = Combine(_scriptsRootPath, $"Actions/{className}.cs");
                var assetPath = Combine(_assetsRootPath, $"Actions/{className}.asset");

                if (IsFirstPassFolder(_scriptsRootPath))
                {
                    EditorGUILayout.HelpBox(
                        "Scripts Root is under a first-pass folder. Generated Actions may fail to compile if they reference regular project types.",
                        MessageType.Warning);
                }

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
                var className = $"{_controllerTypeName}{nameCore}"; // no underscore
                var scriptPath = Combine(_scriptsRootPath, $"Conditions/{className}.cs");
                var assetPath = Combine(_assetsRootPath, $"Conditions/{className}.asset");

                if (IsFirstPassFolder(_scriptsRootPath))
                {
                    EditorGUILayout.HelpBox(
                        "Scripts Root is under a first-pass folder. Generated Conditions may fail to compile if they reference regular project types.",
                        MessageType.Warning);
                }

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

            CreateAssetWhenCompiled(() =>
            {
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                var type = mono?.GetClass();
                if (!IsValidStateTypeForRunner(type, _controllerType))
                    return false;

                var so = CreateInstance(type);
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();

                EditorGUIUtility.PingObject(so);
                Selection.activeObject = so;
                if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
                return true;
            },
            onTimeout: () =>
            {
                // If we timed out likely due to domain reload or slow compile; enqueue for post-reload processing
                CreationQueue.Enqueue(new CreationQueue.Request
                {
                    scriptPath = scriptPath,
                    assetPath = assetPath,
                    openGenericName = "ScriptableState`1",
                    controllerTypeAqn = _controllerType?.AssemblyQualifiedName,
                    openScript = _openScriptAfterCreate,
                    targetGlobalId = null,
                    initialProp = null
                });
            });
        }

        void CreateAction(string className, string scriptPath, string assetPath)
        {
            EnsureDirs(scriptPath, assetPath);

            File.WriteAllText(scriptPath, EmitActionScript(className, _controllerType), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            CreateAssetWhenCompiled(() =>
            {
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                var type = mono?.GetClass();
                if (!IsValidTyped(type, "ScriptableAction`1", _controllerType))
                    return false;

                var so = CreateInstance(type);
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();

                EditorGUIUtility.PingObject(so);
                Selection.activeObject = so;
                if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
                return true;
            },
            onTimeout: () =>
            {
                CreationQueue.Enqueue(new CreationQueue.Request
                {
                    scriptPath = scriptPath,
                    assetPath = assetPath,
                    openGenericName = "ScriptableAction`1",
                    controllerTypeAqn = _controllerType?.AssemblyQualifiedName,
                    openScript = _openScriptAfterCreate,
                    targetGlobalId = null,
                    initialProp = null
                });
            });
        }

        void CreateCondition(string className, string scriptPath, string assetPath)
        {
            EnsureDirs(scriptPath, assetPath);

            File.WriteAllText(scriptPath, EmitConditionScript(className, _controllerType), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            CreateAssetWhenCompiled(() =>
            {
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                var type = mono?.GetClass();
                if (!IsValidTyped(type, "ScriptableCondition`1", _controllerType))
                    return false;

                var so = CreateInstance(type);
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();

                EditorGUIUtility.PingObject(so);
                Selection.activeObject = so;
                if (_openScriptAfterCreate && mono) AssetDatabase.OpenAsset(mono);
                return true;
            },
            onTimeout: () =>
            {
                CreationQueue.Enqueue(new CreationQueue.Request
                {
                    scriptPath = scriptPath,
                    assetPath = assetPath,
                    openGenericName = "ScriptableCondition`1",
                    controllerTypeAqn = _controllerType?.AssemblyQualifiedName,
                    openScript = _openScriptAfterCreate,
                    targetGlobalId = null,
                    initialProp = null
                });
            });
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

        static bool IsFirstPassFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var p = path.Replace("\\", "/");
            return p.StartsWith("Assets/Plugins/") || p.Equals("Assets/Plugins")
                || p.StartsWith("Assets/Standard Assets/") || p.Equals("Assets/Standard Assets")
                || p.StartsWith("Assets/Pro Standard Assets/") || p.Equals("Assets/Pro Standard Assets");
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

        // Resolve the folder (Assets-relative) where the controller type's script file lives
        static string FindScriptFolderOfType(Type t)
        {
            if (t == null) return null;
            var guids = AssetDatabase.FindAssets("t:MonoScript " + t.Name);
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() == t)
                {
                    return Path.GetDirectoryName(path).Replace("\\", "/");
                }
            }
            return null;
        }

        // Wait across editor frames for Unity to compile and the new class to resolve, then run creation.
        // Returns immediately; calls onTimeout if class does not resolve within maxAttempts frames.
        void CreateAssetWhenCompiled(Func<bool> tryCreate, int attempt = 0, int maxAttempts = 60, Action onTimeout = null)
        {
            // If the editor is compiling (or about to), defer to post-reload queue for reliability
            if (EditorApplication.isCompiling)
            {
                onTimeout?.Invoke();
                return;
            }
            // If creation succeeds (type resolved and asset created), stop.
            if (tryCreate()) return;

            // Otherwise, schedule another attempt on next editor frame until timeout.
            if (attempt >= maxAttempts)
            {
                onTimeout?.Invoke();
                return;
            }
            EditorApplication.delayCall += () => CreateAssetWhenCompiled(tryCreate, attempt + 1, maxAttempts, onTimeout);
        }

        // Persistent queue processed after scripts reload to finish creating assets reliably
        static class CreationQueue
        {
            [Serializable]
            public class Request
            {
                public string scriptPath;
                public string assetPath;
                public string openGenericName; // ScriptableState`1, ScriptableAction`1, ScriptableCondition`1
                public string controllerTypeAqn;
                public bool openScript;
                public string targetGlobalId; // for assigning initial state
                public string initialProp;     // usually _initialState
            }

            [Serializable]
            class RequestList { public System.Collections.Generic.List<Request> items = new System.Collections.Generic.List<Request>(); }

            const string Key = "NekoFlow_Scriptable_CreateQueue";

            static RequestList Load()
            {
                var json = EditorPrefs.GetString(Key, null);
                var data = new RequestList();
                if (!string.IsNullOrEmpty(json))
                {
                    try { JsonUtility.FromJsonOverwrite(json, data); } catch { data = new RequestList(); }
                }
                return data;
            }

            static void Save(RequestList list)
            {
                var json = JsonUtility.ToJson(list);
                EditorPrefs.SetString(Key, json);
            }

            public static void Enqueue(Request req)
            {
                var list = Load();
                list.items.Add(req);
                Save(list);
            }

            public static string ToGlobalIdString(UnityEngine.Object obj)
            {
                if (!obj) return null;
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                return gid.ToString();
            }

            public static UnityEngine.Object FromGlobalIdString(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                if (GlobalObjectId.TryParse(s, out var gid))
                    return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                return null;
            }

            [DidReloadScripts]
            static void OnScriptsReloaded()
            {
                EditorApplication.delayCall += ProcessQueue;

                static void ProcessQueue()
                {
                    var list = Load();
                    if (list.items.Count == 0) return;

                    var remaining = new RequestList();
                    bool began = false;
                    try
                    {
                        AssetDatabase.StartAssetEditing();
                        began = true;

                        foreach (var r in list.items)
                        {
                            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(r.scriptPath);
                            var type = mono != null ? mono.GetClass() : null;
                            var ctrlType = !string.IsNullOrEmpty(r.controllerTypeAqn) ? Type.GetType(r.controllerTypeAqn) : null;
                            bool ok = type != null && InheritsGeneric(type, r.openGenericName, ctrlType);
                            if (!ok)
                            {
                                // Keep it for next processing
                                remaining.items.Add(r);
                                continue;
                            }

                            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(r.assetPath) == null)
                            {
                                EnsureDirs(r.scriptPath, r.assetPath);
                                var so = ScriptableObject.CreateInstance(type);
                                AssetDatabase.CreateAsset(so, r.assetPath);

                                if (r.openScript && mono)
                                {
                                    // Schedule opening the script after asset operations to keep UI responsive
                                    var m = mono; // capture
                                    EditorApplication.delayCall += () => AssetDatabase.OpenAsset(m);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (began) AssetDatabase.StopAssetEditing();
                        AssetDatabase.SaveAssets();
                    }

                    Save(remaining);
                }
            }
        }
    }
}
#endif
