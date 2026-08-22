using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Decay.EditorTools
{
    internal static class AnimatorParameterDropdown
    {
        private const string SharedAnimatorPropertyName = "_animator";
        private const string NoneLabel = "<None>";

        internal static Animator GetSharedAnimator(SerializedProperty property)
        {
            SerializedProperty animatorProperty = property.serializedObject.FindProperty(SharedAnimatorPropertyName);
            return animatorProperty?.objectReferenceValue as Animator;
        }

        internal static void Draw(
            Rect position,
            GUIContent label,
            SerializedProperty parameterProperty,
            AnimatorControllerParameterType requiredType)
        {
            Animator animator = GetSharedAnimator(parameterProperty);

            EditorGUI.BeginProperty(position, label, parameterProperty);
            if (animator == null)
            {
                DrawUnavailable(position, label, "<Assign shared Animator above>");
                EditorGUI.EndProperty();
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                DrawUnavailable(position, label, "<Assign Controller on Animator>");
                EditorGUI.EndProperty();
                return;
            }

            List<string> options = new List<string> { NoneLabel };
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == requiredType)
                    options.Add(parameters[i].name);
            }

            string current = parameterProperty.stringValue ?? string.Empty;
            int currentIndex = string.IsNullOrWhiteSpace(current) ? 0 : options.IndexOf(current);
            bool currentIsMissing = currentIndex < 0;
            if (currentIsMissing)
            {
                options.Add($"<Missing: {current}>");
                currentIndex = options.Count - 1;
            }

            if (options.Count == 1)
            {
                DrawUnavailable(position, label, $"<No {requiredType} parameters in Controller>");
                EditorGUI.EndProperty();
                return;
            }

            int selectedIndex = EditorGUI.Popup(position, label, currentIndex, options.ToArray());
            if (selectedIndex == 0)
            {
                parameterProperty.stringValue = string.Empty;
            }
            else if (!(currentIsMissing && selectedIndex == options.Count - 1))
            {
                parameterProperty.stringValue = options[selectedIndex];
            }

            EditorGUI.EndProperty();
        }

        private static void DrawUnavailable(Rect position, GUIContent label, string message)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.Popup(position, label, 0, new[] { message });
        }
    }

    [CustomPropertyDrawer(typeof(AnimatorTriggerPresentationBinding))]
    public sealed class AnimatorTriggerPresentationBindingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty playTrigger = property.FindPropertyRelative("_playTrigger");
            SerializedProperty cancelTrigger = property.FindPropertyRelative("_cancelTrigger");

            float line = EditorGUIUtility.singleLineHeight;
            Rect playRect = new Rect(position.x, position.y, position.width, line);
            Rect cancelRect = new Rect(
                position.x,
                position.y + line + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                line);

            AnimatorParameterDropdown.Draw(
                playRect,
                new GUIContent(label.text, label.tooltip),
                playTrigger,
                AnimatorControllerParameterType.Trigger);

            AnimatorParameterDropdown.Draw(
                cancelRect,
                new GUIContent("Cancel / Interrupt (Optional)", "Optional Trigger used only when presentation is interrupted or reconciled. Normal Animator exit transitions remain authored in the Animator Controller."),
                cancelTrigger,
                AnimatorControllerParameterType.Trigger);
        }
    }

    [CustomPropertyDrawer(typeof(AnimatorBoolPresentationBinding))]
    public sealed class AnimatorBoolPresentationBindingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty boolParameter = property.FindPropertyRelative("_boolParameter");
            AnimatorParameterDropdown.Draw(
                position,
                label,
                boolParameter,
                AnimatorControllerParameterType.Bool);
        }
    }

    [CustomPropertyDrawer(typeof(AnimatorIntPresentationBinding))]
    public sealed class AnimatorIntPresentationBindingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty intParameter = property.FindPropertyRelative("_intParameter");
            AnimatorParameterDropdown.Draw(
                position,
                label,
                intParameter,
                AnimatorControllerParameterType.Int);
        }
    }
}
