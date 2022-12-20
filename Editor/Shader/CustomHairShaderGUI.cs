using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;
using System.IO;

namespace Reallusion.Import
{
    public class CustomHairShaderGUI : ShaderGUI
    {
        private Material[] pass1 = null;
        private Material[] pass2 = null;
        private Material[] sources = null;

        private void CheckMaterials(Object[] targets)
        {            
            bool rebuild = false;

            if (pass1 == null || pass1.Length != targets.Length) 
            { 
                pass1 = new Material[targets.Length]; 
                rebuild = true; 
            }

            if (pass2 == null || pass2.Length != targets.Length)
            { 
                pass2 = new Material[targets.Length]; 
                rebuild = true; 
            }

            if (sources == null || sources.Length != targets.Length)
            {
                sources = new Material[targets.Length];
                rebuild = true;
            }

            if (rebuild)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Material target = targets[i] as Material;
                    
                    string path = AssetDatabase.GetAssetPath(target);
                    string folder = Path.GetDirectoryName(path);
                    string name = Path.GetFileNameWithoutExtension(path);
                    string pass1Path = Path.Combine(folder, name + "_1st_Pass.mat");
                    string pass2Path = Path.Combine(folder, name + "_2nd_Pass.mat");
                    if (File.Exists(pass1Path)) pass1[i] = AssetDatabase.LoadAssetAtPath<Material>(pass1Path);
                    if (File.Exists(pass2Path)) pass2[i] = AssetDatabase.LoadAssetAtPath<Material>(pass2Path);
                    sources[i] = target;
                }
            }
        }

        override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();

            // render the shader properties using the default GUI
            base.OnGUI(materialEditor, properties);

            Material targetMat = materialEditor.target as Material;

            CheckMaterials(materialEditor.targets);

            if (EditorGUI.EndChangeCheck())
            {                
                CopyMaterialProps(targetMat);
            }
        }

        private bool SetFloatIfSourcesAgree(Material from, string prop)
        {
            float value = from.GetFloat(prop);            

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].GetFloat(prop) != value) return false;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (pass1[i]) pass1[i].SetFloatIf(prop, value);
                if (pass2[i]) pass2[i].SetFloatIf(prop, value);                    
            }

            if (prop == "BOOLEAN_ENABLECOLOR")
            {                
                for (int i = 0; i < sources.Length; i++)
                {
                    if (value == 1f)
                    {
                        if (pass1[i]) pass1[i].EnableKeyword("BOOLEAN_ENABLECOLOR_ON");
                        if (pass2[i]) pass2[i].EnableKeyword("BOOLEAN_ENABLECOLOR_ON");                        
                    }
                    else
                    {
                        if (pass1[i]) pass1[i].DisableKeyword("BOOLEAN_ENABLECOLOR_ON");
                        if (pass2[i]) pass2[i].DisableKeyword("BOOLEAN_ENABLECOLOR_ON");
                    }
                }
            }

            return true;
        }

        private bool SetColorIfSourcesAgree(Material from, string prop)
        {
            Color value = from.GetColor(prop);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].GetColor(prop) != value) return false;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (pass1[i]) pass1[i].SetColorIf(prop, value);
                if (pass2[i]) pass2[i].SetColorIf(prop, value);
            }

            return true;
        }

        private bool SetVectorIfSourcesAgree(Material from, string prop)
        {
            Vector4 value = from.GetVector(prop);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].GetVector(prop) != value) return false;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (pass1[i]) pass1[i].SetVectorIf(prop, value);
                if (pass2[i]) pass2[i].SetVectorIf(prop, value);
            }

            return true;
        }

        private bool SetTexureIfSourcesAgree(Material from, string prop)
        {
            Texture value = from.GetTexture(prop);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].GetTexture(prop) != value) return false;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                if (pass1[i] && pass1[i].GetTexture(prop) != value)
                    pass1[i].SetTextureIf(prop, value);
                if (pass2[i] && pass2[i].GetTexture(prop) != value)
                    pass2[i].SetTextureIf(prop, value);
            }

            return true;
        }

        private void CopyMaterialProps(Material from)
        {
            int props = from.shader.GetPropertyCount();
            for (int i = 0; i < props; i++)
            {
                string prop = from.shader.GetPropertyName(i);
                int flagValue = (int)from.shader.GetPropertyFlags(i);
                int checkBit = 0x00000001; //bit for UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector
                int flagHasBit = (flagValue & checkBit);
                ShaderPropertyType type = from.shader.GetPropertyType(i);

                if ((flagValue & checkBit) == 0)
                {                    
                    switch (type)
                    {
                        case ShaderPropertyType.Texture:
                            SetTexureIfSourcesAgree(from, prop);
                            break;

                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            SetFloatIfSourcesAgree(from, prop);
                            break;

                        case ShaderPropertyType.Color:
                            SetColorIfSourcesAgree(from, prop);
                            break;

                        case ShaderPropertyType.Vector:
                            SetVectorIfSourcesAgree(from, prop);
                            break;
                    }        
                }
            }
        }
    }
}