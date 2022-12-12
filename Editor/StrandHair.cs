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

#if UNITY_ALEMBIC_1_0_7 && HAS_PACKAGE_DIGITAL_HUMAN && HAS_PACKAGE_STRAND_HAIR

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Formats.Alembic.Importer;
using Unity.DemoTeam.Hair;
using Unity.DemoTeam.DigitalHuman;
using Object = UnityEngine.Object;

namespace Reallusion.Import
{
    public class StrandHair
    {
        private string hairName;
        private string hairFolder;
        private TextAsset hairTextAsset;
        private QuickJSON jsonData;
        private QuickJSON jsonObjectsData;        
        private GameObject prefabInstance;
        private string prefabPath;


        [MenuItem("Development/Strand Hair Tools/Build Hair")]
        public static void MenuBuildHair()
        {
            string jsonPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            StrandHair sh = new StrandHair(jsonPath);
            WindowManager.HideAnimationPlayer(true);
            GameObject prefabAsset = sh.Import();            
        }

        public StrandHair(TextAsset hairJsonTextAsset)
        {
            hairTextAsset = hairJsonTextAsset;
            string jsonPath = AssetDatabase.GetAssetPath(hairJsonTextAsset);
            hairName = Path.GetFileNameWithoutExtension(jsonPath);
            hairFolder = Path.GetDirectoryName(jsonPath);
        }

        public StrandHair(string jsonPath)
        {
            hairTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            hairName = Path.GetFileNameWithoutExtension(jsonPath);
            hairFolder = Path.GetDirectoryName(jsonPath);
        }

        public GameObject Import()
        {
            if (ImporterWindow.Current)
            {                
                CharacterInfo info = ImporterWindow.Current.Character;

                GameObject prefabAsset = info.PrefabAsset;
                prefabInstance = info.GetPrefabInstance();

                bool doneSomething = false;

                if (prefabAsset && prefabInstance)
                {
                    jsonData = new QuickJSON(hairTextAsset.text);
                    jsonObjectsData = jsonData.GetObjectAtPath("Hair/Objects");
                    prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
                    int index = 0;

                    foreach (MultiValue mvo in jsonObjectsData.values)
                    {
                        string objectName = mvo.Key;
                        QuickJSON groupsJson = mvo.ObjectValue.GetObjectAtPath("Groups");
                        Transform parentObject = prefabInstance.transform.Find(objectName);

                        List<HairAsset> hairAssets = new List<HairAsset>();

                        foreach (MultiValue mvg in groupsJson.values)
                        {
                            string groupName = mvg.Key;
                            QuickJSON groupJson = mvg.ObjectValue;
                            string alembicPath = Path.Combine(hairFolder, groupJson.GetStringValue("File"));
                            string assetName = Path.GetFileNameWithoutExtension(alembicPath);

                            HairAsset hairAsset = CreateHairAsset(assetName);
                            SetupHairAsset(hairAsset, alembicPath);
                            hairAssets.Add(hairAsset);                            
                        }

                        HairInstance hairInstance = AddHairInstance(prefabInstance.transform, hairAssets);
                        AttachToSkin(parentObject, hairName + "_" + index.ToString(), hairInstance);
                        doneSomething = true;
                        index++;
                    }
                }

                if (doneSomething)
                {
                    //prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets" + Path.DirectorySeparatorChar + "test.prefab");
                    //PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.UserAction);
                    //PrefabUtility.SavePrefabAsset(prefabAsset);
                    WindowManager.onTimer += SavePrefabDelayed;
                    WindowManager.StartTimer(1f);
                }

                //GameObject.DestroyImmediate(prefabInstance);

                return prefabAsset;
            }

            return null;
        }

        public void SavePrefabDelayed()
        {
            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.UserAction);
            GameObject.DestroyImmediate(prefabInstance);
            WindowManager.onTimer -= SavePrefabDelayed;
        }
               

        private HairAsset CreateHairAsset(string assetName)
        {
            string hairAssetPath = Path.Combine(hairFolder, assetName + "_hair.asset");
            HairAsset hairAsset = ScriptableObject.CreateInstance<HairAsset>();
            AssetDatabase.CreateAsset(hairAsset, hairAssetPath);
            return hairAsset;
        }

        private void SetupHairAsset(HairAsset hairAsset, string alembicPath)
        {
            hairAsset.settingsBasic.type = HairAsset.Type.Alembic;            

            AlembicStreamPlayer abcPrefab = AssetDatabase.LoadAssetAtPath<AlembicStreamPlayer>(alembicPath);
            hairAsset.settingsAlembic.alembicAsset = abcPrefab;

            HairAssetBuilder.BuildHairAsset(hairAsset);
        }

        private HairInstance AddHairInstance(Transform root, List<HairAsset> hairAssets)
        {
            // remove existing hair instance
            HairInstance hairInstance = root.gameObject.GetComponent<HairInstance>();
            if (hairInstance) Component.DestroyImmediate(hairInstance);

            // add new hair instance
            hairInstance = root.gameObject.AddComponent<HairInstance>();
            
            // add hair assets
            HairInstance.GroupProvider[] prov = new HairInstance.GroupProvider[hairAssets.Count];
            for (int i = 0; i < hairAssets.Count; i++)
            {
                prov[i].hairAsset = hairAssets[i];
            }
            hairInstance.strandGroupProviders = prov;
            hairInstance.strandGroupDefaults.settingsSolver.globalPosition = true;            
            hairInstance.strandGroupDefaults.settingsSolver.globalPositionInfluence = 1f;
            hairInstance.strandGroupDefaults.settingsSolver.globalFade = true;
            hairInstance.strandGroupDefaults.settingsSolver.globalFadeOffset = 0.25f;
            hairInstance.strandGroupDefaults.settingsSolver.globalFadeExtent = 0.5f;

            return hairInstance;
        }
        
        private void AttachToSkin(Transform parent, string assetName, HairInstance hairInstance)
        {                        
            string skinAssetPath = Path.Combine(hairFolder, assetName + "_skin.asset");

            //make a skin attachment data asset
            SkinAttachmentData skinAsset = ScriptableObject.CreateInstance<SkinAttachmentData>();
            AssetDatabase.CreateAsset(skinAsset, skinAssetPath);

            SkinAttachmentTarget skinAttachmentTarget;
            SkinAttachment skinAttachment;
            skinAttachmentTarget = parent.gameObject.AddComponent<SkinAttachmentTarget>();
            skinAttachmentTarget.attachData = skinAsset;
            
            skinAttachment = hairInstance.gameObject.AddComponent<SkinAttachment>();
            skinAttachment.attachmentMode = SkinAttachment.AttachmentMode.BuildPoses;
            skinAttachment.attachmentType = SkinAttachment.AttachmentType.MeshRoots;
            skinAttachment.target = skinAttachmentTarget;
            hairInstance.strandGroupDefaults.settingsSkinning.rootsAttach = true;
            hairInstance.strandGroupDefaults.settingsSkinning.rootsAttachTarget = skinAttachmentTarget;            

            skinAttachment.Attach(storePositionRotation: true); // attach button
            skinAttachmentTarget.CommitSubjects(); // rebuild button
        } 
    }
}

#endif