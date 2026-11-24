using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProvaImpossivel
{
    /// <summary>
    /// "Pele" 3D de um personagem: um modelo Quaternius que copia o estado do <see cref="CharacterVisual"/>.
    /// O boneco de primitivas continua existindo (invisível) e segue sendo a referência do jogo (cabeça, olhar e
    /// altura que a visão usa), então trocar o visual não muda nenhuma regra.
    /// Base: Idle/Walk do pacote (modelos Humanoid) ou pose de descanso (modelos Generic). Por cima, as poses do jogo
    /// apontando os ossos em direções do espaço do personagem (x = direita, y = cima, z = frente), o que vale para
    /// qualquer esqueleto com os nomes do pacote: sentar, escrever, dormir, celular, colinha, garrafinha, bilhete,
    /// mão levantada, braços cruzados. O objeto da mão (<see cref="HandProp"/>) aparece na mão certa.
    /// </summary>
    [DefaultExecutionOrder(100)] // depois do CharacterVisual (que anima o boneco invisível no LateUpdate)
    public class CharacterModelSkin : MonoBehaviour
    {
        CharacterVisual cv;
        CharacterModelSet set;
        Transform model, hips, spine, chest, neck, head;
        // 0 = esquerda, 1 = direita
        readonly Transform[] upperArm = new Transform[2], lowerArm = new Transform[2], hand = new Transform[2],
                             upperLeg = new Transform[2], lowerLeg = new Transform[2], foot = new Transform[2];
        // dedos por mão: indicador, médio, anelar, mínimo, polegar (primeira junta de cada)
        readonly Transform[][] fingers = { new Transform[5], new Transform[5] };
        static readonly string[] FingerNames = { "Index2", "Middle2", "Ring2", "Pinky2", "Thumb2" };
        Transform[] poseBones, fingerBones;
        Quaternion[] fingerRest;
        Quaternion[] restPose;
        bool animated;
        float scale = 1f, walkPhase;
        Vector3 headFacingLocal;
        // orelha direita e eixos da cabeça (espaço do osso da cabeça, guardados antes do cabeção): onde vai o fone
        Vector3 earLocal, earOutLocal, headUpLocal, mouthLocal;

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        const int Idle = 0, Walk = 1;

        float walkW, seatW, sleepW, writeW, crossW, raiseW, phoneW, earW, colaW, drinkW, throwW, foneW;
        HandProp propKind;
        Transform propObj;

        public Transform Model => model;
        public Transform HeadBone => head;
        public Transform Hips => hips;
        public Transform RightHand => hand[1];
        public Transform LeftHand => hand[0];
        public Transform RightUpperArm => upperArm[1];
        public Transform LeftUpperLeg => upperLeg[0];
        public Transform LeftFoot => foot[0];
        public Transform RightFoot => foot[1];
        /// <summary>Objeto que está na mão agora (null se nenhum).</summary>
        public Transform PropObject => propObj;
        public HandProp PropKind => propObj != null ? propKind : HandProp.Nenhum;
        /// <summary>true = toca Idle/Walk do pacote (rig Humanoid); false = só poses por código.</summary>
        public bool Animated => animated;
        /// <summary>Peso atual da caminhada (0–1).</summary>
        public float WalkWeight => walkW;
        /// <summary>Peso atual da pose sentada (0–1).</summary>
        public float SeatWeight => seatW;
        /// <summary>Para onde o rosto do modelo aponta (mundo).</summary>
        public Vector3 HeadFacing => head.rotation * headFacingLocal;

        static readonly string[] Side = { "L", "R" };
        /// <summary>Aumento dos objetos de mão (exagero de desenho animado, para ler pela câmera atrás do jogador).</summary>
        public const float PropScale = 1.35f;
        /// <summary>A garrafinha já é comprida: aumenta menos que os outros objetos.</summary>
        public const float BottleScale = 1.1f;
        /// <summary>Orelha direita em relação ao osso da cabeça (m, modelo em escala 1; x direita, y cima, z frente).</summary>
        static readonly Vector3 EarOffset = new(0.08f, 0.12f, -0.035f);
        /// <summary>Boca em relação ao osso da cabeça (mesmo espaço da orelha).</summary>
        static readonly Vector3 MouthOffset = new(0f, 0.045f, 0.12f);
        /// <summary>Folga entre o objeto e os ossos da mão (meia grossura de um dedo, m).</summary>
        public const float FingerRadius = 0.012f;
        /// <summary>Meia grossura da palma (m): o objeto encosta aqui, não no osso.</summary>
        public const float PalmRadius = 0.02f;

        /// <summary>Monta o modelo. Devolve false (e não faz nada) se faltar algo.</summary>
        public bool Init(CharacterVisual visual, CharacterModelSet modelSet, GameObject prefab, CharacterModelSet.Entry entry)
        {
            cv = visual;
            set = modelSet;
            scale = entry.scale > 0f ? entry.scale : 1f;

            model = Instantiate(prefab, transform).transform;
            model.name = "Modelo";
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one * scale;

            hips = Deep(model, "Hips"); spine = Deep(model, "Abdomen"); chest = Deep(model, "Torso");
            neck = Deep(model, "Neck"); head = Deep(model, "Head");
            bool ok = hips && spine && chest && neck && head;
            for (int s = 0; s < 2; s++)
            {
                upperArm[s] = Deep(model, "UpperArm." + Side[s]); lowerArm[s] = Deep(model, "LowerArm." + Side[s]);
                hand[s] = Deep(model, "Hand." + Side[s]);
                upperLeg[s] = Deep(model, "UpperLeg." + Side[s]); lowerLeg[s] = Deep(model, "LowerLeg." + Side[s]);
                foot[s] = Deep(model, "Foot." + Side[s]);
                ok &= upperArm[s] && lowerArm[s] && hand[s] && upperLeg[s] && lowerLeg[s] && foot[s];
                for (int f = 0; f < FingerNames.Length; f++) fingers[s][f] = hand[s] != null ? Deep(hand[s], FingerNames[f] + "." + Side[s]) : null;
            }
            // os dedos não são animados (nem no Humanoid): a pose deles é refeita do zero a cada quadro
            var fb = new System.Collections.Generic.List<Transform>();
            foreach (var side in fingers)
                foreach (var f0 in side)
                    for (var b = f0; b != null; b = b.childCount > 0 ? b.GetChild(0) : null) fb.Add(b);
            fingerBones = fb.ToArray();
            fingerRest = new Quaternion[fingerBones.Length];
            for (int i = 0; i < fingerBones.Length; i++) fingerRest[i] = fingerBones[i].localRotation;
            if (!ok)
            {
                Destroy(model.gameObject);
                return false;
            }

            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
            hand[0].localScale *= 0.85f; // as mãos do pacote são enormes
            hand[1].localScale *= 0.85f;

            // no modelo recém-criado o personagem olha para a frente: guarda para onde o rosto aponta
            headFacingLocal = Quaternion.Inverse(head.rotation) * transform.forward;
            // orelha direita (antes do cabeção: o ponto cresce junto com a cabeça)
            earLocal = head.InverseTransformPoint(head.position + Dir(EarOffset) * scale);
            mouthLocal = head.InverseTransformPoint(head.position + Dir(MouthOffset) * scale);
            earOutLocal = Quaternion.Inverse(head.rotation) * transform.right;
            headUpLocal = Quaternion.Inverse(head.rotation) * transform.up;

            CharacterLooks.Dress(this, model, head, neck, spine, entry, set, visual.seed, scale);

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animated = animator.avatar != null && animator.avatar.isHuman && set.idle != null;

            bool sentado = visual.seated;
            seatW = sentado ? 1f : 0f;

            if (animated)
            {
                graph = PlayableGraph.Create(name + "_Modelo");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                mixer = AnimationMixerPlayable.Create(graph, 2);
                Connect(set.idle, Idle, 1f);
                Connect(set.walk != null ? set.walk : set.idle, Walk, 0f);
                var output = AnimationPlayableOutput.Create(graph, "Modelo", animator);
                output.SetSourcePlayable(mixer);
                // cada um começa num ponto diferente do ciclo (a turma não respira em coro)
                mixer.GetInput(Idle).SetTime(Mathf.Abs(visual.seed * 0.37f + (int)visual.look * 0.21f) % 1.5f);
                graph.Play();
            }
            else
            {
                animator.enabled = false;
                // sem animação, a pose é refeita do zero a cada quadro (as poses por código são relativas)
                poseBones = new[] { hips, spine, chest, neck, head, upperArm[0], lowerArm[0], hand[0], upperArm[1], lowerArm[1],
                                     hand[1], upperLeg[0], lowerLeg[0], foot[0], upperLeg[1], lowerLeg[1], foot[1] };
                restPose = new Quaternion[poseBones.Length];
                for (int i = 0; i < poseBones.Length; i++) restPose[i] = poseBones[i].localRotation;
            }

            HidePrimitives();
            return true;
        }

        void Connect(AnimationClip clip, int slot, float weight)
        {
            var p = AnimationClipPlayable.Create(graph, clip);
            p.SetApplyFootIK(false);
            graph.Connect(p, 0, mixer, slot);
            mixer.SetInputWeight(slot, weight);
        }

        /// <summary>Esconde o boneco de primitivas (menos as gotas de suor, que passam a seguir a cabeça do modelo).</summary>
        void HidePrimitives()
        {
            var visualRoot = transform.Find("Visual");
            if (visualRoot == null) return;
            var sweat = cv.SweatRoot;
            foreach (var r in visualRoot.GetComponentsInChildren<Renderer>(true))
                if (sweat == null || !r.transform.IsChildOf(sweat)) r.enabled = false;
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        static Transform Deep(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t)
            {
                var r = Deep(c, n);
                if (r != null) return r;
            }
            return null;
        }

        static float Ease(float cur, float target, float rate, float dt) => Mathf.MoveTowards(cur, target, rate * dt);

        void Update()
        {
            if (cv == null) return;
            float dt = Time.deltaTime;
            float tWalk = !cv.seated && cv.IsWalking ? 1f : 0f;
            walkW = Ease(walkW, tWalk, 6f, dt);
            if (animated && graph.IsValid())
            {
                mixer.SetInputWeight(Idle, 1f - walkW);
                mixer.SetInputWeight(Walk, walkW);
                // passo da caminhada acompanha a velocidade
                mixer.GetInput(Walk).SetSpeed(Mathf.Clamp(cv.Speed / 1.3f, 0.6f, 2.2f));
            }
            else if (walkW > 0f) walkPhase += dt * (3.2f + cv.Speed * 2.2f);

            var p = cv.prop;
            seatW = Ease(seatW, cv.seated ? 1f : 0f, 6f, dt);
            sleepW = Ease(sleepW, cv.sleeping ? 1f : 0f, 3f, dt);
            writeW = Ease(writeW, cv.seated && cv.writing && !cv.sleeping ? 1f : 0f, 5f, dt);
            earW = Ease(earW, p == HandProp.Celular && !cv.seated ? 1f : 0f, 4f, dt);
            phoneW = Ease(phoneW, (p == HandProp.Celular && cv.seated) || cv.phone > 0.5f ? 1f : 0f, 6f, dt);
            colaW = Ease(colaW, p == HandProp.Colinha ? 1f : 0f, 6f, dt);
            drinkW = Ease(drinkW, p == HandProp.Garrafinha ? 1f : 0f, 3f, dt);
            throwW = Ease(throwW, p == HandProp.Bilhete ? 1f : 0f, 9f, dt);
            foneW = Ease(foneW, p == HandProp.Fone ? 1f : 0f, 5f, dt);
            raiseW = cv.handRaise * (1f - earW);
            crossW = cv.armsCrossed;
        }

        // ------------------------------------------------------------------ poses

        Vector3 Dir(Vector3 local) => transform.TransformDirection(local);

        /// <summary>Gira <paramref name="bone"/> para que o segmento até <paramref name="child"/> aponte para <paramref name="local"/>.</summary>
        void Aim(Transform bone, Transform child, Vector3 local, float w)
        {
            if (w <= 0.001f) return;
            Vector3 cur = child.position - bone.position;
            if (cur.sqrMagnitude < 1e-8f) return;
            var target = Quaternion.FromToRotation(cur, Dir(local)) * bone.rotation;
            bone.rotation = w >= 0.999f ? target : Quaternion.Slerp(bone.rotation, target, w);
        }

        /// <summary>Pose de braço: direção do braço e do antebraço (x espelhado para o lado esquerdo).</summary>
        void Arm(int s, Vector3 upper, Vector3 lower, float w)
        {
            if (w <= 0.001f) return;
            float m = s == 0 ? -1f : 1f;
            Aim(upperArm[s], lowerArm[s], new Vector3(upper.x * m, upper.y, upper.z), w);
            Aim(lowerArm[s], hand[s], new Vector3(lower.x * m, lower.y, lower.z), w);
        }

        /// <summary>Direção dos dedos (do pulso ao dedo médio).</summary>
        Vector3 FingerDir(int s) => (fingers[s][1] != null ? fingers[s][1].position : hand[s].position + (hand[s].position - lowerArm[s].position)) - hand[s].position;

        /// <summary>Normal da palma (para onde a palma olha), a partir dos dedos indicador e mínimo.</summary>
        Vector3 PalmNormal(int s)
        {
            if (fingers[s][0] == null || fingers[s][3] == null) return Dir(Vector3.down);
            Vector3 across = fingers[s][0].position - fingers[s][3].position; // mínimo → indicador
            Vector3 n = Vector3.Cross(FingerDir(s), across) * (s == 1 ? 1f : -1f);
            return n.sqrMagnitude > 1e-10f ? n.normalized : Dir(Vector3.down);
        }

        /// <summary>Gira a mão em torno do antebraço para a palma olhar para <paramref name="local"/> (espaço do personagem).</summary>
        void Palm(int s, Vector3 local, float w)
        {
            if (w <= 0.001f || fingers[s][0] == null) return;
            Vector3 f = FingerDir(s).normalized;
            Vector3 want = Vector3.ProjectOnPlane(Dir(local), f);
            if (want.sqrMagnitude < 1e-6f) return;
            var target = Quaternion.FromToRotation(PalmNormal(s), want.normalized) * hand[s].rotation;
            hand[s].rotation = Quaternion.Slerp(hand[s].rotation, target, w);
        }

        /// <summary>Dobra o punho para os dedos apontarem para <paramref name="world"/> (mundo).</summary>
        void AimFingers(int s, Vector3 world, float w)
        {
            if (w <= 0.001f || world.sqrMagnitude < 1e-8f) return;
            Vector3 f = FingerDir(s);
            if (f.sqrMagnitude < 1e-8f) return;
            var target = Quaternion.FromToRotation(f, world) * hand[s].rotation;
            hand[s].rotation = Quaternion.Slerp(hand[s].rotation, target, w);
        }

        /// <summary>Fecha os dedos (graus por junta) em direção à palma.</summary>
        void Curl(int s, float degrees)
        {
            if (degrees <= 0.5f || fingers[s][0] == null) return;
            Quaternion.FromToRotation(FingerDir(s), PalmNormal(s)).ToAngleAxis(out _, out Vector3 axis);
            var q = Quaternion.AngleAxis(degrees, axis);
            var qThumb = Quaternion.AngleAxis(degrees * 0.45f, axis);
            for (int f = 0; f < fingers[s].Length; f++)
            {
                var b = fingers[s][f];
                bool thumb = f == 4;
                for (int j = 0; j < (thumb ? 2 : 3) && b != null; j++)
                {
                    b.rotation = (thumb ? qThumb : q) * b.rotation;
                    b = b.childCount > 0 ? b.GetChild(0) : null;
                }
            }
        }

        /// <summary>
        /// IK de dois ossos: leva a mão (osso) até <paramref name="target"/>, com o cotovelo para o lado de
        /// <paramref name="poleLocal"/> (espaço do personagem, lado direito; espelhado para o esquerdo).
        /// </summary>
        void Reach(int s, Vector3 target, Vector3 poleLocal, float w)
        {
            if (w <= 0.001f) return;
            Vector3 sh = upperArm[s].position;
            float a = Vector3.Distance(sh, lowerArm[s].position), b = Vector3.Distance(lowerArm[s].position, hand[s].position);
            Vector3 toT = target - sh;
            if (toT.sqrMagnitude < 1e-8f || a < 1e-4f || b < 1e-4f) return;
            Vector3 dir = toT.normalized;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
            float m = s == 0 ? -1f : 1f;
            Vector3 bend = Vector3.ProjectOnPlane(Dir(new Vector3(poleLocal.x * m, poleLocal.y, poleLocal.z)), dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.ProjectOnPlane(Dir(Vector3.down), dir);
            bend.Normalize();
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f);
            Vector3 elbow = sh + dir * (a * cos) + bend * (a * Mathf.Sqrt(1f - cos * cos));
            Vector3 up = transform.InverseTransformDirection(elbow - sh), low = transform.InverseTransformDirection(sh + dir * d - elbow);
            up.x *= m; low.x *= m; // Arm espelha o lado esquerdo de novo
            Arm(s, up, low, w);
        }

        /// <summary>
        /// Onde a garrafinha fica no gole: <paramref name="axis"/> vai da tampa para o fundo (para cima e para a frente),
        /// <paramref name="side"/> é de que lado do eixo a mão segura, <paramref name="center"/> o meio da garrafa.
        /// </summary>
        void BottleFrame(out Vector3 axis, out Vector3 side, out Vector3 center, out float radius)
        {
            float k = BottleScale * scale;
            axis = Dir(new Vector3(0.12f, 0.6f, 0.79f)).normalized;
            side = Vector3.ProjectOnPlane(Dir(new Vector3(1f, -0.7f, 0f)), axis).normalized;
            radius = 0.04f * k;
            Vector3 cap = Mouth() + Dir(Vector3.forward) * 0.015f * scale;
            center = cap + axis * (0.14f * k);
        }

        /// <summary>Folga mínima de cada pé para o seu lado, a partir do meio do quadril (m, modelo em escala 1).</summary>
        public const float MinFootOut = 0.06f;

        /// <summary>Gira a coxa para fora (em torno do eixo da frente) até o pé ficar do seu lado do quadril.</summary>
        void KeepFootOut(int s, float w)
        {
            if (w <= 0.001f) return;
            float m = s == 0 ? -1f : 1f;
            Vector3 right = transform.right;
            float x = Vector3.Dot(foot[s].position - hips.position, right) * m; // + = para fora
            float want = MinFootOut * scale;
            if (x >= want) return;
            Vector3 leg = foot[s].position - upperLeg[s].position;
            float len = Vector3.ProjectOnPlane(leg, transform.forward).magnitude;
            if (len < 1e-3f) return;
            float ang = Mathf.Asin(Mathf.Clamp((want - x) / len, 0f, 0.5f)) * Mathf.Rad2Deg * w;
            upperLeg[s].rotation = Quaternion.AngleAxis(ang * m, transform.forward) * upperLeg[s].rotation;
        }

        void Bend(Transform bone, float pitch, float yaw = 0f)
        {
            if (Mathf.Abs(pitch) > 0.01f) bone.rotation = Quaternion.AngleAxis(pitch, transform.right) * bone.rotation;
            if (Mathf.Abs(yaw) > 0.01f) bone.rotation = Quaternion.AngleAxis(yaw, transform.up) * bone.rotation;
        }

        void LateUpdate()
        {
            if (cv == null || model == null) return;
            float t = Time.time;
            if (!animated)
                for (int i = 0; i < poseBones.Length; i++) poseBones[i].localRotation = restPose[i];
            for (int i = 0; i < fingerBones.Length; i++) fingerBones[i].localRotation = fingerRest[i];
            model.localPosition = Vector3.zero;

            // de pé sem animação: braços para baixo, respiração e (andando) passos por código
            if (!animated)
            {
                float stand = 1f - seatW;
                for (int s = 0; s < 2; s++)
                {
                    float m = s == 0 ? -1f : 1f;
                    float swing = walkW * Mathf.Sin(walkPhase + s * Mathf.PI) * 0.45f;
                    Arm(s, new Vector3(0.14f, -1f, -swing * 0.8f), new Vector3(0.05f, -1f, 0.12f - swing * 0.6f), 1f);
                    Aim(upperLeg[s], lowerLeg[s], new Vector3(m * 0.04f, -1f, swing), stand);
                    Aim(lowerLeg[s], foot[s], new Vector3(0f, -1f, swing - Mathf.Max(0f, -swing) * 0.6f), stand);
                }
                Bend(chest, Mathf.Sin(t * 2.1f + cv.seed) * 1.2f);
            }

            // sentado: coxas para a frente, canelas para baixo
            for (int s = 0; s < 2; s++)
            {
                float m = s == 0 ? -1f : 1f;
                Aim(upperLeg[s], lowerLeg[s], new Vector3(m * 0.12f, -0.1f, 1f), seatW);
                Aim(lowerLeg[s], foot[s], new Vector3(m * 0.03f, -1f, 0.1f), seatW);
            }

            // de pé andando: o passo do pacote cruza um pé na frente do outro (passarela) e, com as pernas compridas
            // do professor, as pernas pareciam trançar. Afasta cada pé para o seu lado até a folga mínima.
            if (animated && walkW > 0.001f && seatW < 0.999f)
                for (int s = 0; s < 2; s++) KeepFootOut(s, walkW * (1f - seatW));

            // tronco: dormir debruçado, encostar na cadeira
            Bend(spine, sleepW * 16f - cv.leanBack * 0.5f);
            Bend(chest, sleepW * 18f - cv.leanBack * 0.5f + phoneW * 6f + colaW * 10f);

            // cabeça: o mesmo olhar do boneco invisível (é ele que a visão do jogo usa); bebendo, joga a cabeça para trás.
            // Vem antes dos braços: a garrafinha é posicionada pela boca, e a boca tem que já estar no lugar final
            // (com a cabeça girada depois, o queixo subia para dentro da tampa).
            float bob = cv.IsTalking ? Mathf.Sin(t * 17f) * 3f : 0f;
            float pitch = cv.CurrentPitch * set.headPitchFactor * (1f - sleepW * 0.6f) + bob - drinkW * 28f - colaW * 10f;
            head.rotation = Quaternion.AngleAxis(cv.CurrentYaw - colaW * 22f + phoneW * 22f, transform.up)
                          * Quaternion.AngleAxis(pitch, transform.right)
                          * Quaternion.AngleAxis((cv.sleeping ? 12f : 0f) - foneW * 10f, transform.forward) * head.rotation;

            // braços (cada camada puxa por cima da anterior). As colas ficam para o LADO ou para CIMA do corpo:
            // a câmera do jogo fica atrás do jogador, e o objeto tem que aparecer.
            float[] grip = { 0f, 0f };
            for (int s = 0; s < 2; s++)
            {
                Arm(s, new Vector3(0.18f, -0.75f, 0.6f), new Vector3(-0.22f, -0.12f, 1f), seatW);                // mãos na carteira
                Palm(s, new Vector3(0f, -1f, 0.1f), seatW);
                Arm(s, new Vector3(0.28f, -0.3f, 0.9f), new Vector3(-1f, -0.05f, 0.25f), sleepW);                // braços dobrados para deitar a cabeça
                Arm(s, new Vector3(0.16f, -0.9f, 0.35f), new Vector3(-1f, 0.12f, 0.25f), crossW);                // braços cruzados
                grip[s] = Mathf.Lerp(35f, 55f, crossW);
            }
            Arm(1, new Vector3(0.12f, -0.75f, 0.6f), new Vector3(-0.35f + Mathf.Sin(t * 7f) * 0.12f, -0.18f, 1f), writeW); // escrevendo
            // celular escondido do lado da carteira, tela para cima (e o pescoço torto olhando para ele)
            Arm(1, new Vector3(0.5f, -0.82f, 0.18f), new Vector3(0.3f, 0.12f, 1f), phoneW);
            Palm(1, new Vector3(-0.25f, 1f, -0.15f), phoneW);
            // colinha na manga: braço esquerdo para fora, antebraço levantado com o papel virado para o rosto
            Arm(0, new Vector3(0.8f, -0.3f, 0.3f), new Vector3(-0.1f, 0.85f, 0.4f), colaW);
            Palm(0, new Vector3(0.55f, 0.25f, -0.8f), colaW);
            // garrafinha: primeiro decide onde a garrafa fica (tampa na boca, fundo para cima e para a frente),
            // depois leva a mão até ela (IK de braço e antebraço), com a palma encostada por baixo e pela direita
            if (drinkW > 0.001f)
            {
                BottleFrame(out Vector3 axis, out Vector3 side, out Vector3 center, out float radius);
                // a mão pega a garrafa perto da tampa: no meio dela o alvo ficava a ~45 cm do ombro, além do braço
                // (~42 cm), e a mão parava antes, com a garrafa solta no ar
                Vector3 pega = center - axis * (0.07f * BottleScale * scale);
                Vector3 palmTarget = pega + side * (radius + PalmRadius * scale);
                // dedos atravessados na garrafa (em volta dela, por cima) e palma virada para ela. Só girar o punho
                // deixava a palma ~50° torta: os dedos entravam na garrafa e ela era empurrada para longe da mão.
                Vector3 volta = Vector3.Cross(axis, side);
                if (Vector3.Dot(volta, Dir(Vector3.up)) < 0f) volta = -volta;
                // a palma não fica no osso da mão, e girar a mão muda onde ela fica: corrige tudo junto
                for (int i = 0; i < 4; i++)
                {
                    Reach(1, palmTarget - (PalmPos(1) - hand[1].position), new Vector3(1f, -0.6f, -0.2f), drinkW);
                    AimFingers(1, volta, drinkW);
                    Palm(1, transform.InverseTransformDirection(-side), drinkW);
                }
            }
            // fone: a mão sobe até a frente do queixo segurando o microfone do fio ("alô, primo?"). O antebraço vai
            // mais para a frente que para dentro: apontado para o queixo, o polegar entrava no rosto.
            Arm(1, new Vector3(0.3f, -0.75f, 0.5f), new Vector3(-0.3f, 0.7f, 0.65f), foneW);
            Palm(1, new Vector3(-0.6f, 0f, -0.8f), foneW);
            // bilhete: braço armado acima da cabeça (arremesso)
            Arm(1, new Vector3(0.3f, 0.9f, 0.25f), new Vector3(0.05f, 0.6f, 0.8f), throwW);
            Palm(1, new Vector3(0f, 0.2f, 1f), throwW);
            // celular na orelha (professor ao telefone)
            Arm(1, new Vector3(0.55f, -0.45f, 0.05f), new Vector3(-0.2f, 0.98f, -0.05f), earW);
            Palm(1, new Vector3(-1f, 0f, 0f), earW);
            // mão levantada (acenando)
            Arm(1, new Vector3(0.22f, 1f, 0.12f), new Vector3(0.08f + Mathf.Sin(t * 9f) * 0.18f, 1f, 0.05f), raiseW);
            Palm(1, new Vector3(0f, 0f, 1f), raiseW);
            // quanto os dedos fecham depende do objeto: objeto chato (celular, papel) = mão quase aberta por baixo;
            // garrafa = dedos em volta; bolinha = punho; microfone fininho = pinça
            grip[1] = Mathf.Lerp(grip[1], 10f, Mathf.Max(phoneW, earW));
            // garrafa: dedos só encostados. Fechados em volta (25°), entravam na garrafa e o "afasta da palma" a
            // empurrava ~10 cm para longe da mão (garrafa solta no ar)
            grip[1] = Mathf.Lerp(grip[1], 6f, drinkW);
            grip[1] = Mathf.Lerp(grip[1], 62f, throwW);
            grip[1] = Mathf.Lerp(grip[1], 45f, foneW);
            grip[1] = Mathf.Lerp(grip[1], 8f, raiseW);
            grip[0] = Mathf.Lerp(grip[0], 10f, colaW);
            Curl(0, grip[0]);
            Curl(1, grip[1]);

            // sentado: o quadril vai para a altura do assento e recua um pouco da carteira
            if (seatW > 0f)
            {
                float hip = hips.position.y - transform.position.y;
                model.localPosition = new Vector3(0f, seatW * (set.seatHipHeight - hip), -seatW * set.seatBack);
            }

            UpdateProp();

            // gotas de suor acompanham a cabeça do modelo
            var sweat = cv.SweatRoot;
            if (sweat != null && sweat.gameObject.activeSelf)
            {
                sweat.position = head.position + Vector3.up * (0.08f - Mathf.Repeat(t * 0.25f, 0.12f)) * scale;
                sweat.rotation = Quaternion.LookRotation(transform.forward);
                sweat.localScale = Vector3.one * (scale * 0.85f);
            }
        }

        // ------------------------------------------------------------------ objeto na mão

        /// <summary>Centro da palma (no meio do caminho entre o pulso e os dedos).</summary>
        Vector3 PalmPos(int s) => hand[s].position + FingerDir(s) * 0.55f;

        /// <summary>Ponto um pouco à frente da palma, onde o objeto fica (n = para onde a palma olha).</summary>
        Vector3 InPalm(int s, float lift, out Vector3 n)
        {
            n = PalmNormal(s);
            return PalmPos(s) + n * lift * scale;
        }

        void UpdateProp()
        {
            if (cv.prop != propKind || (propObj == null && cv.prop != HandProp.Nenhum))
            {
                if (propObj != null) Destroy(propObj.gameObject);
                propKind = cv.prop;
                propObj = HandProps.Build(propKind, transform);
            }
            if (propObj == null) return;

            Vector3 n;
            if (propKind == HandProp.Fone)
            {
                UpdateFone();
                return;
            }
            propObj.localScale = Vector3.one * ((propKind == HandProp.Garrafinha ? BottleScale : PropScale) * scale); // maior que o real: tem que dar para ler de longe
            switch (propKind)
            {
                case HandProp.Celular:
                    // deitado na palma, tela virada para onde a palma olha (para cima no colo; para a orelha ao telefone)
                    propObj.position = InPalm(1, 0f, out n);
                    propObj.rotation = Quaternion.LookRotation(n, FingerDir(1));
                    RestOnPalm(1, n);
                    break;
                case HandProp.Colinha:
                    propObj.position = InPalm(0, 0f, out n);
                    propObj.rotation = Quaternion.LookRotation(n, Dir(Vector3.up));
                    RestOnPalm(0, n);
                    break;
                case HandProp.Garrafinha:
                {
                    // presa à palma (do lado para onde a palma olha), tampa para a boca: no gole, a mão já foi levada
                    // até a garrafa (pose acima), então ela cai com a tampa na boca
                    BottleFrame(out Vector3 axis, out Vector3 side, out Vector3 center, out float radius);
                    n = PalmNormal(1);
                    // mesmo andamento da mão: o IK do gole roda 4 vezes com peso drinkW, então a mão chega na
                    // posição bem antes de drinkW = 1 (1 - (1 - w)^4). Com a garrafa no ritmo de drinkW, no meio da
                    // subida ela ficava deitada na frente dos olhos.
                    float k = 1f - Mathf.Pow(1f - drinkW, 4f);
                    // enquanto a mão sobe, a garrafa sai de pé (como estava na carteira) e vai deitando até o gole;
                    // antes ela já aparecia deitada, atravessada na frente da mão ainda na carteira
                    var deitada = Quaternion.FromToRotation(Vector3.up, -axis);
                    propObj.rotation = Quaternion.Slerp(transform.rotation, deitada, k);
                    // na mão durante a subida; no gole, no lugar do gole (tampa na boca). Os dedos fechando mudam a
                    // palma de lugar, e presa só à palma a tampa parava a uns 9 cm da boca.
                    propObj.position = Vector3.Lerp(PalmPos(1) + n * (radius + PalmRadius * scale), center, k);
                    RestOnPalm(1, n);
                    break;
                }
                default:
                    // bolinha em cima do punho fechado: aparece até por trás
                    propObj.position = InPalm(1, 0.03f, out _) + Dir(Vector3.up) * 0.07f * scale;
                    propObj.rotation = transform.rotation;
                    break;
            }
        }

        Vector3 Mouth() => head.TransformPoint(mouthLocal);
        /// <summary>Onde fica a boca do modelo agora (mundo).</summary>
        public Vector3 MouthPoint => Mouth();

        /// <summary>Pontos da mão que o objeto não pode atravessar (osso da mão, palma e cada junta dos dedos).</summary>
        public void HandPoints(int s, System.Collections.Generic.List<(Vector3 p, float r)> into)
        {
            into.Clear();
            into.Add((hand[s].position, PalmRadius * scale));
            into.Add((PalmPos(s), PalmRadius * scale));
            foreach (var f0 in fingers[s])
                for (var b = f0; b != null; b = b.childCount > 0 ? b.GetChild(0) : null)
                    into.Add((b.position, FingerRadius * scale));
        }

        readonly System.Collections.Generic.List<(Vector3 p, float r)> handPts = new();

        /// <summary>Afasta o objeto da palma (na direção <paramref name="n"/>) até nenhum osso da mão ficar dentro dele.</summary>
        void RestOnPalm(int s, Vector3 n)
        {
            HandPoints(s, handPts);
            float step = 0.004f * scale;
            for (int i = 0; i < 40; i++)
            {
                bool touching = false;
                foreach (var (p, r) in handPts)
                    if (HandProps.Touches(propObj, p, r)) { touching = true; break; }
                if (!touching) return;
                propObj.position += n * step;
            }
        }

        /// <summary>Fone na orelha direita, microfone na mão e o fio descendo até o bolso.</summary>
        void UpdateFone()
        {
            propObj.localPosition = Vector3.zero;
            propObj.localRotation = Quaternion.identity;
            propObj.localScale = Vector3.one;
            float k = PropScale * scale;
            Vector3 headUp = head.rotation * headUpLocal, earOut = head.rotation * earOutLocal;

            var bud = propObj.Find("Fone");
            bud.position = head.TransformPoint(earLocal) + earOut * 0.012f * k;
            bud.rotation = Quaternion.LookRotation(earOut, headUp);
            bud.localScale = Vector3.one * (k * 1.4f); // exagerado: tem que aparecer de longe
            Vector3 stemEnd = bud.position - headUp * 0.045f * k;

            var mic = propObj.Find("Microfone");
            mic.position = InPalm(1, 0.012f, out _);
            mic.rotation = Quaternion.FromToRotation(Vector3.up, (stemEnd - mic.position).normalized);
            mic.localScale = Vector3.one * k;

            // bolso da calça (de pé) ou em cima da coxa (sentado)
            Vector3 pocket = hips.position + Dir(new Vector3(0.14f, 0.02f, 0.08f + 0.1f * seatW)) * scale;
            float wire = 0.006f * scale;
            HandProps.Segment(propObj.Find("Fio"), stemEnd, mic.position + mic.up * 0.03f * k, wire);
            HandProps.Segment(propObj.Find("Fio2"), mic.position - mic.up * 0.022f * k, pocket, wire);
        }
    }
}
