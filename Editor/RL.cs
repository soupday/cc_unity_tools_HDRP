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
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using UnityEditor.Animations;
using System.Reflection;

namespace Reallusion.Import
{
    public enum BaseGeneration
    {
        None,
        Unknown,
        GameBase,
        G1,
        G3,
        G3Plus,
        ActorCore,
        ActorBuild
    };

    /// <summary>
    ///     Functions taken from old Reallusion AutoSetup...
    /// </summary>
    public class RL
    {
        // Applicable CC character generation:        

        // Applicable CC Character Uid EBaseGeneration:
        public static readonly Dictionary<string, BaseGeneration> GENERATION_MAP = new Dictionary<string, BaseGeneration>
        {
            { "RL_CC3_Plus", BaseGeneration.G3Plus },
            { "RL_CharacterCreator_Base_Game_G1_Divide_Eyelash_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Game_G1_Multi_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Game_G1_One_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Std_G3", BaseGeneration.G3 },
            { "RL_G6_Standard_Series", BaseGeneration.G1 },
            { "NonStdLookAtDataCopyFromCCBase", BaseGeneration.ActorCore },
            { "ActorBuild", BaseGeneration.ActorBuild },
            { "ActorScan", BaseGeneration.ActorCore }
        };
        
        public static BaseGeneration GetCharacterGeneration(GameObject fbx, string generationString)
        {
            if (!string.IsNullOrEmpty(generationString))
            {
                if (GENERATION_MAP.TryGetValue(generationString, out BaseGeneration gen)) return gen;
            }
            else
            {
                if (fbx)
                {                    
                    Transform[] children = fbx.transform.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        string objectName = child.gameObject.name;

                        if (objectName.iContains("RootNode_0_")) return BaseGeneration.ActorCore;
                        if (objectName.iContains("CC_Base_L_Pinky3")) return BaseGeneration.G3;
                        if (objectName.iContains("pinky_03_l")) return BaseGeneration.GameBase;
                        if (objectName.iContains("CC_Base_L_Finger42")) return BaseGeneration.G1;
                        if (objectName.iContains("RL_BoneRoot"))
                        {
                            if (child.Find("CC_Base_Hip"))
                            {
                                Material acMat = GetActorCoreSingleMaterial(fbx);
                                if (acMat) return BaseGeneration.ActorCore;
                                else return BaseGeneration.G3;
                            }
                        }
                    }

                    foreach (Transform child in children)
                    {
                        string objectName = child.gameObject.name;

                        if (objectName.iContains("CC_Game_Body") || objectName.iContains("CC_Game_Tongue"))
                        {
                            return BaseGeneration.GameBase;
                        }

                        if (objectName == "CC_Base_Body")
                        {
                            Renderer renderer = child.GetComponent<Renderer>();
                            foreach (Material mat in renderer.sharedMaterials)
                            {
                                if (!mat) continue;

                                string materialName = mat.name;
                                if (materialName.iContains("Skin_Body"))
                                    return BaseGeneration.G1;
                                else if (materialName.iContains("Std_Skin_Body"))
                                    return BaseGeneration.G3;
                                else if (materialName.iContains("ga_skin_body"))
                                    return BaseGeneration.GameBase;
                            }
                        }
                    }
                }                
            }
            return BaseGeneration.Unknown;
        }

        public static void ForceLegacyBlendshapeNormals(ModelImporter importer)
        {
            string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
            PropertyInfo prop = importer.GetType().GetProperty(pName, 
                                                                BindingFlags.Instance | 
                                                                BindingFlags.NonPublic | 
                                                                BindingFlags.Public);
            prop.SetValue(importer, true);
        }

        public static void HumanoidImportSettings(GameObject fbx, ModelImporter importer, CharacterInfo info, Avatar avatar = null)
        {            
            // import normals to avoid mesh smoothing issues            
            // importing blend shape normals gives disasterously bad results, they need to be recalculated,
            // ideally using the legacy blend shape normals option, but this has not been exposed to scripts so...
            int importSet = 0;
            if (info.IsBlenderProject) importSet = 1;
            switch(importSet)
            {
                case 0: // From CC3/4
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importBlendShapes = true;
                    importer.importBlendShapeNormals = ModelImporterNormals.Import;                    
                    importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;                    
                    importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
                    importer.normalSmoothingAngle = 60f;
                    break;
                case 1: // From Blender
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importBlendShapes = true;
                    importer.importBlendShapeNormals = ModelImporterNormals.Import;
                    importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;                    
                    importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
                    importer.normalSmoothingAngle = 60f;
                    break;                
            }
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.keepQuads = false;
            importer.weldVertices = true;
            ForceLegacyBlendshapeNormals(importer);

            importer.autoGenerateAvatarMappingIfUnspecified = true;
            
            if (info.Generation == BaseGeneration.Unknown)
            {
                switch (info.UnknownRigType)
                {
                    case CharacterInfo.RigOverride.None:
                        importer.animationType = ModelImporterAnimationType.None;
                        break;
                    case CharacterInfo.RigOverride.Humanoid:
                        importer.animationType = ModelImporterAnimationType.Human;
                        break;
                    case CharacterInfo.RigOverride.Generic:
                    default:
                        importer.animationType = ModelImporterAnimationType.Generic;
                        break;
                }
                return;
            }

            if (avatar)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;

                importer.sourceAvatar = avatar;
            }
            else
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                HumanDescription human = importer.humanDescription;
                Func<string, string, HumanBone> Bone = (humanName, boneName) => new HumanBone()
                {
                    humanName = humanName,
                    boneName = boneName
                };
                List<HumanBone> boneList = new List<HumanBone>();

                #region HumanBoneDescription
                if (info.Generation == BaseGeneration.G3 ||
                    info.Generation == BaseGeneration.G3Plus ||
                    info.Generation == BaseGeneration.ActorCore ||
                    info.Generation == BaseGeneration.ActorBuild)
                {
                    boneList = new List<HumanBone> {
                        Bone("Chest", "CC_Base_Spine01"),
                        Bone("Head", "CC_Base_Head"),
                        Bone("Hips", "CC_Base_Hip"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "CC_Base_L_Index3"),
                        Bone("Left Index Intermediate", "CC_Base_L_Index2"),
                        Bone("Left Index Proximal", "CC_Base_L_Index1"),
                        Bone("Left Little Distal","CC_Base_L_Pinky3"),
                        Bone("Left Little Intermediate","CC_Base_L_Pinky2"),
                        Bone("Left Little Proximal","CC_Base_L_Pinky1"),
                        Bone("Left Middle Distal", "CC_Base_L_Mid3"),
                        Bone("Left Middle Intermediate", "CC_Base_L_Mid2"),
                        Bone("Left Middle Proximal", "CC_Base_L_Mid1"),
                        Bone("Left Ring Distal", "CC_Base_L_Ring3"),
                        Bone("Left Ring Intermediate", "CC_Base_L_Ring2"),
                        Bone("Left Ring Proximal", "CC_Base_L_Ring1"),
                        Bone("Left Thumb Distal", "CC_Base_L_Thumb3"),
                        Bone("Left Thumb Intermediate", "CC_Base_L_Thumb2"),
                        Bone("Left Thumb Proximal", "CC_Base_L_Thumb1"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "CC_Base_L_Foot"),
                        Bone("LeftHand", "CC_Base_L_Hand"),
                        Bone("LeftLowerArm", "CC_Base_L_Forearm"),
                        Bone("LeftLowerLeg", "CC_Base_L_Calf"),
                        Bone("LeftShoulder", "CC_Base_L_Clavicle"),
                        Bone("LeftToes", "CC_Base_L_ToeBase"),
                        Bone("LeftUpperArm", "CC_Base_L_Upperarm"),
                        Bone("LeftUpperLeg", "CC_Base_L_Thigh"),
                        Bone("Neck", "CC_Base_NeckTwist01"),
                        Bone("Right Index Distal", "CC_Base_R_Index3"),
                        Bone("Right Index Intermediate", "CC_Base_R_Index2"),
                        Bone("Right Index Proximal", "CC_Base_R_Index1"),
                        Bone("Right Little Distal","CC_Base_R_Pinky3"),
                        Bone("Right Little Intermediate","CC_Base_R_Pinky2"),
                        Bone("Right Little Proximal","CC_Base_R_Pinky1"),
                        Bone("Right Middle Distal", "CC_Base_R_Mid3"),
                        Bone("Right Middle Intermediate", "CC_Base_R_Mid2"),
                        Bone("Right Middle Proximal", "CC_Base_R_Mid1"),
                        Bone("Right Ring Distal", "CC_Base_R_Ring3"),
                        Bone("Right Ring Intermediate", "CC_Base_R_Ring2"),
                        Bone("Right Ring Proximal", "CC_Base_R_Ring1"),
                        Bone("Right Thumb Distal", "CC_Base_R_Thumb3"),
                        Bone("Right Thumb Intermediate", "CC_Base_R_Thumb2"),
                        Bone("Right Thumb Proximal", "CC_Base_R_Thumb1"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "CC_Base_R_Foot"),
                        Bone("RightHand", "CC_Base_R_Hand"),
                        Bone("RightLowerArm", "CC_Base_R_Forearm"),
                        Bone("RightLowerLeg", "CC_Base_R_Calf"),
                        Bone("RightShoulder", "CC_Base_R_Clavicle"),
                        Bone("RightToes", "CC_Base_R_ToeBase"),
                        Bone("RightUpperArm", "CC_Base_R_Upperarm"),
                        Bone("RightUpperLeg", "CC_Base_R_Thigh"),
                        Bone("Spine", "CC_Base_Waist"),
                        Bone("UpperChest", "CC_Base_Spine02"),
                    };
                }
                else if (info.Generation == BaseGeneration.G1)
                {
                    boneList = new List<HumanBone> {
                        Bone("Chest", "CC_Base_Spine01"),
                        Bone("Head", "CC_Base_Head"),
                        Bone("Hips", "CC_Base_Hip"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "CC_Base_L_Finger12"),
                        Bone("Left Index Intermediate", "CC_Base_L_Finger11"),
                        Bone("Left Index Proximal", "CC_Base_L_Finger10"),
                        Bone("Left Little Distal","CC_Base_L_Finger42"),
                        Bone("Left Little Intermediate","CC_Base_L_Finger41"),
                        Bone("Left Little Proximal","CC_Base_L_Finger40"),
                        Bone("Left Middle Distal", "CC_Base_L_Finger22"),
                        Bone("Left Middle Intermediate", "CC_Base_L_Finger21"),
                        Bone("Left Middle Proximal", "CC_Base_L_Finger20"),
                        Bone("Left Ring Distal", "CC_Base_L_Finger32"),
                        Bone("Left Ring Intermediate", "CC_Base_L_Finger31"),
                        Bone("Left Ring Proximal", "CC_Base_L_Finger30"),
                        Bone("Left Thumb Distal", "CC_Base_L_Finger02"),
                        Bone("Left Thumb Intermediate", "CC_Base_L_Finger01"),
                        Bone("Left Thumb Proximal", "CC_Base_L_Finger00"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "CC_Base_L_Foot"),
                        Bone("LeftHand", "CC_Base_L_Hand"),
                        Bone("LeftLowerArm", "CC_Base_L_Forearm"),
                        Bone("LeftLowerLeg", "CC_Base_L_Calf"),
                        Bone("LeftShoulder", "CC_Base_L_Clavicle"),
                        Bone("LeftToes", "CC_Base_L_ToeBase"),
                        Bone("LeftUpperArm", "CC_Base_L_Upperarm"),
                        Bone("LeftUpperLeg", "CC_Base_L_Thigh"),
                        Bone("Neck", "CC_Base_NeckTwist01"),
                        Bone("Right Index Distal", "CC_Base_R_Finger12"),
                        Bone("Right Index Intermediate", "CC_Base_R_Finger11"),
                        Bone("Right Index Proximal", "CC_Base_R_Finger10"),
                        Bone("Right Little Distal","CC_Base_R_Finger42"),
                        Bone("Right Little Intermediate","CC_Base_R_Finger41"),
                        Bone("Right Little Proximal","CC_Base_R_Finger40"),
                        Bone("Right Middle Distal", "CC_Base_R_Finger22"),
                        Bone("Right Middle Intermediate", "CC_Base_R_Finger21"),
                        Bone("Right Middle Proximal", "CC_Base_R_Finger20"),
                        Bone("Right Ring Distal", "CC_Base_R_Finger32"),
                        Bone("Right Ring Intermediate", "CC_Base_R_Finger31"),
                        Bone("Right Ring Proximal", "CC_Base_R_Finger30"),
                        Bone("Right Thumb Distal", "CC_Base_R_Finger02"),
                        Bone("Right Thumb Intermediate", "CC_Base_R_Finger01"),
                        Bone("Right Thumb Proximal", "CC_Base_R_Finger00"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "CC_Base_R_Foot"),
                        Bone("RightHand", "CC_Base_R_Hand"),
                        Bone("RightLowerArm", "CC_Base_R_Forearm"),
                        Bone("RightLowerLeg", "CC_Base_R_Calf"),
                        Bone("RightShoulder", "CC_Base_R_Clavicle"),
                        Bone("RightToes", "CC_Base_R_ToeBase"),
                        Bone("RightUpperArm", "CC_Base_R_Upperarm"),
                        Bone("RightUpperLeg", "CC_Base_R_Thigh"),
                        Bone("Spine", "CC_Base_Waist"),
                        Bone("UpperChest", "CC_Base_Spine02"),
                    };
                }
                else if (info.Generation == BaseGeneration.GameBase)
                {
                    boneList = new List<HumanBone> {
                        Bone("Chest", "spine_02"),
                        Bone("Head", "head"),
                        Bone("Hips", "pelvis"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "index_03_l"),
                        Bone("Left Index Intermediate", "index_02_l"),
                        Bone("Left Index Proximal", "index_01_l"),
                        Bone("Left Little Distal","pinky_03_l"),
                        Bone("Left Little Intermediate","pinky_02_l"),
                        Bone("Left Little Proximal","pinky_01_l"),
                        Bone("Left Middle Distal", "middle_03_l"),
                        Bone("Left Middle Intermediate", "middle_02_l"),
                        Bone("Left Middle Proximal", "middle_01_l"),
                        Bone("Left Ring Distal", "ring_03_l"),
                        Bone("Left Ring Intermediate", "ring_02_l"),
                        Bone("Left Ring Proximal", "ring_01_l"),
                        Bone("Left Thumb Distal", "thumb_03_l"),
                        Bone("Left Thumb Intermediate", "thumb_02_l"),
                        Bone("Left Thumb Proximal", "thumb_01_l"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "foot_l"),
                        Bone("LeftHand", "hand_l"),
                        Bone("LeftLowerArm", "lowerarm_l"),
                        Bone("LeftLowerLeg", "calf_l"),
                        Bone("LeftShoulder", "clavicle_l"),
                        Bone("LeftToes", "ball_l"),
                        Bone("LeftUpperArm", "upperarm_l"),
                        Bone("LeftUpperLeg", "thigh_l"),
                        Bone("Neck", "neck_01"),
                        Bone("Right Index Distal", "index_03_r"),
                        Bone("Right Index Intermediate", "index_02_r"),
                        Bone("Right Index Proximal", "index_01_r"),
                        Bone("Right Little Distal","pinky_03_r"),
                        Bone("Right Little Intermediate","pinky_02_r"),
                        Bone("Right Little Proximal","pinky_01_r"),
                        Bone("Right Middle Distal", "middle_03_r"),
                        Bone("Right Middle Intermediate", "middle_02_r"),
                        Bone("Right Middle Proximal", "middle_01_r"),
                        Bone("Right Ring Distal", "ring_03_r"),
                        Bone("Right Ring Intermediate", "ring_02_r"),
                        Bone("Right Ring Proximal", "ring_01_r"),
                        Bone("Right Thumb Distal", "thumb_03_r"),
                        Bone("Right Thumb Intermediate", "thumb_02_r"),
                        Bone("Right Thumb Proximal", "thumb_01_r"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "foot_r"),
                        Bone("RightHand", "hand_r"),
                        Bone("RightLowerArm", "lowerarm_r"),
                        Bone("RightLowerLeg", "calf_r"),
                        Bone("RightShoulder", "clavicle_r"),
                        Bone("RightToes", "ball_r"),
                        Bone("RightUpperArm", "upperarm_r"),
                        Bone("RightUpperLeg", "thigh_r"),
                        Bone("Spine", "spine_01"),
                        Bone("UpperChest", "spine_03"),
                    };
                }

                // clean up bone list for missing bones (from bone LOD exports)
                for (int b = 0; b < boneList.Count; b++)
                {
                    if (Util.FindChildRecursive(fbx.transform, boneList[b].boneName) == null)
                    {
                        //Debug.LogWarning("Missing bone: " + boneList[b].boneName);
                        boneList.RemoveAt(b--);
                    }
                }

                if (boneList.Count > 0)
                    human.human = boneList.ToArray();

                #endregion

                for (int i = 0; i < human.human.Length; ++i)
                {
                    human.human[i].limit.useDefaultValues = true;
                }

                human.upperArmTwist = 0.5f;
                human.lowerArmTwist = 0.5f;
                human.upperLegTwist = 0.5f;
                human.lowerLegTwist = 0.5f;
                human.armStretch = 0.05f;
                human.legStretch = 0.05f;
                human.feetSpacing = 0.0f;
                human.hasTranslationDoF = true;

                if (info.JsonData != null)
                {
                    Transform[] transforms = fbx.GetComponentsInChildren<Transform>();
                    SkeletonBone[] bones = new SkeletonBone[transforms.Length];
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        bones[i].name = transforms[i].name;
                        bones[i].position = transforms[i].localPosition;
                        bones[i].rotation = transforms[i].localRotation;
                        bones[i].scale = transforms[i].localScale;
                    }
                    human.skeleton = bones;
                }

                importer.humanDescription = human;
            }
        }

        public static AnimatorController CreateDefaultAnimator(GameObject fbx, string assetPath)
        {
            string animatorPath = assetPath + "/" + fbx.name + "_default_animator.controller";

            if (File.Exists(animatorPath))
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(animatorPath);
                if (asset.GetType() == typeof(AnimatorController))
                {                    
                    return AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
                }
            }

            string[] folders = new string[] { "Packages" };
            string animatorName = "RL_Default_Animator_Controller";

            string[] guids = AssetDatabase.FindAssets(animatorName, folders);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.iEquals(animatorName))
                {   
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset.GetType() == typeof(AnimatorController))
                    {
                        if (AssetDatabase.CopyAsset(path, animatorPath))
                        {
                            return AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
                        }
                    }
                }
            }

            return null;
        }


        public static AnimatorController AutoCreateAnimator(GameObject fbx, string assetPath, ModelImporter importer)
        {
            string animatorPath = Path.GetDirectoryName(assetPath) + "/" + fbx.name + "_animator.controller";
            
            AnimatorController controller = null;

            if (!File.Exists(animatorPath))
            {
                ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;

                if (clipAnimations.Length != 0)
                {
                    AnimatorController.CreateAnimatorControllerAtPath(animatorPath);
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    AnimationClip TPoseClip = null;
                    AnimationClip previewClip = null;
                    AnimationClip foundClip = null;
                    foreach (UnityEngine.Object obj in assets)
                    {
                        if (obj.GetType() == typeof(AnimationClip))
                        {
                            AnimationClip clip = obj as AnimationClip;
                            clip = AnimRetargetGUI.TryGetRetargetedAnimationClip(fbx, clip);
                            if (clip)
                            {
                                if (!clip.name.iContains("__preview__") && clip.name.iContains("t-pose"))
                                {
                                    TPoseClip = clip;
                                    continue;
                                }

                                if (clip.name.iContains("__preview__"))
                                {
                                    previewClip = clip;
                                    continue;
                                }     
                                
                                controller.AddMotion(clip, 0);
                                foundClip = clip;
                                break;                                
                            }
                        }
                    }

                    if (!foundClip && TPoseClip)
                    {
                        controller.AddMotion(TPoseClip, 0);
                    }
                    else if (!foundClip && previewClip)
                    {
                        controller.AddMotion(previewClip, 0);
                    }

                    if (AssetDatabase.WriteImportSettingsIfDirty(assetPath))
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
            else
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
            }

            return controller;
        }
        
        public static void SetupAnimation(ModelImporter importer, CharacterInfo characterInfo, bool forceUpdate)
        {
            if (importer.defaultClipAnimations.Length > 0)
            {
                if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
                    importer.clipAnimations = importer.defaultClipAnimations;
            }

            ModelImporterClipAnimation[] animations = importer.clipAnimations;
            if (animations == null) return;

            bool changed = false;

            foreach (ModelImporterClipAnimation anim in animations)
            {
                if (!anim.keepOriginalOrientation || !anim.keepOriginalPositionY || !anim.keepOriginalPositionXZ ||
                    !anim.lockRootRotation || !anim.lockRootHeightY)
                {
                    anim.keepOriginalOrientation = true;
                    anim.keepOriginalPositionY = true;
                    anim.keepOriginalPositionXZ = true;
                    anim.lockRootRotation = true;
                    anim.lockRootHeightY = true;
                    changed = true;
                }

                if (anim.name.iContains("idle") && !anim.lockRootPositionXZ)
                {
                    anim.lockRootPositionXZ = true;
                    changed = true;
                }

                if (anim.name.iContains("_loop") && !anim.loopTime)
                {
                    anim.loopTime = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = animations;                
                if (forceUpdate)
                {
                    AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            characterInfo.animationSetup = true;
        }

        public static void ResetFbxAnimator(GameObject fbx)
        {
            Animator animator = fbx.GetComponentInChildren<Animator>();
            if (animator)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    animator.runtimeAnimatorController = null;
                }
            }
        }

        public static void DoAnimationImport(CharacterInfo info)
        {
            string path = info.path;
            ResetFbxAnimator(info.Fbx);
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            HumanoidImportSettings(info.Fbx, importer, info);
            SetupAnimation(importer, info, true);            

            Avatar sourceAvatar = info.GetCharacterAvatar();

            List<string> motionGuids = info.GetMotionGuids();
            if (motionGuids.Count > 0)
            {
                foreach (string guid in motionGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    DoMotionImport(info, sourceAvatar, assetPath);
                }
            }
        }

        public static void DoMotionImport(CharacterInfo info, Avatar sourceAvatar, string motionFbxPath)
        {            
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(motionFbxPath);            
            HumanoidImportSettings(null, importer, info, sourceAvatar);
            SetupAnimation(importer, info, true);            
        }      

        public static void AddDefaultAnimatorController(CharacterInfo info, GameObject prefab)
        {
            string prefabFolder = Util.CreateFolder(info.folder, Importer.PREFABS_FOLDER);
            string prefabPath = Path.Combine(prefabFolder, info.name + ".prefab");
            string prefabBakedPath = Path.Combine(prefabFolder, info.name + Importer.BAKE_SUFFIX + ".prefab");

            AnimatorController defaultController = CreateDefaultAnimator(info.Fbx, info.folder);
            Animator animator = prefab.GetComponent<Animator>();

            if (!animator || !defaultController) return;            
            
            animator.runtimeAnimatorController = defaultController;
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                
            // replace baked prefab animator too
            if (File.Exists(prefabBakedPath))
            {
                GameObject prefabBaked = AssetDatabase.LoadAssetAtPath<GameObject>(prefabBakedPath);
                animator = prefabBaked.GetComponent<Animator>();

                if (animator)
                {
                    animator.runtimeAnimatorController = defaultController;
                    animator.applyRootMotion = true;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
            }
        }

        public static string InitCharacterPrefab(CharacterInfo info)
        {            
            string prefabFolder = Util.CreateFolder(info.folder, Importer.PREFABS_FOLDER);            
            string prefabPath = Path.Combine(prefabFolder, info.name + ".prefab");

#if UNITY_2023_OR_NEWER
            // Unity 2023.1.1 to 2023.1.5 crashes if saving a new instance over an existing prefab, so delete it first
#if UNITY_2023_1_6_OR_NEWER
            // prefab bug fixed in 2023.1.6
#else
            bool assetExists = Util.AssetPathExists(prefabPath);
            if (assetExists)
            {
                AssetDatabase.DeleteAsset(prefabPath);
                AssetDatabase.Refresh();
            }
#endif
#endif            

            // remove any animator controllers set in the fbx
            ResetFbxAnimator(info.Fbx);

            return prefabPath;
        }

        public static GameObject InstantiateModelFromSource(CharacterInfo info, GameObject fbx, string assetPath)
        {
            GameObject prefabInstance = null;

            if (info.path.iContains("_lod") && CountLODs(fbx) > 1)
            {
                prefabInstance = CreateLODInstanceFromModel(info, fbx);
            }
            else
            {
                prefabInstance = CreateInstanceFromModel(info, fbx);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, assetPath, InteractionMode.AutomatedAction);
            return prefabInstance;
        }

        /// <summary>
        ///     Note: no longer deletes the clone. Use SaveAndRemoveScenePrefab() to finalize the prefab.
        /// </summary>
        public static GameObject CreateInstanceFromModel(CharacterInfo info, GameObject modelSource)
        {                        
            return PrefabUtility.InstantiatePrefab(modelSource) as GameObject;
        }

        public static GameObject CreateLODInstanceFromModel(CharacterInfo info, GameObject modelSource)
        {                        
            Renderer[] renderers = modelSource.transform.GetComponentsInChildren<Renderer>(true);
            int lodLevels = 0;
            foreach (Renderer child in renderers)
            {
                if (child.name.Contains("_LOD"))
                {
                    string level = child.name.Substring((child.name.Length - 1), 1);
                    lodLevels = Math.Max(lodLevels, int.Parse(level));
                }
            }

            bool originalCharacter = renderers.Length != lodLevels;

            lodLevels += 1;
            LOD[] lods = new LOD[lodLevels];
            GameObject sceneLODInstance = PrefabUtility.InstantiatePrefab(modelSource) as GameObject;
            LODGroup lodGroup = sceneLODInstance.AddComponent<LODGroup>();            
            Renderer[] prefabRenderers = sceneLODInstance.transform.GetComponentsInChildren<Renderer>(true);                

            if (originalCharacter)
            {
                List<Renderer> renderersListLOD0 = new List<Renderer>();
                for (int i = 0; i < prefabRenderers.Length; i++) // Process LOD0
                {
                    if (!prefabRenderers[i].name.Contains("_LOD"))
                    {
                        renderersListLOD0.Add(prefabRenderers[i]);
                    }
                }
                Renderer[] renderersLOD0 = renderersListLOD0.ToArray();
                lods[0] = new LOD((1.0F / (2)), renderersLOD0);
            }

            for (int i = 1; i < lodLevels; i++) // Does not process LOD0
            {
                string LODLevel = "_LOD" + i;
                for (int j = 0; j < prefabRenderers.Length; j++)
                {
                    if (prefabRenderers[j].name.EndsWith(LODLevel))
                    {
                        Renderer[] rendererLOD = new Renderer[1];
                        rendererLOD[0] = prefabRenderers[j];
                        lods[i] = new LOD(1.0F / (i + 2), rendererLOD);
                    }

                    if (i == lodLevels - 1)
                    {
                        lods[i].screenRelativeTransitionHeight = 0.02f;
                    }
                }
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            return sceneLODInstance;
        }

        public static GameObject SaveAndRemovePrefabInstance(GameObject prefabInstance, string assetPath)
        {
            //GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
            UnityEngine.Object.DestroyImmediate(prefabInstance);
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        public static int CountLODs(GameObject fbx)
        {
            List<int> levels = new List<int>(5);
            Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                int index = r.name.LastIndexOf("_LOD");                
                if (index >= 0 && r.name.Length == index + 5 && char.IsDigit(r.name[r.name.Length - 1]))
                {
                    // any mesh with a _LOD<N> suffix is a LOD level
                    string levelString = r.name.Substring(r.name.Length - 1, 1);
                    if (int.TryParse(levelString, out int level))
                    {
                        if (!levels.Contains(level)) levels.Add(level);
                    }
                }
                else
                {
                    // assume any mesh without a _LOD<N> suffix is the original model (LOD0)
                    int level = 0;
                    if (!levels.Contains(level)) levels.Add(level);
                }
            }
            return levels.Count;
        }
        
        public static bool IsBodyMesh(SkinnedMeshRenderer smr)
        {
            string meshName = smr.gameObject.name;

            if (meshName.iEquals("CC_Base_Body")) return true;
            if (meshName.iEquals("CC_Game_Body")) return true;

            foreach (Material mat in smr.sharedMaterials)
            {
                if (!mat) continue;

                if (mat.name.iContains("Std_Skin_")) return true;
                if (mat.shader.name.iContains(Pipeline.SHADER_HQ_HEAD) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_SKIN)) return true;
            }

            return false;
        }

        public static bool IsHairMesh(SkinnedMeshRenderer smr)
        {
            string meshName = smr.gameObject.name;

            foreach (Material mat in smr.sharedMaterials)
            {
                if (!mat) continue;

                if (mat.name.iContains("Hair") && mat.name.iContains("Transparency")) return true;
                if (mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR)) return true;
            }

            return false;
        }

        public static Material GetActorCoreSingleMaterial(GameObject fbx)
        {
            if (fbx)
            {                
                Material actorCoreMaterial = null;
                Transform[] transforms = fbx.GetComponentsInChildren<Transform>();
                foreach (Transform t in transforms)
                {
                    Renderer r = t.gameObject.GetComponent<Renderer>();
                    if (r)
                    {
                        if (r.sharedMaterials.Length == 1)
                        {
                            if (actorCoreMaterial && actorCoreMaterial != r.sharedMaterials[0])
                                return null;

                            actorCoreMaterial = r.sharedMaterials[0];
                        }
                        else
                        {
                            return null;
                        }
                    }

                }

                return actorCoreMaterial;
            }

            return null;
        }

        public static Material GetActorBuildSingleMaterial(GameObject fbx)
        {
            if (fbx)
            {
                bool singleMaterial = true;
                Material actorBuildMaterial = null;
                Transform[] transforms = fbx.GetComponentsInChildren<Transform>();
                foreach (Transform t in transforms)
                {
                    switch (t.name)
                    {
                        // for a single material actorbuild these should all have the same material
                        case "CC_Game_Body":
                        case "CC_Game_Tongue":
                        case "CC_Base_Eye":
                        case "CC_Base_Teeth":
                        case "CC_Base_Tongue":
                        case "CC_Base_Body":
                            Renderer r = t.gameObject.GetComponent<Renderer>();
                            if (r)
                            {
                                if (r.sharedMaterials.Length == 1)
                                {
                                    if (actorBuildMaterial && actorBuildMaterial != r.sharedMaterials[0])
                                        singleMaterial = false;

                                    actorBuildMaterial = r.sharedMaterials[0];
                                }
                                else
                                {
                                    singleMaterial = false;
                                }
                            }
                            break;
                    }
                }

                if (singleMaterial) return actorBuildMaterial;
            }

            return null;
        }
    }
}