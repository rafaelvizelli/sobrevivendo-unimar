using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Monta (ou atualiza) a escalação dos modelos 3D em Resources/Personagens/ModelosPersonagens.asset.
    /// Não mexe na cena: os personagens vestem o modelo sozinhos ao dar Play.
    /// </summary>
    public static class CharacterModelSetup
    {
        public const string AssetPath = "Assets/Resources/Personagens/ModelosPersonagens.asset";

        static Color C(float r, float g, float b) => new(r, g, b);
        static CharacterModelSet.Tint T(string material, Color c) => new() { material = material, color = c };

        public struct Role
        {
            public CharacterLook look;
            public string[] models;
            public float scale, headScale;
            public CharacterModelSet.Tint[] tints;
            public Accessory acc;
            public bool randomShirt;
        }

        /// <summary>Quem é quem (modelo Quaternius → papel). Cabeção em todo mundo: é comédia.</summary>
        public static readonly Role[] Cast =
        {
            // moletom laranja ("o de laranja tá colando")
            new() { look = CharacterLook.Jogador, models = new[] { "Homem_Casual" }, scale = 1f, headScale = 1.3f,
                    tints = new[] { T("Purple", C(0.95f, 0.45f, 0.12f)) } },
            // terno, gravata vermelha, cabelo grisalho, bigodão e barriga saltando do paletó
            new() { look = CharacterLook.Professor, models = new[] { "Homem_Suit" }, scale = 1.15f, headScale = 1.3f, // S4: o maior da sala
                    tints = new[] { T("Tie", C(0.8f, 0.1f, 0.12f)), T("Hair", C(0.7f, 0.7f, 0.72f)), T("Suit", C(0.36f, 0.27f, 0.18f)) },
                    acc = Accessory.Bigode | Accessory.Barriga | Accessory.Sobrancelhas },
            // camisa azul-clara, calça cáqui, óculos fundo de garrafa, gravata-borboleta e o maior cérebro da sala
            new() { look = CharacterLook.Nerd, models = new[] { "Homem_Casual2" }, scale = 0.97f, headScale = 1.45f,
                    tints = new[] { T("LightBrown", C(0.62f, 0.8f, 0.95f)), T("LightBlue", C(0.72f, 0.63f, 0.45f)) },
                    acc = Accessory.Oculos | Accessory.GravataBorboleta },
            // de camisa de praia e máscara de dormir na testa
            new() { look = CharacterLook.Dorminhoco, models = new[] { "Homem_Beach" }, scale = 1f, headScale = 1.3f,
                    acc = Accessory.MascaraDeDormir },
            // moicano, óculos escuros e cordão de ouro: "confia no pai"
            new() { look = CharacterLook.Fundao, models = new[] { "Homem_Punk" }, scale = 1.04f, headScale = 1.3f,
                    acc = Accessory.OculosEscuros | Accessory.Corrente },
            // tailleur e gravatinha vermelha
            new() { look = CharacterLook.PuxaSaco, models = new[] { "Mulher_Suit" }, scale = 1f, headScale = 1.35f,
                    acc = Accessory.Gravata },
            new() { look = CharacterLook.Figurante, models = new[] { "Mulher_Casual", "Homem_Adventurer", "Mulher_Formal", "Homem_Worker",
                                                                      "Mulher_Punk", "Mulher_Adventurer", "Mulher_Worker" },
                    scale = 1f, headScale = 1.3f, randomShirt = true },
        };

        /// <summary>Material da roupa de cima de cada figurante (sorteia a cor pelo seed).</summary>
        public static readonly (string model, string material)[] Shirts =
        {
            ("Mulher_Casual", "White"), ("Homem_Adventurer", "Green"), ("Mulher_Formal", "LimeGreen"), ("Homem_Worker", "LightBrown"),
            ("Mulher_Punk", "Pink"), ("Mulher_Adventurer", "Green"), ("Mulher_Worker", "White"),
        };

        public static readonly Color[] ShirtColors =
        {
            C(0.2f, 0.6f, 0.4f), C(0.8f, 0.25f, 0.3f), C(0.25f, 0.45f, 0.85f), C(0.9f, 0.7f, 0.2f),
            C(0.55f, 0.35f, 0.75f), C(0.95f, 0.55f, 0.65f), C(0.3f, 0.75f, 0.8f), C(0.95f, 0.95f, 0.95f),
        };

        static GameObject Load(string model) => AssetDatabase.LoadAssetAtPath<GameObject>(QuaterniusModelImport.Folder + model + ".fbx");

        [MenuItem("Prova Impossível/Personagens/Configurar modelos")]
        public static void Menu()
        {
            var set = Build();
            Selection.activeObject = set;
            Debug.Log("[Personagens] escalação atualizada: " + AssetPath);
        }

        public static CharacterModelSet Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Personagens")) AssetDatabase.CreateFolder("Assets/Resources", "Personagens");

            var set = AssetDatabase.LoadAssetAtPath<CharacterModelSet>(AssetPath);
            bool novo = set == null;
            if (novo) set = ScriptableObject.CreateInstance<CharacterModelSet>();

            var entries = new List<CharacterModelSet.Entry>();
            foreach (var role in Cast)
            {
                var prefabs = role.models.Select(Load).Where(p => p != null).ToArray();
                if (prefabs.Length == 0) { Debug.LogError("[Personagens] nenhum modelo achado para " + role.look); continue; }
                entries.Add(new CharacterModelSet.Entry
                {
                    look = role.look, models = prefabs, scale = role.scale, headScale = role.headScale,
                    tints = role.tints ?? new CharacterModelSet.Tint[0], accessories = role.acc, randomShirt = role.randomShirt,
                });
            }
            set.entries = entries.ToArray();
            set.shirtMaterials = Shirts.Select(s => new CharacterModelSet.ShirtMaterial { model = Load(s.model), material = s.material })
                                       .Where(s => s.model != null).ToArray();
            set.shirtColors = ShirtColors;

            var clips = AssetDatabase.LoadAllAssetsAtPath(QuaterniusModelImport.Folder + QuaterniusModelImport.AnimationsFile)
                                     .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            set.idle = clips.FirstOrDefault(c => c.name == "Idle");
            set.walk = clips.FirstOrDefault(c => c.name == "Walk");
            set.seatHipHeight = 0.62f;
            set.seatBack = 0.05f;
            set.headPitchFactor = 0.8f;
            if (set.idle == null) Debug.LogError("[Personagens] animação Idle não encontrada em " + QuaterniusModelImport.AnimationsFile);

            if (novo) AssetDatabase.CreateAsset(set, AssetPath);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>Para o batch: -executeMethod ProvaImpossivel.EditorTools.CharacterModelSetup.Batch</summary>
        public static void Batch()
        {
            AssetDatabase.Refresh();
            QuaterniusModelImport.Reimport();
            var set = Build();
            Debug.Log("[Personagens] batch ok: " + set.entries.Length + " papéis, idle=" + (set.idle != null));
        }
    }
}
