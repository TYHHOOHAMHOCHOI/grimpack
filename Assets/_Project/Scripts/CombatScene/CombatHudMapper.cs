using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI.Hud;
using UnityEngine;

namespace Project05.CombatScene
{
    /// <summary>
    /// Pure mapper from <see cref="Project05.CombatCore"/> snapshots and events to <see cref="CombatHudData"/>.
    /// Maps 1-hero mode cleanly onto Slot 0 while keeping Slots 1-2 hidden (<c>null</c>).
    /// </summary>
    public static class CombatHudMapper
    {
        public static CombatHudData MapPlanning(
            GameCatalog catalog,
            HeroDef heroDef,
            ArrangementGridView heroGrid,
            GridSynergyView synergy,
            PreparedEncounter prepared,
            int room,
            int roomCount,
            int currency,
            int speedIndex)
        {
            var data = new CombatHudData
            {
                Currency = currency,
                Room = room,
                RoomCount = roomCount,
                SpeedIndex = speedIndex
            };

            if (heroDef != null)
            {
                int hpBonus = 0;
                int speedBonus = 0;
                if (heroGrid != null && heroGrid.Items != null)
                {
                    for (int i = 0; i < heroGrid.Items.Count; i++)
                    {
                        ItemDef item = catalog.FindItem(heroGrid.Items[i].DefinitionId);
                        if (item == null) continue;
                        hpBonus += item.HpBonus;
                        speedBonus += item.SpeedBonus;
                    }
                }

                int maxHp = Mathf.Max(1, heroDef.BaseMaxHp + hpBonus);
                data.Heroes[0] = new HeroSlotData
                {
                    JoinNumeral = 1,
                    Hp = maxHp,
                    MaxHp = maxHp,
                    Armour = heroDef.InitialShield,
                    SpeedGauge = 0,
                    Pose = HeroPose.Idle1,
                    Ready = false,
                    Acting = false,
                    Fallen = false
                };
            }

            if (prepared != null)
            {
                MapBoundEnemies(catalog, prepared.BoundEnemies, data);
                MapForecast(prepared.InitialForecast, prepared.BoundEnemies, null, data);
            }

            return data;
        }

        public static CombatHudData MapCombatSnapshot(
            GameCatalog catalog,
            PreparedEncounter prepared,
            CombatStateSnapshot snapshot,
            CombatEventRecord currentEvent,
            int room,
            int roomCount,
            int currency,
            int speedIndex,
            bool idleFrameToggle)
        {
            var data = new CombatHudData
            {
                Currency = currency,
                Room = room,
                RoomCount = roomCount,
                SpeedIndex = speedIndex
            };

            if (snapshot == null) return data;

            // Map Heroes (1-hero mode populates Slot 0, Slots 1-2 stay null unless present in multi-hero setup).
            for (int i = 0; i < snapshot.Heroes.Count && i < CombatHudData.HeroCount; i++)
            {
                HeroStateView hv = snapshot.Heroes[i];
                if (hv == null || hv.Slot < 0 || hv.Slot >= CombatHudData.HeroCount) continue;

                bool isActing = currentEvent != null &&
                                currentEvent.Actor.Side == UnitSide.Hero &&
                                currentEvent.Actor.Slot == hv.Slot &&
                                (currentEvent.Kind == CombatEventKind.ActionStarted ||
                                 currentEvent.Kind == CombatEventKind.ItemFired);

                bool tookHit = currentEvent != null &&
                               (currentEvent.Kind == CombatEventKind.Damage || currentEvent.Kind == CombatEventKind.DetonationDamage) &&
                               currentEvent.Target.Side == UnitSide.Hero &&
                               currentEvent.Target.Slot == hv.Slot;

                HeroPose pose = !hv.IsAlive
                    ? HeroPose.Down2
                    : (tookHit ? HeroPose.Hurt1 : (idleFrameToggle ? HeroPose.Idle2 : HeroPose.Idle1));

                var heroData = new HeroSlotData
                {
                    JoinNumeral = hv.Slot + 1,
                    Hp = hv.Hp,
                    MaxHp = Mathf.Max(1, hv.MaxHp),
                    Armour = hv.Shield,
                    SpeedGauge = GaugeToPixels(hv.Gauge),
                    Pose = pose,
                    Ready = hv.IsAlive && (hv.Gauge >= TurnOrderScheduler.GaugeThreshold || isActing),
                    Acting = isActing,
                    Fallen = !hv.IsAlive
                };

                AppendStatuses(hv.Statuses, heroData.Statuses);

                if (currentEvent != null &&
                    currentEvent.Kind == CombatEventKind.ItemFired &&
                    currentEvent.Origin != null &&
                    currentEvent.Origin.HeroSlot == hv.Slot)
                {
                    ItemDef firedDef = catalog != null ? catalog.FindItem(currentEvent.Origin.ItemDefId) : null;
                    heroData.Avatars.Add(new AvatarData
                    {
                        Tier = firedDef != null ? (Tier)(int)firedDef.Tier : Tier.White,
                        Icon = ResolveGlyphForItem(firedDef),
                        Bright = true
                    });
                }

                data.Heroes[hv.Slot] = heroData;
            }

            // Map Enemies (up to 6).
            for (int i = 0; i < snapshot.Enemies.Count && i < CombatHudData.EnemyCount; i++)
            {
                EnemyStateView ev = snapshot.Enemies[i];
                if (ev == null || ev.Slot < 0 || ev.Slot >= CombatHudData.EnemyCount) continue;

                MonsterDef mDef = catalog != null ? catalog.FindMonster(ev.DefinitionId) : null;
                int visual = (mDef != null && mDef.Visual == "brute") ? 1 : 0;

                bool isActing = currentEvent != null &&
                                currentEvent.Actor.Side == UnitSide.Enemy &&
                                currentEvent.Actor.Slot == ev.Slot &&
                                (currentEvent.Kind == CombatEventKind.ActionStarted ||
                                 currentEvent.Kind == CombatEventKind.Damage);

                bool tookHit = currentEvent != null &&
                               (currentEvent.Kind == CombatEventKind.Damage || currentEvent.Kind == CombatEventKind.DetonationDamage) &&
                               currentEvent.Target.Side == UnitSide.Enemy &&
                               currentEvent.Target.Slot == ev.Slot;

                MonsterPose mPose = !ev.IsAlive
                    ? MonsterPose.Die2
                    : (tookHit ? MonsterPose.Hurt : (isActing ? MonsterPose.Act : (idleFrameToggle ? MonsterPose.Idle2 : MonsterPose.Idle1)));

                BoundIntentEntry intent = ev.CurrentIntent;
                IntentKind intentKind = ev.IsAlive ? MapIntentKind(intent) : IntentKind.None;
                string mag = ev.IsAlive ? FormatIntentMagnitude(intent) : string.Empty;
                int targetNum = ev.IsAlive ? ResolveTargetNumeral(intent, snapshot.Heroes) : 0;

                var enemyData = new EnemySlotData
                {
                    Present = true,
                    Visual = visual,
                    Hp = ev.Hp,
                    MaxHp = Mathf.Max(1, ev.MaxHp),
                    Armour = ev.Shield,
                    SpeedGauge = GaugeToPixels(ev.Gauge),
                    Pose = mPose,
                    Intent = intentKind,
                    IntentAway = isActing,
                    Magnitude = mag,
                    TargetJoinNumeral = targetNum,
                    EyesLit = ev.IsAlive && (isActing || tookHit),
                    Acting = isActing && ev.IsAlive
                };

                AppendStatuses(ev.Statuses, enemyData.Statuses);
                data.Enemies[ev.Slot] = enemyData;
            }

            for (int i = 0; i < CombatHudData.EnemyCount; i++)
            {
                if (data.Enemies[i] == null) data.Enemies[i] = new EnemySlotData { Present = false };
            }

            MapForecast(snapshot.Forecast, prepared != null ? prepared.BoundEnemies : null, snapshot.Enemies, data);
            AppendReadouts(currentEvent, data.Readouts);
            return data;
        }

        private static void MapBoundEnemies(GameCatalog catalog, IReadOnlyList<BoundEnemyState> bound, CombatHudData data)
        {
            for (int i = 0; i < CombatHudData.EnemyCount; i++)
            {
                data.Enemies[i] = new EnemySlotData { Present = false };
            }

            if (bound == null) return;
            for (int i = 0; i < bound.Count; i++)
            {
                BoundEnemyState be = bound[i];
                if (be == null || be.Slot < 0 || be.Slot >= CombatHudData.EnemyCount) continue;

                MonsterDef mDef = catalog != null ? catalog.FindMonster(be.DefinitionId) : null;
                int visual = (mDef != null && mDef.Visual == "brute") ? 1 : 0;
                BoundIntentEntry opener = be.Sequence != null && be.Sequence.Count > 0 ? be.Sequence[0] : null;

                data.Enemies[be.Slot] = new EnemySlotData
                {
                    Present = true,
                    Visual = visual,
                    Hp = be.ScaledMaxHp,
                    MaxHp = Mathf.Max(1, be.ScaledMaxHp),
                    Armour = 0,
                    SpeedGauge = 0,
                    Pose = MonsterPose.Idle1,
                    Intent = MapIntentKind(opener),
                    Magnitude = FormatIntentMagnitude(opener),
                    TargetJoinNumeral = opener != null && opener.TargetMode == MoveTargetMode.AllHeroes ? 0 : 1
                };
            }
        }

        private static void MapForecast(
            IReadOnlyList<TurnForecastEntry> forecast,
            IReadOnlyList<BoundEnemyState> boundEnemies,
            IReadOnlyList<EnemyStateView> liveEnemies,
            CombatHudData data)
        {
            data.Order.Clear();
            if (forecast == null) return;

            int count = Mathf.Min(6, forecast.Count);
            for (int i = 0; i < count; i++)
            {
                TurnForecastEntry entry = forecast[i];
                if (entry.Unit.Side == UnitSide.Hero)
                {
                    data.Order.Add(new OrderEntryData
                    {
                        IsHero = true,
                        JoinNumeral = entry.Unit.Slot + 1
                    });
                }
                else
                {
                    IntentKind intent = IntentKind.Attack;
                    if (liveEnemies != null)
                    {
                        for (int e = 0; e < liveEnemies.Count; e++)
                        {
                            if (liveEnemies[e].Slot == entry.Unit.Slot)
                            {
                                intent = MapIntentKind(liveEnemies[e].CurrentIntent);
                                break;
                            }
                        }
                    }
                    else if (boundEnemies != null)
                    {
                        for (int e = 0; e < boundEnemies.Count; e++)
                        {
                            if (boundEnemies[e].Slot == entry.Unit.Slot && boundEnemies[e].Sequence.Count > 0)
                            {
                                intent = MapIntentKind(boundEnemies[e].Sequence[0]);
                                break;
                            }
                        }
                    }

                    if (entry.EnemyIntentGlyph == MoveGlyphKind.Windup) intent = IntentKind.Windup;
                    data.Order.Add(new OrderEntryData
                    {
                        IsHero = false,
                        Intent = intent
                    });
                }
            }
        }

        private static void AppendStatuses(IReadOnlyList<ActiveStatusView> src, List<StatusChipData> dst)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
            {
                if (src[i].TotalAmount <= 0) continue;
                dst.Add(new StatusChipData
                {
                    Kind = src[i].StatusId == "an" ? StatusKind.Seal : StatusKind.Droplet,
                    Amount = (int)Mathf.Min(int.MaxValue, src[i].TotalAmount)
                });
            }
        }

        private static void AppendReadouts(CombatEventRecord ev, List<ReadoutData> readouts)
        {
            if (ev == null) return;
            switch (ev.Kind)
            {
                case CombatEventKind.Damage:
                case CombatEventKind.DetonationDamage:
                    if (ev.ResolvedAmount > 0 && ev.Target.IsValid)
                    {
                        readouts.Add(new ReadoutData
                        {
                            OnHero = ev.Target.Side == UnitSide.Hero,
                            Slot = ev.Target.Slot,
                            Text = ev.ResolvedAmount.ToString(),
                            Taken = ev.Target.Side == UnitSide.Hero,
                            Cut = ev.Kind == CombatEventKind.DetonationDamage ? 24 : 16
                        });
                    }
                    break;
                case CombatEventKind.ShieldGained:
                case CombatEventKind.Healed:
                    if (ev.ResolvedAmount > 0 && ev.Target.IsValid)
                    {
                        readouts.Add(new ReadoutData
                        {
                            OnHero = ev.Target.Side == UnitSide.Hero,
                            Slot = ev.Target.Slot,
                            Text = ev.ResolvedAmount.ToString(),
                            Taken = false,
                            Cut = 12
                        });
                    }
                    break;
            }
        }

        public static IntentKind MapIntentKind(BoundIntentEntry intent)
        {
            if (intent == null) return IntentKind.None;
            if (intent.Glyph == MoveGlyphKind.Windup || intent.HitCount <= 0) return IntentKind.Windup;
            if (intent.TargetMode == MoveTargetMode.AllHeroes) return IntentKind.Sweep;
            if (intent.HitCount > 1) return IntentKind.Multi;
            return IntentKind.Attack;
        }

        public static string FormatIntentMagnitude(BoundIntentEntry intent)
        {
            if (intent == null || intent.HitCount <= 0) return string.Empty;
            if (intent.HitCount > 1) return intent.ScaledBaseDamage + "x" + intent.HitCount;
            return intent.ScaledBaseDamage.ToString();
        }

        private static int ResolveTargetNumeral(BoundIntentEntry intent, IReadOnlyList<HeroStateView> heroes)
        {
            if (intent == null || intent.HitCount <= 0 || intent.TargetMode == MoveTargetMode.AllHeroes) return 0;
            if (heroes != null)
            {
                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i].InstanceId == intent.BoundHeroInstanceId) return heroes[i].Slot + 1;
                }
            }
            return 1;
        }

        private static int GaugeToPixels(int gauge)
        {
            if (gauge <= 0) return 0;
            return Mathf.Clamp((gauge * UnitRowsView.GaugeWidth) / TurnOrderScheduler.GaugeThreshold, 1, UnitRowsView.GaugeWidth);
        }

        private static GlyphKind ResolveGlyphForItem(ItemDef item)
        {
            if (item == null || item.Effects == null || item.Effects.Count == 0) return GlyphKind.Speed;
            switch (item.Effects[0].Kind)
            {
                case EffectKind.ApplyStatus: return GlyphKind.Seal;
                case EffectKind.ConsumeDetonate: return GlyphKind.Damage;
                case EffectKind.ShieldGain: return GlyphKind.Shield;
                case EffectKind.Heal: return GlyphKind.Heart;
                default: return GlyphKind.Attack;
            }
        }
    }
}
