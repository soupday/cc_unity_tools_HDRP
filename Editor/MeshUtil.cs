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

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

namespace Reallusion.Import
{
    public static class MeshUtil
    {
        public const string INVERTED_FOLDER_NAME = "Inverted Meshes";
        public const string PRUNED_FOLDER_NAME = "Pruned Meshes";
        public const string MESH_FOLDER_NAME = "Meshes";

        [MenuItem("Reallusion/Mesh Tools/Reverse Triangle Order", priority = 100)]
        private static void DoReverse()
        {
            if (Selection.gameObjects.Length > 1)
                foreach (GameObject go in Selection.gameObjects)
                    ReverseTriangleOrder(go);
            else
                ReverseTriangleOrder(Selection.activeObject);
        }

        [MenuItem("Reallusion/Mesh Tools/Reverse Triangle Order", true)]
        private static bool ValidateDoReverse()
        {
            return Util.IsSavedPrefabInSelection();
        }

        [MenuItem("Reallusion/Mesh Tools/Prune Blend Shapes", priority = 101)]
        private static void DoPrune()
        {            
            if (Selection.gameObjects.Length > 1)
                foreach (GameObject go in Selection.gameObjects)
                    PruneBlendShapes(go);
            else
                PruneBlendShapes(Selection.activeObject);
        }

        [MenuItem("Reallusion/Mesh Tools/Prune Blend Shapes", true)]
        private static bool ValidateDoPrune()
        {
            return Util.IsSavedPrefabInSelection();
        }

        [MenuItem("Reallusion/Mesh Tools/Auto Smooth Mesh", priority = 102)]
        private static void DoAutoSmoothMesh()
        {
            bool playerOpen = false;
            if (AnimPlayerGUI.IsPlayerShown())
            {
                WindowManager.HideAnimationPlayer(false);
                playerOpen = true;
            }

            if (Selection.gameObjects.Length > 1)
                foreach (GameObject go in Selection.gameObjects)
                    AutoSmoothMesh(go);
            else
                AutoSmoothMesh(Selection.activeObject);

            if (playerOpen)  
                WindowManager.ShowAnimationPlayer();
        }

        [MenuItem("Reallusion/Mesh Tools/Auto Smooth Mesh", true)]
        private static bool ValidateDoAutoSmoothMesh()
        {
            return Util.IsSavedPrefabInSelection();
        }

        [MenuItem("Reallusion/Mesh Tools/Open or Close Character Mouth", priority = 201)]
        private static void DoOpenCloseMouth()
        {
            CharacterOpenCloseMouth(Selection.activeObject);
        }

        [MenuItem("Reallusion/Mesh Tools/Open or Close Character Mouth", true)]
        private static bool ValidateDoOpenCloseMouth()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Open or Close Character Eyes", priority = 202)]
        private static void DoOpenCloseEyes()
        {
            CharacterOpenCloseEyes(Selection.activeObject);
        }

        [MenuItem("Reallusion/Mesh Tools/Open or Close Character Eyes", true)]
        private static bool ValidateDoOpenCloseEyes()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Left", priority = 203)]
        private static void DoLookLeft()
        {
            CharacterEyeLook(Selection.activeObject, EyeLookDir.Left);
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Left", true)]
        private static bool ValidateDoLookLeft()
        {            
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Right", priority = 204)]
        private static void DoLookRight()
        {
            CharacterEyeLook(Selection.activeObject, EyeLookDir.Right);
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Right", true)]
        private static bool ValidateDoLookRight()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Up", priority = 205)]
        private static void DoLookUp()
        {
            CharacterEyeLook(Selection.activeObject, EyeLookDir.Up);
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Up", true)]
        private static bool ValidateDoLookUp()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Down", priority = 206)]
        private static void DoLookDown()
        {
            CharacterEyeLook(Selection.activeObject, EyeLookDir.Down);
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Down", true)]
        private static bool ValidateDoLookDown()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Forward", priority = 207)]
        private static void DoLookForward()
        {
            CharacterEyeLook(Selection.activeObject, EyeLookDir.None);
        }

        [MenuItem("Reallusion/Mesh Tools/Eye/Look Forward", true)]
        private static bool ValidateDoLookForward()
        {
            return WindowManager.IsPreviewScene && WindowManager.GetPreviewScene().GetPreviewCharacter() != null;
        }

        public static bool GetSourcePrefab(Object obj, string folderName, 
            out string characterName, out string meshFolder, out Object prefabObject)
        {
            characterName = "";
            meshFolder = "";
            prefabObject = null;

            if (!obj) return false;

            GameObject fbxAsset = Util.FindRootPrefabAssetFromSceneObject(obj);

            if (!fbxAsset)
            {
                Debug.LogWarning("Object: " + obj.name + " is not part of an imported CC3/4 character!");
                return false;
            }

            prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (!prefabObject || !AssetDatabase.GetAssetPath(prefabObject).iEndsWith(".prefab"))
            {
                Debug.LogWarning("Object: " + obj.name + " is not part of prefab asset!");
                return false;
            }            

            string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
            characterName = Path.GetFileNameWithoutExtension(fbxPath);
            string fbxFolder = Path.GetDirectoryName(fbxPath);
            meshFolder = Path.Combine(fbxFolder, folderName, characterName);

            return true;
        }

        public static Mesh GetMeshFrom(Object obj)
        {
            if (obj.GetType() == typeof(Mesh))
            {
                Mesh m = (Mesh)obj;
                if (m) return m;
            }

            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    Mesh m = go.GetComponent<Mesh>();
                    if (m) return m;
                    
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf)
                    {
                        return mf.mesh;
                    }
                    
                    SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        return smr.sharedMesh;
                    }
                }
            }

            return null;
        }

        public static bool ReplaceMesh(Object obj, Mesh mesh)
        {          
            bool replaced = false;
            Object o = null;

            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf)
                    {                        
                        mf.mesh = mesh;
                        o = mf;
                        replaced = true;                        
                    }

                    SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        smr.sharedMesh = mesh;
                        o = smr;
                        replaced = true;
                    }
                }
            }

            if (replaced)
            {                                
                GameObject sceneRoot = Util.GetScenePrefabInstanceRoot(obj);
                // this doesn't work...
                //PrefabUtility.ApplyObjectOverride(obj, prefabPath, InteractionMode.UserAction);
                // only this works:
                PrefabUtility.ApplyPrefabInstance(sceneRoot, InteractionMode.UserAction);
            }

            return replaced;
        }

        public static void PruneBlendShapes(Object obj)
        {
            if (GetSourcePrefab(obj, PRUNED_FOLDER_NAME, out string characterName, out string meshFolder, out Object srcObj))
            {
                //GameObject sceneRoot = Util.GetScenePrefabInstanceRoot(obj);
                //GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(sceneRoot);
                //Object srcObj = PrefabUtility.GetCorrespondingObjectFromSource(obj);                
                //string path = AssetDatabase.GetAssetPath(asset);

                Mesh srcMesh = GetMeshFrom(srcObj);

                if (!srcMesh)
                {
                    Util.LogError("No mesh found in selected object.");
                    return;
                }
                
                Mesh dstMesh = new Mesh();
                dstMesh.indexFormat = srcMesh.indexFormat;
                dstMesh.vertices = srcMesh.vertices;
                dstMesh.uv = srcMesh.uv;
                dstMesh.uv2 = srcMesh.uv2;
                dstMesh.normals = srcMesh.normals;
                dstMesh.colors = srcMesh.colors;
                dstMesh.boneWeights = srcMesh.boneWeights;
                dstMesh.bindposes = srcMesh.bindposes;
                dstMesh.bounds = srcMesh.bounds;
                dstMesh.tangents = srcMesh.tangents;
                dstMesh.triangles = srcMesh.triangles;
                dstMesh.subMeshCount = srcMesh.subMeshCount;

                for (int s = 0; s < srcMesh.subMeshCount; s++)
                {
                    SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                    dstMesh.SetSubMesh(s, submesh);
                }

                // copy any blendshapes across
                if (srcMesh.blendShapeCount > 0)
                {
                    Vector3[] deltaVerts = new Vector3[srcMesh.vertexCount];
                    Vector3[] deltaNormals = new Vector3[srcMesh.vertexCount];
                    Vector3[] deltaTangents = new Vector3[srcMesh.vertexCount];

                    for (int i = 0; i < srcMesh.blendShapeCount; i++)
                    {
                        string name = srcMesh.GetBlendShapeName(i);

                        int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                        for (int f = 0; f < frameCount; f++)
                        {
                            float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                            srcMesh.GetBlendShapeFrameVertices(i, f, deltaVerts, deltaNormals, deltaTangents);

                            Vector3 deltaSum = Vector3.zero;
                            for (int d = 0; d < srcMesh.vertexCount; d++) deltaSum += deltaVerts[d];

                            if (deltaSum.magnitude > 0.1f)
                                dstMesh.AddBlendShapeFrame(name, frameWeight, deltaVerts, deltaNormals, deltaTangents);
                        }
                    }
                }

                // Save the mesh asset.
                if (Util.EnsureAssetsFolderExists(meshFolder))
                {
                    string meshPath = Path.Combine(meshFolder, srcObj.name + ".mesh");
                    AssetDatabase.CreateAsset(dstMesh, meshPath);

                    if (obj.GetType() == typeof(GameObject))
                    {
                        GameObject go = (GameObject)obj;
                        if (go)
                        {
                            Mesh createdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                            if (!ReplaceMesh(obj, createdMesh))
                            {
                                Util.LogError("Unable to set mesh in selected object!");
                            }
                        }
                    }
                }
            }
        }

        public static Mesh CopyMesh(Mesh srcMesh)
        {
            Mesh dstMesh = new Mesh();
            dstMesh.indexFormat = srcMesh.indexFormat;
            dstMesh.vertices = srcMesh.vertices;
            dstMesh.uv = srcMesh.uv;
            dstMesh.uv2 = srcMesh.uv2;
            dstMesh.normals = srcMesh.normals;
            dstMesh.colors = srcMesh.colors;
            dstMesh.boneWeights = srcMesh.boneWeights;
            dstMesh.bindposes = srcMesh.bindposes;
            dstMesh.bounds = srcMesh.bounds;
            dstMesh.tangents = srcMesh.tangents;
            dstMesh.triangles = srcMesh.triangles;

            dstMesh.subMeshCount = srcMesh.subMeshCount;
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                dstMesh.SetSubMesh(s, submesh);
            }
            
            if (srcMesh.blendShapeCount > 0)
            {
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        dstMesh.AddBlendShapeFrame(name, frameWeight, bufVerts, bufNormals, bufTangents);
                    }
                }
            }

            return dstMesh;
        }

        public static void ReverseTriangleOrder(Object obj)
        {
            if (GetSourcePrefab(obj, MESH_FOLDER_NAME, out string characterName, out string meshFolder, out Object srcObj))
            {
                Mesh srcMesh = GetMeshFrom(srcObj);

                if (!srcMesh)
                {
                    Util.LogError("No mesh found in selected object.");
                    return;
                }

                Mesh dstMesh = new Mesh();
                dstMesh.indexFormat = srcMesh.indexFormat;
                dstMesh.vertices = srcMesh.vertices;
                dstMesh.uv = srcMesh.uv;
                dstMesh.uv2 = srcMesh.uv2;
                dstMesh.normals = srcMesh.normals;
                dstMesh.colors = srcMesh.colors;
                dstMesh.boneWeights = srcMesh.boneWeights;
                dstMesh.bindposes = srcMesh.bindposes;
                dstMesh.bounds = srcMesh.bounds;
                dstMesh.tangents = srcMesh.tangents;

                int[] reversed = new int[srcMesh.triangles.Length];
                int[] forward = srcMesh.triangles;

                // first pass: reverse the triangle order for each submesh
                for (int s = 0; s < srcMesh.subMeshCount; s++)
                {
                    SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                    int start = submesh.indexStart;
                    int end = start + submesh.indexCount;
                    int j = end - 3;
                    for (int i = start; i < end; i += 3)
                    {
                        reversed[j] = forward[i];
                        reversed[j + 1] = forward[i + 1];
                        reversed[j + 2] = forward[i + 2];
                        j -= 3;
                    }
                }

                dstMesh.triangles = reversed;
                dstMesh.subMeshCount = srcMesh.subMeshCount;

                // second pass: copy sub-mesh data (vertex and triangle data must be present for this)
                for (int s = 0; s < srcMesh.subMeshCount; s++)
                {
                    SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                    dstMesh.SetSubMesh(s, submesh);
                }

                // copy any blendshapes across
                if (srcMesh.blendShapeCount > 0)
                {
                    Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                    Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                    Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                    for (int i = 0; i < srcMesh.blendShapeCount; i++)
                    {
                        string name = srcMesh.GetBlendShapeName(i);

                        int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                        for (int f = 0; f < frameCount; f++)
                        {
                            float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                            srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                            dstMesh.AddBlendShapeFrame(name, frameWeight, bufVerts, bufNormals, bufTangents);
                        }
                    }
                }

                // Save the mesh asset.
                if (Util.EnsureAssetsFolderExists(meshFolder))
                {
                    string meshPath = Path.Combine(meshFolder, srcObj.name + "_Inverted.mesh");
                    AssetDatabase.CreateAsset(dstMesh, meshPath);

                    if (obj.GetType() == typeof(GameObject))
                    {
                        GameObject go = (GameObject)obj;
                        if (go)
                        {
                            Mesh createdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                            if (!ReplaceMesh(obj, createdMesh))
                            {
                                Util.LogError("Unable to set mesh in selected object!");
                            }
                        }
                    }
                }
            }
        }

        public static GameObject FindCharacterBone(GameObject gameObject, string name)
        {
            if (gameObject)
            {
                if (gameObject.name.iEndsWith(name))
                    return gameObject;

                int children = gameObject.transform.childCount;
                for (int i = 0; i < children; i++)
                {
                    GameObject found = FindCharacterBone(gameObject.transform.GetChild(i).gameObject, name);
                    if (found) return found;
                }
            }

            return null;
        }        

        public static GameObject FindCharacterBone(GameObject gameObject, string name1, string name2)
        {
            if (gameObject)
            {                
                if (gameObject.name.iEndsWith(name1) || gameObject.name.iEndsWith(name2))
                    return gameObject;

                int children = gameObject.transform.childCount;
                for (int i = 0; i < children; i++)
                {
                    GameObject found = FindCharacterBone(gameObject.transform.GetChild(i).gameObject, name1, name2);
                    if (found) return found;
                }
            }

            return null;
        }

        public static void FindCharacterBones(GameObject gameObject, List<GameObject> bones, params string [] searchNames)
        {
            if (gameObject && !bones.Contains(gameObject))
            {
                if (Util.NameContainsKeywords(gameObject.name, searchNames))
                {
                    bones.Add(gameObject);
                }

                int children = gameObject.transform.childCount;
                for (int i = 0; i < children; i++)
                {
                    GameObject childObject = gameObject.transform.GetChild(i).gameObject;
                    FindCharacterBones(childObject, bones, searchNames);
                }
            }
        }

        public static void CharacterOpenCloseMouth(Object obj)
        {
            if (!obj) return;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                bool isOpen = true;

                // find the jaw bone and change it's rotation
                GameObject jawBone = FindCharacterBone(root, "CC_Base_JawRoot", "JawRoot");                
                if (jawBone)
                {
                    Transform jaw = jawBone.transform;
                    Quaternion rotation = jaw.localRotation;
                    Vector3 euler = rotation.eulerAngles;
                    GameObject sourceJawBone = PrefabUtility.GetCorrespondingObjectFromSource(jawBone);
                    Vector3 sourceEuler = sourceJawBone.transform.localRotation.eulerAngles;
                    float difference = Mathf.DeltaAngle(euler.x, sourceEuler.x) +
                                       Mathf.DeltaAngle(euler.y, sourceEuler.y) +
                                       Mathf.DeltaAngle(euler.z, sourceEuler.z);
                    if (difference > 2f) isOpen = true;
                    else isOpen = false;

                    if (isOpen)
                    {
                        jaw.localEulerAngles = sourceEuler;
                        isOpen = false;
                    }
                    else
                    {
                        euler = sourceEuler;
                        euler.z -= 35f;
                        jaw.localEulerAngles = euler;
                        isOpen = true;
                    }

                    const string shapeName = "Mouth_Open";

                    // go through all the mesh object with blendshapes and set the "Mouth_Open" blend shape
                    for (int i = 0; i < root.transform.childCount; i++)
                    {
                        GameObject child = root.transform.GetChild(i).gameObject;
                        SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                        if (renderer)
                        {
                            Mesh mesh = renderer.sharedMesh;
                            if (mesh.blendShapeCount > 0)
                            {
                                int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                                if (shapeIndex > 0)
                                {
                                    renderer.SetBlendShapeWeight(shapeIndex, isOpen ? 100f : 0f);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void CharacterOpenCloseEyes(Object obj)
        {
            if (!obj) return;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                bool isOpen;

                const string shapeNameL = "Eye_Blink_L";
                const string shapeNameR = "Eye_Blink_R";
                const string shapeNameSingle = "Eye_Blink";

                // go through all the mesh object with blendshapes and set the "Mouth_Open" blend shape
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    GameObject child = root.transform.GetChild(i).gameObject;
                    SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (renderer)
                    {
                        Mesh mesh = renderer.sharedMesh;
                        if (mesh.blendShapeCount > 0)
                        {
                            int shapeIndexL = mesh.GetBlendShapeIndex(shapeNameL);
                            int shapeIndexR = mesh.GetBlendShapeIndex(shapeNameR);
                            int shapeIndexS = mesh.GetBlendShapeIndex(shapeNameSingle);

                            if (shapeIndexL > 0 && shapeIndexR > 0)
                            {
                                if (renderer.GetBlendShapeWeight(shapeIndexL) > 0f) isOpen = false;
                                else isOpen = true;

                                renderer.SetBlendShapeWeight(shapeIndexL, isOpen ? 100f : 0f);
                                renderer.SetBlendShapeWeight(shapeIndexR, isOpen ? 100f : 0f);
                            }
                            else if (shapeIndexS > 0)
                            {
                                if (renderer.GetBlendShapeWeight(shapeIndexS) > 0f) isOpen = false;
                                else isOpen = true;

                                renderer.SetBlendShapeWeight(shapeIndexS, isOpen ? 100f : 0f);                                
                            }
                        }
                    }
                }
            }
        }

        public enum EyeLookDir { None = 0, Left = 1, Right = 2, Up = 4, Down = 8 }
        public static void CharacterEyeLook(Object obj, EyeLookDir dirFlags)
        {
            if (!obj) return;

            GameObject root = Util.GetScenePrefabInstanceRoot(obj);

            if (root)
            {
                GameObject leftEye = FindCharacterBone(root, "CC_Base_L_Eye", "L_Eye");
                GameObject rightEye = FindCharacterBone(root, "CC_Base_R_Eye", "R_Eye");

                if (leftEye && rightEye)
                {
                    Vector3 euler;

                    if (dirFlags == 0)
                    {
                        GameObject sourceEye = PrefabUtility.GetCorrespondingObjectFromSource(leftEye);
                        euler = sourceEye.transform.localRotation.eulerAngles;
                    }
                    else euler = leftEye.transform.localRotation.eulerAngles;

                    if ((dirFlags & EyeLookDir.Left) > 0) euler.z -= 15f;
                    if ((dirFlags & EyeLookDir.Right) > 0) euler.z += 15f;
                    if ((dirFlags & EyeLookDir.Up) > 0) euler.x -= 10f;
                    if ((dirFlags & EyeLookDir.Down) > 0) euler.x += 10f;

                    Quaternion rotation = Quaternion.identity;
                    rotation.eulerAngles = euler;
                    leftEye.transform.localRotation = rotation;
                    rightEye.transform.localRotation = rotation;
                }
            }
        }

        /*
        [MenuItem("Reallusion/Tools/Setup Dual Material Hair", priority = 700)]
        private static void DoEHM()
        {
            MeshUtil.Extract2PassHairMeshes(Selection.activeObject);
        }        
        */

        public static Mesh ExtractSubMesh(Mesh srcMesh, int index)
        {
            SubMeshDescriptor extractMeshDesc = srcMesh.GetSubMesh(index);            

            // operate on a local copy of the source mesh data (much faster)
            Vector3[] srcVertices = srcMesh.vertices;
            Vector2[] srcUv = srcMesh.uv;
            Vector2[] srcUv2 = srcMesh.uv2;
            Vector2[] srcUv3 = srcMesh.uv3;
            Vector2[] srcUv4 = srcMesh.uv4;
            Vector2[] srcUv5 = srcMesh.uv5;
            Vector2[] srcUv6 = srcMesh.uv6;
            Vector2[] srcUv7 = srcMesh.uv7;
            Vector2[] srcUv8 = srcMesh.uv8;
            Vector3[] srcNormals = srcMesh.normals;
            Color[] srcColors = srcMesh.colors;
            BoneWeight[] srcBoneWeights = srcMesh.boneWeights;
            Vector4[] srcTangents = srcMesh.tangents;
            int[] srcTriangles = srcMesh.triangles;

            // first determine which vertices are used in the faces of the indexed submesh and remap their indices to the new mesh.
            int maxVerts = srcMesh.vertexCount;
            int[] remapping = new int[maxVerts];
            for (int i = 0; i < maxVerts; i++) remapping[i] = -1;
            int pointer = 0;            
            for (int tIndex = extractMeshDesc.indexStart; tIndex < extractMeshDesc.indexStart + extractMeshDesc.indexCount; tIndex++)
            {
                int vertIndex = srcTriangles[tIndex];
                if (remapping[vertIndex] == -1) remapping[vertIndex] = pointer++;
            }
            // this also tells us how many vertices are in the sub-mesh.
            int numNewVerts = pointer;

            // now create the extracted mesh
            Mesh newMesh = new Mesh();
            newMesh.indexFormat = srcMesh.indexFormat;
            Vector3[] vertices = new Vector3[numNewVerts];
            Vector2[] uv = new Vector2[srcUv.Length > 0 ? numNewVerts : 0];
            Vector2[] uv2 = new Vector2[srcUv2.Length > 0 ? numNewVerts : 0];
            Vector2[] uv3 = new Vector2[srcUv3.Length > 0 ? numNewVerts : 0];
            Vector2[] uv4 = new Vector2[srcUv4.Length > 0 ? numNewVerts : 0];
            Vector2[] uv5 = new Vector2[srcUv5.Length > 0 ? numNewVerts : 0];
            Vector2[] uv6 = new Vector2[srcUv6.Length > 0 ? numNewVerts : 0];
            Vector2[] uv7 = new Vector2[srcUv7.Length > 0 ? numNewVerts : 0];
            Vector2[] uv8 = new Vector2[srcUv8.Length > 0 ? numNewVerts : 0];
            Vector3[] normals = new Vector3[srcNormals.Length > 0 ? numNewVerts : 0];
            Color[] colors = new Color[srcColors.Length > 0 ? numNewVerts : 0];
            BoneWeight[] boneWeights = new BoneWeight[srcBoneWeights.Length > 0 ? numNewVerts : 0];
            Vector4[] tangents = new Vector4[srcTangents.Length > 0 ? numNewVerts : 0];            
            // copy and remap all the submesh vert data into the new mesh
            for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
            {                
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                {
                    vertices[remappedIndex] = srcVertices[vertIndex];
                    if (srcUv.Length > 0)
                        uv[remappedIndex] = srcUv[vertIndex];
                    if (srcUv2.Length > 0)
                        uv2[remappedIndex] = srcUv2[vertIndex];
                    if (srcUv3.Length > 0)
                        uv3[remappedIndex] = srcUv3[vertIndex];
                    if (srcUv4.Length > 0)
                        uv4[remappedIndex] = srcUv4[vertIndex];
                    if (srcUv5.Length > 0)
                        uv5[remappedIndex] = srcUv5[vertIndex];
                    if (srcUv6.Length > 0)
                        uv6[remappedIndex] = srcUv6[vertIndex];
                    if (srcUv7.Length > 0)
                        uv7[remappedIndex] = srcUv7[vertIndex];
                    if (srcUv8.Length > 0)
                        uv8[remappedIndex] = srcUv8[vertIndex];
                    if (srcNormals.Length >0)
                        normals[remappedIndex] = srcNormals[vertIndex];
                    if (srcColors.Length > 0)
                        colors[remappedIndex] = srcColors[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        boneWeights[remappedIndex] = srcBoneWeights[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        tangents[remappedIndex] = srcTangents[vertIndex];
                }
            }            
            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.uv2 = uv2;
            newMesh.uv3 = uv3;
            newMesh.uv4 = uv4;
            newMesh.uv5 = uv5;
            newMesh.uv6 = uv6;
            newMesh.uv7 = uv7;
            newMesh.uv8 = uv8;
            newMesh.normals = normals;
            newMesh.colors = colors;
            newMesh.boneWeights = boneWeights;
            newMesh.tangents = tangents;
            newMesh.bindposes = srcMesh.bindposes;
            newMesh.bounds = srcMesh.bounds;
            newMesh.subMeshCount = 1;
            // finally copy and remap the triangle data last 
            int[] triangles = new int[extractMeshDesc.indexCount];
            pointer = 0;
            for (int tIndex = extractMeshDesc.indexStart; tIndex < extractMeshDesc.indexStart + extractMeshDesc.indexCount; tIndex++)
            {
                int vertIndex = srcTriangles[tIndex];
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                    triangles[pointer++] = remappedIndex;
            }
            newMesh.triangles = triangles;
            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                // source buffer for blend shapes
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                // frame buffer for adding blend shapes to new mesh
                Vector3[] frameVerts = new Vector3[numNewVerts];
                Vector3[] frameNormals = new Vector3[numNewVerts];
                Vector3[] frameTangents = new Vector3[numNewVerts];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
                        {
                            int remappedIndex = remapping[vertIndex];
                            if (remappedIndex >= 0)
                            {
                                frameVerts[remappedIndex] = bufVerts[vertIndex];
                                frameNormals[remappedIndex] = bufNormals[vertIndex];
                                frameTangents[remappedIndex] = bufTangents[vertIndex];
                            }
                        }
                        newMesh.AddBlendShapeFrame(name, frameWeight, frameVerts, frameNormals, frameTangents);
                    }
                }
            }
            SubMeshDescriptor newMeshDesc = extractMeshDesc;
            newMeshDesc.firstVertex = 0;
            newMeshDesc.indexStart = 0;
            newMeshDesc.indexCount = extractMeshDesc.indexCount;
            newMeshDesc.vertexCount = numNewVerts;            
            newMesh.SetSubMesh(0, newMeshDesc);            

            return newMesh;
        }



        public static Mesh RemoveSubMeshes(Mesh srcMesh, List<int> indices)
        {            
            // operate on a local copy of the source mesh data (much faster)
            Vector3[] srcVertices = srcMesh.vertices;
            Vector2[] srcUv = srcMesh.uv;
            Vector2[] srcUv2 = srcMesh.uv2;
            Vector2[] srcUv3 = srcMesh.uv3;
            Vector2[] srcUv4 = srcMesh.uv4;
            Vector2[] srcUv5 = srcMesh.uv5;
            Vector2[] srcUv6 = srcMesh.uv6;
            Vector2[] srcUv7 = srcMesh.uv7;
            Vector2[] srcUv8 = srcMesh.uv8;
            Vector3[] srcNormals = srcMesh.normals;
            Color[] srcColors = srcMesh.colors;
            BoneWeight[] srcBoneWeights = srcMesh.boneWeights;
            Vector4[] srcTangents = srcMesh.tangents;
            int[] srcTriangles = srcMesh.triangles;

            // first determine which vertices are used in the faces of *ALL SUBMESHES EXCEPT* the indexed submesh 
            // and remap their indices to the new mesh.
            int maxVerts = srcMesh.vertexCount;
            int[] remapping = new int[maxVerts];
            for (int i = 0; i < maxVerts; i++) remapping[i] = -1;
            int pointer = 0;
            int numNewTriangles = 0;
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                if (!indices.Contains(s))
                {
                    SubMeshDescriptor meshDesc = srcMesh.GetSubMesh(s);
                    numNewTriangles += meshDesc.indexCount;
                    for (int tIndex = meshDesc.indexStart; tIndex < meshDesc.indexStart + meshDesc.indexCount; tIndex++)
                    {
                        int vertIndex = srcTriangles[tIndex];
                        if (remapping[vertIndex] == -1) remapping[vertIndex] = pointer++;
                    }
                }
            }
            // this also tells us how many vertices are in the new mesh.
            int numNewVerts = pointer;

            // now create the extracted mesh
            Mesh newMesh = new Mesh();
            newMesh.indexFormat = srcMesh.indexFormat;
            Vector3[] vertices = new Vector3[numNewVerts];
            Vector2[] uv = new Vector2[srcUv.Length > 0 ? numNewVerts : 0];
            Vector2[] uv2 = new Vector2[srcUv2.Length > 0 ? numNewVerts : 0];
            Vector2[] uv3 = new Vector2[srcUv3.Length > 0 ? numNewVerts : 0];
            Vector2[] uv4 = new Vector2[srcUv4.Length > 0 ? numNewVerts : 0];
            Vector2[] uv5 = new Vector2[srcUv5.Length > 0 ? numNewVerts : 0];
            Vector2[] uv6 = new Vector2[srcUv6.Length > 0 ? numNewVerts : 0];
            Vector2[] uv7 = new Vector2[srcUv7.Length > 0 ? numNewVerts : 0];
            Vector2[] uv8 = new Vector2[srcUv8.Length > 0 ? numNewVerts : 0];
            Vector3[] normals = new Vector3[srcNormals.Length > 0 ? numNewVerts : 0];
            Color[] colors = new Color[srcColors.Length > 0 ? numNewVerts : 0];
            BoneWeight[] boneWeights = new BoneWeight[srcBoneWeights.Length > 0 ? numNewVerts : 0];
            Vector4[] tangents = new Vector4[srcTangents.Length > 0 ? numNewVerts : 0];            
            // copy and remap all the submesh vert data into the new mesh
            for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
            {
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                {
                    vertices[remappedIndex] = srcVertices[vertIndex];
                    if (srcUv.Length > 0)
                        uv[remappedIndex] = srcUv[vertIndex];
                    if (srcUv2.Length > 0)
                        uv2[remappedIndex] = srcUv2[vertIndex];
                    if (srcUv3.Length > 0)
                        uv3[remappedIndex] = srcUv3[vertIndex];
                    if (srcUv4.Length > 0)
                        uv4[remappedIndex] = srcUv4[vertIndex];
                    if (srcUv5.Length > 0)
                        uv5[remappedIndex] = srcUv5[vertIndex];
                    if (srcUv6.Length > 0)
                        uv6[remappedIndex] = srcUv6[vertIndex];
                    if (srcUv7.Length > 0)
                        uv7[remappedIndex] = srcUv7[vertIndex];
                    if (srcUv8.Length > 0)
                        uv8[remappedIndex] = srcUv8[vertIndex];
                    if (srcNormals.Length >0)
                        normals[remappedIndex] = srcNormals[vertIndex];
                    if (srcColors.Length > 0)
                        colors[remappedIndex] = srcColors[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        boneWeights[remappedIndex] = srcBoneWeights[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        tangents[remappedIndex] = srcTangents[vertIndex];
                }
            }
            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.uv2 = uv2;
            newMesh.normals = normals;
            newMesh.colors = colors;
            newMesh.boneWeights = boneWeights;
            newMesh.tangents = tangents;
            newMesh.bindposes = srcMesh.bindposes;
            newMesh.bounds = srcMesh.bounds;            
            // finally copy and remap the triangle data last
            int[] triangles = new int[numNewTriangles];
            pointer = 0;
            // only consider the triangle lists from the included submeshes...
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                if (!indices.Contains(s))
                {
                    SubMeshDescriptor meshDesc = srcMesh.GetSubMesh(s);
                    for (int tIndex = meshDesc.indexStart; tIndex < meshDesc.indexStart + meshDesc.indexCount; tIndex++)
                    {                    
                        int vertIndex = srcTriangles[tIndex];
                        int remappedIndex = remapping[vertIndex];
                        if (remappedIndex >= 0)
                            triangles[pointer++] = remappedIndex;
                    }
                }
            }
            newMesh.triangles = triangles;
            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                // source buffer for blend shapes
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                // frame buffer for adding blend shapes to new mesh
                Vector3[] frameVerts = new Vector3[numNewVerts];
                Vector3[] frameNormals = new Vector3[numNewVerts];
                Vector3[] frameTangents = new Vector3[numNewVerts];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
                        {
                            int remappedIndex = remapping[vertIndex];
                            if (remappedIndex >= 0)
                            {
                                frameVerts[remappedIndex] = bufVerts[vertIndex];
                                frameNormals[remappedIndex] = bufNormals[vertIndex];
                                frameTangents[remappedIndex] = bufTangents[vertIndex];
                            }
                        }
                        newMesh.AddBlendShapeFrame(name, frameWeight, frameVerts, frameNormals, frameTangents);
                    }
                }
            }

            pointer = 0;
            int indexStart = 0;
            newMesh.subMeshCount = srcMesh.subMeshCount - 1;
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                if (!indices.Contains(s))
                {
                    SubMeshDescriptor meshDesc = srcMesh.GetSubMesh(s);
                    SubMeshDescriptor newMeshDesc = meshDesc;
                    newMeshDesc.firstVertex = remapping[meshDesc.firstVertex];
                    newMeshDesc.indexStart = indexStart;
                    newMeshDesc.indexCount = meshDesc.indexCount;
                    newMeshDesc.vertexCount = meshDesc.vertexCount;                    
                    newMesh.SetSubMesh(pointer++, newMeshDesc);
                    indexStart += meshDesc.indexCount;
                }
            }            

            return newMesh;
        }

        public static void CopyMaterialParameters(Material from, Material to)
        {
            int renderQueue = to.renderQueue;
            to.CopyPropertiesFromMaterial(from);
            to.renderQueue = renderQueue;
        }

        private static void FixHDRP2PassMaterials(Material firstPass, Material secondPass)
        {            
            if (Pipeline.isHDRP)
            {
                /*
                string fp = AssetDatabase.GetAssetPath(firstPass);
                string sp = AssetDatabase.GetAssetPath(secondPass);
                AssetImporter aif = AssetImporter.GetAtPath(fp);
                AssetImporter ais = AssetImporter.GetAtPath(sp);
                // force a save and re-import of the materials
                // otherwise these settings don't take.
                aif.SaveAndReimport();
                ais.SaveAndReimport();
                */

                firstPass.SetFloat("_SurfaceType", 0f);
                firstPass.SetFloat("_ENUMCLIPQUALITY_ON", 0f);                
                firstPass.DisableKeyword("BOOLEAN_SECONDPASS_ON");
                firstPass.SetFloat("BOOLEAN_SECONDPASS", 0f);
                Pipeline.ResetMaterial(firstPass);

                // transparent surface
                secondPass.SetFloat("_SurfaceType", 1f);
                // alpha clip
                secondPass.SetFloat("_AlphaCutoffEnable", 1f);                
                // prepass & postpass
                secondPass.SetFloat("_TransparentDepthPostpassEnable", 0f);
                secondPass.SetFloat("_TransparentDepthPrepassEnable", 0f);
                // preserve specular lighting
                //secondPass.SetFloat("_EnableBlendModePreserveSpecularLighting", 0f);
                // Z test (opaque and transparent): Less
                secondPass.SetFloat("_ZTestDepthEqualForOpaque", 2f);
                secondPass.SetFloat("_ZTestTransparent", 2f);
                // keywords
                secondPass.SetFloat("_ENUMCLIPQUALITY_ON", 0f);
                secondPass.EnableKeyword("BOOLEAN_SECONDPASS_ON");
                secondPass.SetFloat("BOOLEAN_SECONDPASS", 1f);
                Pipeline.ResetMaterial(secondPass);

                /*
                aif.SaveAndReimport();
                ais.SaveAndReimport();
                */
            }
        }

        public struct TwoPassPair
        {
            public Material sourceMaterial;
            public Material firstPassMaterial;
            public Material secondPassMaterial;

            public TwoPassPair(Material s, Material a, Material b)
            {
                sourceMaterial = s;
                firstPassMaterial = a;
                secondPassMaterial = b;
            }
        }


        public static bool Extract2PassHairMeshes(CharacterInfo info, GameObject prefabInstance)
        {
            if (!prefabInstance) return false;

            string name = info.name;
            string fbxFolder = info.folder;
            string materialFolder = Path.Combine(fbxFolder, Importer.MATERIALS_FOLDER, name);
            string meshFolder = Path.Combine(fbxFolder, MESH_FOLDER_NAME, name);                                    

            int processCount = 0;

            Dictionary<Material, TwoPassPair> done = new Dictionary<Material, TwoPassPair>();

            Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in renderers)
            {
                bool hasHairMaterial = false;                
                bool hasScalpMaterial = false;
                int subMeshCount = 0;
                int hairMeshCount = 0;
                foreach (Material m in r.sharedMaterials)
                {
                    if (!m) continue;

                    subMeshCount++;
                    if (m.shader.name.iContains(Pipeline.SHADER_HQ_HAIR))
                    {
                        hasHairMaterial = true;
                        hairMeshCount++;
                    }
                    else if (Util.NameContainsKeywords(m.name, "scalp", "base"))
                    {
                        hasScalpMaterial = true;
                    }
                }

                if (hasHairMaterial)
                {
                    bool isFacialObject = MeshIsFacialHair(r.gameObject);

                    List<int> indicesToRemove = new List<int>();
                    bool dontRemoveMaterials = false;

                    GameObject oldObj = r.gameObject;
                    Mesh oldMesh = GetMeshFrom(oldObj);
                    SkinnedMeshRenderer oldSmr = oldObj.GetComponent<SkinnedMeshRenderer>();

                    for (int index = 0; index < r.sharedMaterials.Length; index++)
                    {
                        Material oldMat = r.sharedMaterials[index];

                        if (!oldMat) continue;

                        if (oldMat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR))
                        {
                            float alphaClipValue = 0.666f;
                            if (Pipeline.is3D) alphaClipValue = 0.55f;
                                                        
                            oldMat.SetFloatIf("_AlphaClip", alphaClipValue);
                            oldMat.SetFloatIf("_AlphaClip2", alphaClipValue);                            
                            oldMat.SetFloatIf("_ShadowClip", 0.5f);                            
                        }

                        bool useTessellation = oldMat.shader.name.iContains("_Tessellation");

                        if (subMeshCount > 1 && oldMat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR))
                        {
                            Util.LogInfo("Extracting subMesh(" + index.ToString() +  ") from Object: " + oldObj.name);

                            // extract mesh into two new meshes, the old mesh without the extracted submesh
                            // and just the extracted submesh
                            Mesh newMesh = ExtractSubMesh(oldMesh, index);
                            // Save the mesh asset.
                            Util.EnsureAssetsFolderExists(meshFolder);
                            string meshPath = Path.Combine(meshFolder, oldObj.name + "_ExtractedHairMesh" + index.ToString() + ".mesh");
                            AssetDatabase.CreateAsset(newMesh, meshPath);
                            newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                            // add new object as sibling to old object:
                            GameObject newObj = new GameObject();
                            newObj.name = oldObj.name + "_Extracted" + index.ToString();
                            newObj.transform.parent = oldObj.transform.parent;
                            newObj.transform.localPosition = oldObj.transform.localPosition;
                            newObj.transform.localRotation = oldObj.transform.localRotation;
                            newObj.transform.localScale = oldObj.transform.localScale;
                            SkinnedMeshRenderer smr = newObj.AddComponent<SkinnedMeshRenderer>();
                            smr.localBounds = oldSmr.localBounds;
                            smr.quality = oldSmr.quality;
                            smr.rootBone = oldSmr.rootBone;
                            smr.bones = oldSmr.bones;

                            // - set skinnedMeshRenderer mesh to extracted mesh
                            smr.sharedMesh = newMesh;
                            Material[] sharedMaterials = new Material[2];
                            if (done.ContainsKey(oldMat))
                            {
                                sharedMaterials[0] = done[oldMat].firstPassMaterial;
                                sharedMaterials[1] = done[oldMat].secondPassMaterial;
                            }
                            else
                            {
                                // - add first pass hair shader material
                                // - add second pass hair shader material
                                Material firstPassTemplate = Util.FindCustomMaterial(Pipeline.MATERIAL_HQ_HAIR_1ST_PASS, useTessellation);
                                Material secondPassTemplate = Util.FindCustomMaterial(Pipeline.MATERIAL_HQ_HAIR_2ND_PASS, useTessellation);
                                Material firstPass = new Material(firstPassTemplate);
                                Material secondPass = new Material(secondPassTemplate);
                                CopyMaterialParameters(oldMat, firstPass);
                                CopyMaterialParameters(oldMat, secondPass);
                                FixHDRP2PassMaterials(firstPass, secondPass);
                                // save the materials to the asset database.
                                AssetDatabase.CreateAsset(firstPass, Path.Combine(materialFolder, oldMat.name + "_1st_Pass.mat"));
                                AssetDatabase.CreateAsset(secondPass, Path.Combine(materialFolder, oldMat.name + "_2nd_Pass.mat"));
                                sharedMaterials[0] = firstPass;
                                sharedMaterials[1] = secondPass;
                                done.Add(oldMat, new TwoPassPair(oldMat, firstPass, secondPass));
                                // call the fix again as Unity reverts some settings when first saving...
                                FixHDRP2PassMaterials(firstPass, secondPass);
                            }
                            // add the 1st and 2nd pass materials to the mesh renderer
                            // a single submesh with multiple materials will render itself again with each material
                            // effectively acting as a multi-pass shader which fully complies with any SRP batching.
                            smr.sharedMaterials = sharedMaterials;

                            indicesToRemove.Add(index);
                            subMeshCount--;
                            processCount++;
                        }
                        else if (subMeshCount == 1 && oldMat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR))
                        {
                            Util.LogInfo("Leaving subMesh(" + index.ToString() + ") in Object: " + oldObj.name);

                            Material[] sharedMaterials = new Material[2];                            
                            if (done.ContainsKey(oldMat))
                            {
                                sharedMaterials[0] = done[oldMat].firstPassMaterial;
                                sharedMaterials[1] = done[oldMat].secondPassMaterial;
                            }
                            else
                            {
                                // - add first pass hair shader material
                                // - add second pass hair shader material
                                Material firstPassTemplate = Util.FindCustomMaterial(Pipeline.MATERIAL_HQ_HAIR_1ST_PASS, useTessellation);
                                Material secondPassTemplate = Util.FindCustomMaterial(Pipeline.MATERIAL_HQ_HAIR_2ND_PASS, useTessellation);
                                Material firstPass = new Material(firstPassTemplate);
                                Material secondPass = new Material(secondPassTemplate);
                                CopyMaterialParameters(oldMat, firstPass);
                                CopyMaterialParameters(oldMat, secondPass);
                                FixHDRP2PassMaterials(firstPass, secondPass);
                                // save the materials to the asset database.   
                                AssetDatabase.CreateAsset(firstPass, Path.Combine(materialFolder, oldMat.name + "_1st_Pass.mat"));
                                AssetDatabase.CreateAsset(secondPass, Path.Combine(materialFolder, oldMat.name + "_2nd_Pass.mat"));
                                sharedMaterials[0] = firstPass;
                                sharedMaterials[1] = secondPass;
                                done.Add(oldMat, new TwoPassPair(oldMat, firstPass, secondPass));
                                // call the fix again as Unity reverts some settings when first saving...
                                FixHDRP2PassMaterials(firstPass, secondPass);
                            }
                            // add the 1st and 2nd pass materials to the mesh renderer
                            // a single submesh with multiple materials will render itself again with each material
                            // effectively acting as a multi-pass shader which fully complies with any SRP batching.
                            oldSmr.sharedMaterials = sharedMaterials;                            
                            // as we have replaced the materials completely, don't remove any later when removing any submeshes...
                            dontRemoveMaterials = true;
                            processCount++;
                        }                        
                    }

                    if (indicesToRemove.Count > 0)
                    {
                        Util.LogInfo("Removing submeshes from Object: " + oldObj.name);
                        Mesh remainingMesh = RemoveSubMeshes(oldMesh, indicesToRemove);
                        // Save the mesh asset.                        
                        string meshPath = Path.Combine(meshFolder, oldObj.name + "_Remaining.mesh");
                        AssetDatabase.CreateAsset(remainingMesh, meshPath);
                        remainingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                        // replace mesh in obj.skinnedMeshRenderer with remaining submeshes
                        oldSmr.sharedMesh = remainingMesh;

                        if (!dontRemoveMaterials)
                        {
                            // remove old hair material from old shared material list...
                            Material[] sharedMaterials = new Material[oldSmr.sharedMaterials.Length - indicesToRemove.Count];
                            int i = 0;
                            for (int j = 0; j < oldSmr.sharedMaterials.Length; j++)
                                if (!indicesToRemove.Contains(j))
                                    sharedMaterials[i++] = oldSmr.sharedMaterials[j];
                            oldSmr.sharedMaterials = sharedMaterials;
                        }

                        // if the hair mesh has a scalp or base then what remains should be the scalp/base
                        // in HDRP ray tracing this should be set to not ray trace.
                        // but only if there is only the scalp material left:
                        if (hasScalpMaterial && oldSmr.sharedMaterials.Length == 1)
                        {
                            Pipeline.DisableRayTracing(oldSmr);
                        }                        

                        processCount++;
                    }
                }                
            }

            if (processCount > 0) return true;

            return false;
        }

        public struct SmoothVertData
        {
            public int index;
            public Vector3 normal;

            public SmoothVertData(int i, Vector3 n)
            {
                index = i;
                normal = n;                
            }
        }

        public static void SmoothNormals2(Mesh mesh, float angle)
        {            
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            int numTriangles = triangles.Length / 3;
            float threshold = Mathf.Cos(angle * Mathf.PI / 180f);

            Dictionary<long, List<SmoothVertData>> uniqueVerts = new Dictionary<long, List<SmoothVertData>>();
                    
            for (int t = 0; t < numTriangles; t++)
            {
                int t0 = t * 3 + 0;
                int t1 = t * 3 + 1;
                int t2 = t * 3 + 2;

                Vector3 p0 = vertices[triangles[t0]];
                Vector3 p1 = vertices[triangles[t1]];
                Vector3 p2 = vertices[triangles[t2]];

                Vector3 edge01 = p1 - p0;
                Vector3 edge02 = p2 - p0;
                Vector3 normal1 = Vector3.Cross(edge01, edge02);
                Vector3 edge12 = p2 - p1;
                Vector3 edge10 = p0 - p1;
                Vector3 normal2 = Vector3.Cross(edge12, edge10);
                Vector3 edge20 = p0 - p2;
                Vector3 edge21 = p1 - p2;
                Vector3 normal3 = Vector3.Cross(edge20, edge21);
                Vector3 normal = (normal1 + normal2 + normal3).normalized;
                                
                for (int i = 0; i < 3; i++)
                {
                    int index = triangles[t * 3 + i];
                    SmoothVertData vertData = new SmoothVertData(index, normal);
                    long hash = SpatialHash(vertices[index]);
                    if (uniqueVerts.TryGetValue(hash, out List<SmoothVertData> verts)) verts.Add(vertData);
                    else uniqueVerts[hash] = new List<SmoothVertData> { vertData };
                }
            }            

            foreach (KeyValuePair<long, List<SmoothVertData>> pair in uniqueVerts)
            {
                List<SmoothVertData> vertData = pair.Value;

                for (int i = 0; i < vertData.Count; i++)                
                {
                    SmoothVertData svdTest = vertData[i];                    
                    Vector3 smoothedNormal = Vector3.zero;                    

                    for (int j = 0; j < vertData.Count; j++)
                    {
                        SmoothVertData svdCompare = vertData[j];                        
                        if (i == j) 
                            smoothedNormal += svdTest.normal;                        
                        else                        
                            if (Vector3.Dot(svdTest.normal, svdCompare.normal) > threshold)
                                smoothedNormal += svdCompare.normal;
                    }

                    normals[svdTest.index] = smoothedNormal.normalized;
                }                
            }

            mesh.normals = normals;
        }

        public static void SmoothNormals(Mesh mesh, float angle)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            int numTriangles = triangles.Length / 3;
            int numVertices = vertices.Length;

            List<Vector3> uniqueVerts = new List<Vector3>(numVertices);
            int[] uniqueIndices = new int[numVertices];            

            for (int i = 0; i < numVertices; i++)
            {                
                bool foundUnique = false;

                for (int m = 0; m < uniqueVerts.Count; m++)
                {
                    if (vertices[i] == uniqueVerts[m])
                    {
                        uniqueIndices[i] = m;
                        foundUnique = true;
                    }                    
                }

                if (!foundUnique)
                {
                    uniqueIndices[i] = uniqueVerts.Count;
                    uniqueVerts.Add(vertices[i]);
                }
            }         
            
            Vector3[] uniqueNormals = new Vector3[uniqueVerts.Count];

            for (int t = 0; t < numTriangles; t++) 
            {
                int t0 = t * 3 + 0;
                int t1 = t * 3 + 1;
                int t2 = t * 3 + 2;

                Vector3 p0 = vertices[triangles[t0]];
                Vector3 p1 = vertices[triangles[t1]];
                Vector3 p2 = vertices[triangles[t2]];

                Vector3 edge01 = p1 - p0;
                Vector3 edge02 = p2 - p0;
                Vector3 normal = Vector3.Cross(edge01, edge02).normalized;

                int unique0 = uniqueIndices[triangles[t0]];
                int unique1 = uniqueIndices[triangles[t1]];
                int unique2 = uniqueIndices[triangles[t2]];

                uniqueNormals[unique0] += normal;
                uniqueNormals[unique1] += normal;
                uniqueNormals[unique2] += normal;                
            }

            for (int i = 0; i < uniqueVerts.Count; i++)
            {
                uniqueNormals[i] = uniqueNormals[i].normalized;
            }

            for (int i = 0; i < numVertices; i++)
            {
                int uniqueIndex = uniqueIndices[i];
                normals[i] = uniqueNormals[uniqueIndex];
            }

            mesh.normals = normals;
        }

        // http://www.beosil.com/download/CollisionDetectionHashing_VMV03.pdf
        public static long SpatialHash(Vector3 v)
        {
            const long p1 = 73868489;
            const long p2 = 23875351;
            const long p3 = 53885459;
            const long discrete = 1000;

            long x = (long)(v.x * discrete);
            long y = (long)(v.y * discrete);
            long z = (long)(v.z * discrete);

            return (x * p1) ^ (y * p2) ^ (z * p3);            
        }

        public static void AutoSmoothMesh(Object obj)
        {
            if (GetSourcePrefab(obj, MESH_FOLDER_NAME, out string characterName, out string meshFolder, out Object srcObj))
            {
                Mesh srcMesh = GetMeshFrom(srcObj);

                if (!srcMesh)
                {
                    Util.LogError("No mesh found in selected object.");
                    return;
                }

                if (srcMesh.name.iEndsWith("_Smoothed"))
                {
                    Util.LogWarn("Mesh is already smoothed!");
                    return;
                }

                Mesh dstMesh = CopyMesh(srcMesh);
                SmoothNormals2(dstMesh, 120f);

                // Save the mesh asset.
                if (Util.EnsureAssetsFolderExists(meshFolder))
                {
                    string meshPath = Path.Combine(meshFolder, srcObj.name + "_Smoothed.mesh");
                    AssetDatabase.CreateAsset(dstMesh, meshPath);

                    if (obj.GetType() == typeof(GameObject))
                    {
                        GameObject go = (GameObject)obj;
                        if (go)
                        {
                            Mesh createdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                            if (ReplaceMesh(obj, createdMesh))
                                Util.LogAlways("Auto Smooth Mesh Complete!");
                            else
                                Util.LogError("Unable to set mesh in selected object!");
                        }
                    }
                }
            }
        }

        public static int GetPrimaryBoneIndex(SkinnedMeshRenderer smr, int skip = 1)
        {
            Mesh mesh = smr.sharedMesh;
            int index = 0;

            if (mesh)
            {
                BoneWeight[] boneWeights = mesh.boneWeights;
                float[] boneTotals = new float[smr.bones.Length];
                for (int i = 0; i < boneTotals.Length; i++) boneTotals[i] = 0f;

                for (int i = 0; i < mesh.boneWeights.Length; i += skip)
                {
                    BoneWeight bw = boneWeights[i];
                    boneTotals[bw.boneIndex0] += bw.weight0;
                    boneTotals[bw.boneIndex1] += bw.weight1;
                    boneTotals[bw.boneIndex2] += bw.weight2;
                    boneTotals[bw.boneIndex3] += bw.weight3;
                }
                
                float weight = boneTotals[index];
                for (int i = 1; i < boneTotals.Length; i++)
                {
                    if (boneTotals[i] > weight)
                    {
                        index = i;
                        weight = boneTotals[i];
                    }
                }
            }

            return index;
        }

        public static int GetBoneIndex(SkinnedMeshRenderer smr, string name, string name2 = "", string name3 = "")
        {
            bool hasName2 = !string.IsNullOrEmpty(name2);
            bool hasName3 = !string.IsNullOrEmpty(name3);
            for (int i = 0; i < smr.bones.Length; i++)
            {
                Transform t = smr.bones[i];
                if (t.name.iContains(name)) return i;
                if (hasName2 && t.name.iContains(name2)) return i;
                if (hasName3 && t.name.iContains(name3)) return i;
            }

            return -1;
        }

        public static void FixSkinnedMeshBounds(GameObject prefabInstance)
        {
            SkinnedMeshRenderer[] renderers = prefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer smr in renderers)
            {
                int primaryBoneIndex = -1;
                if (RL.IsBodyMesh(smr)) primaryBoneIndex = GetBoneIndex(smr, "CC_Base_Waist", "spine_01");
                if (RL.IsHairMesh(smr)) primaryBoneIndex = GetBoneIndex(smr, "CC_Base_Head", "head");
                if (primaryBoneIndex == -1) primaryBoneIndex = GetPrimaryBoneIndex(smr, 5);
                smr.rootBone = smr.bones[primaryBoneIndex];
                smr.updateWhenOffscreen = true;
                Bounds bounds = new Bounds();
                bounds.center = smr.localBounds.center;
                bounds.extents = smr.localBounds.extents;
                smr.updateWhenOffscreen = false;
                smr.localBounds = bounds;
            }
        }

        public static bool MeshHasJawWeights(GameObject obj)
        {
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();

            if (smr)
            {
                foreach (Transform bone in smr.bones)
                {
                    if (bone.gameObject.name.iContains("JawRoot"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool MeshIsFacialHair(GameObject obj)
        {
            // if it has facial blend shapes...
            if (FacialProfileMapper.MeshHasFacialBlendShapes(obj))
            {
                if (Util.HasMaterialKeywords(obj, "scalp"))
                {
                    return false;
                }
                else if (Util.HasMaterialKeywords(obj, "base", "brow", "beard",
                                                  "mustache", "goatee", "stubble",
                                                  "bushy", "sword"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
