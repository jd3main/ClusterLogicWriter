using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using ClusterVR.CreatorKit.Operation;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;

namespace ClusterLogicWriter
{
    public class LogicWriterEditorWindow : EditorWindow
    {
        List<Component> logicComponents;
        int selectedIndex = 0;
        bool autoExtract = true;
        bool showExtractOptions = false;
        Vector2 codeScrollPosition;
        GameObject lastActiveGameObject;

        LogicInterpreter interpreter = new LogicInterpreter();

        ILogic selectedLogicComponent
        {
            get
            {
                if (logicComponents == null || logicComponents.Count == 0)
                    return null;
                if (selectedIndex >= logicComponents.Count)
                    selectedIndex = 0;
                return (ILogic)logicComponents[selectedIndex];
            }
        }


        [MenuItem("Cluster/Logic Writer")]
        private static void Init()
        {
            var window = GetWindow<LogicWriterEditorWindow>();
            window.autoRepaintOnSceneChange = true;
            window.Show();
        }


        private void OnGUI()
        {
            var selectedGOs = Selection.gameObjects;
            if (selectedGOs.Length == 0)
            {
                EditorGUILayout.HelpBox("Select a gameobject to edit", MessageType.Info);
            }
            else if (selectedGOs.Length > 1)
            {
                EditorGUILayout.HelpBox("Multiple selection is not supported", MessageType.Warning);
            }
            else
            {
                Draw();
            }
        }

        private void Draw()
        {
            GameObject activeGameObject = Selection.activeGameObject;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(activeGameObject, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();

            logicComponents = activeGameObject.GetComponents<ILogic>().Cast<Component>().ToList();


            if (logicComponents.Count > 0)
            {
                var options = logicComponents.Select(c =>
                    $"{'[' + c.GetInstanceID().ToString() + ']',-10} {c.GetType().Name} ({c._Get<object>("key")._Get<object>("key")})")
                    .ToArray();

                var newSelectedIndex = EditorGUILayout.Popup(selectedIndex, options);
                if (newSelectedIndex != selectedIndex || interpreter.logicComponent == null)
                {
                    selectedIndex = newSelectedIndex;
                    interpreter.logicComponent = selectedLogicComponent;
                    if (autoExtract)
                    {
                        interpreter.Extract();
                    }
                }


                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Space();
                EditorGUILayout.EnumPopup("Scope", interpreter.GetLogicScope());
                EditorGUILayout.Space();
                EditorGUI.EndDisabledGroup();


                using (var h = new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Extract"))
                    {
                        interpreter.Extract();
                    }
                    if (GUILayout.Button("Compile"))
                    {
                        interpreter.Compile();
                        Debug.Log("Compile complete");
                    }
                }

                showExtractOptions = EditorGUILayout.Foldout(showExtractOptions, "Options");
                if (showExtractOptions)
                {
                    EditorGUI.indentLevel += 1;
                    autoExtract = EditorGUILayout.Toggle("Auto Extract", autoExtract);
                    interpreter.omitCurrentTarget = EditorGUILayout.Toggle("Omit Scope", interpreter.omitCurrentTarget);
                    interpreter.compressStatements = EditorGUILayout.Toggle("Compress Expressions", interpreter.compressStatements);
                    EditorGUI.indentLevel -= 1;
                }

                codeScrollPosition = GUILayout.BeginScrollView(codeScrollPosition);
                EditorGUILayout.LabelField("Logic Code" + (interpreter.codeModified ? "*" : ""));
                interpreter.logicCode = EditorGUILayout.TextArea(interpreter.logicCode, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject != lastActiveGameObject)
            {
                interpreter.logicComponent = null;
                
                Repaint();

                lastActiveGameObject = Selection.activeGameObject;
            }
        }
    }
}
