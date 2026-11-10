using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;

namespace Project05.Progression
{
    /// <summary>
    /// Generates 3 distinct DoorCard choices after clearing a room.
    /// Scales tier probabilities by floor and ensures higher-tier doors grant stronger buffs
    /// while leading into correspondingly tougher rooms (Elite/Boss/High-multiplier Combat) or tactical rooms (Shop/Recruit/Event).
    /// </summary>
    public static class DoorGenerator
    {
        public static float GetBaseDifficultyForTier(CardTier tier, int currentFloor)
        {
            float floorScale = 1f + Mathf.Max(0, currentFloor - 1) * 0.14f;
            float tierMultiplier;
            switch (tier)
            {
                case CardTier.White: tierMultiplier = 1.00f; break;
                case CardTier.Green: tierMultiplier = 1.15f; break;
                case CardTier.Blue: tierMultiplier = 1.32f; break;
                case CardTier.Purple: tierMultiplier = 1.55f; break;
                case CardTier.Gold: tierMultiplier = 1.80f; break;
                case CardTier.Red: tierMultiplier = 2.15f; break;
                default: tierMultiplier = 1.00f; break;
            }
            return Mathf.Round(floorScale * tierMultiplier * 100f) / 100f;
        }

        public static int GetBonusGoldForTier(CardTier tier, int currentFloor)
        {
            int baseGold = 8 + currentFloor * 3;
            return baseGold + ((int)tier * 7);
        }

        public static List<DoorCard> GenerateThreeDoors(int currentFloor, int currentPartyCount, int maxPartySize = 3, System.Random rng = null)
        {
            DefaultGameCatalog.EnsureInitialized();
            rng ??= new System.Random(Environment.TickCount ^ (currentFloor * 7919));

            List<DoorCard> doors = new List<DoorCard>(3);

            // Door 0: Lower/Safe tier, Door 1: Mid tier, Door 2: High Risk / High Reward tier
            CardTier tier0 = RollTierForSlot(0, currentFloor, rng);
            if (currentPartyCount < maxPartySize && tier0 > CardTier.Blue)
            {
                tier0 = CardTier.Blue;
            }
            CardTier tier1 = RollTierForSlot(1, currentFloor, rng);
            CardTier tier2 = RollTierForSlot(2, currentFloor, rng);

            // Ensure variety across the 3 doors
            if (tier1 <= tier0 && tier0 < CardTier.Red)
            {
                tier1 = (CardTier)Mathf.Min((int)CardTier.Red, (int)tier0 + 1);
            }
            if (tier2 <= tier1 && tier1 < CardTier.Red)
            {
                tier2 = (CardTier)Mathf.Min((int)CardTier.Red, (int)tier1 + 1);
            }

            CardTier[] tiers = { tier0, tier1, tier2 };

            for (int i = 0; i < 3; i++)
            {
                CardTier t = tiers[i];
                RoomType roomType = DetermineRoomTypeForDoor(i, t, currentFloor, currentPartyCount, maxPartySize, rng);
                BuffDefinitionSO buff = PickRandomBuffForTier(t, rng);

                doors.Add(new DoorCard
                {
                    DoorIndex = i,
                    Tier = t,
                    NextRoomType = roomType,
                    BuffReward = buff,
                    DifficultyMultiplier = GetBaseDifficultyForTier(t, currentFloor),
                    BonusGoldReward = GetBonusGoldForTier(t, currentFloor)
                });
            }

            return doors;
        }

        private static CardTier RollTierForSlot(int slotIndex, int floor, System.Random rng)
        {
            int roll = rng.Next(0, 100) + Mathf.Clamp(floor * 5, 0, 35) + (slotIndex * 18);

            if (roll < 35) return CardTier.White;
            if (roll < 62) return CardTier.Green;
            if (roll < 82) return CardTier.Blue;
            if (roll < 96) return CardTier.Purple;
            if (roll < 110) return CardTier.Gold;
            return CardTier.Red;
        }

        private static RoomType DetermineRoomTypeForDoor(
            int slotIndex,
            CardTier tier,
            int floor,
            int currentPartyCount,
            int maxPartySize,
            System.Random rng)
        {
            // High-tier cards (Purple, Gold, Red) must ALWAYS lead into dangerous Elite or Boss rooms!
            if (tier == CardTier.Red)
            {
                return RoomType.Boss;
            }

            if (tier == CardTier.Gold)
            {
                return rng.Next(0, 100) < 55 ? RoomType.Boss : RoomType.Elite;
            }

            if (tier == CardTier.Purple)
            {
                return rng.Next(0, 100) < 35 ? RoomType.Boss : RoomType.Elite;
            }

            // Milestone Boss floor on Door 2
            if (floor > 0 && floor % 5 == 0 && slotIndex == 2)
            {
                return RoomType.Boss;
            }

            // When party has fewer than maxPartySize (starts at 1 hero), guarantee Door 0 offers Ally Recruitment!
            if (currentPartyCount < maxPartySize && slotIndex == 0)
            {
                return RoomType.Recruit;
            }

            // Blue (Rare) tier leads into Elite or hardened Combat
            if (tier == CardTier.Blue)
            {
                return slotIndex >= 1 ? RoomType.Elite : RoomType.Combat;
            }

            // Lower tiers (White / Green) on Slot 0 & Slot 1 can offer Shop, Event, Recruit/Training, or Standard Combat
            if (slotIndex == 1)
            {
                int r = rng.Next(0, 100);
                if (r < 35) return RoomType.Shop;
                if (r < 60) return RoomType.Event;
                return RoomType.Combat;
            }

            if (slotIndex == 0)
            {
                int r = rng.Next(0, 100);
                if (r < 30) return RoomType.Shop;
                if (r < 50) return RoomType.Event;
                if (r < 65) return RoomType.Recruit;
                return RoomType.Combat;
            }

            return RoomType.Combat;
        }

        private static BuffDefinitionSO PickRandomBuffForTier(CardTier tier, System.Random rng)
        {
            List<BuffDefinitionSO> pool = DefaultGameCatalog.GetBuffsByTier(tier);
            if (pool == null || pool.Count == 0) return DefaultGameCatalog.AllBuffs[0];
            return pool[rng.Next(0, pool.Count)];
        }
    }
}
