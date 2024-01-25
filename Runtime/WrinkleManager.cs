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

namespace Reallusion.Import
{
    public enum MaskSet { none = 0, set1A = 1, set1B = 2, set2 = 3, set3 = 4, set12C = 5, set3D = 6, setBCC = 7}
    public enum MaskSide { none, left, right, center }

    [System.Serializable]
    public struct WrinkleRule
    {
        public MaskSet mask;
        public MaskSide side;
        public int index;

        public WrinkleRule(MaskSet ms, MaskSide s, int i)
        {
            mask = ms;
            side = s;
            index = i;
        }
    }

    public struct WrinkleProp
    {
        public float weight;
        public float ease;
    }

    [System.Serializable]
    public class WrinkleRuleSet
    {        
        public string ruleName;
        public float weight;        
        public bool enabled = false;
        public float min = 0f, max = 1f;

        public WrinkleRuleSet(Dictionary<string, WrinkleProp> props,
                               string rn, float min = 0f, float max = 1f)
        {
            ruleName = rn;
            this.min = min;
            this.max = max;            
            weight = 1.0f;
            if (props != null)
            {
                if (props.TryGetValue(rn, out WrinkleProp p))
                {
                    weight = p.weight;
                }
            }
        }
    }

    [System.Serializable]
    public class WrinkleConfig
    {
        public string blendShapeName;
        public bool enabled;
        public int blendShapeIndex;
        public List<WrinkleRuleSet> ruleSets;

        public WrinkleConfig(string bsn, List<WrinkleRuleSet> mappings)
        {
            blendShapeName = bsn;
            enabled = false;
            blendShapeIndex = -1;
            this.ruleSets = mappings;
        }
    }

    [System.Serializable]
    [ExecuteInEditMode]
    public class WrinkleManager : MonoBehaviour
    {        
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Material headMaterial;        
        [Range(0f, 2f)]
        public float blendScale = 1f;
        [Range(0.5f, 2f)]
        public float blendCurve = 1.5f;
        [Range(0.1f, 20f)]
        public float blendFalloff = 4f;
        //[Range(0f, 5f)]
        //public float blendFadeDuration = 2f;
        [Range(1f, 120f)]  
        public float updateFrequency = 30f;        

        [SerializeField]
        private Dictionary<string, WrinkleRule> wrinkleRules;
        [SerializeField]
        public List<WrinkleConfig> config;

        public Vector4[] valueSets = new Vector4[12];
        private Vector4[] valueStore = new Vector4[12];
        private Vector4[] valueTemp = new Vector4[12];

        private float updateTimer = 0f;
        private float lastTime = 0f;        

        public void BuildRules()
        {
            wrinkleRules = new Dictionary<string, WrinkleRule>()
            {
                { "head_wm1_normal_head_wm1_blink_L", new WrinkleRule(MaskSet.set1A, MaskSide.left, 0) },
                { "head_wm1_normal_head_wm1_blink_R", new WrinkleRule(MaskSet.set1A, MaskSide.right, 0) },

                { "head_wm1_normal_head_wm1_browRaiseInner_L", new WrinkleRule(MaskSet.set1A, MaskSide.left, 1) },
                { "head_wm1_normal_head_wm1_browRaiseInner_R", new WrinkleRule(MaskSet.set1A, MaskSide.right, 1) },

                { "head_wm1_normal_head_wm1_purse_DL", new WrinkleRule(MaskSet.set1A, MaskSide.left, 2) },
                { "head_wm1_normal_head_wm1_purse_DR", new WrinkleRule(MaskSet.set1A, MaskSide.right, 2) },

                { "head_wm1_normal_head_wm1_purse_UL", new WrinkleRule(MaskSet.set1A, MaskSide.left, 3) },
                { "head_wm1_normal_head_wm1_purse_UR", new WrinkleRule(MaskSet.set1A, MaskSide.right, 3) },


                { "head_wm1_normal_head_wm1_browRaiseOuter_L", new WrinkleRule(MaskSet.set1B, MaskSide.left, 0) },
                { "head_wm1_normal_head_wm1_browRaiseOuter_R", new WrinkleRule(MaskSet.set1B, MaskSide.right, 0) },

                { "head_wm1_normal_head_wm1_chinRaise_L", new WrinkleRule(MaskSet.set1B, MaskSide.left, 1) },
                { "head_wm1_normal_head_wm1_chinRaise_R", new WrinkleRule(MaskSet.set1B, MaskSide.right, 1) },

                { "head_wm1_normal_head_wm1_jawOpen", new WrinkleRule(MaskSet.set1B, MaskSide.center, 2) },

                { "head_wm1_normal_head_wm1_squintInner_L", new WrinkleRule(MaskSet.set1B, MaskSide.left, 3) },
                { "head_wm1_normal_head_wm1_squintInner_R", new WrinkleRule(MaskSet.set1B, MaskSide.right, 3) },


                { "head_wm2_normal_head_wm2_browsDown_L", new WrinkleRule(MaskSet.set2, MaskSide.left, 0) },
                { "head_wm2_normal_head_wm2_browsDown_R", new WrinkleRule(MaskSet.set2, MaskSide.right, 0) },

                { "head_wm2_normal_head_wm2_browsLateral_L", new WrinkleRule(MaskSet.set2, MaskSide.left, 1) },
                { "head_wm2_normal_head_wm2_browsLateral_R", new WrinkleRule(MaskSet.set2, MaskSide.right, 1) },

                { "head_wm2_normal_head_wm2_mouthStretch_L", new WrinkleRule(MaskSet.set2, MaskSide.left, 2) },
                { "head_wm2_normal_head_wm2_mouthStretch_R", new WrinkleRule(MaskSet.set2, MaskSide.right, 2) },

                { "head_wm2_normal_head_wm2_neckStretch_L", new WrinkleRule(MaskSet.set2, MaskSide.left, 3) },
                { "head_wm2_normal_head_wm2_neckStretch_R", new WrinkleRule(MaskSet.set2, MaskSide.right, 3) },


                { "head_wm3_normal_head_wm3_cheekRaiseInner_L", new WrinkleRule(MaskSet.set3, MaskSide.left, 0) },
                { "head_wm3_normal_head_wm3_cheekRaiseInner_R", new WrinkleRule(MaskSet.set3, MaskSide.right, 0) },

                { "head_wm3_normal_head_wm3_cheekRaiseOuter_L", new WrinkleRule(MaskSet.set3, MaskSide.left, 1) },
                { "head_wm3_normal_head_wm3_cheekRaiseOuter_R", new WrinkleRule(MaskSet.set3, MaskSide.right, 1) },

                { "head_wm3_normal_head_wm3_cheekRaiseUpper_L", new WrinkleRule(MaskSet.set3, MaskSide.left, 2) },
                { "head_wm3_normal_head_wm3_cheekRaiseUpper_R", new WrinkleRule(MaskSet.set3, MaskSide.right, 2) },

                { "head_wm3_normal_head_wm3_smile_L", new WrinkleRule(MaskSet.set3, MaskSide.left, 3) },
                { "head_wm3_normal_head_wm3_smile_R", new WrinkleRule(MaskSet.set3, MaskSide.right, 3) },


                { "head_wm1_normal_head_wm13_lips_DL", new WrinkleRule(MaskSet.set12C, MaskSide.left, 0) },
                { "head_wm1_normal_head_wm13_lips_DR", new WrinkleRule(MaskSet.set12C, MaskSide.right, 0) },

                { "head_wm1_normal_head_wm13_lips_UL", new WrinkleRule(MaskSet.set12C, MaskSide.left, 1) },
                { "head_wm1_normal_head_wm13_lips_UR", new WrinkleRule(MaskSet.set12C, MaskSide.right, 1) },

                { "head_wm2_normal_head_wm2_noseWrinkler_L", new WrinkleRule(MaskSet.set12C, MaskSide.left, 2) },
                { "head_wm2_normal_head_wm2_noseWrinkler_R", new WrinkleRule(MaskSet.set12C, MaskSide.right, 2) },

                { "head_wm2_normal_head_wm2_noseCrease_L", new WrinkleRule(MaskSet.set12C, MaskSide.left, 3) },
                { "head_wm2_normal_head_wm2_noseCrease_R", new WrinkleRule(MaskSet.set12C, MaskSide.right, 3) },


                { "head_wm3_normal_head_wm13_lips_DL", new WrinkleRule(MaskSet.set3D, MaskSide.none, 0) },
                { "head_wm3_normal_head_wm13_lips_DR", new WrinkleRule(MaskSet.set3D, MaskSide.none, 1) },

                { "head_wm3_normal_head_wm13_lips_UL", new WrinkleRule(MaskSet.set3D, MaskSide.none, 2) },
                { "head_wm3_normal_head_wm13_lips_UR", new WrinkleRule(MaskSet.set3D, MaskSide.none, 3) },


                { "head_wm3_normal_head_wm3_browRaiseCorrection_L", new WrinkleRule(MaskSet.setBCC, MaskSide.none, 0) },
                { "head_wm3_normal_head_wm3_browRaiseCorrection_R", new WrinkleRule(MaskSet.setBCC, MaskSide.none, 1) },

            };
        }

        public void BuildConfig(Dictionary<string, WrinkleProp> props = null, float overallWeight = 1.0f)
        {
            BuildRules();

            config = new List<WrinkleConfig>()
            {
                new WrinkleConfig("Brow_Raise_Inner_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_browRaiseInner_L"),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_L", 0f, 0.03f),
                }),

                new WrinkleConfig("Brow_Raise_Inner_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_browRaiseInner_R"),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_R", 0f, 0.03f),
                }),

                new WrinkleConfig("Brow_Raise_Outer_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_browRaiseOuter_L"),
                }),

                new WrinkleConfig("Brow_Raise_Outer_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_browRaiseOuter_R"),
                }),

                new WrinkleConfig("Brow_Drop_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsDown_L", 0f, 0.1f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_L"),
                }),

                new WrinkleConfig("Brow_Drop_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsDown_R", 0f, 0.1f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_R"),
                }),

                new WrinkleConfig("Brow_Compress_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_L"),
                }),

                new WrinkleConfig("Brow_Compress_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_R"),
                }),

                new WrinkleConfig("Eye_Blink_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_blink_L"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_squintInner_L", 0f, 0.3f),
                }),

                new WrinkleConfig("Eye_Blink_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_blink_R"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_squintInner_R", 0f, 0.3f),
                }),

                new WrinkleConfig("Eye_Squint_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_squintInner_L"),
                }),

                new WrinkleConfig("Eye_Squint_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_squintInner_R"),
                }),

                new WrinkleConfig("Eye_L_Look_Down", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_blink_L"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_blink_R"),
                }),

                new WrinkleConfig("Nose_Sneer_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsDown_L", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_L", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseWrinkler_L"),
                }),

                new WrinkleConfig("Nose_Sneer_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsDown_R", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_browsLateral_R", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseWrinkler_R"),
                }),

                new WrinkleConfig("Nose_Nostril_Raise_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseWrinkler_L", 0f, 0.6f),
                }),

                new WrinkleConfig("Nose_Nostril_Raise_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseWrinkler_R", 0f, 0.6f),
                }),

                new WrinkleConfig("Nose_Crease_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseCrease_L", 0f, 0.7f),
                }),

                new WrinkleConfig("Nose_Crease_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseCrease_R", 0f, 0.7f),
                }),

                new WrinkleConfig("Cheek_Raise_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_L"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_L"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseUpper_L"),
                }),

                new WrinkleConfig("Cheek_Raise_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_R"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_R"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseUpper_R"),
                }),

                new WrinkleConfig("Jaw_Open", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen"),
                }),

                new WrinkleConfig("Jaw_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_L"),
                }),

                new WrinkleConfig("Jaw_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_R"),
                }),

                new WrinkleConfig("Mouth_Up", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_L"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_R"),
                }),

                new WrinkleConfig("Mouth_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_L", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                }),

                new WrinkleConfig("Mouth_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_R", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                }),

                new WrinkleConfig("Mouth_Smile_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_L"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL"),
                }),

                new WrinkleConfig("Mouth_Smile_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.6f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_R"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR"),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR"),
                }),

                new WrinkleConfig("Mouth_Smile_Sharp_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.4f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.4f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_L", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL", 0f, 0.8f),
                }),

                new WrinkleConfig("Mouth_Smile_Sharp_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.4f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.4f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_R", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR", 0f, 0.8f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR", 0f, 0.8f),
                }),

                new WrinkleConfig("Mouth_Dimple_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.15f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.15f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_L", 0f, 0.3f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL", 0f, 0.3f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL", 0f, 0.3f),
                }),

                new WrinkleConfig("Mouth_Dimple_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.15f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.15f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm3_smile_R", 0f, 0.3f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR", 0f, 0.3f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR", 0f, 0.3f),
                }),

                new WrinkleConfig("Mouth_Stretch_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_mouthStretch_L"),
                }),

                new WrinkleConfig("Mouth_Stretch_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_mouthStretch_R"),
                }),

                new WrinkleConfig("Mouth_Pucker_Up_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL"),
                }),

                new WrinkleConfig("Mouth_Pucker_Up_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR"),
                }),

                new WrinkleConfig("Mouth_Pucker_Down_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_L", 0f, 0.5f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL"),
                }),

                new WrinkleConfig("Mouth_Pucker_Down_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_R", 0f, 0.5f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR"),
                }),

                new WrinkleConfig("Mouth_Pucker", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_L", 0f, 0.5f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_R", 0f, 0.5f),
                }),

                new WrinkleConfig("Mouth_Chin_Up", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_L"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_R"),
                }),

                new WrinkleConfig("Mouth_Up_Upper_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseCrease_L"),
                }),

                new WrinkleConfig("Mouth_Up_Upper_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_noseCrease_R"),
                }),

                new WrinkleConfig("Neck_Tighten_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_L"),
                }),

                new WrinkleConfig("Neck_Tighten_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_R"),
                }),

                new WrinkleConfig("Head_Turn_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_R", 0f, 0.6f),
                }),

                new WrinkleConfig("Head_Turn_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_L", 0f, 0.6f),
                }),

                new WrinkleConfig("Head_Tilt_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_R", 0f, 0.75f),
                }),

                new WrinkleConfig("Head_Tilt_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_neckStretch_L", 0f, 0.75f),
                }),

                new WrinkleConfig("Head_Backward", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.5f),
                }),

                new WrinkleConfig("Mouth_Frown_L", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_mouthStretch_L"),
                }),

                new WrinkleConfig("Mouth_Frown_R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm2_normal_head_wm2_mouthStretch_R"),
                }),

                new WrinkleConfig("Mouth_Shrug_Lower", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_L"),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_chinRaise_R"),
                }),

                new WrinkleConfig("V_Open", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6f),
                }),

                new WrinkleConfig("V_Tight_O", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                }),

                new WrinkleConfig("V_Tight", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                }),

                new WrinkleConfig("V_Wide", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR", 0f, 0.7f),
                }),

                new WrinkleConfig("AE", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.24f),
                }),

                new WrinkleConfig("Ah", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6f),
                }),

                new WrinkleConfig("EE", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR", 0f, 0.7f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR", 0f, 0.7f),
                }),

                new WrinkleConfig("Ih", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.15f),
                }),

                new WrinkleConfig("K_G_H_NG", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.065f),
                }),

                new WrinkleConfig("Oh", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6025f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.56f),
                }),

                new WrinkleConfig("R", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.10f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.63f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.63f),
                }),

                new WrinkleConfig("S_Z", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DL", 0f, 0.14f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UL", 0f, 0.14f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_DR", 0f, 0.14f),
                    new WrinkleRuleSet(props, "head_wm3_normal_head_wm13_lips_UR", 0f, 0.14f),
                }),

                new WrinkleConfig("T_L_D_N", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.0426f),
                }),

                new WrinkleConfig("Th", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_jawOpen", 0f, 0.1212f),
                }),

                new WrinkleConfig("W_OO", new List<WrinkleRuleSet>() {
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_DR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm1_purse_UR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_DR", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UL", 0f, 0.56f),
                    new WrinkleRuleSet(props, "head_wm1_normal_head_wm13_lips_UR", 0f, 0.56f),
                }),

            };

            blendScale = overallWeight;

            UpdateBlendShapeIndices();
        }
         
        private void UpdateBlendShapeIndices()
        {
            if (skinnedMeshRenderer && headMaterial)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;

                foreach (WrinkleConfig wc in config)
                {
                    wc.blendShapeIndex = mesh.GetBlendShapeIndex(wc.blendShapeName);
                    wc.enabled = wc.blendShapeIndex >= 0;

                    foreach (WrinkleRuleSet wm in wc.ruleSets)
                    {
                        if (wrinkleRules.TryGetValue(wm.ruleName, out WrinkleRule rule))
                        {
                            wm.enabled = true;
                        }
                        else
                        {
                            wm.enabled = false;
                        }
                    }
                }                
            }
        }

        void CheckInit()
        {
            if (valueSets == null || valueSets.Length != 12) valueSets = new Vector4[12];
            if (valueStore == null || valueStore.Length != 12) valueStore = new Vector4[12];
            if (valueTemp == null || valueTemp.Length != 12) valueTemp = new Vector4[12];

            if (wrinkleRules == null)
            {
                BuildRules();                
            }                

            if (config == null)
            {
                BuildConfig();
            }
        }

        void Start()
        {
            updateTimer = 0f;

            if (Application.isPlaying)
            {
                SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
                if (smr)
                {
                    foreach (Material mat in smr.materials)
                    {
                        if (mat.IsKeywordEnabled("BOOLEAN_IS_HEAD_ON"))
                        {
                            headMaterial = mat;
                            break;
                        }
                    }
                }
            }

            CheckInit();            
        }

        void Update()
        {
            CheckInit();

            if (config != null && headMaterial && skinnedMeshRenderer)
            {
                if (updateTimer <= 0f)
                {
                    float time = Time.time;
                    float delay = time - lastTime;
                    //float fadeScale = delay / blendFadeDuration;
                    lastTime = time;

                    for (int i = 0; i < valueSets.Length; i++)
                    {
                        valueSets[i] = valueStore[i];
                        valueTemp[i] = Vector4.zero;

                        if (valueSets[i].sqrMagnitude > 0.0001f)
                            valueSets[i] = Vector4.Lerp(valueSets[i], Vector4.zero, blendFalloff * delay);
                        else 
                            valueSets[i] = Vector4.zero;                        
                    }

                    foreach (WrinkleConfig wc in config)
                    {
                        if (wc.blendShapeIndex >= 0 && wc.enabled)
                        {
                            float blendShapeWeight = skinnedMeshRenderer.GetBlendShapeWeight(wc.blendShapeIndex) / 100f;
                            blendShapeWeight = Mathf.Pow(
                                Mathf.Max(0f, Mathf.Min(1f, blendShapeWeight * blendScale)), blendCurve
                            );

                            foreach (WrinkleRuleSet wm in wc.ruleSets)
                            {
                                if (wm.enabled)
                                {
                                    float weight = Mathf.Lerp(wm.min, wm.max, blendShapeWeight) * wm.weight;
                                    WrinkleRule rule = wrinkleRules[wm.ruleName];

                                    int il = ((int)rule.mask) - 1;
                                    int ir = il + 5;
                                    int inone = il + 5;

                                    if (rule.side == MaskSide.left || rule.side == MaskSide.center)
                                    {
                                        valueTemp[il][rule.index] += weight;
                                    }

                                    if (rule.side == MaskSide.right || rule.side == MaskSide.center)
                                    {
                                        valueTemp[ir][rule.index] += weight;
                                    }

                                    if (rule.side == MaskSide.none)
                                    {
                                        valueTemp[inone][rule.index] += weight;
                                    }
                                }
                            }
                        }
                    }

                    // store the uncorrected value sets
                    for (int i = 0; i < valueSets.Length; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (valueTemp[i][j] > valueSets[i][j]) valueSets[i][j] = Mathf.Min(1f, valueTemp[i][j]);
                        }

                        valueStore[i] = valueSets[i];
                    }

                    // brow correction, when browRaiseInner and browDrop are used together
                    // redirect them to browRaiseCorrection
                    //float bri_bd_L_Diff = Mathf.Abs(valueSets[0].y - valueSets[2].y);
                    //float bri_bd_L_Avg = (valueSets[0].y + valueSets[2].y) * 0.5f;
                    float bri_bd_L_Min = Mathf.Min(valueSets[0].y, valueSets[2].y);
                    valueSets[0].y -= bri_bd_L_Min;
                    valueSets[2].y -= bri_bd_L_Min;
                    valueSets[11].x = bri_bd_L_Min;

                    //float bri_bd_R_Diff = Mathf.Abs(valueSets[5].y - valueSets[7].y);
                    //float bri_bd_R_Avg = (valueSets[5].y + valueSets[7].y) * 0.5f;
                    float bri_bd_R_Min = Mathf.Min(valueSets[5].y, valueSets[7].y);
                    valueSets[5].y -= bri_bd_R_Min;
                    valueSets[7].y -= bri_bd_R_Min;
                    valueSets[11].y = bri_bd_R_Min;

                    headMaterial.SetVector("_WrinkleValueSet1AL", valueSets[0]);
                    headMaterial.SetVector("_WrinkleValueSet1BL", valueSets[1]);
                    headMaterial.SetVector("_WrinkleValueSet2L", valueSets[2]);
                    headMaterial.SetVector("_WrinkleValueSet3L", valueSets[3]);
                    headMaterial.SetVector("_WrinkleValueSet12CL", valueSets[4]);
                    headMaterial.SetVector("_WrinkleValueSet1AR", valueSets[5]);
                    headMaterial.SetVector("_WrinkleValueSet1BR", valueSets[6]);
                    headMaterial.SetVector("_WrinkleValueSet2R", valueSets[7]);
                    headMaterial.SetVector("_WrinkleValueSet3R", valueSets[8]);
                    headMaterial.SetVector("_WrinkleValueSet12CR", valueSets[9]);
                    headMaterial.SetVector("_WrinkleValueSet3DB", valueSets[10]);
                    headMaterial.SetVector("_WrinkleValueSetBCCB", valueSets[11]);

                    updateTimer = 1f / updateFrequency;
                }
                else
                {
                    updateTimer -= Time.deltaTime;
                }
            }
        }
    }
}
