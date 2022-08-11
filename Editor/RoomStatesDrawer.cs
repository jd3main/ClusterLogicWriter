using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;


namespace ClusterLogicWriter
{
    public class RoomStatesDrawer : PropertyDrawer
    {
        public ReorderableList list;
        public float padding = 1;

        public void Init(SerializedProperty property)
        {
            list = new ReorderableList(property.serializedObject,
                                       property,
                                       true, true, true, true);
            list.drawElementCallback = DrawListElement;
            list.drawHeaderCallback = DrawListHeader;
            list.elementHeightCallback = ElementHeight;
        }

        public void Draw()
        {
            list.DoLayoutList();
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            list.DoLayoutList();
        }


        public void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Room States");
        }

        public void DrawListElement(Rect entryRect, int index, bool isActive, bool isFocused)
        {
            float spacing = 10;
            float targetWidth = 60;
            float typeWidth = 80;
            var serializedProperty = list.serializedProperty.GetArrayElementAtIndex(index);

            var rect = entryRect;
            rect.height = EditorGUIUtility.singleLineHeight;

            rect.y += padding;

            rect.width = targetWidth;
            EditorGUI.PropertyField(rect, serializedProperty.FindPropertyRelative("target"), GUIContent.none);

            rect.x += rect.width + spacing;

            rect.width = (entryRect.width - spacing*2 - targetWidth - typeWidth);
            EditorGUI.PropertyField(rect, serializedProperty.FindPropertyRelative("key"), GUIContent.none);

            rect.x += rect.width + spacing;

            rect.width = typeWidth;
            EditorGUI.PropertyField(rect, serializedProperty.FindPropertyRelative("type"), GUIContent.none);
        }

        public float ElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + padding*2;
        }
    }
}
