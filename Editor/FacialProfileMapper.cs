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

namespace Reallusion.Import
{
    public enum ExpressionProfile { None, Std, ExPlus, Ext }
    public enum VisemeProfile { None, PairsCC3, PairsCC4, Direct }

    public struct FacialProfile
    {
        public ExpressionProfile expressionProfile;
        public VisemeProfile visemeProfile;
        public bool corrections;
        
        public FacialProfile(ExpressionProfile exp, VisemeProfile vis)
        {
            expressionProfile = exp;
            visemeProfile = vis;
            corrections = false;
        }

        public override string ToString()
        {
            return "(" + expressionProfile + (corrections ? "*" : "") + "/" + visemeProfile + ")";
        }

        public bool IsSameProfileFrom(FacialProfile from)
        {
            return ((from.expressionProfile == expressionProfile || from.expressionProfile == ExpressionProfile.None) && 
                    (from.visemeProfile == visemeProfile || from.visemeProfile == VisemeProfile.None));
        }        

        public bool HasFacialShapes 
        { 
            get { return expressionProfile != ExpressionProfile.None || visemeProfile != VisemeProfile.None; } 
        }

        public string GetMappingFrom(string blendShapeName, FacialProfile from)
        {
            string mapping;

            // ExPlus4 blendshapes are incorrectly named blendshapes from the CC4 exported - CC3+ExPlus profile
            // which need to be corrected when mapping from
            if (from.corrections && FacialProfileMapper.GetCorrection(blendShapeName, out string correction))
            {
                blendShapeName = correction;
                if (FacialProfileMapper.GetFacialProfileMapping(blendShapeName, from, this, out mapping))
                {
                    return mapping;
                }
            }

            if (FacialProfileMapper.GetFacialProfileMapping(blendShapeName, from, this, out mapping))
            {
                // if we are mapping to a profile with EXPlus4 shapes we need to return the incorrect blendshape name
                if (corrections && FacialProfileMapper.GetIncorrection(mapping, out string incorrection))
                {
                    return incorrection;
                }
                return mapping;
            }
            
            return blendShapeName;
        }        
    }

    public struct ExpressionMapping
    {        
        public string Standard;
        public string ExPlus;
        public string Extended;        

        public ExpressionMapping(string std, string exPlus, string ext)
        {
            Standard = std;
            ExPlus = exPlus;
            Extended = ext;
        }

        public bool HasMapping(string blendShapeName)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                return false;

            if (Standard == blendShapeName || ExPlus == blendShapeName || Extended == blendShapeName)
                return true;

            return false;
        }

        public bool HasMapping(string blendShapeName, ExpressionProfile from)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                return false;

            if (from == ExpressionProfile.Std && Standard == blendShapeName) return true;
            if (from == ExpressionProfile.ExPlus && ExPlus == blendShapeName) return true;
            if (from == ExpressionProfile.Ext && Extended == blendShapeName) return true;

            return false;
        }

        public string GetMapping(ExpressionProfile to)
        {
            if (to == ExpressionProfile.Std) return Standard;
            if (to == ExpressionProfile.ExPlus) return ExPlus;
            if (to == ExpressionProfile.Ext) return Extended;
            return null;
        }
    }

    public struct VisemeMapping
    {
        public string CC3;
        public string CC4;
        public string Direct;

        public VisemeMapping(string cc3, string cc4, string direct)
        {
            CC3 = cc3;
            CC4 = cc4;
            Direct = direct;
        }
        
        public bool HasMapping(string blendShapeName, VisemeProfile from)
        {
            if (string.IsNullOrEmpty(blendShapeName))
                return false;

            if (from == VisemeProfile.PairsCC3 && CC3 == blendShapeName) return true;
            if (from == VisemeProfile.PairsCC4 && CC4 == blendShapeName) return true;
            if (from == VisemeProfile.Direct && Direct == blendShapeName) return true;

            return false;
        }

        public string GetMapping(VisemeProfile to)
        {
            if (to == VisemeProfile.PairsCC3) return CC3;
            if (to == VisemeProfile.PairsCC4) return CC4;
            if (to == VisemeProfile.Direct) return Direct;
            return null;
        }
    }

    public struct MappingCorrection
    {
        public string correct;
        public string incorrect;

        public MappingCorrection(string c, string i)
        {
            correct = c;
            incorrect = i;
        }        
    }

    public static class FacialProfileMapper
    {
        public static List<ExpressionMapping> facialProfileMaps = new List<ExpressionMapping>() {
            //new FacialProfileMapping("", "", ""),            
            new ExpressionMapping("Brow_Raise_Inner_L|Brow_Raise_Inner_R", "A01_Brow_Inner_Up", "Brow_Raise_Inner_L|Brow_Raise_Inner_R"), // Brow_Raise_Inner_L/R
            new ExpressionMapping("Brow_Drop_L", "A02_Brow_Down_Left", "Brow_Drop_L"),
            new ExpressionMapping("Brow_Drop_R", "A03_Brow_Down_Right", "Brow_Drop_R"),
            new ExpressionMapping("Brow_Raise_Outer_L", "A04_Brow_Outer_Up_Left", "Brow_Raise_Outer_L"),
            new ExpressionMapping("Brow_Raise_Outer_R", "A05_Brow_Outer_Up_Right", "Brow_Raise_Outer_R"),
            new ExpressionMapping("Brow_Raise_L", "A04_Brow_Outer_Up_Left|A01_Brow_Inner_Up", "Brow_Raise_Inner_L|Brow_Raise_Inner_L"),
            new ExpressionMapping("Brow_Raise_R", "A05_Brow_Outer_Up_Right|A01_Brow_Inner_Up", "Brow_Raise_Inner_R|Brow_Raise_outer_R"),
            new ExpressionMapping("", "A06_Eye_Look_Up_Left", "Eye_L_Look_Up"),
            new ExpressionMapping("", "A07_Eye_Look_Up_Right", "Eye_R_Look_Up"),
            new ExpressionMapping("", "A08_Eye_Look_Down_Left", "Eye_L_Look_Down"),
            new ExpressionMapping("", "A09_Eye_Look_Down_Right", "Eye_R_Look_Down"),
            new ExpressionMapping("", "A10_Eye_Look_Out_Left", "Eye_L_Look_L"),
            new ExpressionMapping("", "A11_Eye_Look_In_Left", "Eye_L_Look_R"),
            new ExpressionMapping("", "A12_Eye_Look_In_Right", "Eye_R_Look_L"),
            new ExpressionMapping("", "A13_Eye_Look_Out_Right", "Eye_R_Look_R"),
            new ExpressionMapping("Eye_Blink", "Eye_Blink", "Eye_Blink_L|Eye_Blink_R"),
            new ExpressionMapping("Eye_Blink_L", "A14_Eye_Blink_Left", "Eye_Blink_L"),
            new ExpressionMapping("Eye_Blink_R", "A15_Eye_Blink_Right", "Eye_Blink_R"),
            new ExpressionMapping("Eye_Squint_L", "A16_Eye_Squint_Left", "Eye_Squint_L"),
            new ExpressionMapping("Eye_Squint_R", "A17_Eye_Squint_Right", "Eye_Squint_R"),
            new ExpressionMapping("Eye_Wide_L", "A18_Eye_Wide_Left", "Eye_Wide_L"),
            new ExpressionMapping("Eye_Wide_R", "A19_Eye_Wide_Right", "Eye_Wide_R"),
            new ExpressionMapping("Cheek_Puff_L|Cheek_Puff_R", "A20_Cheek_Puff", "Cheek_Puff_L|Cheek_Puff_R"), //Cheek_Puff_L/R
            new ExpressionMapping("Cheek_Raise_L", "A21_Cheek_Squint_Left", "Cheek_Raise_L"),
            new ExpressionMapping("Cheek_Raise_R", "A22_Cheek_Squint_Right", "Cheek_Raise_R"),
            new ExpressionMapping("Nose_Flank_Raise_L", "A23_Nose_Sneer_Left", "Nose_Sneer_L"),
            new ExpressionMapping("Nose_Flank_Raise_R", "A24_Nose_Sneer_Right", "Nose_Sneer_R"),
            new ExpressionMapping("", "A25_Jaw_Open", "Jaw_Open"),
            new ExpressionMapping("", "A26_Jaw_Forward", "Jaw_Forward"),
            new ExpressionMapping("", "A27_Jaw_Left", "Jaw_L"),
            new ExpressionMapping("", "A28_Jaw_Right", "Jaw_R"),
            new ExpressionMapping("Mouth_Pucker_Open", "A29_Mouth_Funnel", "Mouth_Funnel_Up_L|Mouth_Funnel_Up_R|Mouth_Funnel_Down_L|Mouth_Funnel_Down_R"), //Mouth_Funnel_Up/Down_L/R
            new ExpressionMapping("Mouth_Pucker", "A30_Mouth_Pucker", "Mouth_Pucker_Up_L|Mouth_Pucker_Up_R|Mouth_Pucker_Down_L|Mouth_Pucker_Down_R"), //Mouth_Pucker_Up/Down_L/R
            new ExpressionMapping("Mouth_L", "A31_Mouth_Left", "Mouth_L"),
            new ExpressionMapping("Mouth_R", "A32_Mouth_Right", "Mouth_R"),
            new ExpressionMapping("Mouth_Top_Lip_Under", "A33_Mouth_Roll_Upper", "Mouth_Roll_Out_Upper_L|Mouth_Roll_Out_Upper_R"), //Mouth_Roll_Out_Upper_L/R
            new ExpressionMapping("Mouth_Bottom_Lip_Under", "A34_Mouth_Roll_Lower", "Mouth_Roll_Out_Lower_L|Mouth_Roll_Out_Lower_R"), //Mouth_Roll_Out_Lower_L/R
            new ExpressionMapping("Mouth_Top_Lip_Up", "A35_Mouth_Shrug_Upper", "Mouth_Shrug_Upper"),
            new ExpressionMapping("", "A36_Mouth_Shrug_Lower", "Mouth_Shrug_Lower"), // -Mouth_Bottom_Lip_Down
            new ExpressionMapping("", "A37_Mouth_Close", "Mouth_Close"), //-Mouth_Open
            new ExpressionMapping("Mouth_Smile_L", "A38_Mouth_Smile_Left", "Mouth_Smile_L"),
            new ExpressionMapping("Mouth_Smile_R", "A39_Mouth_Smile_Right", "Mouth_Smile_R"),
            new ExpressionMapping("Mouth_Frown_L", "A40_Mouth_Frown_Left", "Mouth_Frown_L"),
            new ExpressionMapping("Mouth_Frown_R", "A41_Mouth_Frown_Right", "Mouth_Frown_R"),
            new ExpressionMapping("Mouth_Dimple_L", "A42_Mouth_Dimple_Left", "Mouth_Dimple_L"),
            new ExpressionMapping("Mouth_Dimple_R", "A43_Mouth_Dimple_Right", "Mouth_Dimple_R"),
            new ExpressionMapping("", "A44_Mouth_Upper_Up_Left", "Mouth_Up_Upper_L"), //Mouth_Up
            new ExpressionMapping("", "A45_Mouth_Upper_Up_Right", "Mouth_Up_Upper_R"), //Mouth_Up
            new ExpressionMapping("", "A46_Mouth_Lower_Down_Left", "Mouth_Down_Lower_L"), //Mouth_Down
            new ExpressionMapping("", "A47_Mouth_Lower_Down_Right", "Mouth_Down_Lower_R"), //Mouth_Down
            new ExpressionMapping("", "A48_Mouth_Press_Left", "Mouth_Press_L"),
            new ExpressionMapping("", "A49_Mouth_Press_Right", "Mouth_Press_R"),
            new ExpressionMapping("", "A50_Mouth_Stretch_Left", "Mouth_Stretch_L"),
            new ExpressionMapping("", "A51_Mouth_Stretch_Right", "Mouth_Stretch_R"),
            new ExpressionMapping("", "A52_Tongue_Out", "Tongue_Out"),
            new ExpressionMapping("", "T01_Tongue_Up", "Tongue_Up"),
            new ExpressionMapping("", "T02_Tongue_Down", "Tongue_Down"),
            new ExpressionMapping("", "T03_Tongue_Left", "Tongue_L"),
            new ExpressionMapping("", "T04_Tongue_Right", "Tongue_R"),
            new ExpressionMapping("", "T05_Tongue_Roll", "Tongue_Roll"),
            new ExpressionMapping("", "T06_Tongue_Tip_Up", "Tongue_Tip_Up"),
            new ExpressionMapping("", "T07_Tongue_Tip_Down", "Tongue_Tip_Down"),
            new ExpressionMapping("", "T08_Tongue_Width", "Tongue_Wide"),
            new ExpressionMapping("", "T09_Tongue_Thickness", "Tongue_Narrow"),
            new ExpressionMapping("", "T10_Tongue_Bulge_Left", "Tongue_Bulge_L"),
            new ExpressionMapping("", "T11_Tongue_Bulge_Right", "Tongue_Bulge_R"),
        };

        public static List<VisemeMapping> visemeProfileMaps = new List<VisemeMapping>() {
            //new VisemeProfileMapping("", "", ""),                        
            new VisemeMapping("Open", "V_Open", ""),
            new VisemeMapping("Explosive", "V_Explosive", ""),
            new VisemeMapping("Dental_Lip", "V_Dental_Lip", ""),
            new VisemeMapping("Tight-O", "V_Tight_O", ""),
            new VisemeMapping("Tight", "V_Tight", ""),
            new VisemeMapping("Wide", "V_Wide", ""),
            new VisemeMapping("Affricate", "V_Affricate", ""),
            new VisemeMapping("Lip_Open", "V_Lip_Open", ""),
            new VisemeMapping("Tongue_up", "V_Tongue_up", ""),
            new VisemeMapping("Tongue_Raise", "V_Tongue_Raise", ""),
            new VisemeMapping("Tongue_Out", "V_Tongue_Out", ""),
            new VisemeMapping("Tongue_Narrow", "V_Tongue_Narrow", ""),
            new VisemeMapping("Tongue_Lower", "V_Tongue_Lower", ""),
            new VisemeMapping("Tongue_Curl-U", "V_Tongue_Curl_U", ""),
            new VisemeMapping("Tongue_Curl-D", "V_Tongue_Curl_D", ""),
        };

        public static List<MappingCorrection> corrections = new List<MappingCorrection>()
        {
            new MappingCorrection("Brow_Raise_Inner_L", "Brow_Raise_Inner_Left"),
            new MappingCorrection("Brow_Raise_Inner_R", "Brow_Raise_Inner_Right"),
            new MappingCorrection("Brow_Raise_Outer_L", "Brow_Raise_Outer_Left"),
            new MappingCorrection("Brow_Raise_Outer_R", "Brow_Raise_Outer_Right"),
            new MappingCorrection("Brow_Drop_L", "Brow_Drop_Left"),
            new MappingCorrection("Brow_Drop_R", "Brow_Drop_Right"),
            new MappingCorrection("Brow_Raise_L", "Brow_Raise_Left"),
            new MappingCorrection("Brow_Raise_R", "Brow_Raise_Right"),
        };

        public static Dictionary<string, ExpressionMapping> cacheStd = new Dictionary<string, ExpressionMapping>();
        public static Dictionary<string, ExpressionMapping> cacheExPlus = new Dictionary<string, ExpressionMapping>();
        public static Dictionary<string, ExpressionMapping> cacheExt = new Dictionary<string, ExpressionMapping>();

        public static Dictionary<string, ExpressionMapping> GetExpressionCache(ExpressionProfile profile)
        {
            switch (profile)
            {
                case ExpressionProfile.ExPlus: return cacheExPlus;
                case ExpressionProfile.Ext: return cacheExt;
                default: return cacheStd;
            }
        }

        public static Dictionary<string, VisemeMapping> cacheCC3Pair = new Dictionary<string, VisemeMapping>();
        public static Dictionary<string, VisemeMapping> cacheCC4Pair = new Dictionary<string, VisemeMapping>();
        public static Dictionary<string, VisemeMapping> cacheDirect = new Dictionary<string, VisemeMapping>();

        public static Dictionary<string, VisemeMapping> GetVisemeCache(VisemeProfile profile)
        {
            switch (profile)
            {
                case VisemeProfile.Direct: return cacheDirect;
                case VisemeProfile.PairsCC4: return cacheCC4Pair;
                default: return cacheCC3Pair;
            }
        }

        public static bool HasShape(this Mesh m, string s)
        {
            return (m.GetBlendShapeIndex(s) >= 0);
        }

        public static FacialProfile GetAnimationClipFacialProfile(AnimationClip clip)
        {
            ExpressionProfile expressionProfile = ExpressionProfile.None;
            VisemeProfile visemeProfile = VisemeProfile.None;

            if (!clip) return default;

            const string blendShapePrefix = "blendShape.";
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
            bool corrections = false;

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
                            expressionProfile = ExpressionProfile.ExPlus;
                            break;
                        case "Ear_Up_L":
                        case "Ear_Up_R":
                        case "Eyelash_Upper_Up_L":
                        case "Eyelash_Upper_Up_R":
                        case "Eye_L_Look_L":
                        case "Eye_R_Look_R":
                            if (expressionProfile == ExpressionProfile.None ||
                                expressionProfile == ExpressionProfile.Std)
                                expressionProfile = ExpressionProfile.Ext;
                            break;
                        case "Mouth_L":
                        case "Mouth_R":
                        case "Eye_Wide_L":
                        case "Eye_Wide_R":
                        case "Mouth_Smile":
                        case "Eye_Blink":
                            if (expressionProfile == ExpressionProfile.None)
                                expressionProfile = ExpressionProfile.Std;
                            break;

                        case "V_Open":
                        case "V_Tight":
                        case "V_Tongue_up":
                        case "V_Tongue_Raise":
                            visemeProfile = VisemeProfile.PairsCC4;
                            break;
                        case "Open":
                        case "Tight":
                        case "Tongue_up":
                        case "Tongue_Raise":
                            if (visemeProfile == VisemeProfile.None ||
                                visemeProfile == VisemeProfile.Direct)
                                visemeProfile = VisemeProfile.PairsCC3;
                            break;
                        case "AE":
                        case "EE":
                        case "Er":
                        case "Oh":
                            if (visemeProfile == VisemeProfile.None)
                                visemeProfile = VisemeProfile.Direct;
                            break;
                        case "Brow_Raise_Inner_Left":                        
                        case "Brow_Raise_Outer_Left":
                        case "Brow_Drop_Left":
                        case "Brow_Raise_Right":
                            corrections = true;
                            break;


                    }
                }
            }

            return new FacialProfile(expressionProfile, visemeProfile)
            {
                corrections = corrections
            };            
        }

        public static FacialProfile GetMeshFacialProfile(GameObject prefab)
        {
            ExpressionProfile expressionProfile = ExpressionProfile.None;
            VisemeProfile visemeProfile = VisemeProfile.None;
            if (!prefab) return default;
            bool corrections = false;

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
                            mesh.HasShape("A37_Mouth_Close"))
                            expressionProfile = ExpressionProfile.ExPlus;

                        if (mesh.HasShape("Ear_Up_L") ||
                            mesh.HasShape("Ear_Up_R") ||
                            mesh.HasShape("Eyelash_Upper_Up_L") ||
                            mesh.HasShape("Eyelash_Upper_Up_R") ||
                            mesh.HasShape("Eye_L_Look_L") ||
                            mesh.HasShape("Eye_R_Look_R"))
                            if (expressionProfile == ExpressionProfile.None ||
                                expressionProfile == ExpressionProfile.Std)
                                expressionProfile = ExpressionProfile.Ext;

                        if (mesh.HasShape("Mouth_L") ||
                            mesh.HasShape("Mouth_R") ||
                            mesh.HasShape("Eye_Wide_L") ||
                            mesh.HasShape("Eye_Wide_R") ||
                            mesh.HasShape("Mouth_Smile") ||
                            mesh.HasShape("Eye_Blink"))
                            if (expressionProfile == ExpressionProfile.None)
                                expressionProfile = ExpressionProfile.Std;


                        if (mesh.HasShape("V_Open") ||
                            mesh.HasShape("V_Tight") ||
                            mesh.HasShape("V_Tongue_up") ||
                            mesh.HasShape("V_Tongue_Raise"))
                            visemeProfile = VisemeProfile.PairsCC4;

                        if (mesh.HasShape("Open") ||
                            mesh.HasShape("Tight") ||
                            mesh.HasShape("Tongue_up") ||
                            mesh.HasShape("Tongue_Raise"))
                            if (visemeProfile == VisemeProfile.None ||
                                visemeProfile == VisemeProfile.Direct)
                                visemeProfile = VisemeProfile.PairsCC3;

                        if (mesh.HasShape("AE") ||
                            mesh.HasShape("EE") ||
                            mesh.HasShape("Er") ||
                            mesh.HasShape("Oh"))
                            if (visemeProfile == VisemeProfile.None)
                                visemeProfile = VisemeProfile.Direct;

                        if (mesh.HasShape("Brow_Raise_Inner_Left") ||
                            mesh.HasShape("Brow_Raise_Outer_Left") ||
                            mesh.HasShape("Brow_Drop_Left") ||
                            mesh.HasShape("Brow_Raise_Right"))
                            corrections = true;                        
                    }
                }
            }

            return new FacialProfile(expressionProfile, visemeProfile)
            {
                corrections = corrections
            };
        }

        public static bool MeshHasFacialBlendShapes(GameObject obj)
        {
            FacialProfile profile = GetMeshFacialProfile(obj);
            return profile.HasFacialShapes;
        }

        public static bool GetFacialProfileMapping(string blendShapeName, FacialProfile from, FacialProfile to, out string mapping)
        {
            Dictionary<string, ExpressionMapping> cacheFacial = GetExpressionCache(from.expressionProfile);
            Dictionary<string, VisemeMapping> cacheViseme = GetVisemeCache(from.visemeProfile);

            if (cacheFacial.TryGetValue(blendShapeName, out ExpressionMapping fpm))
            {
                mapping = fpm.GetMapping(to.expressionProfile);
                if (string.IsNullOrEmpty(mapping)) mapping = blendShapeName;
                return true;
            }

            if (cacheViseme.TryGetValue(blendShapeName, out VisemeMapping vpm))
            {
                mapping = vpm.GetMapping(to.visemeProfile);
                if (string.IsNullOrEmpty(mapping)) mapping = blendShapeName;
                return true;
            }

            foreach (ExpressionMapping fpmSearch in facialProfileMaps)
            {
                if (fpmSearch.HasMapping(blendShapeName, from.expressionProfile))
                {
                    cacheFacial.Add(blendShapeName, fpmSearch);
                    mapping = fpmSearch.GetMapping(to.expressionProfile);
                    if (string.IsNullOrEmpty(mapping)) mapping = blendShapeName;
                    return true;
                }
            }

            foreach (VisemeMapping vpmSearch in visemeProfileMaps)
            {
                if (vpmSearch.HasMapping(blendShapeName, from.visemeProfile))
                {
                    cacheViseme.Add(blendShapeName, vpmSearch);
                    mapping = vpmSearch.GetMapping(to.visemeProfile);
                    if (string.IsNullOrEmpty(mapping)) mapping = blendShapeName;
                    return true;
                }
            }

            mapping = blendShapeName;
            return false;
        }

        private static List<string> multiShapeNames = new List<string>(4);        

        public static List<string> GetMultiShapeNames(string profileShapeName)
        {
            multiShapeNames.Clear();            
            if (profileShapeName.Contains("|"))
            {
                string[] splitNames = profileShapeName.Split('|');
                multiShapeNames.AddRange(splitNames);                
            }            

            if (multiShapeNames.Count == 0)
                multiShapeNames.Add(profileShapeName);

            return multiShapeNames;
        }

        public static bool SetCharacterBlendShape(GameObject root, string shapeName,
            FacialProfile from, FacialProfile to, float weight)
        {
            bool res = false;

            if (root)
            {
                string profileShapeName = to.GetMappingFrom(shapeName, from);

                if (!string.IsNullOrEmpty(profileShapeName))
                {
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
            }

            return res;
        }

        public static bool GetCharacterBlendShapeWeight(GameObject root, string shapeName,
            FacialProfile from, FacialProfile to, out float weight)
        {
            weight = 0f;
            int numWeights = 0;

            if (root)
            {
                string profileShapeName = to.GetMappingFrom(shapeName, from);
                if (!string.IsNullOrEmpty(profileShapeName))
                {
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
            }

            if (numWeights > 0) weight /= ((float)numWeights);

            return numWeights > 0;
        }

        public static bool GetCorrection(string blendShapeName, out string correction)
        {            
            foreach (MappingCorrection mc in corrections)
            {
                if (mc.incorrect == blendShapeName)
                {
                    correction = mc.correct;
                    return true;
                }
            }

            correction = blendShapeName;
            return false;
        }

        public static bool GetIncorrection(string blendShapeName, out string incorrection)
        {
            foreach (MappingCorrection mc in corrections)
            {
                if (mc.correct == blendShapeName)
                {
                    incorrection = mc.incorrect;
                    return true;
                }
            }

            incorrection = blendShapeName;
            return false;
        }
    }
}
