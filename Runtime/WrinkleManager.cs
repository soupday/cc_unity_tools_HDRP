using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reallusion.Import
{
    public enum MaskSet { none = 0, set1A = 1, set1B = 2, set2 = 3, set3 = 4, set12C = 5, set3D = 6 }
    public enum MaskSide { none, left, right, center }

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

    public class WrinkleMappings
    {
        public string name;
        public WrinkleRule rule;
        public int blendShapeIndex;
        public bool enabled = false;
        public float min = 0f, max = 1f;

        public WrinkleMappings(Dictionary<string, WrinkleRule> rules, string bsn, string rn, float min = 0f, float max = 1f)
        {
            name = bsn;
            rule = rules[rn];
            this.min = min;
            this.max = max;
            blendShapeIndex = -1;
        }
    }
    
    [ExecuteInEditMode]
    public class WrinkleManager : MonoBehaviour
    {        
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Material headMaterial;
        public bool initialized = false;
        [Range(0f, 2f)]
        public float blendScale = 1f;
        [Range(0.5f, 2f)]
        public float blendCurve = 0.75f;
        [Range(0.1f, 20f)]
        public float blendFalloff = 4f;
        //[Range(0f, 5f)]
        //public float blendFadeDuration = 2f;
        [Range(1f, 120f)]  
        public float updateFrequency = 30f;
        public Vector4[] valueSets = new Vector4[11];

        private Dictionary<string, WrinkleRule> wrinkleRules;
        private List<WrinkleMappings> mappings;

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
            };

            mappings = new List<WrinkleMappings>()
            {   
                // MASK 1-1 (Set 1, Mask 1)

                new WrinkleMappings(wrinkleRules, "Brow_Raise_Inner_L", "head_wm1_normal_head_wm1_browRaiseInner_L"),
                new WrinkleMappings(wrinkleRules, "Brow_Raise_Inner_L", "head_wm2_normal_head_wm2_browsLateral_L", 0f, 0.03f),

                new WrinkleMappings(wrinkleRules, "Brow_Raise_Inner_R", "head_wm1_normal_head_wm1_browRaiseInner_R"),
                new WrinkleMappings(wrinkleRules, "Brow_Raise_Inner_R", "head_wm2_normal_head_wm2_browsLateral_R", 0f, 0.03f),

                new WrinkleMappings(wrinkleRules, "Brow_Raise_Outer_L", "head_wm1_normal_head_wm1_browRaiseOuter_L"),

                new WrinkleMappings(wrinkleRules, "Brow_Raise_Outer_R", "head_wm1_normal_head_wm1_browRaiseOuter_R"),

                new WrinkleMappings(wrinkleRules, "Brow_Drop_L", "head_wm2_normal_head_wm2_browsDown_L", 0f, 0.1f),
                new WrinkleMappings(wrinkleRules, "Brow_Drop_L", "head_wm2_normal_head_wm2_browsLateral_L"),

                new WrinkleMappings(wrinkleRules, "Brow_Drop_R", "head_wm2_normal_head_wm2_browsDown_R", 0f, 0.1f),
                new WrinkleMappings(wrinkleRules, "Brow_Drop_R", "head_wm2_normal_head_wm2_browsLateral_R"),

                new WrinkleMappings(wrinkleRules, "Brow_Compress_L", "head_wm2_normal_head_wm2_browsLateral_L"),

                new WrinkleMappings(wrinkleRules, "Brow_Compress_R", "head_wm2_normal_head_wm2_browsLateral_R"),

                new WrinkleMappings(wrinkleRules, "Eye_Blink_L", "head_wm1_normal_head_wm1_blink_L"),
                new WrinkleMappings(wrinkleRules, "Eye_Blink_L", "head_wm1_normal_head_wm1_squintInner_L", 0f, 0.3f),

                new WrinkleMappings(wrinkleRules, "Eye_Blink_R", "head_wm1_normal_head_wm1_blink_R"),
                new WrinkleMappings(wrinkleRules, "Eye_Blink_R", "head_wm1_normal_head_wm1_squintInner_R", 0f, 0.3f),

                new WrinkleMappings(wrinkleRules, "Eye_Squint_L", "head_wm1_normal_head_wm1_squintInner_L"),

                new WrinkleMappings(wrinkleRules, "Eye_Squint_R", "head_wm1_normal_head_wm1_squintInner_R"),

                new WrinkleMappings(wrinkleRules, "Eye_L_Look_Down", "head_wm1_normal_head_wm1_blink_L"),
                new WrinkleMappings(wrinkleRules, "Eye_R_Look_Down", "head_wm1_normal_head_wm1_blink_R"),

                new WrinkleMappings(wrinkleRules, "Nose_Sneer_L", "head_wm2_normal_head_wm2_browsDown_L", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Nose_Sneer_L", "head_wm2_normal_head_wm2_browsLateral_L", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Nose_Sneer_L", "head_wm2_normal_head_wm2_noseWrinkler_L"),

                new WrinkleMappings(wrinkleRules, "Nose_Sneer_R", "head_wm2_normal_head_wm2_browsDown_R", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Nose_Sneer_R", "head_wm2_normal_head_wm2_browsLateral_R", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Nose_Sneer_R", "head_wm2_normal_head_wm2_noseWrinkler_R"),



                new WrinkleMappings(wrinkleRules, "Nose_Nostril_Raise_L", "head_wm2_normal_head_wm2_noseWrinkler_L", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "Nose_Nostril_Raise_R", "head_wm2_normal_head_wm2_noseWrinkler_R", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "Nose_Crease_L", "head_wm2_normal_head_wm2_noseCrease_L", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "Nose_Crease_R", "head_wm2_normal_head_wm2_noseCrease_R", 0f, 0.7f),


                new WrinkleMappings(wrinkleRules, "Cheek_Raise_L", "head_wm3_normal_head_wm3_cheekRaiseInner_L"),
                new WrinkleMappings(wrinkleRules, "Cheek_Raise_L", "head_wm3_normal_head_wm3_cheekRaiseOuter_L"),
                new WrinkleMappings(wrinkleRules, "Cheek_Raise_L", "head_wm3_normal_head_wm3_cheekRaiseUpper_L"),

                new WrinkleMappings(wrinkleRules, "Cheek_Raise_R", "head_wm3_normal_head_wm3_cheekRaiseInner_R"),
                new WrinkleMappings(wrinkleRules, "Cheek_Raise_R", "head_wm3_normal_head_wm3_cheekRaiseOuter_R"),
                new WrinkleMappings(wrinkleRules, "Cheek_Raise_R", "head_wm3_normal_head_wm3_cheekRaiseUpper_R"),

                new WrinkleMappings(wrinkleRules, "Jaw_Open", "head_wm1_normal_head_wm1_jawOpen"),

                new WrinkleMappings(wrinkleRules, "Jaw_L", "head_wm2_normal_head_wm2_neckStretch_L"),

                new WrinkleMappings(wrinkleRules, "Jaw_R", "head_wm2_normal_head_wm2_neckStretch_R"),

                new WrinkleMappings(wrinkleRules, "Mouth_Up", "head_wm1_normal_head_wm1_chinRaise_L"),
                new WrinkleMappings(wrinkleRules, "Mouth_Up", "head_wm1_normal_head_wm1_chinRaise_R"),

                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm3_normal_head_wm3_smile_L", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_L", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm3_normal_head_wm3_smile_R", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Mouth_R", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "Mouth_Smile_L", "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_L", "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_L", "head_wm3_normal_head_wm3_smile_L"),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_L", "head_wm3_normal_head_wm13_lips_DL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_L", "head_wm3_normal_head_wm13_lips_UL"),

                new WrinkleMappings(wrinkleRules, "Mouth_Smile_R", "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_R", "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.6f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_R", "head_wm3_normal_head_wm3_smile_R"),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_R", "head_wm3_normal_head_wm13_lips_DR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_R", "head_wm3_normal_head_wm13_lips_UR"),

                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_L", "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.4f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_L", "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.4f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_L", "head_wm3_normal_head_wm3_smile_L", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_L", "head_wm3_normal_head_wm13_lips_DL", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_L", "head_wm3_normal_head_wm13_lips_UL", 0f, 0.8f),

                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_R", "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.4f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_R", "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.4f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_R", "head_wm3_normal_head_wm3_smile_R", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_R", "head_wm3_normal_head_wm13_lips_DR", 0f, 0.8f),
                new WrinkleMappings(wrinkleRules, "Mouth_Smile_Sharp_R", "head_wm3_normal_head_wm13_lips_UR", 0f, 0.8f),

                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_L", "head_wm3_normal_head_wm3_cheekRaiseInner_L", 0f, 0.15f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_L", "head_wm3_normal_head_wm3_cheekRaiseOuter_L", 0f, 0.15f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_L", "head_wm3_normal_head_wm3_smile_L", 0f, 0.3f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_L", "head_wm3_normal_head_wm13_lips_DL", 0f, 0.3f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_L", "head_wm3_normal_head_wm13_lips_UL", 0f, 0.3f),

                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_R", "head_wm3_normal_head_wm3_cheekRaiseInner_R", 0f, 0.15f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_R", "head_wm3_normal_head_wm3_cheekRaiseOuter_R", 0f, 0.15f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_R", "head_wm3_normal_head_wm3_smile_R", 0f, 0.3f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_R", "head_wm3_normal_head_wm13_lips_DR", 0f, 0.3f),
                new WrinkleMappings(wrinkleRules, "Mouth_Dimple_R", "head_wm3_normal_head_wm13_lips_UR", 0f, 0.3f),

                new WrinkleMappings(wrinkleRules, "Mouth_Stretch_L", "head_wm2_normal_head_wm2_mouthStretch_L"),

                new WrinkleMappings(wrinkleRules, "Mouth_Stretch_R", "head_wm2_normal_head_wm2_mouthStretch_R"),

                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Up_L", "head_wm1_normal_head_wm1_purse_UL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Up_L", "head_wm1_normal_head_wm13_lips_UL"),

                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Up_R", "head_wm1_normal_head_wm1_purse_UR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Up_R", "head_wm1_normal_head_wm13_lips_UR"),

                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_L", "head_wm1_normal_head_wm1_chinRaise_L", 0f, 0.5f),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_L", "head_wm1_normal_head_wm1_purse_DL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_L", "head_wm1_normal_head_wm13_lips_DL"),

                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_R", "head_wm1_normal_head_wm1_chinRaise_R", 0f, 0.5f),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_R", "head_wm1_normal_head_wm1_purse_DR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker_Down_R", "head_wm1_normal_head_wm13_lips_DR"),

                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_purse_DL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_purse_DR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_purse_UL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_purse_UR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm13_lips_DL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm13_lips_DR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm13_lips_UL"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm13_lips_UR"),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_chinRaise_L", 0f, 0.5f),
                new WrinkleMappings(wrinkleRules, "Mouth_Pucker", "head_wm1_normal_head_wm1_chinRaise_R", 0f, 0.5f),

                new WrinkleMappings(wrinkleRules, "Mouth_Chin_Up", "head_wm1_normal_head_wm1_chinRaise_L"),
                new WrinkleMappings(wrinkleRules, "Mouth_Chin_Up", "head_wm1_normal_head_wm1_chinRaise_R"),

                new WrinkleMappings(wrinkleRules, "Mouth_Up_Upper_L", "head_wm2_normal_head_wm2_noseCrease_L"),

                new WrinkleMappings(wrinkleRules, "Mouth_Up_Upper_R", "head_wm2_normal_head_wm2_noseCrease_R"),

                new WrinkleMappings(wrinkleRules, "Neck_Tighten_L", "head_wm2_normal_head_wm2_neckStretch_L"),

                new WrinkleMappings(wrinkleRules, "Neck_Tighten_R", "head_wm2_normal_head_wm2_neckStretch_R"),

                new WrinkleMappings(wrinkleRules, "Head_Turn_L", "head_wm2_normal_head_wm2_neckStretch_R", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "Head_Turn_R", "head_wm2_normal_head_wm2_neckStretch_L", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "Head_Tilt_L", "head_wm2_normal_head_wm2_neckStretch_R", 0f, 0.75f),

                new WrinkleMappings(wrinkleRules, "Head_Tilt_R", "head_wm2_normal_head_wm2_neckStretch_L", 0f, 0.75f),

                new WrinkleMappings(wrinkleRules, "Head_Backward", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.5f),

                new WrinkleMappings(wrinkleRules, "Mouth_Frown_L", "head_wm2_normal_head_wm2_mouthStretch_L"),
                new WrinkleMappings(wrinkleRules, "Mouth_Frown_R", "head_wm2_normal_head_wm2_mouthStretch_R"),
                new WrinkleMappings(wrinkleRules, "Mouth_Shrug_Lower", "head_wm1_normal_head_wm1_chinRaise_L"),
                new WrinkleMappings(wrinkleRules, "Mouth_Shrug_Lower", "head_wm1_normal_head_wm1_chinRaise_R"),

                new WrinkleMappings(wrinkleRules, "V_Open", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm1_purse_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm1_purse_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm1_purse_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm1_purse_UR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight_O", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm1_purse_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm1_purse_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm1_purse_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm1_purse_UR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Tight", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "V_Wide", "head_wm3_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Wide", "head_wm3_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Wide", "head_wm3_normal_head_wm13_lips_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "V_Wide", "head_wm3_normal_head_wm13_lips_UR", 0f, 0.7f),

                new WrinkleMappings(wrinkleRules, "AE", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.24f),
                new WrinkleMappings(wrinkleRules, "Ah", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6f),

                new WrinkleMappings(wrinkleRules, "EE", "head_wm3_normal_head_wm13_lips_DL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "EE", "head_wm3_normal_head_wm13_lips_UL", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "EE", "head_wm3_normal_head_wm13_lips_DR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "EE", "head_wm3_normal_head_wm13_lips_UR", 0f, 0.7f),
                new WrinkleMappings(wrinkleRules, "Ih", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.15f),
                new WrinkleMappings(wrinkleRules, "K_G_H_NG", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.065f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.6025f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm1_purse_DL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm1_purse_DR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm1_purse_UL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm1_purse_UR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "Oh", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.10f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm1_purse_DL", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm1_purse_DR", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm1_purse_UL", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm1_purse_UR", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.63f),
                new WrinkleMappings(wrinkleRules, "R", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.63f),

                new WrinkleMappings(wrinkleRules, "S_Z", "head_wm3_normal_head_wm13_lips_DL", 0f, 0.14f),
                new WrinkleMappings(wrinkleRules, "S_Z", "head_wm3_normal_head_wm13_lips_UL", 0f, 0.14f),
                new WrinkleMappings(wrinkleRules, "S_Z", "head_wm3_normal_head_wm13_lips_DR", 0f, 0.14f),
                new WrinkleMappings(wrinkleRules, "S_Z", "head_wm3_normal_head_wm13_lips_UR", 0f, 0.14f),

                new WrinkleMappings(wrinkleRules, "T_L_D_N", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.0426f),
                new WrinkleMappings(wrinkleRules, "Th", "head_wm1_normal_head_wm1_jawOpen", 0f, 0.1212f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm1_purse_DL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm1_purse_DR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm1_purse_UL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm1_purse_UR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm13_lips_DL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm13_lips_DR", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm13_lips_UL", 0f, 0.56f),
                new WrinkleMappings(wrinkleRules, "W_OO", "head_wm1_normal_head_wm13_lips_UR", 0f, 0.56f),
            };
        }
         
        private void UpdateBlendShapeIndices()
        {
            if (skinnedMeshRenderer && headMaterial)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;

                foreach (WrinkleMappings wm in mappings)
                {
                    wm.blendShapeIndex = mesh.GetBlendShapeIndex(wm.name);
                    wm.enabled = wm.blendShapeIndex >= 0;                    
                }

                updateTimer = 1f / updateFrequency;
                lastTime = Time.time;                
            }
        }

        void CheckInit()
        {
            if (wrinkleRules == null || mappings == null || !initialized)
            {
                initialized = false;
                BuildRules();
                if (wrinkleRules != null || mappings != null)
                {
                    UpdateBlendShapeIndices();
                    initialized = true;
                }
            }
        }
           
        void Start()
        {
            initialized = false;
            wrinkleRules = null;
            mappings = null;

            CheckInit();
        }

        void Update()
        {
            CheckInit();

            if (initialized && headMaterial && skinnedMeshRenderer)
            {
                if (updateTimer <= 0f)
                {
                    float time = Time.time;
                    float delay = time - lastTime;
                    //float fadeScale = delay / blendFadeDuration;
                    lastTime = time;

                    for (int i = 0; i < 11; i++)
                    {                                                
                        if (valueSets[i].sqrMagnitude > 0.0001f)
                            valueSets[i] = Vector4.Lerp(valueSets[i], Vector4.zero, blendFalloff * delay);
                        else 
                            valueSets[i] = Vector4.zero;                        
                    }

                    foreach (WrinkleMappings wm in mappings)
                    {
                        if (wm.blendShapeIndex >= 0)
                        {
                            float weight = skinnedMeshRenderer.GetBlendShapeWeight(wm.blendShapeIndex) / 100f;
                            weight = Mathf.Pow(
                                Mathf.Max(0f, Mathf.Min(1f, weight * blendScale)), blendCurve
                            );
                            weight = Mathf.Lerp(wm.min, wm.max, weight);


                            int il = ((int)wm.rule.mask) - 1;
                            int ir = il + 5;
                            int inone = il + 5;

                            if (wm.rule.side == MaskSide.left || wm.rule.side == MaskSide.center)
                            {
                                if (weight > valueSets[il][wm.rule.index])
                                    valueSets[il][wm.rule.index] = weight;
                            }

                            if (wm.rule.side == MaskSide.right || wm.rule.side == MaskSide.center)
                            {
                                if (weight > valueSets[ir][wm.rule.index])
                                    valueSets[ir][wm.rule.index] = weight;
                            }

                            if (wm.rule.side == MaskSide.none)
                            {
                                if (weight > valueSets[inone][wm.rule.index])
                                    valueSets[inone][wm.rule.index] = weight;
                            }
                        }
                    }                    

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
