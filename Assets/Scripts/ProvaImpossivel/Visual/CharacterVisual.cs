using UnityEngine;

namespace ProvaImpossivel
{
    public enum CharacterLook { Jogador, Professor, Nerd, Dorminhoco, Fundao, PuxaSaco, Figurante }
    public enum Expression { Neutro, Nervoso, Desconfiado, Bravo, Feliz, Dormindo, Assustado, Convencido }

    /// <summary>
    /// Boneco provisório feito de primitivas + animação procedural (andar, sentar, escrever, dormir, falar, expressões).
    /// Os comportamentos só alteram os campos de "Estado"; para trocar por um modelo definitivo,
    /// preencha <see cref="modelPrefab"/> e rode o menu "Prova Impossível > Montar Fase 1".
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        [Header("Aparência")]
        public CharacterLook look;
        public int seed;
        [Tooltip("Modelo 3D definitivo (opcional). Se vazio, usa o boneco de primitivas.")]
        public GameObject modelPrefab;

        [Header("Estado (controlado pelos comportamentos)")]
        public bool seated;
        public bool writing;
        public bool sleeping;
        public Expression expression;
        [Range(0, 1)] public float sweat;
        public float headYaw;
        public float headPitch;
        [Range(0, 1)] public float handRaise;
        [Range(0, 1)] public float phone;
        [Range(0, 1)] public float armsCrossed;
        public float leanBack;
        [Tooltip("Objeto na mão (celular, colinha, garrafinha, bilhete): só visual")]
        public HandProp prop;

        Transform primitiveProp;
        HandProp primitivePropKind;

        Transform body, head, shoulderL, shoulderR, hipL, hipR, kneeL, kneeR, browL, browR, mouth, lidL, lidR, sweatRoot;
        Vector3 lidBaseL, lidBaseR;
        float talkUntil, speed, phase, curYaw, curPitch, nextBlink, blinkUntil;
        int lastStep;
        [Tooltip("Volume dos passos (o professor aumenta quando investiga)")] public float footstepVolume = 1f;
        Vector3 lastPos;

        public Transform Head => head != null ? head : transform;
        public Vector3 HeadPosition => head != null ? head.position + Vector3.up * 0.2f : transform.position + Vector3.up * 1.6f;

        public Vector3 HeadForward
        {
            get
            {
                var f = head != null ? head.forward : transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : transform.forward;
            }
        }

        public void Talk(float seconds) => talkUntil = Time.time + seconds;

        // estado lido pela pele 3D (CharacterModelSkin)
        public float CurrentYaw => curYaw;
        public float CurrentPitch => curPitch;
        public float Speed => speed;
        public bool IsWalking => !seated && speed > 0.15f;
        public bool IsTalking => Application.isPlaying && Time.time < talkUntil;
        public Transform SweatRoot => sweatRoot;

        /// <summary>Pele 3D em uso (null = boneco de primitivas visível).</summary>
        public CharacterModelSkin Skin { get; private set; }

        void Awake()
        {
            Bind();
            lastPos = transform.position;
            if (Application.isPlaying) ApplyModel();
        }

        /// <summary>Se houver modelo 3D para este visual, veste o personagem com ele (o boneco fica invisível).</summary>
        void ApplyModel()
        {
            if (Skin != null || body == null) return;
            var set = CharacterModelSet.Active;
            if (set == null) return;
            var prefab = set.Pick(look, seed, out var entry);
            if (prefab == null) return;
            var skin = gameObject.AddComponent<CharacterModelSkin>();
            if (skin.Init(this, set, prefab, entry)) Skin = skin;
            else Destroy(skin);
        }

        void LateUpdate() => Animate(Time.deltaTime);

        // ------------------------------------------------------------------ build

        public void Build()
        {
            var old = transform.Find("Visual");
            if (old != null) DestroyImmediate(old.gameObject);

            var v = Pivot("Visual", transform, Vector3.zero);
            if (modelPrefab != null)
            {
                Instantiate(modelPrefab, v);
                Bind();
                return;
            }

            var rnd = new System.Random(seed * 7919 + (int)look * 104729);
            var p = PaletteFor(look, rnd);
            float wide = look == CharacterLook.Fundao ? 1.15f : look == CharacterLook.Nerd ? 0.9f : 1f;
            if (look == CharacterLook.Professor) v.localScale = Vector3.one * 1.06f;

            body = Pivot("Body", v, new Vector3(0, 0.85f, 0));
            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0, 0.3f, 0), new Vector3(0.44f * wide, 0.33f, 0.3f), p.shirt);

            // pernas (coxa + canela com joelho)
            for (int s = -1; s <= 1; s += 2)
            {
                var hip = Pivot(s < 0 ? "HipL" : "HipR", body, new Vector3(0.11f * s * wide, 0, 0));
                Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0, -0.21f, 0), new Vector3(0.16f, 0.22f, 0.16f), p.pants);
                var knee = Pivot(s < 0 ? "KneeL" : "KneeR", hip, new Vector3(0, -0.43f, 0));
                Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0, -0.2f, 0), new Vector3(0.14f, 0.2f, 0.14f), p.pants);
                Part(PrimitiveType.Cube, "Shoe", knee, new Vector3(0, -0.4f, 0.05f), new Vector3(0.13f, 0.07f, 0.24f), p.shoes);
            }

            // braços
            for (int s = -1; s <= 1; s += 2)
            {
                var sh = Pivot(s < 0 ? "ShoulderL" : "ShoulderR", body, new Vector3(0.27f * s * wide, 0.53f, 0));
                Part(PrimitiveType.Capsule, "Arm", sh, new Vector3(0, -0.26f, 0), new Vector3(0.12f, 0.27f, 0.12f), p.sleeve);
                Part(PrimitiveType.Sphere, "Hand", sh, new Vector3(0, -0.55f, 0), Vector3.one * 0.1f, p.skin);
            }

            // cabeça (grande, caricata)
            var neck = Pivot("Neck", body, new Vector3(0, 0.6f, 0));
            head = Pivot("Head", neck, new Vector3(0, 0.04f, 0));
            Part(PrimitiveType.Sphere, "Skull", head, new Vector3(0, 0.2f, 0), new Vector3(0.44f, 0.44f, 0.42f), p.skin);
            Part(PrimitiveType.Sphere, "Nose", head, new Vector3(0, 0.17f, 0.21f), new Vector3(0.07f, 0.07f, 0.08f), p.skin * 0.93f);
            for (int s = -1; s <= 1; s += 2)
            {
                Part(PrimitiveType.Sphere, "Ear", head, new Vector3(0.215f * s, 0.2f, 0), new Vector3(0.06f, 0.1f, 0.08f), p.skin);
                Part(PrimitiveType.Sphere, "Eye", head, new Vector3(0.08f * s, 0.24f, 0.175f), new Vector3(0.1f, 0.1f, 0.06f), Color.white, 0.6f);
                Part(PrimitiveType.Sphere, "Pupil", head, new Vector3(0.08f * s, 0.24f, 0.205f), new Vector3(0.045f, 0.045f, 0.03f), new Color(0.05f, 0.05f, 0.05f), 0.8f);
                Part(PrimitiveType.Sphere, s < 0 ? "LidL" : "LidR", head, new Vector3(0.08f * s, 0.24f, 0.185f), new Vector3(0.11f, 0.11f, 0.07f), p.skin);
                Part(PrimitiveType.Cube, s < 0 ? "BrowL" : "BrowR", head, new Vector3(0.08f * s, 0.32f, 0.19f), new Vector3(0.1f, 0.022f, 0.025f), p.brow);
            }
            Part(PrimitiveType.Cube, "Mouth", head, new Vector3(0, 0.08f, 0.19f), new Vector3(0.11f, 0.025f, 0.03f), new Color(0.35f, 0.07f, 0.08f));

            sweatRoot = Pivot("Sweat", head, Vector3.zero);
            var drop = new Color(0.55f, 0.85f, 1f);
            Part(PrimitiveType.Sphere, "Drop1", sweatRoot, new Vector3(0.21f, 0.33f, 0.08f), new Vector3(0.045f, 0.065f, 0.045f), drop, 0.9f);
            Part(PrimitiveType.Sphere, "Drop2", sweatRoot, new Vector3(-0.2f, 0.29f, 0.1f), new Vector3(0.04f, 0.06f, 0.04f), drop, 0.9f);
            sweatRoot.gameObject.SetActive(false);

            BuildAccessories(look, p, rnd, wide);
            Bind();
            Animate(0f);
        }

        void BuildAccessories(CharacterLook l, Palette p, System.Random rnd, float wide)
        {
            var black = new Color(0.08f, 0.08f, 0.08f);
            switch (l)
            {
                case CharacterLook.Jogador:
                    Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0, 0.34f, -0.02f), new Vector3(0.47f, 0.24f, 0.45f), p.hair);
                    for (int i = 0; i < 4; i++)
                        Part(PrimitiveType.Cube, "Spike", head, new Vector3(-0.12f + i * 0.08f, 0.44f, 0.05f - i * 0.03f), new Vector3(0.07f, 0.12f, 0.07f), p.hair, 0.25f, new Vector3(-20 + i * 12, i * 25, -25 + i * 18));
                    Part(PrimitiveType.Sphere, "Hood", body, new Vector3(0, 0.58f, -0.15f), new Vector3(0.38f, 0.18f, 0.2f), p.shirt * 0.85f);
                    Part(PrimitiveType.Cube, "Mochila", body, new Vector3(0, 0.3f, -0.22f), new Vector3(0.34f, 0.42f, 0.16f), p.accent);
                    Part(PrimitiveType.Cube, "Bolso", body, new Vector3(0, 0.12f, 0.15f), new Vector3(0.24f, 0.1f, 0.02f), p.shirt * 0.8f);
                    break;

                case CharacterLook.Professor:
                    Part(PrimitiveType.Sphere, "Barriga", body, new Vector3(0, 0.2f, 0.07f), new Vector3(0.44f, 0.38f, 0.34f), p.shirt);
                    Part(PrimitiveType.Cube, "Colarinho", body, new Vector3(0, 0.55f, 0.12f), new Vector3(0.2f, 0.06f, 0.05f), Color.white);
                    Part(PrimitiveType.Cube, "Gravata", body, new Vector3(0, 0.36f, 0.215f), new Vector3(0.07f, 0.34f, 0.02f), p.accent, 0.25f, new Vector3(-12, 0, 0));
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Sphere, "CabeloLado", head, new Vector3(0.19f * s, 0.22f, -0.04f), new Vector3(0.1f, 0.17f, 0.3f), p.hair);
                    Part(PrimitiveType.Cube, "Bigode", head, new Vector3(0, 0.125f, 0.205f), new Vector3(0.17f, 0.045f, 0.04f), p.brow);
                    Part(PrimitiveType.Cube, "OculosBarra", head, new Vector3(0, 0.3f, 0.215f), new Vector3(0.28f, 0.016f, 0.012f), black);
                    Part(PrimitiveType.Cube, "OculosPonte", head, new Vector3(0, 0.255f, 0.222f), new Vector3(0.035f, 0.015f, 0.012f), black);
                    break;

                case CharacterLook.Nerd:
                    Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0.02f, 0.35f, -0.03f), new Vector3(0.46f, 0.2f, 0.44f), p.hair, 0.7f);
                    Part(PrimitiveType.Cube, "Repartido", head, new Vector3(-0.07f, 0.43f, 0.04f), new Vector3(0.015f, 0.03f, 0.3f), p.skin);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float x = 0.085f * s;
                        Part(PrimitiveType.Cube, "OculosTopo", head, new Vector3(x, 0.315f, 0.215f), new Vector3(0.15f, 0.02f, 0.012f), black);
                        Part(PrimitiveType.Cube, "OculosBase", head, new Vector3(x, 0.165f, 0.215f), new Vector3(0.15f, 0.02f, 0.012f), black);
                        Part(PrimitiveType.Cube, "OculosLado", head, new Vector3(x + 0.068f, 0.24f, 0.215f), new Vector3(0.02f, 0.15f, 0.012f), black);
                        Part(PrimitiveType.Cube, "OculosLado", head, new Vector3(x - 0.068f, 0.24f, 0.215f), new Vector3(0.02f, 0.15f, 0.012f), black);
                    }
                    Part(PrimitiveType.Cube, "GravataBorboleta", body, new Vector3(-0.05f, 0.55f, 0.15f), new Vector3(0.08f, 0.06f, 0.03f), p.accent, 0.25f, new Vector3(0, 0, 20));
                    Part(PrimitiveType.Cube, "GravataBorboleta", body, new Vector3(0.05f, 0.55f, 0.15f), new Vector3(0.08f, 0.06f, 0.03f), p.accent, 0.25f, new Vector3(0, 0, -20));
                    Part(PrimitiveType.Cube, "Bolso", body, new Vector3(-0.1f, 0.38f, 0.15f), new Vector3(0.1f, 0.1f, 0.02f), Color.white);
                    Part(PrimitiveType.Cylinder, "Caneta", body, new Vector3(-0.12f, 0.45f, 0.155f), new Vector3(0.015f, 0.05f, 0.015f), new Color(0.1f, 0.3f, 0.9f));
                    Part(PrimitiveType.Cylinder, "Caneta", body, new Vector3(-0.08f, 0.45f, 0.155f), new Vector3(0.015f, 0.05f, 0.015f), new Color(0.9f, 0.1f, 0.1f));
                    break;

                case CharacterLook.Dorminhoco:
                    Part(PrimitiveType.Sphere, "Capuz", head, new Vector3(0, 0.23f, -0.06f), new Vector3(0.5f, 0.5f, 0.4f), p.shirt);
                    Part(PrimitiveType.Sphere, "Franja", head, new Vector3(0, 0.37f, 0.1f), new Vector3(0.32f, 0.1f, 0.16f), p.hair);
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Sphere, "Olheira", head, new Vector3(0.08f * s, 0.185f, 0.19f), new Vector3(0.09f, 0.03f, 0.02f), new Color(0.45f, 0.3f, 0.45f));
                    break;

                case CharacterLook.Fundao:
                    Part(PrimitiveType.Sphere, "Bone", head, new Vector3(0, 0.36f, 0), new Vector3(0.47f, 0.2f, 0.47f), p.accent);
                    Part(PrimitiveType.Cube, "Aba", head, new Vector3(0, 0.37f, -0.28f), new Vector3(0.3f, 0.02f, 0.2f), p.accent);
                    Part(PrimitiveType.Cube, "Gola", body, new Vector3(0, 0.56f, 0.11f), new Vector3(0.22f, 0.05f, 0.06f), new Color(0.1f, 0.55f, 0.25f));
                    Part(PrimitiveType.Cube, "Corrente", body, new Vector3(0, 0.47f, 0.155f), new Vector3(0.2f, 0.02f, 0.02f), new Color(1f, 0.8f, 0.2f), 0.85f);
                    Part(PrimitiveType.Cube, "Pingente", body, new Vector3(0, 0.42f, 0.16f), new Vector3(0.05f, 0.06f, 0.015f), new Color(1f, 0.8f, 0.2f), 0.85f);
                    Part(PrimitiveType.Cube, "Numero", body, new Vector3(0, 0.25f, 0.165f), new Vector3(0.1f, 0.14f, 0.01f), new Color(0.1f, 0.55f, 0.25f));
                    break;

                case CharacterLook.PuxaSaco:
                    Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0.03f, 0.35f, -0.02f), new Vector3(0.46f, 0.21f, 0.44f), p.hair, 0.6f);
                    Part(PrimitiveType.Sphere, "Topete", head, new Vector3(0.08f, 0.42f, 0.12f), new Vector3(0.2f, 0.09f, 0.14f), p.hair, 0.6f);
                    Part(PrimitiveType.Capsule, "Colete", body, new Vector3(0, 0.27f, 0.005f), new Vector3(0.47f, 0.28f, 0.32f), p.accent);
                    Part(PrimitiveType.Cube, "Gravata", body, new Vector3(0, 0.4f, 0.165f), new Vector3(0.05f, 0.2f, 0.02f), new Color(0.1f, 0.15f, 0.4f));
                    break;

                default: // figurante
                    int style = rnd.Next(3);
                    if (style == 0)
                        Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0, 0.34f, -0.02f), new Vector3(0.46f, 0.22f, 0.45f), p.hair);
                    else if (style == 1)
                    {
                        Part(PrimitiveType.Sphere, "Hair", head, new Vector3(0, 0.3f, -0.05f), new Vector3(0.48f, 0.34f, 0.46f), p.hair);
                        Part(PrimitiveType.Cube, "HairBack", head, new Vector3(0, 0.08f, -0.13f), new Vector3(0.4f, 0.4f, 0.16f), p.hair);
                    }
                    else
                    {
                        Part(PrimitiveType.Sphere, "Bone", head, new Vector3(0, 0.36f, 0), new Vector3(0.47f, 0.2f, 0.47f), p.accent);
                        Part(PrimitiveType.Cube, "Aba", head, new Vector3(0, 0.37f, 0.27f), new Vector3(0.3f, 0.02f, 0.2f), p.accent);
                    }
                    if (rnd.NextDouble() < 0.3)
                        Part(PrimitiveType.Cube, "Oculos", head, new Vector3(0, 0.3f, 0.215f), new Vector3(0.28f, 0.016f, 0.012f), black);
                    break;
            }
        }

        // ------------------------------------------------------------------ palette

        struct Palette { public Color skin, shirt, sleeve, pants, hair, brow, shoes, accent; }

        static Color C(float r, float g, float b) => new(r, g, b);

        static Palette PaletteFor(CharacterLook l, System.Random rnd)
        {
            var p = new Palette { shoes = C(0.15f, 0.12f, 0.1f) };
            switch (l)
            {
                case CharacterLook.Jogador:
                    p.skin = C(0.87f, 0.67f, 0.52f); p.shirt = C(0.95f, 0.45f, 0.12f); p.pants = C(0.2f, 0.3f, 0.52f);
                    p.hair = C(0.2f, 0.12f, 0.07f); p.accent = C(0.45f, 0.25f, 0.7f); p.shoes = C(0.9f, 0.9f, 0.9f); break;
                case CharacterLook.Professor:
                    p.skin = C(0.93f, 0.76f, 0.62f); p.shirt = C(0.45f, 0.34f, 0.22f); p.pants = C(0.3f, 0.24f, 0.18f);
                    p.hair = C(0.62f, 0.62f, 0.62f); p.brow = C(0.35f, 0.33f, 0.32f); p.accent = C(0.8f, 0.1f, 0.12f); break;
                case CharacterLook.Nerd:
                    p.skin = C(0.96f, 0.82f, 0.7f); p.shirt = C(0.62f, 0.8f, 0.95f); p.pants = C(0.72f, 0.63f, 0.45f);
                    p.hair = C(0.06f, 0.06f, 0.08f); p.accent = C(0.85f, 0.1f, 0.15f); break;
                case CharacterLook.Dorminhoco:
                    p.skin = C(0.55f, 0.38f, 0.26f); p.shirt = C(0.48f, 0.5f, 0.56f); p.pants = C(0.35f, 0.36f, 0.4f);
                    p.hair = C(0.08f, 0.06f, 0.05f); p.accent = p.shirt; p.shoes = C(0.2f, 0.25f, 0.6f); break;
                case CharacterLook.Fundao:
                    p.skin = C(0.72f, 0.52f, 0.38f); p.shirt = C(1f, 0.85f, 0.1f); p.pants = C(0.15f, 0.3f, 0.75f);
                    p.hair = C(0.1f, 0.07f, 0.05f); p.accent = C(0.85f, 0.15f, 0.15f); p.shoes = C(0.95f, 0.95f, 0.95f); break;
                case CharacterLook.PuxaSaco:
                    p.skin = C(0.98f, 0.86f, 0.76f); p.shirt = C(0.97f, 0.97f, 0.97f); p.pants = C(0.12f, 0.15f, 0.3f);
                    p.hair = C(0.88f, 0.72f, 0.35f); p.accent = C(0.55f, 0.12f, 0.2f); break;
                default:
                    Color[] skins = { C(0.96f, 0.82f, 0.7f), C(0.87f, 0.67f, 0.52f), C(0.72f, 0.52f, 0.38f), C(0.5f, 0.34f, 0.24f), C(0.36f, 0.24f, 0.17f) };
                    Color[] shirts = { C(0.2f, 0.6f, 0.4f), C(0.8f, 0.25f, 0.3f), C(0.25f, 0.45f, 0.85f), C(0.9f, 0.7f, 0.2f), C(0.55f, 0.35f, 0.75f), C(0.15f, 0.15f, 0.18f), C(0.95f, 0.55f, 0.65f) };
                    Color[] pants = { C(0.2f, 0.3f, 0.52f), C(0.15f, 0.15f, 0.17f), C(0.55f, 0.5f, 0.42f), C(0.3f, 0.32f, 0.36f) };
                    Color[] hairs = { C(0.08f, 0.06f, 0.05f), C(0.3f, 0.18f, 0.08f), C(0.75f, 0.55f, 0.25f), C(0.55f, 0.15f, 0.1f), C(0.12f, 0.1f, 0.1f) };
                    p.skin = skins[rnd.Next(skins.Length)]; p.shirt = shirts[rnd.Next(shirts.Length)];
                    p.pants = pants[rnd.Next(pants.Length)]; p.hair = hairs[rnd.Next(hairs.Length)];
                    p.accent = shirts[rnd.Next(shirts.Length)]; break;
            }
            if (p.brow == default) p.brow = p.hair * 0.8f;
            p.sleeve = l == CharacterLook.PuxaSaco ? Color.white : p.shirt;
            return p;
        }

        // ------------------------------------------------------------------ animation

        void Bind()
        {
            body = Find("Body");
            if (body == null) return;
            head = Find("Head"); shoulderL = Find("ShoulderL"); shoulderR = Find("ShoulderR");
            hipL = Find("HipL"); hipR = Find("HipR"); kneeL = Find("KneeL"); kneeR = Find("KneeR");
            browL = Find("BrowL"); browR = Find("BrowR"); mouth = Find("Mouth"); sweatRoot = Find("Sweat");
            lidL = Find("LidL"); lidR = Find("LidR");
            if (lidL != null) lidBaseL = new Vector3(lidL.localPosition.x, 0.24f, lidL.localPosition.z);
            if (lidR != null) lidBaseR = new Vector3(lidR.localPosition.x, 0.24f, lidR.localPosition.z);
        }

        Transform Find(string n) => FindDeep(transform, n);

        static Transform FindDeep(Transform t, string n)
        {
            if (t.name == n) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), n);
                if (r != null) return r;
            }
            return null;
        }

        public void Animate(float dt)
        {
            if (body == null) return;
            float t = Application.isPlaying ? Time.time : 0f;

            // velocidade horizontal (para o ciclo de caminhada)
            var delta = transform.position - lastPos;
            delta.y = 0f;
            lastPos = transform.position;
            float sp = dt > 0f ? delta.magnitude / dt : 0f;
            speed = dt > 0f ? Mathf.Lerp(speed, sp, Mathf.Clamp01(10f * dt)) : 0f;
            bool walking = !seated && speed > 0.15f;
            phase += dt * (walking ? 3.2f + speed * 2.2f : 0f);
            float swing = walking ? Mathf.Sin(phase) * Mathf.Clamp01(speed / 1.8f) * 38f : 0f;

            // um passo a cada meio ciclo da perna: o som acompanha a animação e fica mais forte quanto mais rápido
            if (walking && Application.isPlaying)
            {
                int step = Mathf.FloorToInt(phase / Mathf.PI);
                if (step != lastStep)
                {
                    lastStep = step;
                    SoundFX.PlayAt(Sfx.Passo, transform.position, footstepVolume * Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(speed / 3.5f)),
                                   Random.Range(0.92f, 1.08f));
                }
            }

            // corpo
            float shake = sweat > 0.5f ? Mathf.Sin(t * 47f) * 0.008f * sweat : 0f;
            float bob = walking ? Mathf.Abs(Mathf.Cos(phase)) * 0.05f : 0f;
            float breathe = Mathf.Sin(t * (sleeping ? 1.3f : 2.1f)) * (sleeping ? 0.015f : 0.006f);
            body.localPosition = new Vector3(shake, (seated ? 0.5f : 0.85f) + bob + breathe, 0f);
            body.localRotation = Quaternion.Euler(-leanBack + (walking ? 4f : 0f), 0f, walking ? Mathf.Sin(phase) * 2f : 0f);

            // pernas
            if (seated)
            {
                hipL.localRotation = Quaternion.Euler(-88f, 0f, -4f);
                hipR.localRotation = Quaternion.Euler(-88f, 0f, 4f);
                kneeL.localRotation = kneeR.localRotation = Quaternion.Euler(88f, 0f, 0f);
            }
            else
            {
                hipL.localRotation = Quaternion.Euler(swing, 0f, 0f);
                hipR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
                kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, swing) * 1.1f, 0f, 0f);
                kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, -swing) * 1.1f, 0f, 0f);
            }

            // braços
            Quaternion l, r;
            if (sleeping)
            {
                l = Quaternion.Euler(-80f, 25f, 0f);
                r = Quaternion.Euler(-80f, -25f, 0f);
            }
            else if (seated)
            {
                l = Quaternion.Euler(-45f, 10f, -4f);
                r = writing
                    ? Quaternion.Euler(-58f + Mathf.Sin(t * 13f) * 6f, -12f + Mathf.Sin(t * 7f) * 8f, 4f)
                    : Quaternion.Euler(-48f, -10f, 4f);
            }
            else
            {
                l = Quaternion.Euler(-swing * 0.9f, 0f, -6f);
                r = Quaternion.Euler(swing * 0.9f, 0f, 6f);
            }
            if (phone > 0f)
            {
                l = Quaternion.Slerp(l, Quaternion.Euler(-35f, 35f, 0f), phone);
                r = Quaternion.Slerp(r, Quaternion.Euler(-35f, -35f, 0f), phone);
            }
            if (armsCrossed > 0f)
            {
                l = Quaternion.Slerp(l, Quaternion.Euler(-78f, 55f, 0f), armsCrossed);
                r = Quaternion.Slerp(r, Quaternion.Euler(-72f, -55f, 0f), armsCrossed);
            }
            if (handRaise > 0f) r = Quaternion.Slerp(r, Quaternion.Euler(-172f, 0f, 10f), handRaise);
            shoulderL.localRotation = l;
            shoulderR.localRotation = r;

            // cabeça
            float pitch = headPitch + (writing && seated ? 16f : 0f) + (sleeping ? 48f : 0f) + phone * 32f;
            float k = dt > 0f ? Mathf.Clamp01(6f * dt) : 1f;
            curYaw = Mathf.Lerp(curYaw, headYaw, k);
            curPitch = Mathf.Lerp(curPitch, pitch, k);
            head.localRotation = Quaternion.Euler(curPitch, curYaw, sleeping ? 12f : 0f);

            ApplyExpression(t);
            if (Skin == null && Application.isPlaying) UpdatePrimitiveProp();

            if (sweatRoot != null)
            {
                bool show = sweat > 0.35f;
                if (sweatRoot.gameObject.activeSelf != show) sweatRoot.gameObject.SetActive(show);
                if (show) sweatRoot.localPosition = new Vector3(0f, -Mathf.Repeat(t * 0.25f, 0.12f), 0f);
            }
        }

        /// <summary>Sem modelo 3D: o objeto aparece na mão direita do boneco de primitivas.</summary>
        void UpdatePrimitiveProp()
        {
            if (prop == primitivePropKind) return;
            if (primitiveProp != null) Destroy(primitiveProp.gameObject);
            primitivePropKind = prop;
            var hand = shoulderR != null ? shoulderR.Find("Hand") : null;
            primitiveProp = hand != null ? HandProps.Build(prop, hand.parent) : null;
            if (primitiveProp != null) primitiveProp.localPosition = hand.localPosition + new Vector3(0f, -0.06f, 0.05f);
        }

        void ApplyExpression(float t)
        {
            float brow = 0f, browY = 0.32f, mouthW = 0.11f, mouthH = 0.025f, lid = 0f;
            switch (expression)
            {
                case Expression.Nervoso: brow = 16f; mouthW = 0.08f; mouthH = 0.02f; break;
                case Expression.Desconfiado: brow = -14f; lid = 0.45f; mouthW = 0.07f; break;
                case Expression.Bravo: brow = -26f; browY = 0.3f; mouthW = 0.12f; mouthH = 0.02f; break;
                case Expression.Feliz: brow = 6f; mouthW = 0.14f; mouthH = 0.05f; break;
                case Expression.Dormindo: lid = 1f; mouthW = 0.05f; mouthH = 0.045f; break;
                case Expression.Assustado: brow = 12f; browY = 0.35f; mouthW = 0.07f; mouthH = 0.085f; break;
                case Expression.Convencido: brow = -8f; mouthW = 0.15f; mouthH = 0.03f; break;
            }
            if (sleeping) lid = 1f;

            // piscar
            if (Application.isPlaying && lid < 0.5f)
            {
                if (t > nextBlink) { blinkUntil = t + 0.12f; nextBlink = t + Random.Range(2.5f, 5f); }
                if (t < blinkUntil) lid = 1f;
            }
            if (Application.isPlaying && t < talkUntil) mouthH = Mathf.Lerp(0.02f, 0.075f, Mathf.Abs(Mathf.Sin(t * 17f)));

            if (browL != null)
            {
                browL.localPosition = new Vector3(browL.localPosition.x, browY, browL.localPosition.z);
                browR.localPosition = new Vector3(browR.localPosition.x, expression == Expression.Desconfiado ? browY + 0.03f : browY, browR.localPosition.z);
                browL.localRotation = Quaternion.Euler(0f, 0f, brow);
                browR.localRotation = Quaternion.Euler(0f, 0f, expression == Expression.Desconfiado ? 10f : -brow);
            }
            if (mouth != null) mouth.localScale = new Vector3(mouthW, mouthH, 0.03f);
            SetLid(lidL, lidBaseL, lid);
            SetLid(lidR, lidBaseR, lid);
        }

        static void SetLid(Transform lidT, Vector3 basePos, float closure)
        {
            if (lidT == null) return;
            bool on = closure > 0.05f;
            if (lidT.gameObject.activeSelf != on) lidT.gameObject.SetActive(on);
            if (on) lidT.localPosition = basePos + Vector3.up * (1f - closure) * 0.06f;
        }

        // ------------------------------------------------------------------ helpers

        static Transform Pivot(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static Transform Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color,
                              float smooth = 0.25f, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(color, smooth);
            return go.transform;
        }
    }
}
