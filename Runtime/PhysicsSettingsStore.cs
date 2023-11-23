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
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using PhysicsSettings = Reallusion.Import.WeightMapper.PhysicsSettings;
using ColliderSettings = Reallusion.Import.ColliderManager.ColliderSettings;
#endif

namespace Reallusion.Import
{
	[System.Serializable]
	public class PhysicsSettingsStore : ScriptableObject
	{
#if UNITY_EDITOR
		public ColliderSettings[] colliderSettings;
		public List<PhysicsSettings> clothSettings;
		private const string settingsDir = "Settings";
		private const string settingsFileName = "PhysicsSettingsStore";
		private const string settingsSuffix = ".asset";
		// additions        
		public List<ColliderManager.AbstractCapsuleCollider> abstractColliderSettings;
		public ColliderManager.GizmoState gizmoState;
		//public string[] gizmosToRestore;
		//public string[] iconsToRestore;
		private const string referenceSettingsDir = "_Reference";
		// end of additions

		private static string GetSettingsStorePath(Object obj)
		{
			string guid = null;
			if (obj.GetType() == typeof(ColliderManager))
			{
				guid = ((ColliderManager)obj).characterGUID;
			}
			else if (obj.GetType() == typeof(WeightMapper))
			{
				guid = ((WeightMapper)obj).characterGUID;
			}

			string characterPath = null;
			if (!string.IsNullOrEmpty(guid))
			{
				characterPath = AssetDatabase.GUIDToAssetPath(guid);
			}
			else
			{
				Debug.LogWarning("Unable to determine character physics store path.\nPlease rebuild physics for this character to correct this.");
				return null;
			}

			string characterFolder;
			string characterName;
			if (!string.IsNullOrEmpty(characterPath))
			{
				characterFolder = Path.GetDirectoryName(characterPath);
				characterName = Path.GetFileNameWithoutExtension(characterPath);
				return Path.Combine(characterFolder, settingsDir, characterName, settingsFileName + settingsSuffix);
			}
			return null;
		}

		public static string GetCharacterPath(Object sceneObject)
		{
			if (EditorApplication.isPlaying)
			{
				Debug.LogWarning("Unable to determine character physics store path in Play mode.\nPlease rebuild physics for this character to correct this.");
				return null;
			}

			if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
			{
				Object instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(sceneObject);
				if (!instanceRoot) instanceRoot = sceneObject;
				if (instanceRoot.GetType() == typeof(GameObject))
				{
					Object source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceRoot);
					if (source)
					{
						if (source.GetType() == typeof(GameObject))
						{
							return AssetDatabase.GetAssetPath(source);
						}
					}
				}
			}
			return null;
		}

		public static PhysicsSettingsStore SaveColliderSettings(ColliderManager colliderManager)
		{
			ColliderSettings[] workingSettings = new ColliderSettings[colliderManager.settings.Length];
			for (int i = 0; i < colliderManager.settings.Length; i++)
			{
				workingSettings[i] = new ColliderSettings(colliderManager.settings[i]);
			}

			PhysicsSettingsStore settings = TryFindSettingsObject(colliderManager);
			if (settings)
			{
				settings.colliderSettings = workingSettings;
				EditorUtility.SetDirty(settings);
#if UNITY_2021_2_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
#else
				AssetDatabase.SaveAssets();
#endif

				Debug.Log("Collider settings stored.");

				return settings;
			}

			return null;
		}

		public static PhysicsSettingsStore RecallColliderSettings(ColliderManager colliderManager)
		{
			PhysicsSettingsStore saved = TryFindSettingsObject(colliderManager);

			if (saved)
			{
				ColliderSettings[] tmp = saved.colliderSettings;
				foreach (ColliderSettings c in tmp)
				{
					foreach (ColliderSettings s in colliderManager.settings)
					{

						if (s.name == c.name)
						{
							s.Copy(c, false);
							s.Update();
						}
					}
				}

				colliderManager.UpdateColliders();

				Debug.Log("Collider settings recalled.");

				return saved;
			}

			return null;
		}

		public static PhysicsSettingsStore SaveClothSettings(WeightMapper weightMapper)
		{
			PhysicsSettings[] workingSettings = new PhysicsSettings[weightMapper.settings.Length];
			for (int i = 0; i < weightMapper.settings.Length; i++)
			{
				workingSettings[i] = new PhysicsSettings(weightMapper.settings[i]);
			}

			PhysicsSettingsStore settings = TryFindSettingsObject(weightMapper);

			if (settings)
			{
				if (settings.clothSettings == null) settings.clothSettings = new List<PhysicsSettings>();
				foreach (PhysicsSettings s in workingSettings)
				{
					if (TryGetSavedIndex(settings.clothSettings, s, out int idx))
					{
						settings.clothSettings[idx].Copy(s);
					}
					else
					{
						settings.clothSettings.Add(s);
					}
				}

				EditorUtility.SetDirty(settings);
#if UNITY_2021_2_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
#else
				AssetDatabase.SaveAssets();
#endif


				Debug.Log("Cloth physics settings stored.");

				return settings;
			}

			return null;
		}

		public static PhysicsSettingsStore RecallClothSettings(WeightMapper weightMapper)
		{
			PhysicsSettingsStore savedFile = TryFindSettingsObject(weightMapper);

			if (savedFile)
			{
				foreach (PhysicsSettings s in savedFile.clothSettings)
				{
					int index = weightMapper.settings.ToList().FindIndex(x => x.name.Equals(s.name));
					if (index != -1)
					{
						weightMapper.settings[index] = s;
					}
				}

				weightMapper.ApplyWeightMap();

				Debug.Log("Cloth physics settings recalled.");

				return savedFile;
			}

			return null;
		}

		public static bool EnsureAssetsFolderExists(string folder)
		{
			if (string.IsNullOrEmpty(folder)) return true;
			if (AssetDatabase.IsValidFolder(folder)) return true;
			if (folder.Equals("Assets", System.StringComparison.InvariantCultureIgnoreCase)) return true;

			string parentFolder = Path.GetDirectoryName(folder);
			string folderName = Path.GetFileName(folder);

			if (EnsureAssetsFolderExists(parentFolder))
			{
				AssetDatabase.CreateFolder(parentFolder, folderName);
				return true;
			}

			return false;
		}

		public static PhysicsSettingsStore TryFindSettingsObject(Object obj)
		{
			string assetPath = GetSettingsStorePath(obj);

			if (!string.IsNullOrEmpty(assetPath))
			{
				PhysicsSettingsStore settingsStore = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(assetPath);

				if (!settingsStore)
				{
					settingsStore = CreateInstance<PhysicsSettingsStore>();
					EnsureAssetsFolderExists(Path.GetDirectoryName(assetPath));
					AssetDatabase.CreateAsset(settingsStore, assetPath);
				}

				return settingsStore;
			}

			Debug.LogError("Unable to open physics settings store for character.");

			return null;
		}

		private static bool TryGetSavedIndex(List<PhysicsSettings> savedClothSettings, PhysicsSettings workingSetting, out int savedIndex)
		{
			savedIndex = -1;
			if (savedClothSettings == null) return false;

			bool match = true;
			savedIndex = savedClothSettings.FindIndex(s => s.name.Equals(workingSetting.name));
			if (savedIndex == -1)
				match = false;

			if (match)
				return true;

			return false;
		}

		// additions
		/*
		public static void SaveAbstractColliderSettings(ColliderManager colliderManager)
		{
			List<ColliderManager.AbstractCapsuleCollider> target = new List<ColliderManager.AbstractCapsuleCollider>();
			List<ColliderManager.AbstractCapsuleCollider> current = colliderManager.abstractedCapsuleColliders;

			foreach (ColliderManager.AbstractCapsuleCollider c in current)
			{
				target.Add(new ColliderManager.AbstractCapsuleCollider(null, c.transform.position, c.transform.rotation, c.height, c.radius, c.name, c.axis));
			}

			PhysicsSettingsStore settings = TryFindSettingsObject(colliderManager);
			if (settings)
			{
				settings.abstractColliderSettings = target;
				EditorUtility.SetDirty(settings);
#if UNITY_2021_2_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
#else
                AssetDatabase.SaveAssets();
#endif
				Debug.Log("Collider settings stored.");
			}
		}
		*/

		public static void SaveAbstractColliderSettings(Object prefab, List<ColliderManager.AbstractCapsuleCollider> abstractColliders = null, bool initialState = false)
		{
			if (abstractColliders == null)
			{
				GameObject go = prefab as GameObject;
				abstractColliders = go.GetComponent<ColliderManager>().abstractedCapsuleColliders;
			}
			List<ColliderManager.AbstractCapsuleCollider> target = new List<ColliderManager.AbstractCapsuleCollider>();

			foreach (ColliderManager.AbstractCapsuleCollider c in abstractColliders)
			{                
                target.Add(new ColliderManager.AbstractCapsuleCollider(null, c.transform.localPosition, c.transform.localRotation, c.height, c.radius, c.name, c.axis, c.isEnabled));
            }

			PhysicsSettingsStore settings = TryFindSettingsObject(prefab, initialState);

			if (settings)
			{
				settings.abstractColliderSettings = target;
				EditorUtility.SetDirty(settings);
#if UNITY_2021_2_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
#else
                AssetDatabase.SaveAssets();
#endif
				Debug.Log("Collider settings stored.");
			}
		}

		public static List<ColliderManager.AbstractCapsuleCollider> RecallAbstractColliderSettings(ColliderManager colliderManager, bool initialState = false)
		{
			PhysicsSettingsStore saved = TryFindSettingsObject(colliderManager, initialState);

			if (saved)
				return saved.abstractColliderSettings;

			return null;
		}

		public static void SaveGizmoState(ColliderManager colliderManager, ColliderManager.GizmoState gizmoState)
		{
			PhysicsSettingsStore settings = TryFindSettingsObject(colliderManager);
			if (settings)
			{
				settings.gizmoState = gizmoState;
				EditorUtility.SetDirty(settings);
#if UNITY_2021_2_OR_NEWER
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
#else
                AssetDatabase.SaveAssets();
#endif
				//Debug.Log("Gizmo state stored.");
			}
		}

		public static ColliderManager.GizmoState RecallGizmoState(ColliderManager colliderManager)
		{
            PhysicsSettingsStore saved = TryFindSettingsObject(colliderManager, false);

            if (saved)
                return saved.gizmoState;

            return null;
        }

        // original methods extended to accomodate different pathing for the 'initial collider state' save data
        // should be ok to replace the original methods and use [bool initialstate = false] as optional parameter
        public static PhysicsSettingsStore TryFindSettingsObject(Object obj, bool initialState)
        {
            string assetPath = GetSettingsStorePath(obj, initialState);
            if (!string.IsNullOrEmpty(assetPath))
            {
                PhysicsSettingsStore settingsStore = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(assetPath);

                if (!settingsStore)
                {
                    settingsStore = CreateInstance<PhysicsSettingsStore>();
                    EnsureAssetsFolderExists(Path.GetDirectoryName(assetPath));
                    AssetDatabase.CreateAsset(settingsStore, assetPath);
                }

                return settingsStore;
            }

            Debug.LogError("Unable to open physics settings store for character.");

            return null;
        }

        private static string GetSettingsStorePath(Object obj, bool initialState)
        {
            string guid = null;
            if (obj.GetType() == typeof(ColliderManager))
            {
                guid = ((ColliderManager)obj).characterGUID;
            }
            else if (obj.GetType() == typeof(WeightMapper))
            {
                guid = ((WeightMapper)obj).characterGUID;
            }

            string characterPath = null;
            if (!string.IsNullOrEmpty(guid))
            {
                characterPath = AssetDatabase.GUIDToAssetPath(guid);
            }
            else
            {
                Debug.LogWarning("Unable to determine character physics store path.\nPlease rebuild physics for this character to correct this.");
                return null;
            }

            string characterFolder;
            string characterName;
			if (!string.IsNullOrEmpty(characterPath))
			{
				characterFolder = Path.GetDirectoryName(characterPath);
				characterName = Path.GetFileNameWithoutExtension(characterPath);
                // determine pathing for the 'initial collider state' save data
                if (initialState)
					return Path.Combine(characterFolder, settingsDir, characterName + referenceSettingsDir, settingsFileName + settingsSuffix);
				else
					return Path.Combine(characterFolder, settingsDir, characterName, settingsFileName + settingsSuffix);
			}
            return null;
        }		
        // end of additions
#endif
    }
}
