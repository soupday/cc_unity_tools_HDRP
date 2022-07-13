using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using ColliderSettings = Reallusion.Import.ColliderManager.ColliderSettings;

namespace Reallusion.Import
{
	[CustomEditor(typeof(ColliderManager))]
	public class ColliderManagerEditor : Editor
	{
		private ColliderManager colliderManager;
		private ColliderSettings currentCollider;
		private bool symetrical = true;

		const float LABEL_WIDTH = 80f;
		const float GUTTER = 40f;
		const float BUTTON_WIDTH = 160f;

		private void OnEnable()
		{
			colliderManager = (ColliderManager)target;
			if (colliderManager.settings.Length > 0)
				currentCollider = colliderManager.settings[0];
			else
				currentCollider = null;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			OnColliderInspectorGUI();
		}

		public void OnColliderInspectorGUI()
		{
			if (currentCollider == null) return;

			Color background = GUI.backgroundColor;

			GUILayout.Space(10f);

			GUILayout.Label("Adjust Colliders", EditorStyles.boldLabel);

			GUILayout.Space(10f);

			GUILayout.BeginVertical(EditorStyles.helpBox);			

			// custom collider adjuster			
			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Collider", GUILayout.Width(LABEL_WIDTH));
			if (EditorGUILayout.DropdownButton(
				new GUIContent(currentCollider.name),
				FocusType.Passive
				))
			{
				GenericMenu menu = new GenericMenu();
				foreach (ColliderSettings c in colliderManager.settings)
				{
					menu.AddItem(new GUIContent(c.name), c == currentCollider, SelectCurrentCollider, c);
				}
				menu.ShowAsContext();
			}
			GUILayout.EndHorizontal();


			GUILayout.Space(8f);

			EditorGUI.BeginChangeCheck();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Radius", GUILayout.Width(LABEL_WIDTH));
			currentCollider.radiusAdjust = EditorGUILayout.Slider(currentCollider.radiusAdjust, -0.1f, 0.1f);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Height", GUILayout.Width(LABEL_WIDTH));
			currentCollider.heightAdjust = EditorGUILayout.Slider(currentCollider.heightAdjust, -0.1f, 0.1f);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("X", GUILayout.Width(LABEL_WIDTH));
			currentCollider.xAdjust = EditorGUILayout.Slider(currentCollider.xAdjust, -0.1f, 0.1f);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Y", GUILayout.Width(LABEL_WIDTH));
			currentCollider.yAdjust = EditorGUILayout.Slider(currentCollider.yAdjust, -0.1f, 0.1f);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Z", GUILayout.Width(LABEL_WIDTH));
			currentCollider.zAdjust = EditorGUILayout.Slider(currentCollider.zAdjust, -0.1f, 0.1f);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("Symetrical", GUILayout.Width(LABEL_WIDTH));
			symetrical = EditorGUILayout.Toggle(symetrical);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(GUTTER);
			GUILayout.Label("", GUILayout.Width(LABEL_WIDTH));
			if (GUILayout.Button("Reset", GUILayout.Width(80f)))
			{
				currentCollider.Reset();
				if (symetrical) UpdateSymetrical();
			}
			GUILayout.Space(10f);
			if (GUILayout.Button("Select", GUILayout.Width(80f)))
            {
				Selection.activeObject = currentCollider.collider;
            }
			GUILayout.EndHorizontal();


			if (EditorGUI.EndChangeCheck())
			{
				currentCollider.Update();
				if (symetrical) UpdateSymetrical();
			}

			GUILayout.EndVertical();

			GUILayout.Space(10f);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (!EditorApplication.isPlaying)
			{
				GUI.backgroundColor = Color.Lerp(background, Color.cyan, 0.25f);
				if (GUILayout.Button("Apply to Prefab", GUILayout.Width(BUTTON_WIDTH)))
				{
					UpdatePrefab(colliderManager);
				}
				EditorGUI.BeginDisabledGroup(!PhysicsSettingsStore.TryFindSettingsObject(out string foundSettingsGuid));
				GUI.backgroundColor = Color.Lerp(background, Color.yellow, 0.25f);
				if (GUILayout.Button("Recall Settings", GUILayout.Width(BUTTON_WIDTH)))
				{
					PhysicsSettingsStore.RecallColliderSettings(colliderManager);
				}
				GUI.backgroundColor = background;
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUI.backgroundColor = Color.Lerp(background, Color.red, 0.25f);
				if (GUILayout.Button("Save Settings", GUILayout.Width(BUTTON_WIDTH)))
				{
					PhysicsSettingsStore.SaveColliderSettings(colliderManager);
				}
				GUI.backgroundColor = background;
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10f);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Cloth Meshes", EditorStyles.boldLabel);
			GUILayout.BeginVertical();
			
			GUI.backgroundColor = Color.Lerp(background, Color.green, 0.25f);
			foreach (GameObject clothMesh in colliderManager.clothMeshes)
            {
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(clothMesh.name, GUILayout.Width(160f)))
				{
					Selection.activeObject = clothMesh;
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(4f);
			}
			GUI.backgroundColor = background;
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		public void UpdatePrefab(Object component)
		{
			GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(component);			
			if (prefabRoot)
			{									
				// reset collider states
				ColliderManager colliderManager = prefabRoot.GetComponentInChildren<ColliderManager>();
				if (colliderManager)
				{
					foreach (ColliderSettings cs in colliderManager.settings)
					{
						cs.Reset(true);
					}
				}

				// save prefab asset
				PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
			}
		}

		private void UpdateSymetrical()
		{
			string name = currentCollider.name;

			string boneName = name.Remove(name.IndexOf("_Capsule"));
			string symName = null;
			//Debug.Log(boneName);

			if (boneName.Contains("_L_"))
			{
				symName = boneName.Replace("_L_", "_R_");
			}
			else if (boneName.Contains("_R_"))
			{
				symName = boneName.Replace("_R_", "_L_");
			}
			else if (boneName.Contains("_Hip"))
			{
				symName = boneName;
				
			}			

			if (!string.IsNullOrEmpty(symName))
			{
				foreach (ColliderSettings cs in colliderManager.settings)
				{
					if (cs != currentCollider && cs.name.StartsWith(symName))
					{
						cs.MirrorX(currentCollider);
						cs.Update();
					}
				}
			}

			symName = null;
			if (name == "CC_Base_NeckTwist01_Capsule(1)")
			{
				symName = "CC_Base_NeckTwist01_Capsule(2)";
			}

			if (!string.IsNullOrEmpty(symName))
			{
				foreach (ColliderSettings cs in colliderManager.settings)
				{
					if (cs != currentCollider && cs.name.StartsWith(symName))
					{
						cs.MirrorZ(currentCollider);
						cs.Update();
					}
				}
			}

		}

		private void SelectCurrentCollider(object sel)
		{
			currentCollider = (ColliderSettings)sel;
		}			
	}
}






