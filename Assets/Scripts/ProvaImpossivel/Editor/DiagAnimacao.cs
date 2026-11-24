using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Diagnóstico (só editor): mede a passada dos clipes de caminhada/corrida do pacote Quaternius.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod ProvaImpossivel.EditorTools.DiagAnimacao.Medir
    /// </summary>
    public static class DiagAnimacao
    {
        public static void Medir()
        {
            foreach (var path in new[] { "Assets/Art/Personagens/Quaternius/Animacoes_Homem.fbx", "Assets/Art/Personagens/Quaternius/Animacoes_Mulher.fbx" })
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                             .Where(c => !c.name.StartsWith("__preview") && (c.name.Contains("Walk") || c.name.Contains("Run") || c.name == "Idle")))
                    Debug.Log($"[DiagAnimacao] {path.Split('/').Last()} {c.name}: duração {c.length:F3} s, averageSpeed {c.averageSpeed} (|{c.averageSpeed.magnitude:F3}| m/s), humano {c.humanMotion}, loop {c.isLooping}");
            var set = Resources.Load<CharacterModelSet>("Personagens/ModelosPersonagens");
            if (set != null) Debug.Log($"[DiagAnimacao] set: idle {(set.idle ? set.idle.name : "-")}, walk {(set.walk ? set.walk.name : "-")}");

            // passada: amostra o clipe num modelo do pacote e mede quanto o pé de apoio anda para trás (clipe no lugar)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Personagens/Quaternius/Homem_Suit.fbx");
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Personagens/Quaternius/Animacoes_Homem.fbx").OfType<AnimationClip>().ToList();
            var go = Object.Instantiate(prefab);
            var an = go.GetComponent<Animator>();
            if (an == null || an.avatar == null || !an.avatar.isHuman) { Debug.Log("[DiagAnimacao] modelo sem avatar humano"); Object.DestroyImmediate(go); return; }
            foreach (var name in new[] { "Walk", "Run" })
            {
                var c = clips.First(x => x.name == name);
                float minZ = float.MaxValue, maxZ = float.MinValue, minX = float.MaxValue, maxX = float.MinValue, hip = 0f;
                for (int i = 0; i <= 60; i++)
                {
                    c.SampleAnimation(go, c.length * i / 60f);
                    var foot = an.GetBoneTransform(HumanBodyBones.LeftFoot).position - go.transform.position;
                    minZ = Mathf.Min(minZ, foot.z); maxZ = Mathf.Max(maxZ, foot.z);
                    minX = Mathf.Min(minX, foot.x); maxX = Mathf.Max(maxX, foot.x);
                    hip = an.GetBoneTransform(HumanBodyBones.Hips).position.y - go.transform.position.y;
                }
                float range = maxZ - minZ;
                Debug.Log($"[DiagAnimacao] {name}: pé esquerdo z {minZ:F3}..{maxZ:F3} (curso {range:F3} m), x {minX:F3}..{maxX:F3}, quadril {hip:F3} m, "
                          + $"velocidade implícita ≈ {2f * range / c.length:F2} m/s (escala 1)");
            }
            Object.DestroyImmediate(go);
        }
    }
}
