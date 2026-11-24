using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using static ProvaImpossivel.EditorTools.Rig;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Os planos do trailer. Cada um recarrega a sala, aciona as mecânicas reais do jogo (teclas virtuais ou os
    /// métodos públicos que os acontecimentos já usam) e move a câmera. Nada de objeto falso em cena.
    /// </summary>
    public static class TrailerShotsPlanos
    {
        // fala nova do roteiro v2 (só no balão do trailer; o jogo não muda)
        const string PuxaSacoNova = "PROFESSOOOR! Ele tá respirando na direção da minha prova!";

        static readonly (string id, Func<IEnumerator> shot)[] Planos =
        {
            ("00_folha", Folha),
            ("01_prof_anda", ProfAnda),
            ("02_celular", Celular),
            ("03_nerd", NerdOlhando),
            ("04_fundao", Fundao),
            ("05_fe", Fe),
            ("06_colinha", Colinha),
            ("07_garrafinha", Garrafinha),
            ("08_bilhete", Bilhete),
            ("09_fone", Fone),
            ("10_dorminhoco", Dorminhoco),
            ("11_puxasaco", PuxaSaco),
            ("12_prof_sinto", ProfSinto),
            ("13_cone", Cone),
            ("14_flagrante", Flagrante),
            ("15_hud", HudReal),
            ("16_aprovado", () => Resultado("16_aprovado", LevelResult.Aprovado)),
            ("17_expulso", () => Resultado("17_expulso", LevelResult.Flagrado)),
            ("18_dp_final", AlunoDP),
            ("19_sala", SalaLarga),
            // diagnóstico (só rodam pedidos pelo nome com -trailerOnly; nunca entram no trailer)
            ("diag_prof_lado", () => DiagProf("diag_prof_lado", false)),
            ("diag_prof_frente", () => DiagProf("diag_prof_frente", true)),
            ("diag_jogador", DiagJogador),
            ("diag_nerd", DiagNerd),
            ("diag_lousa", DiagLousa),
            ("diag_comparar_tras", () => DiagComparar("diag_comparar_tras", 0)),
            ("diag_comparar_lado", () => DiagComparar("diag_comparar_lado", 1)),
            ("diag_comparar_frente", () => DiagComparar("diag_comparar_frente", 2)),
        };

        /// <summary>
        /// Diagnóstico: o MESMO modelo do professor, limpo (só o clipe Walk do pacote, sem nenhum código nosso), andando
        /// ao lado do professor do jogo. Filma e mede a postura dos dois a cada quadro (log [Postura]).
        /// vista: 0 = de trás, 1 = de lado, 2 = de frente.
        /// </summary>
        static IEnumerator DiagComparar(string id, int vista)
        {
            StopTpc(true);
            SetDof(false);
            var set = Resources.Load<CharacterModelSet>("Personagens/ModelosPersonagens");
            var prefab = set.Pick((CharacterLook)1, 0, out var entry);
            var f = Facing(Jv);
            var r = Vector3.Cross(Vector3.up, f);
            var corredor = OnNav(Player.transform.position + r * 0.9f);
            var de = OnNav(corredor + f * 3.5f);
            var ate = OnNav(corredor - f * 2.5f);
            var dir = (ate - de); dir.y = 0f; dir.Normalize();
            Prof.transform.position = de;
            Prof.transform.rotation = Quaternion.LookRotation(dir);
            Prof.WalkTo(ate, false);

            // boneco limpo, 1,1 m ao lado (à direita de quem anda), mesma escala, mesma velocidade
            var lado = Vector3.Cross(Vector3.up, dir);
            var puro = Object.Instantiate(prefab);
            puro.name = "Modelo PURO (diagnostico)";
            puro.transform.localScale = Vector3.one * entry.scale;
            puro.transform.SetPositionAndRotation(de + lado * 1.1f, Quaternion.LookRotation(dir));
            foreach (var c in puro.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var an = puro.GetComponent<Animator>();
            if (an == null) an = puro.AddComponent<Animator>();
            an.applyRootMotion = false;
            var graph = UnityEngine.Playables.PlayableGraph.Create("diag_puro");
            var clip = UnityEngine.Animations.AnimationClipPlayable.Create(graph, set.walk);
            clip.SetApplyFootIK(false);
            var outp = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "puro", an);
            outp.SetSourcePlayable(clip);
            graph.Play();

            var pa = Prof.visual.Skin.Model.GetComponent<Animator>();
            var stats = new Dictionary<string, List<float>>();
            void Add(string k, float v) { if (!stats.TryGetValue(k, out var l)) stats[k] = l = new List<float>(); l.Add(v); }
            void Medir(string quem, Animator a, Transform root)
            {
                var hips = a.GetBoneTransform(HumanBodyBones.Hips).position;
                var neck = a.GetBoneTransform(HumanBodyBones.Neck).position;
                var lL = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                var rL = a.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
                var lA = a.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                var rA = a.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
                var lF = a.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                var rF = a.GetBoneTransform(HumanBodyBones.RightFoot).position;
                Vector3 R = root.right, F = root.forward, U = Vector3.up;
                Vector3 coluna = neck - hips;
                Add(quem + " inclinacao_lateral", Mathf.Atan2(Vector3.Dot(coluna, R), Vector3.Dot(coluna, U)) * Mathf.Rad2Deg);
                Add(quem + " inclinacao_frente", Mathf.Atan2(Vector3.Dot(coluna, F), Vector3.Dot(coluna, U)) * Mathf.Rad2Deg);
                Vector3 pelve = rL - lL, ombro = rA - lA;
                Add(quem + " giro_pelve", Vector3.SignedAngle(R, Vector3.ProjectOnPlane(pelve, U), U));
                Add(quem + " giro_ombros", Vector3.SignedAngle(R, Vector3.ProjectOnPlane(ombro, U), U));
                Add(quem + " ombro_torto", Mathf.Atan2(Vector3.Dot(ombro, U), Vector3.Dot(ombro, R)) * Mathf.Rad2Deg);
                Add(quem + " pe_esq_lado", Vector3.Dot(lF - hips, R) / root.lossyScale.x);
                Add(quem + " pe_dir_lado", Vector3.Dot(rF - hips, R) / root.lossyScale.x);
                Vector3 v = root == Prof.transform ? Prof.GetComponent<NavMeshAgent>().velocity : dir;
                v.y = 0f;
                if (v.sqrMagnitude > 0.05f) Add(quem + " corpo_vs_movimento", Vector3.SignedAngle(v, F, U));
            }

            float velocidade = 1.1f;
            yield return Capture(id, 4f, (t, p) =>
            {
                float dt = 1f / TrailerShots.FPS;
                clip.SetSpeed(CharacterModelSkin.PlaybackRate(velocidade, CharacterModelSkin.WalkStride, entry.scale, 0.5f, 2.2f));
                puro.transform.position += dir * velocidade * dt;
                if (t > 0.6f) // depois que os dois entraram na caminhada
                {
                    Medir("JOGO", pa, Prof.transform);
                    Medir("PURO", an, puro.transform);
                }
                var meio = (Prof.transform.position + puro.transform.position) * 0.5f + Vector3.up * 0.95f;
                Vector3 cam = vista == 0 ? meio - dir * 3.4f : vista == 1 ? meio - lado * 4.2f : meio + dir * 3.4f;
                Pose(cam + Vector3.up * 0.15f, meio, 50f);
            });
            foreach (var kv in stats.OrderBy(k => k.Key.Substring(5)).ThenBy(k => k.Key))
            {
                var l = kv.Value;
                Debug.Log($"[Postura] {id} {kv.Key,-32} média {l.Average(),7:F2}  min {l.Min(),7:F2}  max {l.Max(),7:F2}");
            }
            graph.Destroy();
            Object.Destroy(puro);
        }

        /// <summary>Diagnóstico da lousa: de frente (perto) e de onde o jogador senta (longe).</summary>
        static IEnumerator DiagLousa()
        {
            StopTpc(true);
            SetDof(false);
            var q = GameObject.Find("Questao no Quadro");
            var centro = q != null ? q.transform.position : Jv.HeadPosition;
            var f = Facing(Jv);
            var olho = Jv.HeadPosition;
            yield return Capture("diag_lousa", 2f, (t, p) =>
            {
                if (t < 1f) Pose(centro - f * 3.2f + Vector3.up * 0.1f, centro, 60f);
                else Pose(olho + Vector3.up * 0.1f, centro, 60f);
            });
        }

        /// <summary>Diagnóstico do indicador do Nerd: mesmo enquadramento do plano 03, em pasta própria.</summary>
        static IEnumerator DiagNerd()
        {
            StopTpc(true);
            var nerd = Find<NerdStudent>();
            var nv = nerd.visual;
            nerd.GlanceSoon();
            yield return Capture("diag_nerd", 4f, (t, p) =>
            {
                Pose(Rel(nv, 2.1f, -0.4f, 0.15f), nv.HeadPosition + Vector3.up * 0.25f, 44f,
                     Vector3.Distance(Rel(nv, 2f, -0.4f, 0.15f), nv.HeadPosition));
            });
        }

        /// <summary>Diagnóstico da caminhada: o professor anda em linha reta pelo corredor; câmera de lado ou de frente.</summary>
        static IEnumerator DiagProf(string id, bool deFrente)
        {
            StopTpc(true);
            SetDof(false);
            var f = Facing(Jv);
            var r = Vector3.Cross(Vector3.up, f);
            var corredor = OnNav(Player.transform.position + r * 0.9f);
            var de = OnNav(corredor + f * 3.5f);
            var ate = OnNav(corredor - f * 2.5f);
            Prof.transform.position = de;
            Prof.WalkTo(ate, false);
            yield return null;
            yield return Capture(id, 4f, (t, p) =>
            {
                var c = Prof.transform.position + Vector3.up * 0.9f;
                var pos = deFrente ? c - (de - ate).normalized * -2.8f + Vector3.up * 0.2f : c + r * 3f + Vector3.up * 0.3f;
                Pose(pos, c, 50f);
            });
        }

        /// <summary>Diagnóstico da caminhada do jogador: levanta e anda para o lado da câmera (D), visto de lado.</summary>
        static IEnumerator DiagJogador()
        {
            StopTpc(true);
            SetDof(false);
            Player.StandUp();
            yield return null;
            var f = Facing(Jv);
            var r = Vector3.Cross(Vector3.up, f);
            var c0 = Player.transform.position + Vector3.up * 0.9f;
            Pose(c0 - f * 3f + Vector3.up * 0.3f, c0, 55f); // câmera atrás: D anda para a direita da tela
            Press(Key.D);
            yield return Capture("diag_jogador", 3f, (t, p) =>
            {
                var c = Player.transform.position + Vector3.up * 0.9f;
                if (t > 1.6f) Press(Key.W);
                Pose(c - f * 3f + Vector3.up * 0.4f, c, 55f);
            });
            Release();
        }

        public static IEnumerator Run(List<string> only)
        {
            var log = new List<string>();
            foreach (var (id, shot) in Planos)
            {
                if (only.Count > 0 && !only.Contains(id)) continue;
                if (only.Count == 0 && id.StartsWith("diag_")) continue; // diagnóstico nunca entra no trailer
                Ui = TrailerUi.Nada;
                ShowBubbles = true;
                PlayerHeadYaw = 0f;
                SetDof(true);
                yield return Fresh();
                yield return shot();
                log.Add(id);
            }
            Debug.Log("[Trailer] Planos feitos: " + string.Join(", ", log));
        }

        static PlayerController Player => PlayerController.I;
        static ProfessorAI Prof => ProfessorAI.I;
        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();
        static CharacterVisual Pv => Prof.visual;
        static CharacterVisual Jv => Player.visual;

        static void StopTpc(bool stop)
        {
            var tpc = Cam.GetComponent<ThirdPersonCamera>();
            if (tpc != null) tpc.enabled = !stop;
        }

        static Vector3 OnNav(Vector3 p)
        {
            return NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas) ? hit.position : p;
        }

        // ------------------------------------------------------------------ planos

        // 0:00 — a folha da prova na carteira, de cima, câmera descendo devagar
        static IEnumerator Folha()
        {
            StopTpc(true);
            var f = Facing(Jv);
            // a folha fica no tampo da carteira, logo à frente do jogador: acha o tampo com um raio de cima
            var probe = Jv.HeadPosition + f * 0.6f + Vector3.up * 0.6f;
            var top = Physics.Raycast(probe, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point : Jv.HeadPosition + f * 0.6f - Vector3.up * 0.55f;
            Debug.Log("[Trailer] tampo da carteira em " + top + " (cabeça " + Jv.HeadPosition + ")");
            var side = Vector3.Cross(Vector3.up, f);
            yield return Capture("00_folha", 3.5f, (t, p) =>
            {
                float e = Ease(p);
                var pos = top + Vector3.up * Mathf.Lerp(1.05f, 0.62f, e) + side * Mathf.Lerp(0.45f, 0.3f, e) + f * 0.1f;
                Pose(pos, top + f * 0.04f, 42f);
            });
        }

        // a) professor andando devagar entre as carteiras, visto de trás das cadeiras (câmera baixa, dolly lateral)
        static IEnumerator ProfAnda()
        {
            StopTpc(true);
            var f = Facing(Jv);
            var r = Vector3.Cross(Vector3.up, f);
            // o corredor ao lado do jogador (ponto andável); ele vem andando por ele, em direção à câmera
            var corredor = OnNav(Player.transform.position + r * 0.9f);
            if (Vector3.Distance(corredor, Player.transform.position) < 0.5f) corredor = OnNav(Player.transform.position - r * 0.9f);
            var lado = (corredor - Player.transform.position); lado.y = 0f; lado.Normalize();
            Prof.WalkTo(OnNav(corredor + f * 1.2f), false);
            yield return Capture("01_prof_anda", 4.5f, (t, p) =>
            {
                // câmera baixa no próprio corredor, atrás da fileira do jogador, deslizando um pouco de lado: com os
                // personagens novos (mais altos) a câmera entre as cadeiras tinha um aluno tapando o professor
                var pos = corredor - f * 0.8f + lado * Mathf.Lerp(-0.15f, 0.1f, Ease(p)) + Vector3.up * 1.0f;
                Pose(pos, Pv.HeadPosition - Vector3.up * 0.3f, 42f);
            });
        }

        // Celular (tecla 2 segurada): a mecânica real, com o celular na mão
        static IEnumerator Celular()
        {
            StopTpc(true);
            Press(Key.Digit2);
            yield return Capture("02_celular", 2.5f, (t, p) =>
            {
                var e = Ease(p);
                // o celular agora aparece na mão (modelo do jogo): enquadra rosto e aparelho juntos
                Pose(Rel(Jv, Mathf.Lerp(1.7f, 1.35f, e), 0.45f, 0.05f), Jv.HeadPosition - Vector3.up * 0.35f, 44f);
            });
        }

        // d) Nerd olhando de lado: aviso "..." e depois "O_O" em cima dele
        static IEnumerator NerdOlhando()
        {
            StopTpc(true);
            var nerd = Find<NerdStudent>();
            var nv = nerd.visual;
            float d = Vector3.Distance(Player.transform.position, nerd.transform.position);
            if (d > nerd.hintDistance) Debug.LogWarning("[Trailer] Nerd longe do jogador (" + d.ToString("0.0") + " m): o ícone pode não aparecer.");
            nerd.GlanceSoon();
            yield return Capture("03_nerd", 3.6f, (t, p) =>
            {
                var e = Ease(p);
                Pose(Rel(nv, Mathf.Lerp(2.3f, 1.8f, e), Mathf.Lerp(-0.5f, -0.25f, e), 0.15f), nv.HeadPosition + Vector3.up * 0.25f, 44f,
                     Vector3.Distance(Rel(nv, 2f, -0.4f, 0.15f), nv.HeadPosition));
            });
        }

        // Fundão: a fala dele, câmera baixa empurrando
        static IEnumerator Fundao()
        {
            StopTpc(true);
            var fundao = Find<BackRowStudent>();
            var fv = fundao.visual;
            ShowBubbles = false; // o balão saía cortado no topo; a legenda do trailer leva a fala
            fundao.Say(Falas.FundaoColaBoa[0], 3f);
            yield return Capture("04_fundao", 2.5f, (t, p) =>
            {
                var e = Ease(p);
                Pose(Rel(fv, Mathf.Lerp(1.6f, 1.2f, e), 0.3f, -0.25f), fv.HeadPosition - Vector3.up * 0.1f, 42f);
            });
        }

        // "Fé" (piada de texto): o jogador olhando pro teto, câmera baixa
        static IEnumerator Fe()
        {
            StopTpc(true);
            yield return Capture("05_fe", 2.5f, (t, p) =>
            {
                Jv.headPitch = Mathf.Lerp(-5f, -28f, EaseOut(p * 2f));
                Jv.expression = Expression.Nervoso;
                Jv.sweat = 1f;
                var e = Ease(p);
                // de lado e um pouco abaixo da cabeça (acima do tampo), olhando para cima com ele
                Pose(Rel(Jv, Mathf.Lerp(0.95f, 0.8f, e), -0.75f, -0.12f), Jv.HeadPosition + Vector3.up * 0.25f, 46f);
            });
        }

        // b) colinha na manga (Q segurado) e o professor se vira no momento exato (pigarro "...")
        static IEnumerator Colinha()
        {
            StopTpc(true);
            var f = Facing(Jv);
            var r = Vector3.Cross(Vector3.up, f);
            bool virou = false;
            yield return Capture("06_colinha", 4.5f, (t, p) =>
            {
                if (t < 0.02f) Press(Key.Q);
                if (!virou && t >= 1.4f)
                {
                    virou = true;
                    Prof.WalkTo(OnNav(Player.transform.position + f * 2.2f), true); // "..." e depois vira pro jogador
                }
                if (t >= 3.6f) Release();
                var e = Ease(p);
                // por cima do ombro do jogador, empurrando para o professor lá na frente
                var pos = Jv.HeadPosition - f * Mathf.Lerp(0.75f, 0.45f, e) + r * 0.42f + Vector3.up * 0.05f;
                var look = Vector3.Lerp(Jv.HeadPosition + f * 1.2f - Vector3.up * 0.5f, Pv.HeadPosition, Ease((t - 1.2f) / 1.2f));
                Pose(pos, look, 50f, Vector3.Distance(pos, Pv.HeadPosition));
            });
        }

        // Garrafinha (G segurado)
        static IEnumerator Garrafinha()
        {
            StopTpc(true);
            yield return Capture("07_garrafinha", 2.5f, (t, p) =>
            {
                if (t < 0.02f) Press(Key.G);
                var e = Ease(p);
                // mais de lado (perfil): de frente a garrafa cobria o rosto e parecia enfiada nele
                Pose(Rel(Jv, Mathf.Lerp(0.95f, 0.8f, e), -Mathf.Lerp(1.25f, 1.05f, e), 0.05f), Jv.HeadPosition - Vector3.up * 0.2f, 44f);
            });
        }

        // c) bilhete (B): o arremesso real até o Nerd; a câmera chicoteia do jogador para a mesa dele
        static IEnumerator Bilhete()
        {
            StopTpc(true);
            var nerd = Find<NerdStudent>();
            var cheats = Player.GetComponent<PlayerCheats>();
            var meio = (Jv.HeadPosition + nerd.visual.HeadPosition) * 0.5f;
            var lado = Vector3.Cross(Vector3.up, (nerd.transform.position - Player.transform.position).normalized);
            var camPos = meio + lado * 2.4f + Vector3.up * 0.2f;
            bool jogou = false;
            yield return Capture("08_bilhete", 3.5f, (t, p) =>
            {
                if (!jogou && t >= 0.5f) { jogou = true; if (!cheats.ThrowAt(nerd)) Debug.LogWarning("[Trailer] Bilhete não saiu."); }
                float w = Ease((t - 0.7f) / 0.45f); // chicote
                var look = Vector3.Lerp(Jv.HeadPosition - Vector3.up * 0.3f, nerd.visual.HeadPosition - Vector3.up * 0.3f, w);
                Pose(camPos + lado * Mathf.Lerp(0f, -0.3f, Ease(p)), look, 46f);
            });
        }

        // Fone (F segurado: mandar a pergunta)
        static IEnumerator Fone()
        {
            StopTpc(true);
            yield return Capture("09_fone", 2.5f, (t, p) =>
            {
                if (t < 0.02f) Press(Key.F);
                var e = Ease(p);
                Pose(Rel(Jv, Mathf.Lerp(0.9f, 0.7f, e), 0.55f, 0f), Jv.HeadPosition - Vector3.up * 0.05f, 40f);
            });
        }

        // e) Dorminhoco cabeceando (zZZ crescendo) e roncando
        static IEnumerator Dorminhoco()
        {
            StopTpc(true);
            var s = Find<SleeperStudent>();
            var sv = s.visual;
            s.StartCoroutine(s.SnoreWarning(2.0f));
            bool roncou = false;
            yield return Capture("10_dorminhoco", 3.6f, (t, p) =>
            {
                // a fala do ronco é sorteada no jogo; o roteiro pede esta, que existe em Falas.DorminhocoRonco
                if (!roncou && t >= 2.1f) { roncou = true; s.Say(Falas.DorminhocoRonco[2], 3f); }
                var e = Ease(p);
                Pose(Rel(sv, 1.5f, Mathf.Lerp(-0.7f, -0.2f, e), 0.0f), sv.HeadPosition - Vector3.up * 0.15f, 42f);
            });
        }

        // f) Puxa-saco se virando para delatar
        static IEnumerator PuxaSaco()
        {
            StopTpc(true);
            var sn = Find<SnitchStudent>();
            var v = sn.visual;
            sn.ForceWatch();
            bool falou = false;
            yield return Capture("11_puxasaco", 3.6f, (t, p) =>
            {
                if (!falou && (sn.Watching || t >= 1.6f)) { falou = true; sn.Say(PuxaSacoNova, 3f); }
                // ela se vira para trás (para o jogador): a câmera fica do lado de trás, para pegar o rosto
                var e = EaseOut(p);
                Pose(Rel(v, -Mathf.Lerp(1.9f, 1.3f, e), 0.35f, -0.1f), v.HeadPosition - Vector3.up * 0.1f, 44f);
            });
        }

        // 0:26 — close do professor: "Eu SINTO a cola." (ele parado na frente da turma)
        static IEnumerator ProfSinto()
        {
            StopTpc(true);
            Prof.enabled = false;
            Pv.transform.position = new Vector3(0f, Pv.transform.position.y, -4.4f);
            Pv.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            ShowBubbles = false; // close: a legenda do trailer leva a fala
            Prof.Say("Eu corrijo prova há 30 anos. Eu SINTO a cola.", 3.2f);
            Pv.Talk(2.5f);
            yield return Capture("12_prof_sinto", 3.2f, (t, p) =>
            {
                Pv.expression = Expression.Desconfiado;
                var e = Ease(p);
                // professor 1,15× com cabelo alto: a câmera recua e mira mais alto para não cortar a cabeça
                Pose(Rel(Pv, Mathf.Lerp(2.3f, 1.75f, e), 0.25f, -0.25f), Pv.HeadPosition - Vector3.up * 0.3f, 40f);
            });
        }

        // h) vista de cima: o cone de visão do professor passando por cima da sala e do jogador
        static IEnumerator Cone()
        {
            StopTpc(true);
            SetDof(false);
            ShowBubbles = false; // com a câmera andando, os balões do jogo pulavam de canto e saíam cortados
            var f = Facing(Jv);
            var c = Player.transform.position;
            bool alertou = false;
            yield return Capture("13_cone", 4.5f, (t, p) =>
            {
                // ele "ouve" algo perto do jogador (a mesma reação de quando o Nerd faz escândalo): o cone esquenta e varre a sala
                if (!alertou && t >= 0.8f) { alertou = true; Prof.Alert(c, 40f, "um barulho estranho", Infracao.Outra); }
                var e = Ease(p);
                var pos = c + new Vector3(Mathf.Lerp(2.6f, -1.6f, e), 2.5f, Mathf.Lerp(2.6f, 2.2f, e));
                Pose(pos, c + f * 2.0f, 64f);
            });
        }

        // g) flagrante: professor virando bravo, "!!!", clarão vermelho
        static IEnumerator Flagrante()
        {
            StopTpc(true);
            Ui = TrailerUi.Nada; // o clarão fica fora dos painéis
            ShowBubbles = false;
            bool pegou = false, parado = false;
            yield return Capture("14_flagrante", 3.2f, (t, p) =>
            {
                if (!pegou && t >= 0.7f)
                {
                    pegou = true;
                    if (!Prof.CatchRedHanded("celular na mão", Infracao.Celular)) Debug.LogWarning("[Trailer] Flagrante não aconteceu (trégua?).");
                }
                // A IA partiria andando até o jogador e sairia de perfil do quadro. Uns quadros depois do flagrante
                // (o "!!!" já apareceu) ele para onde está e vira de frente para o jogador, bravo.
                if (pegou && !parado && t >= 0.8f)
                {
                    parado = true;
                    Prof.enabled = false;
                    var ag = Prof.GetComponent<NavMeshAgent>();
                    if (ag != null && ag.enabled) { ag.isStopped = true; ag.velocity = Vector3.zero; ag.updateRotation = false; ag.ResetPath(); }
                }
                if (parado)
                {
                    var alvo = Player.transform.position - Pv.transform.position; alvo.y = 0f;
                    if (alvo.sqrMagnitude > 0.01f)
                        Pv.transform.rotation = Quaternion.RotateTowards(Pv.transform.rotation, Quaternion.LookRotation(alvo), 240f / 30f);
                    Pv.expression = Expression.Bravo;
                    Pv.headYaw = 0f;
                    Pv.headPitch = 0f;
                }
                var toPlayer = (Player.transform.position - Pv.transform.position); toPlayer.y = 0f; toPlayer.Normalize();
                float push = t < 0.7f ? 0f : EaseOut((t - 0.7f) / 0.25f);
                // enquadra a cabeça e o "!!!" que aparece em cima dele
                var pos = Pv.HeadPosition + toPlayer * Mathf.Lerp(3.1f, 2.3f, push) + Vector3.up * -0.2f;
                Pose(pos, Pv.HeadPosition + Vector3.up * 0.2f, Mathf.Lerp(46f, 40f, push), Vector3.Distance(pos, Pv.HeadPosition));
            });
        }

        // 0:35 — a HUD real, câmera do jogo
        static IEnumerator HudReal()
        {
            StopTpc(false);
            Ui = TrailerUi.Jogo;
            SetDof(false);
            yield return Capture("15_hud", 3.5f, (t, p) => { });
        }

        // i) telas finais reais sobre a sala (dados possíveis no Fácil)
        static IEnumerator Resultado(string id, LevelResult r)
        {
            StopTpc(true);
            Ui = TrailerUi.Resultado;
            SetDof(false);
            var gm = GameManager.I;
            bool aprovado = r == LevelResult.Aprovado;
            var sum = MatchSummary.Build(gm.facil, r, aprovado ? 6f : 0f, aprovado ? 95f : 300f, aprovado ? 0 : 3, aprovado ? 1 : 0,
                                         aprovado ? 6 : 0, aprovado ? 3 : 3, aprovado ? 1 : 7);
            string msg = aprovado ? Falas.FinalAprovado[2] : Falas.FinalFlagrado[1];
            HUD.I.ShowMatchResult(sum, msg, aprovado ? HUD.Green : HUD.Red, () => { }, () => { }, () => { });
            ShowBubbles = false;
            // a tela assenta antes de gravar: no primeiro quadro o botão "Jogar de novo" ainda aparece aceso
            for (int i = 0; i < 10; i++) yield return null;
            var start = Cam.transform.position;
            var fwd = Cam.transform.forward;
            yield return Capture(id, 3.5f, (t, p) =>
            {
                Cam.transform.position = start + fwd * Mathf.Lerp(0f, 0.6f, Ease(p));
            });
        }

        // 0:45 — gag final: você, suando, lendo a colinha na manga; vira para a câmera e confessa (a fala é a legenda
        // do trailer: "Relaxa... no máximo eu levo DP de novo.")
        static IEnumerator AlunoDP()
        {
            StopTpc(true);
            ShowBubbles = false; // close: a legenda do trailer leva a fala
            // a câmera fica fixa em relação ao CORPO (a direção da cabeça muda quando ele vira para a câmera)
            var f = Player.transform.forward; f.y = 0f; f.Normalize();
            var r = Vector3.Cross(Vector3.up, f);
            var head0 = Jv.HeadPosition;
            yield return Capture("18_dp_final", 4.0f, (t, p) =>
            {
                if (t < 0.02f) Press(Key.Q); // a colinha na manga (a mecânica real)
                Jv.expression = Expression.Nervoso;
                Jv.sweat = 1f;
                var e = Ease(p);
                var pos = head0 + f * Mathf.Lerp(1.45f, 1.15f, e) + r * 0.3f - Vector3.up * 0.12f;
                // primeiro lendo a cola; em 1,0 s vira a cabeça e encara a câmera (quebra da quarta parede)
                var toCam = pos - head0; toCam.y = 0f;
                float olhar = Vector3.SignedAngle(f, toCam, Vector3.up);
                PlayerHeadYaw = Mathf.Lerp(0f, olhar + 22f, Ease((t - 1.0f) / 0.35f)); // +22: desfaz o giro da pose da colinha
                Pose(pos, head0 - Vector3.up * 0.2f + r * -0.05f, 40f);
            });
        }

        // fundo da capa: a sala larga, dolly lento
        static IEnumerator SalaLarga()
        {
            StopTpc(true);
            SetDof(false);
            ShowBubbles = false; // fundo da capa: o "z..." do dorminhoco piscava no canto
            yield return Capture("19_sala", 4.5f, (t, p) =>
            {
                var e = Ease(p);
                Pose(new Vector3(Mathf.Lerp(3.4f, 1.6f, e), 2.4f, 5.6f), new Vector3(Mathf.Lerp(-0.6f, -1.2f, e), 0.8f, -2f), 60f);
            });
        }
    }
}
