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
using System.IO;

namespace Reallusion.Import
{
    public class Lodify
    {
        private Transform lod0BoneRoot;
        private GameObject lodRoot;
        private LODGroup lodGroup;
        private string characterName;
        private List<LODObject> lodObjects;
        private List<GameObject> lodInstances;
        private int numLevels;
        private Dictionary<string, Transform> boneMap;        
        private string folder;        

        public struct LODObject
        {
            public GameObject lodObject;
            public int polyCount;
            public Transform boneRoot;
        }

        /*
        [MenuItem("Reallusion/LOD Groups/Make LOD Prefab", priority = 1)]
        public static void MenuMakeLodPrefab()
        {
            WindowManager.HideAnimationPlayer(true);
            Lodify l = new Lodify();
            GameObject lodPrefab = l.MakeLODPrefab(Selection.objects);
            if (lodPrefab && WindowManager.IsPreviewScene)
            {
                WindowManager.previewScene.ShowPreviewCharacter(lodPrefab);
            }
            Selection.activeObject = lodPrefab;
        }
        */

        public GameObject MakeLODPrefab(Object[] objects, string name = "")
        {
            if (objects.Length > 0)
            {
                // determine character name and prefab folder path
                characterName = objects[0].name;
                string prefabName = characterName + "_LODGroup.prefab";
                if (!string.IsNullOrEmpty(name)) prefabName = name + ".prefab";
                folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(objects[0]));

                // create LOD group and add lod instances
                AddLODInstances(objects);

                // process the LOD instances, separate and sort the lod characters
                ProcessLODInstances();

                // remap the bones and fill the LOD groups
                if (lod0BoneRoot)
                {
                    GenerateBoneMap();
                    FillLODGroups();
                }

                // finally copy the LOD0 animator settings
                ProcessAnimators();

                // Clean up
                CleanUp();

                string prefabPath = Path.Combine(folder, prefabName);
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(lodRoot, prefabPath);

                GameObject.DestroyImmediate(lodRoot);

                return prefabAsset;
            }

            return null;
        }

        private void AddLODObject(GameObject lodObj, GameObject boneRootSearch = null)
        {
            if (boneRootSearch == null) boneRootSearch = lodObj;

            LODObject lodObject = new LODObject()
            {
                lodObject = lodObj,
                polyCount = CountPolys(lodObj),
                boneRoot = FindBoneRoot(boneRootSearch.transform),
            };
            for (int i = 0; i < lodObjects.Count; i++)
            {
                if (lodObject.polyCount == lodObjects[i].polyCount)
                {
                    Util.LogWarn("LOD level with same poly count detected: skipping " + lodObj.name);
                    return;
                }
                if (lodObject.polyCount > lodObjects[i].polyCount)
                {
                    lodObjects.Insert(i, lodObject);
                    return;
                }
            }
            lodObjects.Add(lodObject);
        }

        private void ProcessLODInstances()
        {
            lodObjects = new List<LODObject>(lodInstances.Count);
            
            foreach (GameObject lodObj in lodInstances)
            {
                string name = lodObj.name;
                int lodCount = RL.CountLODs(lodObj);

                // check there are no lod groups on this lod instance
                LODGroup lg = lodObj.GetComponent<LODGroup>();
                if (lg) Component.DestroyImmediate(lg);

                if (lodCount == 1)
                {                    
                    // add lod instance to polycount sorted lod objects
                    AddLODObject(lodObj);
                }
                else if (lodCount > 1)
                {
                    // for instalod exported characters LOD0 is a collection of all the unsuffixed meshes
                    // LOD levels 1-5 are suffixed _LOD1 - _LOD5

                    Renderer[] renderers = lodObj.GetComponentsInChildren<Renderer>(true);
                    GameObject lod0Container = null;
                    foreach (Renderer r in renderers)
                    {
                        int index = r.name.LastIndexOf("_LOD");
                        if (index >= 0 && r.name.Length - index == 5 && char.IsDigit(r.name[r.name.Length - 1]))
                        {
                            // any mesh with a _LOD<N> suffix is a LOD level
                            string levelString = r.name.Substring(r.name.Length - 1, 1);
                            if (int.TryParse(levelString, out int level))
                            {
                                // move this LOD level into it's own lodContainer (child of the LODGroup)
                                GameObject lodContainer = new GameObject(name + "_LOD" + level.ToString());
                                lodContainer.transform.parent = lodRoot.transform;
                                lodContainer.transform.localPosition = Vector3.zero;
                                lodContainer.transform.localRotation = Quaternion.identity;
                                CopyAnimator(lodObj, lodContainer);
                                r.transform.parent = lodContainer.transform;                                
                                AddLODObject(lodContainer, lodObj);
                            }
                        }
                        else
                        {
                            // assume any mesh without a _LOD<N> suffix is part of the original model (LOD0)                            
                            // move this LOD level into it's own lod0Container (child of the LODGroup)
                            if (!lod0Container)
                            {
                                lod0Container = new GameObject(name + "_LOD0");
                                lod0Container.transform.parent = lodRoot.transform;
                                lod0Container.transform.localPosition = Vector3.zero;
                                lod0Container.transform.localRotation = Quaternion.identity;
                                CopyAnimator(lodObj, lod0Container);
                            }
                            r.transform.parent = lod0Container.transform;
                        }
                    }

                    // Add the LOD0 container
                    if (lod0Container)
                    {
                        AddLODObject(lod0Container, lodObj);                        
                    }                    
                }

            }

            numLevels = lodObjects.Count;

            if (numLevels > 0)
            {              
                // fetch the LOD0 bone root
                lod0BoneRoot = lodObjects[0].boneRoot;

                // move to lodRoot
                lod0BoneRoot.parent = lodRoot.transform;                
            }
        }

        private void AddLODInstances(Object[] objects)
        {
            lodRoot = new GameObject(characterName);
            lodGroup = lodRoot.AddComponent<LODGroup>();
            lodInstances = new List<GameObject>(objects.Length);

            foreach (Object obj in objects)
            {
                GameObject lodInstance = (GameObject)GameObject.Instantiate(obj, lodRoot.transform);
                lodInstance.name = obj.name;
                lodInstances.Add(lodInstance);

            }            
        }

        private void CopyAnimator(GameObject from, GameObject to)
        {
            Animator fromAnimator = from.GetComponentInChildren<Animator>();
            if (fromAnimator)
            {
                Animator toAnimator = to.GetComponent<Animator>();
                if (!toAnimator) toAnimator = to.AddComponent<Animator>();

                toAnimator.avatar = fromAnimator.avatar;
                toAnimator.runtimeAnimatorController = fromAnimator.runtimeAnimatorController;
                toAnimator.applyRootMotion = fromAnimator.applyRootMotion;
                toAnimator.updateMode = fromAnimator.updateMode;
                toAnimator.cullingMode = fromAnimator.cullingMode;
            }
        }

        private void ProcessAnimators()
        {
            GameObject lod0 = lodObjects[0].lodObject;

            // find and copy the lod0 animator settings
            if (lod0)
            {
                CopyAnimator(lod0, lodRoot);                
            }            
        }

        private void FillLODGroups()
        {
            LOD[] lods = new LOD[numLevels];
            int level = 0;                        
            int totalPolys = 0;
            int processedPolys = 0;

            foreach (LODObject lob in lodObjects) totalPolys += lob.polyCount;            

            foreach (LODObject lob in lodObjects)
            {                
                GameObject lodInstance = lob.lodObject;
                List<Renderer> lodRenderers = new List<Renderer>();
                Renderer[] renderers = lodInstance.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    RemapLODMesh(r.gameObject);
                    lodRenderers.Add(r);
                    r.gameObject.transform.parent = lodRoot.transform;
                }
                processedPolys += lob.polyCount;
                // distribute transition sizes by the square root of processed polygon density
                float transitionSize = Mathf.Sqrt(1f - ((float)processedPolys / (float)totalPolys));                
                if (level == numLevels - 1) transitionSize = 0f;
                lods[level] = new LOD(transitionSize, lodRenderers.ToArray());
                level++;                
            }

            lodGroup.SetLODs(lods);
        }

        private Transform FindBoneRoot(Transform root)
        {
            Transform boneRoot = Util.FindChildRecursive(root, "CC_Base_BoneRoot");
            if (!boneRoot) boneRoot = Util.FindChildRecursive(root, "RL_BoneRoot");
            if (!boneRoot) boneRoot = Util.FindChildRecursive(root, "Rigify_BoneRoot");
            if (!boneRoot) boneRoot = Util.FindChildRecursive(root, "root");
            if (!boneRoot) Debug.LogError("Could not find bone root transform from: " + root);
            return boneRoot;
        }

        private void GenerateBoneMap()
        {
            boneMap = new Dictionary<string, Transform>();
            Transform[] bones = lod0BoneRoot.GetComponentsInChildren<Transform>();

            foreach (Transform bone in bones)
            {
                boneMap[bone.gameObject.name] = bone;
            }
        }

        private Transform GetBoneMapping(Transform lodBone)
        {
            string boneName = lodBone.name;

            // look for this bone in the bone mappings
            if (boneMap.TryGetValue(boneName, out Transform lod0Bone)) return lod0Bone;

            // if the initial bone mapping fails, traverse up the bone heirarchy to find a matching parent bone            
            if (lodBone.parent) return GetBoneMapping(lodBone.parent);

            Debug.LogError("Unable to map bone: " + boneName + " to LOD root skeleton.");

            return null;
        }

        private void RemapLODMesh(GameObject gameObject)
        {
            SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (smr)
            {
                Transform[] newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; ++i)
                {
                    newBones[i] = GetBoneMapping(smr.bones[i]);
                }
                smr.bones = newBones;
            }
        }

        public static int CountPolys(GameObject asset)
        {
            MeshFilter[] meshFilters = asset.GetComponentsInChildren<MeshFilter>();
            SkinnedMeshRenderer[] skinnedMeshRenderers = asset.GetComponentsInChildren<SkinnedMeshRenderer>();

            int count = 0;

            foreach (MeshFilter mf in meshFilters)
            {
                Mesh m = mf.sharedMesh;
                count += m.triangles.Length / 3;
            }

            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                Mesh m = smr.sharedMesh;
                count += m.triangles.Length / 3;
            }

            return count;
        }

        private void CleanUp()
        {   
            // remove all the old lod object containers
            foreach (LODObject lob in lodObjects)
            {
                GameObject.DestroyImmediate(lob.lodObject);
            }

            foreach (GameObject obj in lodInstances)
            {
                GameObject.DestroyImmediate(obj);
            }
        }
    }
}