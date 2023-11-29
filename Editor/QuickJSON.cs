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
using System.Linq;
using UnityEngine;

namespace Reallusion.Import
{
    public class QuickJSON
    {
        public List<MultiValue> values;
        public bool isArray = false;

        public int index = 0;
        private string text;

        public enum NextType { None, OpenBrace, CloseBrace, OpenSquare, CloseSquare, Number, Alpha, String, Seperator, Comma }

        public QuickJSON(string jsonText, int startIndex = 0, bool array = false)
        {
            isArray = array;
            text = jsonText;
            index = startIndex;
            values = new List<MultiValue>();
            try
            {
                if (index == 0) Brace();
                Parse();
            }
            catch
            {
                Util.LogError("Unable to Parse JSON text...");
            }
            text = null;
        }

        void Parse()
        {
            string name = "";
            bool newPair = true;

            while (index < text.Length)
            {
                NextType type = Next();

                switch (type)
                {
                    case NextType.Comma:
                        name = "";
                        newPair = true;
                        break;

                    case NextType.Seperator:
                        newPair = false;
                        break;

                    case NextType.CloseSquare:
                        return;

                    case NextType.CloseBrace:
                        return;

                    case NextType.OpenBrace:
                        QuickJSON childObject = new QuickJSON(text, index);
                        index = childObject.index;
                        values.Add(new MultiValue(name, childObject));
                        break;

                    case NextType.OpenSquare:
                        QuickJSON childArray = new QuickJSON(text, index, true);
                        index = childArray.index;
                        values.Add(new MultiValue(name, childArray));
                        break;

                    case NextType.Number:
                        ParseNumber(name);
                        break;

                    case NextType.String:
                        if (!isArray && newPair)
                        {
                            name = String();
                            newPair = false;
                        }
                        else
                            ParseString(name);
                        break;

                    case NextType.Alpha:
                        ParseAlpha(name);
                        break;
                }
            }
        }

        void ParseNumber(string name)
        {
            string value = Value();

            if (int.TryParse(value,
                    System.Globalization.NumberStyles.Integer, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out int tryInt))
                values.Add(new MultiValue(name, tryInt));

            else if (float.TryParse(value, 
                    System.Globalization.NumberStyles.Number, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out float tryFloat))
                values.Add(new MultiValue(name, tryFloat));
        }

        void ParseAlpha(string name)
        {
            string value = Value();

            if (bool.TryParse(value, out bool tryBool))
                values.Add(new MultiValue(name, tryBool));
            else
                values.Add(new MultiValue(name, value));
        }

        void ParseString(string name)
        {
            string value = String();

            values.Add(new MultiValue(name, value));
        }

        NextType Next()
        {
            while (index < text.Length)
            {
                char c = text[index++];

                if (char.IsWhiteSpace(c)) continue;
                if (char.IsDigit(c) || c == '-' || c == '+') { index--; return NextType.Number; }
                if (IsAlpha(c)) { index--; return NextType.Alpha; }

                switch (c)
                {
                    case ',': return NextType.Comma;
                    case ':': return NextType.Seperator;
                    case '{': return NextType.OpenBrace;
                    case '}': return NextType.CloseBrace;
                    case '[': return NextType.OpenSquare;
                    case ']': return NextType.CloseSquare;
                    case '"':
                        index--;
                        return NextType.String;
                }
            }

            return NextType.None;
        }

        bool IsAlpha(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        string String()
        {
            int s = index + 1;
            int e = text.IndexOf('"', s);
            if (e > -1)
            {
                index = e + 1;
                return text.Substring(s, e - s);
            }
            return "";
        }

        string Value()
        {
            int s = index;

            while (index < text.Length)
            {
                char c = text[index];
                if (char.IsWhiteSpace(c)) break;
                if (c == ',' || c == '}' || c == ']') break;
                index++;
            }

            int e = index;

            return text.Substring(s, e - s);
        }

        void Brace()
        {
            // set index after the next brace
            index = text.IndexOf("{", index) + 1;
        }

        public MultiValue GetValue(string name, bool spaceFix = false)
        {
            foreach (MultiValue mv in values)
                if (mv.Key.iEquals(name)) return mv;                        

            return new MultiValue("None");
        }

        public MultiValue GetValue(int index)
        {
            if (values.Count > index)
                return values[index];            

            return new MultiValue("None");
        }

        public QuickJSON FindObjectWithKey(string keySearch, bool recursive = false)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Type == MultiType.Object)
                {
                    if (values[i].Key.iContains(keySearch))
                    {
                        return values[i].ObjectValue;
                    }
                    else if (recursive)
                    {
                        QuickJSON found = values[i].ObjectValue.FindObjectWithKey(keySearch, recursive);
                        if (found != null) return found;
                    }
                }
            }

            return null;
        }

        public string FindKeyName(string path, string keySearch, bool recursive = false)
        {
            QuickJSON pathJson = GetObjectAtPath(path);
            return pathJson.FindKeyName(keySearch, recursive);
        }

        public string FindKeyName(string keySearch, bool recursive = false)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Type == MultiType.Object)
                {
                    if (values[i].Key.iContains(keySearch))
                    {
                        return values[i].Key;
                    }
                    else if (recursive)
                    {
                        string found = values[i].ObjectValue.FindKeyName(keySearch);
                        if (found != null) return found;
                    }
                }
            }

            return null;
        }

        public bool PathExists(string path)
        {
            string[] paths = path.Split('/');

            return PathExists(paths);
        }

        public bool PathExists(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.PathExists(paths.Skip(1).ToArray());
                else if (mv.Type != MultiType.None)
                    return true;
            }            

            return false;
        }

        public QuickJSON GetObjectAtPath(string path)
        {
            string[] paths = path.Split('/');

            return GetObjectAtPath(paths);
        }

        public QuickJSON GetObjectAtPath(string[] paths)
        {
            if (paths.Length > 0)
            {
                if (string.IsNullOrEmpty(paths[0]))
                    return this;
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetObjectAtPath(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Object)
                    return mv.ObjectValue;
            }            

            return null;
        }

        public bool GetBoolValue(string path)
        {
            string[] paths = path.Split('/');

            return GetBoolValue(paths);
        }

        public bool GetBoolValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetBoolValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Bool)
                    return mv.BoolValue;
            }

            return false;
        }

        public int GetIntValue(string path)
        {
            string[] paths = path.Split('/');

            return GetIntValue(paths);
        }

        public int GetIntValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetIntValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Integer)
                    return mv.IntValue;
            }

            return 0;
        }

        public float GetFloatValue(string path)
        {
            string[] paths = path.Split('/');

            return GetFloatValue(paths);
        }

        public float GetFloatValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetFloatValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Float)
                    return mv.FloatValue;
            }

            return 0.0f;
        }

        public string GetStringValue(string path)
        {
            string[] paths = path.Split('/');

            return GetStringValue(paths);
        }

        public string GetStringValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetStringValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.String)
                    return mv.StringValue;
            }

            return "";
        }

        public Color GetColorValue(string path)
        {
            string[] paths = path.Split('/');

            return GetColorValue(paths);
        }

        public Color GetColorValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetColorValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Object)
                    return mv.ObjectValue.ColorValue;
            }

            return Color.white;
        }


        public Quaternion GetQuaternionValue(string path)
        {
            string[] paths = path.Split('/');

            return GetQuaternionValue(paths);
        }

        public Quaternion GetQuaternionValue(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetQuaternionValue(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Object)
                    return mv.ObjectValue.QuaternionValue;
            }

            return Quaternion.identity;
        }



        public Vector3 GetVector3Value(string path)
        {
            string[] paths = path.Split('/');

            return GetVector3Value(paths);
        }

        public Vector3 GetVector3Value(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetVector3Value(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Object)
                    return mv.ObjectValue.Vector3Value;
            }

            return Vector3.zero;
        }

        public Vector3 GetVector2Value(string path)
        {
            string[] paths = path.Split('/');

            return GetVector2Value(paths);
        }

        public Vector3 GetVector2Value(string[] paths)
        {
            if (paths.Length > 0)
            {
                MultiValue mv = GetValue(paths[0]);
                if (paths.Length > 1 && mv.Type == MultiType.Object)
                    return mv.ObjectValue.GetVector2Value(paths.Skip(1).ToArray());
                else if (mv.Type == MultiType.Object)
                    return mv.ObjectValue.Vector2Value;
            }

            return Vector2.zero;
        }

        public Color ColorValue
        {
            get
            {
                Color value = Color.black;

                if (isArray && values.Count == 3)
                {
                    value.r = values[0].FloatValue / 255.0f;
                    value.g = values[1].FloatValue / 255.0f;
                    value.b = values[2].FloatValue / 255.0f;
                }

                return value;
            }
        }

        public Quaternion QuaternionValue
        {
            get
            {
                Quaternion value = Quaternion.identity;

                if (isArray && values.Count == 4)
                {
                    value.x = values[0].FloatValue;
                    value.y = values[1].FloatValue;
                    value.z = values[2].FloatValue;
                    value.w = values[3].FloatValue;
                }

                return value;
            }
        }

        public Vector3 Vector3Value
        {
            get
            {
                Vector3 value = Vector3.zero;

                if (isArray && values.Count == 3)
                {
                    value.x = values[0].FloatValue;
                    value.y = values[1].FloatValue;
                    value.z = values[2].FloatValue;
                }

                return value;
            }
        }

        public Vector2 Vector2Value
        {
            get
            {
                Vector2 value = Vector2.zero;

                if (isArray && values.Count == 2)
                {
                    value.x = values[0].FloatValue;
                    value.y = values[1].FloatValue;                    
                }

                return value;
            }
        }

        public QuickJSON FindParentOf(QuickJSON jsonObject)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Type == MultiType.Object)
                {
                    if (values[i].ObjectValue == jsonObject)
                    {
                        return this;
                    }
                    else
                    {
                        QuickJSON found = values[i].ObjectValue.FindParentOf(jsonObject);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }

            return null;
        }

    }

    public enum MultiType { Integer, Float, Bool, String, Object, None }

    public struct MultiValue
    {
        private object objectValue;

        public MultiType Type { get; }
        public string Key { get; }
        public bool BoolValue
        { 
            get 
            { 
                return Type == MultiType.Bool ? 
                    (bool)objectValue : false; 
            } 
        }
        public string StringValue 
        { 
            get 
            { 
                return Type == MultiType.String ? 
                    (string)objectValue : null; 
            } 
        }
        public int IntValue
        {
            get
            {
                return Type == MultiType.Integer ?
                    (int)objectValue : 
                        (Type == MultiType.Float ? 
                            (int)((float)objectValue) : 0);
            }
        }
        public float FloatValue
        { 
            get 
            { 
                return Type == MultiType.Float ? 
                    (float)objectValue : 
                        (Type == MultiType.Integer ? 
                            (float)((int)objectValue) : 0); 
            } 
        }
        public QuickJSON ObjectValue 
        { 
            get 
            { 
                return Type == MultiType.Object ? 
                    (QuickJSON)objectValue : null; 
            } 
        }

        public MultiValue(string name)
        {
            Key = name;
            Type = MultiType.None;
            objectValue = null;
        }

        public MultiValue(string name, bool value)
        {
            Key = name;
            Type = MultiType.Bool;            
            objectValue = value;
        }

        public MultiValue(string name, int value)
        {
            Key = name;
            Type = MultiType.Integer;
            objectValue = value;
        }

        public MultiValue(string name, float value)
        {
            Key = name;
            Type = MultiType.Float;
            objectValue = value;
        }

        public MultiValue(string name, string value)
        {
            Key = name;
            Type = MultiType.String;
            objectValue = value;
        }

        public MultiValue(string name, QuickJSON value)
        {
            Key = name;
            Type = MultiType.Object;
            objectValue = value;
        }
    }

}