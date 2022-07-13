/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC3_Unity_Tools <https://github.com/soupday/cc3_unity_tools>
 * 
 * CC3_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC3_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC3_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reallusion.Import
{
    public class CharacterInfo
    {
        public enum ProcessingType { None, Basic, HighQuality }
        public enum EyeQuality { None, Basic, Parallax, Refractive }
        public enum HairQuality { None, Default, TwoPass, Coverage }
        public enum ShaderFeatureFlags { NoFeatures = 0, Tessellation = 1, ClothPhysics = 2, HairPhysics = 4, SpringBones = 8 } //, Tessellation = ~0 }

        public string guid;
        public string path;        
        public string infoPath;
        public string jsonPath;
        public string name;
        public string folder;                
          
        public bool isLOD = false;
        public bool bakeIsBaked = false;
        public bool animationSetup = false;
        public int animationRetargeted = 0;

        // these are the settings the character is currently set to build
        private ProcessingType logType = ProcessingType.None;
        private EyeQuality qualEyes = EyeQuality.Parallax;
        private HairQuality qualHair = HairQuality.TwoPass;
        private bool bakeCustomShaders = true;
        private bool bakeSeparatePrefab = true;
        private bool useTessellation = false;        

        public ProcessingType BuildType { get { return logType; } set { logType = value; } }
        public MaterialQuality BuildQuality
        {
            get
            {                
                if (BuildType == ProcessingType.HighQuality) return MaterialQuality.High;
                else if (BuildType == ProcessingType.Basic) return MaterialQuality.Default;
                return MaterialQuality.None;
            }
            set
            {
                if (value == MaterialQuality.High) BuildType = ProcessingType.HighQuality;
                else if (value == MaterialQuality.Default) BuildType = ProcessingType.Basic;
                else BuildType = ProcessingType.None;
            }
        }

        public ShaderFeatureFlags ShaderFlags { get; set; } = ShaderFeatureFlags.NoFeatures;
        public bool FeatureUseTessellation => (ShaderFlags & ShaderFeatureFlags.Tessellation) > 0;
        public bool FeatureUseClothPhysics => (ShaderFlags & ShaderFeatureFlags.ClothPhysics) > 0;
        public bool FeatureUseHairPhysics => (ShaderFlags & ShaderFeatureFlags.HairPhysics) > 0;
        public bool FeatureUseSpringBones => (ShaderFlags & ShaderFeatureFlags.SpringBones) > 0;
        public bool BasicMaterials => logType == ProcessingType.Basic;
        public bool HQMaterials => logType == ProcessingType.HighQuality;
        public EyeQuality QualEyes { get { return qualEyes; } set { qualEyes = value; } }
        public HairQuality QualHair { get { return qualHair; } set { qualHair = value; } }
        public bool RefractiveEyes => QualEyes == EyeQuality.Refractive;
        public bool BasicEyes => QualEyes == EyeQuality.Basic;
        public bool ParallaxEyes => QualEyes == EyeQuality.Parallax;
        public bool DualMaterialHair { get { return qualHair == HairQuality.TwoPass; } }
        public bool CoverageHair { get { return qualHair == HairQuality.Coverage; } }
        public bool DefaultHair { get { return qualHair == HairQuality.Default; } }
        public bool BakeCustomShaders { get { return bakeCustomShaders; } set { bakeCustomShaders = value; } }
        public bool BakeSeparatePrefab { get { return bakeSeparatePrefab; } set { bakeSeparatePrefab = value; } }        

        // these are the settings the character has been built to.  
        private ProcessingType builtLogType = ProcessingType.None;
        private EyeQuality builtQualEyes = EyeQuality.Parallax;
        private HairQuality builtQualHair = HairQuality.TwoPass;
        private bool builtBakeCustomShaders = true;
        private bool builtBakeSeparatePrefab = true;
        private bool builtTessellation = false;

        public ShaderFeatureFlags BuiltShaderFlags { get; private set; } = ShaderFeatureFlags.NoFeatures;
        public bool BuiltFeatureTessellation => (BuiltShaderFlags & ShaderFeatureFlags.Tessellation) > 0;
        public bool BuiltBasicMaterials => builtLogType == ProcessingType.Basic;
        public bool BuiltHQMaterials => builtLogType == ProcessingType.HighQuality;
        public bool BuiltDualMaterialHair => builtQualHair == HairQuality.TwoPass;
        public bool BuiltCoverageHair => builtQualHair == HairQuality.Coverage;
        public bool BuiltDefaultHair => builtQualHair == HairQuality.Default;
        public EyeQuality BuiltQualEyes => builtQualEyes;
        public HairQuality BuiltQualHair => builtQualHair;
        public bool BuiltRefractiveEyes => BuiltQualEyes == EyeQuality.Refractive;
        public bool BuiltBasicEyes => BuiltQualEyes == EyeQuality.Basic;
        public bool BuiltParallaxEyes => BuiltQualEyes == EyeQuality.Parallax;        

        public MaterialQuality BuiltQuality => BuiltHQMaterials ? MaterialQuality.High : MaterialQuality.Default;
        public bool Unprocessed => builtLogType == ProcessingType.None;

        private BaseGeneration generation = BaseGeneration.None;
        private GameObject fbx;
        private QuickJSON jsonData;

        private void FixCharSettings()
        {
            if (logType == ProcessingType.HighQuality && !CanHaveHighQualityMaterials)
                logType = ProcessingType.Basic;

            if (qualEyes == EyeQuality.Refractive && !Pipeline.isHDRP) 
                qualEyes = EyeQuality.Parallax;

            if (qualHair == HairQuality.Coverage && Pipeline.isHDRP)
                qualHair = HairQuality.Default;

            if (!Pipeline.isHDRP && (ShaderFlags & ShaderFeatureFlags.Tessellation) > 0)
            {
                ShaderFlags = ShaderFlags & (~ShaderFeatureFlags.Tessellation);
            }
        }

        public CharacterInfo(string guid)
        {
            this.guid = guid;
            path = AssetDatabase.GUIDToAssetPath(this.guid);
            name = Path.GetFileNameWithoutExtension(path);
            folder = Path.GetDirectoryName(path);            
            infoPath = Path.Combine(folder, name + "_ImportInfo.txt");
            jsonPath = Path.Combine(folder, name + ".json");
            if (path.iContains("_lod")) isLOD = true;

            if (File.Exists(infoPath))            
                Read();
            else
                Write();            
        }

        public void ApplySettings()
        {            
            FixCharSettings();

            builtLogType = logType;
            builtQualEyes = qualEyes;
            builtQualHair = qualHair;
            builtBakeCustomShaders = bakeCustomShaders;
            builtBakeSeparatePrefab = bakeSeparatePrefab;
            builtTessellation = useTessellation;
            BuiltShaderFlags = ShaderFlags;
        }        

        public GameObject Fbx
        {
            get
            {
                if (fbx == null)
                {
                    fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Util.LogInfo("CharInfo: " + name + " FBX Loaded");
                }
                return fbx;
            }
        }

        public QuickJSON JsonData
        { 
            get
            {
                if (jsonData == null)
                {
                    jsonData = Util.GetJsonData(jsonPath);
                    Util.LogInfo("CharInfo: " + name + " JsonData Fetched");
                }
                return jsonData;
            }
        }

        private FacialProfile facialProfile;
        public FacialProfile FaceProfile
        {
            get
            {
                if (facialProfile.expressionProfile == ExpressionProfile.None && facialProfile.visemeProfile == VisemeProfile.None)
                {
                    facialProfile = FacialProfileMapper.GetMeshFacialProfile(Fbx);
                }
                return facialProfile;
            }
        }

        public void Refresh()
        {
            if (jsonData != null) jsonData = Util.GetJsonData(jsonPath);
        }
        
        public BaseGeneration Generation
        { 
            get 
            { 
                if (generation == BaseGeneration.None)
                {
                    string gen = Util.GetJsonGenerationString(jsonPath);                    
                    generation = RL.GetCharacterGeneration(Fbx, gen);
                    Util.LogInfo("CharInfo: " + name + " Generation " + generation.ToString());
                    Write();
                }

                return generation;
            } 
        }            

        public void Release()
        {
            jsonData = null;
            fbx = null;
            Util.LogInfo("CharInfo: " + name + " Data Released!");
        }

        public bool CanHaveHighQualityMaterials
        {
            get
            {
                switch (Generation)
                {
                    case BaseGeneration.G1:
                    case BaseGeneration.G3:
                    case BaseGeneration.G3Plus:
                    case BaseGeneration.GameBase:
                        return true;
                    default:
                        return false;
                }
            }
        }


        public void Read()
        {
            TextAsset infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(infoPath);

            string[] lineEndings = new string[] { "\r\n", "\r", "\n" };
            char[] propertySplit = new char[] { '=' };
            string[] lines = infoAsset.text.Split(lineEndings, System.StringSplitOptions.None);
            string property = "";
            string value = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(propertySplit, System.StringSplitOptions.None);
                if (line.Length > 0)
                    property = line[0];
                if (line.Length > 1)
                    value = line[1];

                switch (property)
                {
                    case "logType":
                        if (value == "Basic") logType = ProcessingType.Basic;
                        else if (value == "HighQuality") logType = ProcessingType.HighQuality;
                        else logType = ProcessingType.None;
                        break;
                    case "qualEyes":
                        if (value == "Basic") qualEyes = EyeQuality.Basic;
                        else if (value == "Parallax") qualEyes = EyeQuality.Parallax;
                        else if (value == "Refractive") qualEyes = EyeQuality.Refractive;
                        else qualEyes = EyeQuality.None;                        
                        break;
                    case "qualHair":
                        if (value == "Default") qualHair = HairQuality.Default;
                        else if (value == "TwoPass") qualHair = HairQuality.TwoPass;
                        else if (value == "Coverage") qualHair = HairQuality.Coverage;
                        else qualHair = HairQuality.None;
                        break;
                    case "dualMaterialHair":
                        if (qualHair == HairQuality.None)
                            qualHair = value == "true" ? HairQuality.TwoPass : HairQuality.Default;
                        break;
                    case "bakeIsBaked":
                        bakeIsBaked = value == "true" ? true : false;                        
                        break;
                    case "bakeCustomShaders":
                        bakeCustomShaders = value == "true" ? true : false;                        
                        break;
                    case "bakeSeparatePrefab":
                        bakeSeparatePrefab = value == "true" ? true : false;                        
                        break;
                    case "generation":
                        generation = (BaseGeneration)System.Enum.Parse(typeof(BaseGeneration), value);
                        break;
                    case "isLOD":
                        isLOD = value == "true" ? true : false;
                        break;
                    case "shaderFlags":
                        ShaderFlags = (ShaderFeatureFlags)int.Parse(value);
                        break;
                    case "animationSetup":
                        animationSetup = value == "true" ? true : false;
                        break;
                    case "animationRetargeted":
                        animationRetargeted = int.Parse(value);
                        break;
                }
            }
            ApplySettings();
        }

        public void Write()
        {
            ApplySettings();
            StreamWriter writer = new StreamWriter(infoPath, false);
            writer.WriteLine("logType=" + builtLogType.ToString());
            writer.WriteLine("generation=" + generation.ToString());
            writer.WriteLine("isLOD=" + (isLOD ? "true" : "false"));
            writer.WriteLine("qualEyes=" + builtQualEyes.ToString());
            writer.WriteLine("qualHair=" + builtQualHair.ToString());
            writer.WriteLine("bakeIsBaked=" + (bakeIsBaked ? "true" : "false"));
            writer.WriteLine("bakeCustomShaders=" + (builtBakeCustomShaders ? "true" : "false"));
            writer.WriteLine("bakeSeparatePrefab=" + (builtBakeSeparatePrefab ? "true" : "false"));
            writer.WriteLine("shaderFlags=" + (int)BuiltShaderFlags);
            writer.WriteLine("animationSetup=" + (animationSetup ? "true" : "false"));
            writer.WriteLine("animationRetargeted=" + animationRetargeted.ToString());
            writer.Close();
            AssetDatabase.ImportAsset(infoPath);            
        }
    }

}
