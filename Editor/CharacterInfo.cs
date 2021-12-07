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
        
        public string guid;
        public string path;        
        public string infoPath;
        public string jsonPath;
        public string name;
        public string folder;                
        public ProcessingType logType = ProcessingType.None;
        public bool isLOD = false;
        public bool bakeIsBaked = false;

        public bool qualRefractiveEyes = false;
        public bool dualMaterialHair = false;        
        public bool bakeCustomShaders = true;
        public bool bakeSeparatePrefab = true;        
        
        private bool _qualRefractiveEyes = true;
        private bool _dualMaterialHair = false;
        private bool _bakeCustomShaders = true;
        private bool _bakeSeparatePrefab = true;        

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
            _qualRefractiveEyes = qualRefractiveEyes;
            _dualMaterialHair = dualMaterialHair;
            _bakeCustomShaders = bakeCustomShaders;
            _bakeSeparatePrefab = bakeSeparatePrefab;
        }

        public bool IsBuiltDualHair
        {
            get { return _dualMaterialHair; }
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
                    case "qualRefractiveEyes":
                        _qualRefractiveEyes = value == "true" ? true : false;
                        qualRefractiveEyes = _qualRefractiveEyes;
                        break;
                    case "dualMaterialHair":
                        _dualMaterialHair = value == "true" ? true : false;
                        dualMaterialHair = _dualMaterialHair;
                        break;
                    case "bakeIsBaked":
                        bakeIsBaked = value == "true" ? true : false;                        
                        break;
                    case "bakeCustomShaders":
                        _bakeCustomShaders = value == "true" ? true : false;
                        bakeCustomShaders = _bakeCustomShaders;
                        break;
                    case "bakeSeparatePrefab":
                        _bakeSeparatePrefab = value == "true" ? true : false;
                        bakeSeparatePrefab = _bakeSeparatePrefab;
                        break;
                    case "generation":
                        generation = (BaseGeneration)System.Enum.Parse(typeof(BaseGeneration), value);
                        break;
                    case "isLOD":
                        isLOD = value == "true" ? true : false;
                        break;
                }
            }
        }

        public void Write()
        {
            ApplySettings();
            StreamWriter writer = new StreamWriter(infoPath, false);
            writer.WriteLine("logType=" + logType.ToString());
            writer.WriteLine("generation=" + generation.ToString());
            writer.WriteLine("isLOD=" + (isLOD ? "true" : "false"));
            writer.WriteLine("qualRefractiveEyes=" + (_qualRefractiveEyes ? "true" : "false"));
            writer.WriteLine("dualMaterialHair=" + (_dualMaterialHair ? "true" : "false"));
            writer.WriteLine("bakeIsBaked=" + (bakeIsBaked ? "true" : "false"));
            writer.WriteLine("bakeCustomShaders=" + (_bakeCustomShaders ? "true" : "false"));
            writer.WriteLine("bakeSeparatePrefab=" + (_bakeSeparatePrefab ? "true" : "false"));
            writer.Close();
            AssetDatabase.ImportAsset(infoPath);            
        }
    }

}
