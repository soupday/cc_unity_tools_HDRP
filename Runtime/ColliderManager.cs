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
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace Reallusion.Import
{
    [Serializable]
    public class ColliderManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // additions to transpose the capsule maipulation code        
        [HideInInspector][Flags] public enum ColliderType { UnityEngine = 1, MagicaCloth2 = 2, DynamicBone = 4, Unknown = 8 }
        [HideInInspector] public ColliderType currentEditType;
        [HideInInspector] public ColliderType availableColliderTypes;
        [HideInInspector] public enum ManipulatorType { position, rotation, scale }
        [HideInInspector] public ManipulatorType manipulator = ManipulatorType.position;
        [HideInInspector] public string[] manipulatorArray = { "Position", "Rotation", "Scale" };
        [HideInInspector] public enum ColliderAxis { x, y, z }
        [HideInInspector] public bool transformSymmetrically = true;
        [HideInInspector] public bool frameSymmetryPair = false;
        [HideInInspector] public enum MirrorPlane { x, z }
        [HideInInspector] public MirrorPlane selectedMirrorPlane;
        [HideInInspector] public IList genericColliderList;
        [HideInInspector] public bool magicaCloth2Available;
        [HideInInspector] public bool dynamicBoneAvailable;
        [HideInInspector] public Type magicaColliderType;
        [HideInInspector] public Type dynamicBoneColliderType;
        [HideInInspector] public MethodInfo magicaUpdate;
        [HideInInspector] public MethodInfo magicaSetSize;
        [HideInInspector] public MethodInfo magicaGetSize;
        [HideInInspector] public bool hasGizmoUtility = false;
        [HideInInspector] public List<AbstractCapsuleCollider> abstractedCapsuleColliders;
        [HideInInspector] public AbstractCapsuleCollider cachedSelectedCollider;
        [HideInInspector] public AbstractCapsuleCollider cachedMirrorImageCollider;
        [HideInInspector] public AbstractCapsuleCollider selectedAbstractCapsuleCollider;
        [HideInInspector] public AbstractCapsuleCollider mirrorImageAbstractCapsuleCollider;

        [Serializable]
        public class AbstractCapsuleCollider
        {
            public Transform transform;
            public bool isEnabled;
            public Vector3 localPosition;          
            public Quaternion localRotation;
            public float height;
            public float radius;
            public string name;
            public ColliderAxis axis;
            public ColliderType colliderTypes;
            public UnityEngine.Object nativeRef;
            public UnityEngine.Object magicaRef;
            public UnityEngine.Object dynamicRef;

            public AbstractCapsuleCollider()
            {

            }

            public AbstractCapsuleCollider(Transform _transform, Vector3 _position, Quaternion _rotation, float _height, float _radius, string _name, ColliderAxis _axis, bool _enabled = true, ColliderType _colliderTypes = ColliderType.Unknown, UnityEngine.Object _native = null, UnityEngine.Object _magica = null, UnityEngine.Object _dynamic = null)
            {
                transform = _transform;                
                localPosition = _position;
                localRotation = _rotation;
                height = _height;
                radius = _radius;
                name = _name;
                axis = _axis;
                isEnabled = _enabled;
                colliderTypes = _colliderTypes;
                nativeRef = _native;
                magicaRef = _magica;
                dynamicRef = _dynamic;
            }

            public static bool IsNullOrEmpty(AbstractCapsuleCollider c)
            {
                bool result = false;
                if (c == null)
                    return true;

                if (c.height == 0f && c.radius == 0f && string.IsNullOrEmpty(c.name))
                    return true;

                return result;
            }
        }

        [Serializable]
        public class GizmoState
        {
            public bool gizmosEnabled;
            public bool capsuleEnabled;
            public bool clothEnabled;
            public bool sphereEnabled;
            public bool boxEnabled;
            public bool magicaCapsuleEnabled;
            public bool magicaCapsuleIconEnabled;
            public bool magicaClothEnabled;
            public bool magicaClothIconEnabled;
            public bool magicaSphereEnabled;
            public bool magicaSphereIconEnabled;
            public bool magicaPlaneEnabled;
            public bool magicaPlaneIconEnabled;
            public float iconSize;
            public bool iconsEnabled;

            public GizmoState()
            {
                gizmosEnabled = false;
                capsuleEnabled = false;
                clothEnabled = false;
                sphereEnabled = false;
                boxEnabled = false;
                magicaCapsuleEnabled = false;
                magicaCapsuleIconEnabled = false;
                magicaClothEnabled = false;
                magicaClothIconEnabled = false;
                magicaSphereEnabled = false;
                magicaSphereIconEnabled = false;
                magicaPlaneEnabled = false;
                magicaPlaneIconEnabled = false;
                iconSize = 0f;
                iconsEnabled = false;
            }
        }
        
        [HideInInspector]
        public string[] gizmoNames = new string[]
        {
            "CapsuleCollider",
            "Cloth",
            "SphereCollider",
            "BoxCollider",
            "MagicaCapsuleCollider",
            "MagicaCloth",
            "MagicaSphereCollider",
            "MagicaPlaneCollider"            
        };

        public void UpdateColliderFromAbstract(Vector3 mirrorPosDiff, Quaternion localRotation)
        {
            //int selectedIndex = abstractedCapsuleColliders.IndexOf(selectedAbstractCapsuleCollider);            
            //var genericCollider = genericColliderList[selectedIndex] as UnityEngine.Object;

            Vector3 localEuler = localRotation.eulerAngles;
            // transform (as a property of the collider) is inherited and the object ref is already stored
            //SetTypeProperty(genericCollider, "transform", selectedAbstractCapsuleCollider.transform);
            //SetTypeProperty(genericCollider, "height", selectedAbstractCapsuleCollider.height);
            //SetTypeProperty(genericCollider, "radius", selectedAbstractCapsuleCollider.radius);
            SyncNativeCollider(selectedAbstractCapsuleCollider);
            if (magicaCloth2Available) SyncMagicaCollider(selectedAbstractCapsuleCollider);
            if (dynamicBoneAvailable) SyncDynamicBoneCollider(selectedAbstractCapsuleCollider);

            //if (mirrorImageAbstractCapsuleCollider != null)
            if (!AbstractCapsuleCollider.IsNullOrEmpty(mirrorImageAbstractCapsuleCollider))
            {
                mirrorImageAbstractCapsuleCollider.height = selectedAbstractCapsuleCollider.height;
                mirrorImageAbstractCapsuleCollider.radius = selectedAbstractCapsuleCollider.radius;

                //int mirrorIndex = abstractedCapsuleColliders.IndexOf(mirrorImageAbstractCapsuleCollider);
                //var mirrorGenericCollider = genericColliderList[mirrorIndex] as UnityEngine.Object;
                //SetTypeProperty(mirrorGenericCollider, "height", mirrorImageAbstractCapsuleCollider.height);
                //SetTypeProperty(mirrorGenericCollider, "radius", mirrorImageAbstractCapsuleCollider.radius);
                SyncNativeCollider(mirrorImageAbstractCapsuleCollider);
                if (magicaCloth2Available) SyncMagicaCollider(mirrorImageAbstractCapsuleCollider);
                if (dynamicBoneAvailable) SyncDynamicBoneCollider(selectedAbstractCapsuleCollider);

                Transform t = mirrorImageAbstractCapsuleCollider.transform;
                Vector3 diff = Vector3.zero;
                Quaternion rDiff = Quaternion.identity;
                switch (selectedMirrorPlane)
                {
                    case MirrorPlane.x:
                        {
                            // rotation
                            rDiff = Quaternion.Euler(localEuler.x, -localEuler.y, -localEuler.z);

                            // position                            
                            diff = new Vector3(-mirrorPosDiff.x, mirrorPosDiff.y, mirrorPosDiff.z);

                            break;
                        }
                    case MirrorPlane.z:
                        {
                            // rotation
                            rDiff = Quaternion.Euler(localEuler.x, -localEuler.y, -localEuler.z);

                            // position                            
                            diff = new Vector3(-mirrorPosDiff.x, mirrorPosDiff.y, mirrorPosDiff.z);

                            break;
                        }
                }
                t.localPosition = diff;
                t.localRotation = rDiff;

                mirrorImageAbstractCapsuleCollider.transform = t;
            }
        }

        public void SyncNativeCollider(AbstractCapsuleCollider collider)
        {
            if (collider.nativeRef != null)
            {
                CapsuleCollider c = collider.nativeRef as CapsuleCollider;
                c.height = collider.height;
                c.radius = collider.radius;
            }
        }

        public void SyncMagicaCollider(AbstractCapsuleCollider collider)
        {
            if (collider.magicaRef != null)
            {
                var c = collider.magicaRef;
                if (magicaColliderType == null)                
                    magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");                

                if (magicaSetSize == null || magicaUpdate == null)
                {
                    magicaSetSize = c.GetType().GetMethod("SetSize",
                                                BindingFlags.Public | BindingFlags.Instance,
                                                null,
                                                CallingConventions.Any,
                                                new Type[] { typeof(Vector3) },
                                                null);

                    magicaUpdate = c.GetType().GetMethod("UpdateParameters");
                }
                Vector3 sizeVector = new Vector3(collider.radius, collider.radius, collider.height);
                object[] inputParams = new object[] { sizeVector };
                magicaSetSize.Invoke(c, inputParams);
                magicaUpdate.Invoke(c, new object[] { });
            }
        }

        public void SyncDynamicBoneCollider(AbstractCapsuleCollider collider)
        {
            if (collider.dynamicRef != null)
            {
                var c = collider.dynamicRef;
                if (dynamicBoneColliderType == null)
                    dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");

                SetTypeField(dynamicBoneColliderType, c, "m_Radius", collider.radius);
                SetTypeField(dynamicBoneColliderType, c, "m_Height", collider.height);
            }
        }

        public void CacheCollider(AbstractCapsuleCollider collider, AbstractCapsuleCollider mirrorCollider = null)
        {  
            cachedSelectedCollider = new AbstractCapsuleCollider(null, collider.transform.localPosition, collider.transform.localRotation, collider.height, collider.radius, collider.name, collider.axis);
            
            if (mirrorCollider != null)
            {
                cachedMirrorImageCollider = new AbstractCapsuleCollider(null, mirrorCollider.transform.localPosition, mirrorCollider.transform.localRotation, mirrorCollider.height, mirrorCollider.radius, mirrorCollider.name, mirrorCollider.axis);
            }
            else
            {
                cachedMirrorImageCollider = null;
            }
        }

        public AbstractCapsuleCollider DetermineMirrorImageCollider(AbstractCapsuleCollider collider, List<AbstractCapsuleCollider> colliderList)
        {
            if (!transformSymmetrically) { return null; }

            if (DetermineMirrorImageColliderName(collider.name, out string mirrorName, out selectedMirrorPlane))                   
                return colliderList.Find(x => x.name == mirrorName);            
            else
                return null;
        }

        public bool DetermineMirrorImageColliderName(string name, out string mirrorName, out MirrorPlane mirrorPlane)
        {
            // All mirror image determination rules in one place
            mirrorName = "";
            mirrorPlane = MirrorPlane.x;

            if (name.Contains("_L_"))
            {
                mirrorName = name.Replace("_L_", "_R_");
                mirrorPlane = MirrorPlane.x;
            }
            else if (name.Contains("_R_"))
            {
                mirrorName = name.Replace("_R_", "_L_");
                mirrorPlane = MirrorPlane.x;
            }
            else if (name == "CC_Base_NeckTwist01_Capsule(1)")
            {
                mirrorName = "CC_Base_NeckTwist01_Capsule(2)";
                mirrorPlane = MirrorPlane.z;
            }
            else if (name == "CC_Base_NeckTwist01_Capsule(2)")
            {
                mirrorName = "CC_Base_NeckTwist01_Capsule(1)";
                mirrorPlane = MirrorPlane.z;
            }
            else if (name == "CC_Base_Hip_Capsule")
            {
                mirrorName = "CC_Base_Hip_Capsule(0)";
                mirrorPlane = MirrorPlane.x;
            }
            else if (name == "CC_Base_Hip_Capsule(0)")
            {
                mirrorName = "CC_Base_Hip_Capsule";
                mirrorPlane = MirrorPlane.x;
            }

            return !string.IsNullOrEmpty(mirrorName);
        }

        public void ResetColliderFromCache()
        {
            if (selectedAbstractCapsuleCollider != null && cachedSelectedCollider != null)
            {
                //int index = abstractedCapsuleColliders.IndexOf(selectedAbstractCapsuleCollider);
                //if (index != -1)
                    UpdateColliderSettings(cachedSelectedCollider, selectedAbstractCapsuleCollider);//, index);                
            }
            if (!AbstractCapsuleCollider.IsNullOrEmpty(mirrorImageAbstractCapsuleCollider) && !AbstractCapsuleCollider.IsNullOrEmpty(cachedMirrorImageCollider))
            {
                //int mirrorIndex = abstractedCapsuleColliders.IndexOf(mirrorImageAbstractCapsuleCollider);
                //if (mirrorIndex != -1)
                    UpdateColliderSettings(cachedMirrorImageCollider, mirrorImageAbstractCapsuleCollider);//, mirrorIndex);
            }
        }

        public bool SetTypeProperty(object o, string property, object value)
        {
            PropertyInfo propertyInfo = o.GetType().GetProperty(property);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(o, value);
                return true;
            }
            return false;
        }

        public bool SetTypeField(Type t, object o, string field, object value)
        {
            FieldInfo fRoots = t.GetField(field);
            if (fRoots != null)
            {
                fRoots.SetValue(o, value);
                return true;
            }
            return false;
        }

        public static bool GetTypeProperty(object o, string property, out object value)
        {
            PropertyInfo propertyInfo = o.GetType().GetProperty(property);
            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(o);
                return true;
            }
            value = null;
            return false;
        }

        public static System.Type GetTypeInAssemblies(string typeName)
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                System.Type[] types = a.GetTypes();
                foreach (System.Type t in types)
                {
                    if (typeName == t.FullName)
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        /// See: https://stackoverflow.com/questions/10754150/dynamic-type-with-lists-in-c-sharp
        public static object CreateGeneric(Type generic, Type innerType, params object[] args)
        {
            System.Type specificType = generic.MakeGenericType(new System.Type[] { innerType });
            return Activator.CreateInstance(specificType, args);
        }

        public void ResetAbstractColliders(List<AbstractCapsuleCollider> referenceList)
        {
            if (abstractedCapsuleColliders != null && referenceList != null)
            {
                foreach (var s in referenceList)
                {
                    AbstractCapsuleCollider current = abstractedCapsuleColliders.Find(x => x.name == s.name);  // collider in the real world list corresponding to s
                    int colliderIndex = abstractedCapsuleColliders.FindIndex(x => x.name == s.name);

                    if (current != null && colliderIndex != -1)
                        UpdateColliderSettings(s, current);//, colliderIndex);
                }
            }
            SyncTransformActiveStatus();
        }

        public void ResetSingleAbstractCollider(List<AbstractCapsuleCollider> referenceList, string colliderName, bool resetMirror)
        {
            // reset the specified collider name in abstractedCapsuleColliders with data from referenceList
            if (abstractedCapsuleColliders != null && referenceList != null && !string.IsNullOrEmpty(colliderName))
            {                
                AbstractCapsuleCollider target = abstractedCapsuleColliders.Find(x => x.name == colliderName);  
                AbstractCapsuleCollider source = referenceList.Find(y => y.name == colliderName);

                int targetIndex = abstractedCapsuleColliders.FindIndex(x => x.name == colliderName);
                int sourceIndex = referenceList.FindIndex(y => y.name == colliderName);

                if (targetIndex == sourceIndex)
                    UpdateColliderSettings(source, target);//, targetIndex);

                if (resetMirror && !AbstractCapsuleCollider.IsNullOrEmpty(mirrorImageAbstractCapsuleCollider))  // determine the mirror in abstractedCapsuleColliders and reset it with data from the corresponding mirror in referenceList
                {
                    AbstractCapsuleCollider mirrorTarget = DetermineMirrorImageCollider(target, abstractedCapsuleColliders);
                    AbstractCapsuleCollider mirrorSource = DetermineMirrorImageCollider(target, referenceList);
                    int mirrorTargetIndex = abstractedCapsuleColliders.FindIndex(x => x.name == mirrorTarget.name);
                    int mirrorSourceIndex = referenceList.FindIndex(y => y.name == mirrorSource.name);

                    if (!AbstractCapsuleCollider.IsNullOrEmpty(mirrorTarget) && !AbstractCapsuleCollider.IsNullOrEmpty(mirrorSource))
                    {
                        if (mirrorTargetIndex == mirrorSourceIndex)
                            UpdateColliderSettings(mirrorSource, mirrorTarget);//, mirrorTargetIndex);
                    }
                }
            }
            SyncTransformActiveStatus();
        }

        public void UpdateColliderSettings(AbstractCapsuleCollider source, AbstractCapsuleCollider target)//, int genericIndex)
        {
            // update the real world information with the stored info
            target.transform.localPosition = source.localPosition;
            target.transform.localRotation = source.localRotation;

            target.height = source.height;
            target.radius = source.radius;
            target.name = source.name;
            target.axis = source.axis;
            target.isEnabled = source.isEnabled;

            // native UnityEngine.CapsuleCollider
            //var genericCollider = genericColliderList[genericIndex] as UnityEngine.Object;
            //SetTypeProperty(genericCollider, "height", source.height);
            //SetTypeProperty(genericCollider, "radius", source.radius);
            SyncNativeCollider(target);
            if (magicaCloth2Available) SyncMagicaCollider(target);
            if (dynamicBoneAvailable) SyncDynamicBoneCollider(selectedAbstractCapsuleCollider);
        }

        public void SyncTransformActiveStatus()
        {
            foreach (AbstractCapsuleCollider c in abstractedCapsuleColliders)
            {
                c.transform.gameObject.SetActive(c.isEnabled);
            }
        }

        public bool TransformHasAnyValidCollider(Transform t)
        {
            // return true if any valid collider is present
            GameObject go = t.gameObject;
            if (go != null)
            {
                // native
                if (go.GetComponent<CapsuleCollider>() != null) return true;

                // magica
                if (magicaCloth2Available)
                {
                    if (magicaColliderType == null)
                        magicaColliderType = GetTypeInAssemblies("MagicaCloth2.MagicaCapsuleCollider");

                    if (go.GetComponent(magicaColliderType) != null) return true;
                }

                // dynamic bone
                if (dynamicBoneAvailable)
                {
                    if (dynamicBoneColliderType == null)
                        dynamicBoneColliderType = dynamicBoneColliderType = GetTypeInAssemblies("DynamicBoneCollider");

                    if (go.GetComponent(dynamicBoneColliderType) != null) return true;
                }
            }
            return false;
        }


        [Serializable]
        public class EnableStatusGameObject
        {
            public GameObject GameObject;
            public bool EnabledStatus;

            public EnableStatusGameObject(GameObject gameObject, bool enabledStatus)
            {
                GameObject = gameObject;
                EnabledStatus = enabledStatus;
            }

            public EnableStatusGameObject()
            {

            }

        }


        //end of additions

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

        [HideInInspector] public Collider[] colliders;
        //[HideInInspector] public GameObject[] clothMeshes;
        [HideInInspector] public EnableStatusGameObject[] clothMeshes;
        //[HideInInspector] public GameObject[] magicaClothMeshes;
        [HideInInspector] public EnableStatusGameObject[] magicaClothMeshes;
        [HideInInspector] public ColliderSettings[] settings;
        [HideInInspector] public string characterGUID;
        

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

        // Accept UnityEngine.Object list as input -- in this case the objects will be transforms
        public void AddColliders(List<UnityEngine.Object> colliders)
        {
            List<ColliderSettings> settings = new List<ColliderSettings>();
            this.colliders = new Collider[colliders.Count];
            if (colliders != null)
            {
                //Debug.Log("AddColliders - Adding Object list containing: " + colliders.Count);
                if (colliders.Count > 0)
                {
                    for (int i = 0; i < colliders.Count; i++)
                    {
                        //Debug.Log("AddColliders - Found Collider Object: " + colliders[i].name + " Type: " + colliders[0].GetType().ToString());

                        if (colliders[i].GetType() == typeof(Transform))
                        {
                            Transform t = (Transform)colliders[i];
                            if (t.gameObject.GetComponent<Collider>() != null)
                            {
                                this.colliders[i] = t.gameObject.GetComponent<Collider>();
                                ColliderSettings cs = new ColliderSettings(this.colliders[i]);
                                settings.Add(cs);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateColliders()
        {
            foreach (ColliderSettings cs in settings)
            {
                cs.Update();
            }
        }
        /*
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
        */
        // Start or Update message needed to show an enable/disable checkbox in the inspector
        private void Start()
        {
            
        }
        
        private void Update()
        {
            
        }
#endif        
    }
}