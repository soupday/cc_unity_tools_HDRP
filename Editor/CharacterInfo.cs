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
        
        public string guid;
        public string path;        
        public string infoPath;
        public string jsonPath;
        public string name;
        public string folder;                
          
        public bool isLOD = false;
        public bool bakeIsBaked = false;

        // these are the settings the character is currently set to build
        private ProcessingType logType = ProcessingType.None;
        private EyeQuality qualEyes = EyeQuality.Parallax;        
        private bool dualMaterialHair = false;
        private bool bakeCustomShaders = true;
        private bool bakeSeparatePrefab = true;

        public bool BasicMaterials 
        { 
            get 
            {
                if (!CanHaveHighQualityMaterials) return true;
                else return logType == ProcessingType.Basic;
            } 
            set 
            {
                if (!CanHaveHighQualityMaterials) logType = ProcessingType.Basic;
                else logType = value ? ProcessingType.Basic : ProcessingType.HighQuality;
            } 
        }
        public bool HQMaterials { get { return !BasicMaterials; } set { BasicMaterials = !value; } }
        public EyeQuality QualEyes 
        { 
            get 
            { 
                if (!Pipeline.isHDRP && qualEyes == EyeQuality.Refractive) qualEyes = EyeQuality.Parallax; 
                return qualEyes; 
            } 
            set 
            {
                qualEyes = value; 
            } 
        }        
        public bool RefractiveEyes => QualEyes == EyeQuality.Refractive;
        public bool BasicEyes => QualEyes == EyeQuality.Basic;
        public bool ParallaxEyes => QualEyes == EyeQuality.Parallax;
        public bool DualMaterialHair { get { return dualMaterialHair; } set { dualMaterialHair = value; } }
        public bool BakeCustomShaders { get { return bakeCustomShaders; } set { bakeCustomShaders = value; } }
        public bool BakeSeparatePrefab { get { return bakeSeparatePrefab; } set { bakeSeparatePrefab = value; } }

        public MaterialQuality BuildQuality => HQMaterials ? MaterialQuality.High : MaterialQuality.Default;

        // these are the settings the character has been built to.  
        private ProcessingType builtLogType = ProcessingType.None;
        private EyeQuality builtQualEyes = EyeQuality.Parallax;        
        private bool builtDualMaterialHair = false;
        private bool builtBakeCustomShaders = true;
        private bool builtBakeSeparatePrefab = true;

        public bool BuiltBasicMaterials => builtLogType == ProcessingType.Basic;
        public bool BuiltHQMaterials => builtLogType == ProcessingType.HighQuality;
        public bool BuiltDualMaterialHair => builtDualMaterialHair;
        public EyeQuality BuiltQualEyes => builtQualEyes;
        public bool BuiltRefractiveEyes => BuiltQualEyes == EyeQuality.Refractive;
        public bool BuiltBasicEyes => BuiltQualEyes == EyeQuality.Basic;
        public bool BuiltParallaxEyes => BuiltQualEyes == EyeQuality.Parallax;

        public MaterialQuality BuiltQuality => BuiltHQMaterials ? MaterialQuality.High : MaterialQuality.Default;
        public bool Unprocessed => builtLogType == ProcessingType.None;

        private BaseGeneration generation = BaseGeneration.None;
        private GameObject fbx;
        private QuickJSON jsonData;

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
            if (qualEyes == EyeQuality.Refractive && !Pipeline.isHDRP) qualEyes = EyeQuality.Parallax;
            if (!CanHaveHighQualityMaterials && logType == ProcessingType.HighQuality) logType = ProcessingType.Basic;

            builtLogType = logType;
            builtQualEyes = qualEyes;
            builtDualMaterialHair = dualMaterialHair;
            builtBakeCustomShaders = bakeCustomShaders;
            builtBakeSeparatePrefab = bakeSeparatePrefab;
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
                    case "dualMaterialHair":
                        dualMaterialHair = value == "true" ? true : false;                        
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
            writer.WriteLine("dualMaterialHair=" + (builtDualMaterialHair ? "true" : "false"));            
            writer.WriteLine("bakeIsBaked=" + (bakeIsBaked ? "true" : "false"));
            writer.WriteLine("bakeCustomShaders=" + (builtBakeCustomShaders ? "true" : "false"));
            writer.WriteLine("bakeSeparatePrefab=" + (builtBakeSeparatePrefab ? "true" : "false"));
            writer.Close();
            AssetDatabase.ImportAsset(infoPath);            
        }
    }

}
