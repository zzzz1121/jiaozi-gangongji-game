using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The artwork and hit areas share one aspect-preserving canvas.
public sealed class TitleScreenView
{
    readonly VisualElement host, canvas;
    readonly GameFlow flow;
    readonly Dictionary<int, Button> buttons = new Dictionary<int, Button>();
    readonly Dictionary<int, Label> labels = new Dictionary<int, Label>();
    readonly Dictionary<int, float> lifts = new Dictionary<int, float>();
    readonly HashSet<int> pressed = new HashSet<int>();
    readonly Font brushFont;
    double previousTime;
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
        canvas.style.backgroundImage = Resources.Load<Texture2D>("Art2D/title-backdrop-layered");
        canvas.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
        host.Add(canvas);
        brushFont = Font.CreateDynamicFontFromOSFont(new[] { "STXingkai", "华文行楷", "KaiTi", "Microsoft YaHei" }, 64);
        canvas.style.unityFontDefinition = FontDefinition.FromFont(brushFont);
        canvas.style.unityFont = brushFont;
        Add(0, "开始游戏", 0, 43.5f, 34.5f, 18, 56);
        Add(1, "收藏图鉴", 4.5f, 59.8f, 27, 12.6f, 47);
        Add(2, "设置", 4.8f, 71.6f, 25.4f, 13.5f, 48);
        Add(4, "退出", .5f, 89, 13.8f, 8.5f, 31);
        Add(3, "操作说明", 18.5f, 84.1f, 11, 4, 24);
        hint = new Label("↑↓ 选择 · Enter 确认");
        hint.pickingMode = PickingMode.Ignore;
        hint.style.position = Position.Absolute;
        hint.style.left = new Length(1.8f, LengthUnit.Percent);
        hint.style.bottom = new Length(.6f, LengthUnit.Percent);
        hint.style.color = new Color(.98f, .94f, .8f);
        canvas.Add(hint);
        host.RegisterCallback<GeometryChangedEvent>(_ => Resize());
        previousTime = Time.realtimeSinceStartupAsDouble;
        host.schedule.Execute(Animate).Every(16);
    }

    void Add(int index, string text, float x, float y, float w, float h, float fontSize)
    {
        var button = new Button(() => flow.Activate(index)) { focusable = false, name = "title-action-" + index };
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
        if (index != 3)
        {
            var image = new Image { sprite = Art2D.Region("title-tags-layered", x/100, y/100, w/100, h/100), scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute;
            image.style.left = image.style.right = image.style.top = image.style.bottom = 0;
            button.Add(image);
        }
        var label = new Label(text) { name = "title-label-" + index, pickingMode = PickingMode.Ignore };
        label.style.position = Position.Absolute;
        label.style.left = new Length(index == 0 ? 23 : index == 1 ? 22 : index == 2 ? 33 : 5, LengthUnit.Percent);
        label.style.right = new Length(index == 3 ? 0 : 7, LengthUnit.Percent);
        label.style.top = new Length(index == 3 ? 0 : 10, LengthUnit.Percent);
        label.style.bottom = new Length(index == 3 ? 0 : 22, LengthUnit.Percent);
        label.style.color = new Color(.19f, .12f, .065f);
        label.style.unityFontDefinition = FontDefinition.FromFont(brushFont);
        label.style.unityFont = brushFont;
        label.style.unityFontStyleAndWeight = FontStyle.Normal;
        label.style.unityTextOutlineWidth = 0;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.Add(label);
        // Transform-induced enter events must not steal keyboard selection.
        button.RegisterCallback<PointerMoveEvent>(e => { if (e.deltaPosition.sqrMagnitude > .01f && flow.Focus != index) flow.FocusMenuItem(index); });
        button.RegisterCallback<PointerDownEvent>(_ => pressed.Add(index), TrickleDown.TrickleDown);
        button.RegisterCallback<PointerUpEvent>(_ => pressed.Remove(index), TrickleDown.TrickleDown);
        button.RegisterCallback<PointerCaptureOutEvent>(_ => pressed.Remove(index));
        canvas.Add(button);
        buttons.Add(index, button);
        labels.Add(index, label);
        lifts.Add(index, 0);
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
        foreach (var item in labels) item.Value.style.fontSize = sizes[item.Key] * scale;
        hint.style.fontSize = 19 * scale;
    }

    void Animate()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        float dt = Mathf.Min(.05f, (float)(now - previousTime));
        previousTime = now;
        if (host.resolvedStyle.display == DisplayStyle.None) return;
        foreach (var item in buttons)
        {
            float target = pressed.Contains(item.Key) ? -.5f : item.Key == flow.Focus ? 1 : 0;
            float lift = Mathf.Lerp(lifts[item.Key], target, 1 - Mathf.Exp(-18 * dt));
            lifts[item.Key] = lift;
            item.Value.style.scale = new Scale(Vector3.one * (1 + .035f * lift));
            item.Value.style.translate = new Translate(5 * scale * lift, -5 * scale * lift);
        }
    }

    public void Refresh(bool visible)
    {
        host.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible) Resize();
        else pressed.Clear();
    }
}
