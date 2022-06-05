using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor.AnyType
{
    [CustomPropertyDrawer(typeof(AnyType<>))]
    public class AnyTypeDrawer : PropertyDrawer
    {
        private static readonly (string typeEnum, string unityObject, string nativeObject) PropertyName =
            ("isUnityObjectReference", "unityObject", "nativeObject");

        private Type targetAbstractType;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var isUnityObject = property.FindPropertyRelative(PropertyName.typeEnum).boolValue;
            return EditorGUI.GetPropertyHeight(
                property.FindPropertyRelative(isUnityObject ? PropertyName.unityObject : PropertyName.nativeObject),
                label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            targetAbstractType = TypeUtils.ExtractTypeFromString(property
                .FindPropertyRelative(PropertyName.nativeObject)
                .managedReferenceFieldTypename);

            EditorGUI.BeginProperty(rect, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var refTypeProperty = property.FindPropertyRelative(PropertyName.typeEnum);
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
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(PropertyName.nativeObject), label);
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
                var unityObjectProperty = property.FindPropertyRelative(PropertyName.unityObject);
                var searchButton = new Rect(leftButtonRect);
                searchButton.x += leftButtonRect.width + 5;
                searchButton.width = 35;
                rect.x += 25;
                rect.width -= 25;

                if (GUI.Button(searchButton, "Pick"))
                {
                    SearchService.ShowObjectPicker(
                        (o, _) => FillUnityObjectToAnyTypeProperty(o, property), null, GetSearchFilter(), null,
                        typeof(Object), flags: SearchFlags.Expression);
                }

                var newObject = EditorGUI.ObjectField(rect, label, unityObjectProperty.objectReferenceValue,
                    typeof(Object), true);
                if (unityObjectProperty.objectReferenceValue != newObject)
                {
                    FillUnityObjectToAnyTypeProperty(newObject, property);
                }
            }
        }

        private void FillUnityObjectToAnyTypeProperty(Object newUnityObject, SerializedProperty property)
        {
            var targetType = TypeUtils.ExtractTypeFromString(property.FindPropertyRelative(PropertyName.nativeObject)
                .managedReferenceFieldTypename);
            var unityObjectProperty = property.FindPropertyRelative(PropertyName.unityObject);
            Object targetObject = null;
            if (targetType.IsInstanceOfType(newUnityObject))
            {
                targetObject = newUnityObject;
            }
            else
            {
                var component = GetComponentFromGameObject(newUnityObject);
                if (component != null)
                {
                    targetObject = component;
                }
            }

            var isValidNewObject = targetObject != null && targetType.IsInstanceOfType(targetObject);
            var isNullNewObject = unityObjectProperty.objectReferenceValue != null && targetObject == null;

            if (isValidNewObject || isNullNewObject)
            {
                unityObjectProperty.objectReferenceValue = targetObject;
                unityObjectProperty.serializedObject.ApplyModifiedProperties();
                unityObjectProperty.serializedObject.Update();
            }

            Object GetComponentFromGameObject(Object someObject)
            {
                if (someObject != null && someObject is GameObject go)
                {
                    var component = go.GetComponent(targetType);
                    return component;
                }

                return null;
            }
        }

        private string GetSearchFilter()
        {
            var unityTypes = TypeCache.GetTypesDerivedFrom(targetAbstractType).Where(IsAssignableUnityType);

            var sb = new StringBuilder();
            foreach (var type in unityTypes)
            {
                sb.Append($"t:{type.Name} ");
            }

            return sb.ToString();

            bool IsAssignableUnityType(Type type)
            {
                return TypeUtils.IsFinalAssignableType(type) && type.IsSubclassOf(typeof(Object));
            }
        }
    }
}