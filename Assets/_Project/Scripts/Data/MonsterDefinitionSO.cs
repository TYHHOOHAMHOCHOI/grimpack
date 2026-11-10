using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Core;

namespace Project05.Data
{
    [Serializable]
    public class MonsterMoveDefinition
    {
        public string MoveName;
        public IntentType IntentType;
        public int BaseDamage;
        public int ShieldGain;
        public int StatusStacks;
        public StatusType StatusToApply;
        public int SpeedBonus;
        public bool TargetsBacklineFirst;

        public MonsterMoveDefinition(
            string moveName,
            IntentType intentType,
            int baseDamage = 0,
            int shieldGain = 0,
            StatusType statusType = StatusType.Poison,
            int statusStacks = 0,
            int speedBonus = 0,
            bool targetsBackline = false)
        {
            MoveName = moveName;
            IntentType = intentType;
            BaseDamage = baseDamage;
            ShieldGain = shieldGain;
            StatusToApply = statusType;
            StatusStacks = statusStacks;
            SpeedBonus = speedBonus;
            TargetsBacklineFirst = targetsBackline;
        }
    }

    [CreateAssetMenu(fileName = "NewMonsterDefinition", menuName = "Project05/Data/Monster Definition")]
    public class MonsterDefinitionSO : ScriptableObject
    {
        public string MonsterId;
        public string MonsterName;
        public string Subtitle;
        public bool IsElite;
        public bool IsBoss;
        public string SpriteName;

        public int MaxHP = 65;
        public int BaseSpeed = 18;
        public int GoldReward = 18;
        public List<MonsterMoveDefinition> Moveset = new List<MonsterMoveDefinition>();

        public static MonsterDefinitionSO CreateRuntime(
            string id,
            string monsterName,
            string subtitle,
            bool isElite,
            bool isBoss,
            string spriteName,
            int maxHp,
            int baseSpeed,
            int goldReward,
            params MonsterMoveDefinition[] moves)
        {
            MonsterDefinitionSO def = CreateInstance<MonsterDefinitionSO>();
            def.name = id;
            def.MonsterId = id;
            def.MonsterName = monsterName;
            def.Subtitle = subtitle;
            def.IsElite = isElite;
            def.IsBoss = isBoss;
            def.SpriteName = spriteName;
            def.MaxHP = maxHp;
            def.BaseSpeed = baseSpeed;
            def.GoldReward = goldReward;
            def.Moveset = new List<MonsterMoveDefinition>(moves ?? new MonsterMoveDefinition[0]);
            return def;
        }
    }
}
