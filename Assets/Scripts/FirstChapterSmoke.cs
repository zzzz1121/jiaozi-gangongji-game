using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Jiaozi;

public sealed class FirstChapterSmoke : MonoBehaviour
{
    Graybox game;
    GameFlow flow;
    bool failed;
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var a = Environment.GetCommandLineArgs();
        if (Array.IndexOf(a, "--jiaozi-flow-smoke") >= 0 || Array.IndexOf(a, "--jiaozi-flow-reload") >= 0)
            new GameObject("First chapter smoke").AddComponent<FirstChapterSmoke>();
    }

    void Check(bool ok, string message)
    {
        Debug.Log("FLOW " + (ok ? "PASS " : "FAIL ") + message);
        failed |= !ok;
    }

    IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;
    }

    IEnumerator Shot(string name, int w = 1280, int h = 800)
    {
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(.5f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
        yield return new WaitForSecondsRealtime(.25f);
    }

    void MakeTicket()
    {
        var r = game.Rules;
        r.BeginFrame(0);
        r.Process(0, 0, 2, out _);
        r.Interact(0, 0, out _);
        r.Interact(0, 1, out _);
        r.BeginFrame(4);
        r.Interact(0, 1, out _);
        r.Interact(0, 3, out _);
        r.Process(0, 3, 2, out _);
        r.Interact(0, 3, out _);
        r.Interact(0, 4, out _);
        r.Process(0, 4, 1, out _);
        r.Interact(0, 4, out _);
        r.Interact(0, 6, out _);
    }

    IEnumerator Start()
    {
        while (!UnityEngine.Rendering.SplashScreen.isFinished)
            yield return null;
        for (int i = 0; i < 30; i++)
            yield return null;
        game = FindFirstObjectByType<Graybox>();
        flow = game.Flow;
        output = Path.Combine(Application.persistentDataPath, "FirstChapterQA");
        Directory.CreateDirectory(output);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-flow-reload") >= 0)
        {
            Check(flow.Screen == GameScreen.Title, "reload starts at title");
            Check(flow.Progress.collections.Count == 1, "collection persists after process restart");
            Check(flow.Progress.records.Count == 1 && flow.Progress.records[0].score == 50, "best score persists after process restart");
            Check(Mathf.Approximately(flow.Progress.settings.master, .4f), "settings persist after process restart");
            Finish();
            yield break;
        }

        flow.Progress.collections.Clear();
        flow.Progress.records.Clear();
        flow.Progress.tutorialComplete = false;
        flow.Store.Save(flow.Progress);
        Check(flow.Screen == GameScreen.Title, "title boot");
        yield return Shot("title");
        yield return Shot("title-720", 1280, 720);
        yield return Shot("title-1080", 1920, 1080);
        yield return Press(Key.DownArrow);
        Check(flow.Focus == 1, "title keyboard focus moves to collection");
        yield return new WaitForSecondsRealtime(.3f);
        var titleRoot = game.GetComponent<UIDocument>().rootVisualElement;
        var paperAction = titleRoot.Q<Button>("title-action-1");
        var paperLabel = titleRoot.Q<Label>("title-label-1");
        Check(paperAction.style.scale.value.value.x > 1.025f, "whole paper tag scales on selection");
        Check(paperLabel.text == "收藏图鉴" && paperLabel.style.unityTextOutlineWidth.value == 0, "lettering stays plain without arrow or outline");
        yield return Shot("title-paper-focused");
        using (var down = PointerDownEvent.GetPooled()) { down.target = paperAction; paperAction.SendEvent(down); }
        yield return new WaitForSecondsRealtime(.3f);
        Check(paperAction.style.scale.value.value.x < 1, "whole paper tag compresses on press");
        using (var up = PointerUpEvent.GetPooled()) { up.target = paperAction; paperAction.SendEvent(up); }
        yield return new WaitForSecondsRealtime(.3f);
        Check(paperAction.style.scale.value.value.x > 1.025f, "paper tag rebounds after release");
        yield return Press(Key.Enter);
        Check(flow.Screen == GameScreen.Gallery, "title collection action");
        yield return Press(Key.Escape);
        flow.FocusMenuItem(2);
        flow.Activate(flow.Focus);
        Check(flow.Screen == GameScreen.Settings, "title settings action");
        yield return Press(Key.Escape);
        flow.FocusMenuItem(3);
        flow.Activate(flow.Focus);
        Check(flow.Screen == GameScreen.Help, "title help action");
        yield return Press(Key.Escape);
        Check(flow.Screen == GameScreen.Title && flow.Focus == 0, "return restores title focus");
        var before = game.Players[0].Root.position;
        yield return Press(Key.W);
        Check(game.Players[0].Root.position == before, "menus block world input");
        yield return Press(Key.Enter);
        Check(flow.Screen == GameScreen.Map, "title to map");
        yield return Shot("map");
        yield return Press(Key.Enter);
        Check(flow.Screen == GameScreen.Selection, "map to selection");
        yield return Press(Key.D);
        yield return Press(Key.F);
        Check(!flow.Ready[0] && flow.Selected[0] == flow.Selected[1], "duplicate character rejected");
        yield return Press(Key.Escape);
        Check(flow.Screen == GameScreen.Map, "back from conflicting selection");
        yield return Press(Key.Enter);
        yield return Press(Key.D);
        yield return Press(Key.F);
        yield return Press(Key.Slash);
        Check(flow.Screen == GameScreen.Playing && game.Rules.Phase == RoundPhase.Tutorial, "both players confirm to tutorial");
        yield return Shot("tutorial-720", 1280, 720);
        MakeTicket();
        Check(game.Rules.TutorialDelivered, "tutorial complete");
        Check(flow.Progress.collections.Count == 0, "tutorial does not unlock");
        yield return Press(Key.Enter);
        Check(game.Rules.Phase == RoundPhase.Running && game.Rules.Papers.Count == 0, "formal start clears tutorial");
        yield return Press(Key.Escape);
        Check(flow.Screen == GameScreen.Pause, "pause screen");
        float time = game.Rules.Remaining;
        yield return new WaitForSecondsRealtime(.3f);
        Check(time == game.Rules.Remaining, "pause freezes time");
        yield return Press(Key.Enter);
        Check(flow.Screen == GameScreen.Playing, "Enter resumes once");
        for (int i = 0; i < 3; i++)
            MakeTicket();
        game.Rules.BeginFrame(180);
        yield return null;
        Check(flow.Screen == GameScreen.Results && game.Rules.Score == 50, "results from complete round");
        Check(flow.Progress.collections.Count == 1 && flow.Progress.tutorialComplete, "results save collection and tutorial");
        yield return Shot("results", 1920, 1080);
        game.Rules.BeginFrame(1);
        Check(flow.Progress.collections.Count == 1 && flow.Progress.records.Count == 1, "results commit once");
        flow.Activate(1);
        Check(flow.Screen == GameScreen.Selection, "change roles from results");
        flow.Select(0, 1);
        flow.Select(1, 1);
        flow.ConfirmPlayer(0);
        flow.ConfirmPlayer(1);
        Check(game.Rules.Phase == RoundPhase.Running, "learned tutorial skipped on reentry");
        MakeTicket();
        game.Rules.BeginFrame(180);
        yield return null;
        Check(flow.Progress.collections.Count == 1 && flow.Progress.records[0].score == 50, "swapped combination deduplicated and best retained");
        flow.Activate(2);
        Check(flow.Screen == GameScreen.Gallery, "results to gallery");
        yield return Shot("gallery");
        flow.Activate(0);
        flow.Activate(2);
        float original = flow.Progress.settings.master;
        flow.AdjustSetting(0, -1);
        flow.Back();
        Check(Mathf.Approximately(flow.Progress.settings.master, original), "settings cancel restores original");
        flow.OpenSettings();
        flow.SettingsDraft.master = .4f;
        flow.AdjustSetting(2, -1);
        flow.SaveSettings();
        Check(Mathf.Approximately(flow.Store.Load().settings.master, .4f), "settings saved");
        flow.OpenSettings();
        yield return Shot("settings-720", 1280, 720);
        flow.Back();
        flow.OpenSelection();
        yield return Shot("selection");
        flow.BeginPlay(false);
        yield return Press(Key.Escape);
        flow.Activate(1);
        Check(game.Rules.Papers.Count == 0 && game.Rules.Score == 0 && flow.Screen == GameScreen.Playing, "pause restart resets round");
        yield return Press(Key.Escape);
        flow.Activate(4);
        Check(flow.Screen == GameScreen.Map, "pause return to map");
        Finish();
    }

    void Finish()
    {
        File.WriteAllText(Path.Combine(output, "result.txt"), failed ? "FAIL" : "PASS");
        Debug.Log("FLOW OUTPUT " + output);
        Application.Quit(failed ? 1 : 0);
    }
}
