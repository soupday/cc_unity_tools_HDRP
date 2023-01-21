using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reallusion.Import
{
    [ExecuteInEditMode]
    public class WrinkleManager : MonoBehaviour
    {
        public enum MaskSet { none, set11 = 1, set12 = 2, set2 = 3, set3 = 4 }
        public enum MaskSide { none, left, right, center }        

        public class WrinkleMappings
        {
            public string name;
            public MaskSet set;
            public MaskSide side;            
            public int setIndex;
            public int blendShapeIndex;
            public bool enabled = false;
            public float weight = 0f;

            public WrinkleMappings(string bsn, MaskSet s, MaskSide d, int i)
            {
                name = bsn;
                set = s;
                side = d;
                setIndex = i;                
                blendShapeIndex = -1;
            }
        }

        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Material headMaterial;
        public bool initialized = false;
        public float blendScale = 1f;

        public Vector4[] valueSets = new Vector4[8];
        public Vector4[] boolSets = new Vector4[8];

        public List<WrinkleMappings> mappings = new List<WrinkleMappings>()
        {   
            // MASK 1-1 (Set 1, Mask 1)

            // eye_squint
            new WrinkleMappings("Eye_Squint_L", MaskSet.set11, MaskSide.left, 0),
            new WrinkleMappings("Eye_Squint_R", MaskSet.set11, MaskSide.right, 0),

            // brow_raise_inner
            new WrinkleMappings("Brow_Raise_Inner_L", MaskSet.set11, MaskSide.left, 1),
            new WrinkleMappings("Brow_Raise_Inner_R", MaskSet.set11, MaskSide.right, 1),

            // mouth_pucker_lower
            new WrinkleMappings("Mouth_Pucker_Down_L", MaskSet.set11, MaskSide.left, 2),
            new WrinkleMappings("Mouth_Pucker_Down_R", MaskSet.set11, MaskSide.right, 2),

            // chin_up
            new WrinkleMappings("Mouth_Shrug_Lower", MaskSet.set11, MaskSide.center, 3),            

            // MASK 1-2 (Set 1, Mask 2)   
            
            // jaw_open
            new WrinkleMappings("Jaw_Open", MaskSet.set12, MaskSide.center, 0),

            // eye_blink
            new WrinkleMappings("Eye_Blink_L", MaskSet.set12, MaskSide.left, 1),
            new WrinkleMappings("Eye_Blink_R", MaskSet.set12, MaskSide.right, 1),

            // brow_raise_outer
            new WrinkleMappings("Brow_Raise_Outer_L", MaskSet.set12, MaskSide.left, 2),
            new WrinkleMappings("Brow_Raise_Outer_R", MaskSet.set12, MaskSide.right, 2),

            // mouth_pucker_upper
            new WrinkleMappings("Mouth_Pucker_Up_L", MaskSet.set12, MaskSide.left, 3),
            new WrinkleMappings("Mouth_Pucker_Up_R", MaskSet.set12, MaskSide.right, 3),            

            // MASK 2 (Set 2, Mask 1)            

            // neck_tighten
            new WrinkleMappings("Neck_Tighten_L", MaskSet.set2, MaskSide.left, 0),
            new WrinkleMappings("Neck_Tighten_R", MaskSet.set2, MaskSide.right, 0),
            
            // brow_drop
            new WrinkleMappings("Brow_Drop_L", MaskSet.set2, MaskSide.left, 1),
            new WrinkleMappings("Brow_Drop_R", MaskSet.set2, MaskSide.right, 1),
                // brow_drop
                new WrinkleMappings("Nose_Sneer_L", MaskSet.set2, MaskSide.left, 1),
                new WrinkleMappings("Nose_Sneer_R", MaskSet.set2, MaskSide.right, 1),            

            // nose_sneer
            new WrinkleMappings("Nose_Sneer_L", MaskSet.set2, MaskSide.left, 2),
            new WrinkleMappings("Nose_Sneer_R", MaskSet.set2, MaskSide.right, 2),
                // nose_sneer
                new WrinkleMappings("Nose_Nostril_Raise_L", MaskSet.set2, MaskSide.left, 2),
                new WrinkleMappings("Nose_Nostril_Raise_R", MaskSet.set2, MaskSide.right, 2),

            // mouth_stretch
            new WrinkleMappings("Mouth_Stretch_L", MaskSet.set2, MaskSide.left, 3),
            new WrinkleMappings("Mouth_Stretch_R", MaskSet.set2, MaskSide.right, 3),
                     
            // MASK 3 (Set 3, Mask 1)    

            // mouth_smile
            new WrinkleMappings("Mouth_Smile_L", MaskSet.set3, MaskSide.left, 0),
            new WrinkleMappings("Mouth_Smile_R", MaskSet.set3, MaskSide.right, 0),
                // mouth_smile
                new WrinkleMappings("Mouth_Smile_Sharp_L", MaskSet.set3, MaskSide.left, 0),
                new WrinkleMappings("Mouth_Smile_Sharp_R", MaskSet.set3, MaskSide.right, 0),            

            // brow_compress
            new WrinkleMappings("Brow_Compress_L", MaskSet.set3, MaskSide.left, 1),
            new WrinkleMappings("Brow_Compress_R", MaskSet.set3, MaskSide.right, 1),

            // cheek_raise
            new WrinkleMappings("Cheek_Raise_L", MaskSet.set3, MaskSide.left, 2),
            new WrinkleMappings("Cheek_Raise_R", MaskSet.set3, MaskSide.right, 2),

            // nose_crease
            new WrinkleMappings("Nose_Crease_L", MaskSet.set3, MaskSide.left, 3),
            new WrinkleMappings("Nose_Crease_R", MaskSet.set3, MaskSide.right, 3),                        
                // nose_crease
                new WrinkleMappings("Mouth_Up_Upper_L", MaskSet.set3, MaskSide.left, 3),
                new WrinkleMappings("Mouth_Up_Upper_R", MaskSet.set3, MaskSide.right, 3),                        
        };

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

                initialized = true;
            }
        }
        

        void Start()
        {
            UpdateBlendShapeIndices();
        }

        private float GetBlendShapeWeight(string name)
        {
            int id = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
            float weight = skinnedMeshRenderer.GetBlendShapeWeight(id);
            return weight;
        }

        float updateTimer = 0f;
        void Update()
        {
            if (!initialized) UpdateBlendShapeIndices();

            if (initialized && headMaterial && skinnedMeshRenderer)
            {
                if (updateTimer <= 0f)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        boolSets[i] = Vector4.zero;
                        valueSets[i] = Vector4.Lerp(valueSets[i], Vector4.zero, 8f * Time.deltaTime);
                    }

                    foreach (WrinkleMappings wm in mappings)
                    {
                        if (wm.blendShapeIndex >= 0)
                        {
                            float weight = skinnedMeshRenderer.GetBlendShapeWeight(wm.blendShapeIndex) / 100f;
                            weight = Mathf.Max(0f, Mathf.Min(1f, weight * blendScale));

                            int il = ((int)wm.set) - 1;
                            int ir = il + 4;

                            if (wm.side == MaskSide.left || wm.side == MaskSide.center)
                            {
                                /*
                                if (boolSets[il][wm.setIndex] > 0f)
                                {
                                    if (weight > valueSets[il][wm.setIndex])
                                        valueSets[il][wm.setIndex] = weight;
                                }
                                else
                                {
                                    valueSets[il][wm.setIndex] = weight;
                                    boolSets[il][wm.setIndex] = 1f;
                                }
                                */
                                if (weight > valueSets[il][wm.setIndex])
                                    valueSets[il][wm.setIndex] = weight;
                            }

                            if (wm.side == MaskSide.right || wm.side == MaskSide.center)
                            {
                                /*
                                if (boolSets[ir][wm.setIndex] > 0f)
                                {
                                    if (weight > valueSets[ir][wm.setIndex])
                                        valueSets[ir][wm.setIndex] = weight;
                                }
                                else
                                {
                                    valueSets[ir][wm.setIndex] = weight;
                                    boolSets[ir][wm.setIndex] = 1f;
                                }
                                */
                                if (weight > valueSets[ir][wm.setIndex])
                                    valueSets[ir][wm.setIndex] = weight;
                            }                            
                        }
                    }

                    headMaterial.SetVector("_WrinkleValueSet11L", valueSets[0]);
                    headMaterial.SetVector("_WrinkleValueSet12L", valueSets[1]);
                    headMaterial.SetVector("_WrinkleValueSet2L", valueSets[2]);
                    headMaterial.SetVector("_WrinkleValueSet3L", valueSets[3]);
                    headMaterial.SetVector("_WrinkleValueSet11R", valueSets[4]);
                    headMaterial.SetVector("_WrinkleValueSet12R", valueSets[5]);
                    headMaterial.SetVector("_WrinkleValueSet2R", valueSets[6]);
                    headMaterial.SetVector("_WrinkleValueSet3R", valueSets[7]);
                }
                else
                {
                    updateTimer -= Time.deltaTime;
                }
            }
        }
    }
}
