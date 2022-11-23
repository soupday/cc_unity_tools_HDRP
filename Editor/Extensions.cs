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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reallusion.Import
{
    public static class Extensions
    {
        public static bool iContains(this List<string> list, string search)
        {
            foreach (string s in list)
            {
                if (s.iEquals(search)) return true;
            }

            return false;
        }

        public static bool iEquals(this string a, string b)
        {
            return a.Equals(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool iContains(this string a, string b)
        {
            return a.ToLowerInvariant().Contains(b.ToLowerInvariant());
        }

        public static bool iStartsWith(this string a, string b)
        {
            return a.StartsWith(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool iEndsWith(this string a, string b)
        {
            return a.EndsWith(b, System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool SetRemapRange(this Material mat, string shaderRef, float from, float to)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                Color range;
                range.r = from;
                range.g = to;
                range.b = 0f;
                range.a = 0f;
                mat.SetColor(shaderRef, range);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     e.g. mat.SetMinMaxRange("_SmoothnessRemap", 0f, 1f);
        /// </summary>        
        public static bool SetMinMaxRange(this Material mat, string shaderRef, float min, float max)
        {
            string shaderRefMin = shaderRef + "Min";
            string shaderRefMax = shaderRef + "Max";

            if (mat.shader && 
                mat.shader.FindPropertyIndex(shaderRefMin) >= 0 &&
                mat.shader.FindPropertyIndex(shaderRefMax) >= 0)
            {
                mat.SetFloat(shaderRefMin, min);
                mat.SetFloat(shaderRefMax, max);
                return true;
            }
            return false;
        }

        public static bool GetRemapRange(this Material mat, string shaderRef, out float from, out float to)
        {
            from = 0f;
            to = 1f;
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                Color range = mat.GetColor(shaderRef);
                from = range.r;
                to = range.g;
                return true;
            }
            return false;
        }

        public static bool SetTextureIf(this Material mat, string shaderRef, Texture2D tex)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetTexture(shaderRef, tex);
                return true;
            }
            return false;
        }

        public static bool SetTextureIf(this Material mat, string shaderRef, Texture tex)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetTexture(shaderRef, tex);
                return true;
            }
            return false;
        }

        public static Texture GetTextureIf(this Material mat, string shaderRef)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetTexture(shaderRef);
            }
            return null;
        }

        public static bool SetTextureScaleIf(this Material mat, string shaderRef, Vector2 scale)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetTextureScale(shaderRef, scale);
                return true;
            }
            return false;
        }

        public static bool SetFloatIf(this Material mat, string shaderRef, float value)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetFloat(shaderRef, value);
                return true;
            }
            return false;
        }

        public static float GetFloatIf(this Material mat, string shaderRef, float defaultValue = 0)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetFloat(shaderRef);                
            }
            return defaultValue;
        }

        public static bool SetVectorIf(this Material mat, string shaderRef, Vector4 value)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetVector(shaderRef, value);
                return true;
            }
            return false;
        }

        public static Vector4 GetVectorIf(this Material mat, string shaderRef, Vector4 defaultValue = default(Vector4))
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetVector(shaderRef);
            }
            return defaultValue;
        }

        public static bool SetColorIf(this Material mat, string shaderRef, Color value)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                mat.SetColor(shaderRef, value);
                return true;
            }
            return false;
        }

        public static Color GetColorIf(this Material mat, string shaderRef, Color defaultValue = default(Color))
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetColor(shaderRef);
            }
            return defaultValue;
        }
    }
}
