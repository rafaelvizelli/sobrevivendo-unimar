using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Configura a importação dos personagens Quaternius (Ultimate Modular Men/Women, CC0) em
    /// Assets/Art/Personagens/Quaternius: rig Humanoid (as poses do jogo usam os ossos humanoides, então qualquer
    /// modelo serve) e loop nas animações contínuas.
    /// O mapeamento automático do Unity erra nesses arquivos (toma "Root" por quadril), então os ossos são mapeados
    /// pelo nome. Na primeira importação o Unity monta o esqueleto; a segunda (menu "Reimportar") corrige o mapa.
    /// </summary>
    public class QuaterniusModelImport : AssetPostprocessor
    {
        public const string Folder = "Assets/Art/Personagens/Quaternius/";
        /// <summary>Arquivo com as animações do pacote (esqueleto um pouco diferente do dos modelos).</summary>
        public const string AnimationsFile = "Animacoes_Homem.fbx";

        bool IsQuaternius => assetPath.Replace('\\', '/').StartsWith(Folder) && assetPath.EndsWith(".fbx");
        bool IsAnimations => assetPath.EndsWith(AnimationsFile) || assetPath.EndsWith(FemaleAnimationsFile);
        /// <summary>
        /// Modelos femininos: as pernas saem da raiz (no chão), o que o rig Humanoid não aceita. Ficam em Generic, com as
        /// animações do próprio pacote feminino (mesmo esqueleto, mesmos nomes de ossos).
        /// </summary>
        bool IsFemale => System.IO.Path.GetFileName(assetPath).StartsWith("Mulher_") || assetPath.EndsWith(FemaleAnimationsFile);
        public const string FemaleAnimationsFile = "Animacoes_Mulher.fbx";

        /// <summary>Animações que repetem (o resto toca uma vez).</summary>
        public static bool Loops(string clip)
        {
            string n = clip.ToLowerInvariant();
            return n.Contains("idle") || n.Contains("walk") || n.Contains("run");
        }

        /// <summary>Osso humanoide → nome do osso no arquivo. O de animações tem "Body" como quadril e "Wrist" como mão.</summary>
        public static Dictionary<string, string> BoneMap(bool animations)
        {
            var m = new Dictionary<string, string>
            {
                ["Hips"] = animations ? "Body" : "Hips",
                ["Spine"] = "Abdomen",
                ["Chest"] = "Torso",
                ["UpperChest"] = "Chest",
                ["Neck"] = "Neck",
                ["Head"] = "Head",
            };
            foreach (var (side, s) in new[] { ("Left", "L"), ("Right", "R") })
            {
                m[side + "Shoulder"] = "Shoulder." + s;
                m[side + "UpperArm"] = "UpperArm." + s;
                m[side + "LowerArm"] = "LowerArm." + s;
                m[side + "Hand"] = (animations ? "Wrist." : "Hand.") + s;
                m[side + "UpperLeg"] = "UpperLeg." + s;
                m[side + "LowerLeg"] = "LowerLeg." + s;
                // nas animações o pé fica fora da perna (alvo de IK); a ponta da canela faz o papel
                m[side + "Foot"] = animations ? "LowerLeg." + s + "_end" : "Foot." + s;
            }
            return m;
        }

        void OnPreprocessModel()
        {
            if (!IsQuaternius) return;
            var mi = (ModelImporter)assetImporter;
            mi.importAnimation = IsAnimations;
            mi.importCameras = false;
            mi.importLights = false;
            mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            if (IsFemale)
            {
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                return;
            }
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            var hd = mi.humanDescription;
            if (hd.skeleton == null || hd.skeleton.Length == 0) return; // 1ª importação: o Unity monta o esqueleto
            var names = new HashSet<string>(hd.skeleton.Select(b => b.name));
            hd.human = BoneMap(IsAnimations).Where(kv => names.Contains(kv.Value))
                .Select(kv => new HumanBone { humanName = HumanName(kv.Key), boneName = kv.Value, limit = new HumanLimit { useDefaultValues = true } })
                .ToArray();
            hd.upperArmTwist = hd.lowerArmTwist = hd.upperLegTwist = hd.lowerLegTwist = 0.5f;
            hd.armStretch = hd.legStretch = 0.05f;
            hd.feetSpacing = 0f;
            hd.hasTranslationDoF = false;
            mi.humanDescription = hd;
        }

        /// <summary>"LeftUpperArm" → "Left UpperArm" (o nome que o Unity usa em HumanTrait.BoneName).</summary>
        static string HumanName(string key)
        {
            foreach (var n in HumanTrait.BoneName)
                if (n.Replace(" ", "") == key) return n;
            return key;
        }

        void OnPreprocessAnimation()
        {
            if (!IsQuaternius || !IsAnimations) return;
            var mi = (ModelImporter)assetImporter;
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = c.name.Replace("CharacterArmature|", "");
                c.loopTime = Loops(c.name);
                if (!IsFemale)
                {
                    c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                }
            }
            mi.clipAnimations = clips;
        }

        [MenuItem("Prova Impossível/Personagens/Reimportar Quaternius")]
        public static void Reimport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Folder.TrimEnd('/') }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }
    }
}
