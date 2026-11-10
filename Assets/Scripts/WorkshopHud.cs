using UnityEngine;
using UnityEngine.UIElements;
using Jiaozi;

public sealed class WorkshopHud : MonoBehaviour
{
    Graybox game;
    VisualElement root, overlay, top, bottom;
    Label status, hint, players, title, details;
    Label[] stations, actors;
    Button[] buttons;
    public void Initialize(Graybox owner)
    {
        game = owner;
        var panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1280, 800);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = .5f;
        panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("GrayboxTheme");
        var doc = gameObject.AddComponent<UIDocument>();
        doc.panelSettings = panel;
        root = doc.rootVisualElement;
        root.style.flexGrow = 1;
        root.pickingMode = PickingMode.Ignore;
        root.style.unityFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 20);
        root.style.fontSize = 18;
        root.style.color = new Color(.96f, .94f, .87f);
        top = new VisualElement();
        top.style.position = Position.Absolute;
        top.style.top = 12;
        top.style.left = 16;
        top.style.right = 16;
        top.style.paddingTop = 12;
        top.style.paddingBottom = 12;
        top.style.paddingLeft = 18;
        top.style.backgroundColor = new Color(.08f, .13f, .15f, .96f);
        root.Add(top);
        status = Text(top, 24);
        hint = Text(top, 17);
        hint.style.color = new Color(.83f, .87f, .82f);
        bottom = new VisualElement();
        bottom.style.position = Position.Absolute;
        bottom.style.bottom = 12;
        bottom.style.left = 16;
        bottom.style.right = 16;
        bottom.style.paddingLeft = 18;
        bottom.style.paddingTop = 10;
        bottom.style.paddingBottom = 10;
        bottom.style.backgroundColor = new Color(.08f, .13f, .15f, .96f);
        root.Add(bottom);
        players = Text(bottom, 19);
        Text(bottom, 16).text = "P1  WASD · F 拿放/递交 · 按住 G 加工     P2  方向键 · / 拿放/递交 · 按住 . 加工     Esc 暂停";
        stations = new Label[game.layout.Length];
        for (int i = 0; i < stations.Length; i++)
            stations[i] = WorldLabel();
        actors = new[]
        {
            WorldLabel(),
            WorldLabel()
        };
        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.backgroundColor = new Color(0, 0, 0, .72f);
        root.Add(overlay);
        var card = new VisualElement();
        card.style.width = new Length(70, LengthUnit.Percent);
        card.style.maxWidth = 670;
        card.style.paddingTop = 28;
        card.style.paddingBottom = 28;
        card.style.paddingLeft = 30;
        card.style.paddingRight = 30;
        card.style.backgroundColor = new Color(.12f, .19f, .21f);
        overlay.Add(card);
        title = Text(card, 30);
        details = Text(card, 20);
        details.style.whiteSpace = WhiteSpace.Normal;
        details.style.marginTop = 15;
        details.style.marginBottom = 20;
        buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var b = new Button(() => game.MenuAction(index));
            b.style.height = 46;
            b.style.fontSize = 20;
            b.style.marginBottom = 10;
            b.style.color = Color.white;
            card.Add(b);
            buttons[i] = b;
        }

        buttons[2].text = "重开教学（R）";
        Text(card, 16).text = "↑↓ 选择 · Enter 确认 · Esc 继续/暂停";
        root.RegisterCallback<NavigationSubmitEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationMoveEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
        root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
    }

    void ApplySafeArea()
    {
        if (Screen.width == 0 || Screen.height == 0) return;
        var safe = Screen.safeArea;
        float sx = root.resolvedStyle.width / Screen.width;
        float sy = root.resolvedStyle.height / Screen.height;
        top.style.left = bottom.style.left = 16 + safe.xMin * sx;
        top.style.right = bottom.style.right = 16 + (Screen.width - safe.xMax) * sx;
        top.style.top = 12 + (Screen.height - safe.yMax) * sy;
        bottom.style.bottom = 12 + safe.yMin * sy;
    }

    Label Text(VisualElement parent, int size)
    {
        var l = new Label();
        l.style.fontSize = size;
        l.pickingMode = PickingMode.Ignore;
        parent.Add(l);
        return l;
    }

    Label WorldLabel()
    {
        var l = Text(root, 15);
        l.style.position = Position.Absolute;
        l.style.width = 100;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.style.whiteSpace = WhiteSpace.Normal;
        l.style.backgroundColor = new Color(.08f, .13f, .15f, .88f);
        l.style.paddingTop = 4;
        l.style.paddingBottom = 4;
        return l;
    }

    public void Refresh(string message)
    {
        var r = game.Rules;
        bool tutorial = r.Phase == RoundPhase.Tutorial;
        status.text = tutorial ? "交子赶工记 / 机制灰盒    教学 · 不计时" : $"订单 {r.Received}/{r.Required} 张    剩余 {Mathf.CeilToInt(r.Remaining)} 秒    已交 {r.Tickets} 张    完成 {r.Orders} 单    得分 {r.Score}";
        hint.text = tutorial ? (r.TutorialDelivered ? "教学已交付！按 Enter 清空教学物料并正式开张。" : "抄纸 → 晾干 → 印刷 → 盖印 → 交付。Enter 可跳过教学。") : message;
        players.text = $"P1：{Graybox.PaperName(r.Hands[0])}     |     P2：{Graybox.PaperName(r.Hands[1])}     |     " + message;
        for (int i = 0; i < stations.Length; i++)
        {
            var s = r.Slots[i];
            string state = s.Item == null ? (s.Progress > 0 ? "抄纸中" : "空闲") : Graybox.PaperName(s.Item);
            float duration = r.Duration(s);
            if (duration > 0 && s.Progress > 0 && !s.Complete)
                state += $" {Mathf.Clamp01(s.Progress / duration) * 100:F0}%";
            stations[i].text = game.layout[i].label + "\n" + state;
            stations[i].style.color = game.Players[0].Target == i ? new Color(.4f, 1, 1) : game.Players[1].Target == i ? new Color(1, .7f, .4f) : Color.white;
        }

        for (int i = 0; i < 2; i++)
            actors[i].text = $"P{i + 1} · {Graybox.PaperName(r.Hands[i])}";
        bool menu = r.Phase == RoundPhase.Paused || r.Phase == RoundPhase.Results;
        overlay.style.display = menu ? DisplayStyle.Flex : DisplayStyle.None;
        if (menu)
        {
            bool pause = r.Phase == RoundPhase.Paused;
            title.text = pause ? "作坊已暂停" : "本轮结算";
            details.text = pause ? "所有玩家、工位加工、晾纸与回合计时已冻结。" : $"交付 {r.Tickets} 张 · 完成 {r.Orders} 单\n得分 {r.Score} · 星级 {r.Stars}/3\n未完成订单已交的票保留逐张得分。";
            buttons[0].text = pause ? "继续游戏" : "再玩一局";
            buttons[1].text = "退出游戏";
        }
    }

    public void SelectMenu(int index)
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].style.backgroundColor = i == index ? new Color(.16f, .48f, .48f) : new Color(.25f, .29f, .3f);
        buttons[index].Focus();
    }

    public void PositionLabels()
    {
        if (root == null || root.resolvedStyle.width <= 0)
            return;
        for (int i = 0; i < stations.Length; i++)
            PositionLabel(stations[i], game.ItemAnchors[i].position + Vector3.up * .5f);
        for (int i = 0; i < actors.Length; i++)
            PositionLabel(actors[i], game.Players[i].Root.position + Vector3.up * 1.8f);
    }

    void PositionLabel(Label l, Vector3 world)
    {
        var p = game.View.WorldToViewportPoint(world);
        l.style.left = p.x * root.resolvedStyle.width - 50;
        l.style.top = (1 - p.y) * root.resolvedStyle.height - 35;
    }
}
