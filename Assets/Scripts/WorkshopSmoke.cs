using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Jiaozi;

// Opt-in development build smoke test. Never changes a normal play session.
public sealed class WorkshopSmoke : MonoBehaviour
{
    Graybox game;
    string output;
    bool failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-smoke") >= 0)
            new GameObject("Smoke test").AddComponent<WorkshopSmoke>();
    }

    void Check(bool condition, string name)
    {
        Debug.Log("SMOKE " + (condition ? "PASS " : "FAIL ") + name);
        if (!condition)
            failed = true;
    }

    IEnumerator KeyPress(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
    }

    IEnumerator Hold(Key key, float seconds)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(seconds);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        yield return null;
        yield return null;
    }

    void Place(int player, Vector3 position, Vector3 facing)
    {
        var p = game.Players[player];
        p.Controller.enabled = false;
        p.Root.position = position;
        p.Facing = facing;
        p.Root.rotation = Quaternion.LookRotation(facing);
        p.Controller.enabled = true;
        Physics.SyncTransforms();
    }

    IEnumerator At(int player, int station)
    {
        Place(player, game.layout[station].position + new Vector3(0, 0, -1.1f), Vector3.forward);
        yield return null;
        Check(game.FindTarget(player) == station, "target " + station);
    }

    IEnumerator MakeNote()
    {
        yield return At(0, 0);
        yield return Hold(Key.G, .15f);
        yield return KeyPress(Key.F);
        Check(game.Rules.Hands[0]?.Kind == PaperKind.Wet, "wet paper pickup");
        yield return At(0, 1);
        yield return KeyPress(Key.F);
        yield return new WaitForSecondsRealtime(.2f);
        yield return KeyPress(Key.F);
        Check(game.Rules.Hands[0]?.Kind == PaperKind.Dry, "automatic drying");
        Place(0, new Vector3(-.55f, 0, 0), Vector3.right);
        Place(1, new Vector3(.55f, 0, 0), Vector3.left);
        yield return null;
        Check(game.FindTarget(0) == -2, "highlight partner in doorway");
        yield return KeyPress(Key.F);
        Check(game.Rules.Hands[1]?.Kind == PaperKind.Dry && game.Rules.Hands[0] == null, "handoff through clear doorway");
        yield return At(1, 3);
        yield return KeyPress(Key.Slash);
        yield return Hold(Key.Period, .15f);
        yield return KeyPress(Key.Slash);
        yield return At(1, 4);
        yield return KeyPress(Key.Slash);
        yield return Hold(Key.Period, .15f);
        yield return KeyPress(Key.Slash);
        Check(game.Rules.Hands[1]?.Kind == PaperKind.Stamped, "print and stamp");
        yield return At(1, 6);
        yield return KeyPress(Key.Slash);
        Check(game.Rules.OwnershipValid(), "world ownership invariant");
    }

    IEnumerator Capture(string file, int w, int h)
    {
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(.7f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(output, file));
        yield return new WaitForSecondsRealtime(.3f);
    }

    IEnumerator Start()
    {
        while (!UnityEngine.Rendering.SplashScreen.isFinished)
            yield return null;
        for (int frame = 0; frame < 30; frame++)
            yield return null;
        game = FindFirstObjectByType<Graybox>();
        output = Path.Combine(Application.persistentDataPath, "Smoke");
        Directory.CreateDirectory(output);
        Check(game != null, "scene boot");
        if (game == null)
        {
            Application.Quit(1);
            yield break;
        }

        yield return Capture("tutorial-1280x800.png", 1280, 800);
        var start = game.Players[0].Root.position;
        yield return Hold(Key.W, .4f);
        Debug.Log("SMOKE MOVEMENT " + start + " -> " + game.Players[0].Root.position);
        Check(game.Players[0].Root.position.z > start.z + .2f, "Input System movement");
        Place(0, new Vector3(-.55f, 0, 2.6f), Vector3.right);
        Place(1, new Vector3(.55f, 0, 2.6f), Vector3.left);
        Check(!game.ClearPath(game.Players[0].Root.position, game.Players[1].Root.position, 0, 1), "wall blocks handoff");
        Place(0, new Vector3(-.6f, 0, 2.6f), Vector3.right);
        float bx = game.Players[0].Root.position.x;
        yield return Hold(Key.D, .4f);
        Check(game.Players[0].Root.position.x < -.3f, "wall blocks movement");
        game.config.pulpSeconds = .08f;
        game.config.dryingSeconds = .08f;
        game.config.printSeconds = .08f;
        game.config.stampSeconds = .08f;
        game.config.roundSeconds = 20;
        yield return KeyPress(Key.Enter);
        Check(game.Rules.Phase == RoundPhase.Running, "skip tutorial via Enter");
        yield return Capture("playing-1280x720.png", 1280, 720);
        for (int round = 0; round < 2; round++)
        {
            for (int i = 0; i < 3; i++)
                yield return MakeNote();
            Check(game.Rules.Tickets == 3 && game.Rules.Orders == 2 && game.Rules.Score == 50, "round scoring example " + round);
            yield return KeyPress(Key.Escape);
            float before = game.Rules.Remaining;
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.Rules.Remaining == before, "pause freezes world");
            if (round == 0)
                yield return Capture("pause-1920x1080.png", 1920, 1080);
            yield return KeyPress(Key.Escape);
            game.Rules.BeginFrame(100);
            Check(game.Rules.Phase == RoundPhase.Results, "results transition");
            if (round == 0)
            {
                yield return Capture("results-1280x720.png", 1280, 720);
                yield return KeyPress(Key.R);
                Check(game.Rules.Papers.Count == 0 && game.Rules.Score == 0, "restart clears world");
                yield return KeyPress(Key.Enter);
            }
        }

        File.WriteAllText(Path.Combine(output, "result.txt"), failed ? "FAIL" : "PASS");
        Debug.Log("SMOKE OUTPUT " + output);
        Application.Quit(failed ? 1 : 0);
    }
}
