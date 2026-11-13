using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Ferramenta do trailer (só editor, fora do build): roda a cena real em Play, aciona as mecânicas do jogo
    /// e grava cada plano como sequência de PNG 1920×1080 a 30 fps em Trailer/clips/NN_nome/ (+ .mp4 pelo
    /// codificador do próprio Unity). Tudo o que ela cria (câmera de UI, Volume, teclado virtual) é temporário e
    /// morre com o Play; a cena nunca é salva.
    /// Menu: Prova Impossível > Trailer > Gerar planos.
    /// Batch: Unity.exe -batchmode -projectPath . -executeMethod ProvaImpossivel.EditorTools.TrailerShots.Batch
    ///        [-trailerOnly 06_colinha,08_bilhete] -logFile x.log   (com o Unity FECHADO)
    /// </summary>
    [InitializeOnLoad]
    public static class TrailerShots
    {
        public const string ScenePath = "Assets/Scenes/Fase1_ProvaImpossivel.unity";
        public const int W = 1920, H = 1080, FPS = 30;
        const string KeyPending = "TrailerShots.pending", KeyBatch = "TrailerShots.batch", KeyOnly = "TrailerShots.only";

        public static string ClipsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Trailer", "clips"));

        static bool finished, failed;
        static double startedAt;

        static TrailerShots() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem("Prova Impossível/Trailer/Gerar planos")]
        static void Menu()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[Trailer] Saia do Play antes de gerar os planos."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Begin(false, "");
        }

        /// <summary>Entrada para -executeMethod (batch). -trailerOnly a,b limita aos planos com esses ids.</summary>
        public static void Batch()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-trailerOnly");
            Begin(true, i >= 0 && i + 1 < args.Length ? args[i + 1] : "");
        }

        static void Begin(bool batch, string only)
        {
            EditorSceneManager.OpenScene(ScenePath);
            finished = failed = false;
            SessionState.SetBool(KeyPending, true);
            SessionState.SetBool(KeyBatch, batch);
            SessionState.SetString(KeyOnly, only ?? "");
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyPending, false))
            {
                SessionState.SetBool(KeyPending, false);
                startedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += WaitForGame;
            }
            else if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(KeyBatch, false))
            {
                SessionState.SetBool(KeyBatch, false);
                EditorApplication.Exit(finished && !failed ? 0 : 1);
            }
        }

        static void WaitForGame()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 60)
            {
                EditorApplication.update -= WaitForGame;
                Debug.LogError("[Trailer] A cena não iniciou em 60 s.");
                Done(false);
                return;
            }
            if (GameManager.I == null || HUD.I == null) return;
            EditorApplication.update -= WaitForGame;
            // o anfitrião das corrotinas sobrevive às recargas da cena (cada plano começa numa sala limpa)
            var host = new GameObject("TrailerHost (temporário)");
            Object.DontDestroyOnLoad(host);
            var runner = host.AddComponent<LayoutElement>(); // qualquer MonoBehaviour sem efeito serve de anfitrião
            var only = SessionState.GetString(KeyOnly, "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            runner.StartCoroutine(Flatten(TrailerShotsPlanos.Run(only), e => { Debug.LogException(e); Done(false); }, () => Done(true)));
        }

        static void Done(bool ok)
        {
            finished = true;
            failed |= !ok;
            Debug.Log(ok ? "[Trailer] Planos gerados em " + ClipsDir : "[Trailer] Falhou (veja o log).");
            EditorApplication.ExitPlaymode();
        }

        /// <summary>Roda corrotinas aninhadas numa pilha só, para pegar exceção de qualquer nível.</summary>
        static IEnumerator Flatten(IEnumerator root, Action<Exception> onError, Action onDone)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e) { Rig.Teardown(); onError(e); yield break; }
                if (!moved) { stack.Pop(); continue; }
                if (top.Current is IEnumerator sub) { stack.Push(sub); continue; }
                yield return top.Current;
            }
            Rig.Teardown();
            onDone();
        }
    }

    public enum TrailerUi { Nada, Jogo, Resultado }

    /// <summary>Câmera, pós-processamento, teclado virtual e captura. Reaplicado a cada recarga da cena.</summary>
    public static class Rig
    {
        public const int UiLayer = 31;
        public static Camera Cam;     // a câmera do jogo (Camera.main), conduzida pelo trailer
        static Camera uiCam;          // desenha só a interface, sem pós-processamento, por cima
        static Canvas canvas;
        static RenderTexture rt, rtUi;
        static Texture2D tex, texUi;
        static Volume volume;
        static DepthOfField dof;
        static Keyboard kb;
        static InputSettings oldInput;
        static bool tutorialWasDone, hooked;
        public static TrailerUi Ui = TrailerUi.Nada;
        public static float BubbleScale = 1.5f;
        public static bool ShowBubbles = true; // nos closes a legenda do trailer substitui o balão (que cairia no rosto)
        // No jogo a cabeça do jogador sentado segue a câmera. No trailer a câmera fica na frente/ao lado dele e o
        // enquadramento usa a direção da cabeça: os dois se perseguiam e a cabeça balançava de um lado pro outro.
        // Por isso cada quadro capturado fixa o giro da cabeça neste valor (0 = olhando pra frente, pra prova).
        public static float PlayerHeadYaw = 0f;

        /// <summary>Recarrega a cena, começa uma prova no Fácil e prepara tudo. A sala sempre começa igual.</summary>
        public static IEnumerator Fresh()
        {
            if (!hooked)
            {
                tutorialWasDone = TutorialFlag.Done;
                TutorialFlag.MarkDone();
                oldInput = InputSystem.settings;
                var ns = Object.Instantiate(oldInput);
                ns.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                ns.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings = ns;
                kb = InputSystem.AddDevice<Keyboard>("TrailerTeclado");
                Canvas.willRenderCanvases += BiggerBubbles;
                hooked = true;
            }
            Release();
            Time.captureFramerate = 0;
            var op = SceneManager.LoadSceneAsync(TrailerShots.ScenePath, LoadSceneMode.Single);
            while (op != null && !op.isDone) yield return null;
            float limit = Time.realtimeSinceStartup + 20f;
            while ((GameManager.I == null || HUD.I == null || GameManager.I.Phase != Phase.Intro) && Time.realtimeSinceStartup < limit) yield return null;
            UnityEngine.Random.InitState(1234);
            GameManager.I.Begin(Difficulty.Facil);
            kb.MakeCurrent();
            SwapChalkboard();
            Setup();
            float settle = Time.realtimeSinceStartup + 5f; // a sala "assenta" (as falas de início somem)
            while (Time.realtimeSinceStartup < settle) yield return null;
        }

        static void Setup()
        {
            Cam = Camera.main;
            Cam.cullingMask &= ~(1 << UiLayer);
            var urp = Cam.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urp.antialiasingQuality = AntialiasingQuality.High;
            if (rt == null) rt = new RenderTexture(TrailerShots.W, TrailerShots.H, 24, RenderTextureFormat.ARGB32);
            if (tex == null) tex = new Texture2D(TrailerShots.W, TrailerShots.H, TextureFormat.RGBA32, false);
            if (texUi == null) texUi = new Texture2D(TrailerShots.W, TrailerShots.H, TextureFormat.RGBA32, false);
            if (rtUi == null) rtUi = new RenderTexture(TrailerShots.W, TrailerShots.H, 24, RenderTextureFormat.ARGB32);
            Cam.targetTexture = rt;

            var ug = new GameObject("TrailerUiCam (temporária)");
            uiCam = ug.AddComponent<Camera>();
            uiCam.enabled = false;
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.cullingMask = 1 << UiLayer;
            uiCam.targetTexture = rtUi;
            var uurp = uiCam.GetUniversalAdditionalCameraData();
            uurp.renderPostProcessing = false;
            uurp.antialiasing = AntialiasingMode.None;

            canvas = HUD.I.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 0.5f;

            // pós-processamento só da captura: vinheta leve, profundidade de campo, cor quente
            var vg = new GameObject("TrailerVolume (temporário)");
            volume = vg.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.26f); vig.smoothness.Override(0.5f);
            dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focusDistance.Override(2f);
            dof.aperture.Override(5.6f); dof.focalLength.Override(55f);
            var ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.08f); ca.contrast.Override(10f); ca.saturation.Override(6f);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(16f); wb.tint.Override(3f);
            var sm = profile.Add<ShadowsMidtonesHighlights>(true);
            sm.shadows.Override(new Vector4(0.98f, 0.97f, 1.04f, 0f)); sm.highlights.Override(new Vector4(1.04f, 1f, 0.94f, 0f));
        }

        /// <summary>Tira o texto do "boleto" do quadro só na memória (a cena no disco não muda).</summary>
        static void SwapChalkboard()
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.text == null || !t.text.Contains("boleto")) continue;
                t.text = string.Join("\n", t.text.Split('\n')
                    .Where(l => !l.Contains("boleto") && !l.Contains("desespero"))
                    .Select(l => l.StartsWith("Q2)") ? "Q1)" + l.Substring(3) : l));
            }
        }

        /// <summary>Balões maiores, legíveis no vídeo. Roda logo antes do canvas desenhar (depois do HUD posicionar).</summary>
        public static void BiggerBubbles()
        {
            if (canvas == null || !Application.isPlaying) return;
            var root = (RectTransform)canvas.transform;
            var balloons = root.Find("Baloes");
            if (balloons == null) return;
            var rr = new Vector3[4]; root.GetWorldCorners(rr);
            foreach (RectTransform b in balloons)
            {
                if (!b.gameObject.activeSelf) continue;
                if (b.localScale.x < BubbleScale * 0.7f) b.localScale = b.localScale * BubbleScale;
                var c = new Vector3[4]; b.GetWorldCorners(c);
                Vector3 shift = Vector3.zero;
                if (c[2].y > rr[2].y) shift.y = rr[2].y - c[2].y;
                if (c[0].x < rr[0].x) shift.x = rr[0].x - c[0].x;
                if (c[2].x > rr[2].x) shift.x = rr[2].x - c[2].x;
                b.position += shift;
            }
        }

        static void ApplyUi()
        {
            if (canvas == null) return;
            foreach (var n in new[] { "HUD", "Mensagens" })
            {
                var t = canvas.transform.Find(n);
                if (t != null) t.localScale = Ui == TrailerUi.Jogo ? Vector3.one : Vector3.zero;
            }
            var bal = canvas.transform.Find("Baloes");
            if (bal != null) bal.localScale = ShowBubbles ? Vector3.one : Vector3.zero;
            var p = canvas.transform.Find("Paineis");
            if (p != null) p.localScale = Ui == TrailerUi.Nada ? Vector3.zero : Vector3.one;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer != UiLayer) t.gameObject.layer = UiLayer;
        }

        /// <summary>Posiciona a câmera e foca a profundidade de campo no alvo.</summary>
        public static void Pose(Vector3 pos, Vector3 lookAt, float fov, float focus = -1f)
        {
            Cam.transform.position = pos;
            Cam.transform.rotation = Quaternion.LookRotation(lookAt - pos, Vector3.up);
            Cam.fieldOfView = fov;
            SetFocus(focus > 0f ? focus : Vector3.Distance(pos, lookAt));
        }

        public static void SetFocus(float meters)
        {
            if (dof == null) return;
            dof.focusDistance.Override(meters);
        }

        public static void SetDof(bool on) { if (dof != null) dof.active = on; }

        public static void Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(kb, keys.Length == 0 ? new KeyboardState() : new KeyboardState(keys));
        }

        public static void Release() { if (kb != null) InputSystem.QueueStateEvent(kb, new KeyboardState()); }

        /// <summary>
        /// Grava um plano: <paramref name="frame"/> recebe (segundos, 0..1) a cada quadro, antes do jogo atualizar a
        /// animação; depois a câmera é desenhada e o quadro vai para PNG (e para o .mp4).
        /// </summary>
        public static IEnumerator Capture(string id, float seconds, Action<float, float> frame)
        {
            string dir = Path.Combine(TrailerShots.ClipsDir, id);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            string mp4 = Path.Combine(TrailerShots.ClipsDir, id + ".mp4");
            if (File.Exists(mp4)) File.Delete(mp4);

            MediaEncoder enc = null;
            try
            {
                enc = new MediaEncoder(mp4, new VideoTrackAttributes
                {
                    frameRate = new MediaRational(TrailerShots.FPS),
                    width = TrailerShots.W, height = TrailerShots.H,
                    includeAlpha = false, bitRateMode = VideoBitrateMode.High,
                });
            }
            catch (Exception e) { Debug.LogWarning("[Trailer] Sem .mp4 (" + e.Message + "); ficam só os PNG."); enc = null; }

            int n = Mathf.RoundToInt(seconds * TrailerShots.FPS);
            Time.captureFramerate = TrailerShots.FPS;
            try
            {
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)TrailerShots.FPS;
                    frame(t, n > 1 ? i / (float)(n - 1) : 1f);
                    // aqui o Update do jogador já rodou (e mandou a cabeça seguir a câmera) e o LateUpdate que
                    // anima o boneco ainda não: é o ponto certo para travar a cabeça
                    if (PlayerController.I != null && PlayerController.I.visual != null)
                        PlayerController.I.visual.headYaw = PlayerHeadYaw;
                    ApplyUi();
                    yield return null; // o jogo anda 1/30 s: Update, LateUpdate (animação, balões)
                    uiCam.transform.SetPositionAndRotation(Cam.transform.position, Cam.transform.rotation);
                    uiCam.fieldOfView = Cam.fieldOfView;
                    Cam.Render();
                    BiggerBubbles();
                    Canvas.ForceUpdateCanvases();
                    uiCam.Render();
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, TrailerShots.W, TrailerShots.H), 0, 0);
                    RenderTexture.active = rtUi;
                    texUi.ReadPixels(new Rect(0, 0, TrailerShots.W, TrailerShots.H), 0, 0);
                    RenderTexture.active = prev;
                    // mundo (com pós) + interface (sem pós, alfa pré-multiplicado) por cima; o quadro final é opaco
                    var px = tex.GetPixelData<Color32>(0);
                    var ui = texUi.GetPixelData<Color32>(0);
                    for (int k = 0; k < px.Length; k++)
                    {
                        var w = px[k]; var u = ui[k];
                        int inv = 255 - u.a;
                        px[k] = new Color32((byte)Mathf.Min(255, u.r + w.r * inv / 255), (byte)Mathf.Min(255, u.g + w.g * inv / 255),
                                            (byte)Mathf.Min(255, u.b + w.b * inv / 255), 255);
                    }
                    tex.Apply(false);
                    File.WriteAllBytes(Path.Combine(dir, i.ToString("0000") + ".png"), tex.EncodeToPNG());
                    enc?.AddFrame(tex);
                }
            }
            finally
            {
                enc?.Dispose();
                Time.captureFramerate = 0;
                Release();
            }
            Debug.Log("[Trailer] " + id + ": " + n + " quadros");
        }

        public static void Teardown()
        {
            Time.captureFramerate = 0;
            if (!hooked) return;
            Canvas.willRenderCanvases -= BiggerBubbles;
            if (kb != null) InputSystem.RemoveDevice(kb);
            kb = null;
            if (oldInput != null) InputSystem.settings = oldInput;
            if (!tutorialWasDone) TutorialFlag.Clear();
            if (rt != null) rt.Release();
            if (rtUi != null) rtUi.Release();
            rtUi = null;
            rt = null;
            hooked = false;
        }

        // ------------------------------------------------------------- ajudas de enquadramento

        public static CharacterVisual Look(CharacterLook l) =>
            Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None).FirstOrDefault(v => v.look == l);

        /// <summary>Para onde o personagem olha (no chão).</summary>
        public static Vector3 Facing(CharacterVisual v)
        {
            var f = v.look == CharacterLook.Jogador ? v.HeadForward : v.transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }

        /// <summary>Ponto relativo à cabeça: na frente (dist), de lado (side, + = direita dele) e altura.</summary>
        public static Vector3 Rel(CharacterVisual v, float dist, float side, float up)
        {
            var f = Facing(v);
            var r = Vector3.Cross(Vector3.up, f);
            return v.HeadPosition + f * dist + r * side + Vector3.up * up;
        }

        public static float Ease(float p) { p = Mathf.Clamp01(p); return p * p * (3f - 2f * p); }
        public static float EaseOut(float p) { p = Mathf.Clamp01(p); return 1f - (1f - p) * (1f - p) * (1f - p); }
    }
}
