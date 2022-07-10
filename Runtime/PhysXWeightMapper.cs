using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Reallusion.Import
{
    [Serializable]
    public class PhysXWeightMapper : MonoBehaviour
    {
#if UNITY_EDITOR
        [Serializable]
        public class PhysicsSettings
        {
            public string name;
            [Space(8)]
            [Range(0f, 1f)]
            public float maxDistance = 0.2f;
            [Range(0f, 1f)]
            public float maxPenetration = 0.1f;
            [Space(8)]
            public Texture2D weightMap;
            [Range(0.01f, 8f)]
            public float weightMapPower = 1f;
            [Range(-1f, 1f)]
            public float weightMapOffset = 0f;
            [Range(0f, 10f)]
            public float weightMapScale = 1f;
            [Space(8)]
            public bool activate;
            public bool gravity;
            [Space(8)]
            [Range(0f, 10f)]
            public float mass;
            [Range(0f, 1f)]
            public float friction;
            [Range(0f, 1f)]
            public float damping;
            [HideInInspector]
            [Range(0f, 1f)]
            public float drag;            
            [Range(0f, 100f)]
            public float stretch;
            [Range(0f, 100f)]
            public float bending;
            [Space(8)]
            public bool softRigidCollision;
            public float softRigidMargin;
            [HideInInspector]
            public bool selfCollision;
            [HideInInspector]
            public float selfMargin;
            [Space(8)]
            [Range(1f, 500f)]
            public float solverFrequency;
            [Range(1f, 50f)]
            public float stiffnessFrequency;            
        }                

        public PhysicsSettings[] settings;        

        public void DoCloth()
        {
            GameObject clothTarget = gameObject;
            SkinnedMeshRenderer renderer = clothTarget.GetComponent<SkinnedMeshRenderer>();
            if (!renderer) return;
            Mesh mesh = renderer.sharedMesh;
            if (!mesh) return;

            // add cloth component
            Cloth cloth = clothTarget.GetComponent<Cloth>();
            if (!cloth) cloth = clothTarget.AddComponent<Cloth>();            

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
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // apply weight maps to cloth coefficients and cloth settings
            for (int i = 0; i < mesh.subMeshCount; i++)//
            {
                Material mat = renderer.sharedMaterials[i];
                string sourceName = mat.name;
                if (sourceName.Contains("_2nd_Pass")) continue;
                if (sourceName.Contains("_1st_Pass"))
                {
                    sourceName = sourceName.Remove(sourceName.IndexOf("_1st_Pass"));
                }

                foreach (PhysicsSettings data in settings)
                {
                    if (data.name == sourceName && data.activate)
                    {
                        cloth.useGravity = data.gravity;
                        cloth.bendingStiffness = 1f - (data.bending / 100f);
                        cloth.stretchingStiffness = 1f - (data.stretch / 100f);
                        cloth.clothSolverFrequency = data.solverFrequency;
                        cloth.stiffnessFrequency = data.stiffnessFrequency;
                        cloth.collisionMassScale = data.mass;
                        cloth.friction = data.friction;
                        cloth.damping = data.damping;
                        cloth.selfCollisionDistance = data.selfMargin * 0.01f;
                        cloth.selfCollisionStiffness = 1f;

                        Texture2D weightMap = data.weightMap;
                        if (!weightMap) weightMap = Texture2D.blackTexture;
                        Color32[] pixels = weightMap.GetPixels32(0);
                        int w = weightMap.width;
                        int h = weightMap.height;
                        int x, y;

                        int[] tris = mesh.GetTriangles(i);
                        foreach (int vertIdx in tris)
                        {
                            if (uniqueVertices.TryGetValue(SpatialHash(mesh.vertices[vertIdx]), out int clothVert))
                            {
                                Vector2 coord = uvs[vertIdx];
                                x = Mathf.FloorToInt(coord.x * w);
                                y = Mathf.FloorToInt(coord.y * h);
                                Color32 sample = pixels[x + y * w];
                                float weight = (Mathf.Pow(sample.g / 255f, data.weightMapPower) + data.weightMapOffset) * data.weightMapScale;
                                if (data.softRigidCollision)
                                {
                                    coefficients[clothVert].maxDistance = data.maxDistance * weight;
                                    coefficients[clothVert].collisionSphereDistance = data.maxPenetration * weight;
                                }
                            }
                        }
                    }

                }
            }

            // set coefficients
            cloth.coefficients = coefficients;

            // set colliders
            GameObject characterPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (characterPrefab)
            {                
                CapsuleCollider[] colliders = characterPrefab.GetComponentsInChildren<CapsuleCollider>();
                cloth.capsuleColliders = colliders;
            }
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
#endif
    }
}