using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawIMGUITypeDropdown(rect, property, label);
            }
            else
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawUIToolkitTypeDropdown(root, property);
            }
            else
            {
                root.Add(new PropertyField(property));
            }

            return root;
        }

        private void DrawUIToolkitTypeDropdown(VisualElement root, SerializedProperty property)
        {
            string uiToolkitLayoutPath =
                "Packages/com.alexeytaranov.serializereferencedropdown/Editor/Layouts/SerializeReferenceDropdown.uxml";
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uiToolkitLayoutPath);
            root.Add(visualTreeAsset.Instantiate());
            var selectTypeButton = root.Q<Button>();
            var propertyField = root.Q<PropertyField>();
            propertyField.BindProperty(property);
            assignableTypes ??= GetAssignableTypes(property);
            var selectedType =
                TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            var selectedTypeName = GetTypeName(selectedType);
            selectTypeButton.clickable.clicked += ShowDropdown;
            selectTypeButton.text = selectedTypeName;

            void ShowDropdown()
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName), index => WriteNewInstanceByIndexType(index, property));
                var buttonRect = new Rect(selectTypeButton.worldTransform.GetPosition(),
                    selectTypeButton.contentRect.size);
                dropdown.Show(buttonRect);
            }
        }

        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);
            Rect dropdownRect = new Rect(rect);
            dropdownRect.width -= EditorGUIUtility.labelWidth;
            dropdownRect.x += EditorGUIUtility.labelWidth;
            dropdownRect.height = EditorGUIUtility.singleLineHeight;
            var referenceType = TypeUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(GetTypeName(referenceType)), FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(new AdvancedDropdownState(),
                    assignableTypes.Select(GetTypeName),
                    index => WriteNewInstanceByIndexType(index, property));
                dropdown.Show(dropdownRect);
            }

            EditorGUI.PropertyField(rect, property, label, true);
        }

        private string GetTypeName(Type type) => type == null ? NullName : ObjectNames.NicifyVariableName(type.Name);

        private List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = TypeUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var nonUnityTypes = TypeCache.GetTypesDerivedFrom(propertyType).Where(IsAssignableNonUnityType).ToList();
            nonUnityTypes.Insert(0, null);
            return nonUnityTypes;

            bool IsAssignableNonUnityType(Type type)
            {
                return TypeUtils.IsFinalAssignableType(type) && !type.IsSubclassOf(typeof(UnityEngine.Object));
            }
        }

        private void WriteNewInstanceByIndexType(int typeIndex, SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var newObject = newType != null ? Activator.CreateInstance(newType) : null;
            ApplyValueToProperty(newObject, property);
        }

        private void ApplyValueToProperty(object value, SerializedProperty property)
        {
            property.managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }
    }
}