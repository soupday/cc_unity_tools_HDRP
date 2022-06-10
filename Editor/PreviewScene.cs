using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        public bool IsValid { get { return scene.IsValid(); } }                

        public Transform GetCamera()
        {
            if (!camera) camera = GameObject.Find("Main Camera")?.transform;

            if (!camera)
            {
                Camera[] cams = GameObject.FindObjectsOfType<Camera>();
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
                Debug.Log("Replacing prefab with new generated prefab...");
                Selection.activeGameObject = clone;
                return clone;
            }

            return null;
        }

        public void PostProcessingAndLighting()
        {
            if (Pipeline.is3D || Pipeline.isURP)
            {
                Material skybox = (Material)Util.FindAsset("RL Preview Gradient Skybox");
                if (skybox)
                {
                    RenderSettings.skybox = skybox;
                    if (Pipeline.is3D) RenderSettings.ambientIntensity = 1.25f;
                }
            }

#if UNITY_POST_PROCESSING_3_1_1
            PostProcessLayer ppl = camera.gameObject.AddComponent<PostProcessLayer>();
            PostProcessVolume ppv = camera.gameObject.AddComponent<PostProcessVolume>();
            PostProcessProfile volume = (PostProcessProfile)Util.FindAsset("RL Preview Scene Post Processing Volume Profile 3.1.1");
            ppl.volumeTrigger = camera.transform;
            LayerMask everything = ~0;
            ppl.volumeLayer = everything;
            ppl.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            ppv.isGlobal = true;
            ppv.profile = volume;
#endif
        }
    }
}
