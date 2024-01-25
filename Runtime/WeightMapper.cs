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
using UnityEngine.Rendering;
using UnityEditor;
using System;
using Object = UnityEngine.Object;


namespace Reallusion.Import
{
    [Serializable]
    public class WeightMapper : MonoBehaviour
    {
#if UNITY_EDITOR
        [Serializable]
        public class PhysicsSettings
        {
            public string name;
            [Space(8)]
            [Range(0f, 100f)]
            public float maxDistance = 20f;
            [Range(0f, 100f)]
            public float maxPenetration = 10f;
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
            [Range(0f, 100f)]
            public float softRigidMargin;
            [Space(8)]
            public bool selfCollision;
            [Range(0f, 10f)]
            public float selfMargin;
            [Space(8)]
            [Range(1f, 5000f)]
            public float solverFrequency;
            [Range(1f, 500f)]
            public float stiffnessFrequency;
            [Space(8)]
            [HideInInspector]
            [Range(0f, 1f)]
            public float colliderThreshold;

            public PhysicsSettings()
            {

            }

            public PhysicsSettings(PhysicsSettings ps)
            {
                Copy(ps);
            }

            public void Copy(PhysicsSettings p)
            {
                name = p.name;
                maxDistance = p.maxDistance;
                maxPenetration = p.maxPenetration;
                weightMap = p.weightMap;
                weightMapPower = p.weightMapPower;
                weightMapOffset = p.weightMapOffset;
                weightMapScale = p.weightMapScale;
                activate = p.activate;
                gravity = p.gravity;
                mass = p.mass;
                friction = p.friction;
                damping = p.damping;
                drag = p.drag;
                stretch = p.stretch;
                bending = p.bending;
                softRigidCollision = p.softRigidCollision;
                softRigidMargin = p.softRigidMargin;
                selfCollision = p.selfCollision;
                selfMargin = p.selfMargin;
                solverFrequency = p.solverFrequency;
                stiffnessFrequency = p.stiffnessFrequency;
                colliderThreshold = p.colliderThreshold;
            }
        }

        public PhysicsSettings[] settings;
        public bool updateColliders = true;
        public bool optimizeColliders = true;
        public bool includeAllLimbColliders = false;
        public bool updateConstraints = true;

        [HideInInspector]
        public string characterGUID;

        public void ApplyWeightMap()
        {
            GameObject clothTarget = gameObject;
            SkinnedMeshRenderer renderer = clothTarget.GetComponent<SkinnedMeshRenderer>();
            if (!renderer) return;
            Mesh mesh = renderer.sharedMesh;
            if (!mesh) return;

            // object scale
            Vector3 objectScale = renderer.gameObject.transform.localScale;
            float modelScale = 0.03f / (objectScale.x + objectScale.y + objectScale.z);
            float worldScale = (objectScale.x + objectScale.y + objectScale.z) / 3f;

            // add cloth component
            Cloth cloth = clothTarget.GetComponent<Cloth>();
            if (!cloth) cloth = clothTarget.AddComponent<Cloth>();

            // generate a mapping dictionary of cloth vertices to mesh vertices
            Dictionary<long, int> uniqueVertices = new Dictionary<long, int>();
            int count = 0;
            Vector3[] meshVertices = mesh.vertices;
            for (int k = 0; k < mesh.vertexCount; k++)
            {
                long hash = SpatialHash(meshVertices[k]);
                if (!uniqueVertices.ContainsKey(hash))
                {
                    uniqueVertices.Add(hash, count++);
                }
            }

            // fetch UV's
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            List<uint> selfCollisionIndices = new List<uint>();
            List<Collider> colliders = new List<Collider>();
            List<Collider> detectedColliders = new List<Collider>(colliders.Count);
            ColliderManager colliderManager = gameObject.GetComponentInParent<ColliderManager>();
            if (colliderManager) colliders.AddRange(colliderManager.colliders);
            else colliders.AddRange(gameObject.transform.parent.GetComponentsInChildren<Collider>());

            ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[cloth.coefficients.Length];
            Array.Copy(cloth.coefficients, coefficients, coefficients.Length);

            // reset coefficients
            for (int i = 0; i < cloth.coefficients.Length; i++)
            {
                coefficients[i].maxDistance = 0f;
                coefficients[i].collisionSphereDistance = 0f;
            }

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
                    if (data.name == sourceName)// && data.activate)
                    {
                        float rigidMargin = data.softRigidMargin * modelScale;
                        float selfMargin = data.selfCollision ? data.selfMargin * modelScale : 0f;
                        cloth.useGravity = data.gravity;
                        cloth.bendingStiffness = Mathf.Pow(1f - (data.bending / 100f), 0.5f);
                        cloth.stretchingStiffness = Mathf.Pow(1f - (data.stretch / 100f), 0.5f);
                        cloth.clothSolverFrequency = data.solverFrequency;
                        cloth.stiffnessFrequency = data.stiffnessFrequency;
                        cloth.collisionMassScale = data.mass;
                        cloth.friction = data.friction;
                        cloth.damping = Mathf.Pow(data.damping, 0.333f);
                        cloth.selfCollisionDistance = USE_SELF_COLLISION ? selfMargin : 0f;
                        cloth.selfCollisionStiffness = 0.2f;

                        bool doColliders = updateColliders && data.softRigidCollision;

                        if (doColliders)
                        {
                            if (optimizeColliders)
                            {
                                for (int ci = 0; ci < colliders.Count; ci++)
                                {
                                    if (colliders[ci] != null)
                                    {
                                        //Debug.Log("Collider: " + ci + "/" + colliders.Count + " is " + colliders[ci].GetType());
                                        Collider cc = colliders[ci];

                                        bool include = false;
                                        if (includeAllLimbColliders)
                                        {
                                            if (cc.name.Contains("_Thigh_")) include = true;
                                            if (cc.name.Contains("_Calf_")) include = true;
                                            if (cc.name.Contains("_Upperarm_")) include = true;
                                        }
                                        if (cc.name.Contains("_Forearm_")) include = true;
                                        if (cc.name.Contains("_Hand_")) include = true;
                                        if (include && !detectedColliders.Contains(cc))
                                        {
                                            detectedColliders.Add(cc);
                                            colliders.Remove(cc);
                                            ci--;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (Collider cc in colliders)
                                {
                                    if (!detectedColliders.Contains(cc)) detectedColliders.Add(cc);
                                }
                                colliders.Clear();
                            }
                        }

                        Texture2D weightMap = data.weightMap;
                        if (!weightMap) weightMap = Texture2D.blackTexture;
                        Color32[] pixels = weightMap.GetPixels32(0);
                        int w = weightMap.width;
                        int h = weightMap.height;
                        int wm1 = w - 1;
                        int hm1 = h - 1;
                        int x, y;

                        SubMeshDescriptor submesh = mesh.GetSubMesh(i);
                        int start = submesh.firstVertex + submesh.baseVertex;
                        int end = submesh.vertexCount + start;
                        for (int vertIdx = start; vertIdx < end; vertIdx++)
                        {
                            Vector3 vert = meshVertices[vertIdx];
                            if (uniqueVertices.TryGetValue(SpatialHash(vert), out int clothVert))
                            {
                                Vector2 coord = uvs[vertIdx];
                                x = Mathf.Max(0, Mathf.Min(wm1, Mathf.FloorToInt(0.5f + coord.x * wm1)));
                                y = Mathf.Max(0, Mathf.Min(hm1, Mathf.FloorToInt(0.5f + coord.y * hm1)));
                                Color32 sample = pixels[x + y * w];
                                float weight = Mathf.Clamp01((Mathf.Pow(sample.g / 255f, data.weightMapPower) + data.weightMapOffset)) * data.weightMapScale;
                                float maxDistance = data.maxDistance * weight * modelScale;
                                float maxPenetration = data.maxPenetration * weight * modelScale;
                                float modelMax = Mathf.Max(maxDistance, maxPenetration);
                                float worldMax = modelMax * worldScale;
                                coefficients[clothVert].maxDistance = maxDistance;
                                coefficients[clothVert].collisionSphereDistance = maxPenetration;

                                if (data.selfCollision && modelMax > selfMargin)
                                {
                                    selfCollisionIndices.Add((uint)clothVert);
                                }

                                if (doColliders && optimizeColliders &&
                                    //weight >= data.colliderThreshold &&
                                    modelMax > rigidMargin)
                                {
                                    Vector3 world = transform.localToWorldMatrix * vert;

                                    for (int ci = 0; ci < colliders.Count; ci++)
                                    {
                                        Collider cc = colliders[ci];
                                        if (cc != null)
                                        {
                                            if (cc.bounds.Contains(world))
                                            {
                                                detectedColliders.Add(cc);
                                                colliders.Remove(cc);
                                                ci--;
                                            }
                                            else if (cc.bounds.SqrDistance(world) < worldMax * worldMax)
                                            {
                                                detectedColliders.Add(cc);
                                                colliders.Remove(cc);
                                                ci--;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        cloth.enabled = data.activate;
                        this.enabled = data.activate;
                    }
                }
            }

            // set coefficients
            if (updateConstraints)
            {
                cloth.coefficients = coefficients;
            }

            cloth.SetSelfAndInterCollisionIndices(selfCollisionIndices);

            // set colliders
            if (updateColliders)
            {
                List<CapsuleCollider> detectedCapsuleColliders = new List<CapsuleCollider>();
                foreach (Collider c in detectedColliders)
                {
                    if (c.GetType() == typeof(CapsuleCollider))
                    {
                        detectedCapsuleColliders.Add((CapsuleCollider)c);
                    }
                }
                cloth.capsuleColliders = detectedCapsuleColliders.ToArray();
            }
        }

        private static long SpatialHash(Vector3 v)
        {
            const long p1 = 73868489;
            const long p2 = 23875351;
            const long p3 = 53885459;
            const long discrete = 10000000;

            long x = (long)(v.x * discrete);
            long y = (long)(v.y * discrete);
            long z = (long)(v.z * discrete);

            return (x * p1) ^ (y * p2) ^ (z * p3);
        }

        private static bool USE_SELF_COLLISION
        {
            get
            {
                if (EditorPrefs.HasKey("RL_Importer_Use_Self_Collision"))
                    return EditorPrefs.GetBool("RL_Importer_Use_Self_Collision");
                return false;
            }
        }
        /*
        private void Update()
        {
            // needed so that the component can have an enable/disable toggle
        }
        */
#endif
    }
}