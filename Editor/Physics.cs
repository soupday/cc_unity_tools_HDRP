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

            public SoftPhysicsData(string mesh, string material, QuickJSON softPhysicsJson)
            {
                meshName = mesh;
                materialName = material;

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
                return 1f;
            }

            set
            {
                EditorPrefs.SetFloat("RL_Physics_Shrink_Collider_Radius", value);
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
        private List<string> textureFolders;

        public Physics(CharacterInfo info, GameObject prefabAsset, GameObject prefabInstance, QuickJSON physicsJson)
        {
            this.prefabAsset = prefabAsset;
            this.prefabInstance = prefabInstance;
            boneColliders = new List<CollisionShapeData>();
            softPhysics = new List<SoftPhysicsData>();
            clothMeshes = new List<GameObject>();
            modelScale = 0.01f;
            fbxFolder = info.folder;
            characterName = info.name;
            fbxFolder = info.folder;
            addClothPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.ClothPhysics) > 0;
            addHairPhysics = (info.ShaderFlags & CharacterInfo.ShaderFeatureFlags.HairPhysics) > 0;
            string fbmFolder = Path.Combine(fbxFolder, characterName + ".fbm");
            string texFolder = Path.Combine(fbxFolder, "textures", characterName);
            textureFolders = new List<string>() { fbmFolder, texFolder };

            ReadPhysicsJson(physicsJson);
        }

        private void ReadPhysicsJson(QuickJSON physicsJson)
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
                    QuickJSON materialsJson = meshJson.ObjectValue.GetObjectAtPath("Materials");
                    if (materialsJson != null)
                    {
                        foreach (MultiValue matJson in materialsJson.values)
                        {
                            string materialName = matJson.Key;
                            if (matJson.ObjectValue != null)
                                softPhysics.Add(new SoftPhysicsData(meshName, materialName, matJson.ObjectValue));
                        }
                    }
                }
            }
        }

        public GameObject AddPhysics()
        {                        
            AddColliders();
            AddCloth();

            return PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(prefabAsset));
        }

        private void AddColliders()
        {
            GameObject parent = new GameObject();
            GameObject g;
            Transform[] objs = prefabInstance.GetComponentsInChildren<Transform>();
            Dictionary<Collider, string> colliderLookup = new Dictionary<Collider, string>();

            foreach (CollisionShapeData collider in boneColliders)
            {
                g = new GameObject();
                g.transform.SetParent(parent.transform);
                g.name = collider.boneName + "_" + collider.name;

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
                }                
            }
            parent.transform.Rotate(Vector3.left, 90);
            parent.transform.localScale = new Vector3(-1f, 1f, 1f);

            List<Collider> listColliders = new List<Collider>(colliderLookup.Count);

            foreach (KeyValuePair<Collider, string> collPair in colliderLookup)
            {
                for (int i = 0; i < objs.Length; i++)
                {
                    if (collPair.Value.Equals(objs[i].name))
                    {
                        collPair.Key.transform.SetParent(objs[i].transform, true);
                        listColliders.Add(collPair.Key);
                    }
                }
            }

            // add collider manager to prefab root
            ColliderManager colliderManager = prefabInstance.GetComponent<ColliderManager>();
            if (colliderManager == null) colliderManager = prefabInstance.AddComponent<ColliderManager>();

            // add colliders to manager
            if (colliderManager) colliderManager.AddColliders(listColliders);
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

        private bool MaterialIsHair(Material mat)
        {
            bool isHair = false;

            if (mat)
            {                
                if (mat.shader.name.iContains(Pipeline.SHADER_DEFAULT_HAIR) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR_1ST_PASS) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR_2ND_PASS) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR_COVERAGE))
                    isHair = true;
            }

            return isHair;
        }

        private bool CanAddPhysics(Material mat)
        {
            if (mat)
            {
                if (MaterialIsHair(mat))
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

        private bool HasHairMaterials(GameObject obj)
        {
            SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
            if (renderer)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (MaterialIsHair(mat)) return true;
                }
            }

            return false;
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
                        if (HasHairMaterials(obj) && !hairMeshNames.Contains(meshName)) 
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
                if (CanAddPhysics(mat))
                {
                    string sourceName = mat.name;
                    if (sourceName.iContains("_2nd_Pass")) continue;
                    if (sourceName.iContains("_1st_Pass"))
                    {
                        sourceName = sourceName.Remove(sourceName.IndexOf("_1st_Pass"));
                    }

                    foreach (SoftPhysicsData data in softPhysics)
                    {
                        if (data.materialName == sourceName && data.meshName == meshName)
                        {
                            WeightMapper.PhysicsSettings settings = new WeightMapper.PhysicsSettings();

                            settings.name = sourceName;
                            settings.activate = data.activate;
                            settings.gravity = data.gravity;
                            settings.selfCollision = data.selfCollision;
                            settings.softRigidCollision = data.softRigidCollision;
                            settings.softRigidMargin = data.softRigidMargin;

                            if (MaterialIsHair(mat))
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

                            Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                            if (!weightMap) weightMap = Texture2D.blackTexture;
                            settings.weightMap = weightMap;

                            settingsList.Add(settings);
                        }
                    }
                }
            }

            mapper.settings = settingsList.ToArray();           

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

        

        
    }
}
