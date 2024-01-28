/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC_Unity_Tools <https://github.com/soupday/CC_Unity_Tools>
 * 
 * CC_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

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
