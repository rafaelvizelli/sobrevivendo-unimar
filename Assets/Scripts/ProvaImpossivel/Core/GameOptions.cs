using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Opções do jogador (Bloco 14): volume geral, volume dos efeitos e sensibilidade da câmera.
    /// Salvas em PlayerPrefs (chaves com o prefixo do jogo) e aplicadas ao iniciar. Padrões e limites só aqui.
    /// - Volume geral: <see cref="AudioListener.volume"/> (vale para TUDO, inclusive o ambiente).
    /// - Volume dos efeitos: multiplica os sons do <see cref="SoundFX"/>.
    /// - Sensibilidade: multiplica a sensibilidade da câmera gravada na cena (1 = como sempre foi).
    /// </summary>
    public static class GameOptions
    {
        public const string Prefix = "sobrevivendo_unimar.";
        public const string KeyMasterVolume = Prefix + "volume_geral";
        public const string KeySfxVolume = Prefix + "volume_efeitos";
        public const string KeyCameraSensitivity = Prefix + "sensibilidade_camera";

        public const float DefaultMasterVolume = 1f;
        public const float DefaultSfxVolume = 1f;
        public const float DefaultCameraSensitivity = 1f;

        public const float MinVolume = 0f, MaxVolume = 1f;
        public const float MinCameraSensitivity = 0.25f, MaxCameraSensitivity = 3f;

        // cópia em memória (a câmera lê todo frame; PlayerPrefs só ao carregar e ao salvar)
        static float master = DefaultMasterVolume, sfx = DefaultSfxVolume, sensitivity = DefaultCameraSensitivity;

        public static float MasterVolume => master;
        public static float SfxVolume => sfx;
        public static float CameraSensitivity => sensitivity;

        public static float ClampVolume(float v) => float.IsNaN(v) ? DefaultMasterVolume : Mathf.Clamp(v, MinVolume, MaxVolume);
        public static float ClampSensitivity(float v) => float.IsNaN(v) ? DefaultCameraSensitivity : Mathf.Clamp(v, MinCameraSensitivity, MaxCameraSensitivity);

        /// <summary>Lê do disco (valores fora dos limites são corrigidos) e aplica. Roda a cada Play, antes da cena.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            master = ClampVolume(PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume));
            sfx = ClampVolume(PlayerPrefs.GetFloat(KeySfxVolume, DefaultSfxVolume));
            sensitivity = ClampSensitivity(PlayerPrefs.GetFloat(KeyCameraSensitivity, DefaultCameraSensitivity));
            Apply();
        }

        public static void SetMasterVolume(float v) { master = ClampVolume(v); Save(KeyMasterVolume, master); Apply(); }
        public static void SetSfxVolume(float v) { sfx = ClampVolume(v); Save(KeySfxVolume, sfx); }
        public static void SetCameraSensitivity(float v) { sensitivity = ClampSensitivity(v); Save(KeyCameraSensitivity, sensitivity); }

        /// <summary>Apaga as opções salvas e volta aos padrões (testes / "restaurar padrão").</summary>
        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyMasterVolume);
            PlayerPrefs.DeleteKey(KeySfxVolume);
            PlayerPrefs.DeleteKey(KeyCameraSensitivity);
            PlayerPrefs.Save();
            Load();
        }

        /// <summary>Volta a memória aos padrões sem tocar no disco (testes: simula o jogo abrindo de novo).</summary>
        public static void ResetToDefaultsInMemoryOnly()
        {
            master = DefaultMasterVolume;
            sfx = DefaultSfxVolume;
            sensitivity = DefaultCameraSensitivity;
        }

        static void Save(string key, float v)
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
        }

        static void Apply() => AudioListener.volume = master;
    }
}
