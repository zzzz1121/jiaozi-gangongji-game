using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The artwork and hit areas share one aspect-preserving canvas.
public sealed class TitleScreenView
{
    readonly VisualElement host, canvas;
    readonly GameFlow flow;
    readonly Dictionary<int, Button> buttons = new Dictionary<int, Button>();
    readonly Dictionary<int, float> sizes = new Dictionary<int, float>();
    readonly Label hint;
    float scale = 1;

    public TitleScreenView(VisualElement parent, GameFlow owner)
    {
        flow = owner;
        host = new VisualElement { name = "title-screen-v1" };
        host.style.position = Position.Absolute;
        host.style.left = host.style.right = host.style.top = host.style.bottom = 0;
        host.style.backgroundColor = new Color(.12f, .16f, .15f);
        parent.Add(host);
        canvas = new VisualElement { name = "title-art-canvas" };
        canvas.style.position = Position.Absolute;
        canvas.style.backgroundImage = Resources.Load<Texture2D>("Art2D/title-workshop-v1");
        canvas.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
        host.Add(canvas);
        var font = Font.CreateDynamicFontFromOSFont(new[] { "KaiTi", "STKaiti", "Microsoft YaHei" }, 48);
        canvas.style.unityFont = font;
        Add(0, "开始游戏", 8, 46.6f, 22, 9, 52);
        Add(1, "收藏图鉴", 10.8f, 61.9f, 17.6f, 7, 43);
        Add(2, "设置", 13.4f, 73.8f, 14, 7, 43);
        Add(4, "退出", 2.3f, 90.6f, 10, 5, 27);
        Add(3, "操作说明", 18.5f, 84.1f, 11, 4, 22);
        hint = new Label("↑↓ 选择 · Enter 确认");
        hint.pickingMode = PickingMode.Ignore;
        hint.style.position = Position.Absolute;
        hint.style.left = new Length(1.8f, LengthUnit.Percent);
        hint.style.bottom = new Length(.6f, LengthUnit.Percent);
        hint.style.color = new Color(.98f, .94f, .8f);
        canvas.Add(hint);
        host.RegisterCallback<GeometryChangedEvent>(_ => Resize());
    }

    void Add(int index, string text, float x, float y, float w, float h, float fontSize)
    {
        var button = new Button(() => flow.Activate(index)) { text = text, focusable = false, name = "title-action-" + index };
        button.style.position = Position.Absolute;
        button.style.left = new Length(x, LengthUnit.Percent);
        button.style.top = new Length(y, LengthUnit.Percent);
        button.style.width = new Length(w, LengthUnit.Percent);
        button.style.height = new Length(h, LengthUnit.Percent);
        button.style.marginLeft = button.style.marginRight = button.style.marginTop = button.style.marginBottom = 0;
        button.style.paddingLeft = button.style.paddingRight = button.style.paddingTop = button.style.paddingBottom = 0;
        button.style.backgroundColor = Color.clear;
        button.style.backgroundImage = StyleKeyword.None;
        button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = 0;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.RegisterCallback<PointerEnterEvent>(_ => flow.FocusMenuItem(index));
        canvas.Add(button);
        buttons.Add(index, button);
        sizes.Add(index, fontSize);
    }

    void Resize()
    {
        float width = host.contentRect.width, height = host.contentRect.height;
        if (width <= 0 || height <= 0) return;
        scale = Mathf.Min(width / 1600f, height / 1000f);
        canvas.style.width = 1600 * scale;
        canvas.style.height = 1000 * scale;
        canvas.style.left = (width - 1600 * scale) / 2;
        canvas.style.top = (height - 1000 * scale) / 2;
        foreach (var item in buttons) item.Value.style.fontSize = sizes[item.Key] * scale;
        hint.style.fontSize = 19 * scale;
        Paint();
    }

    void Paint()
    {
        foreach (var item in buttons)
        {
            bool selected = item.Key == flow.Focus;
            item.Value.style.color = selected ? new Color(.65f, .12f, .055f) : new Color(.19f, .12f, .065f);
            item.Value.style.unityTextOutlineColor = selected ? new Color(1, .84f, .36f, .95f) : Color.clear;
            item.Value.style.unityTextOutlineWidth = selected ? 1.5f * scale : 0;
            item.Value.text = (selected ? "› " : "") + (item.Key == 0 ? "开始游戏" : item.Key == 1 ? "收藏图鉴" : item.Key == 2 ? "设置" : item.Key == 3 ? "操作说明" : "退出");
        }
    }

    public void Refresh(bool visible)
    {
        host.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible) { Resize(); Paint(); }
    }
}
