using System.Collections.Generic;
using UnityEngine;
using Project05.Core;

namespace Project05.Data
{
    [CreateAssetMenu(fileName = "NewHeroDefinition", menuName = "Project05/Data/Hero Definition")]
    public class HeroDefinitionSO : ScriptableObject
    {
        public string HeroId;
        public string HeroName;
        public string RoleTitle;
        [TextArea(2, 3)] public string LoreDescription;
        public PartyPosition PreferredPosition;
        public string SpriteName;

        [Header("Base Combat Stats")]
        public int MaxHP = 90;
        public int BaseSpeed = 22;
        public int StartingShield = 0;

        [Header("Personal Backpack Configuration")]
        public int BackpackWidth = 5;
        public int BackpackHeight = 4;
        public int InitialUnlockedRows = 3;
        public int InitialUnlockedCols = 4;
        public List<string> StartingItemIds = new List<string>();

        public static HeroDefinitionSO CreateRuntime(
            string id,
            string heroName,
            string roleTitle,
            string lore,
            PartyPosition preferredPos,
            string spriteName,
            int maxHp,
            int baseSpeed,
            int startingShield,
            int bagWidth,
            int bagHeight,
            int unlockedCols,
            int unlockedRows,
            params string[] startingItems)
        {
            HeroDefinitionSO def = CreateInstance<HeroDefinitionSO>();
            def.name = id;
            def.HeroId = id;
            def.HeroName = heroName;
            def.RoleTitle = roleTitle;
            def.LoreDescription = lore;
            def.PreferredPosition = preferredPos;
            def.SpriteName = spriteName;
            def.MaxHP = maxHp;
            def.BaseSpeed = baseSpeed;
            def.StartingShield = startingShield;
            def.BackpackWidth = bagWidth;
            def.BackpackHeight = bagHeight;
            def.InitialUnlockedCols = unlockedCols;
            def.InitialUnlockedRows = unlockedRows;
            def.StartingItemIds = new List<string>(startingItems ?? new string[0]);
            return def;
        }
    }
}
