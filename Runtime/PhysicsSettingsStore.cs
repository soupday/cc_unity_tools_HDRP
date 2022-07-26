using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
using PhysicsSettings = Reallusion.Import.WeightMapper.PhysicsSettings;
using ColliderSettings = Reallusion.Import.ColliderManager.ColliderSettings;

namespace Reallusion.Import
{
    [System.Serializable]
    public class PhysicsSettingsStore : ScriptableObject
    {
        public ColliderSettings[] colliderSettings;
        public List<PhysicsSettings> clothSettings;

		private const string settingsDir = "Settings";
		private const string settingsFileName = "PhysicsSettingsStore";
		private const string settingsSuffix = ".asset";

		private static string GetSettingsStorePath(Object obj)
		{
			string characterPath = GetCharacterPath(obj);
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
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
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
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(settings)));
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
			PhysicsSettingsStore settingsStore = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(assetPath);
			
			if (!settingsStore)
			{
				settingsStore = CreateInstance<PhysicsSettingsStore>();
				EnsureAssetsFolderExists(Path.GetDirectoryName(assetPath));
				AssetDatabase.CreateAsset(settingsStore, assetPath);
				return settingsStore;
			}

			return settingsStore;
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
	}
}
