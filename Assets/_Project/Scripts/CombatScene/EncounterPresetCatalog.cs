using System;
using System.Collections.Generic;
using Project05.CombatCore;
using UnityEngine;

namespace Project05.CombatScene
{
    /// <summary>
    /// Reads <c>encounterPresets</c> and <c>tierTable</c> from <c>monsters_mvp.json</c> and builds
    /// deterministic <see cref="EnemySpawnInput"/> lists for 1..6 monsters at any stage number.
    /// </summary>
    public sealed class EncounterPresetCatalog
    {
        [Serializable]
        private sealed class MonstersExtraFile
        {
            public TierTableData tierTable = new TierTableData();
            public EncounterPresetData[] encounterPresets = Array.Empty<EncounterPresetData>();
        }

        [Serializable]
        private sealed class TierTableData
        {
            public int hpRampPerRoom = 60;
            public int damageRampPerRoom = 40;
            public TierScalingData[] tiers = Array.Empty<TierScalingData>();
        }

        [Serializable]
        private sealed class TierScalingData
        {
            public string tier = "White";
            public int minRoom = 1;
            public int hpKick;
            public int damageKick;
        }

        [Serializable]
        private sealed class EncounterPresetData
        {
            public int monsterCount;
            public string[] roster = Array.Empty<string>();
        }

        private readonly MonstersExtraFile _data;

        public EncounterPresetCatalog(string monstersJson)
        {
            if (string.IsNullOrWhiteSpace(monstersJson))
            {
                _data = new MonstersExtraFile();
                return;
            }

            _data = JsonUtility.FromJson<MonstersExtraFile>(monstersJson) ?? new MonstersExtraFile();
        }

        public List<EnemySpawnInput> BuildSpawns(GameCatalog catalog, int monsterCount, int stageNumber)
        {
            int count = Mathf.Clamp(monsterCount, 1, 6);
            int stage = Mathf.Max(1, stageNumber);

            int hpKick = 0;
            int dmgKick = 0;
            if (_data.tierTable != null && _data.tierTable.tiers != null)
            {
                foreach (TierScalingData t in _data.tierTable.tiers)
                {
                    if (stage >= t.minRoom)
                    {
                        hpKick = t.hpKick;
                        dmgKick = t.damageKick;
                    }
                }
            }

            int hpRamp = _data.tierTable != null ? _data.tierTable.hpRampPerRoom : 60;
            int dmgRamp = _data.tierTable != null ? _data.tierTable.damageRampPerRoom : 40;
            int hpPermille = 1000 + hpRamp * (stage - 1) + hpKick;
            int dmgPermille = 1000 + dmgRamp * (stage - 1) + dmgKick;

            string[] roster = FindPresetRoster(count);
            var spawns = new List<EnemySpawnInput>(count);

            for (int slot = 0; slot < count; slot++)
            {
                string monId = slot < roster.Length ? roster[slot] : "mon_swarmer";
                MonsterDef def = catalog.FindMonster(monId);
                if (def == null && catalog.Monsters.Count > 0)
                {
                    def = catalog.Monsters[slot % catalog.Monsters.Count];
                }

                if (def != null)
                {
                    spawns.Add(new EnemySpawnInput(slot, def, hpPermille, dmgPermille));
                }
            }

            return spawns;
        }

        private string[] FindPresetRoster(int count)
        {
            if (_data.encounterPresets != null)
            {
                foreach (EncounterPresetData p in _data.encounterPresets)
                {
                    if (p != null && p.monsterCount == count && p.roster != null && p.roster.Length > 0)
                    {
                        return p.roster;
                    }
                }
            }

            switch (count)
            {
                case 1: return new[] { "mon_hunter" };
                case 2: return new[] { "mon_swarmer", "mon_brute" };
                case 3: return new[] { "mon_swarmer", "mon_hunter", "mon_brute" };
                case 4: return new[] { "mon_swarmer", "mon_striker", "mon_sweeper", "mon_brute" };
                case 5: return new[] { "mon_swarmer", "mon_hunter", "mon_striker", "mon_sweeper", "mon_brute" };
                default: return new[] { "mon_swarmer", "mon_swarmer", "mon_hunter", "mon_striker", "mon_sweeper", "mon_brute" };
            }
        }
    }
}
