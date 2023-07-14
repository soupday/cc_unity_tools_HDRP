/* 
 * Copyright (C) 2022 Victor Soupday
 * This file is part of CC_Unity_Tools <https://github.com/soupday/cc_unity_tools_HDRP>
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

/*
 * Simple Utility to close the jaw of animations imported into Unity
 * Usage:  Right-click on any imported model file
 * select 'Quick Animation Processing/Process Jaw Animations'  
 * This will then find any humanoid animations in the model file
 * and create a duplicate animations in the same folder
 * which have a closed jaw. Should any jaw data be already present
 * then use the 'Process Jaw Animations (Force)' option to 
 * overwrite the existing jaw data with a closed jaw.
 * 
 * NB: This script must be placed in a folder called 'Editor'
 * or in a subfolder of 'Editor' e.g. Editor/Tools/<this script>.cs
 * 'Editor' can be anywhere in the project.
 */


using Reallusion.Import;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class QuickAnimProcess : Editor
{
    [MenuItem("Assets/Quick Animation Processing/Process Jaw Animations", priority = 2001)]
    public static void InitAssetProcessing()
    {
        ProcessModel(Selection.activeObject, false);
    }

    [MenuItem("Assets/Quick Animation Processing/Process Jaw Animations", true)]
    public static bool ValidateInitAssetProcessing()
    {
        return IsModel(Selection.activeObject);         
    }

    [MenuItem("Assets/Quick Animation Processing/Process Jaw Animations (Force)", priority = 2002)]
    public static void InitAssetProcessingForce()
    {
        ProcessModel(Selection.activeObject, true);
    }

    [MenuItem("Assets/Quick Animation Processing/Process Jaw Animations (Force)", true)]
    public static bool ValidateInitAssetProcessingForce()
    {
        return IsModel(Selection.activeObject);
    }

    [MenuItem("Reallusion/Animation Tools/Process Jaw Animations", priority = 300)]
    public static void ATInitAssetProcessing()
    {
        ProcessModel(Selection.activeObject, false);
    }

    [MenuItem("Reallusion/Animation Tools/Process Jaw Animations", true)]
    public static bool ATValidateInitAssetProcessing()
    {
        return IsModel(Selection.activeObject);
    }

    [MenuItem("Reallusion/Animation Tools/Process Jaw Animations (Force)", priority = 301)]
    public static void ATInitAssetProcessingForce()
    {
        ProcessModel(Selection.activeObject, true);
    }

    [MenuItem("Reallusion/Animation Tools/Process Jaw Animations (Force)", true)]
    public static bool ATValidateInitAssetProcessingForce()
    {
        return IsModel(Selection.activeObject);
    }

    private static string[] modelFileExtensions = new string[] { ".fbx", ".blend", ".dae", ".obj" };

    public static bool IsModel(Object o)
    {
        string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
        if (string.IsNullOrEmpty(assetPath)) return false;
        
        string extension = Path.GetExtension(assetPath);
        foreach (string ext in modelFileExtensions)
        {
            if (extension.Equals(ext, System.StringComparison.InvariantCultureIgnoreCase))
            {
                //only check against file extension on the right-click menu
                return true;
            }
        }
        return false;
    }

    public static bool IsHumanoidModel(Object o)
    {        
        string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
        string extension = Path.GetExtension(assetPath);

        foreach (string ext in modelFileExtensions)
        {
            if (extension.Equals(ext, System.StringComparison.InvariantCultureIgnoreCase))
            {
                //open the model importer here rather than the right-click menu
                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(assetPath);
                return (importer.animationType == ModelImporterAnimationType.Human);
            }
        }        
        return false;
    }

    public static void ProcessModel(Object o, bool force)
    {
        if (!IsHumanoidModel(o)) 
        {
            Debug.LogWarning(o.name + " is not a humanoid model. Please change the Rig type to 'Humanoid' in the model importer.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
        Object[] data = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                
        foreach (Object subObject in data)
        {
            if (subObject.GetType().Equals(typeof(AnimationClip)))
            {
                // Make a working copy of source clip. NB will be appended with (Clone)
                Object workingClip = Object.Instantiate(subObject);
                bool process = true;

                if (!force)
                {
                    if (!IsJawOpen(workingClip))
                    process = false;
                    Debug.LogWarning("Jaw Curve data found in: " + subObject.name + ". Use the force option to overwrite data.");
                }

                if (process)                
                    WriteAnimationClip(o, ProcessAnimation(workingClip));                             
            }
        }
    }

    private static bool IsJawOpen(Object clipObject)
    {
        if (clipObject.GetType().Equals(typeof(AnimationClip)))
        {
            AnimationClip animationClip = clipObject as AnimationClip;
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);

            //find 'jaw close' curve
            foreach (EditorCurveBinding binding in curveBindings)
            {
                if (binding.propertyName.Equals("Jaw Close"))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, binding);
                    Keyframe[] targetKeys = curve.keys;
                    float targetChecksum = 0;
                    for (int i = 0; i < targetKeys.Length; i++)
                    {
                        targetChecksum += targetKeys[i].value;
                    }

                    if (targetChecksum < 0.01f) //curve has no data - will need correcting
                        return true;
                    else //curve has data - do not correct
                        return false;
                }
            }
            //no curve present - will need correcting
            return true;
        }
        return false;
    }

    private static AnimationClip ProcessAnimation(Object clipObject)
    {
        AnimationClip animationClip = clipObject as AnimationClip;
        EditorCurveBinding targetBinding = new EditorCurveBinding();
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);

        foreach (EditorCurveBinding binding in curveBindings)
        {
            if (binding.propertyName.Equals("Jaw Close"))
            {
                targetBinding = binding;
            }
        }

        AnimationCurve jawCurve = AnimationUtility.GetEditorCurve(animationClip, targetBinding);
        Keyframe[] jawKeys = jawCurve.keys;
        for (int i = 0; i < jawKeys.Length; i++)
        {
            jawKeys[i].value = 1;
        }
        jawCurve.keys = jawKeys;

        AnimationUtility.SetEditorCurve(animationClip, targetBinding, jawCurve);

        return animationClip;
    }

    private static void WriteAnimationClip(Object o, AnimationClip animationClip)
    {
        string assetPath = AssetDatabase.GetAssetPath(o).ToLower();
        string workingDirectory = Path.GetDirectoryName(assetPath);

        string animName = SanitizeName(o.name + " - " + animationClip.name);
        string fullOutputPath = workingDirectory + "/" + animName + ".anim";

        
        if (AssetPathIsEmpty(fullOutputPath))
        {
            for (int i = 0; i < 999; i++)
            {
                string extension = string.Format("{0:000}", i);
                fullOutputPath = workingDirectory + "/" + animName + "." + extension + ".anim";
                if (AssetPathIsEmpty(fullOutputPath)) break;
            }
        }
        Util.LogInfo("Writing Asset: " + fullOutputPath);
        AssetDatabase.CreateAsset(animationClip, fullOutputPath);
    }

    private static string SanitizeName(string inputName)
    { 
        inputName = inputName.Replace("(Clone)", "");
        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalid)));
        return r.Replace(inputName, " - ");
    }

    public static bool AssetPathIsEmpty(string assetPath)
    {
        const string emptyGuid = "00000000000000000000000000000000";

        return AssetDatabase.AssetPathToGUID(assetPath).Equals(emptyGuid);
    }
}
