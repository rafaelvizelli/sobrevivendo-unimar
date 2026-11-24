using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Objeto que o personagem segura e que aparece na mão (só visual; as regras ficam em PlayerCheats).</summary>
    public enum HandProp { Nenhum, Celular, Colinha, Garrafinha, Bilhete, Fone, Borracha }

    /// <summary>
    /// Monta os objetos de mão com primitivas, um pouco maiores que o real para dar para ler de longe
    /// (a câmera fica atrás do jogador). Eixos do objeto: +Y = "para cima" do objeto, +Z = a face que importa
    /// (tela do celular, lado escrito da colinha).
    /// </summary>
    public static class HandProps
    {
        public static Transform Build(HandProp prop, Transform parent)
        {
            if (prop == HandProp.Nenhum) return null;
            var root = new GameObject("Objeto_" + prop).transform;
            root.SetParent(parent, false);
            switch (prop)
            {
                case HandProp.Celular:
                    Part(PrimitiveType.Cube, "Corpo", root, Vector3.zero, new Vector3(0.085f, 0.165f, 0.018f), new Color(0.06f, 0.06f, 0.08f), 0.8f);
                    Part(PrimitiveType.Cube, "Tela", root, new Vector3(0f, 0.004f, 0.0095f), new Vector3(0.074f, 0.145f, 0.002f), new Color(0.35f, 0.75f, 1f), 0.9f, 1.6f);
                    Part(PrimitiveType.Cube, "Capinha", root, new Vector3(0f, 0f, -0.011f), new Vector3(0.09f, 0.17f, 0.006f), new Color(0.95f, 0.3f, 0.55f), 0.3f);
                    break;
                case HandProp.Colinha:
                    Part(PrimitiveType.Cube, "Papel", root, Vector3.zero, new Vector3(0.13f, 0.17f, 0.004f), new Color(0.98f, 0.97f, 0.9f), 0.1f);
                    for (int i = 0; i < 6; i++)
                        Part(PrimitiveType.Cube, "Linha", root, new Vector3(-0.008f + (i % 2) * 0.012f, 0.06f - i * 0.024f, 0.0025f),
                             new Vector3(0.09f - (i % 3) * 0.015f, 0.007f, 0.001f), new Color(0.15f, 0.25f, 0.75f), 0.1f);
                    break;
                case HandProp.Garrafinha:
                    Part(PrimitiveType.Cylinder, "Garrafa", root, Vector3.zero, new Vector3(0.08f, 0.12f, 0.08f), new Color(0.55f, 0.85f, 1f), 0.9f);
                    Part(PrimitiveType.Cylinder, "Rotulo", root, new Vector3(0f, -0.01f, 0f), new Vector3(0.084f, 0.035f, 0.084f), new Color(0.1f, 0.45f, 0.95f), 0.4f);
                    Part(PrimitiveType.Cylinder, "Tampa", root, new Vector3(0f, 0.135f, 0f), new Vector3(0.032f, 0.015f, 0.032f), new Color(0.1f, 0.3f, 0.9f), 0.4f);
                    break;
                case HandProp.Bilhete:
                    // papel de caderno amassado (malha facetada com as linhas do caderno), o mesmo que voa no arremesso
                    PaperBall.Create(root, PlayerCheats.BallSize);
                    break;
                case HandProp.Borracha:
                    BuildEraser(root);
                    break;
                case HandProp.Fone:
                {
                    // fone de ouvido com fio: o fone vai na orelha, o microfone do fio na mão e o fio desce até o bolso
                    // (as peças são posicionadas a cada quadro pela pele 3D; aqui ficam juntas, para o boneco simples)
                    var branco = new Color(0.97f, 0.97f, 0.97f);
                    var bud = new GameObject("Fone").transform;
                    bud.SetParent(root, false);
                    Part(PrimitiveType.Sphere, "Concha", bud, Vector3.zero, new Vector3(0.032f, 0.032f, 0.026f), branco, 0.8f);
                    Part(PrimitiveType.Cylinder, "Haste", bud, new Vector3(0f, -0.028f, 0f), new Vector3(0.011f, 0.022f, 0.011f), branco, 0.8f);
                    var mic = new GameObject("Microfone").transform;
                    mic.SetParent(root, false);
                    mic.localPosition = new Vector3(0f, -0.08f, 0f);
                    Part(PrimitiveType.Cylinder, "Controle", mic, Vector3.zero, new Vector3(0.016f, 0.022f, 0.016f), branco, 0.6f);
                    Part(PrimitiveType.Sphere, "Grade", mic, new Vector3(0f, 0.022f, 0f), new Vector3(0.012f, 0.012f, 0.012f), new Color(0.2f, 0.2f, 0.22f), 0.4f);
                    Part(PrimitiveType.Cylinder, "Fio", root, new Vector3(0f, -0.04f, 0f), new Vector3(0.005f, 0.04f, 0.005f), branco, 0.5f);
                    Part(PrimitiveType.Cylinder, "Fio2", root, new Vector3(0f, -0.14f, 0f), new Vector3(0.005f, 0.05f, 0.005f), branco, 0.5f);
                    break;
                }
            }
            return root;
        }

        /// <summary>
        /// Borracha escolar de duas cores (metade rosa, metade azul, cantos de leve chanfro), um pouco maior que a real
        /// para dar para ver voando. Usada na mão (favor com mira) e no voo.
        /// </summary>
        public static Transform BuildEraser(Transform parent)
        {
            var root = new GameObject("Borracha").transform;
            if (parent != null) root.SetParent(parent, false);
            var rosa = new Color(0.98f, 0.45f, 0.55f);
            var azul = new Color(0.25f, 0.45f, 0.95f);
            Part(PrimitiveType.Cube, "Rosa", root, new Vector3(-0.02f, 0f, 0f), new Vector3(0.04f, 0.026f, 0.045f), rosa, 0.15f);
            Part(PrimitiveType.Cube, "Azul", root, new Vector3(0.02f, 0f, 0f), new Vector3(0.04f, 0.026f, 0.045f), azul, 0.15f);
            // chanfro: uma tira mais clara no meio (a "emenda" das duas cores)
            Part(PrimitiveType.Cube, "Emenda", root, Vector3.zero, new Vector3(0.004f, 0.0265f, 0.0455f), new Color(0.95f, 0.93f, 0.9f), 0.1f);
            return root;
        }

        /// <summary>Encosta o cilindro (altura 2, raio 0.5 no espaço da peça) entre os pontos a e b do mundo.</summary>
        public static void Segment(Transform part, Vector3 a, Vector3 b, float thickness)
        {
            Vector3 d = b - a;
            part.position = (a + b) * 0.5f;
            part.rotation = d.sqrMagnitude > 1e-8f ? Quaternion.FromToRotation(Vector3.up, d) : Quaternion.identity;
            Vector3 ps = part.parent != null ? part.parent.lossyScale : Vector3.one;
            part.localScale = new Vector3(thickness / ps.x, d.magnitude * 0.5f / ps.y, thickness / ps.z);
        }

        /// <summary>
        /// true se o ponto do mundo está dentro de alguma peça do objeto (com folga <paramref name="margin"/> em metros).
        /// Usado para tirar o objeto de dentro dos dedos e nos testes.
        /// </summary>
        public static bool Touches(Transform prop, Vector3 point, float margin, string onlyChild = null)
        {
            var from = onlyChild != null ? prop.Find(onlyChild) : prop;
            if (from == null) return false;
            foreach (var mf in from.GetComponentsInChildren<MeshFilter>())
            {
                var t = mf.transform;
                Vector3 s = t.lossyScale;
                Vector3 p = t.InverseTransformPoint(point);
                // folga convertida para o espaço da peça (cada eixo tem sua escala)
                float mx = margin / Mathf.Max(1e-5f, Mathf.Abs(s.x)), my = margin / Mathf.Max(1e-5f, Mathf.Abs(s.y)),
                      mz = margin / Mathf.Max(1e-5f, Mathf.Abs(s.z));
                string mesh = mf.sharedMesh != null ? mf.sharedMesh.name : "";
                bool inside;
                if (mesh.StartsWith("Sphere"))
                {
                    float ex = p.x / (0.5f + mx), ey = p.y / (0.5f + my), ez = p.z / (0.5f + mz);
                    inside = ex * ex + ey * ey + ez * ez < 1f;
                }
                else if (mesh.StartsWith("Cylinder"))
                {
                    float ex = p.x / (0.5f + mx), ez = p.z / (0.5f + mz);
                    inside = Mathf.Abs(p.y) < 1f + my && ex * ex + ez * ez < 1f;
                }
                else inside = Mathf.Abs(p.x) < 0.5f + mx && Mathf.Abs(p.y) < 0.5f + my && Mathf.Abs(p.z) < 0.5f + mz;
                if (inside) return true;
            }
            return false;
        }

        static void Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color,
                         float smooth, float emission = 0f, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // sem colisor: não pode atrapalhar a visão do professor
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(color, smooth, emission);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
