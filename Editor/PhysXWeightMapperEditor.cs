using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Reallusion.Import
{
	[CustomEditor(typeof(PhysXWeightMapper))]
	public class PhysXWeightMapperEditor : Editor
	{
		private PhysXWeightMapper script;
		private PhysXColliders collidersScript;
		private PhysXColliders.ColliderSettings currentCollider;
		private bool symetrical = true;


		const float LABEL_WIDTH = 80f;
		const float GUTTER = 40f;
		const float BUTTON_WIDTH = 200f;

		private void OnEnable()
		{
			// Method 1
			script = (PhysXWeightMapper)target;
			collidersScript = script.GetComponentInParent<PhysXColliders>();
			
			if (collidersScript.colliders.Length > 0)
				currentCollider = collidersScript.colliders[0];
			else
				currentCollider = null;
		}

		public override void OnInspectorGUI()
		{			
			// Draw default inspector after button...
			base.OnInspectorGUI();

			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Rebuild Constraints", GUILayout.Width(BUTTON_WIDTH)))
			{
				script.DoCloth();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if (!collidersScript)
			{
				GUILayout.Space(10f);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Apply to Prefab", GUILayout.Width(BUTTON_WIDTH)))
				{
					UpdatePrefab();
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Space(10f);
				OnInspectorGUColliders();
			}
		}

		public void OnInspectorGUColliders()
		{
			GUILayout.Label("Adjust Colliders", EditorStyles.boldLabel);

			GUILayout.Space(10f);

			GUILayout.BeginVertical(EditorStyles.helpBox);

			// custom collider adjuster
			if (currentCollider != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(GUTTER);
				GUILayout.Label("Collider", GUILayout.Width(LABEL_WIDTH));
				if (EditorGUILayout.DropdownButton(
					new GUIContent(currentCollider.name),
					FocusType.Passive
					))
				{
					GenericMenu menu = new GenericMenu();
					foreach (PhysXColliders.ColliderSettings c in collidersScript.colliders)
					{
						menu.AddItem(new GUIContent(c.name), c == currentCollider, SelectCurrentCollider, c);
					}
					menu.ShowAsContext();
				}
				GUILayout.EndHorizontal();
			}

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
			GUILayout.EndHorizontal();


			if (EditorGUI.EndChangeCheck())
			{
				currentCollider.Update();
				if (symetrical) UpdateSymetrical();
			}

			GUILayout.EndVertical();

			if (Application.isPlaying) GUI.enabled = false;
			GUILayout.Space(10f);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Apply to Prefab", GUILayout.Width(BUTTON_WIDTH)))
			{
				UpdatePrefab();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUI.enabled = true;
		}



		private void UpdateSerialized()
		{
			// doesn't do nothing in play mode...

			//"colliders.Array.data[3].name.Array.data[2]"
			serializedObject.Update();

			int i = 0;
			foreach (PhysXColliders.ColliderSettings cs in collidersScript.colliders)
			{
				SerializedProperty prop = serializedObject.FindProperty("colliders.Array.data[" + i + "].radiusAdjust");
				prop.floatValue = cs.radiusAdjust;
				prop = serializedObject.FindProperty("colliders.Array.data[" + i + "].heightAdjust");
				prop.floatValue = cs.heightAdjust;
				i++;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void UpdatePrefab()
		{
			GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(script);
			if (prefabRoot)
			{
				PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
				foreach (PhysXColliders.ColliderSettings cs in collidersScript.colliders)
				{
					cs.Reset(true);
				}
			}
		}

		private void UpdateSymetrical()
		{
			string name = currentCollider.name;

			string boneName = name.Remove(name.IndexOf("_Capsule"));
			string symName = null;
			//Debug.Log(boneName);

			if (boneName.Contains("_L_", System.StringComparison.InvariantCultureIgnoreCase))
			{
				symName = boneName.Replace("_L_", "_R_");
			}
			else if (boneName.Contains("_R_", System.StringComparison.InvariantCultureIgnoreCase))
			{
				symName = boneName.Replace("_R_", "_L_");
			}
			else if (boneName.Contains("_Hip", System.StringComparison.InvariantCultureIgnoreCase))
			{
				symName = boneName;

			}

			if (!string.IsNullOrEmpty(symName))
			{
				foreach (PhysXColliders.ColliderSettings cs in collidersScript.colliders)
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
				foreach (PhysXColliders.ColliderSettings cs in collidersScript.colliders)
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
			currentCollider = (PhysXColliders.ColliderSettings)sel;
		}


	}
}






