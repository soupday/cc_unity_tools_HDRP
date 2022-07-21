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

		private const string settingsDir = "Assets";
		private const string settingsFileName = "PhysicsSettingsStore";
		private const string settingsSuffix = ".asset";

		public static PhysicsSettingsStore SaveColliderSettings(ColliderManager colliderManager)
		{
			bool settingsObjectFound = TryFindSettingsObject(out string foundSettingsGuid);
			string settingsPath;
			if (settingsObjectFound)
			{
				settingsPath = AssetDatabase.GUIDToAssetPath(foundSettingsGuid);
				PhysicsSettingsStore settings = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(settingsPath);
				settings.colliderSettings = colliderManager.settings;
				EditorUtility.SetDirty(settings);
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(settingsPath));
				return settings;
			}
			else
			{
				settingsPath = settingsDir + "/" + settingsFileName + settingsSuffix;
				PhysicsSettingsStore settings = CreateInstance<PhysicsSettingsStore>();
				settings.colliderSettings = colliderManager.settings;
				AssetDatabase.CreateAsset(settings, settingsPath);
				return settings;
			}
		}

		public static PhysicsSettingsStore RecallColliderSettings(ColliderManager colliderManager)
		{
			bool settingsObjectFound = TryFindSettingsObject(out string foundSettingsGuid);

			if (settingsObjectFound)
			{
				string settingsPath = AssetDatabase.GUIDToAssetPath(foundSettingsGuid);
				PhysicsSettingsStore saved = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(settingsPath);
				if (saved)
				{
					ColliderSettings[] tmp = saved.colliderSettings;
					foreach (ColliderSettings c in tmp)
					{
						foreach (ColliderSettings s in colliderManager.settings)
						{
							if (s.name == c.name)
							{
								s.radius = c.radius;
								s.height = c.height;
								s.xAdjust = c.xAdjust;
								s.yAdjust = c.yAdjust;
								s.zAdjust = c.zAdjust;
								s.radiusAdjust = c.radiusAdjust;
								s.heightAdjust = c.heightAdjust;
								s.position = c.position;
								s.Update();
							}
						}
					}

					colliderManager.UpdateColliders();

					return saved;
				}
			}

			return null;
		}



		public static PhysicsSettingsStore SaveClothSettings(WeightMapper weightMapper)
		{
			string settingsPath;

			PhysicsSettings[] workingSettings = new PhysicsSettings[weightMapper.settings.Length];
			System.Array.Copy(weightMapper.settings, workingSettings, workingSettings.Length);

			if (TryFindSettingsObject(out string foundSettingsGuid))
			{
				settingsPath = AssetDatabase.GUIDToAssetPath(foundSettingsGuid);
				PhysicsSettingsStore settings = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(settingsPath);

				if (settings.clothSettings == null) settings.clothSettings = new List<PhysicsSettings>();
				foreach (PhysicsSettings s in workingSettings)
				{
					if (TryGetSavedIndex(settings.clothSettings, s, out int idx))
					{
						settings.clothSettings[idx] = s;
					}
					else
					{
						settings.clothSettings.Add(s);
					}
				}
				EditorUtility.SetDirty(settings);
				AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(settingsPath));
				return settings;
			}
			else
			{
				settingsPath = settingsDir + "/" + settingsFileName + settingsSuffix;
				PhysicsSettingsStore settings = CreateInstance<PhysicsSettingsStore>();
				AssetDatabase.CreateAsset(settings, settingsPath);
				settings.clothSettings = new List<PhysicsSettings>();
				foreach (PhysicsSettings s in workingSettings)
				{
					settings.clothSettings.Add(s);
				}
				return settings;
			}
		}

		public static PhysicsSettingsStore RecallClothSettings(WeightMapper weightMapper)
		{
			bool settingsObjectFound = TryFindSettingsObject(out string foundSettingsGuid);

			if (settingsObjectFound)
			{
				string settingsPath = AssetDatabase.GUIDToAssetPath(foundSettingsGuid);
				if ((PhysicsSettingsStore)AssetDatabase.LoadAssetAtPath(settingsPath, typeof(PhysicsSettingsStore)))
				{
					PhysicsSettingsStore savedFile = AssetDatabase.LoadAssetAtPath<PhysicsSettingsStore>(settingsPath);

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
				}				
			}

			return null;
		}



		public static bool TryFindSettingsObject(out string foundSettingsGuid)
		{
			string[] folders = new string[] { "Assets" };
			string[] guids = AssetDatabase.FindAssets(settingsFileName, folders);

			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				string assetName = Path.GetFileNameWithoutExtension(assetPath);
				if (assetName.Equals(settingsFileName, System.StringComparison.InvariantCultureIgnoreCase))
				{
					if (Path.GetExtension(assetPath) != ".cs")
					{
						foundSettingsGuid = guid;
						return true;
					}
				}
			}
			foundSettingsGuid = "Not Found";
			return false;
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
