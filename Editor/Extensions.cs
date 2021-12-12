using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reallusion.Import
{
    public static class Extensions
    {
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

        public static float GetFloatIf(this Material mat, string shaderRef)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetFloat(shaderRef);                
            }
            return 0f;
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

        public static Vector4 GetVectorIf(this Material mat, string shaderRef)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetVector(shaderRef);
            }
            return Vector4.zero;
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

        public static Color GetColorIf(this Material mat, string shaderRef)
        {
            if (mat.shader && mat.shader.FindPropertyIndex(shaderRef) >= 0)
            {
                return mat.GetColor(shaderRef);
            }
            return Color.magenta;
        }
    }
}
