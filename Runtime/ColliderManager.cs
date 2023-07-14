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
using System;

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
            [Space(4)]
            public float xAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float yAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            public float zAdjust = 0f;
            [Range(-0.5f, 0.5f)]
            [Space(4)]
            public float xRotate = 0f;
            [Range(-0.5f, 0.5f)]
            public float yRotate = 0f;
            [Range(-0.5f, 0.5f)]
            public float zRotate = 0f;

            public float radius;
            public float height;
            public Vector3 position;
            public Quaternion rotation;

            public ColliderSettings(Collider collider)
            {                
                this.collider = collider;
                FetchSettings();
            }

            public ColliderSettings(ColliderSettings cs)
            {
                Copy(cs);
            }

            public void Copy(ColliderSettings c, bool copyCollider = true)
            {
                name = c.name;
                if (copyCollider) collider = c.collider;
                radiusAdjust = c.radiusAdjust;
                heightAdjust = c.heightAdjust;
                xAdjust = c.xAdjust;
                yAdjust = c.yAdjust;
                zAdjust = c.zAdjust;
                xRotate = c.xRotate;
                yRotate = c.yRotate;
                zRotate = c.zRotate;
                radius = c.radius;
                height = c.height;
                position = c.position;
                rotation = c.rotation;
            }

            public void FetchSettings()
            {
                name = collider.name;

                if (collider.GetType() == typeof(CapsuleCollider))
                {
                    CapsuleCollider cc = (CapsuleCollider)collider;
                    radius = cc.radius;
                    height = cc.height;
                    position = cc.transform.localPosition;
                    rotation = cc.transform.localRotation;
                }
                else if (collider.GetType() == typeof(BoxCollider))
                {
                    BoxCollider bc = (BoxCollider)collider;
                    radius = Vector3.Dot(bc.size, Vector3.one) / 3f;
                    position = bc.transform.localPosition;
                    rotation = bc.transform.localRotation;
                }
                else if (collider.GetType() == typeof(SphereCollider))
                {
                    SphereCollider sc = (SphereCollider)collider;
                    radius = sc.radius;
                    position = sc.transform.localPosition;
                    rotation = sc.transform.localRotation;
                }

                radiusAdjust = 0f;
                heightAdjust = 0f;
                xAdjust = 0f;
                yAdjust = 0f;
                zAdjust = 0f;
                xRotate = 0f;
                yRotate = 0f;
                zRotate = 0f;
            }

            public void MirrorX(ColliderSettings cs)
            { 
                radiusAdjust = cs.radiusAdjust;
                heightAdjust = cs.heightAdjust;
                xAdjust = -cs.xAdjust;
                yAdjust = cs.yAdjust;
                zAdjust = cs.zAdjust;
                xRotate = cs.xRotate;
                yRotate = -cs.yRotate;
                zRotate = -cs.zRotate;
            }

            public void MirrorZ(ColliderSettings cs)
            {
                radiusAdjust = cs.radiusAdjust;
                heightAdjust = cs.heightAdjust;
                xAdjust = -cs.xAdjust;
                yAdjust = cs.yAdjust;
                zAdjust = cs.zAdjust;
                xRotate = -cs.xRotate;
                yRotate = -cs.yRotate;
                zRotate = cs.zRotate;
            }

            public void Reset(bool fetch = false)
            {
                radiusAdjust = 0f;
                heightAdjust = 0f;
                xAdjust = 0f;
                yAdjust = 0f;
                zAdjust = 0f;
                xRotate = 0f;
                yRotate = 0f;
                zRotate = 0f;
                if (fetch) FetchSettings();
            }

            public void Update()
            {
                if (collider.GetType() == typeof(CapsuleCollider))
                {
                    CapsuleCollider capsule = (CapsuleCollider)collider;                    
                    capsule.radius = radius + radiusAdjust;
                    capsule.height = height + heightAdjust;                    
                    capsule.transform.localPosition = position + new Vector3(xAdjust, yAdjust, zAdjust);
                    capsule.transform.localRotation = rotation * Quaternion.Euler(new Vector3(xRotate, yRotate, zRotate));
                }
                else if (collider.GetType() == typeof(BoxCollider))
                {
                    BoxCollider box = (BoxCollider)collider;
                    box.size = new Vector3(radius + radiusAdjust, radius + radiusAdjust, radius + radiusAdjust);
                    box.transform.localPosition = position + new Vector3(xAdjust, yAdjust, zAdjust);
                    box.transform.localRotation = rotation * Quaternion.Euler(new Vector3(xRotate, yRotate, zRotate));
                }
            }
        }

        public Collider[] colliders;
        [HideInInspector]
        public GameObject[] clothMeshes;
        [HideInInspector]
        public ColliderSettings[] settings;
        [HideInInspector]
        public string characterGUID;
        
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