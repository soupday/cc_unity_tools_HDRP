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

using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
	[CustomEditor(typeof(WeightMapper))]
	public class WeightMapperEditor : Editor
	{
		private WeightMapper weightMapper;
		private ColliderManager colliderManager;
		
		const float LABEL_WIDTH = 80f;
		const float GUTTER = 40f;
		const float BUTTON_WIDTH = 160f;

		private void OnEnable()
		{
			// Method 1
			weightMapper = (WeightMapper)target;
			colliderManager = weightMapper.GetComponentInParent<ColliderManager>();					
		}

		public override void OnInspectorGUI()
		{ 
			// Draw default inspector after button...
			base.OnInspectorGUI();

			OnClothInspectorGUI();
		}

		public void OnClothInspectorGUI()
		{
			Color background = GUI.backgroundColor;

			GUILayout.Space(10f);

			EditorGUILayout.HelpBox("Recalculate all the cloth constraints from the weight maps and cloth settings. Can be done in play mode.", MessageType.Info, true);	

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = Color.Lerp(background, Color.white, 0.25f);
			if (GUILayout.Button("Rebuild Constraints", GUILayout.Width(BUTTON_WIDTH)))
			{
				weightMapper.ApplyWeightMap();
			}
			GUI.backgroundColor = background;
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			EditorGUILayout.HelpBox("Settings can be saved in play mode and reloaded after play mode ends.", MessageType.Info, true);			

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = Color.Lerp(background, Color.red, 0.25f);
			if (GUILayout.Button("Save Settings", GUILayout.Width(BUTTON_WIDTH)))
			{
				PhysicsSettingsStore.SaveClothSettings(weightMapper);
			}
			GUI.backgroundColor = background;
			GUILayout.Space(10f);
			GUI.backgroundColor = Color.Lerp(background, Color.yellow, 0.25f);
			if (GUILayout.Button("Recall Settings", GUILayout.Width(BUTTON_WIDTH)))
			{
				PhysicsSettingsStore.RecallClothSettings(weightMapper);
			}
			GUI.backgroundColor = background;			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (Application.isPlaying) GUI.enabled = false;
			GUI.backgroundColor = Color.Lerp(background, Color.cyan, 0.25f);
			if (GUILayout.Button("Apply to Prefab", GUILayout.Width(BUTTON_WIDTH)))
			{
				UpdatePrefab(weightMapper);
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






