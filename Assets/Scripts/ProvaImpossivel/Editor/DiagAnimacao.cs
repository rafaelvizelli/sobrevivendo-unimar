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
            Pelve();
        }

        /// <summary>Giro da pelve (linha entre as coxas) e dos ombros, em graus, na pose de repouso e em cada clipe.</summary>
        public static void Pelve()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Personagens/Quaternius/Animacoes_Homem.fbx").OfType<AnimationClip>().ToList();
            foreach (var modelo in new[] { "Homem_Suit", "Homem_Casual" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Personagens/Quaternius/" + modelo + ".fbx");
                var go = Object.Instantiate(prefab);
                var an = go.GetComponent<Animator>();
                string Medir()
                {
                    Vector3 pel = an.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - an.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                    Vector3 omb = an.GetBoneTransform(HumanBodyBones.RightUpperArm).position - an.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                    float p = Vector3.SignedAngle(Vector3.right, Vector3.ProjectOnPlane(pel, Vector3.up), Vector3.up);
                    float o = Vector3.SignedAngle(Vector3.right, Vector3.ProjectOnPlane(omb, Vector3.up), Vector3.up);
                    return $"pelve {p,7:F1}°  ombros {o,7:F1}°";
                }
                Debug.Log($"[Pelve] {modelo} repouso (sem animação): {Medir()}");
                foreach (var nome in new[] { "Idle", "Walk", "Run", "Idle_Neutral" })
                {
                    var c = clips.FirstOrDefault(x => x.name == nome);
                    if (c == null) continue;
                    for (int i = 0; i < 4; i++)
                    {
                        c.SampleAnimation(go, c.length * i / 4f);
                        Debug.Log($"[Pelve] {modelo} {nome} t={i / 4f:F2}: {Medir()}");
                    }
                }
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Para onde aponta cada pé (graus em torno do "cima"; 0 = frente, + = para a direita), cru e com a pelve alinhada.</summary>
        public static void Pes()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Personagens/Quaternius/Animacoes_Homem.fbx").OfType<AnimationClip>().ToList();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Personagens/Quaternius/Homem_Suit.fbx");
            var go = Object.Instantiate(prefab);
            var an = go.GetComponent<Animator>();
            Transform Fim(Transform t) { while (t.childCount > 0) t = t.GetChild(0); return t; }
            var pe = new[] { an.GetBoneTransform(HumanBodyBones.LeftFoot), an.GetBoneTransform(HumanBodyBones.RightFoot) };
            var ponta = new[] { Fim(pe[0]), Fim(pe[1]) };
            var hips = an.GetBoneTransform(HumanBodyBones.Hips);
            var spine = an.GetBoneTransform(HumanBodyBones.Spine);
            var ul = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var ur = an.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Debug.Log($"[Pes] ponta do pé: {ponta[0].name} / {ponta[1].name}");
            // eixo "dobradiça" de cada osso (o que aponta para a direita no repouso): não muda quando o osso só inclina
            var ossos = new[] { ul, an.GetBoneTransform(HumanBodyBones.LeftLowerLeg), pe[0], ur, an.GetBoneTransform(HumanBodyBones.RightLowerLeg), pe[1] };
            var eixo = ossos.Select(o => o.InverseTransformDirection(Vector3.right)).ToArray();
            string Med()
            {
                string r = "";
                for (int k = 0; k < 6; k++)
                {
                    Vector3 d = Vector3.ProjectOnPlane(ossos[k].TransformDirection(eixo[k]), Vector3.up);
                    r += $"{(k < 3 ? "E" : "D")}{"cjp"[k % 3]} {Vector3.SignedAngle(Vector3.right, d, Vector3.up),6:F1} ";
                }
                return r;
            }
            Debug.Log($"[Pes] repouso: {Med()}");
            foreach (var nome in new[] { "Idle", "Walk", "Run" })
            {
                var c = clips.First(x => x.name == nome);
                for (int i = 0; i < 8; i++)
                {
                    c.SampleAnimation(go, c.length * i / 8f);
                    string cru = Med();
                    float ang = CharacterModelSkin.PelvisYaw(ul.position, ur.position, Vector3.right, Vector3.up);
                    var q = Quaternion.AngleAxis(-ang, Vector3.up);
                    hips.rotation = q * hips.rotation;
                    spine.rotation = Quaternion.Inverse(q) * spine.rotation;
                    Debug.Log($"[Pes] {nome} t={i / 8f:F2} pelve {ang,6:F1}°  cru: {cru} | alinhado: {Med()}");
                }
            }
            Object.DestroyImmediate(go);
        }
    }
}
