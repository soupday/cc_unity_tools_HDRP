using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Reallusion.Import
{
    public enum FacialProfile { None, CC3, CC3Ex, CC4 }

    public struct FacialProfileMapping
    {        
        public string CC3;
        public string CC3Ex;
        public string CC4;

        public FacialProfileMapping(string cc3, string cc3Ex, string cc4)
        {
            CC3 = cc3;
            CC3Ex = cc3Ex;
            CC4 = cc4;
        }

        public bool HasMapping(string blendShapeName)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                return false;

            if (CC3 == blendShapeName || CC3Ex == blendShapeName || CC4 == blendShapeName)
                return true;

            return false;
        }

        public bool HasMapping(string blendShapeName, FacialProfile from)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                return false;

            if (from == FacialProfile.CC3 && CC3 == blendShapeName) return true;
            if (from == FacialProfile.CC3Ex && CC3Ex == blendShapeName) return true;
            if (from == FacialProfile.CC4 && CC4 == blendShapeName) return true;

            return false;
        }

        public string GetMapping(FacialProfile to)
        {
            if (to == FacialProfile.CC3) return CC3;
            if (to == FacialProfile.CC3Ex) return CC3Ex;
            if (to == FacialProfile.CC4) return CC4;
            return null;
        }
    }
    
    public static class FacialProfileMapper
    {
        public static List<FacialProfileMapping> facialProfileMaps = new List<FacialProfileMapping>() {
            //new FacialProfileMapping("", "", ""),            
            new FacialProfileMapping("Brow_Raise_Inner_L/R", "A01_Brow_Inner_Up", "Brow_Raise_Inner_L/R"), // Brow_Raise_Inner_L/R
            new FacialProfileMapping("Brow_Drop_L", "A02_Brow_Down_Left", "Brow_Drop_L"),
            new FacialProfileMapping("Brow_Drop_R", "A03_Brow_Down_Right", "Brow_Drop_R"),
            new FacialProfileMapping("Brow_Raise_Outer_L", "A04_Brow_Outer_Up_Left", "Brow_Raise_Outer_L"),
            new FacialProfileMapping("Brow_Raise_Outer_R", "A05_Brow_Outer_Up_Right", "Brow_Raise_Outer_R"),
            new FacialProfileMapping("", "A06_Eye_Look_Up_Left", "Eye_L_Look_Up"),
            new FacialProfileMapping("", "A07_Eye_Look_Up_Right", "Eye_R_Look_Up"),
            new FacialProfileMapping("", "A08_Eye_Look_Down_Left", "Eye_L_Look_Down"),
            new FacialProfileMapping("", "A09_Eye_Look_Down_Right", "Eye_R_Look_Down"),
            new FacialProfileMapping("", "A10_Eye_Look_Out_Left", "Eye_L_Look_L"),
            new FacialProfileMapping("", "A11_Eye_Look_In_Left", "Eye_L_Look_R"),
            new FacialProfileMapping("", "A12_Eye_Look_In_Right", "Eye_R_Look_L"),
            new FacialProfileMapping("", "A13_Eye_Look_Out_Right", "Eye_R_Look_R"),
            new FacialProfileMapping("Eye_Blink", "Eye_Blink", "Eyes_Blink"),
            new FacialProfileMapping("Eye_Blink_L", "A14_Eye_Blink_Left", "Eye_Blink_L"),
            new FacialProfileMapping("Eye_Blink_R", "A15_Eye_Blink_Right", "Eye_Blink_R"),
            new FacialProfileMapping("Eye_Squint_L", "A16_Eye_Squint_Left", "Eye_Squint_L"),
            new FacialProfileMapping("Eye_Squint_R", "A17_Eye_Squint_Right", "Eye_Squint_R"),
            new FacialProfileMapping("Eye_Wide_L", "A18_Eye_Wide_Left", "Eye_Wide_L"),
            new FacialProfileMapping("Eye_Wide_R", "A19_Eye_Wide_Right", "Eye_Wide_R"),
            new FacialProfileMapping("Cheek_Puff_L/R", "A20_Cheek_Puff", "Cheek_Puff_L/R"), //Cheek_Puff_L/R
            new FacialProfileMapping("Cheek_Raise_L", "A21_Cheek_Squint_Left", "Cheek_Raise_L"),
            new FacialProfileMapping("Cheek_Raise_R", "A22_Cheek_Squint_Right", "Cheek_Raise_R"),
            new FacialProfileMapping("Nose_Flank_Raise_L", "A23_Nose_Sneer_Left", "Nose_Sneer_L"),
            new FacialProfileMapping("Nose_Flank_Raise_R", "A24_Nose_Sneer_Right", "Nose_Sneer_R"),
            new FacialProfileMapping("", "A25_Jaw_Open", "Jaw_Open"),
            new FacialProfileMapping("", "A26_Jaw_Forward", "Jaw_Forward"),
            new FacialProfileMapping("", "A27_Jaw_Left", "Jaw_L"),
            new FacialProfileMapping("", "A28_Jaw_Right", "Jaw_R"),
            new FacialProfileMapping("Mouth_Pucker_Open", "A29_Mouth_Funnel", "Mouth_Funnel_Up/Down_L/R"), //Mouth_Funnel_Up/Down_L/R
            new FacialProfileMapping("Mouth_Pucker", "A30_Mouth_Pucker", "Mouth_Pucker_Up/Down_L/R"), //Mouth_Pucker_Up/Down_L/R
            new FacialProfileMapping("Mouth_L", "A31_Mouth_Left", "Mouth_L"),
            new FacialProfileMapping("Mouth_R", "A32_Mouth_Right", "Mouth_R"),
            new FacialProfileMapping("Mouth_Top_Lip_Under", "A33_Mouth_Roll_Upper", "Mouth_Roll_Out_Upper_L/R"), //Mouth_Roll_Out_Upper_L/R
            new FacialProfileMapping("Mouth_Bottom_Lip_Under", "A34_Mouth_Roll_Lower", "Mouth_Roll_Out_Lower_L/R"), //Mouth_Roll_Out_Lower_L/R
            new FacialProfileMapping("Mouth_Top_Lip_Up", "A35_Mouth_Shrug_Upper", "Mouth_Shrug_Upper"),
            new FacialProfileMapping("", "A36_Mouth_Shrug_Lower", "Mouth_Shrug_Lower"), // -Mouth_Bottom_Lip_Down
            new FacialProfileMapping("", "A37_Mouth_Close", "Mouth_Close"), //-Mouth_Open
            new FacialProfileMapping("Mouth_Smile_L", "A38_Mouth_Smile_Left", "Mouth_Smile_L"),
            new FacialProfileMapping("Mouth_Smile_R", "A39_Mouth_Smile_Right", "Mouth_Smile_R"),
            new FacialProfileMapping("Mouth_Frown_L", "A40_Mouth_Frown_Left", "Mouth_Frown_L"),
            new FacialProfileMapping("Mouth_Frown_R", "A41_Mouth_Frown_Right", "Mouth_Frown_R"),
            new FacialProfileMapping("Mouth_Dimple_L", "A42_Mouth_Dimple_Left", "Mouth_Dimple_L"),
            new FacialProfileMapping("Mouth_Dimple_R", "A43_Mouth_Dimple_Right", "Mouth_Dimple_R"),
            new FacialProfileMapping("", "A44_Mouth_Upper_Up_Left", "Mouth_Up_Upper_L"), //Mouth_Up
            new FacialProfileMapping("", "A45_Mouth_Upper_Up_Right", "Mouth_Up_Upper_R"), //Mouth_Up
            new FacialProfileMapping("", "A46_Mouth_Lower_Down_Left", "Mouth_Down_Lower_L"), //Mouth_Down
            new FacialProfileMapping("", "A47_Mouth_Lower_Down_Right", "Mouth_Down_Lower_R"), //Mouth_Down
            new FacialProfileMapping("", "A48_Mouth_Press_Left", "Mouth_Press_L"),
            new FacialProfileMapping("", "A49_Mouth_Press_Right", "Mouth_Press_R"),
            new FacialProfileMapping("", "A50_Mouth_Stretch_Left", "Mouth_Stretch_L"),
            new FacialProfileMapping("", "A51_Mouth_Stretch_Right", "Mouth_Stretch_R"),
            new FacialProfileMapping("", "T10_Tongue_Bulge_Left", "Tongue_Bulge_L"),
            new FacialProfileMapping("", "T11_Tongue_Bulge_Right", "Tongue_Bulge_R"),
            new FacialProfileMapping("Open", "Open", "V_Open"),
            new FacialProfileMapping("Explosive", "Explosive", "V_Explosive"),
            new FacialProfileMapping("Dental_Lip", "Dental_Lip", "V_Dental_Lip"),
            new FacialProfileMapping("Tight-O", "Tight", "V_Tight_O"),
            new FacialProfileMapping("Tight", "Tight", "V_Tight"),
            new FacialProfileMapping("Wide", "Wide", "V_Wide"),
            new FacialProfileMapping("Affricate", "Affricate", "V_Affricate"),
            new FacialProfileMapping("Lip_Open", "Lip_Open", "V_Lip_Open"),            
        };

        public static Dictionary<string, FacialProfileMapping> cacheCC3 = new Dictionary<string, FacialProfileMapping>();
        public static Dictionary<string, FacialProfileMapping> cacheCC3Ex = new Dictionary<string, FacialProfileMapping>();
        public static Dictionary<string, FacialProfileMapping> cacheCC4 = new Dictionary<string, FacialProfileMapping>();

        public static Dictionary<string, FacialProfileMapping> GetCache(FacialProfile profile)
        {
            switch(profile)
            {
                case FacialProfile.CC3Ex: return cacheCC3Ex;
                case FacialProfile.CC4: return cacheCC4;
                default: return cacheCC3;
            }
        }

        public static bool HasShape(this Mesh m, string s)
        {
            return (m.GetBlendShapeIndex(s) >= 0);
        }

        public static FacialProfile GetAnimationClipFacialProfile(AnimationClip clip)
        {
            const string blendShapePrefix = "blendShape.";
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            bool possibleCC3Profile = false;

            foreach (EditorCurveBinding binding in curveBindings)
            {
                if (binding.propertyName.StartsWith(blendShapePrefix))
                {
                    string blendShapeName = binding.propertyName.Substring(blendShapePrefix.Length);

                    switch (blendShapeName)
                    {
                        case "A01_Brow_Inner_Up":
                        case "A06_Eye_Look_Up_Left":
                        case "A15_Eye_Blink_Right":
                        case "A25_Jaw_Open":
                        case "A37_Mouth_Close":
                            return FacialProfile.CC3Ex;
                        case "V_Open":
                        case "V_Wide":
                        case "Eye_L_Look_L":
                        case "Eye_R_Look_R":
                            return FacialProfile.CC4;
                        case "Open":
                        case "Wide":
                        case "Mouth_Smile":
                        case "Eye_Blink":
                            possibleCC3Profile = true;
                            break;
                    }
                }
            }

            return possibleCC3Profile ? FacialProfile.CC3 : FacialProfile.None;
        }

        public static FacialProfile GetMeshFacialProfile(GameObject prefab)
        {
            bool possibleCC3Profile = false;

            SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (r.sharedMesh)
                {
                    Mesh mesh = r.sharedMesh;                    

                    if (mesh.blendShapeCount > 0)
                    {
                        if (mesh.HasShape("A01_Brow_Inner_Up") ||
                            mesh.HasShape("A06_Eye_Look_Up_Left") ||
                            mesh.HasShape("A15_Eye_Blink_Right") ||
                            mesh.HasShape("A25_Jaw_Open") ||
                            mesh.HasShape("A37_Mouth_Close")) return FacialProfile.CC3Ex;

                        if (mesh.HasShape("V_Open") ||
                            mesh.HasShape("V_Wide") ||
                            mesh.HasShape("Eye_L_Look_L") ||
                            mesh.HasShape("Eye_R_Look_R")) return FacialProfile.CC4;

                        if (mesh.HasShape("Open") ||
                            mesh.HasShape("Wide") ||
                            mesh.HasShape("Mouth_Smile") ||
                            mesh.HasShape("Eye_Blink")) possibleCC3Profile = true;
                    }
                }
            }

            return possibleCC3Profile ? FacialProfile.CC3 : FacialProfile.None;
        }

        public static bool MeshHasFacialBlendShapes(GameObject obj)
        {
            return GetMeshFacialProfile(obj) != FacialProfile.None;
        }

        public static string GetFacialProfileMapping(string blendShapeName, FacialProfile from, FacialProfile to)
        {
            Dictionary<string, FacialProfileMapping> cache = GetCache(from);

            if (cache.TryGetValue(blendShapeName, out FacialProfileMapping fpm))
                return fpm.GetMapping(to);

            foreach (FacialProfileMapping fpmSearch in facialProfileMaps)
            {
                if (fpmSearch.HasMapping(blendShapeName, from))
                {
                    cache.Add(blendShapeName, fpmSearch);
                    return fpmSearch.GetMapping(to);
                }
            }

            return blendShapeName;
        }

        private static List<string> multiShapeNames = new List<string>(4);
        private static List<string> tempNames = new List<string>(4);

        public static List<string> GetMultiShapeNames(string profileShapeName)
        {
            multiShapeNames.Clear();
            tempNames.Clear();
            if (profileShapeName.Contains("/"))
            {
                if (profileShapeName.Contains("_L/R"))
                {
                    multiShapeNames.Add(profileShapeName.Replace("_L/R", "_L"));
                    multiShapeNames.Add(profileShapeName.Replace("_L/R", "_R"));
                    
                    foreach (string LRShapeName in multiShapeNames)
                    {
                        if (LRShapeName.Contains("_Up/Down"))
                        {
                            tempNames.Add(LRShapeName.Replace("_Up/Down", "_Up"));
                            tempNames.Add(LRShapeName.Replace("_Up/Down", "_Down"));
                        }
                    }                    
                }
                else if (profileShapeName.Contains("_Up/Down"))
                {
                    multiShapeNames.Add(profileShapeName.Replace("_Up/Down", "_Up"));
                    multiShapeNames.Add(profileShapeName.Replace("_Up/Down", "_Down"));                    
                }
            }

            if (tempNames.Count > 0)
            {
                multiShapeNames.Clear();
                multiShapeNames.AddRange(tempNames);
                tempNames.Clear();
            } 

            if (multiShapeNames.Count == 0)
                multiShapeNames.Add(profileShapeName);

            return multiShapeNames;
        }

        public static bool SetCharacterBlendShape(GameObject root, string shapeName, FacialProfile fromProfile, FacialProfile toProfile, float weight)
        {
            bool res = false;

            if (root)
            {               
                string profileShapeName = GetFacialProfileMapping(shapeName, fromProfile, toProfile);
                GetMultiShapeNames(profileShapeName);

                for (int i = 0; i < root.transform.childCount; i++)
                {
                    GameObject child = root.transform.GetChild(i).gameObject;
                    SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (renderer)
                    {
                        Mesh mesh = renderer.sharedMesh;
                        if (mesh.blendShapeCount > 0)
                        {
                            foreach (string name in multiShapeNames)
                            {
                                int index = mesh.GetBlendShapeIndex(name);
                                if (index >= 0)
                                {
                                    renderer.SetBlendShapeWeight(index, weight);
                                    res = true;
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }

        public static bool GetCharacterBlendShapeWeight(GameObject root, string shapeName, FacialProfile fromProfile, FacialProfile toProfile, out float weight)
        {
            weight = 0f;
            int numWeights = 0;

            if (root)
            {
                string profileShapeName = GetFacialProfileMapping(shapeName, fromProfile, toProfile);

                for (int i = 0; i < root.transform.childCount; i++)
                {
                    GameObject child = root.transform.GetChild(i).gameObject;
                    SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (renderer)
                    {
                        Mesh mesh = renderer.sharedMesh;
                        if (mesh.blendShapeCount > 0)
                        {
                            int shapeIndexS = mesh.GetBlendShapeIndex(profileShapeName);

                            if (shapeIndexS > 0)
                            {
                                weight = renderer.GetBlendShapeWeight(shapeIndexS);
                                numWeights++;
                            }
                        }
                    }
                }
            }

            if (numWeights > 0) weight /= ((float)numWeights);

            return numWeights > 0;
        }
    }
}
