using NUnit.Framework;
using Jiaozi;

public class WorkshopRulesTests
{
    WorkshopRules r;
    [SetUp]
    public void Setup()
    {
        r = new WorkshopRules();
        r.Reset(false);
    }

    void Process(int p, int s, float dt)
    {
        r.BeginFrame(0);
        Assert.IsTrue(r.Process(p, s, dt, out var why), why);
    }

    void Take(int p, int s)
    {
        Assert.IsTrue(r.Interact(p, s, out var why), why);
        Assert.IsTrue(r.OwnershipValid());
    }

    void Wet(int p = 0)
    {
        Process(p, 0, 2);
        Take(p, 0);
    }

    void Stamped()
    {
        Wet();
        Take(0, 1);
        r.BeginFrame(4);
        Take(0, 1);
        Take(0, 3);
        Process(0, 3, 2);
        Take(0, 3);
        Take(0, 4);
        Process(0, 4, 1);
        Take(0, 4);
    }

    [Test]
    public void FullChainAndScoringExample()
    {
        for (int i = 0; i < 3; i++)
        {
            Stamped();
            Take(0, 6);
        }

        Assert.AreEqual(3, r.Tickets);
        Assert.AreEqual(2, r.Orders);
        Assert.AreEqual(50, r.Score);
        Assert.AreEqual(1, r.Stars);
        Assert.AreEqual(1, r.Received);
    }

    [Test]
    public void UniquePaperCanOnlyBeTakenOnce()
    {
        Process(0, 0, 2);
        Take(0, 0);
        Assert.IsFalse(r.Interact(1, 0, out _));
        Assert.IsNull(r.Hands[1]);
        Assert.AreEqual(1, r.Papers.Count);
    }

    [Test]
    public void BothDryingSlotsFullKeepThirdInHand()
    {
        Wet();
        Take(0, 1);
        Wet();
        Take(0, 2);
        Wet();
        var item = r.Hands[0];
        Assert.IsFalse(r.Interact(0, 1, out _));
        Assert.AreSame(item, r.Hands[0]);
        Assert.IsTrue(r.OwnershipValid());
    }

    [Test]
    public void DryingSlotsHaveIndependentTimers()
    {
        Wet();
        Take(0, 1);
        r.BeginFrame(2);
        Wet();
        Take(0, 2);
        r.BeginFrame(2);
        Assert.AreEqual(PaperKind.Dry, r.Slots[1].Item.Kind);
        Assert.AreEqual(PaperKind.Wet, r.Slots[2].Item.Kind);
    }

    [Test]
    public void WrongStationKeepsOwnershipAndScore()
    {
        Wet();
        var item = r.Hands[0];
        Assert.IsFalse(r.Interact(0, 3, out _));
        Assert.IsFalse(r.Interact(0, 6, out _));
        Assert.AreSame(item, r.Hands[0]);
        Assert.AreEqual(0, r.Score);
    }

    [Test]
    public void InterruptedProcessingCanBeContinuedButNotDoubled()
    {
        r.BeginFrame(0);
        Assert.IsTrue(r.Process(0, 0, .75f, out _));
        Assert.IsFalse(r.Process(1, 0, .75f, out _));
        Assert.AreEqual(.75f, r.Slots[0].Progress);
        r.BeginFrame(0);
        Assert.IsTrue(r.Process(1, 0, 1.25f, out _));
        Assert.IsTrue(r.Slots[0].Complete);
    }

    [Test]
    public void PauseFreezesDryingProcessingAndRound()
    {
        Wet();
        Take(0, 1);
        r.BeginFrame(1);
        r.TogglePause();
        float time = r.Remaining, progress = r.Slots[1].Progress;
        r.BeginFrame(10);
        Assert.AreEqual(time, r.Remaining);
        Assert.AreEqual(progress, r.Slots[1].Progress);
        Assert.IsFalse(r.Process(0, 0, 10, out _));
        Assert.IsFalse(r.Interact(1, 1, out _));
        r.TogglePause();
        r.BeginFrame(3);
        Assert.AreEqual(PaperKind.Dry, r.Slots[1].Item.Kind);
    }

    [Test]
    public void HandoffIsAtomicAndRecipientMustBeEmpty()
    {
        Wet();
        var item = r.Hands[0];
        Assert.IsTrue(r.Handoff(0, 1, out _));
        Assert.AreSame(item, r.Hands[1]);
        Assert.IsNull(r.Hands[0]);
        Wet();
        Assert.IsFalse(r.Handoff(0, 1, out _));
        Assert.IsTrue(r.OwnershipValid());
    }

    [Test]
    public void ZeroTimeRejectsLateDeliveryAndOnlyEndsOnce()
    {
        Stamped();
        r.BeginFrame(180);
        Assert.AreEqual(RoundPhase.Results, r.Phase);
        Assert.IsFalse(r.Interact(0, 6, out _));
        r.BeginFrame(1);
        Assert.AreEqual(1, r.ResultsCount);
        Assert.AreEqual(0, r.Tickets);
        Assert.NotNull(r.Hands[0]);
    }

    [Test]
    public void ResetClearsEveryState()
    {
        Stamped();
        Take(0, 6);
        Wet();
        Take(0, 1);
        r.TogglePause();
        r.Reset(true);
        Assert.AreEqual(0, r.Score);
        Assert.AreEqual(0, r.Papers.Count);
        Assert.AreEqual(180, r.Remaining);
        foreach (var s in r.Slots)
        {
            Assert.IsNull(s.Item);
            Assert.AreEqual(0, s.Progress);
        }

        Assert.IsNull(r.Hands[0]);
        Assert.IsNull(r.Hands[1]);
    }

    [Test]
    public void TutorialDoesNotCountAndTransitionClearsObjects()
    {
        r.Reset(true);
        r.BeginFrame(1000);
        Assert.AreEqual(180, r.Remaining);
        Stamped();
        Take(0, 6);
        Assert.IsTrue(r.TutorialDelivered);
        Assert.AreEqual(0, r.Score);
        r.Reset(false);
        Assert.IsFalse(r.TutorialDelivered);
        Assert.AreEqual(0, r.Papers.Count);
        Assert.AreEqual(180, r.Remaining);
    }

    [Test]
    public void ConsumedPaperCannotBeDeliveredTwice()
    {
        Stamped();
        var paper = r.Hands[0];
        Take(0, 6);
        Assert.IsTrue(paper.Consumed);
        Assert.IsFalse(r.Interact(0, 6, out _));
        Assert.AreEqual(20, r.Score);
    }
}
