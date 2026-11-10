using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Os números das dificuldades ficam gravados na CENA (Inspector do GameManager), e é o valor da cena que vale.
    /// A simulação de equilíbrio (EditMode) lê os valores do CÓDIGO. Este teste garante que os dois são iguais,
    /// senão a simulação estaria medindo um jogo que não é o que roda.
    /// </summary>
    public class EquilibrioNaCenaTests
    {
        [UnityTest]
        public IEnumerator ValoresDaCenaIguaisAosDoCodigo()
        {
            yield return EnterGame();
            var cena = GameManager.I;

            // inativo: o Awake não roda, então não vira o GameManager.I; só queremos os valores padrão do código
            var go = new GameObject("GameManager só com os valores do código");
            go.SetActive(false);
            var codigo = go.AddComponent<GameManager>();
            try
            {
                Assert.AreEqual(JsonUtility.ToJson(codigo.facil, true), JsonUtility.ToJson(cena.facil, true), "Fácil: cena diferente do código");
                Assert.AreEqual(JsonUtility.ToJson(codigo.normal, true), JsonUtility.ToJson(cena.normal, true), "Normal: cena diferente do código");
                Assert.AreEqual(JsonUtility.ToJson(codigo.dificil, true), JsonUtility.ToJson(cena.dificil, true), "Difícil: cena diferente do código");
                Assert.AreEqual(JsonUtility.ToJson(codigo.tutorial, true), JsonUtility.ToJson(cena.tutorial, true), "Treino: cena diferente do código");
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
