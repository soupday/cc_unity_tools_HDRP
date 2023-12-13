using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Reallusion.Import
{
    [CustomEditor(typeof(PrefabNavigation))]
    public class PrefabNavigationEditor : Editor
    {
        private PrefabNavigation prefabNavigation;
        private ColliderManager colliderManager;

        const float BUTTON_WIDTH = 120f;

        private void OnEnable()
        {
            prefabNavigation = (PrefabNavigation)target;
            colliderManager = prefabNavigation.GetComponentInParent<ColliderManager>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            OnNavigatorInspectorGUI();
        }

        private void OnNavigatorInspectorGUI()
        {
            Color background = GUI.backgroundColor;

            GUILayout.Space(10f);

            EditorGUILayout.HelpBox("This tool allows saving of current data to the prefab, and quick navigation back to the root ColliderManager.", MessageType.Info, true);

            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (Application.isPlaying) GUI.enabled = false;
            GUI.backgroundColor = Color.Lerp(background, Color.cyan, 0.25f);
            if (GUILayout.Button("Apply to Prefab", GUILayout.Width(BUTTON_WIDTH)))
            {
                UpdatePrefab(prefabNavigation);
            }
            GUI.enabled = true;
            GUILayout.Space(10f);
            GUI.backgroundColor = Color.Lerp(background, Color.green, 0.25f);
            if (GUILayout.Button("Collider Manager", GUILayout.Width(BUTTON_WIDTH)))
            {
                Selection.activeObject = colliderManager;
            }
            GUI.backgroundColor = background;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void UpdatePrefab(Object component)
        {
            WindowManager.HideAnimationPlayer(true);
            WindowManager.HideAnimationRetargeter(true);

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(component);
            if (prefabRoot)
            {
                // save prefab asset
                PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            }
        }
    }
}
