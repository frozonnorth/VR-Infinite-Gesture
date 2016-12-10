﻿using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Edwon.VR.Gesture
{
    [CustomEditor(typeof(Tutorial))]
    [CanEditMultipleObjects]
    public class TutorialEditor : Editor
    {
        Tutorial tutorial;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //DrawDefaultInspector();
            tutorial = (Tutorial)target;

            SerializedProperty tutorialState = serializedObject.FindProperty("tutorialState");

            EditorGUILayout.IntField(tutorial.TutorialSettings.currentTutorialStep);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(tutorialState);
            if (EditorGUI.EndChangeCheck())
            {
                int enumIndex = tutorialState.enumValueIndex;
                tutorial.SwitchTutorialState((Tutorial.TutorialState)enumIndex);
            }

            //if (GUILayout.Button("VR Setup Complete"))
            //{
            //    tutorial.SwitchTutorialState(Tutorial.TutorialState.VRSetupComplete);
            //}

            serializedObject.ApplyModifiedProperties();
        }
    }
}