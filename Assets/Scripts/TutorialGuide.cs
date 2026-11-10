using Jiaozi;

public static class TutorialGuide
{
    public static int Target(WorkshopRules r)
    {
        if (r.TutorialDelivered)
            return -1;
        if (r.Hands[0]?.Kind == PaperKind.Stamped || r.Hands[1]?.Kind == PaperKind.Stamped)
            return 6;
        if (r.Slots[4].Item != null)
            return 4;
        if (r.Hands[0]?.Kind == PaperKind.Printed || r.Hands[1]?.Kind == PaperKind.Printed)
            return 4;
        if (r.Slots[3].Item != null)
            return 3;
        if (r.Hands[0]?.Kind == PaperKind.Dry || r.Hands[1]?.Kind == PaperKind.Dry)
            return 3;
        for (int i = 1; i <= 2; i++)
            if (r.Slots[i].Item != null)
                return i;
        if (r.Hands[0]?.Kind == PaperKind.Wet || r.Hands[1]?.Kind == PaperKind.Wet)
            return r.Slots[1].Item == null ? 1 : 2;
        if (r.Slots[5].Item != null)
            return 5;
        return 0;
    }

    public static string Instruction(WorkshopRules r)
    {
        if (r.TutorialDelivered)
            return "教学完成！按 Enter 清空教学物料，开始正式三分钟。";
        int target = Target(r);
        var s = r.Slots[target];
        string keys = "拿放点一下 F 或 /，加工按住 G 或 .";
        if (target == 6)
            return "第 6 步  手持朱印交子，到右下交付窗口点按拿放。";
        if (target == 0)
            return s.Complete ? "第 1 步  湿纸做好了！空手点按拿放取走。" : "第 1 步  靠近左上抄纸槽，面向高亮工位，按住加工 2 秒。";
        if (target == 1 || target == 2)
            return s.Item == null ? "第 2 步  手持湿纸，到晾纸架点按拿放；它会自动晾干。" : s.Item.Kind == PaperKind.Wet ? "第 2 步  正在自动晾干，等 4 秒；等待时可准备下一张。" : "第 3 步  干纸好了！空手点按拿放取走，再送右侧印刷台。";
        if (target == 3)
            return s.Item == null ? "第 3 步  手持干纸，到印刷台点按拿放。" : s.Complete ? "第 4 步  黑票做好了！点按拿放取走，送到盖印台。" : "第 4 步  纸已放好，在印刷台按住加工 2 秒。";
        if (target == 4)
            return s.Item == null ? "第 5 步  手持黑票，到盖印台点按拿放。" : s.Complete ? "第 5 步  朱印盖好了！点按拿放取走，送右下窗口。" : "第 5 步  在盖印台按住加工 1 秒。完成后还要拿起。";
        return "空手到共享台拿回纸张，再继续下一道工序。 " + keys;
    }
}
