using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Polimento da entrega 1.0 (24/11): trilha, Cara de Paisagem no tutorial e correções do fluxo.</summary>
    public class EntregaPlayTests
    {
        [TearDown]
        public void Limpa() => Time.timeScale = 1f;

        /// <summary>Normal sem acontecimentos sorteados nem viradas do Puxa-saco (só o que o teste provoca).</summary>
        static IEnumerator ComecaNormalIsolado()
        {
            yield return EnterGame();
            var s = GameManager.I.normal;
            s.eventInterval = new Vector2(9999f, 9999f);
            s.snitchInterval = new Vector2(9999f, 9999f);
            s.nerdGlanceInterval = new Vector2(999f, 999f);
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        // ------------------------------------------------------------------ caminhada (polimento visual)


        [UnityTest]
        public IEnumerator ProfessorAndaSemBracoCruzadoESemPesNaLinhaDoMeio()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var skin = prof.visual.Skin;
            prof.WalkTo(prof.transform.position + prof.transform.forward * 4f, false);
            float piorBraco = 0f, piorPe = 1f;
            int quadros = 0;
            float fim = Time.time + 2f;
            while (Time.time < fim)
            {
                yield return null;
                if (skin.WalkWeight < 0.9f) continue;
                quadros++;
                piorBraco = Mathf.Max(piorBraco, prof.visual.armsCrossed);
                Vector3 r = prof.transform.right;
                piorPe = Mathf.Min(piorPe, -Vector3.Dot(skin.LeftFoot.position - skin.Hips.position, r),
                                   Vector3.Dot(skin.RightFoot.position - skin.Hips.position, r));
            }
            Assert.Greater(quadros, 10, "a caminhada não chegou a entrar");
            Assert.Less(piorBraco, 0.05f, "andando, ele não pode estar de braços cruzados (mão na barriga)");
            Assert.Greater(piorPe, 0.04f, "cada pé fica do seu lado do quadril (sem andar de passarela): " + piorPe);
            Assert.Less(skin.RunWeight, 0.05f, "andando devagar não toca a corrida");
        }

        /// <summary>
        /// Causa raiz do "andar torto": os clipes Walk/Run giram a pelve ~27° (constante). Andando, a pelve final tem que
        /// ficar alinhada com o corpo, e os ombros não podem ter sido arrastados junto.
        /// </summary>
        [UnityTest]
        public IEnumerator AndandoAPelveFicaAlinhadaComOCorpo()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var skin = prof.visual.Skin;
            var an = skin.Model.GetComponent<Animator>();
            prof.WalkTo(prof.transform.position + prof.transform.forward * 4f, false);
            float piorPelve = 0f, maiorCorrecao = 0f, piorOmbro = 0f;
            int quadros = 0;
            float fim = Time.time + 2f;
            while (Time.time < fim)
            {
                yield return null; // aqui os ossos ainda têm o resultado final do quadro anterior (animação + correção)
                if (skin.WalkWeight < 0.9f) continue;
                quadros++;
                var t = prof.transform;
                float pelve = CharacterModelSkin.PelvisYaw(an.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,
                                                           an.GetBoneTransform(HumanBodyBones.RightUpperLeg).position, t.right, t.up);
                float ombro = CharacterModelSkin.PelvisYaw(an.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,
                                                           an.GetBoneTransform(HumanBodyBones.RightUpperArm).position, t.right, t.up);
                piorPelve = Mathf.Max(piorPelve, Mathf.Abs(pelve));
                piorOmbro = Mathf.Max(piorOmbro, Mathf.Abs(ombro));
                maiorCorrecao = Mathf.Max(maiorCorrecao, Mathf.Abs(skin.PelvisCorrection));
            }
            Assert.Greater(quadros, 10, "a caminhada não chegou a entrar");
            Assert.Greater(maiorCorrecao, 15f, "o clipe gira a pelve (~27°): a correção tinha que estar agindo");
            Assert.Less(piorPelve, 3f, "a pelve ficou girada em relação ao corpo: " + piorPelve + "°");
            Assert.Less(piorOmbro, 15f, "os ombros foram arrastados pela correção: " + piorOmbro + "°");
        }

        [UnityTest]
        public IEnumerator AndandoOsPesApontamParaAFrente()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var skin = prof.visual.Skin;
            prof.WalkTo(prof.transform.position + prof.transform.forward * 4f, false);
            float pior = 0f, piorParado = 0f;
            int quadros = 0;
            float fim = Time.time + 2f;
            while (Time.time < fim)
            {
                yield return null; // ossos com o resultado final do quadro anterior
                for (int s = 0; s < 2; s++)
                {
                    if (skin.FootFlatness(s) < 0.4f) continue; // pé quase na vertical: a direção vista de cima não vale
                    float erro = Mathf.Abs(CharacterModelSkin.FootCorrection(skin.FootYaw(s), s));
                    if (skin.WalkWeight > 0.9f) pior = Mathf.Max(pior, erro);
                    else if (skin.WalkWeight < 0.05f) piorParado = Mathf.Max(piorParado, erro);
                }
                if (skin.WalkWeight > 0.9f) quadros++;
            }
            Assert.Greater(quadros, 10, "a caminhada não chegou a entrar");
            Assert.Less(pior, 4f, "andando, a ponta do pé ficou virada de lado: " + pior + "°");
            Assert.Less(piorParado, 4f, "parado, a ponta do pé ficou virada de lado: " + piorParado + "°");
        }

        [UnityTest]
        public IEnumerator ProfessorParadoContinuaCruzandoOsBracos()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            yield return Wait(2.5f);
            if (prof.State == ProfState.Tranquilo && UnityEngine.AI.NavMesh.SamplePosition(prof.transform.position, out _, 1f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // parado e tranquilo por mais de 0,6 s: os braços cruzam como antes
                prof.WalkTo(prof.transform.position, false);
                yield return Wait(1.8f);
                if (prof.visual.Skin.WalkWeight < 0.1f)
                    Assert.Greater(prof.visual.armsCrossed, 0.5f, "parado e tranquilo, ele cruza os braços");
            }
        }

        // ------------------------------------------------------------------ auditoria das caixas de texto

        static float Altura(UnityEngine.UI.Text t, string s)
        {
            t.text = s;
            return t.preferredHeight;
        }

        /// <summary>Cada pergunta e cada alternativa do jogo, no painel real da prova, cabe na sua caixa.</summary>
        [UnityTest]
        public IEnumerator TodasAsPerguntasEAlternativasCabemNoPainelDaProva()
        {
            yield return ComecaNormalIsolado();
            var quiz = QuizChallenge.I;
            quiz.Begin(30f, _ => { });
            yield return null;
            var painel = GameObject.Find("Desafio B").transform;
            var pergunta = painel.Find("Pergunta").GetComponent<UnityEngine.UI.Text>();
            var problemas = new System.Collections.Generic.List<string>();
            foreach (var q in QuizBank.AllForAudit)
            {
                float h = Altura(pergunta, q.text);
                if (h > pergunta.rectTransform.rect.height + 1f) problemas.Add("pergunta (" + h + " px): " + q.text);
                for (int i = 0; i < 4; i++)
                {
                    var opt = painel.Find("Opcao" + (i + 1) + "/Texto").GetComponent<UnityEngine.UI.Text>();
                    foreach (var o in q.options)
                    {
                        float ho = Altura(opt, "D)  " + o);
                        if (ho > opt.rectTransform.rect.height + 1f) problemas.Add("alternativa (" + ho + " px): " + o);
                    }
                }
            }
            Assert.IsEmpty(problemas, string.Join("\n", problemas.Distinct()));
        }

        /// <summary>Balão de fala: nome e fala sempre dentro do balão, com fala curta ou longa.</summary>
        [UnityTest]
        public IEnumerator BalaoDeFalaSempreContemNomeEFala()
        {
            yield return ComecaNormalIsolado();
            var hud = HUD.I;
            var alvo = ProfessorAI.I.transform;
            var falas = new System.Collections.Generic.List<string> { "Hm.", "Ok", "Silêncio!" };
            foreach (var f in typeof(Falas).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (f.FieldType == typeof(string[])) falas.AddRange((string[])f.GetValue(null));
            var problemas = new System.Collections.Generic.List<string>();
            var baloes = GameObject.Find("Baloes").transform;
            foreach (var fala in falas)
            {
                hud.Say(alvo, "PROF. TAKANERVOSO", fala, 5f, HUD.Red, 2);
                Transform b = null;
                foreach (Transform c in baloes) if (c.gameObject.activeSelf) b = c;
                var caixa = (RectTransform)b;
                var nome = b.Find("Nome").GetComponent<UnityEngine.UI.Text>();
                var texto = b.Find("Fala").GetComponent<UnityEngine.UI.Text>();
                float w = caixa.rect.width;
                if (nome.preferredWidth + 12f > w + 1f) problemas.Add("nome sai do balão com a fala: " + fala);
                float topo = -texto.rectTransform.anchoredPosition.y; // a fala começa abaixo do nome
                if (topo + texto.preferredHeight > caixa.rect.height + 1f) problemas.Add("fala passa do fundo do balão: " + fala);
                if (texto.rectTransform.anchoredPosition.x + texto.rectTransform.rect.width > w + 1f) problemas.Add("fala passa da largura: " + fala);
            }
            Assert.IsEmpty(problemas, string.Join("\n", problemas.Distinct()));
        }

        /// <summary>Texto do tutorial (todos os passos + "[T] pular tutorial"), prompt longo e ajuda longa cabem nas caixas.</summary>
        [UnityTest]
        public IEnumerator TutorialPromptEAjudaCabemNasCaixas()
        {
            yield return ComecaNormalIsolado();
            var hud = HUD.I;
            var problemas = new System.Collections.Generic.List<string>();
            var hudRoot = GameObject.Find("HUD").transform;

            foreach (var passo in TutorialSteps.Order)
            {
                if (passo == TutorialStep.Concluido) continue;
                hud.SetTutorial(TutorialSteps.Text(passo) + "    <color=#3C372F>[T] pular tutorial</color>");
                var t = hudRoot.Find("Tutorial/Texto").GetComponent<UnityEngine.UI.Text>();
                if (t.preferredHeight > t.rectTransform.rect.height + 1f) problemas.Add("tutorial " + passo + ": " + t.preferredHeight + " > " + t.rectTransform.rect.height);
            }
            hud.SetTutorial(null);

            string longo = "[E] Segure para pedir ao colega da frente que finja uma crise de tosse bem alta enquanto você confere a resposta da questão sete";
            hud.SetPrompt(longo);
            var p = hudRoot.Find("Prompt/Texto").GetComponent<UnityEngine.UI.Text>();
            if (p.preferredHeight > p.rectTransform.rect.height + 1f) problemas.Add("prompt longo: " + p.preferredHeight + " > " + p.rectTransform.rect.height);
            hud.SetPrompt(null);

            hud.SetSeatedHelp("[1] Responder (sabe 3)    [2] Celular (segure; bateria p/ 3)    [Espaço] Levantar    [Q] Colinha    [G] Garrafinha    [B] Bilhete    [F] Fone");
            var a = hudRoot.Find("AjudaFundo/Ajuda").GetComponent<UnityEngine.UI.Text>();
            if (a.preferredWidth > a.rectTransform.rect.width + 1f) problemas.Add("ajuda longa: " + a.preferredWidth + " > " + a.rectTransform.rect.width);
            yield return null;
            Assert.IsEmpty(problemas, string.Join("\n", problemas));
        }

        // ------------------------------------------------------------------ lousa

        [UnityTest]
        public IEnumerator LousaTemAPiadaDoProfessorENaoOTextoAntigo()
        {
            yield return ComecaNormalIsolado();
            yield return null;
            string lousa = Cenografia.LousaTexto();
            StringAssert.Contains(Falas.LousaPergunta, lousa);
            StringAssert.Contains(Falas.LousaResposta, lousa);
            StringAssert.DoesNotContain("boleto", lousa, "o texto antigo da prova saiu da lousa");
            var q = GameObject.Find("Questao no Quadro");
            Assert.IsNotNull(q.transform.Find("Cinto"), "o 8 está de cinto");
            foreach (var t in q.GetComponentsInChildren<UnityEngine.UI.Text>())
                if (t.enabled && t.text.Length > 1)
                    Assert.LessOrEqual(t.preferredWidth, t.rectTransform.rect.width + 2f, "texto da lousa quebrou/estourou: " + t.text);
        }

        // ------------------------------------------------------------------ olho desenhado do Nerd

        [UnityTest]
        public IEnumerator NerdOlhandoDeLadoMostraOOlhoDesenhadoQueOlhaParaOJogador()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            var icon = nerd.GetComponent<MoodIcon>();
            nerd.GlanceSoon();
            yield return WaitUntil(() => nerd.Glancing, 4f, "o Nerd não chegou a olhar de lado");
            yield return Wait(0.5f);
            Assert.AreEqual(MoodIcon.EyeKey, icon.Symbol, "a lógica continua mandando o mesmo sinal");
            Assert.IsTrue(icon.EyeVisible, "o olho desenhado tinha que aparecer");
            var texto = icon.GetComponentInChildren<Canvas>().transform.Find("Simbolo").GetComponent<UnityEngine.UI.Text>();
            Assert.AreEqual("", texto.text, "nada de texto 'O_O' por cima do olho");
            var cam = Camera.main.transform;
            float lado = Vector3.Dot(PlayerController.I.transform.position - nerd.transform.position, cam.right);
            if (Mathf.Abs(lado) > 0.3f)
                Assert.AreEqual(Mathf.Sign(lado), Mathf.Sign(icon.EyeLook), "a pupila olha para o lado do jogador na tela");

            yield return WaitUntil(() => !nerd.Glancing, 8f, "o Nerd não voltou a escrever");
            yield return Wait(0.6f);
            Assert.IsFalse(icon.EyeVisible, "o olho sai depois que ele volta a escrever");
        }

        // ------------------------------------------------------------------ Cara de Paisagem

        [UnityTest]
        public IEnumerator ProvaQueAcabaNoMeioDaCaraDePaisagemFechaOPainel()
        {
            yield return ComecaNormalIsolado();
            var fp = FacePokerChallenge.I;
            bool avisou = false;
            fp.Begin(0.5f, _ => avisou = true);
            yield return null;
            Assert.IsTrue(fp.Active, "pré-condição");
            GameManager.I.DeliverExam();
            yield return null;
            yield return null;
            Assert.IsFalse(fp.Active, "o desafio tinha que fechar com a prova encerrada");
            Assert.IsNull(GameObject.Find("Desafio A"), "o painel não pode ficar na tela de resultado");
            Assert.IsFalse(avisou, "fechar sem resultado não conta como acerto nem erro");
        }

        [UnityTest]
        public IEnumerator PainelDaCaraDePaisagemNoPapelComTextosQueCabem()
        {
            yield return ComecaNormalIsolado();
            var fp = FacePokerChallenge.I;
            fp.Begin(1f, _ => { });
            yield return null;
            var painel = GameObject.Find("Desafio A");
            Assert.IsNotNull(painel);
            var titulo = painel.transform.Find("Titulo").GetComponent<UnityEngine.UI.Text>();
            Assert.AreEqual(UITheme.StampRed, titulo.color, "título carimbado em vermelho, como a HUD de papel");
            foreach (var nome in new[] { "Titulo", "Ajuda", "Status", "Tempo", "Feedback" })
            {
                var t = painel.transform.Find(nome).GetComponent<UnityEngine.UI.Text>();
                var r = t.rectTransform.rect;
                Assert.GreaterOrEqual(UITheme.Contrast(t.color, UITheme.Paper), 4.5f, nome + " ilegível no papel");
                Assert.LessOrEqual(t.preferredHeight, r.height + 2f, nome + " não cabe na altura");
            }
            // espera o status ser escrito e confere a largura dele (a linha mais longa do painel)
            yield return Wait(0.2f);
            var status = painel.transform.Find("Status").GetComponent<UnityEngine.UI.Text>();
            Assert.LessOrEqual(status.cachedTextGenerator.lineCount, 1, "status quebrou em duas linhas");
            fp.Cancel();
        }

        // ------------------------------------------------------------------ HUD

        [UnityTest]
        public IEnumerator MensagemCinzaNaoTiraOutraDaTela()
        {
            yield return ComecaNormalIsolado();
            var hud = HUD.I;
            hud.Toast("primeira", HUD.Yellow, 10f);
            hud.Toast("segunda", HUD.Red, 10f);
            hud.Toast("informativa", HUD.Gray, 10f);
            yield return null;
            var textos = hud.ToastTexts();
            CollectionAssert.Contains(textos, "primeira");
            CollectionAssert.Contains(textos, "segunda");
            CollectionAssert.DoesNotContain(textos, "informativa", "com a pilha cheia, a cinza fica de fora");
        }

        // ------------------------------------------------------------------ trilha

        [UnityTest]
        public IEnumerator TrilhaCalmaSemEstouroESemEstaloNoLoop()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            Assert.IsNotNull(m);
            for (int i = 0; i < 3; i++) Assert.LessOrEqual(m.LayerPeak(i), 1f, "camada " + i + " estoura");
            Assert.LessOrEqual(m.LayerPeak(0), 0.8f + 1e-4f, "a calma é normalizada com folga");
            Assert.Less(m.LoopSeam(0), 0.02f, "a calma dá a volta no loop sem degrau (estalo)");
            Assert.Less(m.LoopSeam(1), 0.02f, "a desconfiança dá a volta no loop sem degrau (estalo)");
            Assert.AreEqual(3, CountLayerSources(), "uma fonte por camada, sem duplicar (o objeto também guarda os efeitos)");
            Assert.AreEqual(1, Object.FindObjectsByType<MusicDirector>(FindObjectsInactive.Include).Length, "uma trilha só");
        }

        static int CountLayerSources()
        {
            int n = 0;
            foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
                if (s.clip != null && s.clip.name.StartsWith("musica_") && s.loop) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator HmDoProfessorAbaixaATrilhaEElaVolta()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            yield return Wait(2f); // o sinal de início também é som importante: deixa passar
            Assert.Greater(m.Duck, 0.95f, "pré-condição: trilha cheia");
            SoundFX.Play(Sfx.Ouviu);
            yield return Wait(0.6f);
            Assert.Less(m.Duck, 0.7f, "com o 'hm?' a trilha tinha que abaixar");
            yield return Wait(2.5f);
            Assert.Greater(m.Duck, 0.95f, "depois a trilha volta");
        }

        [UnityTest]
        public IEnumerator FimDaProvaTiraATrilhaDevagar()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            yield return Wait(1.5f);
            Assert.Greater(m.Layer(0), 0.5f, "pré-condição");
            GameManager.I.DeliverExam();
            yield return Wait(0.3f);
            Assert.Greater(m.Layer(0), 0.2f, "sai devagar, não corta seco");
            yield return Wait(2.5f);
            Assert.Less(m.Layer(0), 0.02f, "na tela de resultado a trilha já saiu");
        }
    }
}
