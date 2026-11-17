using System;
using System.Collections.Generic;

namespace Project05.CombatCore.Run
{
    /// <summary>Where the run is (System 5, simplified for the one-hero slice).</summary>
    public enum RunPhase
    {
        /// <summary>Arranging before the room's fight.</summary>
        Planning = 0,
        /// <summary>A room was won; the door's buff (and a Gold door's curse) waits for a hero.</summary>
        BuffClaim = 1,
        /// <summary>Take one offered item or skip.</summary>
        Reward = 2,
        /// <summary>Arm one of three door cards and confirm.</summary>
        DoorChoice = 3,
        /// <summary>The boss is down: the run is won.</summary>
        Won = 4,
        /// <summary>A defeat or draw ended the run.</summary>
        Lost = 5,
        /// <summary>In a fusion room (item-fusion.md Rule 6): no fight; arrange, sell, forge and fuse once, then leave.</summary>
        Fusion = 6,
    }

    /// <summary>Why a claim was refused (System 19 Rule 5).</summary>
    public enum ClaimStatus { Ok = 0, WrongPhase = 1, InvalidHero = 2, HeroHasCurse = 3, NeedsSpace = 4 }

    /// <summary>What a claim granted; the caller opens the cells and creates the pinned item.</summary>
    public readonly struct ClaimResult
    {
        public readonly ClaimStatus Status;
        public readonly int HpGranted;
        public readonly int CellsGranted;
        public readonly CurseDef Curse;

        public ClaimResult(ClaimStatus status, int hp, int cells, CurseDef curse)
        {
            Status = status;
            HpGranted = hp;
            CellsGranted = cells;
            Curse = curse;
        }
    }

    /// <summary>An active curse in the run (System 19 Rule 8).</summary>
    public sealed class ActiveCurse
    {
        public CurseDef Curse { get; }
        public int HeroSlot { get; }
        public int AcquiredRoom { get; }

        public ActiveCurse(CurseDef curse, int heroSlot, int acquiredRoom)
        {
            Curse = curse;
            HeroSlot = heroSlot;
            AcquiredRoom = acquiredRoom;
        }
    }

    /// <summary>One cleared room, for the corridor and its history popup.</summary>
    public sealed class RoomRecord
    {
        public int Room { get; }
        /// <summary>The tier of the door that led into the room (White for room 1).</summary>
        public TierKind Tier { get; }
        public bool Entrance { get; }
        public int HpGranted { get; internal set; }
        public int CellsGranted { get; internal set; }
        public string CurseId { get; internal set; } = string.Empty;
        public string ItemTaken { get; internal set; } = string.Empty;
        /// <summary>True for a fusion room (item-fusion.md Rule 6).</summary>
        public bool Fusion { get; internal set; }
        /// <summary>The recipe fused in this fusion room, or empty.</summary>
        public string RecipeMade { get; internal set; } = string.Empty;

        public RoomRecord(int room, TierKind tier, bool entrance)
        {
            Room = room;
            Tier = tier;
            Entrance = entrance;
        }
    }

    /// <summary>
    /// One climb of the floor (System 5, MVP slice): rooms 1..Rooms, then the boss. After a win the next door row and
    /// the item offer are built in one step, then the player claims the buff, takes or skips an item and chooses a door.
    /// Engine-free and deterministic from the seed and the commands. The grid itself is the caller's: a claim returns
    /// the cells to open and the curse (with its pinned item) to apply.
    /// </summary>
    public sealed class RunSession
    {
        private readonly RunConfig _cfg;
        private readonly GameCatalog _catalog;
        private readonly SeededRng _rng;
        private readonly int[] _doorHp;
        private readonly List<ActiveCurse> _curses = new List<ActiveCurse>();
        private readonly List<RoomRecord> _history = new List<RoomRecord>();
        private DoorCard[] _row = Array.Empty<DoorCard>();
        private List<string> _offer = new List<string>();
        private readonly RngStream _fusionStream;
        private bool _lastRowHadFusion;
        private string _fusedRecipe = string.Empty;

        public RunConfig Config => _cfg;
        public ulong Seed => _rng.Seed;
        public int HeroCount { get; }
        /// <summary>1..Rooms for the floor's rooms; Rooms + 1 is the boss room.</summary>
        public int Room { get; private set; } = 1;
        public int BossRoom => _cfg.Rooms + 1;
        public bool IsBossRoom => Room == BossRoom;
        public RunPhase Phase { get; private set; } = RunPhase.Planning;
        /// <summary>The card that led into the current room; null in room 1 and the boss room.</summary>
        public DoorCard CurrentDoor { get; private set; }
        public EncounterSpec CurrentEncounter { get; private set; }
        /// <summary>The three cards after a win (empty after the last room).</summary>
        public IReadOnlyList<DoorCard> Row => _row;
        public IReadOnlyList<string> Offer => _offer;
        public IReadOnlyList<ActiveCurse> Curses => _curses;
        public IReadOnlyList<RoomRecord> History => _history;
        public int DoorCells { get; private set; }
        /// <summary>The curse waiting with the buff in BuffClaim, or null.</summary>
        public CurseDef PendingCurse => Phase == RunPhase.BuffClaim ? CurrentDoor?.Curse : null;
        /// <summary>The buff waiting in BuffClaim.</summary>
        public DoorBuff PendingBuff => Phase == RunPhase.BuffClaim && CurrentDoor != null ? CurrentDoor.Buff : default;
        /// <summary>Debug policy (System 19 Rule 1): make card 3 Gold whenever Gold is allowed.</summary>
        public bool ForceGold { get; set; }
        /// <summary>Calls made on the doors stream, for tests (System 17 Rule 5).</summary>
        public int DoorDrawCalls { get; private set; }
        /// <summary>Debug policy (like ForceGold): every eligible row holds a Fusion Door; the fusion draw is still made.</summary>
        public bool ForceFusion { get; set; }
        /// <summary>Calls made on the fusion stream, for tests (item-fusion.md Rule 4).</summary>
        public int FusionDrawCalls { get; private set; }
        /// <summary>Fusion doors offered so far in this run.</summary>
        public int FusionDoorsOffered { get; private set; }
        /// <summary>True in the Fusion phase once the visit's one fusion is made (Rule 7).</summary>
        public bool FusedThisRoom => Phase == RunPhase.Fusion && !string.IsNullOrEmpty(_fusedRecipe);
        /// <summary>True when the current room is a fusion room (no fight).</summary>
        public bool IsFusionRoom => CurrentDoor != null && CurrentDoor.IsFusion;

        /// <summary>The party curse cap: never more than the party has heroes (System 19 Rule 9).</summary>
        public int CurseCap => Math.Min(_cfg.CurseCapParty, Math.Max(1, HeroCount));

        public RunSession(RunConfig cfg, GameCatalog catalog, ulong seed, int heroCount)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _catalog = catalog;
            _rng = new SeededRng(seed);
            HeroCount = Math.Max(1, heroCount);
            _doorHp = new int[HeroCount];
            _fusionStream = SeededRng.DeriveSideStream(seed, "fusion");
            RngStream doors = _rng.GetStream("doors");
            CurrentEncounter = EncounterBuilder.Draw(_cfg, _catalog, 1, TierKind.White, doors)
                ?? throw new InvalidOperationException("ContractViolation: room 1 has no White template.");
            DoorDrawCalls = 1;
        }

        public int DoorHp(int heroSlot) => heroSlot >= 0 && heroSlot < _doorHp.Length ? _doorHp[heroSlot] : 0;

        public ActiveCurse CurseOf(int heroSlot)
        {
            foreach (ActiveCurse c in _curses)
            {
                if (c.HeroSlot == heroSlot) return c;
            }

            return null;
        }

        /// <summary>The baseline of a hero for the next fight, with its door HP and curse terms.</summary>
        public HeroBaselineInput Baseline(int heroSlot, HeroDef def, ArrangementGridView grid, GridSynergyView synergy = null)
        {
            CurseDef curse = CurseOf(heroSlot)?.Curse;
            return new HeroBaselineInput(heroSlot, def, DoorHp(heroSlot), grid, synergy,
                curse?.HpDelta ?? 0, curse?.SpeedDelta ?? 0, curse?.WitherPerTurn ?? 0, curse?.Id);
        }

        /// <summary>The enemies of the current room.</summary>
        public List<EnemySpawnInput> BuildSpawns() => CurrentEncounter.BuildSpawns(_catalog);

        /// <summary>The current room's fight was won: builds the row and the offer, then waits for the claim.</summary>
        public void OnVictory()
        {
            if (Phase != RunPhase.Planning) return;
            var record = new RoomRecord(Room, CurrentDoor?.Tier ?? TierKind.White, Room == 1);
            if (IsBossRoom)
            {
                Phase = RunPhase.Won;
                return;
            }

            _history.Add(record);
            TierKind doorTier = CurrentDoor?.Tier ?? TierKind.White;
            RngStream doors = _rng.GetStream("doors").Clone();
            RngStream drops = _rng.GetStream("drops").Clone();
            int pending = CurrentDoor?.Curse != null ? 1 : 0;
            DoorCard[] row = Room < _cfg.Rooms
                ? DoorGenerator.BuildRow(_cfg, _catalog, Room + 1, _curses.Count + pending, CurseCap, doors, ForceGold)
                : Array.Empty<DoorCard>();
            List<string> offer = RewardOffer.Build(_cfg, _catalog, doorTier, drops);

            // both built: commit together (System 17 Rule 2)
            _rng.GetStream("doors").CopyFrom(doors);
            _rng.GetStream("drops").CopyFrom(drops);
            if (row.Length > 0) DoorDrawCalls += 6;
            FusionPass(row, Room + 1);
            _row = row;
            _offer = offer;
            Phase = CurrentDoor != null && (!CurrentDoor.Buff.IsEmpty || CurrentDoor.Curse != null) ? RunPhase.BuffClaim : RunPhase.Reward;
        }

        /// <summary>A defeat or a draw ends the run (no retry, no reroll).</summary>
        public void OnDefeat()
        {
            if (Phase == RunPhase.Planning) Phase = RunPhase.Lost;
        }

        /// <summary>
        /// The buff's cells this hero would receive now (caps applied). lockedCellsLeft is how many locked cells the
        /// hero's grid still has; a hero with none loses the cell part (System 17 Rule 7).
        /// </summary>
        public int CellsFor(int lockedCellsLeft) =>
            Math.Max(0, Math.Min(PendingBuff.Cells, Math.Min(_cfg.DoorCellCap - DoorCells, lockedCellsLeft)));

        /// <summary>The buff's HP this hero would receive now (cap applied).</summary>
        public int HpFor(int heroSlot) => Math.Max(0, Math.Min(PendingBuff.Hp, _cfg.DoorHpCap - DoorHp(heroSlot)));

        /// <summary>
        /// Claims the door's buff, and a Gold door's curse, for one hero (System 17 Rule 7, System 19 Rule 5). Checks in
        /// order: phase, hero, HeroHasCurse, NeedsSpace. On Ok the HP and counters change here; the caller opens
        /// CellsGranted cells and, for a Pinned curse, creates the pinned item.
        /// </summary>
        public ClaimResult ClaimBuff(int heroSlot, int lockedCellsLeft, bool pinnedFits)
        {
            if (Phase != RunPhase.BuffClaim) return new ClaimResult(ClaimStatus.WrongPhase, 0, 0, null);
            if (heroSlot < 0 || heroSlot >= HeroCount) return new ClaimResult(ClaimStatus.InvalidHero, 0, 0, null);
            CurseDef curse = PendingCurse;
            if (curse != null && CurseOf(heroSlot) != null) return new ClaimResult(ClaimStatus.HeroHasCurse, 0, 0, null);
            int cells = CellsFor(lockedCellsLeft);
            if (curse != null && curse.Kind == CurseKind.Pinned && !pinnedFits) return new ClaimResult(ClaimStatus.NeedsSpace, 0, 0, null);

            int hp = HpFor(heroSlot);
            _doorHp[heroSlot] += hp;
            DoorCells += cells;
            if (curse != null) _curses.Add(new ActiveCurse(curse, heroSlot, Room));
            RoomRecord rec = _history.Count > 0 ? _history[_history.Count - 1] : null;
            if (rec != null)
            {
                rec.HpGranted = hp;
                rec.CellsGranted = cells;
                rec.CurseId = curse?.Id ?? string.Empty;
            }

            Phase = RunPhase.Reward;
            return new ClaimResult(ClaimStatus.Ok, hp, cells, curse);
        }

        /// <summary>Takes offered item i; returns its id, or null when not possible.</summary>
        public string TakeReward(int index)
        {
            if (Phase != RunPhase.Reward || index < 0 || index >= _offer.Count) return null;
            string id = _offer[index];
            if (_history.Count > 0) _history[_history.Count - 1].ItemTaken = id;
            AfterReward();
            return id;
        }

        /// <summary>Skips the item offer.</summary>
        public bool SkipReward()
        {
            if (Phase != RunPhase.Reward) return false;
            AfterReward();
            return true;
        }

        private void AfterReward()
        {
            _offer = new List<string>();
            if (_row.Length > 0)
            {
                Phase = RunPhase.DoorChoice;
                return;
            }

            // after the last room the boss follows: no door, no tier
            Room = BossRoom;
            CurrentDoor = null;
            CurrentEncounter = EncounterBuilder.Spec(_cfg, _catalog, _cfg.Boss, BossRoom, true);
            Phase = RunPhase.Planning;
        }

        /// <summary>
        /// Enters the room behind door i. Final: there is no retry or reroll. A fusion door leads to the Fusion phase
        /// (item-fusion.md Rule 6): no fight, so no encounter.
        /// </summary>
        public bool ChooseDoor(int index)
        {
            if (Phase != RunPhase.DoorChoice || index < 0 || index >= _row.Length) return false;
            CurrentDoor = _row[index];
            CurrentEncounter = CurrentDoor.Encounter;
            Room += 1;
            _row = Array.Empty<DoorCard>();
            _fusedRecipe = string.Empty;
            Phase = CurrentDoor.IsFusion ? RunPhase.Fusion : RunPhase.Planning;
            return true;
        }

        /// <summary>
        /// Records the visit's one fusion (item-fusion.md Rules 7 and 8, phase checks only): the caller has planned it
        /// with <see cref="FusionRules.Plan"/>, removed the ingredients, paid and placed the result.
        /// </summary>
        public FusionStatus RecordFusion(string recipeId)
        {
            if (Phase != RunPhase.Fusion) return FusionStatus.NotFusionRoom;
            if (FusedThisRoom) return FusionStatus.AlreadyFused;
            if (_catalog?.FindRecipe(recipeId) == null) return FusionStatus.UnknownRecipe;
            _fusedRecipe = recipeId;
            return FusionStatus.Ok;
        }

        /// <summary>The phase checks of a fusion before planning it: NotFusionRoom, AlreadyFused, or Ok.</summary>
        public FusionStatus CanFuse => Phase != RunPhase.Fusion ? FusionStatus.NotFusionRoom : FusedThisRoom ? FusionStatus.AlreadyFused : FusionStatus.Ok;

        /// <summary>
        /// Leaves the fusion room (Rule 7): records it, then builds the row to the next room on the doors stream (and the
        /// fusion pass), or after the last room enters the boss room.
        /// </summary>
        public bool LeaveFusion()
        {
            if (Phase != RunPhase.Fusion) return false;
            _history.Add(new RoomRecord(Room, TierKind.White, false) { Fusion = true, RecipeMade = _fusedRecipe });
            _fusedRecipe = string.Empty;
            if (Room < _cfg.Rooms)
            {
                RngStream doors = _rng.GetStream("doors").Clone();
                DoorCard[] row = DoorGenerator.BuildRow(_cfg, _catalog, Room + 1, _curses.Count, CurseCap, doors, ForceGold);
                _rng.GetStream("doors").CopyFrom(doors);
                DoorDrawCalls += 6;
                FusionPass(row, Room + 1);
                _row = row;
                Phase = RunPhase.DoorChoice;
                return true;
            }

            _row = Array.Empty<DoorCard>();
            Room = BossRoom;
            CurrentDoor = null;
            CurrentEncounter = EncounterBuilder.Spec(_cfg, _catalog, _cfg.Boss, BossRoom, true);
            Phase = RunPhase.Planning;
            return true;
        }

        private void FusionPass(DoorCard[] row, int targetRoom)
        {
            if (row.Length == 0) return;
            bool eligible = FusionDoor.Eligible(_cfg, targetRoom, FusionDoorsOffered, _lastRowHadFusion);
            bool placed = FusionDoor.Apply(_cfg, row, targetRoom, FusionDoorsOffered, _lastRowHadFusion, _fusionStream, ForceFusion);
            if (eligible) FusionDrawCalls++;
            if (placed) FusionDoorsOffered++;
            _lastRowHadFusion = placed;
        }
    }
}
