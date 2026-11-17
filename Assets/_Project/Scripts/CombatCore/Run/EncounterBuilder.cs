using System;
using System.Collections.Generic;

namespace Project05.CombatCore.Run
{
    /// <summary>
    /// The encounter behind a door or the current room (System 16 Rule 8): roster, multipliers, threat, danger and the
    /// class counts shown on the card. Immutable.
    /// </summary>
    public sealed class EncounterSpec
    {
        public string TemplateId { get; }
        public int Room { get; }
        public TierKind Tier { get; }
        public bool IsBoss { get; }
        public IReadOnlyList<string> Roster { get; }
        public int HpPermille { get; }
        public int DamagePermille { get; }
        public long Threat { get; }
        /// <summary>1..5 (Formula 3).</summary>
        public int Danger { get; }
        public int SmallCount { get; }
        public int MediumCount { get; }
        public int LargeCount { get; }
        public int EliteCount { get; }
        /// <summary>The curse of a Gold template, or empty.</summary>
        public string CurseId { get; }

        public EncounterSpec(string templateId, int room, TierKind tier, bool isBoss, IReadOnlyList<string> roster,
            int hpPermille, int damagePermille, long threat, int danger, int small, int medium, int large, int elite, string curseId)
        {
            TemplateId = templateId ?? string.Empty;
            Room = room;
            Tier = tier;
            IsBoss = isBoss;
            Roster = roster ?? Array.Empty<string>();
            HpPermille = hpPermille;
            DamagePermille = damagePermille;
            Threat = threat;
            Danger = danger;
            SmallCount = small;
            MediumCount = medium;
            LargeCount = large;
            EliteCount = elite;
            CurseId = curseId ?? string.Empty;
        }

        /// <summary>The spawn inputs for System 14's binding, in roster (slot) order.</summary>
        public List<EnemySpawnInput> BuildSpawns(GameCatalog catalog)
        {
            var spawns = new List<EnemySpawnInput>(Roster.Count);
            for (int slot = 0; slot < Roster.Count; slot++)
            {
                MonsterDef def = catalog?.FindMonster(Roster[slot]);
                if (def != null) spawns.Add(new EnemySpawnInput(slot, def, HpPermille, DamagePermille));
            }

            return spawns;
        }
    }

    /// <summary>
    /// System 16 as pure functions: multipliers (Formula 1), threat and danger (Formula 3) and the spec of a template.
    /// Holds no state and makes no draw; the caller draws the variant.
    /// </summary>
    public static class EncounterBuilder
    {
        /// <summary>Party size the threat is computed against, so the rating does not depend on the run (Formula 3).</summary>
        public const int ThreatPartySize = 3;

        /// <summary>m_hp and m_dmg in permille for room r and tier t; the boss uses 1000 / 1000.</summary>
        public static (int hp, int dmg) Multipliers(RunConfig cfg, int room, TierKind tier, bool boss)
        {
            if (boss) return (1000, 1000);
            (int kickHp, int kickDmg) = cfg.KickOf(tier);
            int r = Math.Max(1, room);
            return (1000 + cfg.HpRampPerRoom * (r - 1) + kickHp, 1000 + cfg.DamageRampPerRoom * (r - 1) + kickDmg);
        }

        /// <summary>Round-half-up permille scaling, as System 14 does.</summary>
        public static int Scale(int value, int permille) => (int)(((long)value * permille + 500L) / 1000L);

        /// <summary>threat_e = ceil(ScaledHP x N_D x v / (1000 x W)) with N_D the weighted scaled damage per action.</summary>
        public static long ThreatOf(MonsterDef def, int hpPermille, int dmgPermille)
        {
            if (def == null) return 0;
            long nd = 0, w = 0;
            foreach (MonsterMoveDef m in def.Moves)
            {
                // a linked follow-up (weight 0, e.g. the heavy after a windup) happens as often as the move that links to it
                int weight = m.Weight;
                if (weight <= 0)
                {
                    foreach (MonsterMoveDef src in def.Moves)
                    {
                        if (string.Equals(src.LinkedNextMoveId, m.Id, StringComparison.Ordinal)) weight += Math.Max(0, src.Weight);
                    }
                }

                if (weight <= 0) continue;
                int hits = Math.Max(0, m.HitCount);
                int b = Scale(Math.Max(0, m.BaseDamage), dmgPermille);
                int targets = m.TargetMode == MoveTargetMode.AllHeroes ? ThreatPartySize : 1;
                nd += (long)weight * hits * b * targets;
                w += weight;
            }

            if (w <= 0) return 0;
            long scaledHp = Scale(def.BaseMaxHp, hpPermille);
            long num = scaledHp * nd * def.BaseSpeed;
            long den = 1000L * w;
            return (num + den - 1) / den;
        }

        /// <summary>danger = 1 + |{ k : T >= theta_k }|.</summary>
        public static int DangerOf(RunConfig cfg, long threat)
        {
            int d = 1;
            foreach (int t in cfg.DangerThresholds)
            {
                if (threat >= t) d++;
            }

            return d;
        }

        /// <summary>The spec of a template at room r (System 16 Rule 8).</summary>
        public static EncounterSpec Spec(RunConfig cfg, GameCatalog catalog, EncounterTemplate template, int room, bool boss)
        {
            TierKind tier = template.Tier;
            (int hp, int dmg) = Multipliers(cfg, room, tier, boss);
            long threat = 0;
            int small = 0, medium = 0, large = 0, elite = 0;
            foreach (string id in template.Roster)
            {
                MonsterDef def = catalog?.FindMonster(id);
                if (def == null) continue;
                threat += ThreatOf(def, hp, dmg);
                if (def.Classification == MonsterClassKind.Elite) elite++;
                switch (def.SizeClass)
                {
                    case MonsterSizeKind.Small: small++; break;
                    case MonsterSizeKind.Medium: medium++; break;
                    default: large++; break;
                }
            }

            return new EncounterSpec(template.Id, room, tier, boss, template.Roster, hp, dmg, threat, DangerOf(cfg, threat),
                small, medium, large, elite, template.CurseId);
        }

        /// <summary>
        /// Draws the variant of cell (room, tier) with one call on the stream (a bound-1 call when the cell has one
        /// template) and returns its spec, or null when the cell is empty.
        /// </summary>
        public static EncounterSpec Draw(RunConfig cfg, GameCatalog catalog, int room, TierKind tier, RngStream doors)
        {
            List<EncounterTemplate> cell = cfg.FindCell(room, tier);
            if (cell.Count == 0) return null;
            int index;
            if (cell.Count == 1)
            {
                index = doors.NextBounded(1);
            }
            else
            {
                var weights = new int[cell.Count];
                for (int i = 0; i < cell.Count; i++) weights[i] = Math.Max(1, cell[i].Weight);
                index = doors.NextWeighted(weights);
            }

            return Spec(cfg, catalog, cell[index], room, false);
        }
    }
}
