using System;
using System.IO;
using NUnit.Framework;
using Jiaozi;

public sealed class ProgressTests
{
    string directory;
    ProgressStore store;
    [SetUp]
    public void Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "jiaozi-progress-" + Guid.NewGuid().ToString("N"));
        store = new ProgressStore(Path.Combine(directory, "progress.json"));
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Test]
    public void CombinationIsOrderIndependent()
    {
        Assert.AreEqual(GameProgress.Combination("zhituan", "yintao"), GameProgress.Combination("yintao", "zhituan"));
        Assert.Throws<ArgumentException>(() => GameProgress.Combination("a", "a"));
    }

    [Test]
    public void ZeroDeliveryDoesNotUnlock()
    {
        var p = new GameProgress();
        Assert.IsFalse(p.Complete("first", "zhituan", "yintao", 0, 0, 0, false));
        Assert.AreEqual(0, p.collections.Count);
    }

    [Test]
    public void SwappingPlayersDoesNotDuplicateCollection()
    {
        var p = new GameProgress();
        Assert.IsTrue(p.Complete("first", "zhituan", "yintao", 1, 20, 0, true));
        Assert.IsFalse(p.Complete("first", "yintao", "zhituan", 1, 20, 0, false));
        Assert.AreEqual(1, p.collections.Count);
        Assert.IsTrue(p.tutorialComplete);
    }

    [Test]
    public void BestStarsAndScoreUpdateIndependently()
    {
        var p = new GameProgress();
        p.Complete("first", "zhituan", "yintao", 3, 100, 1, false);
        p.Complete("first", "zhituan", "yintao", 3, 50, 2, false);
        Assert.AreEqual(100, p.records[0].score);
        Assert.AreEqual(2, p.records[0].stars);
    }

    [Test]
    public void FreshLoadHasDefaults()
    {
        var p = store.Load();
        Assert.AreEqual(0, p.collections.Count);
        Assert.IsFalse(p.tutorialComplete);
        Assert.AreEqual("", store.Notice);
    }

    [Test]
    public void SaveAndLoadPreserveSettingsAndCollection()
    {
        var p = new GameProgress();
        p.settings.master = .3f;
        p.settings.resolution = 2;
        p.Complete("first", "zhituan", "yintao", 1, 20, 0, true);
        Assert.IsTrue(store.Save(p));
        var loaded = store.Load();
        Assert.AreEqual(.3f, loaded.settings.master);
        Assert.AreEqual(2, loaded.settings.resolution);
        Assert.AreEqual(1, loaded.collections.Count);
        Assert.IsTrue(loaded.tutorialComplete);
    }

    [Test]
    public void CorruptMainLoadsBackup()
    {
        var p = new GameProgress();
        store.Save(p);
        p.Complete("first", "zhituan", "yintao", 1, 20, 0, true);
        store.Save(p);
        File.WriteAllText(store.PathName, "broken");
        var loaded = store.Load();
        Assert.AreEqual(0, loaded.collections.Count);
        Assert.IsNotEmpty(store.Notice);
    }

    [Test]
    public void CorruptFilesAreRetainedAndCanBeReplacedSafely()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(store.PathName, "broken-main");
        File.WriteAllText(store.PathName + ".bak", "broken-backup");
        var p = store.Load();
        Assert.IsNotEmpty(store.Notice);
        Assert.AreEqual("broken-main", File.ReadAllText(store.PathName));
        Assert.IsTrue(store.Save(p));
        Assert.AreEqual("broken-main", File.ReadAllText(store.PathName + ".corrupt"));
        Assert.AreEqual("broken-backup", File.ReadAllText(store.PathName + ".bak"));
    }

    [Test]
    public void IncompleteJsonIsNotAcceptedAsValidSave()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(store.PathName, "{}");
        store.Load();
        Assert.IsNotEmpty(store.Notice);
    }

    [Test]
    public void TutorialGuideFollowsPhysicalPaper()
    {
        var r = new WorkshopRules();
        Assert.AreEqual(0, TutorialGuide.Target(r));
        r.Process(0, 0, 2, out _);
        r.Interact(0, 0, out _);
        Assert.AreEqual(1, TutorialGuide.Target(r));
        r.Interact(0, 1, out _);
        Assert.AreEqual(1, TutorialGuide.Target(r));
        r.BeginFrame(4);
        Assert.IsTrue(TutorialGuide.Instruction(r).Contains("干纸好了"));
    }
}
