using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Data;
using Project05.Backpack;
using Project05.Combat;

namespace Project05.Progression
{
    public class ShopOffer
    {
        public ItemDefinitionSO ItemDef;
        public int Price;
        public bool IsSold;
    }

    /// <summary>
    /// Manages the Dark Merchant Shop displayed in the Right 1/3 column during Shop rooms.
    /// Allows purchasing items into the Shared Stash, unlocking extra Backpack slots for the selected Hero,
    /// healing the party, and rerolling shop stock.
    /// </summary>
    public class ShopManager
    {
        public const int SlotUnlockCost = 18;
        public const int PartyHealCost = 14;
        public const int RerollCost = 10;

        private readonly List<ShopOffer> _currentOffers = new List<ShopOffer>(4);
        public IReadOnlyList<ShopOffer> CurrentOffers => _currentOffers;

        public void GenerateOffers(int currentFloor, System.Random rng = null)
        {
            DefaultGameCatalog.EnsureInitialized();
            rng ??= new System.Random(Environment.TickCount ^ (currentFloor * 3571));
            _currentOffers.Clear();

            IReadOnlyList<ItemDefinitionSO> allItems = DefaultGameCatalog.AllItems;
            List<ItemDefinitionSO> candidates = new List<ItemDefinitionSO>(allItems);

            int count = Mathf.Min(4, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = rng.Next(0, candidates.Count);
                ItemDefinitionSO picked = candidates[idx];
                candidates.RemoveAt(idx);

                int price = Mathf.Max(8, picked.GoldCost + (currentFloor - 1) * 2);
                _currentOffers.Add(new ShopOffer
                {
                    ItemDef = picked,
                    Price = price,
                    IsSold = false
                });
            }
        }

        public bool TryBuyOffer(
            int offerIndex,
            ref int playerGold,
            BackpackGrid sharedStash,
            BackpackGrid fallbackHeroBackpack,
            out string statusMessage)
        {
            if (offerIndex < 0 || offerIndex >= _currentOffers.Count)
            {
                statusMessage = "Vật phẩm không hợp lệ.";
                return false;
            }

            ShopOffer offer = _currentOffers[offerIndex];
            if (offer.IsSold)
            {
                statusMessage = "Vật phẩm này đã được mua.";
                return false;
            }

            if (playerGold < offer.Price)
            {
                statusMessage = $"Không đủ Vàng! Cần {offer.Price} Vàng.";
                return false;
            }

            ItemData newItem = new ItemData(offer.ItemDef);
            bool placed = sharedStash != null && sharedStash.TryAutoPlaceItem(newItem);
            if (!placed && fallbackHeroBackpack != null)
            {
                placed = fallbackHeroBackpack.TryAutoPlaceItem(newItem);
            }

            if (!placed)
            {
                statusMessage = "Kho đồ chung & Balo đều đã đầy! Hãy dọn chỗ trước.";
                return false;
            }

            playerGold -= offer.Price;
            offer.IsSold = true;
            statusMessage = $"Đã mua [{offer.ItemDef.DisplayName}] vào Kho đồ chung (-{offer.Price} Vàng)!";
            return true;
        }

        public bool TryReroll(int currentFloor, ref int playerGold, out string statusMessage, System.Random rng = null)
        {
            if (playerGold < RerollCost)
            {
                statusMessage = $"Không đủ Vàng để làm mới cửa hàng! Cần {RerollCost} Vàng.";
                return false;
            }

            playerGold -= RerollCost;
            GenerateOffers(currentFloor, rng);
            statusMessage = $"Đã làm mới kệ hàng Thương Nhân Hắc Ám (-{RerollCost} Vàng)!";
            return true;
        }
    }
}
