using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Monta a cena da Fase 1 a partir da sala de aula (SampleScene): personagens, interativos, cenário extra,
    /// pontos de patrulha, NavMesh e sistemas. Pode ser rodado de novo: tudo que ele cria fica sob "== PROVA IMPOSSIVEL ==".
    /// </summary>
    public static class ProvaImpossivelSetup
    {
        public const string ScenePath = "Assets/Scenes/Fase1_ProvaImpossivel.unity";
        const string SourceScene = "Assets/Scenes/SampleScene.unity";
        const string NavMeshFolder = "Assets/Scenes/Fase1_ProvaImpossivel";
        const string NavMeshPath = NavMeshFolder + "/NavMesh-Sala.asset";
        const string RootName = "== PROVA IMPOSSIVEL ==";

        // (fileira, coluna): fileira 1 = frente; coluna 1 = lado da porta, coluna 6 = lado das janelas
        static readonly Vector2Int SeatJogador = new(4, 5);
        static readonly Vector2Int SeatNerd = new(4, 6);
        static readonly Vector2Int SeatDorminhoco = new(3, 2);
        static readonly Vector2Int SeatFundao = new(6, 1);
        static readonly Vector2Int SeatPuxaSaco = new(1, 4);
        static readonly Vector2Int[] SeatsFigurantes = { new(1, 2), new(2, 1), new(2, 5), new(3, 4), new(3, 6), new(5, 3), new(5, 6), new(6, 4) };
        static readonly string[] NomesFigurantes = { "JÚLIA", "PEDRÃO", "BIA", "THIAGO", "LARI", "CAIO", "MANU", "RAFA" };

        static Transform carteiras;

        [MenuItem("Prova Impossível/Montar Fase 1")]
        public static void MenuBuild() => Debug.Log(Build());

        public static string Build()
        {
            var log = new System.Text.StringBuilder();
            OpenScene(log);

            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                // a cena tem ajustes feitos à mão (valores das dificuldades, posições): recriar apaga tudo isso
                bool confirmou = EditorUtility.DisplayDialog("Montar Fase 1",
                    "ATENÇÃO: isto vai APAGAR tudo sob \"" + RootName + "\" na cena " + ScenePath
                    + " e recriar do zero.\n\nTodos os ajustes manuais da sala (personagens, valores das dificuldades no "
                    + "GameManager, posições) serão perdidos.\n\nContinuar?",
                    "Apagar e recriar", "Cancelar");
                if (!confirmou) return "Montar Fase 1 cancelado: a cena não foi alterada.";
                Object.DestroyImmediate(oldRoot);
            }
            var root = new GameObject(RootName).transform;
            var personagens = Group("Personagens", root);
            var cenario = Group("Cenario Extra", root);
            var pontos = Group("Pontos", root);

            carteiras = GameObject.Find("Carteiras").transform;
            log.AppendLine("Renderers com materiais sobrando corrigidos: " + FixExtraMaterials());

            // ---------------------------------------------------------------- sistemas
            var sys = new GameObject("Sistemas");
            sys.transform.SetParent(root);
            sys.AddComponent<GameManager>();
            sys.AddComponent<ExamSystem>();
            sys.AddComponent<HUD>();
            sys.AddComponent<QuizChallenge>();
            sys.AddComponent<FacePokerChallenge>();
            sys.AddComponent<SoundFX>();
            var director = sys.AddComponent<EventDirector>();

            // ---------------------------------------------------------------- jogador
            var deskJ = Desk(SeatJogador);
            var player = MakeCharacter("Jogador", CharacterLook.Jogador, 1, SeatPos(deskJ), SeatRot(deskJ), personagens);
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.height = 1.7f;
            cc.radius = 0.2f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.03f;
            var pc = player.AddComponent<PlayerController>();
            pc.visual = player.GetComponent<CharacterVisual>();
            player.AddComponent<PlayerInteraction>();

            var seat = Point("Assento do Jogador", pontos, SeatPos(deskJ), SeatRot(deskJ));
            pc.seatPoint = seat;
            var neighbor = Desk(new Vector2Int(SeatJogador.x, SeatJogador.y - 1));
            float gapX = (Bounds(neighbor).max.x + Bounds(deskJ).min.x) * 0.5f;
            pc.standPoint = Point("Onde o Jogador Levanta", pontos, new Vector3(gapX, 0f, SeatPos(deskJ).z), SeatRot(deskJ));
            var pdesk = Point("Carteira do Jogador (sentar)", pontos, SeatPos(deskJ), SeatRot(deskJ)).gameObject.AddComponent<PlayerDesk>();
            pdesk.range = 1.3f;
            pc.visual.seated = true;
            pc.visual.Animate(0f);

            // ---------------------------------------------------------------- professor
            var patrol = Group("Patrulha", root);
            var points = new List<Transform>();
            void P(float x, float z) => points.Add(Point("P" + (points.Count + 1), patrol, new Vector3(x, 0f, z), Quaternion.identity));
            foreach (var z in new[] { -5.4f, -3f, -1f, 1f, 3f, 5f, 7.1f }) P(0f, z);
            foreach (var z in new[] { -4.5f, -1f, 2.5f, 6f }) { P(-5.1f, z); P(5.1f, z); }
            P(-3f, -5.4f); P(3f, -5.4f); P(-3f, 7.2f); P(3f, 7.2f);

            var prof = MakeCharacter("Prof. Takanervoso", CharacterLook.Professor, 7, new Vector3(1.4f, 0f, -6.3f), Quaternion.LookRotation(Vector3.forward), personagens);
            var agent = prof.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.9f;
            agent.speed = 1.1f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.3f;
            agent.enabled = false; // o ProfessorAI liga no Start, depois que o NavMesh da cena carregou
            var pcol = prof.AddComponent<CapsuleCollider>();
            pcol.center = new Vector3(0f, 0.95f, 0f);
            pcol.height = 1.9f;
            pcol.radius = 0.28f;
            var ai = prof.AddComponent<ProfessorAI>();
            ai.visual = prof.GetComponent<CharacterVisual>();
            ai.patrolPoints = points.ToArray();

            // ---------------------------------------------------------------- alunos
            var nerd = MakeStudent<NerdStudent>("Nerd", CharacterLook.Nerd, 2, SeatNerd, "NERD", new Color(0.5f, 0.8f, 1f), personagens);
            nerd.range = 1.9f;
            var sleeper = MakeStudent<SleeperStudent>("Dorminhoco", CharacterLook.Dorminhoco, 3, SeatDorminhoco, "DORMINHOCO", new Color(0.75f, 0.75f, 0.85f), personagens);
            var fundao = MakeStudent<BackRowStudent>("Cara do Fundão", CharacterLook.Fundao, 4, SeatFundao, "FUNDÃO", new Color(1f, 0.85f, 0.1f), personagens);
            var puxa = MakeStudent<SnitchStudent>("Puxa-saco", CharacterLook.PuxaSaco, 5, SeatPuxaSaco, "PUXA-SACO", new Color(1f, 0.55f, 0.65f), personagens);

            var extras = new List<ExtraStudent>();
            for (int i = 0; i < SeatsFigurantes.Length; i++)
            {
                var e = MakeStudent<ExtraStudent>("Figurante " + NomesFigurantes[i], CharacterLook.Figurante, 100 + i * 13, SeatsFigurantes[i], NomesFigurantes[i], new Color(0.85f, 0.85f, 0.85f), personagens);
                var d = Desk(SeatsFigurantes[i]);
                Color[] bagColors = { new(0.2f, 0.4f, 0.8f), new(0.8f, 0.3f, 0.3f), new(0.2f, 0.6f, 0.4f), new(0.3f, 0.3f, 0.35f), new(0.9f, 0.6f, 0.2f) };
                e.bag = Backpack(cenario, SeatPos(d) - d.right * 0.48f + d.forward * 0.15f, d.rotation, bagColors[i % bagColors.Length]);
                extras.Add(e);
                Paper(cenario, d);
            }

            // ---------------------------------------------------------------- interativos e cenário
            var mesa = GameObject.Find("table3");
            var mesaB = Bounds(mesa.transform);
            var entrega = Point("Mesa do Professor (entregar prova)", pontos, new Vector3(mesaB.center.x, 0f, mesaB.max.z + 0.2f), Quaternion.identity);
            entrega.gameObject.AddComponent<TeacherDesk>().range = 1.7f;

            BuildDeskProps(cenario, deskJ, sleeper, nerd, fundao, puxa, mesaB);
            var teacherChair = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/school/Prefabs/props/chair.prefab"));
            teacherChair.name = "Cadeira do Professor";
            teacherChair.transform.SetParent(cenario);
            teacherChair.transform.SetPositionAndRotation(new Vector3(mesaB.center.x, 0f, mesaB.min.z - 0.2f), Quaternion.identity);
            var tcn = teacherChair.AddComponent<NavMeshModifier>();
            tcn.ignoreFromBuild = true;

            BuildBoard(cenario);
            BuildClock(cenario);
            var projector = BuildProjector(cenario, pontos);
            var janela = Point("Janela (telefone do professor)", pontos, new Vector3(5.6f, 1.6f, -2f), Quaternion.identity);

            director.dorminhoco = sleeper;
            director.fundao = fundao;
            director.nerd = nerd;
            director.puxaSaco = puxa;
            director.projetor = projector;
            director.figurantes = extras.ToArray();
            director.janela = janela;

            // ---------------------------------------------------------------- câmera
            var camGo = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
            var tpc = camGo.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = pc;
            camGo.GetComponent<Camera>().nearClipPlane = 0.05f;
            camGo.transform.SetPositionAndRotation(SeatPos(deskJ) + new Vector3(0.4f, 1.9f, 2.6f), Quaternion.Euler(15f, 180f, 0f));
            EditorUtility.SetDirty(camGo);

            // ---------------------------------------------------------------- NavMesh
            var navGo = new GameObject("NavMesh");
            navGo.transform.SetParent(root);
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0f, 1f, 0f);
            surface.size = new Vector3(12.4f, 3f, 16.4f);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (!AssetDatabase.IsValidFolder(NavMeshFolder)) AssetDatabase.CreateFolder("Assets/Scenes", "Fase1_ProvaImpossivel");
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshPath) != null) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
            var tri = NavMesh.CalculateTriangulation();
            log.AppendLine("NavMesh: " + tri.vertices.Length + " vértices");

            // ---------------------------------------------------------------- build settings + salvar
            var scenes = new List<EditorBuildSettingsScene> { new(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("Cena salva: " + ScenePath);
            log.AppendLine("Personagens: jogador, professor, 4 alunos principais, " + extras.Count + " figurantes. Pontos de patrulha: " + points.Count);
            return log.ToString();
        }

        // ==================================================================== cena

        static void OpenScene(System.Text.StringBuilder log)
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return;
            if (active.isDirty) EditorSceneManager.SaveScene(active);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                AssetDatabase.CopyAsset(SourceScene, ScenePath);
                log.AppendLine("Cena criada a partir da SampleScene.");
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Os modelos do pacote da escola têm slots de material vazios a mais que submeshes.
        /// Com static batching (no Play) esses slots aparecem em magenta; aqui deixamos só os materiais válidos.
        /// </summary>
        static int FixExtraMaterials()
        {
            int fixedCount = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mats = r.sharedMaterials;
                int subs = mf.sharedMesh.subMeshCount;
                if (mats.Length <= subs) continue;
                var valid = new List<Material>();
                foreach (var m in mats) if (m != null) valid.Add(m);
                var result = new Material[subs];
                for (int i = 0; i < subs; i++)
                    result[i] = valid.Count == 0 ? null : valid[Mathf.Max(0, valid.Count - subs + i)];
                r.sharedMaterials = result;
                EditorUtility.SetDirty(r);
                fixedCount++;
            }
            return fixedCount;
        }

        static Transform Desk(Vector2Int s)
        {
            var d = carteiras.Find("Carteira F" + s.x + " C" + s.y);
            if (d == null) throw new System.Exception("Carteira não encontrada: F" + s.x + " C" + s.y);
            return d;
        }

        /// <summary>Assento da carteira (no chão), medido na malha da carteira.</summary>
        static Vector3 SeatPos(Transform desk)
        {
            var p = desk.TransformPoint(new Vector3(-0.116f, 0f, 0.05f));
            p.y = 0f;
            return p;
        }

        static Quaternion SeatRot(Transform desk) => Quaternion.LookRotation(desk.forward);
        static Vector3 DeskTop(Transform desk) => SeatPos(desk) + desk.forward * 0.6f + Vector3.up * 1.012f;

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            return t;
        }

        static Transform Point(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            t.SetPositionAndRotation(pos, rot);
            return t;
        }

        // ==================================================================== personagens

        static GameObject MakeCharacter(string name, CharacterLook look, int seed, Vector3 pos, Quaternion rot, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, rot);
            var v = go.AddComponent<CharacterVisual>();
            v.look = look;
            v.seed = seed;
            v.Build();
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            return go;
        }

        static T MakeStudent<T>(string name, CharacterLook look, int seed, Vector2Int seat, string display, Color nameColor, Transform parent) where T : StudentBase
        {
            var d = Desk(seat);
            var go = MakeCharacter(name, look, seed, SeatPos(d), SeatRot(d), parent);
            var col = go.AddComponent<CapsuleCollider>();
            // cobre também a cabeça (a câmera colide com os colegas em vez de atravessar)
            col.center = new Vector3(0f, 0.85f, 0f);
            col.height = 1.7f;
            col.radius = 0.24f;
            var s = go.AddComponent<T>();
            s.visual = go.GetComponent<CharacterVisual>();
            s.displayName = display;
            s.nameColor = nameColor;
            s.range = 2f;
            s.visual.seated = true;
            s.visual.writing = true;
            s.visual.Animate(0f);
            return s;
        }

        // ==================================================================== objetos

        static Transform Prop(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default, float smooth = 0.3f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(color, smooth);
            go.isStatic = true;
            return go.transform;
        }

        static Transform Backpack(Transform parent, Vector3 pos, Quaternion rot, Color color)
        {
            var root = new GameObject("Mochila").transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(pos, rot);
            Prop("Corpo", PrimitiveType.Cube, root, pos + Vector3.up * 0.19f, new Vector3(0.32f, 0.36f, 0.18f), color).SetParent(root, true);
            var pocket = Prop("Bolso", PrimitiveType.Cube, root, pos + Vector3.up * 0.12f + root.forward * 0.1f, new Vector3(0.24f, 0.14f, 0.05f), color * 0.75f);
            pocket.rotation = rot;
            pocket.SetParent(root, true);
            Prop("Topo", PrimitiveType.Sphere, root, pos + Vector3.up * 0.37f, new Vector3(0.32f, 0.08f, 0.18f), color).SetParent(root, true);
            return root;
        }

        static void Paper(Transform parent, Transform desk)
        {
            var top = DeskTop(desk);
            var yaw = desk.eulerAngles.y;
            Prop("Prova", PrimitiveType.Cube, parent, top + Vector3.up * 0.003f, new Vector3(0.21f, 0.004f, 0.29f), new Color(0.97f, 0.97f, 0.95f), new Vector3(0f, yaw + Random.Range(-12f, 12f), 0f));
            Prop("Lapis", PrimitiveType.Cylinder, parent, top + desk.right * 0.16f + Vector3.up * 0.012f, new Vector3(0.012f, 0.08f, 0.012f), new Color(1f, 0.82f, 0.15f), new Vector3(90f, yaw + 20f, 0f));
        }

        static void BuildDeskProps(Transform parent, Transform deskJ, SleeperStudent sleeper, NerdStudent nerd, BackRowStudent fundao, SnitchStudent puxa, Bounds mesaB)
        {
            Paper(parent, deskJ);
            Backpack(parent, SeatPos(deskJ) - deskJ.right * 0.0f + deskJ.forward * -0.55f, deskJ.rotation, new Color(0.45f, 0.25f, 0.7f));

            var dN = Desk(SeatNerd);
            Paper(parent, dN);
            var tn = DeskTop(dN) - dN.right * 0.22f;
            Prop("Livro", PrimitiveType.Cube, parent, tn + Vector3.up * 0.02f, new Vector3(0.18f, 0.04f, 0.25f), new Color(0.2f, 0.35f, 0.8f), new Vector3(0, 10, 0));
            Prop("Livro", PrimitiveType.Cube, parent, tn + Vector3.up * 0.06f, new Vector3(0.17f, 0.04f, 0.24f), new Color(0.8f, 0.2f, 0.25f), new Vector3(0, -5, 0));
            Prop("Livro", PrimitiveType.Cube, parent, tn + Vector3.up * 0.1f, new Vector3(0.16f, 0.035f, 0.22f), new Color(0.2f, 0.65f, 0.35f), new Vector3(0, 15, 0));
            Prop("Calculadora", PrimitiveType.Cube, parent, DeskTop(dN) + dN.right * 0.2f + Vector3.up * 0.01f, new Vector3(0.08f, 0.015f, 0.13f), new Color(0.15f, 0.15f, 0.17f));

            var dS = Desk(SeatDorminhoco);
            Prop("Travesseiro", PrimitiveType.Cube, parent, DeskTop(dS) + Vector3.up * 0.05f, new Vector3(0.42f, 0.1f, 0.3f), new Color(0.7f, 0.82f, 0.98f), new Vector3(0, 5, 0));
            Prop("Baba", PrimitiveType.Cylinder, parent, DeskTop(dS) + dS.forward * -0.05f + Vector3.up * 0.003f, new Vector3(0.09f, 0.002f, 0.07f), new Color(0.75f, 0.9f, 1f), default, 0.95f);

            var dF = Desk(SeatFundao);
            Prop("Energetico", PrimitiveType.Cylinder, parent, DeskTop(dF) + dF.right * 0.2f + Vector3.up * 0.065f, new Vector3(0.065f, 0.065f, 0.065f), new Color(0.2f, 0.85f, 0.3f), default, 0.8f);
            Prop("Salgadinho", PrimitiveType.Cube, parent, DeskTop(dF) - dF.right * 0.12f + Vector3.up * 0.04f, new Vector3(0.18f, 0.08f, 0.24f), new Color(1f, 0.5f, 0.1f), new Vector3(0, 30, 10));
            for (int i = 0; i < 5; i++)
                Prop("Bolinha de papel", PrimitiveType.Sphere, parent, SeatPos(dF) + new Vector3(-0.6f + i * 0.25f, 0.035f, 0.5f + (i % 2) * 0.3f), Vector3.one * 0.07f, new Color(0.95f, 0.95f, 0.92f));

            var dP = Desk(SeatPuxaSaco);
            Paper(parent, dP);
            var apple = DeskTop(dP) - dP.right * 0.2f + Vector3.up * 0.045f;
            Prop("Maca pro professor", PrimitiveType.Sphere, parent, apple, Vector3.one * 0.09f, new Color(0.85f, 0.1f, 0.1f), default, 0.7f);
            Prop("Cabinho", PrimitiveType.Cylinder, parent, apple + Vector3.up * 0.05f, new Vector3(0.008f, 0.015f, 0.008f), new Color(0.35f, 0.2f, 0.1f));

            // mesa do professor
            float top = mesaB.max.y;
            for (int i = 0; i < 4; i++)
                Prop("Pilha de provas", PrimitiveType.Cube, parent, new Vector3(mesaB.center.x - 0.45f, top + 0.004f + i * 0.008f, mesaB.center.z + 0.1f), new Vector3(0.21f, 0.006f, 0.29f), Color.white, new Vector3(0, i * 7f, 0));
            Prop("Caneca", PrimitiveType.Cylinder, parent, new Vector3(mesaB.center.x + 0.5f, top + 0.06f, mesaB.center.z + 0.15f), new Vector3(0.08f, 0.06f, 0.08f), new Color(0.95f, 0.95f, 0.95f));
            Prop("Caneta vermelha", PrimitiveType.Cylinder, parent, new Vector3(mesaB.center.x - 0.15f, top + 0.008f, mesaB.center.z + 0.25f), new Vector3(0.01f, 0.07f, 0.01f), new Color(0.9f, 0.1f, 0.1f), new Vector3(90f, 30f, 0f));

            Prop("Lixeira", PrimitiveType.Cylinder, parent, new Vector3(4.6f, 0.25f, 7.55f), new Vector3(0.36f, 0.25f, 0.36f), new Color(0.35f, 0.38f, 0.42f));
        }

        static void BuildBoard(Transform parent)
        {
            var board = GameObject.Find("board");
            var b = Bounds(board.transform);
            var go = new GameObject("Questao no Quadro", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            // abaixo do projetor, que fica pendurado na frente da parte de cima do quadro
            rt.sizeDelta = new Vector2(680f, 200f);
            rt.localScale = Vector3.one * 0.01f;
            rt.SetPositionAndRotation(new Vector3(b.center.x, 1.9f, b.max.z + 0.01f), Quaternion.Euler(0f, 180f, 0f));
            var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(go.transform, false);
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.color = new Color(0.96f, 0.96f, 0.9f);
            t.alignment = TextAnchor.UpperLeft;
            t.text = "PROVA FINAL — Prof. Takanervoso   (nota mínima: depende do meu humor)\n"
                   + "Q1) Um boleto vence em 3 dias. O aluno tem R$ 4,50 e um pastel.\n"
                   + "Calcule a velocidade do desespero em m/s², sem o atrito da esperança.\n"
                   + "Q2) Prove que 'eu ia estudar ontem' não é uma demonstração válida.\n\n"
                   + "* Proibido: colar, chorar, rezar olhando pro teto e respirar alto.";
        }

        static void BuildClock(Transform parent)
        {
            var root = new GameObject("Relogio de Parede").transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(new Vector3(4.85f, 2.75f, -7.96f), Quaternion.identity);
            Color black = new(0.08f, 0.08f, 0.08f);
            Prop("Aro", PrimitiveType.Cylinder, root, root.position + Vector3.back * 0.005f, new Vector3(0.62f, 0.02f, 0.62f), black, new Vector3(90f, 0f, 0f)).SetParent(root, true);
            Prop("Mostrador", PrimitiveType.Cylinder, root, root.position + Vector3.forward * 0.012f, new Vector3(0.56f, 0.015f, 0.56f), new Color(0.98f, 0.97f, 0.92f), new Vector3(90f, 0f, 0f)).SetParent(root, true);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                var pos = root.position + new Vector3(Mathf.Sin(a) * 0.23f, Mathf.Cos(a) * 0.23f, 0.03f);
                Prop("Marca", PrimitiveType.Cube, root, pos, i % 3 == 0 ? new Vector3(0.025f, 0.06f, 0.01f) : new Vector3(0.015f, 0.035f, 0.01f), black, new Vector3(0f, 0f, -i * 30f)).SetParent(root, true);
            }
            var clock = root.gameObject.AddComponent<ClassClock>();
            clock.hourHand = Hand(root, "Ponteiro Horas", 0.034f, 0.14f, 0.012f, black, 0.036f);
            clock.minuteHand = Hand(root, "Ponteiro Minutos", 0.022f, 0.21f, 0.01f, black, 0.042f);
            clock.secondHand = Hand(root, "Ponteiro Segundos", 0.008f, 0.23f, 0.006f, new Color(0.85f, 0.1f, 0.1f), 0.048f);
            Prop("Centro", PrimitiveType.Cylinder, root, root.position + Vector3.forward * 0.052f, new Vector3(0.03f, 0.005f, 0.03f), black, new Vector3(90f, 0f, 0f)).SetParent(root, true);
        }

        static Transform Hand(Transform root, string name, float width, float length, float depth, Color color, float z)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, 0f, z);
            var h = Prop("Haste", PrimitiveType.Cube, pivot, pivot.position + Vector3.up * length * 0.4f, new Vector3(width, length, depth), color);
            h.SetParent(pivot, true);
            h.gameObject.isStatic = false;
            return pivot;
        }

        static ProjectorDevice BuildProjector(Transform parent, Transform pontos)
        {
            var proj = GameObject.Find("projector");
            var pb = Bounds(proj.transform);
            var board = Bounds(GameObject.Find("board").transform);
            var go = new GameObject("Projetor (pode falhar)");
            go.transform.SetParent(parent);
            var lensPos = new Vector3(pb.center.x, pb.min.y + 0.12f, pb.max.z - 0.15f);
            var target = new Vector3(board.center.x, 2.2f, board.max.z);
            go.transform.SetPositionAndRotation(lensPos, Quaternion.LookRotation(target - lensPos));
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(0.8f, 0.9f, 1f);
            light.intensity = 4f;
            light.range = 6f;
            light.spotAngle = 55f;
            light.shadows = LightShadows.None;
            var dev = go.AddComponent<ProjectorDevice>();
            dev.beam = light;
            dev.fixSpot = Point("Onde o professor conserta o projetor", pontos, new Vector3(board.center.x + 0.8f, 0f, -5.6f), Quaternion.identity);
            return dev;
        }
    }
}
