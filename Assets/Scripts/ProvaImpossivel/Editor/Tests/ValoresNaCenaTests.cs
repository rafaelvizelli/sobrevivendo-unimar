using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco S0 (Operação Steam): os números de cada dificuldade QUE O JOGO USA são os da cena (o GameManager gravado
    /// nela), não os do código. A cena foi salva antes do Bloco 15, então os campos novos podiam ter ficado com o padrão
    /// da classe. Este teste abre a cena (sem salvar) e compara cada dificuldade com a do código, campo a campo.
    /// </summary>
    public class ValoresNaCenaTests
    {
        const string CenaFase1 = "Assets/Scenes/Fase1_ProvaImpossivel.unity";

        [Test]
        public void DificuldadesDaCenaSaoAsDoCodigo()
        {
            var cena = EditorSceneManager.OpenScene(CenaFase1, OpenSceneMode.Additive);
            var refGo = new GameObject("GameManager (referência)");
            refGo.SetActive(false); // sem Awake
            try
            {
                GameManager naCena = null;
                foreach (var root in cena.GetRootGameObjects())
                {
                    naCena = root.GetComponentInChildren<GameManager>(true);
                    if (naCena != null) break;
                }
                Assert.IsNotNull(naCena, "a cena da Fase 1 não tem GameManager");
                var codigo = refGo.AddComponent<GameManager>();

                Compara(codigo.facil, naCena.facil);
                Compara(codigo.normal, naCena.normal);
                Compara(codigo.dificil, naCena.dificil);
                Compara(codigo.tutorial, naCena.tutorial);
            }
            finally
            {
                Object.DestroyImmediate(refGo);
                EditorSceneManager.CloseScene(cena, true); // fecha SEM salvar
            }
        }

        static void Compara(DifficultySettings codigo, DifficultySettings cena)
        {
            foreach (var f in typeof(DifficultySettings).GetFields())
            {
                object a = f.GetValue(codigo), b = f.GetValue(cena);
                Assert.AreEqual(a, b, codigo.name + "." + f.Name + ": a cena usa " + b + ", o código diz " + a);
            }
        }
    }
}
