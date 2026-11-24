using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// S9 (Operação Steam): a sala com cara de faculdade brasileira em fim de tarde. Tudo criado por código quando a
    /// fase começa (a cena .unity não muda) e tudo SÓ VISUAL: nenhum objeto novo tem colisor, então a NavMesh, a visão
    /// do professor (raios) e a câmera continuam exatamente iguais.
    /// - luz de fim de tarde: sol baixo e alaranjado entrando pelas janelas, luzes do teto um pouco mais fracas;
    /// - ventiladores de teto girando; ar-condicionado pingando num balde;
    /// - cartazes de faculdade nas paredes (textos em <see cref="Falas.Cartazes"/>);
    /// - a estante de metal do fundo (parecia uma escada) vira um armário de aço, no mesmo lugar e com o mesmo colisor;
    /// - o projetor fica menor e mais alto (tapava a lousa) e a lousa ganha letra de giz.
    /// </summary>
    public class Cenografia : MonoBehaviour
    {
        [Header("Luz de fim de tarde")]
        [Tooltip("Rotação do sol: elevação baixa (graus) e vindo das janelas (lado +x)")] public Vector3 sunEuler = new(17f, 248f, 0f);
        public Color sunColor = new(1f, 0.76f, 0.52f);
        public float sunIntensity = 2.5f;
        [Tooltip("As luzes do teto ficam nesta fração (contraste com o sol entrando)")] [Range(0, 1)] public float ceilingLights = 0.72f;
        public Color ambientEquator = new(0.66f, 0.58f, 0.5f);

        [Header("Movimento")]
        [Tooltip("Graus por segundo das hélices dos ventiladores")] public float fanSpeed = 160f;
        [Tooltip("Segundos entre uma gota e outra do ar-condicionado")] public float dripInterval = 1.4f;

        public static Cenografia I { get; private set; }
        public Transform Root { get; private set; }
        public bool Built { get; private set; }

        readonly List<Transform> fans = new();
        Transform drop;
        Vector3 dropStart;
        float dropY, dropSpeed, nextDrop;
        float bucketTop;

        static readonly Color Papel = new(0.96f, 0.94f, 0.86f);
        static readonly Color Branco = new(0.93f, 0.93f, 0.9f);

        void Awake() => I = this;

        void Start() => Build();

        public void Build()
        {
            if (Built) return;
            Built = true;
            Root = new GameObject("Cenografia (S9)").transform;
            Luz();
            LaFora();
            Ventiladores();
            Cartazes();
            ArCondicionado();
            Armario();
            Projetor();
            Lousa();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var f in fans) if (f != null) f.Rotate(0f, fanSpeed * dt, 0f, Space.Self);
            if (drop == null) return;
            if (dropSpeed <= 0f)
            {
                nextDrop -= dt;
                if (nextDrop > 0f) return;
                drop.gameObject.SetActive(true);
                drop.position = dropStart;
                dropY = dropStart.y;
                dropSpeed = 0.01f;
                return;
            }
            dropSpeed += 9.8f * dt;
            dropY -= dropSpeed * dt;
            if (dropY <= bucketTop)
            {
                drop.gameObject.SetActive(false);
                dropSpeed = 0f;
                nextDrop = dripInterval;
                return;
            }
            drop.position = new Vector3(dropStart.x, dropY, dropStart.z);
        }

        // ------------------------------------------------------------------ luz

        void Luz()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.rotation = Quaternion.Euler(sunEuler);
                    l.color = sunColor;
                    l.intensity = sunIntensity;
                }
                else if (l.type == LightType.Point && l.name.StartsWith("Luz ")) l.intensity *= ceilingLights;
            }
            RenderSettings.ambientEquatorColor = ambientEquator;
            // céu de fim de tarde: uma CÓPIA do material do céu (o original é do Unity e não pode mudar)
            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_SkyTint"))
            {
                var tarde = new Material(sky) { name = "Ceu de fim de tarde" };
                tarde.SetColor("_SkyTint", skyTint);
                if (tarde.HasProperty("_AtmosphereThickness")) tarde.SetFloat("_AtmosphereThickness", 1.5f);
                RenderSettings.skybox = tarde;
            }
        }

        [Tooltip("Cor do céu pela janela (fim de tarde)")] public Color skyTint = new(0.85f, 0.62f, 0.5f);

        // ------------------------------------------------------------------ lá fora (pela janela)

        /// <summary>Prédios do campus e árvores do lado de fora das janelas (lado +x), só para dar profundidade.</summary>
        void LaFora()
        {
            var g = Grupo("Campus la fora");
            Peca(PrimitiveType.Cube, "Predio (reitoria)", g, new Vector3(22f, 4f, -6f), new Vector3(8f, 8f, 10f), new Color(0.82f, 0.74f, 0.62f));
            Peca(PrimitiveType.Cube, "Predio (biblioteca)", g, new Vector3(26f, 3f, 9f), new Vector3(10f, 6f, 9f), new Color(0.72f, 0.62f, 0.55f));
            Peca(PrimitiveType.Cube, "Predio (cantina)", g, new Vector3(16f, 1.6f, 14f), new Vector3(5f, 3.2f, 6f), new Color(0.9f, 0.85f, 0.7f));
            // janelinhas dos prédios
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                    Peca(PrimitiveType.Cube, "Janela", g, new Vector3(17.95f, 1.6f + j * 2.2f, -9.5f + i * 2.3f), new Vector3(0.05f, 1.1f, 1.2f), new Color(0.35f, 0.42f, 0.5f), default, 0.8f);
            var rng = new System.Random(11);
            for (int i = 0; i < 7; i++)
            {
                float x = 9.5f + (float)rng.NextDouble() * 5f, z = -12f + i * 4f + (float)rng.NextDouble() * 1.5f;
                float h = 2.2f + (float)rng.NextDouble() * 1.4f;
                Peca(PrimitiveType.Cylinder, "Tronco", g, new Vector3(x, h * 0.5f, z), new Vector3(0.25f, h * 0.5f, 0.25f), new Color(0.4f, 0.28f, 0.18f));
                Peca(PrimitiveType.Sphere, "Copa", g, new Vector3(x, h + 0.9f, z), Vector3.one * (2.2f + (float)rng.NextDouble()), new Color(0.3f, 0.5f + (float)rng.NextDouble() * 0.12f, 0.24f));
            }
            // calçada
            Peca(PrimitiveType.Cube, "Calcada", g, new Vector3(8.2f, 0.01f, 0f), new Vector3(2.2f, 0.02f, 40f), new Color(0.7f, 0.68f, 0.64f));
        }

        // ------------------------------------------------------------------ peças (sem colisor)

        Transform Peca(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, Color color, Vector3 euler = default, float smooth = 0.25f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            DestroyImmediate(go.GetComponent<Collider>());
            var t = go.transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            t.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(color, smooth);
            return t;
        }

        Transform Grupo(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : Root, false);
            return t;
        }

        // ------------------------------------------------------------------ ventiladores

        void Ventiladores()
        {
            foreach (float z in new[] { -2.75f, 2.15f })
            {
                var g = Grupo("Ventilador de teto");
                Peca(PrimitiveType.Cylinder, "Haste", g, new Vector3(0f, 4.0f, z), new Vector3(0.05f, 0.18f, 0.05f), new Color(0.35f, 0.35f, 0.36f));
                Peca(PrimitiveType.Cylinder, "Motor", g, new Vector3(0f, 3.8f, z), new Vector3(0.24f, 0.06f, 0.24f), new Color(0.82f, 0.82f, 0.8f));
                var helice = Grupo("Helice", g);
                helice.position = new Vector3(0f, 3.77f, z);
                for (int i = 0; i < 3; i++)
                {
                    var pa = Grupo("Pa", helice);
                    pa.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
                    var p = Peca(PrimitiveType.Cube, "Lamina", pa, Vector3.zero, new Vector3(0.75f, 0.015f, 0.16f), Branco, new Vector3(8f, 0f, 0f));
                    p.localPosition = new Vector3(0.48f, 0f, 0f);
                    p.localRotation = Quaternion.Euler(8f, 0f, 0f);
                }
                fans.Add(helice);
            }
        }

        // ------------------------------------------------------------------ cartazes

        /// <summary>Cartaz de papel na parede: título datilografado e texto à caneta. Lado visível virado para quem olha.</summary>
        void Cartaz(Vector3 pos, Quaternion rot, Vector2 size, Color paper, string title, string body, float tilt)
        {
            var g = Grupo("Cartaz");
            g.SetPositionAndRotation(pos, rot * Quaternion.Euler(0f, 0f, tilt));
            var papel = Peca(PrimitiveType.Cube, "Papel", g, Vector3.zero, new Vector3(size.x, size.y, 0.006f), paper);
            papel.localPosition = Vector3.zero;
            papel.localRotation = Quaternion.identity;
            // fita crepe nos cantos de cima
            foreach (float s in new[] { -1f, 1f })
            {
                var fita = Peca(PrimitiveType.Cube, "Fita", g, Vector3.zero, new Vector3(0.09f, 0.035f, 0.008f), new Color(0.93f, 0.86f, 0.62f));
                fita.localPosition = new Vector3(s * (size.x * 0.5f - 0.03f), size.y * 0.5f - 0.01f, -0.002f);
                fita.localRotation = Quaternion.Euler(0f, 0f, s * -30f);
            }

            var canvasGo = new GameObject("Texto", typeof(RectTransform), typeof(Canvas));
            var rt = (RectTransform)canvasGo.transform;
            rt.SetParent(g, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            const float pxPorMetro = 400f;
            rt.sizeDelta = size * pxPorMetro;
            rt.localScale = Vector3.one / pxPorMetro;
            rt.localPosition = new Vector3(0f, 0f, -0.005f);
            rt.localRotation = Quaternion.identity;

            var t = Texto("Titulo", rt, title, UITheme.Typewriter, Mathf.RoundToInt(size.x * 50f), UITheme.StampRed, TextAnchor.UpperCenter);
            t.rectTransform.anchorMin = new Vector2(0.06f, 0.62f);
            t.rectTransform.anchorMax = new Vector2(0.94f, 0.94f);
            var b = Texto("Corpo", rt, body, UITheme.Hand, Mathf.RoundToInt(size.x * 52f), UITheme.Pen, TextAnchor.UpperCenter);
            b.rectTransform.anchorMin = new Vector2(0.06f, 0.06f);
            b.rectTransform.anchorMax = new Vector2(0.94f, 0.62f);
        }

        static Text Texto(string name, Transform parent, string text, Font font, int size, Color color, TextAnchor anchor)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(parent, false);
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 8;
            t.resizeTextMaxSize = size;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        void Cartazes()
        {
            // parede da frente (vista de frente por toda a turma): rotação 180° = visível para quem olha para a frente (-z)
            var frente = Quaternion.Euler(0f, 180f, 0f);
            var esquerda = Quaternion.Euler(0f, -90f, 0f); // parede x = -6, vista de dentro da sala
            var fundo = Quaternion.identity;               // parede z = +8, vista de dentro da sala
            var c = Falas.Cartazes;
            Cartaz(new Vector3(-4.85f, 1.95f, -7.99f), frente, new Vector2(0.78f, 1.0f), new Color(1f, 0.93f, 0.55f), c[0].titulo, c[0].texto, 2f);
            Cartaz(new Vector3(4.85f, 1.75f, -7.99f), frente, new Vector2(0.7f, 0.62f), Papel, c[1].titulo, c[1].texto, -3f);
            // no mural de cortiça da parede esquerda, perto da frente
            Cartaz(new Vector3(-5.96f, 1.78f, -4.95f), esquerda, new Vector2(0.6f, 0.78f), new Color(0.75f, 0.9f, 1f), c[2].titulo, c[2].texto, -4f);
            Cartaz(new Vector3(-5.96f, 1.68f, -4.05f), esquerda, new Vector2(0.58f, 0.7f), Papel, c[3].titulo, c[3].texto, 3f);
            Cartaz(new Vector3(-5.97f, 2.05f, 0.6f), esquerda, new Vector2(0.7f, 0.9f), new Color(0.82f, 1f, 0.78f), c[4].titulo, c[4].texto, -2f);
            // fundo da sala
            Cartaz(new Vector3(-2.4f, 2.0f, 7.99f), fundo, new Vector2(0.8f, 1.0f), new Color(1f, 0.85f, 0.85f), c[5].titulo, c[5].texto, 2.5f);
            Cartaz(new Vector3(1.3f, 1.9f, 7.99f), fundo, new Vector2(0.7f, 0.8f), Papel, c[6].titulo, c[6].texto, -3f);
        }

        // ------------------------------------------------------------------ ar-condicionado pingando

        void ArCondicionado()
        {
            var g = Grupo("Ar-condicionado pingando");
            const float x = -5.86f, z = 2.7f;
            Peca(PrimitiveType.Cube, "Corpo", g, new Vector3(x, 3.38f, z), new Vector3(0.24f, 0.32f, 1.0f), new Color(0.95f, 0.95f, 0.93f), default, 0.5f);
            Peca(PrimitiveType.Cube, "Saida", g, new Vector3(x + 0.12f, 3.27f, z), new Vector3(0.02f, 0.05f, 0.9f), new Color(0.55f, 0.57f, 0.6f));
            Peca(PrimitiveType.Cube, "Luz", g, new Vector3(x + 0.121f, 3.44f, z + 0.38f), new Vector3(0.01f, 0.02f, 0.03f), new Color(0.2f, 0.9f, 0.3f), default, 0.9f);
            // o balde azul embaixo (a solução oficial de toda faculdade)
            bucketTop = 0.32f;
            Peca(PrimitiveType.Cylinder, "Balde", g, new Vector3(-5.66f, 0.16f, z), new Vector3(0.3f, 0.16f, 0.3f), new Color(0.2f, 0.45f, 0.85f));
            Peca(PrimitiveType.Cylinder, "Agua", g, new Vector3(-5.66f, 0.3f, z), new Vector3(0.27f, 0.005f, 0.27f), new Color(0.55f, 0.75f, 0.95f), default, 0.95f);
            dropStart = new Vector3(-5.66f, 3.2f, z);
            drop = Peca(PrimitiveType.Sphere, "Gota", g, dropStart, Vector3.one * 0.03f, new Color(0.6f, 0.8f, 1f), default, 0.95f);
            drop.gameObject.SetActive(false);
            nextDrop = Random.Range(0f, dripInterval);
        }

        // ------------------------------------------------------------------ armário no lugar da estante

        void Armario()
        {
            var rack = GameObject.Find("rack");
            if (rack == null) return;
            // só a APARÊNCIA muda: o colisor da estante continua lá (NavMesh e visão iguais)
            foreach (var r in rack.GetComponentsInChildren<Renderer>()) r.enabled = false;
            var g = Grupo("Armario de aco");
            Color aco = new(0.66f, 0.64f, 0.58f);
            Peca(PrimitiveType.Cube, "Corpo", g, new Vector3(5.51f, 1.0f, 7.69f), new Vector3(0.94f, 2.0f, 0.55f), aco, default, 0.45f);
            Peca(PrimitiveType.Cube, "Fresta", g, new Vector3(5.51f, 1.0f, 7.41f), new Vector3(0.012f, 1.9f, 0.01f), aco * 0.55f);
            foreach (float s in new[] { -1f, 1f })
                Peca(PrimitiveType.Cube, "Puxador", g, new Vector3(5.51f + s * 0.06f, 1.1f, 7.4f), new Vector3(0.02f, 0.14f, 0.02f), new Color(0.25f, 0.25f, 0.27f), default, 0.7f);
            // em cima: caixas de provas antigas
            Peca(PrimitiveType.Cube, "Caixa", g, new Vector3(5.4f, 2.14f, 7.7f), new Vector3(0.5f, 0.28f, 0.4f), new Color(0.75f, 0.6f, 0.4f), new Vector3(0f, 8f, 0f));
            Peca(PrimitiveType.Cube, "Caixa", g, new Vector3(5.62f, 2.39f, 7.7f), new Vector3(0.42f, 0.22f, 0.36f), new Color(0.7f, 0.55f, 0.36f), new Vector3(0f, -6f, 0f));
            var c = Falas.Cartazes[7];
            Cartaz(new Vector3(5.51f, 1.45f, 7.405f), Quaternion.identity, // porta do armário, vista de dentro da sala (olhando para +z)
                   new Vector2(0.4f, 0.3f), Papel, c.titulo, c.texto, -5f);
        }

        // ------------------------------------------------------------------ projetor e lousa

        void Projetor()
        {
            var proj = GameObject.Find("projector");
            if (proj == null) return;
            var rs = proj.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds antes = Juntar(rs);
            proj.transform.localScale *= 0.72f;
            Bounds depois = Juntar(rs);
            // encolhe em direção ao teto: o ponto de cima (onde ele se prende) fica no mesmo lugar
            Vector3 topoAntes = new(antes.center.x, antes.max.y, antes.center.z);
            Vector3 topoDepois = new(depois.center.x, depois.max.y, depois.center.z);
            proj.transform.position += topoAntes - topoDepois;
        }

        static Bounds Juntar(Renderer[] rs)
        {
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        // giz: branco meio gasto e amarelo (o destaque da resposta)
        static readonly Color CorGiz = new(0.97f, 0.97f, 0.93f, 0.92f), CorGizAmarelo = new(1f, 0.92f, 0.55f, 0.95f);

        /// <summary>
        /// A lousa vira a piada de tiozão do professor (textos em <see cref="Falas"/>): pergunta em giz branco, resposta
        /// grande em giz amarelo sublinhada, assinatura dele e, à direita, um 0 e um 8 desenhados — o 8 de cinto.
        /// Tudo no canvas da lousa que já existe na cena (680×200, 1 unidade = 1 cm); a cena não é alterada.
        /// </summary>
        void PiadaNaLousa(Text t)
        {
            var canvas = (RectTransform)t.canvas.transform;
            t.text = "";
            t.enabled = false; // o texto antigo some; os novos ficam em filhos com posição própria
            Text Giz(string nome, string texto, int tamanho, Color cor, float x, float y, float w, float h, TextAnchor anc = TextAnchor.UpperLeft)
            {
                var l = new GameObject(nome, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                l.transform.SetParent(canvas, false);
                l.font = UITheme.Hand;
                l.fontSize = tamanho;
                l.color = cor;
                l.alignment = anc;
                l.text = texto;
                l.horizontalOverflow = HorizontalWrapMode.Wrap;
                l.verticalOverflow = VerticalWrapMode.Overflow;
                l.raycastTarget = false;
                var r = l.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(x, -y);
                r.sizeDelta = new Vector2(w, h);
                return l;
            }
            Image Traco(string nome, Color cor, float x, float y, float w, float h, float giro = 0f)
            {
                var img = new GameObject(nome, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.transform.SetParent(canvas, false);
                img.color = cor;
                img.raycastTarget = false;
                var r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(x, -y);
                r.sizeDelta = new Vector2(w, h);
                r.localRotation = Quaternion.Euler(0f, 0f, giro);
                return img;
            }

            // texto (à esquerda): começa um pouco abaixo do topo, onde o projetor fica pendurado
            Giz("Piada Pergunta", Falas.LousaPergunta, 34, CorGiz, 18f, 16f, 470f, 50f);
            Giz("Piada Resposta", Falas.LousaResposta, 50, CorGizAmarelo, 40f, 66f, 440f, 64f);
            Traco("Sublinhado", CorGizAmarelo, 205f, 128f, 330f, 3f, -1.2f);
            Traco("Sublinhado 2", CorGizAmarelo, 215f, 134f, 260f, 2f, -0.6f);
            Giz("Piada Assinatura", Falas.LousaAssinatura, 24, CorGiz, 230f, 146f, 260f, 34f);

            // desenho (à direita): o 0 olhando para o 8, que está de cinto com fivela
            Giz("Desenho Zero", "0", 120, CorGiz, 498f, 22f, 80f, 150f, TextAnchor.UpperCenter);
            Giz("Desenho Oito", "8", 120, CorGiz, 580f, 22f, 80f, 150f, TextAnchor.UpperCenter);
            // olhinhos (o 0 olha para a direita, para o cinto do 8)
            Traco("Olho Zero E", CorGiz, 539f, 96f, 5f, 5f);
            Traco("Olho Zero D", CorGiz, 550f, 96f, 5f, 5f);
            Traco("Olho Oito E", CorGiz, 631f, 86f, 4f, 4f);
            Traco("Olho Oito D", CorGiz, 641f, 86f, 4f, 4f);
            // cinto na "cintura" do 8 + fivela (quadradinho vazado)
            Traco("Cinto", CorGizAmarelo, 629f, 107f, 36f, 6f, 3f);
            float fx = 629f, fy = 107f;
            Traco("Fivela Cima", CorGiz, fx, fy - 6f, 12f, 2f);
            Traco("Fivela Baixo", CorGiz, fx, fy + 6f, 12f, 2f);
            Traco("Fivela Esq", CorGiz, fx - 5f, fy, 2f, 13f);
            Traco("Fivela Dir", CorGiz, fx + 5f, fy, 2f, 13f);
            // "brilho" de admiração do 0 (três tracinhos)
            Traco("Brilho 1", CorGiz, 572f, 76f, 9f, 2f, 35f);
            Traco("Brilho 2", CorGiz, 576f, 84f, 9f, 2f, 0f);
            Traco("Brilho 3", CorGiz, 572f, 92f, 9f, 2f, -35f);
        }

        /// <summary>Os textos da lousa agora (testes).</summary>
        public static string LousaTexto()
        {
            var q = GameObject.Find("Questao no Quadro");
            if (q == null) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var t in q.GetComponentsInChildren<Text>()) if (t.enabled && t.text.Length > 0) sb.Append(t.text).Append('\n');
            return sb.ToString();
        }

        void Lousa()
        {
            var quadro = GameObject.Find("Questao no Quadro");
            if (quadro == null) return;
            var t = quadro.GetComponentInChildren<Text>();
            if (t == null) return;
            t.font = UITheme.Hand; // letra de giz
            t.fontSize = 26;
            t.color = new Color(0.97f, 0.97f, 0.93f, 0.93f);
            t.lineSpacing = 0.9f;
            PiadaNaLousa(t);
            // giz e apagador no parapeito da lousa
            var g = Grupo("Giz e apagador");
            var board = GameObject.Find("board");
            if (board == null) return;
            var b = Juntar(board.GetComponentsInChildren<Renderer>());
            float y = b.min.y + 0.05f, z = b.max.z + 0.05f;
            Peca(PrimitiveType.Cube, "Apagador", g, new Vector3(1.9f, y + 0.03f, z), new Vector3(0.2f, 0.05f, 0.07f), new Color(0.45f, 0.3f, 0.2f));
            Peca(PrimitiveType.Cube, "Feltro", g, new Vector3(1.9f, y + 0.003f, z), new Vector3(0.19f, 0.008f, 0.065f), new Color(0.3f, 0.3f, 0.32f));
            Peca(PrimitiveType.Cylinder, "Giz", g, new Vector3(-1.4f, y + 0.008f, z), new Vector3(0.012f, 0.04f, 0.012f), Color.white, new Vector3(0f, 0f, 90f));
            Peca(PrimitiveType.Cylinder, "Giz", g, new Vector3(-1.25f, y + 0.008f, z + 0.01f), new Vector3(0.012f, 0.025f, 0.012f), new Color(1f, 0.85f, 0.9f), new Vector3(0f, 30f, 90f));
        }
    }
}
