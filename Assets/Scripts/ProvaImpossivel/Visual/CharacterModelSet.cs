using System;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Enfeites de comédia montados com primitivas em cima do modelo (cabeça/peito).</summary>
    [Flags]
    public enum Accessory
    {
        Nenhum = 0,
        Bigode = 1,
        Oculos = 2,          // armação grossa e redonda (nerd)
        OculosEscuros = 4,
        GravataBorboleta = 8,
        Corrente = 16,       // cordão de ouro
        Barriga = 32,        // barriguinha de professor
        MascaraDeDormir = 64,
        Bone = 128,          // boné virado para trás
        Topete = 256,
        Gravata = 512,       // gravata vermelha comprida
        Sobrancelhas = 1024, // S4: sobrancelhas grossas que ATUAM (mexem com a expressão): o rosto do professor
    }

    /// <summary>
    /// Escalação dos modelos 3D dos personagens (Quaternius Ultimate Modular Men/Women, CC0). Fica em Resources e é
    /// lido em tempo de jogo: cada <see cref="CharacterLook"/> ganha um modelo (os figurantes sorteiam pelo seed).
    /// Se o asset não existir ou <see cref="useModels"/> estiver desligado, o jogo usa os bonecos de primitivas.
    /// Gerado pelo menu "Prova Impossível > Personagens > Configurar modelos".
    /// </summary>
    [CreateAssetMenu(fileName = "ModelosPersonagens", menuName = "Prova Impossível/Modelos dos personagens")]
    public class CharacterModelSet : ScriptableObject
    {
        public const string ResourcePath = "Personagens/ModelosPersonagens";

        [Serializable]
        public struct Tint
        {
            [Tooltip("Nome do material no modelo (ex.: Purple, Suit)")] public string material;
            public Color color;
        }

        [Serializable]
        public struct Entry
        {
            public CharacterLook look;
            [Tooltip("Um ou mais modelos; com mais de um, o seed do personagem escolhe")]
            public GameObject[] models;
            [Tooltip("Escala do modelo (o original tem ~1,85 m)")]
            public float scale;
            [Tooltip("Cabeção de comédia (1 = normal)")]
            public float headScale;
            [Tooltip("Cores trocadas por nome de material")]
            public Tint[] tints;
            public Accessory accessories;
            [Tooltip("Figurantes: sorteia a cor da roupa (material em shirtMaterials) pelo seed")]
            public bool randomShirt;
        }

        [Serializable]
        public struct ShirtMaterial
        {
            public GameObject model;
            public string material;
        }

        [Tooltip("Desligado: volta para os bonecos de primitivas")]
        public bool useModels = true;
        public Entry[] entries = Array.Empty<Entry>();
        [Tooltip("Qual material é a roupa de cima de cada modelo (para variar a cor dos figurantes)")]
        public ShirtMaterial[] shirtMaterials = Array.Empty<ShirtMaterial>();
        [Tooltip("Cores sorteadas para a roupa dos figurantes")]
        public Color[] shirtColors = Array.Empty<Color>();

        [Header("Animações (rig Humanoid; os modelos Generic usam só as poses por código)")]
        public AnimationClip idle;
        public AnimationClip walk;
        [Tooltip("Corrida (opcional; sem ela, a corrida usa a caminhada acelerada)")] public AnimationClip run;

        [Header("Ajustes")]
        [Tooltip("Altura do quadril (m) quando sentado na cadeira")]
        public float seatHipHeight = 0.62f;
        [Tooltip("Quanto o modelo recua (m) da carteira quando sentado")]
        public float seatBack = 0.05f;
        [Tooltip("Fração da inclinação da cabeça do boneco usada no modelo")]
        [Range(0f, 1f)] public float headPitchFactor = 0.8f;

        static CharacterModelSet cached;
        static bool loaded;

        // Domain Reload está desligado: zera o cache a cada Play (padrão do projeto).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            cached = null;
            loaded = false;
        }

        /// <summary>A escalação ativa, ou null (sem asset ou desligada).</summary>
        public static CharacterModelSet Active
        {
            get
            {
                if (!loaded)
                {
                    cached = Resources.Load<CharacterModelSet>(ResourcePath);
                    loaded = true;
                }
                return cached != null && cached.useModels ? cached : null;
            }
        }

        /// <summary>Entrada do visual pedido e o modelo sorteado pelo seed (prefab null se não houver).</summary>
        public GameObject Pick(CharacterLook look, int seed, out Entry entry)
        {
            foreach (var e in entries)
            {
                if (e.look != look || e.models == null || e.models.Length == 0) continue;
                entry = e;
                return e.models[Index(seed, e.models.Length)];
            }
            entry = default;
            return null;
        }

        /// <summary>Material de roupa do modelo (para os figurantes variarem de cor), ou null.</summary>
        public string ShirtOf(GameObject model)
        {
            foreach (var s in shirtMaterials) if (s.model == model) return s.material;
            return null;
        }

        /// <summary>Cor de roupa sorteada pelo seed (estável: o mesmo aluno sempre com a mesma cor).</summary>
        public Color ShirtColor(int seed) =>
            shirtColors.Length == 0 ? Color.gray : shirtColors[Index(seed * 7 + 3, shirtColors.Length)];

        public static int Index(int seed, int count) => ((seed % count) + count) % count;
    }
}
