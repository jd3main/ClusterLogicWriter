using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using ClusterVR.CreatorKit.Operation;



namespace ClusterLogicWriter
{
    [CustomEditor(typeof(LogicWriter))]
    public class LogicWriterEditor : Editor
    {
        SerializedProperty logicComponent;
        SerializedProperty logicCode;
        SerializedProperty omitCurrentTarget;

        SerializedProperty debugOutput;



        private void OnEnable()
        {
            logicComponent = serializedObject.FindProperty("_logicComponent");
            logicCode = serializedObject.FindProperty("_logicCode");
            omitCurrentTarget = serializedObject.FindProperty("omitCurrentTarget");

            debugOutput = serializedObject.FindProperty("debugOutput");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                logicComponent.objectReferenceValue =
                    EditorGUILayout.ObjectField(logicComponent.objectReferenceValue, typeof(ILogic), true);


                EditorGUILayout.LabelField("Logic Code");
                logicCode.stringValue = EditorGUILayout.TextArea(logicCode.stringValue, GUILayout.MinHeight(100));

                EditorGUILayout.PropertyField(omitCurrentTarget);
                

                using (var h = new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Extract"))
                    {
                        Extract();
                    }

                    if (GUILayout.Button("Compile"))
                    {
                        Compile();
                    }
                }

                //debugOutput.stringValue = EditorGUILayout.TextArea(debugOutput.stringValue, GUILayout.MinHeight(100));
            }
            serializedObject.ApplyModifiedProperties();
        }


        public void Extract()
        {
            var logicInterpreter = (LogicWriter)serializedObject.targetObject;
            logicInterpreter.Extract();
        }

        public void Compile()
        {
            var logicInterpreter = (LogicWriter)serializedObject.targetObject;
            logicInterpreter.Compile();
            EditorUtility.SetDirty((Component)logicInterpreter.logicComponent);
        }
    }
}