using System;
using System.Collections.Generic;

namespace Project05.CombatCore.Run
{
    /// <summary>The four curse kinds of System 19 (curses-and-trade-offs.md Rule 3).</summary>
    public enum CurseKind { Pinned = 0, Wither = 1, Frailty = 2, Sluggish = 3 }

    /// <summary>What a Pinned curse's rider changes.</summary>
    public enum CurseRiderKind { None = 0, Speed = 1, MaxHp = 2 }

    /// <summary>A curse record (System 19 Rule 2). Pure data.</summary>
    public sealed class CurseDef
    {
        public string Id { get; }
        public CurseKind Kind { get; }
        /// <summary>Positive magnitude: cells for Pinned, damage per turn for Wither, max HP for Frailty, speed for Sluggish.</summary>
        public int P1 { get; }
        public CurseRiderKind RiderKind { get; }
        /// <summary>Signed rider amount (Pinned only), e.g. -4 speed.</summary>
        public int RiderAmount { get; }
        /// <summary>The pinned item's definition id (Pinned only).</summary>
        public string ItemId { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        /// <summary>The one plain line the card popup states (Rule 12).</summary>
        public string LineVi { get; }
        public string LineEn { get; }

        public CurseDef(string id, CurseKind kind, int p1, CurseRiderKind riderKind, int riderAmount, string itemId,
            string nameVi, string nameEn, string lineVi, string lineEn)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            P1 = p1;
            RiderKind = riderKind;
            RiderAmount = riderAmount;
            ItemId = itemId ?? string.Empty;
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            LineVi = lineVi ?? string.Empty;
            LineEn = lineEn ?? string.Empty;
        }

        /// <summary>The signed max-HP term this curse gives its hero (System 19 Formula 3).</summary>
        public int HpDelta => Kind == CurseKind.Frailty ? -P1 : (Kind == CurseKind.Pinned && RiderKind == CurseRiderKind.MaxHp ? RiderAmount : 0);

        /// <summary>The signed speed term this curse gives its hero.</summary>
        public int SpeedDelta => Kind == CurseKind.Sluggish ? -P1 : (Kind == CurseKind.Pinned && RiderKind == CurseRiderKind.Speed ? RiderAmount : 0);

        /// <summary>Wither damage per own turn.</summary>
        public int WitherPerTurn => Kind == CurseKind.Wither ? P1 : 0;
    }

    /// <summary>The certain buff of a door tier (System 17 Rule 6).</summary>
    public readonly struct DoorBuff
    {
        public readonly int Hp;
        public readonly int Cells;

        public DoorBuff(int hp, int cells)
        {
            Hp = hp;
            Cells = cells;
        }

        public bool IsEmpty => Hp <= 0 && Cells <= 0;
    }

    /// <summary>An authored encounter template (System 16 Rule 4).</summary>
    public sealed class EncounterTemplate
    {
        public string Id { get; }
        public TierKind Tier { get; }
        public int RoomMin { get; }
        public int RoomMax { get; }
        public int Weight { get; }
        public IReadOnlyList<string> Roster { get; }
        public string CurseId { get; }

        public EncounterTemplate(string id, TierKind tier, int roomMin, int roomMax, int weight, IReadOnlyList<string> roster, string curseId)
        {
            Id = id ?? string.Empty;
            Tier = tier;
            RoomMin = roomMin;
            RoomMax = roomMax;
            Weight = weight;
            Roster = roster ?? Array.Empty<string>();
            CurseId = curseId ?? string.Empty;
        }
    }

    /// <summary>
    /// Every tuning value of the run: door tier tables, buffs, caps, enemy scaling, templates, curses and the item rows.
    /// Loaded from Data/run_mvp.json (all values provisional, from Systems 16 to 19). Engine-free; no mutable static state.
    /// </summary>
    public sealed class RunConfig
    {
        /// <summary>Rooms per floor before the boss.</summary>
        public int Rooms { get; private set; } = 8;
        /// <summary>The tier order of every weight array (zeros kept, never compacted).</summary>
        public IReadOnlyList<TierKind> Tiers { get; private set; } = Array.Empty<TierKind>();
        public int MinDistinctRoom { get; private set; } = 3;
        public int SafeCardRoom { get; private set; } = 4;
        public int DoorHpCap { get; private set; } = 100;
        public int DoorCellCap { get; private set; } = 3;
        public int CurseCapParty { get; private set; } = 2;
        public int CurseCapHero { get; private set; } = 1;
        public int HpRampPerRoom { get; private set; } = 60;
        public int DamageRampPerRoom { get; private set; } = 40;
        public IReadOnlyList<int> DangerThresholds { get; private set; } = new[] { 20, 40, 70, 110 };
        public int OfferSize { get; private set; } = 3;
        /// <summary>Gold at the start of a run (item-upgrades.md).</summary>
        public int StartGold { get; private set; } = 5;
        public IReadOnlyList<EncounterTemplate> Templates { get; private set; } = Array.Empty<EncounterTemplate>();
        public EncounterTemplate Boss { get; private set; }
        public IReadOnlyList<CurseDef> Curses { get; private set; } = Array.Empty<CurseDef>();
        /// <summary>True for an item that never appears in a reward offer (a pinned curse item).</summary>
        public bool IsNonDroppable(string itemId) => itemId != null && _nonDroppable.Contains(itemId);

        private readonly Dictionary<TierKind, int> _minRoom = new Dictionary<TierKind, int>();
        private readonly Dictionary<int, int[]> _weights = new Dictionary<int, int[]>();
        private readonly Dictionary<TierKind, DoorBuff> _buffs = new Dictionary<TierKind, DoorBuff>();
        private readonly Dictionary<TierKind, (int hp, int dmg)> _kicks = new Dictionary<TierKind, (int hp, int dmg)>();
        private readonly Dictionary<TierKind, int[]> _itemRows = new Dictionary<TierKind, int[]>();
        private readonly Dictionary<TierKind, int> _gold = new Dictionary<TierKind, int>();
        private readonly Dictionary<TierKind, int> _upgradeCost = new Dictionary<TierKind, int>();

        /// <summary>Gold paid when a room behind a door of this tier is won (room 1 counts as White; the boss pays 0).</summary>
        public int GoldFor(TierKind doorTier) => _gold.TryGetValue(doorTier, out int g) ? g : 0;

        /// <summary>Gold an upgrade of an item of this tier costs.</summary>
        public int UpgradeCostFor(TierKind itemTier) => _upgradeCost.TryGetValue(itemTier, out int c) ? c : 0;
        private readonly HashSet<string> _nonDroppable = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>The first room a tier may lead to.</summary>
        public int MinRoomFor(TierKind tier) => _minRoom.TryGetValue(tier, out int r) ? r : int.MaxValue;

        /// <summary>A copy of the tier weights for target room r (length = Tiers.Count), or all zeros.</summary>
        public int[] WeightsFor(int room)
        {
            var copy = new int[Tiers.Count];
            if (_weights.TryGetValue(room, out int[] w)) Array.Copy(w, copy, Math.Min(w.Length, copy.Length));
            return copy;
        }

        /// <summary>The certain buff of a tier (System 17 Rule 6, System 19 Rule 10).</summary>
        public DoorBuff BuffOf(TierKind tier) => _buffs.TryGetValue(tier, out DoorBuff b) ? b : default;

        /// <summary>The HP and damage kicks of a tier (System 16 Rule 9).</summary>
        public (int hp, int dmg) KickOf(TierKind tier) => _kicks.TryGetValue(tier, out (int, int) k) ? k : (0, 0);

        /// <summary>Item-tier weights [White, Green, Blue, Purple] of the reward offer for a door tier (System 18).</summary>
        public int[] ItemRowFor(TierKind doorTier) =>
            _itemRows.TryGetValue(doorTier, out int[] row) ? (int[])row.Clone() : new[] { 1000, 0, 0, 0 };

        public CurseDef FindCurse(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (CurseDef c in Curses)
            {
                if (string.Equals(c.Id, id, StringComparison.Ordinal)) return c;
            }

            return null;
        }

        /// <summary>The index of a tier in <see cref="Tiers"/>, or -1.</summary>
        public int IndexOf(TierKind tier)
        {
            for (int i = 0; i < Tiers.Count; i++)
            {
                if (Tiers[i] == tier) return i;
            }

            return -1;
        }

        /// <summary>
        /// Parses run_mvp.json and validates it against the catalog (System 17 Rule 13, System 16 Rule 12 in part,
        /// System 19 Rule 9 in part). Returns null and lists every error when the content is illegal.
        /// </summary>
        public static RunConfig Load(string json, GameCatalog catalog, out IReadOnlyList<string> errors)
        {
            var list = new List<string>();
            errors = list;
            if (string.IsNullOrWhiteSpace(json))
            {
                list.Add("run config is empty.");
                return null;
            }

            object root;
            try
            {
                root = SimpleJsonParser.Parse(json);
            }
            catch (Exception e)
            {
                list.Add("run config is not valid JSON: " + e.Message);
                return null;
            }

            if (!(root is Dictionary<string, object> o))
            {
                list.Add("run config root must be an object.");
                return null;
            }

            var cfg = new RunConfig
            {
                Rooms = Int(o, "rooms", 8),
                MinDistinctRoom = Int(o, "minDistinctRoom", 3),
                SafeCardRoom = Int(o, "safeCardRoom", 4),
                DoorHpCap = Int(o, "doorHpCap", 100),
                DoorCellCap = Int(o, "doorCellCap", 3),
                CurseCapParty = Int(o, "curseCapParty", 2),
                CurseCapHero = Int(o, "curseCapHero", 1),
                HpRampPerRoom = Int(o, "hpRampPerRoom", 60),
                DamageRampPerRoom = Int(o, "damageRampPerRoom", 40),
                OfferSize = Int(o, "offerSize", 3),
                StartGold = Int(o, "startGold", 5),
            };

            var tiers = new List<TierKind>();
            foreach (object t in Arr(o, "tiers"))
            {
                if (t is string s && Enum.TryParse(s, out TierKind k)) tiers.Add(k);
                else list.Add($"tiers: unknown tier '{t}'.");
            }

            cfg.Tiers = tiers;
            var thresholds = new List<int>();
            foreach (object v in Arr(o, "dangerThresholds")) thresholds.Add(v is long l ? (int)l : 0);
            if (thresholds.Count > 0) cfg.DangerThresholds = thresholds;

            foreach (object e in Arr(o, "tierInfo"))
            {
                if (!(e is Dictionary<string, object> d) || !Enum.TryParse(Str(d, "tier"), out TierKind tier))
                {
                    list.Add("tierInfo: entry without a known tier.");
                    continue;
                }

                cfg._minRoom[tier] = Int(d, "minRoom", int.MaxValue);
                cfg._buffs[tier] = new DoorBuff(Int(d, "buffHp", 0), Int(d, "buffCells", 0));
                cfg._kicks[tier] = (Int(d, "hpKick", 0), Int(d, "damageKick", 0));
                cfg._gold[tier] = Int(d, "gold", 0);
                cfg._upgradeCost[tier] = Int(d, "upgradeCost", 0);
                var row = new List<int>();
                foreach (object v in Arr(d, "itemRow")) row.Add(v is long l ? (int)l : 0);
                if (row.Count > 0) cfg._itemRows[tier] = row.ToArray();
            }

            foreach (object e in Arr(o, "tierWeights"))
            {
                if (!(e is Dictionary<string, object> d)) continue;
                var w = new List<int>();
                foreach (object v in Arr(d, "weights")) w.Add(v is long l ? (int)l : 0);
                cfg._weights[Int(d, "room", 0)] = w.ToArray();
            }

            var curses = new List<CurseDef>();
            foreach (object e in Arr(o, "curses"))
            {
                if (!(e is Dictionary<string, object> d)) continue;
                if (!Enum.TryParse(Str(d, "kind"), out CurseKind kind))
                {
                    list.Add($"curse '{Str(d, "id")}': unknown kind '{Str(d, "kind")}'.");
                    continue;
                }

                Enum.TryParse(Str(d, "riderKind"), out CurseRiderKind rider);
                curses.Add(new CurseDef(Str(d, "id"), kind, Int(d, "p1", 0), rider, Int(d, "riderAmount", 0), Str(d, "itemId"),
                    Str(d, "nameVi"), Str(d, "nameEn"), Str(d, "lineVi"), Str(d, "lineEn")));
            }

            cfg.Curses = curses;

            var templates = new List<EncounterTemplate>();
            foreach (object e in Arr(o, "templates"))
            {
                if (!(e is Dictionary<string, object> d)) continue;
                EncounterTemplate t = ReadTemplate(d, list);
                if (t != null) templates.Add(t);
            }

            templates.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));   // candidates in ascending id ordinal (System 16 Rule 7)
            cfg.Templates = templates;
            if (o.TryGetValue("boss", out object bossObj) && bossObj is Dictionary<string, object> bd)
            {
                cfg.Boss = ReadTemplate(bd, list);
            }

            foreach (object v in Arr(o, "nonDroppable"))
            {
                if (v is string s) cfg._nonDroppable.Add(s);
            }

            cfg.Validate(catalog, list);
            return list.Count == 0 ? cfg : null;
        }

        private static EncounterTemplate ReadTemplate(Dictionary<string, object> d, List<string> errors)
        {
            string id = Str(d, "id");
            TierKind tier = TierKind.White;
            string tierText = Str(d, "tier");
            if (!string.IsNullOrEmpty(tierText) && !Enum.TryParse(tierText, out tier))
            {
                errors.Add($"template '{id}': unknown tier '{tierText}'.");
                return null;
            }

            var roster = new List<string>();
            foreach (object m in Arr(d, "roster"))
            {
                if (m is string s) roster.Add(s);
            }

            return new EncounterTemplate(id, tier, Int(d, "roomMin", 1), Int(d, "roomMax", 1), Int(d, "weight", 1), roster, Str(d, "curseId"));
        }

        private void Validate(GameCatalog catalog, List<string> errors)
        {
            if (Tiers.Count < 2) errors.Add("tiers: at least two tiers are needed.");
            for (int r = 2; r <= Rooms; r++)
            {
                if (!_weights.TryGetValue(r, out int[] w))
                {
                    errors.Add($"tierWeights: room {r} has no row.");
                    continue;
                }

                if (w.Length != Tiers.Count) errors.Add($"tierWeights: room {r} has {w.Length} weights for {Tiers.Count} tiers.");
                int sum = 0, positive = 0, safe = 0;
                for (int i = 0; i < w.Length && i < Tiers.Count; i++)
                {
                    if (w[i] < 0) errors.Add($"tierWeights: room {r} has a negative weight.");
                    sum += w[i];
                    if (w[i] > 0) positive++;
                    if (w[i] > 0 && r < MinRoomFor(Tiers[i])) errors.Add($"tierWeights: room {r} gives {Tiers[i]} a weight before its first room.");
                    if (w[i] > 0 && (Tiers[i] == TierKind.White || Tiers[i] == TierKind.Green)) safe += w[i];
                    if (w[i] > 0 && FindCell(r, Tiers[i]).Count == 0) errors.Add($"templates: no template for room {r}, {Tiers[i]}.");
                }

                if (sum != 1000) errors.Add($"tierWeights: room {r} sums to {sum}, not 1000.");
                if (positive < 2) errors.Add($"tierWeights: room {r} has fewer than two positive tiers.");
                if (r >= SafeCardRoom && safe <= 0) errors.Add($"tierWeights: room {r} has no White or Green weight.");
            }

            if (FindCell(1, TierKind.White).Count == 0) errors.Add("templates: room 1 needs a White template.");
            int lastHp = int.MinValue;
            foreach (TierKind t in Tiers)
            {
                DoorBuff b = BuffOf(t);
                if (b.Hp < lastHp) errors.Add($"tierInfo: the {t} buff is smaller than a lower tier's.");
                lastHp = b.Hp;
            }

            if (MinDistinctRoom < 2 || MinDistinctRoom > 8 || SafeCardRoom < 2 || SafeCardRoom > 8) errors.Add("knobs: MinDistinctRoom and SafeCardRoom must be in 2..8.");
            if (DoorHpCap < 1 || DoorCellCap < 1 || CurseCapParty < 1 || CurseCapHero < 1) errors.Add("knobs: caps must be at least 1.");
            if (Boss == null || Boss.Roster.Count != 1) errors.Add("boss: exactly one boss alone is needed.");

            foreach (EncounterTemplate t in Templates)
            {
                if (t.Roster.Count < 1 || t.Roster.Count > 6) errors.Add($"template '{t.Id}': roster size {t.Roster.Count} is outside 1..6.");
                if (t.Weight < 1) errors.Add($"template '{t.Id}': weight must be at least 1.");
                if (t.Tier == TierKind.Gold && FindCurse(t.CurseId) == null) errors.Add($"template '{t.Id}': a Gold template needs a known curseId.");
                if (t.Tier != TierKind.Gold && !string.IsNullOrEmpty(t.CurseId)) errors.Add($"template '{t.Id}': only Gold carries a curse.");
                CheckRoster(t, catalog, errors);
            }

            if (Boss != null) CheckRoster(Boss, catalog, errors);
            DoorBuff gold = BuffOf(TierKind.Gold);
            foreach (CurseDef c in Curses)
            {
                if (c.P1 <= 0) errors.Add($"curse '{c.Id}': p1 must be positive.");
                if (c.Kind == CurseKind.Frailty && c.P1 <= gold.Hp) errors.Add($"curse '{c.Id}': Frailty must exceed the Gold buff's HP.");
                if (c.Kind == CurseKind.Pinned && (catalog == null || catalog.FindItem(c.ItemId) == null)) errors.Add($"curse '{c.Id}': pinned item '{c.ItemId}' is not in the catalog.");
            }
        }

        private static void CheckRoster(EncounterTemplate t, GameCatalog catalog, List<string> errors)
        {
            if (catalog == null) return;
            foreach (string id in t.Roster)
            {
                if (catalog.FindMonster(id) == null) errors.Add($"template '{t.Id}': unknown monster '{id}'.");
            }
        }

        /// <summary>The templates eligible for (room, tier), in ascending id ordinal.</summary>
        public List<EncounterTemplate> FindCell(int room, TierKind tier)
        {
            var cell = new List<EncounterTemplate>();
            foreach (EncounterTemplate t in Templates)
            {
                if (t.Tier == tier && room >= t.RoomMin && room <= t.RoomMax) cell.Add(t);
            }

            return cell;
        }

        private static IEnumerable<object> Arr(Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out object v) && v is List<object> l ? l : (IEnumerable<object>)Array.Empty<object>();

        private static int Int(Dictionary<string, object> o, string key, int fallback) =>
            o != null && o.TryGetValue(key, out object v) && v is long l ? (int)l : fallback;

        private static string Str(Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out object v) && v is string s ? s : string.Empty;
    }
}
