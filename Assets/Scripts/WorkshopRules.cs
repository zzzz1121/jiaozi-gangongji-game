using System;
using System.Collections.Generic;

namespace Jiaozi
{
    public enum PaperKind
    {
        Wet,
        Dry,
        Printed,
        Stamped
    }

    public enum StationKind
    {
        Pulp,
        Drying,
        Print,
        Stamp,
        Shelf,
        Delivery
    }

    public enum RoundPhase
    {
        Tutorial,
        Running,
        Paused,
        Results
    }

    [Serializable]
    public class WorkshopConfig
    {
        public float roundSeconds = 180, pulpSeconds = 2, dryingSeconds = 4, printSeconds = 2, stampSeconds = 1;
        public float moveSpeed = 3, interactionDistance = 1.2f;
        public int[] orderSequence =
        {
            1,
            1,
            2
        };
    }

    public sealed class Paper
    {
        public readonly int Id;
        public PaperKind Kind { get; internal set; }
        public string Owner { get; internal set; }
        public bool Consumed { get; internal set; }

        internal Paper(int id, PaperKind kind, string owner)
        {
            Id = id;
            Kind = kind;
            Owner = owner;
        }
    }

    public sealed class Slot
    {
        public readonly string Id;
        public readonly StationKind Kind;
        public Paper Item { get; internal set; }
        public float Progress { get; internal set; }
        public int Worker { get; internal set; } = -1;

        internal Slot(string id, StationKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public bool Complete => Item != null && (Kind == StationKind.Shelf || Kind == StationKind.Pulp || Kind == StationKind.Drying && Item.Kind == PaperKind.Dry || Kind == StationKind.Print && Item.Kind == PaperKind.Printed || Kind == StationKind.Stamp && Item.Kind == PaperKind.Stamped);
    }

    // Pure simulation. Caller submits players in stable ID order once per frame.
    public sealed class WorkshopRules
    {
        public readonly WorkshopConfig Config;
        public readonly Slot[] Slots;
        public readonly Paper[] Hands = new Paper[2];
        public readonly List<Paper> Papers = new List<Paper>();
        public event Action Changed;
        public event Action<string> Recorded;
        public RoundPhase Phase { get; private set; } = RoundPhase.Tutorial;

        RoundPhase resumePhase;
        int nextId;
        public float Remaining { get; private set; }
        public int Tickets { get; private set; }
        public int Orders { get; private set; }
        public int Received { get; private set; }
        public int Score => Tickets * 10 + Orders * 10;
        public int Stars => Orders >= 6 ? 3 : Orders >= 4 ? 2 : Orders >= 2 ? 1 : 0;
        public int Required => Config.orderSequence[Orders % Config.orderSequence.Length];
        public bool TutorialDelivered { get; private set; }
        public int ResultsCount { get; private set; }
        public bool Active => Phase == RoundPhase.Tutorial || Phase == RoundPhase.Running;

        public WorkshopRules(WorkshopConfig config = null)
        {
            Config = config ?? new WorkshopConfig();
            Remaining = Config.roundSeconds;
            Slots = new[]
            {
                new Slot("pulp", StationKind.Pulp),
                new Slot("dry-a", StationKind.Drying),
                new Slot("dry-b", StationKind.Drying),
                new Slot("print", StationKind.Print),
                new Slot("stamp", StationKind.Stamp),
                new Slot("shelf", StationKind.Shelf),
                new Slot("delivery", StationKind.Delivery)
            };
        }

        void Notify()
        {
            Changed?.Invoke();
        }

        void Log(string s)
        {
            Recorded?.Invoke(s);
        }

        public void Reset(bool tutorial)
        {
            foreach (var p in Papers)
            {
                p.Consumed = true;
                p.Owner = "reset";
            }

            Papers.Clear();
            Array.Clear(Hands, 0, 2);
            foreach (var s in Slots)
            {
                s.Item = null;
                s.Progress = 0;
                s.Worker = -1;
            }

            Remaining = Config.roundSeconds;
            Tickets = Orders = Received = ResultsCount = 0;
            TutorialDelivered = false;
            Phase = tutorial ? RoundPhase.Tutorial : RoundPhase.Running;
            Log(tutorial ? "tutorial_start" : "round_start");
            Notify();
        }

        public void TogglePause()
        {
            if (Phase == RoundPhase.Results)
                return;
            if (Phase == RoundPhase.Paused)
                Phase = resumePhase;
            else
            {
                resumePhase = Phase;
                Phase = RoundPhase.Paused;
            }

            Log("pause_" + Phase);
            Notify();
        }

        public void BeginFrame(float dt)
        {
            foreach (var s in Slots)
                s.Worker = -1;
            if (!Active)
                return;
            dt = Math.Max(0, dt);
            if (Phase == RoundPhase.Running)
            {
                Remaining = Math.Max(0, Remaining - dt);
                if (Remaining <= 0)
                {
                    Phase = RoundPhase.Results;
                    ResultsCount++;
                    Log($"round_end notes={Tickets} orders={Orders} score={Score}");
                    Notify();
                    return;
                }
            }

            foreach (var s in Slots)
                if (s.Kind == StationKind.Drying && s.Item != null && s.Item.Kind == PaperKind.Wet)
                {
                    s.Progress = Math.Min(Config.dryingSeconds, s.Progress + dt);
                    if (s.Progress >= Config.dryingSeconds)
                    {
                        s.Item.Kind = PaperKind.Dry;
                        Log("dry_complete " + s.Id);
                    }
                }

            Notify();
        }

        public float Duration(Slot s) => s.Kind == StationKind.Pulp ? Config.pulpSeconds : s.Kind == StationKind.Drying ? Config.dryingSeconds : s.Kind == StationKind.Print ? Config.printSeconds : s.Kind == StationKind.Stamp ? Config.stampSeconds : 0;
        public bool Process(int player, int index, float dt, out string reason)
        {
            reason = "";
            if (!Active)
                return false;
            var s = Slots[index];
            if (s.Worker >= 0)
            {
                reason = "搭档正在操作";
                return false;
            }

            bool valid = s.Kind == StationKind.Pulp ? s.Item == null && Hands[player] == null : s.Kind == StationKind.Print ? s.Item?.Kind == PaperKind.Dry : s.Kind == StationKind.Stamp && s.Item?.Kind == PaperKind.Printed;
            if (!valid)
            {
                reason = "请放入正确材料，或先取走成品";
                return false;
            }

            s.Worker = player;
            s.Progress = Math.Min(Duration(s), s.Progress + Math.Max(0, dt));
            if (s.Progress >= Duration(s))
            {
                if (s.Kind == StationKind.Pulp)
                {
                    s.Item = new Paper(++nextId, PaperKind.Wet, s.Id);
                    Papers.Add(s.Item);
                }
                else
                    s.Item.Kind = s.Kind == StationKind.Print ? PaperKind.Printed : PaperKind.Stamped;
                Log("process_complete " + s.Id);
            }

            Notify();
            return true;
        }

        public bool Interact(int player, int index, out string reason)
        {
            reason = "";
            if (!Active)
                return false;
            var s = Slots[index];
            var hand = Hands[player];
            if (s.Kind == StationKind.Delivery)
            {
                if (hand == null || hand.Kind != PaperKind.Stamped)
                {
                    reason = "尚未盖印：窗口只收朱印交子";
                    return false;
                }

                hand.Consumed = true;
                hand.Owner = "consumed";
                Hands[player] = null;
                if (Phase == RoundPhase.Tutorial)
                {
                    TutorialDelivered = true;
                    Log("tutorial_delivered");
                }
                else
                {
                    Tickets++;
                    Received++;
                    if (Received == Required)
                    {
                        Orders++;
                        Received = 0;
                    }

                    Log($"delivered player={player + 1} notes={Tickets} score={Score}");
                }

                Notify();
                return true;
            }

            if (hand == null)
            {
                if (!s.Complete)
                {
                    reason = s.Item == null ? "这里还没有可取物件" : "加工尚未完成";
                    return false;
                }

                var item = s.Item;
                if (item.Owner != s.Id || item.Consumed)
                    throw new InvalidOperationException("Invalid slot ownership");
                s.Item = null;
                s.Progress = 0;
                item.Owner = "p" + player;
                Hands[player] = item;
                Log("take " + s.Id);
                Notify();
                return true;
            }

            if (s.Item != null || s.Progress > 0)
            {
                reason = "工位已占用";
                return false;
            }

            bool accepted = s.Kind == StationKind.Shelf || s.Kind == StationKind.Drying && hand.Kind == PaperKind.Wet || s.Kind == StationKind.Print && hand.Kind == PaperKind.Dry || s.Kind == StationKind.Stamp && hand.Kind == PaperKind.Printed;
            if (!accepted)
            {
                reason = hand.Kind == PaperKind.Wet ? "先晾干湿纸" : "材料不符，请检查工序";
                return false;
            }

            if (hand.Owner != "p" + player || hand.Consumed)
                throw new InvalidOperationException("Invalid hand ownership");
            Hands[player] = null;
            s.Item = hand;
            hand.Owner = s.Id;
            Log("place " + s.Id);
            Notify();
            return true;
        }

        public bool Handoff(int from, int to, out string reason)
        {
            reason = "";
            if (!Active || from == to || Hands[from] == null)
            {
                reason = "手中没有物件";
                return false;
            }

            if (Hands[to] != null)
            {
                reason = "搭档双手已占用";
                return false;
            }

            var p = Hands[from];
            if (p.Owner != "p" + from || p.Consumed)
                throw new InvalidOperationException("Invalid transfer ownership");
            Hands[from] = null;
            Hands[to] = p;
            p.Owner = "p" + to;
            Log($"handoff {from + 1}->{to + 1}");
            Notify();
            return true;
        }

        public bool OwnershipValid()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < 2; i++)
                if (Hands[i] != null && (!seen.Add(Hands[i].Id) || Hands[i].Consumed || Hands[i].Owner != "p" + i))
                    return false;
            foreach (var s in Slots)
                if (s.Item != null && (!seen.Add(s.Item.Id) || s.Item.Consumed || s.Item.Owner != s.Id))
                    return false;
            foreach (var p in Papers)
                if (!p.Consumed && !seen.Contains(p.Id))
                    return false;
            return true;
        }
    }
}
