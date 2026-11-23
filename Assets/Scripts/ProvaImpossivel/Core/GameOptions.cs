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
        public const string KeyMusicVolume = Prefix + "volume_musica"; // S5

        public const float DefaultMasterVolume = 1f;
        public const float DefaultSfxVolume = 1f;
        public const float DefaultCameraSensitivity = 1f;
        public const float DefaultMusicVolume = 0.8f;

        public const float MinVolume = 0f, MaxVolume = 1f;
        public const float MinCameraSensitivity = 0.25f, MaxCameraSensitivity = 3f;

        // cópia em memória (a câmera lê todo frame; PlayerPrefs só ao carregar e ao salvar)
        static float master = DefaultMasterVolume, sfx = DefaultSfxVolume, sensitivity = DefaultCameraSensitivity, music = DefaultMusicVolume;

        public static float MasterVolume => master;
        public static float SfxVolume => sfx;
        public static float CameraSensitivity => sensitivity;
        /// <summary>S5: volume da trilha (multiplica o máximo do <see cref="MusicDirector"/>).</summary>
        public static float MusicVolume => music;

        public static float ClampVolume(float v) => float.IsNaN(v) ? DefaultMasterVolume : Mathf.Clamp(v, MinVolume, MaxVolume);
        public static float ClampSensitivity(float v) => float.IsNaN(v) ? DefaultCameraSensitivity : Mathf.Clamp(v, MinCameraSensitivity, MaxCameraSensitivity);

        /// <summary>Lê do disco (valores fora dos limites são corrigidos) e aplica. Roda a cada Play, antes da cena.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            master = ClampVolume(PlayerPrefs.GetFloat(KeyMasterVolume, DefaultMasterVolume));
            sfx = ClampVolume(PlayerPrefs.GetFloat(KeySfxVolume, DefaultSfxVolume));
            sensitivity = ClampSensitivity(PlayerPrefs.GetFloat(KeyCameraSensitivity, DefaultCameraSensitivity));
            music = ClampVolume(PlayerPrefs.GetFloat(KeyMusicVolume, DefaultMusicVolume));
            ApplyVideo();
            Apply();
        }

        public static void SetMasterVolume(float v) { master = ClampVolume(v); Save(KeyMasterVolume, master); Apply(); }
        public static void SetSfxVolume(float v) { sfx = ClampVolume(v); Save(KeySfxVolume, sfx); }
        public static void SetCameraSensitivity(float v) { sensitivity = ClampSensitivity(v); Save(KeyCameraSensitivity, sensitivity); }
        public static void SetMusicVolume(float v) { music = ClampVolume(v); Save(KeyMusicVolume, music); }

        // ------------------------------------------------------------------ vídeo (S8, Operação Steam)

        public const string KeyFullscreen = Prefix + "tela_cheia";
        public const string KeyVSync = Prefix + "vsync";
        public const string KeyQuality = Prefix + "qualidade";

        /// <summary>Tela cheia (janela sem borda do tamanho da tela) ou janela. Padrão: tela cheia.</summary>
        public static bool Fullscreen => PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        /// <summary>Sincronia vertical (sem rasgo na tela, FPS preso ao monitor). Padrão: ligada.</summary>
        public static bool VSync => PlayerPrefs.GetInt(KeyVSync, 1) == 1;
        /// <summary>Nível de qualidade gráfica (índice de QualitySettings). -1 = o padrão do projeto para a plataforma.</summary>
        public static int Quality => PlayerPrefs.GetInt(KeyQuality, -1);

        /// <summary>Índice de qualidade válido para <paramref name="levels"/> níveis (-1 e fora da faixa = o mais alto). Puro.</summary>
        public static int ClampQuality(int q, int levels) => levels <= 0 ? 0 : q < 0 || q >= levels ? levels - 1 : q;

        /// <summary>Nome do nível de qualidade para a tela: o mais baixo é "Leve", o mais alto "Bonito". Puro.</summary>
        public static string QualityLabel(int q, int levels) =>
            levels <= 1 ? "Única" : q <= 0 ? "Leve" : q >= levels - 1 ? "Bonito" : "Médio";

        public static void SetFullscreen(bool on) { PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0); PlayerPrefs.Save(); ApplyVideo(); }
        public static void SetVSync(bool on) { PlayerPrefs.SetInt(KeyVSync, on ? 1 : 0); PlayerPrefs.Save(); ApplyVideo(); }
        public static void SetQuality(int q) { PlayerPrefs.SetInt(KeyQuality, q); PlayerPrefs.Save(); ApplyVideo(); }

        /// <summary>
        /// Aplica tela cheia, vsync e qualidade. Só no jogo compilado: no editor, mexer na qualidade/vsync altera o
        /// ProjectSettings do projeto (e os testes rodam no editor), então lá as opções só são guardadas.
        /// </summary>
        public static void ApplyVideo()
        {
            if (Application.isEditor) return;
            int levels = QualitySettings.names.Length;
            if (Quality >= 0) QualitySettings.SetQualityLevel(ClampQuality(Quality, levels), true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : 144; // sem vsync, um teto para não fritar a placa no menu
            Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        /// <summary>Apaga as opções salvas e volta aos padrões (testes / "restaurar padrão").</summary>
        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyMasterVolume);
            PlayerPrefs.DeleteKey(KeySfxVolume);
            PlayerPrefs.DeleteKey(KeyCameraSensitivity);
            PlayerPrefs.DeleteKey(KeyMusicVolume);
            PlayerPrefs.Save();
            Load();
        }

        /// <summary>Volta a memória aos padrões sem tocar no disco (testes: simula o jogo abrindo de novo).</summary>
        public static void ResetToDefaultsInMemoryOnly()
        {
            master = DefaultMasterVolume;
            sfx = DefaultSfxVolume;
            sensitivity = DefaultCameraSensitivity;
            music = DefaultMusicVolume;
        }

        static void Save(string key, float v)
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();
        }

        static void Apply() => AudioListener.volume = master;
    }
}
