using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.AnyType
{
    public static class AnyTypeDrawerIMGUI
    {
        public static void Draw(Rect rect, SerializedProperty property, GUIContent label, Action<Object> writeNewObject,
            Action onClickPicker)
        {
            EditorGUI.BeginProperty(rect, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var refTypeProperty = property.FindPropertyRelative(AnyTypeDrawer.PropertyName.typeEnum);
            var isUnityObj = refTypeProperty.boolValue;

            var leftButtonRect = DrawLeftReferenceTypeButton();

            rect.width -= 40;
            rect.x += 40;
            if (isUnityObj)
            {
                DrawIMGUIUnityReferenceType();
            }
            else
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(AnyTypeDrawer.PropertyName.nativeObject),
                    label);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();

            Rect DrawLeftReferenceTypeButton()
            {
                var refTypeButton = isUnityObj ? "U" : "#";
                var buttonRect = new Rect(rect);
                buttonRect.width = 20;
                buttonRect.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(buttonRect, refTypeButton))
                {
                    isUnityObj = !isUnityObj;
                    refTypeProperty.boolValue = isUnityObj;
                    refTypeProperty.serializedObject.ApplyModifiedProperties();
                }

                return buttonRect;
            }

            void DrawIMGUIUnityReferenceType()
            {
                var unityObjectProperty = property.FindPropertyRelative(AnyTypeDrawer.PropertyName.unityObject);
                var searchButton = new Rect(leftButtonRect);
                searchButton.x += leftButtonRect.width + 5;
                searchButton.width = 35;
                rect.x += 25;
                rect.width -= 25;

                if (GUI.Button(searchButton, "Pick"))
                {
                    onClickPicker.Invoke();
                    return;
                }

                var newObject = EditorGUI.ObjectField(rect, label, unityObjectProperty.objectReferenceValue,
                    typeof(Object), true);
                if (unityObjectProperty.objectReferenceValue != newObject)
                {
                    writeNewObject.Invoke(newObject);
                }
            }
        }
    }
}