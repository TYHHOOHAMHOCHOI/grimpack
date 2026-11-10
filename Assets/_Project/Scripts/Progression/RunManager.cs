using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;
using Project05.Combat;

namespace Project05.Progression
{
    /// <summary>
    /// Central controller for a Roguelike Run:
    /// - Starts the run with 1 Hero ("khởi đầu với 1") in the Frontline slot.
    /// - Allows recruiting up to MaxPartySize = 3 Heroes (Frontline, Midline, Backline).
    /// - Maintains the persistent Shared Stash (Bottom-Left) and individual Hero Backpacks (Top-Left).
    /// - Drives transitions between Combat, 3-Door Selection (6 color tiers), Shop, Recruit, and Dark Events.
    /// </summary>
    public class RunManager
    {
        public const int StartingPartySize = 1;
        public const int MaxPartySize = 3;

        private readonly List<HeroCombatant> _party = new List<HeroCombatant>(MaxPartySize);
        private readonly List<MonsterCombatant> _currentMonsters = new List<MonsterCombatant>(3);
        private readonly List<BuffDefinitionSO> _activeBuffs = new List<BuffDefinitionSO>();
        private readonly List<DoorCard> _currentDoorChoices = new List<DoorCard>(3);
        private readonly List<HeroDefinitionSO> _recruitCandidates = new List<HeroDefinitionSO>(3);

        public IReadOnlyList<HeroCombatant> Party => _party;
        public IReadOnlyList<MonsterCombatant> CurrentMonsters => _currentMonsters;
        public IReadOnlyList<BuffDefinitionSO> ActiveBuffs => _activeBuffs;
        public IReadOnlyList<DoorCard> CurrentDoorChoices => _currentDoorChoices;
        public IReadOnlyList<HeroDefinitionSO> RecruitCandidates => _recruitCandidates;

        public BackpackGrid SharedStash { get; private set; }
        public CombatManager Combat { get; private set; } = new CombatManager();
        public ShopManager Shop { get; private set; } = new ShopManager();

        public RunState State { get; private set; }
        public RoomType CurrentRoomType { get; private set; }
        public int CurrentFloor { get; private set; }
        public int Gold { get; set; }
        public int SelectedHeroIndex { get; private set; }
        public float CurrentRoomDifficulty { get; private set; } = 1f;
        public string LatestBannerMessage { get; private set; }

        public HeroCombatant SelectedHero
        {
            get
            {
                if (_party.Count == 0) return null;
                int clamped = Mathf.Clamp(SelectedHeroIndex, 0, _party.Count - 1);
                return _party[clamped];
            }
        }

        public event Action OnRunStateChanged;
        public event Action<int> OnSelectedHeroChanged;
        public event Action<string> OnNotification;

        public RunManager()
        {
            Combat.OnCombatEnded += HandleCombatEnded;
        }

        /// <summary>
        /// Starts a brand new run with 1 Hero ("khởi đầu với 1") and a populated Shared Stash.
        /// </summary>
        public void StartNewRun()
        {
            DefaultGameCatalog.EnsureInitialized();

            _party.Clear();
            _currentMonsters.Clear();
            _activeBuffs.Clear();
            _currentDoorChoices.Clear();
            _recruitCandidates.Clear();

            CurrentFloor = 1;
            Gold = 35;
            SelectedHeroIndex = 0;
            CurrentRoomDifficulty = 1.0f;

            // Shared Stash (6x4 grid, all slots unlocked)
            SharedStash = new BackpackGrid("Kho Đồ Chung (Shared Stash)", 6, 4, 6, 4);
            SharedStash.TryAutoPlaceItem(new ItemData(DefaultGameCatalog.GetItemById("item_poison_flask")));
            SharedStash.TryAutoPlaceItem(new ItemData(DefaultGameCatalog.GetItemById("item_crimson_vial")));
            SharedStash.TryAutoPlaceItem(new ItemData(DefaultGameCatalog.GetItemById("item_curved_dagger")));

            // Start with 1 Hero in Frontline (Tiền Tuyến): Duskblade Crusader
            HeroDefinitionSO starterDef = DefaultGameCatalog.GetHeroById("hero_crusader");
            HeroCombatant starterHero = new HeroCombatant(starterDef, PartyPosition.Frontline, populateStartingItems: true);
            WireHeroBackpackEvents(starterHero);
            _party.Add(starterHero);

            // Enter Floor 1 Combat immediately
            EnterRoom(RoomType.Combat, 1.0f);
            Notify("Khởi đầu hành trình với 1 Nhân vật (Kỵ Sĩ Hắc Quang)! Chiêu mộ thêm đồng đội qua các Cánh Cửa.");
        }

        public bool SelectHero(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= _party.Count || _party[heroIndex] == null)
            {
                return false;
            }

            SelectedHeroIndex = heroIndex;
            _party[SelectedHeroIndex].RecalculateSynergies(_activeBuffs);
            OnSelectedHeroChanged?.Invoke(SelectedHeroIndex);
            OnRunStateChanged?.Invoke();
            return true;
        }

        public bool TryRecruitHero(HeroDefinitionSO heroDef, out string message)
        {
            if (heroDef == null)
            {
                message = "Nhân vật chiêu mộ không hợp lệ.";
                return false;
            }

            if (_party.Count >= MaxPartySize)
            {
                // Party already at max 3: grant training buff to all party members instead
                for (int i = 0; i < _party.Count; i++)
                {
                    _party[i].PermanentBonusMaxHp += 15;
                    _party[i].PersonalBackpack.UnlockNextLockedSlots(1);
                    _party[i].RecalculateSynergies(_activeBuffs);
                    _party[i].Heal(25);
                }
                message = "Đội hình đã đủ 3 người! Huấn luyện toàn đội: +15 Máu tối đa, hồi 25 HP & mở +1 Ô Balo!";
                PrepareDoorSelection(message);
                Notify(LatestBannerMessage);
                return true;
            }

            PartyPosition assignedPos = (PartyPosition)_party.Count;
            HeroCombatant newAlly = new HeroCombatant(heroDef, assignedPos, populateStartingItems: true);
            newAlly.RecalculateSynergies(_activeBuffs);
            WireHeroBackpackEvents(newAlly);
            _party.Add(newAlly);

            SelectedHeroIndex = _party.Count - 1;
            message = $"Đã chiêu mộ [{newAlly.DisplayName}] vào vị trí {GetPositionVietnamese(assignedPos)} ({_party.Count}/{MaxPartySize})!";
            OnSelectedHeroChanged?.Invoke(SelectedHeroIndex);
            PrepareDoorSelection(message);
            Notify(LatestBannerMessage);
            return true;
        }

        public void PrepareDoorSelection(string prefixNotification = null)
        {
            State = RunState.DoorSelection;
            _currentDoorChoices.Clear();
            List<DoorCard> generated = DoorGenerator.GenerateThreeDoors(CurrentFloor, _party.Count, MaxPartySize);
            _currentDoorChoices.AddRange(generated);
            string basePrompt = $"Tầng {CurrentFloor} hoàn tất! Hãy chọn 1 trong 3 Cánh Cửa:";
            LatestBannerMessage = string.IsNullOrEmpty(prefixNotification)
                ? basePrompt
                : $"{prefixNotification} • {basePrompt}";
            OnRunStateChanged?.Invoke();
        }

        /// <summary>
        /// Selects 1 of the 3 Doors: immediately applies its colored Buff reward, increments floor,
        /// and transitions into the room designated by the Door's top icon.
        /// </summary>
        public bool SelectDoor(int doorIndex)
        {
            if (State != RunState.DoorSelection || doorIndex < 0 || doorIndex >= _currentDoorChoices.Count)
            {
                return false;
            }

            DoorCard chosen = _currentDoorChoices[doorIndex];
            ApplyBuffReward(chosen.BuffReward);

            Gold += chosen.BonusGoldReward;
            CurrentFloor++;

            string buffName = chosen.BuffReward != null ? chosen.BuffReward.BuffName : "Chúc Phúc";
            string doorOutcome = $"Đã chọn Cửa {Palette.GetTierVietnameseName(chosen.Tier)}: Nhận [{buffName}] (+{chosen.BonusGoldReward} Vàng)";

            EnterRoom(chosen.NextRoomType, chosen.DifficultyMultiplier, doorOutcome);
            Notify(LatestBannerMessage);
            return true;
        }

        public void ApplyBuffReward(BuffDefinitionSO buff)
        {
            if (buff == null) return;
            _activeBuffs.Add(buff);

            if (buff.ImmediateGoldGain > 0)
            {
                Gold += buff.ImmediateGoldGain;
            }

            for (int i = 0; i < _party.Count; i++)
            {
                HeroCombatant hero = _party[i];
                if (hero == null) continue;

                if (buff.UnlockExtraBackpackSlots > 0)
                {
                    hero.PersonalBackpack.UnlockNextLockedSlots(buff.UnlockExtraBackpackSlots);
                }

                hero.RecalculateSynergies(_activeBuffs);

                if (buff.ImmediateHealPercent > 0 && hero.IsAlive)
                {
                    int healAmt = Mathf.Max(1, Mathf.RoundToInt(hero.MaxHP * (buff.ImmediateHealPercent / 100f)));
                    hero.Heal(healAmt);
                }
            }
        }

        public void EnterRoom(RoomType roomType, float difficultyMultiplier, string prefixNotification = null)
        {
            CurrentRoomType = roomType;
            CurrentRoomDifficulty = Mathf.Max(0.5f, difficultyMultiplier);
            string roomBanner;

            switch (roomType)
            {
                case RoomType.Combat:
                case RoomType.Elite:
                case RoomType.Boss:
                    State = RunState.Combat;
                    SpawnMonstersForRoom(roomType, CurrentRoomDifficulty);
                    Combat.StartCombat(_party, _currentMonsters, _activeBuffs);
                    roomBanner = $"Tầng {CurrentFloor} — {Palette.GetRoomVietnameseName(roomType)} (Độ khó {Mathf.RoundToInt(CurrentRoomDifficulty * 100)}%)";
                    break;

                case RoomType.Shop:
                    State = RunState.Shop;
                    Shop.GenerateOffers(CurrentFloor);
                    roomBanner = $"Tầng {CurrentFloor} — Thương Nhân Hắc Ám: Mua trang bị, Làm mới hoặc Mở rộng Ô Balo.";
                    break;

                case RoomType.Recruit:
                    State = RunState.Recruit;
                    BuildRecruitCandidates();
                    roomBanner = $"Tầng {CurrentFloor} — Trại Chiêu Mộ: Chọn 1 Đồng Đội mới gia nhập đội hình (Tối đa 3 người)!";
                    break;

                case RoomType.Event:
                default:
                    State = RunState.Event;
                    roomBanner = $"Tầng {CurrentFloor} — Điện Thờ Huyết Nguyệt: Chọn một nghi thức cổ xưa.";
                    break;
            }

            LatestBannerMessage = string.IsNullOrEmpty(prefixNotification)
                ? roomBanner
                : $"{prefixNotification} -> {roomBanner}";
            OnRunStateChanged?.Invoke();
        }

        public void ResolveEventChoice(int choiceIndex, out string resultMessage)
        {
            switch (choiceIndex)
            {
                case 0:
                    // Blood sacrifice: lose 12 HP on selected hero, receive a high-tier item into Shared Stash + unlock 2 backpack slots
                    HeroCombatant hero = SelectedHero;
                    if (hero != null && hero.CurrentHP > 15)
                    {
                        hero.CurrentHP -= 12;
                    }
                    ItemDefinitionSO relic = DefaultGameCatalog.GetItemById("item_cursed_idol");
                    SharedStash.TryAutoPlaceItem(new ItemData(relic));
                    hero?.PersonalBackpack.UnlockNextLockedSlots(2);
                    resultMessage = "Hiến tế máu tại Điện Thờ: Nhận [Tượng Tà Thần Cổ Quái] vào Kho Chung & mở +2 Ô Balo!";
                    break;

                case 1:
                    // Sacred blessing: heal all party 40% HP and gain +25 Gold
                    for (int i = 0; i < _party.Count; i++)
                    {
                        if (_party[i] != null && _party[i].IsAlive)
                        {
                            _party[i].Heal(Mathf.RoundToInt(_party[i].MaxHP * 0.4f));
                        }
                    }
                    Gold += 25;
                    resultMessage = "Khấn nguyện Ánh Nến Cổ: Hồi 40% HP cho toàn đội và nhặt được +25 Vàng!";
                    break;

                default:
                    // Take a random weapon into Shared Stash
                    ItemDefinitionSO axe = DefaultGameCatalog.GetItemById("item_executioner_axe");
                    SharedStash.TryAutoPlaceItem(new ItemData(axe));
                    resultMessage = "Khám phá xác chiến binh cổ: Nhặt được [Rìu Đồ Tể Chữ L] vào Kho Chung!";
                    break;
            }

            PrepareDoorSelection(resultMessage);
            Notify(LatestBannerMessage);
        }

        public bool TrySellItem(ItemData item, BackpackGrid sourceGrid, out string statusMessage)
        {
            if (item == null || item.Definition == null || sourceGrid == null)
            {
                statusMessage = "Không có trang bị hợp lệ để bán.";
                return false;
            }

            if (!sourceGrid.RemoveItem(item))
            {
                statusMessage = "Không tìm thấy trang bị trong kho để bán.";
                return false;
            }

            int sellValue = Mathf.Max(5, item.Definition.GoldCost / 2);
            Gold += sellValue;
            RecalculateAllPartySynergies();
            statusMessage = $"💰 Đã bán [{item.Definition.DisplayName}] nhận +{sellValue} Vàng!";
            Notify(statusMessage);
            OnRunStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Transfers an item between two grids (e.g. Shared Stash <-> Selected Hero's Personal Backpack).
        /// Reverts automatically to its original grid & coordinates if the target placement is invalid.
        /// </summary>
        public bool TryTransferOrMoveItem(
            ItemData item,
            BackpackGrid sourceGrid,
            BackpackGrid targetGrid,
            int targetAnchorX,
            int targetAnchorY,
            int targetRotationSteps)
        {
            if (item == null || sourceGrid == null || targetGrid == null) return false;

            int oldX = item.AnchorX;
            int oldY = item.AnchorY;
            int oldRot = item.RotationSteps;

            if (sourceGrid == targetGrid)
            {
                bool moved = targetGrid.TryPlaceItem(item, targetAnchorX, targetAnchorY, targetRotationSteps);
                if (moved)
                {
                    RecalculateAllPartySynergies();
                    OnRunStateChanged?.Invoke();
                }
                return moved;
            }

            if (!targetGrid.CanPlaceItemAt(item, targetAnchorX, targetAnchorY, targetRotationSteps))
            {
                return false;
            }

            sourceGrid.RemoveItem(item);
            if (targetGrid.TryPlaceItem(item, targetAnchorX, targetAnchorY, targetRotationSteps))
            {
                RecalculateAllPartySynergies();
                OnRunStateChanged?.Invoke();
                return true;
            }

            // Revert on unexpected failure
            sourceGrid.TryPlaceItem(item, oldX, oldY, oldRot);
            return false;
        }

        public void RecalculateAllPartySynergies()
        {
            for (int i = 0; i < _party.Count; i++)
            {
                _party[i]?.RecalculateSynergies(_activeBuffs);
            }
        }

        private void SpawnMonstersForRoom(RoomType roomType, float diff)
        {
            _currentMonsters.Clear();

            if (roomType == RoomType.Boss)
            {
                MonsterDefinitionSO bossDef = DefaultGameCatalog.GetMonsterById("monster_lich_boss");
                _currentMonsters.Add(new MonsterCombatant(bossDef, diff));
                return;
            }

            if (roomType == RoomType.Elite)
            {
                MonsterDefinitionSO eliteDef = DefaultGameCatalog.GetMonsterById("monster_gargoyle");
                _currentMonsters.Add(new MonsterCombatant(eliteDef, diff));
                if (CurrentFloor >= 3)
                {
                    MonsterDefinitionSO cultist = DefaultGameCatalog.GetMonsterById("monster_cultist");
                    _currentMonsters.Add(new MonsterCombatant(cultist, diff * 0.85f));
                }
                return;
            }

            // Standard Combat room: 1 monster on Floor 1, 2 monsters on later floors
            MonsterDefinitionSO ghoul = DefaultGameCatalog.GetMonsterById("monster_ghoul");
            _currentMonsters.Add(new MonsterCombatant(ghoul, diff));

            if (CurrentFloor >= 2 || _party.Count >= 2)
            {
                MonsterDefinitionSO second = DefaultGameCatalog.GetMonsterById("monster_cultist");
                _currentMonsters.Add(new MonsterCombatant(second, diff * 0.9f));
            }
        }

        private void BuildRecruitCandidates()
        {
            _recruitCandidates.Clear();
            IReadOnlyList<HeroDefinitionSO> allHeroes = DefaultGameCatalog.AllHeroes;

            // Prioritize hero classes not yet in the party
            for (int i = 0; i < allHeroes.Count; i++)
            {
                HeroDefinitionSO candidate = allHeroes[i];
                bool alreadyOwned = false;
                for (int p = 0; p < _party.Count; p++)
                {
                    if (_party[p].Definition.HeroId == candidate.HeroId)
                    {
                        alreadyOwned = true;
                        break;
                    }
                }

                if (!alreadyOwned)
                {
                    _recruitCandidates.Add(candidate);
                }
            }

            // Fallback if all classes are already recruited
            if (_recruitCandidates.Count == 0)
            {
                for (int i = 0; i < allHeroes.Count && _recruitCandidates.Count < 3; i++)
                {
                    _recruitCandidates.Add(allHeroes[i]);
                }
            }
        }

        private void HandleCombatEnded(CombatOutcome outcome)
        {
            if (outcome == CombatOutcome.HeroVictory)
            {
                int goldEarned = 0;
                for (int i = 0; i < _currentMonsters.Count; i++)
                {
                    goldEarned += _currentMonsters[i].Definition.GoldReward;
                }
                Gold += goldEarned;

                // Revive any fallen party members at 20% MaxHP so knocked-out allies recover after battle victory
                for (int i = 0; i < _party.Count; i++)
                {
                    HeroCombatant h = _party[i];
                    if (h != null && !h.IsAlive)
                    {
                        h.ResetCombatStatuses();
                        h.CurrentHP = Mathf.Max(1, Mathf.RoundToInt(h.MaxHP * 0.20f));
                    }
                }

                string victoryPrefix;
                // Reward a random item into Shared Stash on Elite/Boss victory
                if (CurrentRoomType == RoomType.Elite || CurrentRoomType == RoomType.Boss)
                {
                    IReadOnlyList<ItemDefinitionSO> items = DefaultGameCatalog.AllItems;
                    ItemDefinitionSO bonusItem = items[(CurrentFloor * 3) % items.Count];
                    SharedStash.TryAutoPlaceItem(new ItemData(bonusItem));
                    victoryPrefix = $"Chiến thắng! Nhận +{goldEarned} Vàng & chiến lợi phẩm [{bonusItem.DisplayName}] vào Kho Chung";
                }
                else
                {
                    victoryPrefix = $"Chiến thắng! Nhận +{goldEarned} Vàng";
                }

                PrepareDoorSelection(victoryPrefix);
                Notify(LatestBannerMessage);
            }
            else if (outcome == CombatOutcome.HeroDefeat)
            {
                State = RunState.Defeat;
                LatestBannerMessage = "Đội hình đã bị tiêu diệt! Nhấn [Chơi Lại Từ Đầu] để bắt đầu hành trình mới.";
                OnRunStateChanged?.Invoke();
            }
        }

        private void WireHeroBackpackEvents(HeroCombatant hero)
        {
            if (hero == null || hero.PersonalBackpack == null) return;
            hero.PersonalBackpack.OnGridChanged += () =>
            {
                hero.RecalculateSynergies(_activeBuffs);
                OnRunStateChanged?.Invoke();
            };
        }

        public static string GetPositionVietnamese(PartyPosition pos)
        {
            switch (pos)
            {
                case PartyPosition.Frontline: return "Tiền Tuyến";
                case PartyPosition.Midline: return "Trung Tâm";
                case PartyPosition.Backline: return "Hậu Tuyến";
                default: return "Đội Hình";
            }
        }

        private void Notify(string msg)
        {
            LatestBannerMessage = msg;
            OnNotification?.Invoke(msg);
        }
    }
}
