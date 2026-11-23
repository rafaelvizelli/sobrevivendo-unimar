using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Figurino do modelo 3D: troca de cores por material, cabeção de comédia e enfeites de primitivas (bigode, óculos,
    /// barriga, cordão...). Os enfeites são posicionados com o modelo ainda na pose original (de pé, olhando para a
    /// frente) e presos no osso, então acompanham a cabeça/tronco animados.
    /// </summary>
    public static class CharacterLooks
    {
        /// <summary>Clareia as cores do FBX (vêm escuras do pacote) para combinar com a sala.</summary>
        public const float ColorBoost = 1.0f;

        public static void Dress(MonoBehaviour owner, Transform model, Transform head, Transform neck, Transform spine,
                                 CharacterModelSet.Entry entry, CharacterModelSet set, int seed, float scale)
        {
            Recolor(model, entry, set, seed);
            var root = owner.transform;
            // a barriga do professor tem a cor do paletó (a troca de cor "Suit"), senão branca
            Color belly = new(0.93f, 0.93f, 0.9f);
            if (entry.tints != null)
                foreach (var t in entry.tints)
                    if (t.material == "Suit") belly = t.color;
            Build(entry.accessories, root, head, neck, spine, scale, belly);
            if (entry.accessories.HasFlag(Accessory.Sobrancelhas)) BuildBrows(owner, root, head, scale);
            if (entry.headScale > 0f && Mathf.Abs(entry.headScale - 1f) > 0.001f) head.localScale *= entry.headScale;
        }

        static readonly Color SobrancelhaCor = new(0.2f, 0.19f, 0.2f);

        /// <summary>
        /// S4: sobrancelhas grossas (bem maiores que as reais, para ler pela câmera do jogo a 10 m) presas na cabeça, com
        /// o componente que as faz atuar conforme a expressão do personagem. Criadas antes do cabeção, como os outros
        /// enfeites, para crescer junto com a cabeça.
        /// </summary>
        static void BuildBrows(MonoBehaviour owner, Transform root, Transform head, float scale)
        {
            var left = Part(PrimitiveType.Cube, "SobrancelhaE", root, head, new Vector3(-0.047f, 0.131f, 0.122f), new Vector3(0.085f, 0.03f, 0.03f), SobrancelhaCor, scale);
            var right = Part(PrimitiveType.Cube, "SobrancelhaD", root, head, new Vector3(0.047f, 0.131f, 0.122f), new Vector3(0.085f, 0.03f, 0.03f), SobrancelhaCor, scale);
            var brows = owner.gameObject.AddComponent<ExpressiveBrows>();
            brows.Init(owner.GetComponentInParent<CharacterVisual>(), left, right, head.InverseTransformVector(root.up) * scale);
        }

        static string Clean(string matName) => matName.Replace(" (Instance)", "").Trim();

        static void Recolor(Transform model, CharacterModelSet.Entry entry, CharacterModelSet set, int seed)
        {
            string shirt = entry.randomShirt ? set.ShirtOf(SourceOf(model, set, entry)) : null;
            Color shirtColor = set.ShirtColor(seed);
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = Clean(mats[i].name);
                    Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : mats[i].color;
                    c = c.gamma; // o pacote grava as cores em espaço linear
                    c = new Color(Mathf.Clamp01(c.r * ColorBoost), Mathf.Clamp01(c.g * ColorBoost), Mathf.Clamp01(c.b * ColorBoost));
                    if (shirt != null && n == shirt) c = shirtColor;
                    if (entry.tints != null)
                        foreach (var t in entry.tints)
                            if (t.material == n) c = t.color;
                    mats[i] = MaterialLibrary.Get(c, 0.2f);
                }
                r.sharedMaterials = mats;
            }
        }

        /// <summary>O prefab de onde este modelo veio (para achar o material de roupa dos figurantes).</summary>
        static GameObject SourceOf(Transform model, CharacterModelSet set, CharacterModelSet.Entry entry)
        {
            // o modelo instanciado tem as mesmas malhas do prefab: compara pela primeira malha
            var mine = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (mine == null || entry.models == null) return null;
            foreach (var m in entry.models)
            {
                if (m == null) continue;
                var r = m.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (r != null && r.sharedMesh == mine.sharedMesh) return m;
            }
            return null;
        }

        // ------------------------------------------------------------------ enfeites

        static readonly Color Preto = new(0.05f, 0.05f, 0.06f);
        static readonly Color Ouro = new(1f, 0.78f, 0.15f);
        static readonly Color Vermelho = new(0.85f, 0.08f, 0.1f);
        static readonly Color Rosa = new(1f, 0.5f, 0.75f);
        static readonly Color Bigode = new(0.22f, 0.13f, 0.07f);

        static void Build(Accessory a, Transform root, Transform head, Transform neck, Transform spine, float scale, Color bellyColor)
        {
            if (a == Accessory.Nenhum) return;
            // pontos de referência (m, modelo em escala 1), relativos ao osso: x direita, y cima, z frente
            if (a.HasFlag(Accessory.Bigode))
            {
                Part(PrimitiveType.Cube, "Bigode", root, head, new Vector3(0f, 0.062f, 0.122f), new Vector3(0.1f, 0.032f, 0.03f), Bigode, scale);
                Part(PrimitiveType.Cube, "BigodePonta", root, head, new Vector3(0.062f, 0.052f, 0.115f), new Vector3(0.05f, 0.024f, 0.024f), Bigode, scale, new Vector3(0f, 0f, -25f));
                Part(PrimitiveType.Cube, "BigodePonta", root, head, new Vector3(-0.062f, 0.052f, 0.115f), new Vector3(0.05f, 0.024f, 0.024f), Bigode, scale, new Vector3(0f, 0f, 25f));
            }
            if (a.HasFlag(Accessory.Oculos))
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Part(PrimitiveType.Cylinder, "OculosAro", root, head, new Vector3(0.045f * s, 0.12f, 0.122f), new Vector3(0.068f, 0.006f, 0.068f), Preto, scale, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Cylinder, "OculosLente", root, head, new Vector3(0.045f * s, 0.12f, 0.127f), new Vector3(0.05f, 0.003f, 0.05f), new Color(0.85f, 0.95f, 1f), scale, new Vector3(90f, 0f, 0f), 0.4f);
                }
                Part(PrimitiveType.Cube, "OculosPonte", root, head, new Vector3(0f, 0.125f, 0.126f), new Vector3(0.03f, 0.008f, 0.008f), Preto, scale);
            }
            if (a.HasFlag(Accessory.OculosEscuros))
            {
                for (int s = -1; s <= 1; s += 2)
                    Part(PrimitiveType.Cube, "OculosEscuros", root, head, new Vector3(0.042f * s, 0.12f, 0.124f), new Vector3(0.062f, 0.036f, 0.012f), Preto, scale);
                Part(PrimitiveType.Cube, "OculosBarra", root, head, new Vector3(0f, 0.133f, 0.125f), new Vector3(0.16f, 0.01f, 0.01f), Preto, scale);
            }
            if (a.HasFlag(Accessory.MascaraDeDormir))
            {
                Part(PrimitiveType.Cube, "Mascara", root, head, new Vector3(0f, 0.19f, 0.11f), new Vector3(0.15f, 0.05f, 0.02f), Rosa, scale, new Vector3(-15f, 0f, 0f));
                Part(PrimitiveType.Cube, "Elastico", root, head, new Vector3(0f, 0.19f, 0f), new Vector3(0.175f, 0.015f, 0.2f), Rosa * 0.85f, scale);
            }
            if (a.HasFlag(Accessory.Bone))
            {
                var cor = new Color(0.1f, 0.55f, 0.25f);
                Part(PrimitiveType.Sphere, "Bone", root, head, new Vector3(0f, 0.2f, -0.01f), new Vector3(0.2f, 0.11f, 0.22f), cor, scale);
                Part(PrimitiveType.Cube, "BoneAba", root, head, new Vector3(0f, 0.19f, -0.15f), new Vector3(0.14f, 0.012f, 0.1f), cor, scale, new Vector3(-10f, 0f, 0f));
            }
            if (a.HasFlag(Accessory.Topete))
                Part(PrimitiveType.Sphere, "Topete", root, head, new Vector3(0f, 0.25f, 0.07f), new Vector3(0.12f, 0.08f, 0.12f), new Color(0.95f, 0.8f, 0.35f), scale);
            if (a.HasFlag(Accessory.GravataBorboleta))
            {
                Part(PrimitiveType.Cube, "Borboleta", root, neck, new Vector3(-0.03f, -0.01f, 0.085f), new Vector3(0.05f, 0.04f, 0.02f), Vermelho, scale, new Vector3(0f, 0f, 20f));
                Part(PrimitiveType.Cube, "Borboleta", root, neck, new Vector3(0.03f, -0.01f, 0.085f), new Vector3(0.05f, 0.04f, 0.02f), Vermelho, scale, new Vector3(0f, 0f, -20f));
            }
            if (a.HasFlag(Accessory.Gravata))
            {
                Part(PrimitiveType.Cube, "GravataNo", root, neck, new Vector3(0f, -0.03f, 0.12f), new Vector3(0.045f, 0.04f, 0.03f), Vermelho, scale);
                Part(PrimitiveType.Cube, "Gravata", root, neck, new Vector3(0f, -0.15f, 0.135f), new Vector3(0.05f, 0.2f, 0.015f), Vermelho, scale, new Vector3(-12f, 0f, 0f));
            }
            if (a.HasFlag(Accessory.Corrente))
            {
                for (int s = -1; s <= 1; s += 2)
                    Part(PrimitiveType.Cube, "Corrente", root, neck, new Vector3(0.05f * s, -0.07f, 0.1f), new Vector3(0.012f, 0.13f, 0.012f), Ouro, scale, new Vector3(0f, 0f, 30f * s), 0.85f);
                Part(PrimitiveType.Cube, "Pingente", root, neck, new Vector3(0f, -0.14f, 0.12f), new Vector3(0.05f, 0.06f, 0.012f), Ouro, scale, default, 0.85f);
            }
            if (a.HasFlag(Accessory.Barriga))
            {
                // pança na cor do paletó, com dois botões heroicos segurando tudo
                Part(PrimitiveType.Sphere, "Barriga", root, spine, new Vector3(0f, 0.1f, 0.05f), new Vector3(0.26f, 0.23f, 0.2f), bellyColor, scale);
                Part(PrimitiveType.Sphere, "Botao", root, spine, new Vector3(0f, 0.15f, 0.148f), new Vector3(0.024f, 0.024f, 0.012f), Ouro, scale, default, 0.8f);
                Part(PrimitiveType.Sphere, "Botao", root, spine, new Vector3(0f, 0.07f, 0.15f), new Vector3(0.024f, 0.024f, 0.012f), Ouro, scale, default, 0.8f);
            }
        }

        static Transform Part(PrimitiveType type, string name, Transform root, Transform bone, Vector3 offset, Vector3 size, Color color,
                              float scale, Vector3 euler = default, float smooth = 0.25f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var t = go.transform;
            t.position = bone.position + root.TransformDirection(offset) * scale;
            t.rotation = root.rotation * Quaternion.Euler(euler);
            t.localScale = size * scale;
            t.SetParent(bone, true);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(color, smooth);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
    }
}
