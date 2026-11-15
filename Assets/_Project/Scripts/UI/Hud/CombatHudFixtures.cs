using System.Collections.Generic;

namespace Project05.UI.Hud
{
    /// <summary>Preview states for the combat HUD scene.</summary>
    public enum HudFixture { Planning, PartyActing, EnemyActing, SixEnemies }

    /// <summary>
    /// Hand-written HUD states that match the mock-ups in production/qa/evidence. They stand in for the combat
    /// presentation (Systems 27 and 29), which does not exist yet, so the scene can be checked on its own.
    /// </summary>
    public static class CombatHudFixtures
    {
        private const int Swarmer = 0;
        private const int Brute = 1;

        /// <summary>Builds a fixture.</summary>
        public static CombatHudData Create(HudFixture fixture)
        {
            CombatHudData d = Baseline();
            switch (fixture)
            {
                case HudFixture.PartyActing:
                    d.SpeedIndex = 0;
                    d.Order = Order(H(2), E(IntentKind.Multi), H(3), E(IntentKind.Attack), H(1), E(IntentKind.Multi));
                    HeroSlotData acting = d.Heroes[1];
                    acting.Pose = HeroPose.Idle2;
                    acting.Ready = true;
                    acting.Acting = true;
                    acting.Avatars.Add(new AvatarData { Tier = Tier.Green, Icon = GlyphKind.Attack, Bright = true });
                    acting.Avatars.Add(new AvatarData { Tier = Tier.Blue, Icon = GlyphKind.Speed });
                    d.Enemies[0].Hp = 40;
                    d.Enemies[1].EyesLit = true;
                    d.Readouts.Add(new ReadoutData { OnHero = false, Slot = 0, Text = "12", Cut = 16 });
                    break;
                case HudFixture.EnemyActing:
                    d.SpeedIndex = 1;
                    d.Order = Order(E(IntentKind.Attack), H(1), E(IntentKind.Multi), H(2), E(IntentKind.Attack), H(3));
                    d.Heroes[0].Pose = HeroPose.Hurt1;
                    d.Heroes[0].Hp = 31;
                    d.Heroes[2].Fallen = true;
                    d.Heroes[2].Hp = 0;
                    EnemySlotData brute = d.Enemies[1];
                    brute.Pose = MonsterPose.Act;
                    brute.EyesLit = true;
                    brute.Acting = true;
                    brute.IntentAway = true;
                    d.Readouts.Add(new ReadoutData { OnHero = true, Slot = 0, Text = "18", Taken = true, Cut = 16 });
                    break;
                case HudFixture.SixEnemies:
                    d.Heroes[1].Statuses.Add(new StatusChipData { Kind = StatusKind.Seal, Amount = 12 });
                    d.Enemies[2] = Enemy(Swarmer, 52, IntentKind.Multi, "4x3", 3);
                    d.Enemies[3] = Enemy(Swarmer, 31, IntentKind.Multi, "4x3", 2);
                    d.Enemies[3].Statuses.Add(new StatusChipData { Kind = StatusKind.Seal, Amount = 4 });
                    d.Enemies[4] = Enemy(Swarmer, 52, IntentKind.Multi, "4x3", 1);
                    d.Enemies[5] = Enemy(Swarmer, 52, IntentKind.Multi, "4x3", 3);
                    d.Enemies[1].Statuses.Add(new StatusChipData { Kind = StatusKind.Droplet, Amount = 5 });
                    break;
            }

            return d;
        }

        private static CombatHudData Baseline()
        {
            var d = new CombatHudData { Currency = 120, Room = 3, RoomCount = 8 };
            d.Heroes[0] = Hero(1, 86, 110, 4);
            d.Heroes[1] = Hero(2, 64, 92, 0);
            d.Heroes[1].Statuses.Add(new StatusChipData { Kind = StatusKind.Droplet, Amount = 3 });
            d.Heroes[2] = Hero(3, 82, 82, 0);
            d.Enemies[0] = Enemy(Swarmer, 52, IntentKind.Multi, "4x3", 1);
            d.Enemies[1] = Enemy(Brute, 88, IntentKind.Attack, "18", 2);
            d.Enemies[1].Statuses.Add(new StatusChipData { Kind = StatusKind.Seal, Amount = 2 });
            for (int i = 2; i < d.Enemies.Length; i++) d.Enemies[i] = new EnemySlotData { Present = false };
            d.Order = Order(H(1), E(IntentKind.Multi), H(2), E(IntentKind.Attack), H(3), E(IntentKind.Multi));
            return d;
        }

        private static HeroSlotData Hero(int join, int hp, int max, int armour)
        {
            return new HeroSlotData { JoinNumeral = join, Hp = hp, MaxHp = max, Armour = armour, SpeedGauge = 14 - 3 * (join - 1) };
        }

        private static EnemySlotData Enemy(int visual, int hp, IntentKind intent, string magnitude, int target)
        {
            return new EnemySlotData
            {
                Present = true, Visual = visual, Hp = hp, MaxHp = visual == Swarmer ? 52 : 88, SpeedGauge = 12,
                Intent = intent, Magnitude = magnitude, TargetJoinNumeral = target,
            };
        }

        private static OrderEntryData H(int join) => new OrderEntryData { IsHero = true, JoinNumeral = join };

        private static OrderEntryData E(IntentKind intent) => new OrderEntryData { IsHero = false, Intent = intent };

        private static List<OrderEntryData> Order(params OrderEntryData[] entries) => new List<OrderEntryData>(entries);
    }
}
