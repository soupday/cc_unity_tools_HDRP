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
        private List<LODObject> lodSortObjects;
        private List<GameObject> lodInstances;
        private List<GameObject> toDelete;
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
                string prefabName = characterName + "_LOD.prefab";
                if (!string.IsNullOrEmpty(name)) prefabName = name + ".prefab";
                folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(objects[0]));

                // create LOD group and add lod instances
                MakeLODInstances(objects);

                // process the LOD instances, separate and sort the lod characters
                ProcessLODInstances();                

                // remap the bones and fill the LOD groups
                if (lodRoot && lod0BoneRoot)
                {
                    lodRoot.name = characterName;
                    GenerateBoneMap();
                    FillLODGroups();
                }                

                // Clean up
                CleanUp();
                
                string prefabPath = Path.Combine(folder, prefabName);
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(lodRoot, prefabPath);

                GameObject.DestroyImmediate(lodRoot);

                return prefabAsset;
            }

            return null;
        }

        private bool AddSortLODObject(GameObject lodLevelObj, GameObject boneRootSearch = null)
        {
            if (boneRootSearch == null) boneRootSearch = lodLevelObj;

            LODObject lodObject = new LODObject()
            {
                lodObject = lodLevelObj,
                polyCount = CountPolys(lodLevelObj),
                boneRoot = FindBoneRoot(boneRootSearch.transform),
            };

            // unparent from original container
            lodLevelObj.transform.parent = null;

            for (int i = 0; i < lodSortObjects.Count; i++)
            {
                if (lodObject.polyCount == lodSortObjects[i].polyCount)
                {
                    Util.LogWarn("LOD level with same poly count detected: skipping " + lodLevelObj.name);
                    return false;
                }
                if (lodObject.polyCount > lodSortObjects[i].polyCount)
                {
                    // insert largest first
                    lodSortObjects.Insert(i, lodObject);
                    return true;
                }
            }

            // add smallest last
            lodSortObjects.Add(lodObject);
            return true;
        }

        private void ProcessLODInstances()
        {
            lodSortObjects = new List<LODObject>(lodInstances.Count);
            toDelete = new List<GameObject>();
            
            foreach (GameObject lodObj in lodInstances)
            {
                string name = lodObj.name;
                int lodCount = RL.CountLODs(lodObj);

                // check there are no lod groups on this lod instance
                LODGroup lg = lodObj.GetComponent<LODGroup>();
                if (lg) Component.DestroyImmediate(lg);

                if (lodCount == 1)
                {                    
                    // add lod character instance to polycount sorted lod objects
                    AddSortLODObject(lodObj);
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
                                // move this LOD level into it's own lodContainer and add to sorted lod objects
                                GameObject lodContainer = new GameObject(name + "_LOD" + level.ToString());                                
                                r.transform.parent = lodContainer.transform;
                                AddSortLODObject(lodContainer, lodObj);
                            }
                        }
                        else
                        {
                            // assume any mesh without a _LOD<N> suffix is part of the original model (LOD0)                            
                            // leave these meshes in their original container and use this as the
                            // LOD0 root container and unparent this from the instance.
                            // (this lod0Container should have the animator and any physics components)
                            if (!lod0Container)
                            {
                                Animator lodAnimator = lodObj.GetComponentInChildren<Animator>();
                                if (lodAnimator)
                                {
                                    lod0Container = lodAnimator.gameObject;
                                    if (lodAnimator.transform != r.transform.parent)
                                        toDelete.Add(r.transform.parent.gameObject);
                                }
                            }
                        }
                    }

                    // Add the LOD0 container if found
                    if (lod0Container)
                    {                        
                        AddSortLODObject(lod0Container, lodObj);
                    }
                }
            }

            numLevels = lodSortObjects.Count;

            if (numLevels > 0)
            {
                // fetch the LOD0 bone root
                lod0BoneRoot = lodSortObjects[0].boneRoot;
                lodRoot = lodSortObjects[0].lodObject;

                // parent to lodRoot if needed
                if (lod0BoneRoot.parent != lodRoot.transform)
                {
                    lod0BoneRoot.parent = lodRoot.transform;
                }
            }
        }

        private void MakeLODInstances(Object[] lodCharacters)
        {
            lodInstances = new List<GameObject>(lodCharacters.Length);

            if (lodCharacters.Length > 0)
            {
                foreach (Object lodCharacter in lodCharacters)
                {
                    GameObject lodInstance = (GameObject)GameObject.Instantiate(lodCharacter);
                    lodInstance.name = lodCharacter.name;
                    lodInstances.Add(lodInstance);
                }
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
            GameObject lod0 = lodSortObjects[0].lodObject;

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

            foreach (LODObject lob in lodSortObjects) totalPolys += lob.polyCount;            

            foreach (LODObject lob in lodSortObjects)
            {                                
                List<Renderer> lodRenderers = new List<Renderer>();
                Renderer[] renderers = lob.lodObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    RemapLODMesh(r.gameObject);
                    lodRenderers.Add(r);
                    if (r.gameObject.transform.parent != lodRoot.transform)
                    {
                        r.gameObject.transform.parent = lodRoot.transform;
                    }
                }
                processedPolys += lob.polyCount;
                // distribute transition sizes by the square root of processed polygon density
                float transitionSize = Mathf.Sqrt(1f - ((float)processedPolys / (float)totalPolys));                
                if (level == numLevels - 1) transitionSize = 0f;
                lods[level] = new LOD(transitionSize, lodRenderers.ToArray());
                level++;                
            }

            lodGroup = lodRoot.GetComponent<LODGroup>();
            if (lodGroup == null) lodGroup = lodRoot.AddComponent<LODGroup>();
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
                Transform rootBone = smr.rootBone;
                Transform[] newBones = new Transform[smr.bones.Length];
                for (int i = 0; i < smr.bones.Length; ++i)
                {
                    newBones[i] = GetBoneMapping(smr.bones[i]);
                }
                smr.bones = newBones;
                smr.rootBone = GetBoneMapping(rootBone);
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
            foreach (LODObject lob in lodSortObjects)
            {
                if (lob.lodObject != lodRoot) GameObject.DestroyImmediate(lob.lodObject);
            }

            foreach (GameObject obj in lodInstances)
            {
                if (obj != lodRoot) GameObject.DestroyImmediate(obj);
            }

            foreach (GameObject obj in toDelete)
            {
                if (obj != lodRoot) GameObject.DestroyImmediate(obj);
            }
        }
    }
}