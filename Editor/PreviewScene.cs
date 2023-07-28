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

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_3_1_1
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Reallusion.Import
{
    public struct PreviewScene
    {
        UnityEngine.SceneManagement.Scene scene;
        Transform container;
        Transform stage;
        Transform lighting;
        Transform character;
        Transform baked;
        Transform camera;

        public bool IsValidPreviewScene { get { return scene.IsValid() && container && stage && lighting && character; } }
        public Scene SceneHandle { get { return scene; } }

        public Transform GetCamera()
        {
            if (!camera)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                if (cameraObject)
                    camera = cameraObject.transform;
            }

            if (!camera)
            {
#if UNITY_2023_OR_NEWER
                Camera[] cams = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
                Camera[] cams = GameObject.FindObjectsOfType<Camera>();
#endif
                foreach (Camera cam in cams)
                {
                    if (cam.isActiveAndEnabled)
                    {
                        return cam.transform;
                    }
                }
            }

            return camera;
        }        

        public static PreviewScene FetchPreviewScene(Scene scene)
        {
            if (!scene.IsValid()) scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            PreviewScene ps = new PreviewScene();
            ps.scene = scene;
            ps.container = GameObject.Find("Preview Scene Container")?.transform;
            ps.character = GameObject.Find("Character Container")?.transform;
            ps.baked = GameObject.Find("Baked Character Container")?.transform;
            ps.stage = GameObject.Find("Stage")?.transform;
            ps.lighting = GameObject.Find("Lighting")?.transform;            
            ps.camera = GameObject.Find("Main Camera")?.transform;            
            return ps;
        }        
        
        public static void CycleLighting()
        {
            if (WindowManager.IsPreviewScene)
            {
                PreviewScene ps = WindowManager.GetPreviewScene();

                List<GameObject> lightingContainers = new List<GameObject>();
                Util.FindSceneObjects(ps.lighting, "LightingConfig", lightingContainers);

                int active = 0;
                for (int i = 0; i < lightingContainers.Count; i++)
                {
                    if (lightingContainers[i].activeSelf) active = i;
                    lightingContainers[i].SetActive(false);
                }

                active++;
                if (active >= lightingContainers.Count) active = 0;

                lightingContainers[active].SetActive(true);

                EditorPrefs.SetString("RL_Lighting_Preset", lightingContainers[active].name);
            }
        }

        public static void RestoreLighting()
        {
            if (EditorPrefs.HasKey("RL_Lighting_Preset"))
            {
                string presetName = EditorPrefs.GetString("RL_Lighting_Preset");

                PreviewScene ps = WindowManager.GetPreviewScene();

                List<GameObject> lightingContainers = new List<GameObject>();
                Util.FindSceneObjects(ps.lighting, "LightingConfig", lightingContainers);

                bool found = false;
                for (int i = 0; i < lightingContainers.Count; i++)
                {
                    if (lightingContainers[i].name == presetName)
                    {
                        lightingContainers[i].SetActive(true);
                        found = true;
                    }
                    else
                    {
                        lightingContainers[i].SetActive(false);
                    }
                }

                if (!found)
                {
                    lightingContainers[0].SetActive(true);
                    EditorPrefs.SetString("RL_Lighting_Preset", lightingContainers[0].name);
                }
            }
        }

        public static void PokeLighting()
        {
            PreviewScene ps = WindowManager.GetPreviewScene();

            List<GameObject> lightingContainers = new List<GameObject>();
            Util.FindSceneObjects(ps.lighting, "LightingConfig", lightingContainers);

            for (int i = 0; i < lightingContainers.Count; i++)
            {
                if (lightingContainers[i].activeInHierarchy)
                {
                    lightingContainers[i].SetActive(false);
                    lightingContainers[i].SetActive(true);
                }                        
            }                
        }

        public GameObject GetPreviewCharacter()
        {
            if (character.transform.childCount > 0)
            {
                return character.transform.GetChild(0).gameObject;
            }

            return null;
        }

        public GameObject GetBakedCharacter()
        {
            if (baked.transform.childCount > 0)
            {
                return baked.transform.GetChild(0).gameObject;
            }

            return null;
        }

        public void ClearCharacter()
        {
            if (character)
            {
                GameObject[] children = new GameObject[character.childCount];

                for (int i = 0; i < character.childCount; i++)
                {
                    children[i] = character.GetChild(i).gameObject;
                }

                foreach (GameObject child in children)
                {
                    GameObject.DestroyImmediate(child);
                }
            }
        }

        public void ClearBaked()
        {
            if (baked)
            {
                GameObject[] children = new GameObject[baked.childCount];

                for (int i = 0; i < baked.childCount; i++)
                {
                    children[i] = baked.GetChild(i).gameObject;
                }

                foreach (GameObject child in children)
                {
                    GameObject.DestroyImmediate(child);
                }
            }
        }

        public GameObject ShowPreviewCharacter(GameObject fbxAsset)
        {
            if (!fbxAsset) return null;
            GameObject prefabAsset = Util.FindCharacterPrefabAsset(fbxAsset);

            if (character)
            {
                ClearCharacter();

                GameObject clone = PrefabUtility.InstantiatePrefab(prefabAsset ? prefabAsset : fbxAsset, character.transform) as GameObject;
                if (clone)
                {
                    Selection.activeGameObject = clone;                    

                    return clone;
                }
            }

            return null;
        }

        public GameObject ShowBakedCharacter(GameObject bakedAsset)
        {
            if (!bakedAsset) return null;            

            ClearBaked();

            GameObject clone = PrefabUtility.InstantiatePrefab(bakedAsset, baked.transform) as GameObject;
            if (clone)
            {
                Selection.activeGameObject = clone;
                return clone;
            }

            return null;
        }
    

        public GameObject UpdatePreviewCharacter(GameObject prefabAsset)
        {
            if (!prefabAsset) return null;            

            ClearCharacter();
            
            GameObject clone = PrefabUtility.InstantiatePrefab(prefabAsset, character.transform) as GameObject;
            if (clone)
            {
                Util.LogInfo("Replacing prefab with new generated prefab...");
                Selection.activeGameObject = clone;
                return clone;
            }

            return null;
        }

        public void PostProcessingAndLighting()
        {
            Util.LogInfo("PostProcessingAndLighting");
            if (Pipeline.is3D || Pipeline.isURP)
            {
                Material skybox = (Material)Util.FindAsset("RL Preview Gradient Skybox");
                if (skybox)
                {
                    RenderSettings.skybox = skybox;
                    if (Pipeline.is3D) RenderSettings.ambientIntensity = 1.0f;
                }
            }

#if UNITY_POST_PROCESSING_3_1_1
            if (Pipeline.is3D)
            {
                PostProcessLayer ppl = camera.gameObject.AddComponent<PostProcessLayer>();
                PostProcessVolume ppv = camera.gameObject.AddComponent<PostProcessVolume>();
                PostProcessProfile volume = (PostProcessProfile)Util.FindAsset("RL Preview Scene Post Processing Volume Profile 3.1.1");
                ppl.volumeTrigger = camera.transform;
                LayerMask everything = ~0;
                ppl.volumeLayer = everything;
                ppl.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;                
                ppv.isGlobal = true;
                //ppv.profile = volume;
                ppv.sharedProfile = volume;
            }
#endif

            RestoreLighting();
        }
    }
}
