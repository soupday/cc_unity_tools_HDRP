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

        public GameObject fbx;
        public string guid;
        public string path;        
        public string infoPath;
        public string name;
        public string folder;        
        public TextAsset infoAsset;
        public ProcessingType logType = ProcessingType.None;
        public bool qualRefractiveEyes = true;
        public bool bakeIsBaked = false;
        public bool bakeCustomShaders = true;
        private QuickJSON jsonData;
        private BaseGeneration generation;

        public CharacterInfo(GameObject obj)
        {
            path = AssetDatabase.GetAssetPath(obj);
            fbx = obj;
            name = Path.GetFileNameWithoutExtension(path);
            folder = Path.GetDirectoryName(path);
            infoPath = Path.Combine(folder, name + "_ImportInfo.txt");
            infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(infoPath);
            if (infoAsset)
                Read();
            else
                Write();
        }

        public CharacterInfo(string guid)
        {
            this.guid = guid;
            path = AssetDatabase.GUIDToAssetPath(this.guid);
            fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            name = Path.GetFileNameWithoutExtension(path);
            folder = Path.GetDirectoryName(path);            
            infoPath = Path.Combine(folder, name + "_ImportInfo.txt");
            infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(infoPath);
            if (infoAsset)
                Read();
            else
                Write();
        }        

        public QuickJSON JsonData
        {
            get
            {
                if (jsonData == null)
                {
                    TextAsset jsonAsset = Util.GetJSONAsset(name, new string[] { folder });
                    jsonData = new QuickJSON(jsonAsset.text);
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
                    generation = RL.GetCharacterGeneration(fbx, name, JsonData);
                }

                return generation;
            } 
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
                        if (value == "true") qualRefractiveEyes = true;
                        else qualRefractiveEyes = false;
                        break;
                    case "bakeIsBaked":
                        if (value == "true") bakeIsBaked = true;
                        else bakeIsBaked = false;
                        break;
                    case "bakeCustomShaders":
                        if (value == "true") bakeCustomShaders = true;
                        else bakeCustomShaders = false;
                        break;                    
                }
            }
        }

        public void Write()
        {
            StreamWriter writer = new StreamWriter(infoPath, false);
            writer.WriteLine("logType=" + logType.ToString());
            writer.WriteLine("qualRefractiveEyes=" + (qualRefractiveEyes ? "true" : "false"));
            writer.WriteLine("bakeIsBaked=" + (bakeIsBaked ? "true" : "false"));
            writer.WriteLine("bakeCustomShaders=" + (bakeCustomShaders ? "true" : "false"));            
            writer.Close();
            AssetDatabase.ImportAsset(infoPath);
            infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(infoPath);
        }
    }

}
