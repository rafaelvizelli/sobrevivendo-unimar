using NUnit.Framework;
using ProvaImpossivel.EditorTools;
using UnityEditor;

namespace ProvaImpossivel.Tests
{
    /// <summary>S4 (Operação Steam): o professor como estrela, a parte que dá para testar sem cena.</summary>
    public class ProfessorEstrelaTests
    {
        [Test]
        public void SobrancelhasTemUmaPoseLegivelPorExpressao()
        {
            var bravo = ExpressiveBrows.PoseFor(Expression.Bravo);
            Assert.Greater(bravo.leftTilt, 0f, "bravo: ponta de dentro para baixo");
            Assert.Greater(bravo.rightTilt, 0f);
            Assert.Less(bravo.leftUp, 0f, "bravo: sobrancelhas descem");

            var aflito = ExpressiveBrows.PoseFor(Expression.Assustado);
            Assert.Greater(aflito.leftUp, 0f, "assustado: sobem");
            Assert.Less(aflito.leftTilt, 0f, "assustado: ponta de dentro para cima");

            var desconfiado = ExpressiveBrows.PoseFor(Expression.Desconfiado);
            Assert.Greater(desconfiado.rightUp - desconfiado.leftUp, 0.02f, "desconfiado: uma sobe e a outra não (assimétrico)");

            var neutro = ExpressiveBrows.PoseFor(Expression.Neutro);
            Assert.AreEqual(0f, neutro.leftUp);
            Assert.AreEqual(0f, neutro.rightTilt);
        }

        [Test]
        public void ProfessorEOMaiorDaSalaETemSobrancelhas()
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterModelSet>(CharacterModelSetup.AssetPath);
            Assert.IsNotNull(set);
            CharacterModelSet.Entry? prof = null;
            float maiorAluno = 0f;
            foreach (var e in set.entries)
            {
                if (e.look == CharacterLook.Professor) prof = e;
                else if (e.scale > maiorAluno) maiorAluno = e.scale;
            }
            Assert.IsTrue(prof.HasValue, "sem professor na escalação");
            var p = prof.Value;
            Assert.GreaterOrEqual(p.scale, maiorAluno * 1.08f, "o professor tem que ser visivelmente maior que qualquer aluno");
            Assert.IsTrue(p.accessories.HasFlag(Accessory.Sobrancelhas));
            // o asset gravado e a escalação do código (menu "Configurar modelos") dizem a mesma coisa
            foreach (var r in CharacterModelSetup.Cast)
                if (r.look == CharacterLook.Professor)
                {
                    Assert.AreEqual(r.scale, p.scale, 1e-4f);
                    Assert.AreEqual(r.acc, p.accessories);
                }
        }
    }
}
