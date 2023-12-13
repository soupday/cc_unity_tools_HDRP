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

using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Reallusion.Import
{
    public class CharacterInfo
    {
        public enum ProcessingType { None, Basic, HighQuality }
        public enum EyeQuality { None, Basic, Parallax, Refractive }
        public enum HairQuality { None, Default, TwoPass, Coverage }
        public enum ShaderFeatureFlags 
        { 
            NoFeatures = 0, 
            Tessellation = 1, 
            ClothPhysics = 2, // group flag to allow selection between UnityClothPhysics & MagicaCloth
            HairPhysics = 4, // group flag to allow selection between UnityClothHairPhysics & MagicaClothHairPhysics
            SpringBoneHair = 8,  // dynamic bone springbones
            WrinkleMaps = 16,
            MagicaCloth = 32, // Magica Mesh Cloth for clothing items
            MagicaBone = 64, // Magica Bone Cloth for hair
            UnityClothPhysics = 128, // Unity Cloth for clothing items 
            UnityClothHairPhysics = 256, // Unity Cloth for hair items
            MagicaClothHairPhysics = 512, // Magica Mesh Cloth for hair items
            SpringBonePhysics = 1024  // group flag to allow selection between SpringBoneHair & MagicaBone
        }

        // 'radio groups' of mutually exclusive settings
        public static ShaderFeatureFlags[] clothGroup =
        {
            ShaderFeatureFlags.UnityClothPhysics, // UnityEngine.Cloth instance
            ShaderFeatureFlags.MagicaCloth // MagicaCloth2 instance set to 'Mesh Cloth' mode
        };

        public static ShaderFeatureFlags[] hairGroup =
        {
            ShaderFeatureFlags.UnityClothHairPhysics, // UnityEngine.Cloth instance for hair objects
            ShaderFeatureFlags.MagicaClothHairPhysics // Magica Cloth 2 'Mesh Cloth' for hair objects
        };

        public static ShaderFeatureFlags[] springGroup =
        {
            ShaderFeatureFlags.SpringBoneHair, // DynamicBone springbones
            ShaderFeatureFlags.MagicaBone // MagicaCloth2 instance set to 'Bone Cloth' mode for springbones
        };

        public enum RigOverride { None = 0, Generic, Humanoid }

        public string guid;
        public string path;        
        public string infoFilepath;
        public string jsonFilepath;
        public string name;
        public string folder;                
          
        public bool isLOD = false;
        public bool bakeIsBaked = false;
        public bool tempHairBake = false;
        public bool animationSetup = false;
        public int animationRetargeted = 0;

        public bool selectedInList;
        public bool settingsChanged;

        // these are the settings the character is currently set to build
        private ProcessingType logType = ProcessingType.None;
        private EyeQuality qualEyes = EyeQuality.Parallax;
        private HairQuality qualHair = HairQuality.TwoPass;
        public RigOverride UnknownRigType { get; set; }
        private bool bakeCustomShaders = true;
        private bool bakeSeparatePrefab = true;
        private GameObject prefabAsset;

        public struct GUIDRemap
        {
            public string from;
            public string to;

            public GUIDRemap(string from, string to)
            {
                this.from = from;
                this.to = to;
            }

            public GUIDRemap(Object from, Object to)
            {
                this.from = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(from));
                this.to = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(to));
            }
        }

        private List<GUIDRemap> guidRemaps;

        public void AddGUIDRemap(Object from, Object to)
        {
            guidRemaps.Add(new GUIDRemap(from, to));
        }

        public Object GetGUIDRemapFrom(Object to)
        {
            string guidTo = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(to));

            foreach (GUIDRemap gr in guidRemaps)
            {
                if (gr.to == guidTo)
                {
                    string path = AssetDatabase.GUIDToAssetPath(gr.from);
                    if (!string.IsNullOrEmpty(path)) return AssetDatabase.LoadAssetAtPath<Object>(path);
                    else return null;
                }
            }

            return null;
        }

        public Object GetGUIDRemapTo(Object from)
        {
            string guidFrom = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(from));

            foreach (GUIDRemap gr in guidRemaps)
            {
                if (gr.from == guidFrom)
                {
                    string path = AssetDatabase.GUIDToAssetPath(gr.to);
                    if (!string.IsNullOrEmpty(path)) return AssetDatabase.LoadAssetAtPath<Object>(path);
                    else return null;
                }
            }

            return null;
        }

        public void RemoveGUIDRemap(Object from, Object to)
        {
            string guidTo = "";
            string guidFrom = "";
            if (to) guidTo = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(to));
            if (from) guidFrom = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(from));
            for (int i = 0; i < guidRemaps.Count; i++)
            {
                GUIDRemap gr = guidRemaps[i];
                if (gr.from == guidFrom || gr.to == guidTo)
                {
                    guidRemaps.RemoveAt(i--);
                }
            }
        }

        public void CleanGUIDRemaps()
        {
            RemoveGUIDRemap(null, null);
        }

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

        public bool FeatureUseWrinkleMaps => (ShaderFlags & ShaderFeatureFlags.WrinkleMaps) > 0;
        public bool FeatureUseTessellation => (ShaderFlags & ShaderFeatureFlags.Tessellation) > 0;
        public bool FeatureUseClothPhysics => (ShaderFlags & ShaderFeatureFlags.ClothPhysics) > 0;
        public bool FeatureUseHairPhysics => (ShaderFlags & ShaderFeatureFlags.HairPhysics) > 0;
        //public bool FeatureUseSpringBones => (ShaderFlags & ShaderFeatureFlags.SpringBones) > 0;        
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

        public ShaderFeatureFlags BuiltShaderFlags { get; private set; } = ShaderFeatureFlags.NoFeatures;
        public bool BuiltFeatureWrinkleMaps => (BuiltShaderFlags & ShaderFeatureFlags.WrinkleMaps) > 0;
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

        public string CharacterName => name;

        public bool IsBlenderProject { get { return JsonData.GetBoolValue(CharacterName + "/Blender_Project"); } }

        private BaseGeneration generation = BaseGeneration.None;
        private GameObject fbx;
        private QuickJSON jsonData;

        public void FixCharSettings()
        {
            if (logType == ProcessingType.HighQuality && !CanHaveHighQualityMaterials)
                logType = ProcessingType.Basic;

            if (qualEyes == EyeQuality.Refractive && !Pipeline.isHDRP) 
                qualEyes = EyeQuality.Parallax;

            if (qualHair == HairQuality.Coverage && Pipeline.isHDRP)
                qualHair = HairQuality.Default;

            //if ((ShaderFlags & ShaderFeatureFlags.SpringBoneHair) > 0 &&
            //    (ShaderFlags & ShaderFeatureFlags.HairPhysics) > 0)
            //{
            //    ShaderFlags -= ShaderFeatureFlags.SpringBoneHair;
            //}
            CheckRadioGroupFlags();  // set default unity cloth simulation flags if unset
        }

        public CharacterInfo(string guid)
        {
            this.guid = guid;
            path = AssetDatabase.GUIDToAssetPath(this.guid);
            name = Path.GetFileNameWithoutExtension(path);
            folder = Path.GetDirectoryName(path);            
            infoFilepath = Path.Combine(folder, name + "_ImportInfo.txt");
            jsonFilepath = Path.Combine(folder, name + ".json");
            if (path.iContains("_lod")) isLOD = true;
            guidRemaps = new List<GUIDRemap>();

            selectedInList = false;
            settingsChanged = false;

            if (File.Exists(infoFilepath))            
                Read();
            else
                Write();
        }

        public void CopySettings(CharacterInfo from)
        {
            UnknownRigType = from.UnknownRigType;
            logType = from.logType;
            qualEyes = from.qualEyes;
            qualHair = from.qualHair;
            bakeCustomShaders = from.bakeCustomShaders;
            bakeSeparatePrefab = from.bakeSeparatePrefab;  
            ShaderFlags = from.ShaderFlags;
            FixCharSettings();
        }

        public void ApplySettings()
        {            
            FixCharSettings();
            CleanGUIDRemaps();

            builtLogType = logType;
            builtQualEyes = qualEyes;
            builtQualHair = qualHair;
            builtBakeCustomShaders = bakeCustomShaders;
            builtBakeSeparatePrefab = bakeSeparatePrefab;
            BuiltShaderFlags = ShaderFlags;
        }        

        public GameObject Fbx
        {
            get
            {
                if (fbx == null)
                {
                    fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Util.LogDetail("CharInfo: " + name + " FBX Loaded");
                }
                return fbx;
            }
        }

        public bool FbxLoaded
        {
            get { return fbx != null; }
        }

        public Avatar GetCharacterAvatar()
        {                        
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in objects)
            {
                if (obj.GetType() == typeof(Avatar))
                {
                    return obj as Avatar;
                }
            }

            return null;
        }

        public List<string> GetMotionGuids()
        {
            List<string> motionGuids = new List<string>();
            DirectoryInfo di = new DirectoryInfo(folder);
            string prefix = name + "_";
            string suffix = "_Motion.fbx";
            foreach (FileInfo fi in di.GetFiles("*.fbx"))
            {
                if (fi.Name.iStartsWith(prefix) && fi.Name.iEndsWith(suffix))
                {
                    string path = Path.Combine(folder, fi.Name);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    motionGuids.Add(guid);
                }
            }

            return motionGuids;
        }

        public string GetPrefabsFolder()
        {
            return Path.Combine(folder, Importer.PREFABS_FOLDER);
        }

        public GameObject PrefabAsset
        {
            get
            {
                if (!prefabAsset) prefabAsset = Util.FindCharacterPrefabAsset(Fbx);
                return prefabAsset;
            }
        }

        public GameObject BakedPrefabAsset
        {
            get
            {
                return Util.FindCharacterPrefabAsset(Fbx, true);
            }
        }

        public GameObject GetPrefabInstance(bool baked = false)
        {
            if (baked)
            {
                GameObject bakedPrefabAsset = BakedPrefabAsset;
                if (bakedPrefabAsset) 
                    return (GameObject)PrefabUtility.InstantiatePrefab(BakedPrefabAsset);
            }
            else
            {
                GameObject prefabAsset = PrefabAsset;
                if (prefabAsset)
                    return (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            }

            return null;
        }

        public QuickJSON JsonData
        { 
            get
            {
                if (jsonData == null)
                {
                    jsonData = Util.GetJsonData(jsonFilepath);
                    Util.LogDetail("CharInfo: " + name + " JsonData Fetched");
                }
                return jsonData;
            }
        }

        public bool JsonLoaded { get { return jsonData != null; } }

        public QuickJSON RootJsonData
        {
            get
            {
                string jsonPath = name;
                if (JsonData.PathExists(jsonPath))
                    return JsonData.GetObjectAtPath(jsonPath);
                return null;
            }
        }

        public string JsonVersion
        {
            get
            {
                string jsonPath = name + "/Version";
                if (JsonData.PathExists(jsonPath))
                    return JsonData.GetStringValue(jsonPath);
                return "";
            }
        }

        public QuickJSON CharacterJsonData
        {
            get
            {
                string jsonPath = name + "/Object/" + name;
                if (JsonData.PathExists(jsonPath))
                    return JsonData.GetObjectAtPath(jsonPath);
                return null;
            }
        }

        public QuickJSON ObjectsJsonData
        {
            get
            {
                if (JsonVersion.StartsWith("1.20."))
                {
                    string jsonPath = name + "/Object/" + name + "/Nodes";
                    if (JsonData.PathExists(jsonPath))
                        return JsonData.GetObjectAtPath(jsonPath);
                }
                else
                {
                    string jsonPath = name + "/Object/" + name + "/Meshes";
                    if (JsonData.PathExists(jsonPath))
                        return JsonData.GetObjectAtPath(jsonPath);
                }
                return null;
            }
        }

        public string ObjectsMatJsonPath(string objName, string matName)
        {
            if (JsonVersion.StartsWith("1.20."))
            {
                return objName + "/Meshes/" + objName + "/Materials/" + matName;
            }
            else
            {
                return objName + "/Materials/" + matName;
            }            
        }

        public string ObjectsMaterialsJsonPath(string objName)
        {
            if (JsonVersion.StartsWith("1.20."))
            {
                return objName + "/Meshes/" + objName + "/Materials/";
            }
            else
            {
                return objName + "/Materials/";
            }
        }

        public string ObjectsMeshJsonPath(string objName)
        {
            if (JsonVersion.StartsWith("1.20."))
            {
                return "Nodes/" + objName + "/Meshes/" + objName;
            }
            else
            {
                return "Meshes/" + objName;
            }
        }

        public QuickJSON GetMatJson(GameObject obj, string sourceName)
        {
            QuickJSON objectsData = ObjectsJsonData;
            QuickJSON matJson = null;
            string objName = obj.name;
            string jsonPath = "";
            if (objectsData != null)
            {
                jsonPath = ObjectsMatJsonPath(objName, sourceName);
                matJson = objectsData.GetObjectAtPath(jsonPath);
                                
                if (matJson == null)
                {
                    if (objName.iContains("_Extracted"))
                    {
                        objName = objName.Substring(0, objName.IndexOf("_Extracted", System.StringComparison.InvariantCultureIgnoreCase));

                        jsonPath = ObjectsMatJsonPath(objName, sourceName);                        
                        matJson = objectsData.GetObjectAtPath(jsonPath);
                    }
                }

                if (matJson == null)
                {
                    // there is a bug where a space in name causes the name to be truncated on export from CC3/4
                    if (objName.Contains(" "))
                    {
                        Util.LogWarn("Object name " + objName + " contains a space, this can cause the materials to setup incorrectly...");
                        string[] split = objName.Split(' ');
                        jsonPath = ObjectsMatJsonPath(split[0], sourceName);                        
                        if (objectsData.PathExists(jsonPath))
                        {
                            matJson = objectsData.GetObjectAtPath(jsonPath);
                            Util.LogWarn(" - Found matching object/material data for: " + split[0] + "/" + sourceName);
                        }
                    }
                }                
                    
                if (matJson == null)
                {
                    // instalod will generate unique suffixes _0/_1/_2 on character objects where object names and container
                    // transforms have the same name, try to untangle the object name by speculatively removing this suffix.
                    // (seems to happen mostly on accessories)

                    string realObjName = null;                    

                    if (objectsData.PathExists(objName))
                    {
                        realObjName = objName;
                    }

                    if (realObjName == null)
                    {
                        // remove instalod suffix and attempt to find object name in json again
                        if (objName[objName.Length - 2] == '_' && char.IsDigit(objName[objName.Length - 1]))
                        {
                            Util.LogWarn("Object name " + objName + " may be incorrectly suffixed by InstaLod exporter. Attempting to untangle...");
                            string specObjName = objName.Substring(0, objName.Length - 2);
                            if (objectsData.PathExists(specObjName))
                            {
                                realObjName = specObjName;
                            }                            
                            else
                            {
                                // finally search for an object name in the mesh json whose name starts with the truncted source name
                                realObjName = objectsData.FindKeyName(specObjName);                                
                            }
                        }
                    }

                    if (realObjName != null)
                    {
                        string realMatName = null;                        

                        if (objectsData.PathExists(ObjectsMatJsonPath(realObjName, sourceName)))
                        {
                            realMatName = sourceName;
                        }

                        if (realMatName == null)
                        {                            
                            if (sourceName[sourceName.Length - 2] == '_' && char.IsDigit(sourceName[sourceName.Length - 1]))
                            {
                                Util.LogWarn("Material name " + sourceName + " may by suffixed by InstaLod exporter. Attempting to untangle...");
                                string specMatName = sourceName.Substring(0, sourceName.Length - 2);
                                if (objectsData.PathExists(ObjectsMatJsonPath(realObjName, specMatName)))
                                {
                                    realMatName = specMatName;
                                }
                                else
                                {
                                    // finally search for an object name in the mesh json whose name starts with the truncted source name
                                    realMatName = objectsData.FindKeyName(ObjectsMaterialsJsonPath(realObjName), specMatName);
                                }
                            }
                        }

                        if (realObjName != null && realMatName != null &&
                            objectsData.PathExists(ObjectsMatJsonPath(realObjName, realMatName)))
                        {
                            matJson = objectsData.GetObjectAtPath(ObjectsMatJsonPath(realObjName, realMatName));
                            if (matJson != null)
                            {
                                Util.LogWarn(" - Found matching object/material data for: " + realObjName + "/" + realMatName);
                            }
                        }
                    }
                }
                
            }
            if (matJson == null) Util.LogError("Unable to find json material data: " + jsonPath);

            return matJson;
        }

        public QuickJSON PhysicsJsonData
        {
            get
            {                              
                string jsonPath = name + "/Object/" + name + "/Physics";
                if (JsonData.PathExists(jsonPath))
                    return JsonData.GetObjectAtPath(jsonPath);
                return null;
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
            if (jsonData != null) jsonData = Util.GetJsonData(jsonFilepath);
        }
        
        public BaseGeneration Generation
        { 
            get
            { 
                if (generation == BaseGeneration.None)
                {
                    CheckGeneration();
                }                

                return generation;
            } 
        }

        public bool HasColorEnabledHair()
        {
            if (PrefabAsset)
            {
                Renderer[] renderers = PrefabAsset.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m && m.HasProperty("BOOLEAN_ENABLECOLOR"))
                        {
                            if (m.GetFloat("BOOLEAN_ENABLECOLOR") > 0f) return true;
                        }
                    }
                }
            }
            return false;
        }

        public void CheckGeneration()
        {
            BaseGeneration oldGen = generation;
            string gen = "";

            string generationPath = name + "/Object/" + name + "/Generation";
            if (JsonData.PathExists(generationPath))
            {
                gen = JsonData.GetStringValue(generationPath);
            }

            generation = RL.GetCharacterGeneration(Fbx, gen);
            CheckOverride();            

            // new character detected, initialize settings
            if (oldGen == BaseGeneration.None)
            {
                InitSettings();
            }

            if (generation != oldGen)
            {
                Util.LogDetail("CharInfo: " + name + " Generation detected: " + generation.ToString());
                Write();
            }
        }

        public void CheckGenerationQuick()
        {
            BaseGeneration oldGen = generation;
            string gen = Util.GetJsonGenerationString(jsonFilepath);
            generation = RL.GetCharacterGeneration(Fbx, gen);
            CheckOverride();
            if (generation != oldGen)
            {
                Util.LogDetail("CharInfo: " + name + " Generation detected: " + generation.ToString());
                Write();
            }
        }

        public void CheckOverride()
        {
            if (UnknownRigType == RigOverride.None)
            {
                if (generation == BaseGeneration.Unknown) UnknownRigType = RigOverride.Generic;
                else UnknownRigType = RigOverride.Humanoid;
            }
        }

        public void InitSettings()
        {
            // if wrinkle map data present, enable wrinkle maps.
            if (HasWrinkleMaps())
            {
                ShaderFlags |= ShaderFeatureFlags.WrinkleMaps;
            }
        }

        public bool HasWrinkleMaps()
        {
            return AnyJsonMaterialPathExists("Wrinkle/Textures");            
        }

        public bool AnyJsonMaterialPathExists(string path)
        {
            QuickJSON objectsJson = ObjectsJsonData;

            foreach (MultiValue mvMesh in objectsJson.values)
            {
                if (mvMesh.Type == MultiType.Object)
                {
                    QuickJSON objJson = mvMesh.ObjectValue;
                    string objName = mvMesh.Key;
                    string materialsPath = ObjectsMaterialsJsonPath(objName);
                    QuickJSON materialsJson = objectsJson.GetObjectAtPath(materialsPath);
                    if (materialsJson != null)
                    {
                        foreach (MultiValue mvMat in materialsJson.values)
                        {
                            if (mvMat.Type == MultiType.Object)
                            {
                                QuickJSON matjson = mvMat.ObjectValue;
                                if (matjson.PathExists(path)) return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void Release()
        {
            if (jsonData != null || fbx != null)
            {
                jsonData = null;
                fbx = null;
                Util.LogDetail("CharInfo: " + name + " Data Released!");
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
                    case BaseGeneration.ActorBuild:
                    case BaseGeneration.Unknown:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void Read()
        {
            TextAsset infoAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(infoFilepath);
            guidRemaps.Clear();
            string[] lineEndings = new string[] { "\r\n", "\r", "\n" };
            char[] propertySplit = new char[] { '=' };
            char[] guidSplit = new char[] { '|' };
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
                    case "tempHairBake":
                        tempHairBake = value == "true" ? true : false;
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
                    case "rigOverride":
                        UnknownRigType = (RigOverride)System.Enum.Parse(typeof(RigOverride), value);
                        break;
                    case "GUIDRemap":
                        string[] guids = value.Split(guidSplit, System.StringSplitOptions.None);
                        if (guids.Length == 2)
                        {
                            guidRemaps.Add(new GUIDRemap(guids[0], guids[1]));
                        }
                        break;
                }
            }
            ApplySettings();
        }

        public void Write()
        {
            ApplySettings();
            StreamWriter writer = new StreamWriter(infoFilepath, false);
            writer.WriteLine("logType=" + builtLogType.ToString());
            writer.WriteLine("generation=" + generation.ToString());
            writer.WriteLine("isLOD=" + (isLOD ? "true" : "false"));
            writer.WriteLine("qualEyes=" + builtQualEyes.ToString());
            writer.WriteLine("qualHair=" + builtQualHair.ToString());
            writer.WriteLine("bakeIsBaked=" + (bakeIsBaked ? "true" : "false"));
            writer.WriteLine("tempHairBake=" + (tempHairBake ? "true" : "false"));
            writer.WriteLine("bakeCustomShaders=" + (builtBakeCustomShaders ? "true" : "false"));
            writer.WriteLine("bakeSeparatePrefab=" + (builtBakeSeparatePrefab ? "true" : "false"));
            writer.WriteLine("shaderFlags=" + (int)BuiltShaderFlags);
            writer.WriteLine("animationSetup=" + (animationSetup ? "true" : "false"));
            writer.WriteLine("animationRetargeted=" + animationRetargeted.ToString());
            writer.WriteLine("rigOverride=" + UnknownRigType.ToString());
            foreach (GUIDRemap gr in guidRemaps)
            {
                writer.WriteLine("GUIDRemap=" + gr.from + "|" + gr.to);
            }
            writer.Close();
            AssetDatabase.ImportAsset(infoFilepath);            
        }

        public void CheckRadioGroupFlags()
        {
            if (ImporterWindow.Current == null)
            {
                Util.LogWarn("The Importer Window is not open - please open the CC/iC importer window before continuing.");
                return;
            }            

            if (ShaderFlags.HasFlag(ShaderFeatureFlags.ClothPhysics))
            {
                if (!ImporterWindow.Current.MagicaCloth2Available)
                {
                    ShaderFlags |= ShaderFeatureFlags.UnityClothPhysics;
                }

                if (!GroupHasFlagSet(clothGroup))
                {
                    ShaderFlags |= ShaderFeatureFlags.UnityClothPhysics;
                }
            }
            else
            {
                if (GroupHasFlagSet(clothGroup))
                {
                    ShaderFlags |= ShaderFeatureFlags.ClothPhysics;
                }
            }

            if (ShaderFlags.HasFlag(ShaderFeatureFlags.HairPhysics))
            {
                if (!ImporterWindow.Current.MagicaCloth2Available && !ImporterWindow.Current.DynamicBoneAvailable)
                {
                    ShaderFlags |= ShaderFeatureFlags.UnityClothHairPhysics;
                }

                if (!GroupHasFlagSet(hairGroup))
                {
                    ShaderFlags |= ShaderFeatureFlags.UnityClothHairPhysics;
                }
            }
            else
            {
                if (GroupHasFlagSet(hairGroup))
                {
                    ShaderFlags |= ShaderFeatureFlags.HairPhysics;
                }
            }
        }

        public void EnsureDefaultsAreSet(ShaderFeatureFlags flag)
        {
            if (ImporterWindow.Current == null)
            {
                Util.LogWarn("The Importer Window is not open - please open the CC/iC importer window before continuing.");
                return;
            }

            // if no alternatives are available or the flags are unset - then set unity physics as a default when activating cloth or hair physics
            switch (flag)
            {
                case ShaderFeatureFlags.ClothPhysics:
                    {
                        if (!ImporterWindow.Current.MagicaCloth2Available)
                        {
                            ShaderFlags |= ShaderFeatureFlags.UnityClothPhysics;
                        }

                        if (!GroupHasFlagSet(clothGroup))
                        {
                            ShaderFlags |= ShaderFeatureFlags.UnityClothPhysics;
                        }

                        break;
                    }
                case ShaderFeatureFlags.HairPhysics:
                    {
                        if (!ImporterWindow.Current.MagicaCloth2Available && !ImporterWindow.Current.DynamicBoneAvailable)
                        {
                            ShaderFlags |= ShaderFeatureFlags.UnityClothHairPhysics;
                        }

                        if (!GroupHasFlagSet(hairGroup))
                        {
                            ShaderFlags |= ShaderFeatureFlags.UnityClothHairPhysics;
                        }

                        break;
                    }
                case ShaderFeatureFlags.SpringBonePhysics:
                    {
                        bool dyn = ImporterWindow.Current.DynamicBoneAvailable;
                        bool mag = ImporterWindow.Current.MagicaCloth2Available;

                        if (dyn && mag)
                        {
                            ShaderFlags |= ShaderFeatureFlags.MagicaBone;
                        }
                        else
                        {
                            if (mag)
                                ShaderFlags |= ShaderFeatureFlags.MagicaBone; 
                            else if (dyn)
                                ShaderFlags |= ShaderFeatureFlags.SpringBoneHair;
                        }

                        break;
                    }
            }
        }

        public bool GroupHasFlagSet(ShaderFeatureFlags[] group)
        {
            foreach (ShaderFeatureFlags groupFlag in group)
            {
                if (ShaderFlags.HasFlag(groupFlag)) return true;
            }
            return false;
        }
    }
}
