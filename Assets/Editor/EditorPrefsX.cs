using UnityEditor;
using UnityEngine;

// Helper class to save and load Color values in EditorPrefs
public static class EditorPrefsX
{
    public static Color GetColor(string key)
    {
        float r = EditorPrefs.GetFloat(key + "_r");
        float g = EditorPrefs.GetFloat(key + "_g");
        float b = EditorPrefs.GetFloat(key + "_b");
        float a = EditorPrefs.GetFloat(key + "_a");
        return new Color(r, g, b, a);
    }

    public static void SetColor(string key, Color value)
    {
        EditorPrefs.SetFloat(key + "_r", value.r);
        EditorPrefs.SetFloat(key + "_g", value.g);
        EditorPrefs.SetFloat(key + "_b", value.b);
        EditorPrefs.SetFloat(key + "_a", value.a);
    }
}
