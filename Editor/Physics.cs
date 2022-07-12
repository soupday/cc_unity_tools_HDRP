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
        private GameObject workingPrefab;
        private float modelScale = 0.01f;
        private float maxDistance = MAX_DISTANCE;
        private float radiusScale = 1f;
        private float radiusReduction = 0.5f / 100f;
        private bool addClothPhysics = false;
        private bool addHairPhysics = false;

        private List<CollisionShapeData> BoneColliders;
        private List<SoftPhysicsData> softPhysics;

        private string characterName;
        private string fbxFolder;
        private List<string> textureFolders;

        public Physics(CharacterInfo info, GameObject prefabAsset, QuickJSON physicsJson)
        {
            this.prefabAsset = prefabAsset;            
            BoneColliders = new List<CollisionShapeData>();
            softPhysics = new List<SoftPhysicsData>();            
            modelScale = 0.01f;
            maxDistance = MAX_DISTANCE;
            radiusScale = 0.9f;
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
                BoneColliders.Clear();

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
                                BoneColliders.Add(new CollisionShapeData(boneName, colliderName, colliderJson.ObjectValue));
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
            workingPrefab = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;

            AddColliders();
            AddCloth();

            GameObject newPrefabAsset = PrefabUtility.SaveAsPrefabAsset(workingPrefab, AssetDatabase.GetAssetPath(prefabAsset));
            UnityEngine.Object.DestroyImmediate(workingPrefab);
            workingPrefab = null;
            return newPrefabAsset;
        }

        private void AddColliders()
        {
            GameObject parent = new GameObject();
            GameObject g;
            Transform[] objs = workingPrefab.GetComponentsInChildren<Transform>();
            Dictionary<Collider, string> colliderLookup = new Dictionary<Collider, string>();

            foreach (CollisionShapeData collider in BoneColliders)
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
                    BoxCollider b = g.gameObject.AddComponent<BoxCollider>();
                    b.size = collider.extent * modelScale;
                    colliderLookup.Add(b, collider.boneName);
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
            ColliderManager colliderManager = workingPrefab.GetComponent<ColliderManager>();
            if (colliderManager == null) colliderManager = workingPrefab.AddComponent<ColliderManager>();

            // add colliders to manager
            if (colliderManager) colliderManager.AddColliders(listColliders);
        }

        private void AddCloth()
        {
            Transform[] transforms = workingPrefab.GetComponentsInChildren<Transform>();
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
                        if (CanAddPhysics(obj)) DoCloth(obj, meshName);
                        else RemoveCloth(obj);
                    }
                }
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
        
        private bool CanAddPhysics(GameObject obj)
        {
            SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
            if (renderer)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (CanAddPhysics(mat)) return true;
                }
            }

            return false;
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
            List<WeightMapper.PhysicsSettings> allSettings = new List<WeightMapper.PhysicsSettings>();            

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

                            allSettings.Add(settings);
                        }
                    }
                }
            }

            mapper.settings = allSettings.ToArray();           

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


        private static long SpatialHash(Vector3 v)
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

        private void CleanUP()
        {
            GameObject model = workingPrefab.gameObject;
            Transform[] objs = model.GetComponentsInChildren<Transform>();

            //horrible
            for (int i = 0; i < 5; i++)
            {
                foreach (Transform t in objs)
                {
                    if (t.gameObject.GetComponent<CapsuleCollider>())
                        Component.DestroyImmediate(t.gameObject.GetComponent<CapsuleCollider>());

                    if (t.gameObject.GetComponent<BoxCollider>())
                        Component.DestroyImmediate(t.gameObject.GetComponent<BoxCollider>());

                }
            }
        }            
    }
}
