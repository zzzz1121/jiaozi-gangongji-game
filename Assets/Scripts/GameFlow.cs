using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Jiaozi;

public enum GameScreen
{
    Title,
    Map,
    Selection,
    Playing,
    Pause,
    Results,
    Gallery,
    Settings,
    Help
}

public sealed class GameFlow : MonoBehaviour
{
    public Graybox Game { get; private set; }
    public GameProgress Progress { get; private set; }
    public ProgressStore Store { get; private set; }
    public GameScreen Screen { get; private set; } = GameScreen.Title;

    public event Action Changed;
    public int[] Selected =
    {
        0,
        1
    };
    public bool[] Ready =
    {
        false,
        false
    };
    public string Notice { get; private set; } = "";
    public bool NewCollection { get; private set; }
    public int Focus { get; private set; }
    public int TransitionFrame { get; private set; } = -1;
    public bool WorldActive => Screen == GameScreen.Playing;
    public bool LegacySmoke { get; private set; }

    GameScreen returnScreen;
    bool committed, learned;
    GameSettings draft;
    public GameSettings SettingsDraft => draft;
    public int StarCount => Game.Rules.Orders >= Game.chapter.starOrders[2] ? 3 : Game.Rules.Orders >= Game.chapter.starOrders[1] ? 2 : Game.Rules.Orders >= Game.chapter.starOrders[0] ? 1 : 0;
    public string Pair => Game.chapter.PairName(Selected[0], Selected[1]);
    public string CollectionKey => GameProgress.Combination(Game.chapter.artisans[Selected[0]].id, Game.chapter.artisans[Selected[1]].id);

    public void Initialize(Graybox game)
    {
        Game = game;
        LegacySmoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-smoke") >= 0;
        bool smoke = LegacySmoke || Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-flow-smoke") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-flow-reload") >= 0;
        Store = new ProgressStore(Path.Combine(Application.persistentDataPath, smoke ? "SmokeProfile/progress.json" : "progress.json"));
        Progress = Store.Load();
        Notice = Store.Notice;
        ApplySettings(Progress.settings);
        Game.Rules.Changed += RulesChanged;
        if (LegacySmoke)
            Screen = GameScreen.Playing;
    }

    void RulesChanged()
    {
        if (Screen == GameScreen.Playing && Game.Rules.Phase == RoundPhase.Results)
        {
            if (!committed)
            {
                committed = true;
                NewCollection = Progress.Complete(Game.chapter.levelId, Game.chapter.artisans[Selected[0]].id, Game.chapter.artisans[Selected[1]].id, Game.Rules.Tickets, Game.Rules.Score, StarCount, learned || Game.Rules.TutorialDelivered);
                Save();
            }

            Show(GameScreen.Results);
        }
    }

    void Save()
    {
        Store.Save(Progress);
        Notice = Store.Notice;
    }

    public void Show(GameScreen screen)
    {
        Screen = screen;
        Focus = 0;
        TransitionFrame = Time.frameCount;
        Changed?.Invoke();
    }

    public void OpenSelection()
    {
        Ready[0] = Ready[1] = false;
        Notice = "";
        Show(GameScreen.Selection);
    }

    public void Select(int player, int delta)
    {
        if (Ready[player])
            return;
        Selected[player] = (Selected[player] + delta + Game.chapter.artisans.Length) % Game.chapter.artisans.Length;
        Notice = "";
        Game.ApplyArtisans(Selected);
        Changed?.Invoke();
    }

    public void ConfirmPlayer(int player)
    {
        if (Ready[player])
        {
            Ready[player] = false;
            Changed?.Invoke();
            return;
        }

        if (Selected[0] == Selected[1])
        {
            Notice = "两人不能选同一名工匠，请先切换角色。";
            Changed?.Invoke();
            return;
        }

        Ready[player] = true;
        if (Ready[0] && Ready[1])
            BeginPlay(!Progress.tutorialComplete);
        else
            Changed?.Invoke();
    }

    public void BeginPlay(bool tutorial)
    {
        committed = false;
        NewCollection = false;
        learned = false;
        Notice = "";
        Screen = GameScreen.Playing;
        TransitionFrame = Time.frameCount;
        Game.ApplyArtisans(Selected);
        if (tutorial)
            Game.Restart();
        else
            Game.StartRound();
        Changed?.Invoke();
    }

    public void StartFormal()
    {
        learned |= Game.Rules.TutorialDelivered;
        Game.StartRound();
        Changed?.Invoke();
    }

    public void Pause()
    {
        if (Screen == GameScreen.Playing)
        {
            Game.Rules.TogglePause();
            Show(GameScreen.Pause);
        }
        else if (Screen == GameScreen.Pause)
        {
            Game.Rules.TogglePause();
            Show(GameScreen.Playing);
        }
    }

    public void Back()
    {
        if (Screen == GameScreen.Settings)
        {
            ApplySettings(Progress.settings);
            Show(returnScreen);
            return;
        }

        if (Screen == GameScreen.Help)
        {
            Show(returnScreen);
            return;
        }

        if (Screen == GameScreen.Pause)
        {
            Pause();
            return;
        }

        if (Screen == GameScreen.Playing)
        {
            Pause();
            return;
        }

        if (Screen == GameScreen.Selection)
        {
            Show(GameScreen.Map);
            return;
        }

        if (Screen == GameScreen.Map || Screen == GameScreen.Gallery)
            Show(GameScreen.Title);
    }

    public void OpenSettings()
    {
        returnScreen = Screen;
        draft = JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(Progress.settings));
        Show(GameScreen.Settings);
    }

    public void OpenHelp()
    {
        returnScreen = Screen;
        Show(GameScreen.Help);
    }

    public void SaveSettings()
    {
        Progress.settings = draft;
        ApplySettings(draft);
        Save();
        Show(returnScreen);
    }

    public void AdjustSetting(int row, int direction)
    {
        if (row == 0)
            draft.master = Mathf.Clamp01(draft.master + direction * .1f);
        if (row == 1)
            draft.music = Mathf.Clamp01(draft.music + direction * .1f);
        if (row == 2)
            draft.effects = Mathf.Clamp01(draft.effects + direction * .1f);
        if (row == 3)
            draft.resolution = (draft.resolution + direction + 3) % 3;
        if (row == 4)
            draft.fullscreen = !draft.fullscreen;
        ApplySettings(draft);
        Changed?.Invoke();
    }

    void ApplySettings(GameSettings settings)
    {
        AudioListener.volume = settings.master;
        Game.GetComponent<WorkshopAudio>()?.Apply(settings);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jiaozi-smoke") >= 0)
            return;
        int[] w =
        {
            1280,
            1920,
            1280
        }, h =
        {
            800,
            1080,
            720
        };
        int index = Mathf.Clamp(settings.resolution, 0, 2);
        if (UnityEngine.Screen.width != w[index] || UnityEngine.Screen.height != h[index] || UnityEngine.Screen.fullScreen != settings.fullscreen)
            UnityEngine.Screen.SetResolution(w[index], h[index], settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public string[] MenuItems()
    {
        switch (Screen)
        {
            case GameScreen.Title:
                return new[]
                {
                    "开始游戏",
                    "收藏图鉴",
                    "设置",
                    "操作说明",
                    "退出游戏"
                };
            case GameScreen.Map:
                return new[]
                {
                    "第一关  开张第一单",
                    "返回标题"
                };
            case GameScreen.Pause:
                return new[]
                {
                    "继续游戏",
                    "重开本关",
                    "重新选角",
                    "设置",
                    "返回地图"
                };
            case GameScreen.Results:
                return new[]
                {
                    "同组合再玩",
                    "更换角色",
                    "收藏图鉴",
                    "返回地图"
                };
            case GameScreen.Gallery:
                return new[]
                {
                    "返回标题"
                };
            case GameScreen.Help:
                return new[]
                {
                    "返回"
                };
            case GameScreen.Settings:
                return new[]
                {
                    $"总音量  {draft.master:P0}",
                    $"音乐  {draft.music:P0}",
                    $"音效  {draft.effects:P0}",
                    "窗口尺寸  " + new[]
                    {
                        "1280 × 800",
                        "1920 × 1080",
                        "1280 × 720"
                    }[draft.resolution],
                    "全屏  " + (draft.fullscreen ? "开" : "关"),
                    "保存并返回",
                    "取消修改"
                };
            default:
                return Array.Empty<string>();
        }
    }

    public void Activate(int index)
    {
        switch (Screen)
        {
            case GameScreen.Selection:
                if(index==0)Select(0,1);
                else if(index==1)ConfirmPlayer(0);
                else if(index==2)Select(1,1);
                else if(index==3)ConfirmPlayer(1);
                else Show(GameScreen.Map);
                break;
            case GameScreen.Title:
                if (index == 0)
                    Show(GameScreen.Map);
                else if (index == 1)
                    Show(GameScreen.Gallery);
                else if (index == 2)
                    OpenSettings();
                else if (index == 3)
                    OpenHelp();
                else
                    Application.Quit();
                break;
            case GameScreen.Map:
                if (index == 0)
                    OpenSelection();
                else
                    Show(GameScreen.Title);
                break;
            case GameScreen.Pause:
                if (index == 0)
                    Pause();
                else if (index == 1)
                    BeginPlay(false);
                else if (index == 2)
                    OpenSelection();
                else if (index == 3)
                    OpenSettings();
                else
                    Show(GameScreen.Map);
                break;
            case GameScreen.Results:
                if (index == 0)
                    BeginPlay(false);
                else if (index == 1)
                    OpenSelection();
                else if (index == 2)
                    Show(GameScreen.Gallery);
                else
                    Show(GameScreen.Map);
                break;
            case GameScreen.Gallery:
                Show(GameScreen.Title);
                break;
            case GameScreen.Help:
                Show(returnScreen);
                break;
            case GameScreen.Settings:
                if (index < 5)
                    AdjustSetting(index, 1);
                else if (index == 5)
                    SaveSettings();
                else
                    Back();
                break;
        }
    }

    void Update()
    {
        if (LegacySmoke)
            return;
        var input = Game.Controls;
        var kb = Keyboard.current;
        if (kb == null)
            return;
        if (input.Pause.WasPressedThisFrame())
        {
            Back();
            return;
        }

        if (Screen == GameScreen.Playing)
            return;
        if (Screen == GameScreen.Selection)
        {
            if (kb.aKey.wasPressedThisFrame)
                Select(0, -1);
            if (kb.dKey.wasPressedThisFrame)
                Select(0, 1);
            if (kb.leftArrowKey.wasPressedThisFrame)
                Select(1, -1);
            if (kb.rightArrowKey.wasPressedThisFrame)
                Select(1, 1);
            if (input.Interact(0))
                ConfirmPlayer(0);
            if (input.Interact(1) && Screen == GameScreen.Selection)
                ConfirmPlayer(1);
            if(Screen==GameScreen.Selection){
                if(input.Up.WasPressedThisFrame()){Focus=(Focus+4)%5;Changed?.Invoke();}
                if(input.Down.WasPressedThisFrame()){Focus=(Focus+1)%5;Changed?.Invoke();}
                if(input.Confirm.WasPressedThisFrame())Activate(Focus);
            }
            return;
        }

        var items = MenuItems();
        if (items.Length == 0)
            return;
        if (input.Up.WasPressedThisFrame())
        {
            Focus = (Focus + items.Length - 1) % items.Length;
            Changed?.Invoke();
        }

        if (input.Down.WasPressedThisFrame())
        {
            Focus = (Focus + 1) % items.Length;
            Changed?.Invoke();
        }

        if (Screen == GameScreen.Settings && (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) && Focus < 5)
            AdjustSetting(Focus, kb.leftArrowKey.wasPressedThisFrame ? -1 : 1);
        if (input.Confirm.WasPressedThisFrame())
            Activate(Focus);
    }

    void OnDestroy()
    {
        if (Game != null)
            Game.Rules.Changed -= RulesChanged;
    }
}
