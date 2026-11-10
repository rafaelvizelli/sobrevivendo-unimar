using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProvaImpossivel
{
    /// <summary>
    /// Materiais lisos (URP/Lit) reaproveitados por cor.
    /// No editor (fora do Play) viram assets em Assets/Materials/Personagens para ficarem salvos na cena.
    /// </summary>
    public static class MaterialLibrary
    {
        const string Folder = "Assets/Materials/Personagens";
        static readonly Dictionary<string, Material> cache = new();

        public static Material Get(Color color, float smoothness = 0.25f, float emission = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGB(color) + "_" + Mathf.RoundToInt(smoothness * 100)
                         + (emission > 0f ? "_E" + Mathf.RoundToInt(emission * 10) : "");
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Material m;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = Folder + "/" + key + ".mat";
                m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                    if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Materials", "Personagens");
                    m = Create(color, smoothness, emission);
                    AssetDatabase.CreateAsset(m, path);
                }
                cache[key] = m;
                return m;
            }
#endif
            m = Create(color, smoothness, emission);
            cache[key] = m;
            return m;
        }

        static Material Create(Color color, float smoothness, float emission)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
            }
            return m;
        }
    }
}
