using System;
using System.IO;
using UnityEngine;
using Jiaozi;

public class Graybox : MonoBehaviour
{
    [Serializable]
    public class StationLayout
    {
        public GameObject visualPrefab;
        public string label;
        public Vector3 position;
        public Vector3 operationOffset = new Vector3(0, 0, -.7f);
        public Vector3 size = Vector3.one;
    }

    public WorkshopConfig config = new WorkshopConfig();
    public FirstChapterData chapter;
    public GameFlow Flow { get; private set; }

    public StationLayout[] layout =
    {
        new StationLayout
        {
            label = "抄纸槽",
            position = new Vector3(-4.5f, 0, 1.8f)
        },
        new StationLayout
        {
            label = "晾纸 A",
            position = new Vector3(-3, 0, 1.8f)
        },
        new StationLayout
        {
            label = "晾纸 B",
            position = new Vector3(-1.5f, 0, 1.8f)
        },
        new StationLayout
        {
            label = "印刷台",
            position = new Vector3(2, 0, 1.8f)
        },
        new StationLayout
        {
            label = "盖印台",
            position = new Vector3(4, 0, 1.8f)
        },
        new StationLayout
        {
            label = "共享台",
            position = new Vector3(-1.1f, 0, -2.2f)
        },
        new StationLayout
        {
            label = "交付窗口",
            position = new Vector3(5, 0, -1.5f)
        }
    };
    public Vector3[] partitionPositions =
    {
        new Vector3(0, .5f, 2.5f),
        new Vector3(0, .5f, -2.5f)
    };
    public sealed class Actor
    {
        public Transform Root, Cargo;
        public GameObject Visual;
        public CharacterController Controller;
        public Vector3 Facing = Vector3.forward;
        public int Target = -1;
    }

    public WorkshopRules Rules { get; private set; }
    public Actor[] Players { get; private set; } = new Actor[2];
    public Camera View { get; private set; }
    public WorkshopInput Controls { get; private set; }
    public Transform[] ItemAnchors { get; private set; }

    Renderer[] surfaces;
    bool modeledWorkshop;
    Transform[] operations, paperViews;
    WorkshopHud hud;
    StreamWriter log;
    string message = "先在抄纸槽按住加工，再空手取纸。";
    int menuIndex, width, height;
    float elapsed, first = -1;
    readonly float[] idle =
    {
        0,
        0
    };
    static readonly Color[] paperColors =
    {
        new Color(.3f, .62f, .85f),
        new Color(.95f, .88f, .65f),
        new Color(.2f, .23f, .25f),
        new Color(.8f, .25f, .17f)
    };
    public static string PaperName(Paper p) => p == null ? "空手" : p.Kind == PaperKind.Wet ? "湿纸" : p.Kind == PaperKind.Dry ? "干纸" : p.Kind == PaperKind.Printed ? "黑墨交子" : "朱印交子";
    Material Mat(Color c) => new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
    {
        color = c
    };
    GameObject Box(string name, Vector3 p, Vector3 size, Color c, bool solid = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(transform);
        g.transform.position = p;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        if (!solid)
            Destroy(g.GetComponent<Collider>());
        return g;
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (chapter == null)
            chapter = Resources.Load<FirstChapterData>("FirstChapter");
        if (chapter == null)
            chapter = ScriptableObject.CreateInstance<FirstChapterData>();
        config = JsonUtility.FromJson<WorkshopConfig>(JsonUtility.ToJson(chapter.rules));
        config.starOrders = chapter.starOrders;
        if (chapter.stations != null && chapter.stations.Length == 7)
            layout = chapter.stations;
        if (chapter.partitions != null)
            partitionPositions = chapter.partitions;
        Rules = new WorkshopRules(config);
        Controls = new WorkshopInput();
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "Playtests");
            Directory.CreateDirectory(dir);
            log = new StreamWriter(Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"))
            {
                AutoFlush = true
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }

        Rules.Recorded += Record;
        Rules.Changed += Refresh;
        BuildWorld();
        hud = gameObject.AddComponent<WorkshopHud>();
        hud.Initialize(this);
        gameObject.AddComponent<WorkshopAudio>().Initialize(this);
        Flow = gameObject.AddComponent<GameFlow>();
        Flow.Initialize(this);
        gameObject.AddComponent<FirstChapterMenu>().Initialize(this);
        ApplyArtisans(Flow.Selected);
        Rules.Reset(true);
        Refresh();
    }

    void Record(string s)
    {
        log?.WriteLine($"{elapsed:F3} {s}");
        if (s.StartsWith("round_end"))
            log?.WriteLine($"{elapsed:F3} stationary_seconds p1={idle[0]:F1} p2={idle[1]:F1}");
    }

    void BuildWorld()
    {
        View = new GameObject("固定斜俯视镜头").AddComponent<Camera>();
        View.transform.SetParent(transform);
        View.tag = "MainCamera";
        View.transform.position = new Vector3(0, 12, -10);
        View.transform.LookAt(new Vector3(0, 0, .1f));
        View.orthographic = true;
        View.nearClipPlane = .1f;
        View.farClipPlane = 60;
        View.backgroundColor = new Color(.11f, .16f, .19f);
        View.clearFlags = CameraClearFlags.SolidColor;
        View.gameObject.AddComponent<AudioListener>();
        var light = new GameObject("日光").AddComponent<Light>();
        light.transform.SetParent(transform);
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(55, -25, 0);
        RenderSettings.ambientLight = Color.gray;
        var environment = Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-smoke") < 0
            ? Resources.Load<GameObject>("WorkshopModels/Workshop") : null;
        var art = environment != null ? Instantiate(environment, new Vector3(-6, 0, -4), Quaternion.Euler(0, 180, 0), transform) : null;
        modeledWorkshop = art != null;
        if (modeledWorkshop)
        {
            light.intensity = .80f;
            light.color = new Color(1f, .96f, .90f);
            light.transform.rotation = Quaternion.Euler(58, -32, 0);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .72f;
            light.shadowBias = .18f;
            light.shadowNormalBias = .22f;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.31f, .34f, .37f);
            // Keep the camera on the centerline so the floor grid stays square to the screen.
            View.transform.position = new Vector3(0, 10.8f, -12.5f);
            View.transform.LookAt(new Vector3(0, 0, .45f));
        }
        var boundaryStart = transform.childCount;
        Box("12x8 地面", new Vector3(0, -.16f, 0), new Vector3(12.4f, .3f, 8.4f), new Color(.61f, .59f, .51f));
        Box("纸院", new Vector3(-3, .001f, 0), new Vector3(5.9f, .01f, 7.9f), new Color(.62f, .66f, .57f), false);
        Box("印坊", new Vector3(3, .001f, 0), new Vector3(5.9f, .01f, 7.9f), new Color(.69f, .61f, .5f), false);
        Box("北墙", new Vector3(0, .4f, 4.1f), new Vector3(12.4f, .8f, .2f), Color.gray);
        Box("南墙", new Vector3(0, .15f, -4.1f), new Vector3(12.4f, .3f, .2f), Color.gray);
        foreach (float x in new[]
        {
            -6.1f,
            6.1f
        }

        )
            Box("侧墙", new Vector3(x, .4f, 0), new Vector3(.2f, .8f, 8.4f), Color.gray);
        if (modeledWorkshop)
        {
            for (int i = boundaryStart; i < transform.childCount; i++)
                transform.GetChild(i).GetComponent<Renderer>().enabled = false;
            foreach (var node in art.GetComponentsInChildren<Transform>(true))
            {
                if (!node.name.Contains("__Collider_")) continue;
                node.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 2;
                foreach (var renderer in node.GetComponentsInChildren<Renderer>())
                    renderer.enabled = false;
            }
        }
        else
            foreach (var p in partitionPositions)
                Box("可调整隔断", p, new Vector3(.18f, 1, 3), new Color(.38f, .4f, .38f));
        surfaces = new Renderer[layout.Length];
        operations = new Transform[layout.Length];
        ItemAnchors = new Transform[layout.Length];
        paperViews = new Transform[layout.Length];
        var stationNames = new[] { "pulp", "dry", "dry", "print", "stamp", "shared", "delivery" };
        for (int i = 0; i < layout.Length; i++)
        {
            var l = layout[i];
            if (!modeledWorkshop) surfaces[i] = Box(l.label, l.position + Vector3.up * .45f, new Vector3(l.size.x, .9f, l.size.z), new Color(.35f, .37f, .35f)).GetComponent<Renderer>();
            if (!modeledWorkshop && l.visualPrefab != null)
                Instantiate(l.visualPrefab, l.position, Quaternion.identity, transform);
            var op = new GameObject(l.label + "操作点");
            op.transform.SetParent(transform);
            op.transform.position = l.position + l.operationOffset;
            operations[i] = op.transform;
            var a = new GameObject(l.label + "物件锚点");
            a.transform.SetParent(transform);
            a.transform.position = l.position + Vector3.up;
            if (modeledWorkshop)
            {
                string prefix = "station_" + stationNames[i] + "__";
                string suffix = i == 1 ? "A" : i == 2 ? "B" : "main";
                var target = FindModelNode(art.transform, prefix + "interaction_" + suffix);
                var slot = FindModelNode(art.transform, prefix + "slot_" + (suffix == "main" ? "0" : suffix));
                if (target == null) throw new InvalidOperationException("Missing model interaction point: " + prefix + suffix);
                op.transform.position = new Vector3(target.position.x, 0, target.position.z);
                a.transform.position = slot != null ? slot.position : target.position;
            }
            ItemAnchors[i] = a.transform;
            paperViews[i] = Box("纸张", a.transform.position, new Vector3(.60f, .018f, .42f), Color.white, false).transform;
        }

        for (int i = 0; i < 2; i++)
        {
            var root = new GameObject("P" + (i + 1));
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(i == 0 ? -4 : 3, 0, -.7f);
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.3f;
            cc.radius = .325f;
            cc.center = new Vector3(0, .65f, 0);
            cc.stepOffset = .1f;
            cc.skinWidth = .025f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * .65f;
            body.transform.localScale = new Vector3(.65f, .65f, .65f);
            body.GetComponent<Renderer>().sharedMaterial = Mat(i == 0 ? new Color(.1f, .62f, .67f) : new Color(.87f, .38f, .2f));
            var marker = Box("朝向", root.transform.position + new Vector3(0, .1f, .55f), new Vector3(.15f, .08f, .35f), Color.white, false).transform;
            marker.SetParent(root.transform, true);
            var cargo = Box("手持锚点", root.transform.position + new Vector3(0, .85f, .5f), new Vector3(.60f, .018f, .42f), Color.white, false).transform;
            cargo.SetParent(root.transform, true);
            Players[i] = new Actor
            {
                Root = root.transform,
                Controller = cc,
                Cargo = cargo,
                Visual = body
            };
        }

        Physics.SyncTransforms();
    }

    void Update()
    {
        if (width != Screen.width || height != Screen.height)
        {
            width = Screen.width;
            height = Screen.height;
            View.orthographicSize = Mathf.Max(5.9f, 7.2f / View.aspect);
        }

        hud.PositionLabels();
        if (Flow != null && !Flow.LegacySmoke)
        {
            if (!Flow.WorldActive || Flow.TransitionFrame == Time.frameCount)
                return;
            // GameFlow handles Esc; never advance simulation on the pause input frame.
            if (Controls.Pause.WasPressedThisFrame())
                return;
        }

        if ((Flow == null || Flow.LegacySmoke) && Controls.Pause.WasPressedThisFrame())
        {
            Rules.TogglePause();
            menuIndex = 0;
            Refresh();
            hud.SelectMenu(0);
            return;
        }

        if (Rules.Phase == RoundPhase.Paused || Rules.Phase == RoundPhase.Results)
        {
            if (Controls.Up.WasPressedThisFrame())
            {
                menuIndex = (menuIndex + 2) % 3;
                hud.SelectMenu(menuIndex);
            }

            if (Controls.Down.WasPressedThisFrame())
            {
                menuIndex = (menuIndex + 1) % 3;
                hud.SelectMenu(menuIndex);
            }

            if (Controls.Restart.WasPressedThisFrame())
                Restart();
            else if (Controls.Confirm.WasPressedThisFrame())
                MenuAction(menuIndex);
            return;
        }

        if (Rules.Phase == RoundPhase.Tutorial && Controls.Confirm.WasPressedThisFrame())
        {
            if (Flow != null && !Flow.LegacySmoke)
                Flow.StartFormal();
            else
                StartRound();
            return;
        }

        Rules.BeginFrame(Time.deltaTime);
        if (!Rules.Active)
        {
            menuIndex = 0;
            hud.SelectMenu(0);
            return;
        }

        if (Rules.Phase == RoundPhase.Running)
            elapsed += Time.deltaTime;
        for (int i = 0; i < 2; i++)
        {
            var p = Players[i];
            var m = Controls.Move(i);
            var delta = new Vector3(m.x, 0, m.y);
            if (delta.sqrMagnitude > .001f)
            {
                p.Facing = delta.normalized;
                p.Root.rotation = Quaternion.LookRotation(p.Facing);
            }

            var before = p.Root.position;
            p.Controller.Move((delta * config.moveSpeed + Vector3.down * 5) * Time.deltaTime);
            if (Rules.Phase == RoundPhase.Running)
            {
                if (m.sqrMagnitude < .01f && !Controls.Work(i))
                    idle[i] += Time.deltaTime;
                if (delta.sqrMagnitude > .1f && Vector3.Distance(before, p.Root.position) < config.moveSpeed * Time.deltaTime * .25f)
                    Record("blocked player=" + (i + 1));
            }
        }

        Physics.SyncTransforms();
        bool changed = false;
        for (int i = 0; i < 2; i++)
        {
            var p = Players[i];
            int t = FindTarget(i);
            if (t != p.Target)
            {
                p.Target = t;
                changed = true;
            }

            if (Controls.Interact(i))
            {
                string reason = "面前没有可交互目标";
                bool ok = t >= 0 ? Rules.Interact(i, t, out reason) : t == -2 && Rules.Handoff(i, 1 - i, out reason);
                message = "P" + (i + 1) + "：" + (ok ? "交接成功" : reason);
                if (!ok)
                    Record("invalid_interaction player=" + (i + 1) + " " + reason);
                if (Rules.Phase == RoundPhase.Running && Rules.Tickets > 0 && first < 0)
                {
                    first = elapsed;
                    Record("first_delivery=" + first);
                }

                changed = true;
            }

            if (Controls.Work(i) && t >= 0 && !Rules.Process(i, t, Time.deltaTime, out var why) && !string.IsNullOrEmpty(why))
            {
                message = "P" + (i + 1) + "：" + why;
                changed = true;
            }
        }

        if (changed)
            Refresh();
        if (!Rules.OwnershipValid())
            Debug.LogError("纸张所有权校验失败");
    }

    public int FindTarget(int player)
    {
        var p = Players[player];
        float nearest = config.interactionDistance;
        int result = -1;
        for (int i = 0; i < operations.Length; i++)
        {
            var d = operations[i].position - p.Root.position;
            d.y = 0;
            if (d.magnitude <= nearest && Vector3.Dot(p.Facing, d.normalized) >= .7071067f && ClearPath(p.Root.position, operations[i].position, player, -1))
            {
                nearest = d.magnitude;
                result = i;
            }
        }

        var other = Players[1 - player];
        var diff = other.Root.position - p.Root.position;
        diff.y = 0;
        if (diff.magnitude < nearest && Vector3.Dot(p.Facing, diff.normalized) >= .7071067f && ClearPath(p.Root.position, other.Root.position, player, 1 - player))
            result = -2;
        return result;
    }

    public bool ClearPath(Vector3 from, Vector3 to, int source, int receiver)
    {
        foreach (var h in Physics.RaycastAll(from + Vector3.up * .65f, (to - from).normalized, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.IsChildOf(Players[source].Root) || receiver >= 0 && h.transform.IsChildOf(Players[receiver].Root))
                continue;
            return false;
        }

        return true;
    }

    void Refresh()
    {
        if (surfaces == null)
            return;
        for (int i = 0; i < surfaces.Length; i++)
        {
            bool p1 = Players[0].Target == i, p2 = Players[1].Target == i;
            if (surfaces[i] != null) surfaces[i].material.color = p1 && p2 ? new Color(.85f, .78f, .32f) : p1 ? new Color(.15f, .65f, .7f) : p2 ? new Color(.9f, .45f, .25f) : new Color(.35f, .37f, .35f);
            var item = Rules.Slots[i].Item;
            paperViews[i].gameObject.SetActive(item != null);
            if (item != null)
                paperViews[i].GetComponent<Renderer>().material.color = paperColors[(int)item.Kind];
        }

        for (int i = 0; i < 2; i++)
        {
            var p = Rules.Hands[i];
            Players[i].Cargo.gameObject.SetActive(p != null);
            if (p != null)
                Players[i].Cargo.GetComponent<Renderer>().material.color = paperColors[(int)p.Kind];
        }

        hud?.Refresh(message);
    }

    internal static Transform FindModelNode(Transform root, string nodeName)
    {
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
            if (node.name == nodeName) return node;
        return null;
    }

    public void ApplyArtisans(int[] selected)
    {
        for (int i = 0; i < 2; i++)
        {
            var actor = Players[i];
            var definition = chapter.artisans[selected[i]];
            if (actor.Visual != null)
                Destroy(actor.Visual);
            GameObject visual;
            var prefab = definition.visualPrefab;
            if (prefab == null && modeledWorkshop)
                prefab = Resources.Load<GameObject>("WorkshopModels/" + (selected[i] == 0 ? "Zhituan" : "Yintao"));
            if (prefab != null)
                visual = Instantiate(prefab, actor.Root);
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(visual.GetComponent<Collider>());
                visual.transform.SetParent(actor.Root, false);
                visual.transform.localPosition = Vector3.up * .65f;
                visual.transform.localScale = new Vector3(.65f, .65f, .65f);
                visual.GetComponent<Renderer>().material = Mat(definition.color);
            }

            actor.Visual = visual;
            actor.Cargo.localPosition = definition.handOffset;
            var adapter = visual.AddComponent<ArtisanVisual>();
            adapter.Initialize(this, i, definition.animator);
        }
    }

    public void StartRound()
    {
        menuIndex = 0;
        elapsed = 0;
        first = -1;
        Array.Clear(idle, 0, 2);
        Rules.Reset(false);
        ResetPlayers();
        message = "正式开张！一单一单完成交子。";
        Refresh();
    }

    public void Restart()
    {
        menuIndex = 0;
        Record($"restart idleP1={idle[0]:F1} idleP2={idle[1]:F1}");
        elapsed = 0;
        first = -1;
        Array.Clear(idle, 0, 2);
        Rules.Reset(true);
        ResetPlayers();
        message = "教学不计时，Enter 可跳过。";
        Refresh();
    }

    void ResetPlayers()
    {
        for (int i = 0; i < 2; i++)
        {
            var p = Players[i];
            p.Controller.enabled = false;
            p.Root.position = modeledWorkshop ? new Vector3(i == 0 ? -3.5f : 2.5f, 0, 0) : chapter.spawnPoints[i];
            p.Root.rotation = Quaternion.identity;
            p.Facing = Vector3.forward;
            p.Target = -1;
            p.Controller.enabled = true;
        }

        Physics.SyncTransforms();
    }

    public void MenuAction(int index)
    {
        if (index == 1)
        {
            Application.Quit();
            return;
        }

        if (index == 2)
        {
            Restart();
            return;
        }

        if (Rules.Phase == RoundPhase.Paused)
            Rules.TogglePause();
        else
            Restart();
        Refresh();
    }

    void OnDestroy()
    {
        Rules.Changed -= Refresh;
        Rules.Recorded -= Record;
        Controls?.Dispose();
        log?.Dispose();
    }
}
