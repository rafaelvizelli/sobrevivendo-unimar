using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProvaImpossivel.Tests
{
    /// <summary>Os personagens vestem o modelo 3D sem mudar nada do jogo (cabeça/olhar vêm do boneco invisível).</summary>
    public class ModelosPersonagensPlayTests
    {
        static IEnumerator Sala()
        {
            yield return FluxoEProfessorTests.EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            for (int i = 0; i < 20; i++) yield return null;
        }

        /// <summary>Segura o objeto por um tempo (não por quadros: no modo batch os quadros podem ser bem curtos e a pose não assenta).</summary>
        static IEnumerator Segura(CharacterVisual v, HandProp prop, float segundos = 1f)
        {
            float fim = Time.time + segundos;
            while (Time.time < fim) { v.prop = prop; yield return null; }
            v.prop = prop; yield return null;
        }

        static CharacterVisual[] Todos =>Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None);

        [UnityTest]
        public IEnumerator TodoPersonagemVesteOModeloEOBonecoSome()
        {
            yield return Sala();
            Assert.Greater(Todos.Length, 5);
            foreach (var v in Todos)
            {
                Assert.IsNotNull(v.Skin, v.name + " sem modelo 3D");
                Assert.IsNotNull(v.Skin.Model, v.name);
                var visual = v.transform.Find("Visual");
                foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                    if (v.SweatRoot == null || !r.transform.IsChildOf(v.SweatRoot))
                        Assert.IsFalse(r.enabled, v.name + ": parte do boneco antigo ainda visível (" + r.name + ")");
                var modelRenderers = v.Skin.Model.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(modelRenderers.Length, 0, v.name);
                Assert.IsTrue(modelRenderers.All(r => r.enabled), v.name + ": modelo escondido");
                Assert.AreEqual(0, v.Skin.Model.GetComponentsInChildren<Collider>(true).Length, v.name + ": o modelo não pode ter colisor (a visão usa os do personagem)");
            }
        }

        [UnityTest]
        public IEnumerator CadaPapelPrincipalTemModeloDiferente()
        {
            yield return Sala();
            var principais = Todos.Where(v => v.look != CharacterLook.Figurante).ToList();
            Assert.GreaterOrEqual(principais.Count, 6);
            var malhas = principais.Select(v => v.Skin.Model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.sharedMesh).ToArray()).ToList();
            for (int i = 0; i < malhas.Count; i++)
                for (int j = i + 1; j < malhas.Count; j++)
                    Assert.IsFalse(malhas[i].SequenceEqual(malhas[j]), principais[i].look + " e " + principais[j].look + " com a mesma malha");
        }

        [UnityTest]
        public IEnumerator CabecaoEEnfeitesDeComedia()
        {
            yield return Sala();
            var prof = ProfessorAI.I.visual.Skin;
            Assert.Greater(prof.HeadBone.lossyScale.y, prof.Hips.lossyScale.y * 1.1f, "o professor devia ter cabeção");
            Assert.IsNotNull(prof.HeadBone.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bigode"), "professor sem bigode");
            var nerd = Todos.First(v => v.look == CharacterLook.Nerd).Skin;
            Assert.IsNotNull(nerd.HeadBone.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "OculosAro"), "nerd sem óculos");
            foreach (var v in Todos)
                Assert.AreEqual(0, v.Skin.Model.GetComponentsInChildren<Collider>(true).Length, v.name + ": enfeite com colisor");
        }

        [UnityTest]
        public IEnumerator SentadoFicaNaAlturaDoAssentoEDePeNoChao()
        {
            yield return Sala();
            var set = CharacterModelSet.Active;
            Assert.IsNotNull(set);
            for (int i = 0; i < 30; i++) yield return null; // pesos das poses assentam
            foreach (var v in Todos)
            {
                float hip = v.Skin.Hips.position.y - v.transform.position.y;
                if (v.seated)
                    Assert.That(hip, Is.InRange(set.seatHipHeight - 0.08f, set.seatHipHeight + 0.08f), v.name + " sentado com o quadril em " + hip);
                else
                    Assert.That(v.Skin.Model.localPosition.y, Is.InRange(-0.01f, 0.01f), v.name + " de pé flutuando");
            }
        }

        [UnityTest]
        public IEnumerator CabecaDoModeloSegueOOlharDoJogo()
        {
            yield return Sala();
            // a IA do professor gira a cabeça sozinha (varredura); em vários momentos, o rosto do modelo aponta
            // para o mesmo lado que o olhar que a visão do jogo usa
            var prof = ProfessorAI.I.visual;
            float pior = 0f;
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                Vector3 f = prof.Skin.HeadFacing; f.y = 0f;
                pior = Mathf.Max(pior, Vector3.Angle(prof.HeadForward, f.normalized));
            }
            Assert.That(pior, Is.LessThan(20f), "rosto do modelo e olhar do jogo divergem (até " + pior + "°)");
            // e um giro forçado aparece no modelo
            prof.headYaw = 40f;
            for (int i = 0; i < 20; i++) yield return null;
            Vector3 g = prof.Skin.HeadFacing; g.y = 0f;
            Assert.That(Vector3.Angle(prof.HeadForward, g.normalized), Is.LessThan(20f));
        }

        [UnityTest]
        public IEnumerator MaoLevantadaSobeAcimaDaCabeca()
        {
            yield return Sala();
            var nerd = Todos.First(v => v.look == CharacterLook.Nerd);
            var skin = nerd.Skin;
            for (int i = 0; i < 30; i++) { nerd.handRaise = 0f; yield return null; }
            Assert.Less(skin.RightHand.position.y, skin.HeadBone.position.y, "mão abaixada já está acima da cabeça");
            for (int i = 0; i < 30; i++) { nerd.handRaise = 1f; yield return null; }
            Assert.Greater(skin.RightHand.position.y, skin.HeadBone.position.y + 0.1f, "a mão não subiu acima da cabeça");
        }

        [UnityTest]
        public IEnumerator ObjetosDeColaAparecemNaMao()
        {
            yield return Sala();
            // um figurante (nenhum comportamento mexe no objeto da mão dele)
            var v = Todos.First(x => x.look == CharacterLook.Figurante && x.seated);
            var skin = v.Skin;
            foreach (var prop in new[] { HandProp.Celular, HandProp.Colinha, HandProp.Garrafinha, HandProp.Bilhete })
            {
                for (int i = 0; i < 40; i++) { v.prop = prop; yield return null; }
                Assert.AreEqual(prop, skin.PropKind, "objeto não apareceu: " + prop);
                var obj = skin.PropObject;
                Assert.IsTrue(obj.gameObject.activeInHierarchy);
                Assert.AreEqual(0, obj.GetComponentsInChildren<Collider>(true).Length, prop + " com colisor");
                Assert.IsTrue(obj.GetComponentsInChildren<Renderer>().All(r => r.enabled));
                float perto = Mathf.Min(Vector3.Distance(obj.position, skin.RightHand.position), Vector3.Distance(obj.position, skin.LeftHand.position));
                Assert.Less(perto, 0.25f, prop + " longe da mão (" + perto + " m)");
                // na frente do corpo e acima da carteira: dá para ver
                Assert.Greater(obj.position.y - v.transform.position.y, 0.75f, prop + " baixo demais (escondido pela carteira)");
            }
            for (int i = 0; i < 5; i++) { v.prop = HandProp.Nenhum; yield return null; }
            Assert.IsNull(skin.PropObject, "o objeto devia sumir ao soltar");
        }

        [UnityTest]
        public IEnumerator ObjetosNaoAtravessamAMao()
        {
            yield return Sala();
            // um figurante e o jogador (esqueletos e alturas diferentes)
            var quem = new[] { Todos.First(x => x.look == CharacterLook.Figurante && x.seated), Todos.First(x => x.look == CharacterLook.Jogador) };
            var pc = quem[1].GetComponent<PlayerController>();
            pc.enabled = false; // senão ele zera o objeto da mão todo quadro
            var pts = new List<(Vector3 p, float r)>();
            foreach (var v in quem)
                foreach (var prop in new[] { HandProp.Celular, HandProp.Colinha, HandProp.Garrafinha })
                {
                    yield return Segura(v, prop);
                    int lado = prop == HandProp.Colinha ? 0 : 1;
                    v.Skin.HandPoints(lado, pts);
                    for (int k = 0; k < pts.Count; k++)
                        Assert.IsFalse(HandProps.Touches(v.Skin.PropObject, pts[k].p, pts[k].r * 0.9f),
                                       v.look + ": " + prop + " atravessa a mão (ponto " + k + ")");
                    float gap = Vector3.Distance(v.Skin.PropObject.position, lado == 0 ? v.Skin.LeftHand.position : v.Skin.RightHand.position);
                    Assert.Less(gap, 0.2f, v.look + ": " + prop + " descolou da mão (" + gap + " m)");
                }
            pc.enabled = true;
        }

        [UnityTest]
        public IEnumerator FoneApareceNaOrelhaENaoNoCelular()
        {
            yield return Sala();
            var v = Todos.First(x => x.look == CharacterLook.Figurante && x.seated);
            yield return Segura(v, HandProp.Fone);
            var obj = v.Skin.PropObject;
            Assert.AreEqual(HandProp.Fone, v.Skin.PropKind);
            Assert.IsNull(obj.Find("Tela"), "o fone não pode ser um celular");
            Assert.AreEqual(0, obj.GetComponentsInChildren<Collider>(true).Length);
            float orelha = Vector3.Distance(obj.Find("Fone").position, v.Skin.HeadBone.position);
            Assert.Less(orelha, 0.35f, "o fone devia estar na cabeça (" + orelha + " m)");
            float mic = Vector3.Distance(obj.Find("Microfone").position, v.Skin.RightHand.position);
            Assert.Less(mic, 0.15f, "o microfone devia estar na mão (" + mic + " m)");
            Assert.Greater(obj.Find("Fio").localScale.y, 0.01f, "o fio devia ligar o fone à mão");
        }

        [UnityTest]
        public IEnumerator GarrafinhaVaiAteABoca()
        {
            yield return Sala();
            var v = Todos.First(x => x.look == CharacterLook.Figurante && x.seated);
            yield return Segura(v, HandProp.Garrafinha);
            // a tampa (não o meio da garrafa, que é comprida) é que vai na boca
            var tampa = v.Skin.PropObject.GetComponentsInChildren<Transform>().First(t => t.name == "Tampa");
            float d = Vector3.Distance(tampa.position, v.Skin.MouthPoint);
            // perto de verdade: com a cabeça girada depois de posicionar a garrafa, a boca subia ~8 cm e o queixo
            // entrava na tampa
            Assert.Less(d, 0.06f, "a tampa da garrafinha não chegou perto da boca (" + d + " m)");
            Assert.Less(tampa.position.y, v.Skin.PropObject.position.y, "bebendo, o fundo da garrafa fica para cima (tampa mais baixa)");
        }

        [UnityTest]
        public IEnumerator GarrafinhaSaiDePeEDeitaAosPoucos()
        {
            yield return Sala();
            var v = Todos.First(x => x.look == CharacterLook.Figurante && x.seated);
            for (int i = 0; i < 5; i++) { v.prop = HandProp.Nenhum; yield return null; }
            v.prop = HandProp.Garrafinha;
            yield return null; // primeiro quadro com a garrafa: a mão ainda está na carteira
            Assert.AreEqual(HandProp.Garrafinha, v.Skin.PropKind);
            float a = Vector3.Angle(v.Skin.PropObject.up, v.transform.up);
            Assert.Less(a, 30f, "a garrafa já apareceu deitada (" + a + "°), antes de a mão subir");
        }

        [UnityTest]
        public IEnumerator FoneNaoEnfiaAMaoNoRosto()
        {
            yield return Sala();
            var v = Todos.First(x => x.look == CharacterLook.Figurante && x.seated);
            yield return Segura(v, HandProp.Fone);
            var pts = new List<(Vector3 p, float r)>();
            v.Skin.HandPoints(1, pts);
            Vector3 rosto = v.Skin.HeadFacing.normalized;
            for (int k = 0; k < pts.Count; k++)
            {
                float frente = Vector3.Dot(pts[k].p - v.Skin.MouthPoint, rosto);
                Assert.Greater(frente, 0f, "a mão do fone entra no rosto (ponto " + k + " está " + (-frente) + " m atrás da boca)");
            }
        }

        [UnityTest]
        public IEnumerator ProfessorAndandoNaoCruzaAsPernas()
        {
            yield return Sala();
            var prof = ProfessorAI.I;
            var skin = prof.visual.Skin;
            prof.WalkTo(prof.transform.position + prof.transform.forward * 4f, false);
            float piorL = 1f, piorR = 1f;
            float fim = Time.time + 2f;
            while (Time.time < fim)
            {
                yield return null;
                if (skin.WalkWeight < 0.9f) continue;
                Vector3 r = prof.transform.right;
                piorL = Mathf.Min(piorL, -Vector3.Dot(skin.LeftFoot.position - skin.Hips.position, r));
                piorR = Mathf.Min(piorR, Vector3.Dot(skin.RightFoot.position - skin.Hips.position, r));
            }
            Assert.Less(piorL, 1f, "a caminhada não chegou a entrar");
            Assert.Greater(piorL, 0.02f, "o pé esquerdo cruzou para o lado direito (" + piorL + " m)");
            Assert.Greater(piorR, 0.02f, "o pé direito cruzou para o lado esquerdo (" + piorR + " m)");
        }

        [UnityTest]
        public IEnumerator ProfessorAndandoTocaACaminhada()
        {
            yield return Sala();
            var prof = ProfessorAI.I;
            Assert.IsTrue(prof.visual.Skin.Animated, "o professor (Humanoid) devia usar a animação do pacote");
            var legL = prof.visual.Skin.LeftUpperLeg;
            prof.WalkTo(prof.transform.position + prof.transform.forward * 3f, false);
            float min = 1f, max = -1f, peso = 0f;
            float fim = Time.time + 1.5f;
            while (Time.time < fim)
            {
                yield return null;
                var child = legL.GetChild(0);
                float d = Vector3.Dot((child.position - legL.position).normalized, prof.transform.forward);
                min = Mathf.Min(min, d); max = Mathf.Max(max, d);
                peso = Mathf.Max(peso, prof.visual.Skin.WalkWeight);
            }
            Assert.Greater(peso, 0.8f, "a animação de caminhada não entrou");
            Assert.Greater(max - min, 0.1f, "as pernas não balançaram");
        }
    }
}
