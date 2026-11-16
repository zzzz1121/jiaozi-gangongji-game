using UnityEngine;
using UnityEngine.UIElements;
using Jiaozi;

public sealed class WorkshopHud : MonoBehaviour
{
    Graybox game;
    VisualElement root, overlay, top, bottom;
    Label order, timer, score, hint, title, details;
    readonly Label[] hands = new Label[2];
    VisualElement[] stationProgress;
    Label[] stations, actors;
    Button[] buttons;
    int observedOrders;
    float orderNoticeUntil;
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
        top = HudStrip();
        var summary = Row(top);
        summary.style.alignItems = Align.FlexStart;
        var orderStack = new VisualElement { pickingMode = PickingMode.Ignore };
        summary.Add(orderStack);
        var orderPaper = Paper(orderStack, new Rect(.02f, .105f, .46f, .36f), 228, 94);
        var orderRow = Row(orderPaper);
        orderRow.style.alignItems = Align.Center;
        orderRow.Add(Art2D.Picture(Art2D.Region("source-07", 0, 0, 1, 1), 36, 55));
        order = Text(orderRow, 20);
        order.style.marginLeft = 10;
        order.style.whiteSpace = WhiteSpace.Normal;
        order.style.unityFontStyleAndWeight = FontStyle.Bold;
        score = Text(Paper(orderStack, new Rect(.52f, .17f, .46f, .245f), 226, 37), 15);
        score.style.unityTextAlign = TextAnchor.MiddleCenter;
        var gap = new VisualElement { pickingMode = PickingMode.Ignore };
        gap.style.flexGrow = 1;
        summary.Add(gap);
        var clock = Paper(summary, new Rect(.52f, .17f, .46f, .245f), 134, 48);
        timer = Text(clock, 25);
        timer.style.unityTextAlign = TextAnchor.MiddleCenter;
        timer.style.unityFontStyleAndWeight = FontStyle.Bold;
        var pause = new Button(() => game.Flow.Pause()) { text = "Ⅱ", tooltip = "暂停（Esc）", focusable = false };
        pause.style.width = pause.style.height = 45;
        pause.style.marginLeft = 6;
        pause.style.backgroundColor = new Color(.58f, .18f, .12f);
        pause.style.color = new Color(1, .93f, .76f);
        pause.style.fontSize = 23;
        pause.style.borderTopLeftRadius = pause.style.borderTopRightRadius =
            pause.style.borderBottomLeftRadius = pause.style.borderBottomRightRadius = 10;
        summary.Add(pause);
        hint = Text(top, 15);
        hint.style.position = Position.Absolute;
        hint.style.top = 58;
        hint.style.whiteSpace = WhiteSpace.Normal;
        hint.style.unityTextAlign = TextAnchor.MiddleCenter;
        Caption(hint);
        hint.style.width = Length.Percent(42);

        bottom = HudStrip();
        var players = Row(bottom);
        players.style.justifyContent = Justify.SpaceBetween;
        for (int i = 0; i < 2; i++)
        {
            var inventoryCard = Paper(players, new Rect(i == 0 ? .015f : .51f, .59f, .47f, .31f), 228, 74);
            var badge = Text(inventoryCard, 17);
            badge.text = $"P{i + 1}";
            badge.style.position = Position.Absolute;
            badge.style.left = 14;
            badge.style.top = 10;
            badge.style.color = new Color(1, .96f, .84f);
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            hands[i] = Text(inventoryCard, 19);
            hands[i].style.position = Position.Absolute;
            hands[i].style.left = 56;
            hands[i].style.top = 18;
            var keys = Text(inventoryCard, 12);
            keys.style.position = Position.Absolute;
            keys.style.left = 56;
            keys.style.top = 40;
            keys.text = i == 0 ? "F 拿放 · 按住 G 加工" : "/ 拿放 · 按住 . 加工";
        }
        stations = new Label[game.layout.Length];
        stationProgress = new VisualElement[stations.Length];
        for (int i = 0; i < stations.Length; i++)
        {
            stations[i] = WorldLabel();
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = 0;
            bar.style.bottom = 0;
            bar.style.height = 4;
            bar.style.backgroundColor = new Color(.27f, .51f, .32f);
            stations[i].Add(bar);
            stationProgress[i] = bar;
        }
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

    VisualElement HudStrip()
    {
        var strip = new VisualElement { pickingMode = PickingMode.Ignore };
        strip.style.position = Position.Absolute;
        root.Add(strip);
        return strip;
    }

    static VisualElement Row(VisualElement parent)
    {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.style.flexDirection = FlexDirection.Row;
        parent.Add(row);
        return row;
    }

    static VisualElement Paper(VisualElement parent, Rect region, float width, float height)
    {
        var card = new VisualElement { pickingMode = PickingMode.Ignore };
        card.style.width = width;
        card.style.height = height;
        card.style.justifyContent = Justify.Center;
        card.style.paddingLeft = 13;
        card.style.paddingRight = 10;
        card.style.color = new Color(.24f, .14f, .08f);
        var art = new Image { sprite = Art2D.Region("hud-paper-v1-atlas", region.x, region.y, region.width, region.height),
            scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
        art.style.position = Position.Absolute;
        art.style.left = art.style.right = art.style.top = art.style.bottom = 0;
        card.Add(art);
        parent.Add(card);
        return card;
    }

    void ApplySafeArea()
    {
        if (Screen.width == 0 || Screen.height == 0)
            return;
        var safe = Screen.safeArea;
        float sx = root.resolvedStyle.width / Screen.width;
        float sy = root.resolvedStyle.height / Screen.height;
        const float inset = 18;
        top.style.left = bottom.style.left = inset + safe.xMin * sx;
        top.style.right = bottom.style.right = inset + (Screen.width - safe.xMax) * sx;
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

    static void Caption(Label label)
    {
        label.style.alignSelf = Align.Center;
        label.style.maxWidth = Length.Percent(100);
        label.style.marginTop = 3;
        label.style.paddingLeft = 11;
        label.style.paddingRight = 11;
        label.style.paddingTop = 3;
        label.style.paddingBottom = 3;
        label.style.backgroundColor = new Color(.17f, .12f, .085f, .92f);
        label.style.borderTopLeftRadius = label.style.borderTopRightRadius =
            label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 6;
    }

    Label WorldLabel()
    {
        var l = Text(root, 14);
        l.style.position = Position.Absolute;
        l.style.width = 86;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.style.whiteSpace = WhiteSpace.Normal;
        Caption(l);
        l.style.paddingLeft = l.style.paddingRight = 2;
        return l;
    }

    public void Refresh(string message)
    {
        var r = game.Rules;
        bool tutorial = r.Phase == RoundPhase.Tutorial;
        bool inWorld = game.Flow == null || game.Flow.WorldActive || game.Flow.LegacySmoke;
        top.style.display = bottom.style.display = inWorld ? DisplayStyle.Flex : DisplayStyle.None;
        if(tutorial || r.Orders<observedOrders){observedOrders=r.Orders;orderNoticeUntil=0;}
        if(!tutorial && r.Orders>observedOrders){observedOrders=r.Orders;orderNoticeUntil=Time.time+4;}
        order.text = tutorial ? "练习做一张交子" : $"第 {r.Orders + 1} 单\n{r.Received}/{r.Required} 张";
        int seconds = Mathf.CeilToInt(r.Remaining);
        timer.text = tutorial ? "不计时" : $"{seconds / 60:00}:{seconds % 60:00}";
        score.text = $"累计 {r.Tickets} 张 · {r.Orders} 单 · {r.Score} 分";
        hint.text = tutorial ? TutorialGuide.Instruction(r) : Time.time < orderNoticeUntil ? OrderFeedback.NewOrder(r) : message;
        hint.style.display = string.IsNullOrWhiteSpace(hint.text) ? DisplayStyle.None : DisplayStyle.Flex;
        for (int i = 0; i < hands.Length; i++)
            hands[i].text = $"持有 · {Graybox.PaperName(r.Hands[i])}";
        for (int i = 0; i < stations.Length; i++)
        {
            var s = r.Slots[i];
            string state = s.Item == null ? (s.Progress > 0 ? "抄纸中" : "空闲") : Graybox.PaperName(s.Item);
            float duration = r.Duration(s);
            if (duration > 0 && s.Progress > 0 && !s.Complete)
                state += $" {Mathf.Clamp01(s.Progress / duration) * 100:F0}%";
            float progress = duration > 0 && !s.Complete ? Mathf.Clamp01(s.Progress / duration) : 0;
            stationProgress[i].style.width = Length.Percent(progress * 100);
            stations[i].style.height = state == "空闲" ? 24 : 44;
            bool guide = tutorial && TutorialGuide.Target(r) == i;
            stations[i].text = (guide ? "▸ " : "") + game.layout[i].label;
            if (state != "空闲")
                stations[i].text += "\n" + state;
            bool p1 = game.Players[0].Target == i;
            bool p2 = game.Players[1].Target == i;
            stations[i].style.backgroundColor = guide ? new Color(1f, .83f, .46f) :
                p1 && p2 ? new Color(.88f, .74f, .43f) :
                p1 ? new Color(.70f, .84f, .69f) :
                p2 ? new Color(.94f, .71f, .58f) : new Color(.98f, .89f, .72f, .96f);
            stations[i].style.color = new Color(.22f, .12f, .065f);
        }

        for (int i = 0; i < 2; i++)
        {
            actors[i].text = $"P{i + 1}";
            actors[i].style.width = 34;
            actors[i].style.backgroundColor = i == 0 ? new Color(.20f, .40f, .28f, .96f) : new Color(.58f, .20f, .14f, .96f);
        }
        bool menu = r.Phase == RoundPhase.Paused || r.Phase == RoundPhase.Results;
        menu &= game.Flow == null || game.Flow.LegacySmoke;
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
        bool visible = game.Flow == null || game.Flow.WorldActive || game.Flow.LegacySmoke;
        top.style.display = bottom.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (var label in stations)
            label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (var label in actors)
            label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        for (int i = 0; i < stations.Length; i++)
            PositionLabel(stations[i], game.ItemAnchors[i].position + Vector3.up * .5f);
        for (int i = 0; i < actors.Length; i++)
            PositionLabel(actors[i], game.Players[i].Root.position + Vector3.up * 1.45f);
    }

    void PositionLabel(Label l, Vector3 world)
    {
        var p = game.View.WorldToViewportPoint(world);
        l.style.left = p.x * root.resolvedStyle.width - l.style.width.value.value * .5f;
        l.style.top = (1 - p.y) * root.resolvedStyle.height - 26;
    }
}
