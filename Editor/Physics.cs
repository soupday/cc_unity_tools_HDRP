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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Reallusion.Import
{
    public class Physics
    {
        const float MAX_DISTANCE = 0.1f;
        const float MAX_PENETRATION = 0.02f;
        const float WEIGHT_POWER = 1f;        

        public enum PhysicsType
        {
            CollisionShapes,
            SoftPhysics
        }

        public enum ColliderType
        {
            Capsule,
            Sphere,
            Box
        }

        public enum ColliderAxis
        {
            X,
            Y,
            Z
        }

        public class CollisionShapeData
        {
            public string name;
            public string boneName;            
            public bool boneActive;
            public ColliderType colliderType;
            public ColliderAxis colliderAxis;
            public float margin;
            public float friction;
            public float elasticity;
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3 scale;
            public float radius;
            public float length;
            public Vector3 extent;            

            public CollisionShapeData(string bone, string name, QuickJSON colliderJson)
            {
                this.name = name;
                boneName = bone;

                if (colliderJson != null)
                {
                    boneActive = colliderJson.GetBoolValue("Bone Active");
                    colliderType = (ColliderType)Enum.Parse(typeof(ColliderType), colliderJson.GetStringValue("Bound Type"));
                    colliderAxis = (ColliderAxis)Enum.Parse(typeof(ColliderAxis), colliderJson.GetStringValue("Bound Axis"));
                    margin = colliderJson.GetFloatValue("Margin");
                    friction = colliderJson.GetFloatValue("Friction");
                    elasticity = colliderJson.GetFloatValue("Elasticity");
                    translation = colliderJson.GetVector3Value("WorldTranslate");
                    rotation = colliderJson.GetQuaternionValue("WorldRotationQ");
                    scale = colliderJson.GetVector3Value("WorldScale");
                    radius = colliderJson.GetFloatValue("Radius");
                    length = colliderJson.GetFloatValue("Capsule Length");
                    if (colliderType == ColliderType.Box) extent = colliderJson.GetVector3Value("Extent");
                }
            }
        }

        public class SoftPhysicsData
        {
            public string meshName;
            public string materialName;
            public bool activate;
            public bool gravity;
            public string weightMapPath;
            public float mass;
            public float friction;
            public float damping;
            public float drag;
            public float solverFrequency;
            public float tetherLimit;
            public float Elasticity;
            public float stretch;
            public float bending;
            public Vector3 inertia;
            public bool softRigidCollision;
            public float softRigidMargin;
            public bool selfCollision;
            public float selfMargin;
            public float stiffnessFrequency;            
            public bool isHair;

            public SoftPhysicsData(string mesh, string material, QuickJSON softPhysicsJson, QuickJSON characterJson)
            {
                meshName = mesh;
                materialName = material;
                isHair = false;
                QuickJSON objectMaterialJson = characterJson.GetObjectAtPath("Meshes/" + mesh + "/Materials/" + material);
                if (objectMaterialJson != null && 
                    objectMaterialJson.PathExists("Custom Shader/Shader Name") &&
                    objectMaterialJson.GetStringValue("Custom Shader/Shader Name") == "RLHair")
                    isHair = true;

                if (softPhysicsJson != null)
                {
                    activate = softPhysicsJson.GetBoolValue("Activate Physics");
                    gravity = softPhysicsJson.GetBoolValue("Use Global Gravity");
                    weightMapPath = softPhysicsJson.GetStringValue("Weight Map Path");
                    mass = softPhysicsJson.GetFloatValue("Mass");
                    friction = softPhysicsJson.GetFloatValue("Friction");
                    damping = softPhysicsJson.GetFloatValue("Damping");
                    drag = softPhysicsJson.GetFloatValue("Drag");
                    solverFrequency = softPhysicsJson.GetFloatValue("Solver Frequency");
                    tetherLimit = softPhysicsJson.GetFloatValue("Tether Limit");
                    Elasticity = softPhysicsJson.GetFloatValue("Elasticity");
                    stretch = softPhysicsJson.GetFloatValue("Stretch");
                    bending = softPhysicsJson.GetFloatValue("Bending");
                    inertia = softPhysicsJson.GetVector3Value("Inertia");
                    softRigidCollision = softPhysicsJson.GetBoolValue("Soft Vs Rigid Collision");
                    softRigidMargin = softPhysicsJson.GetFloatValue("Soft Vs Rigid Collision_Margin");
                    selfCollision = softPhysicsJson.GetBoolValue("Self Collision");
                    selfMargin = softPhysicsJson.GetFloatValue("Self Collision Margin");
                    stiffnessFrequency = softPhysicsJson.GetFloatValue("Stiffness Frequency");
                }
            }
        }

        public static float PHYSICS_SHRINK_COLLIDER_RADIUS
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Physics_Shrink_Collider_Radius"))
                    return EditorPrefs.GetFloat("RL_Physics_Shrink_Collider_Radius");
                return 0.5f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Physics_Shrink_Collider_Radius", value);
            }
        }

        public static float PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Physics_Weight_Map_Collider_Detect_Threshold"))
                    return EditorPrefs.GetFloat("RL_Physics_Weight_Map_Collider_Detect_Threshold");
                return 0.25f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Physics_Weight_Map_Collider_Detect_Threshold", value);
            }
        }

        private GameObject prefabAsset;        
        private GameObject prefabInstance;
        private float modelScale = 0.01f;
        private bool addClothPhysics = false;
        private bool addHairPhysics = false;        

        private List<CollisionShapeData> boneColliders;
        private List<SoftPhysicsData> softPhysics;
        private List<GameObject> clothMeshes;

        private string characterName;
        private string fbxFolder;
        private string characterGUID;
        private List<string> textureFolders;
        private QuickJSON jsonData;
        private bool aPose;

        public Physics(CharacterInfo info, GameObject prefabAsset, GameObject prefabInstance)
        {
            this.prefabAsset = prefabAsset;
            this.prefabInstance = prefabInstance;
            boneColliders = new List<CollisionShapeData>();
            softPhysics = new List<SoftPhysicsData>();
            clothMeshes = new List<GameObject>();
            modelScale = 0.01f;
            fbxFolder = info.folder;
            characterGUID = info.guid;
            characterName = info.name;
            fbxFolder = info.folder;
            jsonData = info.JsonData;
            addClothPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.ClothPhysics) > 0;
            addHairPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.HairPhysics) > 0;
            string fbmFolder = Path.Combine(fbxFolder, characterName + ".fbm");
            string texFolder = Path.Combine(fbxFolder, "textures", characterName);
            textureFolders = new List<string>() { fbmFolder, texFolder };
            if (info.CharacterJsonData.PathExists("Bind_Pose"))
            {
                if (info.CharacterJsonData.GetStringValue("Bind_Pose") == "APose") aPose = true;
            }

            ReadPhysicsJson(info.PhysicsJsonData, info.CharacterJsonData);
        }

        private void ReadPhysicsJson(QuickJSON physicsJson, QuickJSON characterJson)
        {
            QuickJSON shapesJson = physicsJson.GetObjectAtPath("Collision Shapes");
            QuickJSON softPhysicsJson = physicsJson.GetObjectAtPath("Soft Physics/Meshes");            
            
            if (shapesJson != null)
            {
                boneColliders.Clear();

                foreach (MultiValue boneJson in shapesJson.values)
                {
                    string boneName = boneJson.Key;
                    QuickJSON collidersJson = boneJson.ObjectValue;
                    if (collidersJson != null)
                    {
                        foreach (MultiValue colliderJson in collidersJson.values)
                        {
                            string colliderName = colliderJson.Key;
                            if (colliderJson.ObjectValue != null)
                                boneColliders.Add(new CollisionShapeData(boneName, colliderName, colliderJson.ObjectValue));
                        }
                    }
                }
            }

            if (softPhysicsJson != null)
            {
                softPhysics.Clear();

                foreach (MultiValue meshJson in softPhysicsJson.values)
                {
                    string meshName = meshJson.Key;                    
                    QuickJSON physicsMaterialsJson = meshJson.ObjectValue.GetObjectAtPath("Materials");
                    if (physicsMaterialsJson != null)
                    {
                        foreach (MultiValue matJson in physicsMaterialsJson.values)
                        {
                            string materialName = matJson.Key;                            
                            if (matJson.ObjectValue != null)
                                softPhysics.Add(new SoftPhysicsData(meshName, materialName, matJson.ObjectValue, characterJson));
                        }
                    }
                }
            }
        }

        public GameObject AddPhysics()
        {
            bool animationMode = AnimationMode.InAnimationMode();
            if (animationMode) AnimationMode.StopAnimationMode();

            AddColliders();
            AddCloth();

            prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(prefabAsset));

            if (animationMode) AnimationMode.StartAnimationMode();

            return prefabAsset;
        }

        public void RemoveAllPhysics()
        {
            Collider[] colliders = prefabInstance.GetComponentsInChildren<Collider>();
        }

        private void AddColliders()
        {
            GameObject parent = new GameObject();
            GameObject g;
            Transform[] objects = prefabInstance.GetComponentsInChildren<Transform>();
            Dictionary<Collider, string> colliderLookup = new Dictionary<Collider, string>();
            Dictionary<Collider, Collider> existingLookup = new Dictionary<Collider, Collider>();
            
            // delegates
            Func<string, Transform> FindBone = (boneName) => Array.Find(objects, o => o.name.Equals(boneName));
            Func<string, Transform, Collider> FindColliderObj = (colliderName, bone) =>
            {
                for (int i = 0; i < bone.childCount; i++)
                {
                    Transform child = bone.GetChild(i);
                    if (child.name == colliderName)
                    {
                        return child.GetComponent<Collider>();
                    }
                }
                return null;
            };
            
            foreach (CollisionShapeData collider in boneColliders)
            {
                string colliderName = collider.boneName + "_" + collider.name;
                Transform bone = FindBone(collider.boneName);
                Collider existingCollider = FindColliderObj(colliderName, bone);                

                g = new GameObject();
                g.transform.SetParent(parent.transform);
                g.name = colliderName;
                
                Transform t = g.transform;
                t.position = collider.translation * modelScale;
                t.rotation = collider.rotation;                

                if (collider.colliderType.Equals(ColliderType.Capsule))
                {
                    CapsuleCollider c = g.AddComponent<CapsuleCollider>();

                    c.direction = (int)collider.colliderAxis;                    
                    float radius = (collider.radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    c.radius = radius;
                    c.height = collider.length * modelScale + radius * 2f;                    
                    colliderLookup.Add(c, collider.boneName);
                    if (existingCollider) existingLookup.Add(c, existingCollider);
                }
                else
                {
                    //BoxCollider b = g.gameObject.AddComponent<BoxCollider>();
                    //b.size = collider.extent * modelScale;
                    //colliderLookup.Add(b, collider.boneName);

                    CapsuleCollider c = g.AddComponent<CapsuleCollider>();
                    c.direction = (int)collider.colliderAxis;
                    float radius;
                    float height;
                    switch (collider.colliderAxis)
                    {
                        case ColliderAxis.X: 
                            radius = (collider.extent.y + collider.extent.z) / 4f;
                            height = collider.extent.x;
                            break;                        
                        case ColliderAxis.Z:
                            radius = (collider.extent.x + collider.extent.y) / 4f;
                            height = collider.extent.z;
                            break;
                        case ColliderAxis.Y:
                        default:
                            radius = (collider.extent.x + collider.extent.z) / 4f;
                            height = collider.extent.y;
                            break;
                    }
                    c.radius = (radius - collider.margin * PHYSICS_SHRINK_COLLIDER_RADIUS) * modelScale;
                    c.height = height * modelScale;
                    colliderLookup.Add(c, collider.boneName);
                    if (existingCollider) existingLookup.Add(c, existingCollider);
                }                
            }
            parent.transform.Rotate(Vector3.left, 90);
            parent.transform.localScale = new Vector3(-1f, 1f, 1f);

            if (aPose) FixColliderAPose(objects, colliderLookup);

            // as the transforms have moved, need to re-sync the transforms in the physics engine
            UnityEngine.Physics.SyncTransforms(); 

            List<Collider> listColliders = new List<Collider>(colliderLookup.Count);

            // revert all existing prefabs overrides...
            foreach (KeyValuePair<Collider, Collider> collPair in existingLookup)
            {
                PrefabUtility.RevertObjectOverride(collPair.Value, InteractionMode.UserAction);
            }

            foreach (KeyValuePair<Collider, string> collPair in colliderLookup)
            {
                Collider transformedCollider = collPair.Key;
                string colliderBone = collPair.Value;

                Transform bone = FindBone(colliderBone);
                if (bone)
                {
                    if (existingLookup.TryGetValue(transformedCollider, out Collider existingCollider))
                    {
                        // reparent with keep position
                        transformedCollider.transform.SetParent(bone, true);
                        // copy the transformed collider to the existing if they match
                        CopyCollider(transformedCollider, existingCollider);
                        // delete the transformed collider
                        GameObject.DestroyImmediate(transformedCollider.gameObject);
                        // add to list of colliders
                        listColliders.Add(existingCollider);
                    }
                    else
                    {
                        // reparent with keep position
                        transformedCollider.transform.SetParent(bone, true);
                        // add to list of colliders
                        listColliders.Add(transformedCollider);
                    }
                }
            }
            
            // add collider manager to prefab root
            ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
            if (colliderManager == null) colliderManager = prefabInstance.AddComponent<ColliderManager>();

            // add colliders to manager
            if (colliderManager)
            {
                colliderManager.characterGUID = characterGUID;
                colliderManager.AddColliders(listColliders);
            }

            GameObject.DestroyImmediate(parent);
        }

        private bool CopyCollider(Collider from, Collider to)
        {
            if (from.GetType() != to.GetType()) return false;

            if (from.GetType() == typeof(CapsuleCollider))
            {
                CapsuleCollider ccFrom = (CapsuleCollider)from;
                CapsuleCollider ccTo = (CapsuleCollider)to;                
                ccTo.direction = ccFrom.direction;
                ccTo.radius = ccFrom.radius;
                ccTo.height = ccFrom.height;
                ccTo.center = ccFrom.center;
                ccTo.transform.SetPositionAndRotation(ccFrom.transform.position, ccFrom.transform.rotation);
                ccTo.transform.localScale = ccFrom.transform.localScale;
                ccTo.enabled = ccFrom.enabled;                
                return true;
            }
            else if (from.GetType() == typeof(SphereCollider))
            {
                SphereCollider ccFrom = (SphereCollider)from;
                SphereCollider ccTo = (SphereCollider)to;                
                ccTo.radius = ccFrom.radius;
                ccTo.center = ccFrom.center;
                ccTo.transform.SetPositionAndRotation(ccFrom.transform.position, ccFrom.transform.rotation);
                ccTo.transform.localScale = ccFrom.transform.localScale;
                ccTo.enabled = ccFrom.enabled;                
                return true;
            }

            return false;
        }

        private void AddCloth()
        {
            clothMeshes.Clear();
            List<string> hairMeshNames = FindHairMeshes();
            Transform[] transforms = prefabInstance.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                GameObject obj = t.gameObject;
                foreach (SoftPhysicsData data in softPhysics)
                {
                    string meshName = obj.name;
                    if (meshName.iContains("_Extracted"))
                    {
                        meshName = meshName.Remove(meshName.IndexOf("_Extracted"));
                    }

                    if (meshName == data.meshName)
                    {
                        if (CanAddPhysics(meshName, hairMeshNames))
                        {
                            DoCloth(obj, meshName);
                            clothMeshes.Add(obj);
                        }
                        else
                        {
                            RemoveCloth(obj);
                        }
                    }
                }
            }

            ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
            if (colliderManager)
            {
                colliderManager.clothMeshes = clothMeshes.ToArray();
            }
        }        

        private bool CanAddPhysics(SoftPhysicsData data)
        {
            if (data != null)
            {
                if (data.isHair)
                {
                    if (addHairPhysics) return true;
                }
                else
                {
                    if (addClothPhysics) return true;
                }
            }

            return false;
        }
        
        private bool CanAddPhysics(string meshName, List<string>hairMeshNames)
        {
            if (hairMeshNames.iContains(meshName))
            {
                return addHairPhysics;
            }
            else
            {
                return addClothPhysics;
            }
        }        

        private List<string> FindHairMeshes()
        {
            List<string> hairMeshNames = new List<string>();
            Transform[] transforms = prefabInstance.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                GameObject obj = t.gameObject;
                foreach (SoftPhysicsData data in softPhysics)
                {
                    string meshName = obj.name;
                    if (meshName.iContains("_Extracted"))
                    {
                        meshName = meshName.Remove(meshName.IndexOf("_Extracted"));
                    }

                    if (meshName == data.meshName)
                    {
                        if (data.isHair && !hairMeshNames.Contains(meshName)) 
                            hairMeshNames.Add(meshName);
                    }
                }
            }

            return hairMeshNames;
        }

        private void DoCloth(GameObject clothTarget, string meshName)
        {            
            SkinnedMeshRenderer renderer = clothTarget.GetComponent<SkinnedMeshRenderer>();
            if (!renderer) return;
            Mesh mesh = renderer.sharedMesh;
            if (!mesh) return;
                       
            foreach (SoftPhysicsData data in softPhysics)
            {
                Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                SetWeightMapImport(weightMap);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WeightMapper mapper = clothTarget.GetComponent<WeightMapper>();
            if (!mapper) mapper = clothTarget.AddComponent<WeightMapper>();
            List<WeightMapper.PhysicsSettings> settingsList = new List<WeightMapper.PhysicsSettings>();

            for (int i = 0; i < mesh.subMeshCount; i++)//
            {
                Material mat = renderer.sharedMaterials[i];
                string sourceName = mat.name;
                if (sourceName.iContains("_2nd_Pass")) continue;
                if (sourceName.iContains("_1st_Pass"))
                {
                    sourceName = sourceName.Remove(sourceName.IndexOf("_1st_Pass"));
                }

                foreach (SoftPhysicsData data in softPhysics)
                {
                    if (data.materialName == sourceName &&
                        data.meshName == meshName &&
                        CanAddPhysics(data))
                    {
                        WeightMapper.PhysicsSettings settings = new WeightMapper.PhysicsSettings();

                        settings.name = sourceName;
                        settings.activate = data.activate;
                        settings.gravity = data.gravity;
                        settings.selfCollision = data.selfCollision;
                        settings.softRigidCollision = data.softRigidCollision;
                        settings.softRigidMargin = data.softRigidMargin;

                        if (data.isHair)
                        {
                            // hair meshes degenerate quickly if less than full stiffness 
                            // (too dense, too many verts?)
                            settings.bending = 0f;
                            settings.stretch = 0f;
                        }
                        else
                        {
                            settings.bending = data.bending;
                            settings.stretch = data.stretch;
                        }

                        settings.solverFrequency = data.solverFrequency;
                        settings.stiffnessFrequency = data.stiffnessFrequency;
                        settings.mass = data.mass;
                        settings.friction = data.friction;
                        settings.damping = data.damping;
                        settings.selfMargin = data.selfMargin;
                        settings.maxDistance = 20f;
                        settings.maxPenetration = 10f;
                        settings.colliderThreshold = PHYSICS_WEIGHT_MAP_DETECT_COLLIDER_THRESHOLD;

                        Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                        if (!weightMap) weightMap = Texture2D.blackTexture;
                        settings.weightMap = weightMap;

                        settingsList.Add(settings);
                    }
                }
            }

            mapper.settings = settingsList.ToArray();
            mapper.characterGUID = characterGUID;
            mapper.ApplyWeightMap();
        }

        public void RemoveCloth(GameObject obj)
        {
            Cloth cloth = obj.GetComponent<Cloth>();
            WeightMapper mapper = obj.GetComponent<WeightMapper>();

            if (cloth) Component.DestroyImmediate(cloth);
            if (mapper) Component.DestroyImmediate(mapper);
        }

        private Texture2D GetTextureFrom(string jsonTexturePath, string materialName, string suffix, out string name, bool search)
        {
            Texture2D tex = null;
            name = "";

            // try to find the texture from the supplied texture path (usually from the json data).
            if (!string.IsNullOrEmpty(jsonTexturePath))
            {
                // try to load the texture asset directly from the json path.
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Util.CombineJsonTexPath(fbxFolder, jsonTexturePath));
                name = Path.GetFileNameWithoutExtension(jsonTexturePath);

                // if that fails, try to find the texture by name in the texture folders.
                if (!tex && search)
                {
                    tex = Util.FindTexture(textureFolders.ToArray(), name);
                }
            }

            // as a final fallback try to find the texture from the material name and suffix.
            if (!tex && search)
            {
                name = materialName + "_" + suffix;
                tex = Util.FindTexture(textureFolders.ToArray(), name);
            }

            return tex;
        }

        private void SetWeightMapImport(Texture2D tex)
        {
            if (!tex) return;

            // now fix the import settings for the texture.
            string path = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.sRGBTexture = false;                                                                        
            importer.alphaIsTransparency = false;                            
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 0;
            importer.maxTextureSize = 256;

            AssetDatabase.WriteImportSettingsIfDirty(path);
        }        
        
        public static GameObject RebuildPhysics(CharacterInfo characterInfo)
        {            
            GameObject prefabAsset = characterInfo.PrefabAsset;

            if (prefabAsset)
            {
                GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);                

                if (prefabAsset && prefabInstance && characterInfo.PhysicsJsonData != null)
                {
                    characterInfo.ShaderFlags |= CharacterInfo.ShaderFeatureFlags.ClothPhysics;

                    Physics physics = new Physics(characterInfo, prefabAsset, prefabInstance);
                    physics.AddPhysics();
                    characterInfo.Write();
                }

                if (prefabInstance) GameObject.DestroyImmediate(prefabInstance);
            }

            return prefabAsset;
        }

        void FixColliderAPose(Transform[] objects, Dictionary<Collider, string> colliderLookup)
        {
            Func<string, Transform> FindBone = (boneName) => Array.Find(objects, o => o.name.iEquals(boneName));            

            Transform leftArm = FindBone("CC_Base_L_Upperarm");
            if (!leftArm) leftArm = FindBone("L_Upperarm");

            Transform rightArm = FindBone("CC_Base_R_Upperarm");
            if (!leftArm) rightArm = FindBone("R_Upperarm");

            foreach(KeyValuePair<Collider, string> pair in colliderLookup)
            {
                Collider c = pair.Key;
                string boneName = pair.Value;
                Transform bone = FindBone(boneName);
                if (bone)
                {
                    if (bone == leftArm || bone.IsChildOf(leftArm))
                    {
                        c.transform.RotateAround(leftArm.position, Vector3.forward, -30f);
                    }
                    else if (bone == rightArm || bone.IsChildOf(rightArm))
                    {
                        c.transform.RotateAround(rightArm.position, Vector3.forward, 30f);
                    }
                }
            }
        }

    }
}
