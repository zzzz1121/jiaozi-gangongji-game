using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Jiaozi;

public sealed class FirstChapterMenu : MonoBehaviour
{
    Graybox game;
    VisualElement root, overlay, card, actions, ticket;
    Label title, description, notice, ticketText;
    Image sealArtwork;
    readonly List<Button> buttons = new List<Button>();
    GameScreen last = (GameScreen)(-1);
    public void Initialize(Graybox owner)
    {
        game = owner;
        root = GetComponent<UIDocument>().rootVisualElement;
        overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(.035f, .075f, .09f, .95f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;
        root.Add(overlay);
        card = new VisualElement();
        card.style.width = new Length(88, LengthUnit.Percent);
        card.style.maxWidth = 920;
        card.style.maxHeight = new Length(96, LengthUnit.Percent);
        card.style.paddingTop = 18;
        card.style.paddingBottom = 18;
        card.style.paddingLeft = 30;
        card.style.paddingRight = 30;
        card.style.backgroundColor = new Color(.10f, .16f, .18f);
        overlay.Add(card);
        var scroll = new ScrollView();
        scroll.style.flexShrink = 1;
        card.Add(scroll);
        title = new Label();
        title.style.fontSize = 30;
        title.style.color = new Color(.97f, .89f, .69f);
        scroll.Add(title);
        description = new Label();
        description.style.whiteSpace = WhiteSpace.Normal;
        description.style.fontSize = 18;
        description.style.marginTop = 12;
        description.style.marginBottom = 12;
        scroll.Add(description);
        ticket = new VisualElement();
        ticket.style.backgroundColor = new Color(.93f, .87f, .7f);
        ticket.style.paddingTop = 10;
        ticket.style.paddingBottom = 10;
        ticket.style.paddingLeft = 16;
        ticket.style.marginBottom = 10;
        scroll.Add(ticket);
        ticketText = new Label();
        ticketText.style.color = new Color(.2f, .12f, .08f);
        ticketText.style.fontSize = 20;
        ticketText.style.whiteSpace = WhiteSpace.Normal;
        ticket.Add(ticketText);
        if(game.chapter.ticketInk!=null){
            var artwork=new VisualElement();artwork.style.width=150;artwork.style.height=200;ticket.Add(artwork);
            var ink=new Image{image=game.chapter.ticketInk,scaleMode=ScaleMode.ScaleToFit};ink.style.position=Position.Absolute;ink.style.left=0;ink.style.right=0;ink.style.top=0;ink.style.bottom=0;artwork.Add(ink);
            if(game.chapter.ticketSeal!=null){sealArtwork=new Image{image=game.chapter.ticketSeal,scaleMode=ScaleMode.ScaleToFit};sealArtwork.style.position=Position.Absolute;sealArtwork.style.left=0;sealArtwork.style.right=0;sealArtwork.style.top=0;sealArtwork.style.bottom=0;artwork.Add(sealArtwork);}
        }
        notice = new Label();
        notice.style.whiteSpace = WhiteSpace.Normal;
        notice.style.color = new Color(1f, 0.72f, 0.45f);
        notice.style.fontSize = 16;
        scroll.Add(notice);
        actions = new VisualElement();
        scroll.Add(actions);
        var keys = new Label("↑↓ 选择 · Enter 确认 · Esc 返回    选角：P1 A/D + F，P2 ←/→ + /");
        keys.style.fontSize = 14;
        keys.style.marginTop = 10;
        card.Add(keys);
        root.RegisterCallback<NavigationSubmitEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
        game.Flow.Changed += Refresh;
        Refresh();
    }

    string Body(GameScreen screen)
    {
        var f = game.Flow;
        var r = game.Rules;
        switch (screen)
        {
            case GameScreen.Title:
                return "完整第一关试玩版\n两人合作，从抄纸到盖印，亲手交出第一张交子。\n当前角色、票面与场景为占位表现，正式美术待接入。";
            case GameScreen.Map:
                var record = f.Progress.records.Find(x => x.id == game.chapter.levelId);
                return $"第一关已开放 · 最高星级 {record?.stars ?? 0}/3 · 最高分 {record?.score ?? 0}\n第二关 院里院外 / 第三关 赶工时刻 / Boss 关：后续开发，当前不可进入。";
            case GameScreen.Selection:
                return $"P1  {game.chapter.artisans[f.Selected[0]].displayName}  {(f.Ready[0] ? "已确认" : "待确认")}\nP2  {game.chapter.artisans[f.Selected[1]].displayName}  {(f.Ready[1] ? "已确认" : "待确认")}\n两人选不同工匠并分别确认后进入。再次确认可取消。\n首批两名角色不带技能，基础操作完全相同。";
            case GameScreen.Pause:
                return "回合、角色、干燥与加工均已暂停。返回地图或重新选角会丢弃未结算的本轮进度。";
            case GameScreen.Results:
                return $"交付 {r.Tickets} 张 · 完成 {r.Orders} 单 · 得分 {r.Score} · 星级 {f.StarCount}/3\n" + (f.NewCollection ? "新收藏已解锁！" : r.Tickets > 0 ? "本组合已收藏，最佳成绩已更新。" : "本轮未交付，未解锁收藏。") + "\n下一关尚在开发，本版可重玩第一关。";
            case GameScreen.Gallery:
                return Gallery();
            case GameScreen.Settings:
                return "用 ↑↓ 选择项目，←→ 调整；点按钮也可切换。\n保存后写入本地；Esc 或取消会恢复原设置。音乐与音效目前为程序占位声音。";
            case GameScreen.Help:
                return "① 抄纸槽按住加工 2 秒，再拿起湿纸。\n② 放纸架等 4 秒，再取干纸。\n③ 放印刷台，按住加工 2 秒，再拿起黑票。\n④ 放盖印台，按住加工 1 秒，再拿起朱票。\n⑤ 手持朱票到交付窗口点按拿放。\n\nP1：WASD / F 拿放 / G 按住加工\nP2：方向键 / 主键盘斜杠拿放 / 句点按住加工\n先面向高亮工位。加工后还要再拿起来。双方可以做所有工序。";
            default:
                return "";
        }
    }

    string Gallery()
    {
        var ids = new[]
        {
            "zhituan",
            "yintao",
            "paopao",
            "dijiao",
            "modun",
            "hongli"
        };
        var names = new[]
        {
            "纸团",
            "印桃",
            "泡泡",
            "递蕉",
            "墨墩",
            "烘栗"
        };
        var b = new StringBuilder($"已收藏 {game.Flow.Progress.collections.Count}/15\n");
        for (int i = 0; i < 6; i++)
            for (int j = i + 1; j < 6; j++)
            {
                bool owned = game.Flow.Progress.collections.Contains(GameProgress.Combination(ids[i], ids[j]));
                b.Append(owned ? "已解锁  " : "未解锁  ").Append(names[i]).Append(" ＋ ").Append(names[j]).Append('\n');
            }

        return b.ToString();
    }

    public void Refresh()
    {
        var f = game.Flow;
        var screen = f.Screen;
        overlay.style.display = f.LegacySmoke || screen == GameScreen.Playing ? DisplayStyle.None : DisplayStyle.Flex;
        if (screen == GameScreen.Playing || f.LegacySmoke)
            return;
        title.text = screen == GameScreen.Title ? "交子赶工记" : screen == GameScreen.Map ? "作坊地图" : screen == GameScreen.Selection ? "双人工匠选择" : screen == GameScreen.Pause ? "作坊已暂停" : screen == GameScreen.Results ? "本轮结算" : screen == GameScreen.Gallery ? "组合交子图鉴" : screen == GameScreen.Settings ? "设置" : "操作说明";
        description.text = Body(screen);
        notice.text = f.Notice;
        bool showTicket = screen == GameScreen.Selection || screen == GameScreen.Results || screen == GameScreen.Gallery;
        ticket.style.display = showTicket ? DisplayStyle.Flex : DisplayStyle.None;
        ticketText.text = "交子票面占位预览\n" + f.Pair + "\n" + (screen == GameScreen.Selection ? "黑墨画像 → 加盖朱印" : f.Selected[0] != f.Selected[1] && f.Progress.collections.Contains(f.CollectionKey) ? "朱印已落 · 已收藏" : "尚未收藏，正式轮次交付后结算解锁");
        if (game.chapter.ticketPaper != null)
            ticket.style.backgroundImage = game.chapter.ticketPaper;
        if(sealArtwork!=null)sealArtwork.style.display=screen!=GameScreen.Selection&&f.Selected[0]!=f.Selected[1]&&f.Progress.collections.Contains(f.CollectionKey)?DisplayStyle.Flex:DisplayStyle.None;
        actions.Clear();
        buttons.Clear();
        if (screen == GameScreen.Selection)
        {
            Add("P1 切换工匠", () => f.Select(0, 1));
            Add(f.Ready[0] ? "P1 取消确认" : "P1 确认", () => f.ConfirmPlayer(0));
            Add("P2 切换工匠", () => f.Select(1, 1));
            Add(f.Ready[1] ? "P2 取消确认" : "P2 确认", () => f.ConfirmPlayer(1));
            Add("返回地图", () => f.Show(GameScreen.Map));
        }
        else
        {
            var items = f.MenuItems();
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                if(screen==GameScreen.Settings&&index<5)AddSetting(items[i],index);
                else Add(items[i], () => f.Activate(index));
            }
        }

        for (int i = 0; i < buttons.Count; i++)
            buttons[i].style.backgroundColor = i == f.Focus ? new Color(.15f, .45f, .45f) : new Color(.2f, .25f, .27f);
        if (last != screen)
        {
            last = screen;
            card.Focus();
        }
    }

    void Add(string text, System.Action action)
    {
        var b = new Button(action)
        {
            text = text,
            focusable = false
        };
        b.style.height = 38;
        b.style.marginTop = 5;
        b.style.fontSize = 18;
        b.style.color = Color.white;
        actions.Add(b);
        buttons.Add(b);
    }

    void AddSetting(string text,int index)
    {
        var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.marginTop=5;row.style.height=38;actions.Add(row);
        var minus=new Button(()=>game.Flow.AdjustSetting(index,-1)){text="−",focusable=false};minus.style.width=46;minus.style.fontSize=20;row.Add(minus);
        var label=new Button(()=>game.Flow.AdjustSetting(index,1)){text=text,focusable=false};label.style.flexGrow=1;label.style.fontSize=18;label.style.color=Color.white;row.Add(label);buttons.Add(label);
        var plus=new Button(()=>game.Flow.AdjustSetting(index,1)){text="＋",focusable=false};plus.style.width=46;plus.style.fontSize=20;row.Add(plus);
    }

    void OnDestroy()
    {
        if (game?.Flow != null)
            game.Flow.Changed -= Refresh;
    }
}
