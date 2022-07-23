using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Reallusion.Import
{
    [Serializable]
    public class ColliderManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [Serializable]
        public class ColliderSettings
        {
            public string name;            
            [Space(8)]
            public Collider collider;
            [Range(-0.5f, 0.5f)]
            public float radiusAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float heightAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float xAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float yAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float zAdjust = 0f;
            public float radius;
            public float height;
            public Vector3 position;

            public ColliderSettings(Collider collider)
            {                
                this.collider = collider;
                FetchSettings();
            }

            public void FetchSettings()
            {
                name = collider.name;

                if (collider.GetType() == typeof(CapsuleCollider))
                {
                    CapsuleCollider cc = (CapsuleCollider)collider;
                    radius = cc.radius;
                    height = cc.height;
                    position = cc.center;
                }
                else if (collider.GetType() == typeof(BoxCollider))
                {
                    BoxCollider bc = (BoxCollider)collider;
                    radius = Vector3.Dot(bc.size, Vector3.one) / 3f;
                    position = bc.center;
                }
                else if (collider.GetType() == typeof(SphereCollider))
                {
                    SphereCollider sc = (SphereCollider)collider;
                    radius = sc.radius;
                    position = sc.center;
                }

                radiusAdjust = 0f;
                heightAdjust = 0f;
                xAdjust = 0f;
                yAdjust = 0f;
                zAdjust = 0f;
            }

            public void MirrorX(ColliderSettings cs)
            { 
                radiusAdjust = cs.radiusAdjust;
                heightAdjust = cs.heightAdjust;
                xAdjust = -cs.xAdjust;
                yAdjust = cs.yAdjust;
                zAdjust = cs.zAdjust;
            }

            public void MirrorZ(ColliderSettings cs)
            {
                radiusAdjust = cs.radiusAdjust;
                heightAdjust = cs.heightAdjust;
                xAdjust = cs.xAdjust;
                yAdjust = cs.yAdjust;
                zAdjust = -cs.zAdjust;
            }

            public void Reset(bool fetch = false)
            {
                radiusAdjust = 0f;
                heightAdjust = 0f;
                xAdjust = 0f;
                yAdjust = 0f;
                zAdjust = 0f;
                if (fetch) FetchSettings();
            }

            public void Update()
            {
                if (collider.GetType() == typeof(CapsuleCollider))
                {
                    CapsuleCollider capsule = (CapsuleCollider)collider;                    
                    capsule.radius = radius + radiusAdjust;
                    capsule.height = height + heightAdjust;
                    capsule.center = position + new Vector3(xAdjust, yAdjust, zAdjust);
                }
                else if (collider.GetType() == typeof(BoxCollider))
                {
                    BoxCollider box = (BoxCollider)collider;
                    box.size = new Vector3(radius + radiusAdjust, radius + radiusAdjust, radius + radiusAdjust);
                    box.center = position + new Vector3(xAdjust, yAdjust, zAdjust);
                }
            }
        }

        public Collider[] colliders;
        [HideInInspector]
        public GameObject[] clothMeshes;
        [HideInInspector]
        public ColliderSettings[] settings;

        public void AddColliders(List<Collider> colliders)
        {
            List<ColliderSettings> settings = new List<ColliderSettings>();
            foreach (Collider col in colliders)
            {                
                ColliderSettings cs = new ColliderSettings(col);
                settings.Add(cs);                
            }
            this.settings = settings.ToArray();
            this.colliders = colliders.ToArray();
        }

        public void UpdateColliders()
        {
            foreach (ColliderSettings cs in settings)
            {
                cs.Update();
            }
        }

        public void RefreshData()
        {
            Collider[] allColliders = gameObject.GetComponentsInChildren<Collider>();
            List<Collider> foundColliders = new List<Collider>();
            foreach (Collider c in allColliders)
            {
                if (c.GetType() == typeof(SphereCollider) ||
                    c.GetType() == typeof(CapsuleCollider))
                {
                    foundColliders.Add(c);
                }
            }

            List<ColliderSettings> foundColliderSettings = new List<ColliderSettings>();
            foreach (Collider c in foundColliders)
            {
                foundColliderSettings.Add(new ColliderSettings(c));
            }

            SkinnedMeshRenderer[] renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<GameObject> foundClothMeshes = new List<GameObject>();
            foreach (SkinnedMeshRenderer smr in renderers)
            {
                Cloth cloth = smr.gameObject.GetComponent<Cloth>();
                if (cloth)
                {
                    foundClothMeshes.Add(smr.gameObject);
                }
            }

            colliders = foundColliders.ToArray();
            settings = foundColliderSettings.ToArray();
            clothMeshes = foundClothMeshes.ToArray();            
        }
#endif        
    }
}