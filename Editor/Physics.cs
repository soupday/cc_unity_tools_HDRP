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

        private GameObject prefabAsset;
        private GameObject workingPrefab;
        private float modelScale = 0.01f;
        private float maxDistance = 0.2f;
        private float radiusScale = 1f;

        private Dictionary<GameObject, string> colliderObjs;
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
            colliderObjs = new Dictionary<GameObject, string>();
            modelScale = 0.01f;
            maxDistance = 0.1f;
            radiusScale = 1f;
            fbxFolder = info.folder;
            characterName = info.name;
            fbxFolder = info.folder;
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
            colliderObjs.Clear();

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
                    c.radius = collider.radius * modelScale * radiusScale;
                    c.height = collider.length * modelScale + c.radius * 2f;
                }
                else
                {
                    BoxCollider b = g.gameObject.AddComponent<BoxCollider>();
                    b.size = collider.extent * modelScale;
                }
                colliderObjs.Add(g, collider.boneName);
            }
            parent.transform.Rotate(Vector3.left, 90);
            parent.transform.localScale = new Vector3(-1f, 1f, 1f);

            foreach (KeyValuePair<GameObject, string> collPair in colliderObjs)
            {
                for (int i = 0; i < objs.Length; i++)
                {
                    if (collPair.Value.Equals(objs[i].name))
                    {
                        collPair.Key.transform.SetParent(objs[i].transform, true);
                    }
                }
            }
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
                        DoCloth(obj, meshName);
                    }
                }
            }
        }

        private void DoCloth(GameObject clothTarget, string meshName)
        {            
            SkinnedMeshRenderer renderer = clothTarget.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = renderer.sharedMesh;

            // add cloth component
            if (clothTarget.GetComponent<Cloth>()) Component.DestroyImmediate(clothTarget.GetComponent<Cloth>());
            Cloth cloth = clothTarget.AddComponent<Cloth>();            

            // generate a mapping dictionary of cloth vertices to mesh vertices
            Dictionary<long, int> uniqueVertices = new Dictionary<long, int>();            
            int count = 0;
            for (int k = 0; k < mesh.vertexCount; k++)
            {
                if (!uniqueVertices.ContainsKey(SpatialHash(mesh.vertices[k])))
                {
                    uniqueVertices.Add(SpatialHash(mesh.vertices[k]), count++);
                }
            }

            // reset coefficients
            ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[cloth.coefficients.Length];
            Array.Copy(cloth.coefficients, coefficients, coefficients.Length);            
            for (int i = 0; i < cloth.coefficients.Length; i++)
            {
                coefficients[i].maxDistance = 0;
            }

            // fetch UV's
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            foreach (SoftPhysicsData data in softPhysics)
            {
                Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                SetWeightMapImport(weightMap);                
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // apply weight maps to cloth coefficients and cloth settings
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
                    if (data.materialName == sourceName && data.meshName == meshName)
                    {
                        cloth.bendingStiffness = 1f - (data.bending / 100f);
                        cloth.clothSolverFrequency = data.solverFrequency;
                        cloth.stiffnessFrequency = data.stiffnessFrequency;
                        cloth.stretchingStiffness = 1f - (data.stretch / 100f);
                        cloth.collisionMassScale = data.mass;
                        cloth.friction = data.friction;
                        cloth.damping = data.damping;                        
                        //cloth.selfCollisionDistance = data.selfMargin * modelScale;
                        //cloth.selfCollisionStiffness = 1f;

                        Texture2D weightMap = GetTextureFrom(data.weightMapPath, data.materialName, "WeightMap", out string texName, true);
                        if (!weightMap) weightMap = Texture2D.blackTexture;
                        Color32[] pixels = weightMap.GetPixels32(0);
                        int w = weightMap.width;
                        int h = weightMap.height;
                        int x, y;

                        int[] tris = mesh.GetTriangles(i);
                        Debug.Log(tris.Length);
                        foreach (int vertIdx in tris)
                        {
                            if (uniqueVertices.TryGetValue(SpatialHash(mesh.vertices[vertIdx]), out int clothVert))
                            {
                                Vector2 coord = uvs[vertIdx];
                                x = Mathf.FloorToInt(coord.x * w);
                                y = Mathf.FloorToInt(coord.y * h);
                                Color32 sample = pixels[x + y * w];
                                float weight = sample.g / 255f;                                
                                if (data.softRigidCollision)
                                {
                                    coefficients[clothVert].maxDistance = data.softRigidMargin * modelScale * weight;
                                    coefficients[clothVert].collisionSphereDistance = data.softRigidMargin * modelScale * weight;
                                }                                
                            }
                        }
                    }
                }
            }

            // set colliders
            cloth.coefficients = coefficients;
            CapsuleCollider[] colliders = new CapsuleCollider[colliderObjs.Count];
            int index = 0;
            foreach (KeyValuePair<GameObject, string> collPair in colliderObjs)
            {
                colliders[index] = collPair.Key.GetComponent<CapsuleCollider>();
                index++;
            }
            cloth.capsuleColliders = colliders;
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
            importer.maxTextureSize = 4096;

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
