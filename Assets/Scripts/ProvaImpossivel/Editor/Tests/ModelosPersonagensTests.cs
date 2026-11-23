using System;
using System.Linq;
using NUnit.Framework;
using ProvaImpossivel.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Escalação dos modelos 3D (Quaternius), importação dos FBX e objeto na mão.</summary>
    public class ModelosPersonagensTests
    {
        static CharacterModelSet Set => AssetDatabase.LoadAssetAtPath<CharacterModelSet>(CharacterModelSetup.AssetPath);

        static readonly string[] OssosDasPoses =
        {
            "Hips", "Abdomen", "Torso", "Neck", "Head", "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R", "LowerArm.R", "Hand.R",
            "UpperLeg.L", "LowerLeg.L", "Foot.L", "UpperLeg.R", "LowerLeg.R", "Foot.R",
        };

        [Test]
        public void EscalacaoExisteEmResourcesEEstaLigada()
        {
            Assert.IsNotNull(Set, "falta " + CharacterModelSetup.AssetPath + " (menu Prova Impossível > Personagens > Configurar modelos)");
            Assert.IsTrue(Set.useModels);
            Assert.AreSame(Set, Resources.Load<CharacterModelSet>(CharacterModelSet.ResourcePath), "precisa estar no caminho que o jogo carrega");
        }

        [Test]
        public void TodoPapelTemModeloEEscalaRazoavel()
        {
            foreach (CharacterLook look in Enum.GetValues(typeof(CharacterLook)))
            {
                var e = Set.entries.FirstOrDefault(x => x.look == look);
                Assert.IsNotNull(e.models, "sem modelo para " + look);
                Assert.Greater(e.models.Length, 0, "sem modelo para " + look);
                Assert.IsTrue(e.models.All(m => m != null), "modelo nulo em " + look);
                Assert.That(e.scale, Is.InRange(0.85f, 1.2f), "escala fora do esperado em " + look);
                Assert.That(e.headScale, Is.InRange(1f, 1.6f), "cabeção exagerado demais em " + look);
            }
        }

        [Test]
        public void FigurantesVariamESorteioEhEstavel()
        {
            var e = Set.entries.First(x => x.look == CharacterLook.Figurante);
            Assert.GreaterOrEqual(e.models.Length, 5, "poucos figurantes diferentes");
            Assert.IsTrue(e.randomShirt);
            var a = Set.Pick(CharacterLook.Figurante, 3, out _);
            var b = Set.Pick(CharacterLook.Figurante, 3, out _);
            Assert.AreSame(a, b, "o mesmo seed tem que dar o mesmo modelo");
            Assert.IsNotNull(Set.Pick(CharacterLook.Figurante, -7, out _), "seed negativo não pode quebrar");
            Assert.AreEqual(Set.ShirtColor(5), Set.ShirtColor(5));
            Assert.GreaterOrEqual(Set.shirtColors.Length, 5);
        }

        [Test]
        public void FiguranteTemMaterialDeRoupaQueExisteNoModelo()
        {
            foreach (var m in Set.entries.First(x => x.look == CharacterLook.Figurante).models)
            {
                string shirt = Set.ShirtOf(m);
                Assert.IsNotNull(shirt, m.name + " sem material de roupa definido");
                var nomes = m.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(x => x != null).Select(x => x.name);
                CollectionAssert.Contains(nomes.ToList(), shirt, m.name + " não tem o material " + shirt);
            }
        }

        [Test]
        public void CoresTrocadasApontamParaMateriaisQueExistem()
        {
            foreach (var e in Set.entries)
                foreach (var t in e.tints)
                    foreach (var m in e.models)
                    {
                        var nomes = m.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(x => x != null).Select(x => x.name).ToList();
                        CollectionAssert.Contains(nomes, t.material, e.look + ": " + m.name + " não tem o material " + t.material);
                    }
        }

        [Test]
        public void PapeisPrincipaisNaoRepetemModelo()
        {
            var principais = Set.entries.Where(x => x.look != CharacterLook.Figurante).Select(x => x.models[0]).ToList();
            Assert.AreEqual(principais.Count, principais.Distinct().Count(), "dois papéis com o mesmo modelo");
            var figs = Set.entries.First(x => x.look == CharacterLook.Figurante).models;
            Assert.IsFalse(figs.Any(principais.Contains), "figurante com cara de personagem principal");
        }

        [Test]
        public void PrincipaisTemEnfeitesDeComedia()
        {
            Accessory Of(CharacterLook l) => Set.entries.First(x => x.look == l).accessories;
            Assert.IsTrue(Of(CharacterLook.Professor).HasFlag(Accessory.Bigode));
            Assert.IsTrue(Of(CharacterLook.Nerd).HasFlag(Accessory.Oculos));
            Assert.IsTrue(Of(CharacterLook.Fundao).HasFlag(Accessory.OculosEscuros));
            Assert.IsTrue(Of(CharacterLook.Dorminhoco).HasFlag(Accessory.MascaraDeDormir));
        }

        [Test]
        public void AnimacoesBaseExistemEFazemLoop()
        {
            Assert.IsNotNull(Set.idle); Assert.IsNotNull(Set.walk);
            Assert.AreEqual("Idle", Set.idle.name);
            Assert.AreEqual("Walk", Set.walk.name);
            Assert.IsTrue(Set.idle.isLooping && Set.walk.isLooping, "Idle/Walk precisam repetir");
            Assert.IsTrue(Set.idle.isHumanMotion, "Idle precisa ser Humanoid para servir em todos os homens");
        }

        [Test]
        public void FbxTemOsOssosDasPosesEHomensSaoHumanoid()
        {
            foreach (var e in Set.entries)
                foreach (var m in e.models)
                {
                    var path = AssetDatabase.GetAssetPath(m);
                    foreach (var bone in OssosDasPoses)
                        Assert.IsNotNull(Find(m.transform, bone), path + " sem o osso " + bone);
                    var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                    if (m.name.StartsWith("Homem_"))
                    {
                        Assert.AreEqual(ModelImporterAnimationType.Human, mi.animationType, path);
                        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First();
                        Assert.IsTrue(avatar.isValid && avatar.isHuman, path + ": avatar humanoide inválido");
                        Assert.AreEqual("Hips", m.GetComponent<Animator>().avatar.humanDescription.human.First(h => h.humanName == "Hips").boneName,
                                        path + ": quadril mapeado errado");
                    }
                    else Assert.AreEqual(ModelImporterAnimationType.Generic, mi.animationType, path + " (pernas presas na raiz: fica Generic)");
                }
        }

        [Test]
        public void AjustesVisuaisDentroDoRazoavel()
        {
            Assert.That(Set.seatHipHeight, Is.InRange(0.4f, 0.8f));
            Assert.That(Set.seatBack, Is.InRange(0f, 0.3f));
            Assert.That(Set.headPitchFactor, Is.InRange(0.3f, 1f));
        }

        [Test]
        public void ObjetoNaMaoSegueOQueOJogadorEstaFazendo()
        {
            Assert.AreEqual(HandProp.Nenhum, CheatRules.PropInHand(false, PlayerCheats.Hold.Nenhum, false));
            Assert.AreEqual(HandProp.Celular, CheatRules.PropInHand(true, PlayerCheats.Hold.Nenhum, false), "pesquisando (tecla 2)");
            Assert.AreEqual(HandProp.Fone, CheatRules.PropInHand(false, PlayerCheats.Hold.FoneEnviar, false), "fone mostra o fone, não o celular");
            Assert.AreEqual(HandProp.Fone, CheatRules.PropInHand(false, PlayerCheats.Hold.FoneLer, false));
            Assert.AreEqual(HandProp.Colinha, CheatRules.PropInHand(false, PlayerCheats.Hold.Colinha, false));
            Assert.AreEqual(HandProp.Garrafinha, CheatRules.PropInHand(false, PlayerCheats.Hold.Garrafinha, false));
            Assert.AreEqual(HandProp.Bilhete, CheatRules.PropInHand(false, PlayerCheats.Hold.Nenhum, true));
            Assert.AreEqual(HandProp.Bilhete, CheatRules.PropInHand(true, PlayerCheats.Hold.Colinha, true), "o arremesso tem prioridade (é rápido)");
        }

        static Transform Find(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = Find(c, n); if (r != null) return r; }
            return null;
        }
    }
}
