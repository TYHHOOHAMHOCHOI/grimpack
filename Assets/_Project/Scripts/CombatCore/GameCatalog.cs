using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    /// <summary>
    /// Immutable validated catalog of items, heroes, and monsters loaded from plain-C# JSON.
    /// Enumerations are sorted ordinally by ID for order-independent determinism.
    /// </summary>
    public sealed class GameCatalog
    {
        public const int MaxEffectsPerItem = 5;
        public const int MaxRulesPerItem = 3;

        private readonly Dictionary<string, ItemDef> _itemsById;
        private readonly Dictionary<string, HeroDef> _heroesById;
        private readonly Dictionary<string, MonsterDef> _monstersById;

        public IReadOnlyList<ItemDef> Items { get; }
        public IReadOnlyList<HeroDef> Heroes { get; }
        public IReadOnlyList<MonsterDef> Monsters { get; }

        public GameCatalog(
            IReadOnlyList<ItemDef> items,
            IReadOnlyList<HeroDef> heroes,
            IReadOnlyList<MonsterDef> monsters)
        {
            var sortedItems = new List<ItemDef>(items ?? Array.Empty<ItemDef>());
            sortedItems.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            Items = sortedItems;
            _itemsById = new Dictionary<string, ItemDef>(sortedItems.Count, StringComparer.Ordinal);
            for (int i = 0; i < sortedItems.Count; i++) _itemsById[sortedItems[i].Id] = sortedItems[i];

            var sortedHeroes = new List<HeroDef>(heroes ?? Array.Empty<HeroDef>());
            sortedHeroes.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            Heroes = sortedHeroes;
            _heroesById = new Dictionary<string, HeroDef>(sortedHeroes.Count, StringComparer.Ordinal);
            for (int i = 0; i < sortedHeroes.Count; i++) _heroesById[sortedHeroes[i].Id] = sortedHeroes[i];

            var sortedMonsters = new List<MonsterDef>(monsters ?? Array.Empty<MonsterDef>());
            sortedMonsters.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            Monsters = sortedMonsters;
            _monstersById = new Dictionary<string, MonsterDef>(sortedMonsters.Count, StringComparer.Ordinal);
            for (int i = 0; i < sortedMonsters.Count; i++) _monstersById[sortedMonsters[i].Id] = sortedMonsters[i];
        }

        public ItemDef FindItem(string id) =>
            id != null && _itemsById.TryGetValue(id, out ItemDef item) ? item : null;

        public HeroDef FindHero(string id) =>
            id != null && _heroesById.TryGetValue(id, out HeroDef hero) ? hero : null;

        public MonsterDef FindMonster(string id) =>
            id != null && _monstersById.TryGetValue(id, out MonsterDef monster) ? monster : null;

        public static GameCatalog LoadFromJson(
            string itemsJson,
            string heroesJson,
            string monstersJson,
            out IReadOnlyList<string> validationErrors)
        {
            var errors = new List<string>();
            var items = new List<ItemDef>();
            var heroes = new List<HeroDef>();
            var monsters = new List<MonsterDef>();

            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            var heroIds = new HashSet<string>(StringComparer.Ordinal);
            var monsterIds = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(itemsJson))
            {
                errors.Add("itemsJson is empty.");
            }
            else
            {
                try
                {
                    if (SimpleJsonParser.Parse(itemsJson) is Dictionary<string, object> root &&
                        TryGetList(root, "items", out List<object> rawItems))
                    {
                        for (int i = 0; i < rawItems.Count; i++)
                        {
                            if (!(rawItems[i] is Dictionary<string, object> obj))
                            {
                                errors.Add("items[" + i + "]: expected JSON object.");
                                continue;
                            }
                            ItemDef item = ParseItem(obj, i, errors);
                            if (item != null)
                            {
                                if (!itemIds.Add(item.Id))
                                {
                                    errors.Add("Item '" + item.Id + "': duplicate item ID.");
                                }
                                else
                                {
                                    items.Add(item);
                                }
                            }
                        }
                    }
                    else
                    {
                        errors.Add("itemsJson: missing 'items' array.");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("itemsJson parse error: " + ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(heroesJson))
            {
                errors.Add("heroesJson is empty.");
            }
            else
            {
                try
                {
                    if (SimpleJsonParser.Parse(heroesJson) is Dictionary<string, object> root &&
                        TryGetList(root, "heroes", out List<object> rawHeroes))
                    {
                        for (int i = 0; i < rawHeroes.Count; i++)
                        {
                            if (!(rawHeroes[i] is Dictionary<string, object> obj))
                            {
                                errors.Add("heroes[" + i + "]: expected JSON object.");
                                continue;
                            }
                            HeroDef hero = ParseHero(obj, i, itemIds, errors);
                            if (hero != null)
                            {
                                if (!heroIds.Add(hero.Id))
                                {
                                    errors.Add("Hero '" + hero.Id + "': duplicate hero ID.");
                                }
                                else
                                {
                                    heroes.Add(hero);
                                }
                            }
                        }
                    }
                    else
                    {
                        errors.Add("heroesJson: missing 'heroes' array.");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("heroesJson parse error: " + ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(monstersJson))
            {
                errors.Add("monstersJson is empty.");
            }
            else
            {
                try
                {
                    if (SimpleJsonParser.Parse(monstersJson) is Dictionary<string, object> root &&
                        TryGetList(root, "monsters", out List<object> rawMonsters))
                    {
                        for (int i = 0; i < rawMonsters.Count; i++)
                        {
                            if (!(rawMonsters[i] is Dictionary<string, object> obj))
                            {
                                errors.Add("monsters[" + i + "]: expected JSON object.");
                                continue;
                            }
                            MonsterDef monster = ParseMonster(obj, i, errors);
                            if (monster != null)
                            {
                                if (!monsterIds.Add(monster.Id))
                                {
                                    errors.Add("Monster '" + monster.Id + "': duplicate monster ID.");
                                }
                                else
                                {
                                    monsters.Add(monster);
                                }
                            }
                        }
                    }
                    else
                    {
                        errors.Add("monstersJson: missing 'monsters' array.");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("monstersJson parse error: " + ex.Message);
                }
            }

            validationErrors = errors;
            return errors.Count == 0 ? new GameCatalog(items, heroes, monsters) : null;
        }

        private static ItemDef ParseItem(Dictionary<string, object> obj, int index, List<string> errors)
        {
            string id = GetString(obj, "id", "");
            string label = string.IsNullOrEmpty(id) ? "items[" + index + "]" : "Item '" + id + "'";
            if (!IsValidId(id))
            {
                errors.Add(label + ": invalid or empty id.");
            }

            string art = GetString(obj, "art", "");
            TierKind tier = ParseTier(GetString(obj, "tier", "White"), label, errors);
            string footprint = GetString(obj, "footprint", "1x1");
            List<GridCell> cells = ParseCells(obj, "cells", label, errors);
            ValidateFootprintCells(cells, label, errors);

            List<string> tags = ParseStringList(obj, "tags");

            Dictionary<string, object> triggerObj = GetDict(obj, "trigger");
            TriggerDef trigger = triggerObj != null
                ? ParseTrigger(triggerObj, label + ".trigger", errors)
                : new TriggerDef(TriggerKind.Passive, 0, 0);

            var effects = new List<EffectDef>();
            if (TryGetList(obj, "effects", out List<object> rawEffects))
            {
                for (int i = 0; i < rawEffects.Count; i++)
                {
                    if (rawEffects[i] is Dictionary<string, object> effObj)
                    {
                        effects.Add(ParseEffect(effObj, label + ".effects[" + i + "]", errors));
                    }
                    else
                    {
                        errors.Add(label + ".effects[" + i + "]: expected object.");
                    }
                }
            }

            ValidateTriggerAndEffects(trigger, effects, label, errors);

            int hpBonus = GetInt(obj, "hpBonus", 0);
            int speedBonus = GetInt(obj, "speedBonus", 0);
            if (hpBonus < -20 || hpBonus > 40)
            {
                errors.Add(label + ".hpBonus: out of range [-20..40]: " + hpBonus);
            }
            if (speedBonus < -10 || speedBonus > 10)
            {
                errors.Add(label + ".speedBonus: out of range [-10..10]: " + speedBonus);
            }

            var synergy = new List<SynergyRuleDef>();
            if (TryGetList(obj, "synergy", out List<object> rawSyn))
            {
                if (rawSyn.Count > MaxRulesPerItem)
                {
                    errors.Add(label + ".synergy: exceeds MaxRulesPerItem (" + MaxRulesPerItem + ").");
                }
                for (int i = 0; i < rawSyn.Count; i++)
                {
                    if (rawSyn[i] is Dictionary<string, object> synObj)
                    {
                        synergy.Add(ParseSynergyRule(synObj, label + ".synergy[" + i + "]", errors));
                    }
                    else
                    {
                        errors.Add(label + ".synergy[" + i + "]: expected object.");
                    }
                }
            }

            string nameVi = GetString(obj, "nameVi", id);
            string nameEn = GetString(obj, "nameEn", id);
            string flavorVi = GetString(obj, "flavorVi", "");
            string flavorEn = GetString(obj, "flavorEn", "");

            return new ItemDef(
                id, art, tier, footprint, cells, tags, trigger, effects,
                hpBonus, speedBonus, synergy, nameVi, nameEn, flavorVi, flavorEn);
        }

        private static HeroDef ParseHero(
            Dictionary<string, object> obj,
            int index,
            HashSet<string> validItemIds,
            List<string> errors)
        {
            string id = GetString(obj, "id", "");
            string label = string.IsNullOrEmpty(id) ? "heroes[" + index + "]" : "Hero '" + id + "'";
            if (!IsValidId(id)) errors.Add(label + ": invalid or empty id.");

            string art = GetString(obj, "art", "tank");
            string nameVi = GetString(obj, "nameVi", id);
            string nameEn = GetString(obj, "nameEn", id);
            string roleVi = GetString(obj, "roleVi", "");
            string roleEn = GetString(obj, "roleEn", "");

            int baseMaxHp = HasKey(obj, "baseMaxHp")
                ? GetInt(obj, "baseMaxHp", 0)
                : GetInt(obj, "baseHp", 100);
            int baseSpeed = GetInt(obj, "baseSpeed", 20);
            int initialShield = GetInt(obj, "initialShield", 0);

            if (baseMaxHp < 40 || baseMaxHp > 300)
                errors.Add(label + ".baseMaxHp: out of range [40..300]: " + baseMaxHp);
            if (baseSpeed < 5 || baseSpeed > 60)
                errors.Add(label + ".baseSpeed: out of range [5..60]: " + baseSpeed);
            if (initialShield < 0 || initialShield > 30)
                errors.Add(label + ".initialShield: out of range [0..30]: " + initialShield);

            Dictionary<string, object> bpObj = GetDict(obj, "backpackProfile") ?? GetDict(obj, "backpack");
            BackpackProfileDef profile = ParseBackpackProfile(bpObj, label + ".backpackProfile", errors);

            var startingItems = new List<string>();
            string signatureItemId = GetString(obj, "signatureItemId", "");
            if (TryGetList(obj, "startingItems", out List<object> rawStarting))
            {
                for (int i = 0; i < rawStarting.Count; i++)
                {
                    if (rawStarting[i] is string sId)
                    {
                        startingItems.Add(sId);
                    }
                    else if (rawStarting[i] is Dictionary<string, object> itemEntry)
                    {
                        string sItemId = GetString(itemEntry, "itemId", GetString(itemEntry, "id", ""));
                        bool isSig = GetBool(itemEntry, "isSignature", false);
                        if (!string.IsNullOrEmpty(sItemId))
                        {
                            startingItems.Add(sItemId);
                            if (isSig && string.IsNullOrEmpty(signatureItemId))
                            {
                                signatureItemId = sItemId;
                            }
                        }
                        else
                        {
                            errors.Add(label + ".startingItems[" + i + "]: missing itemId.");
                        }
                    }
                }
            }

            for (int i = 0; i < startingItems.Count; i++)
            {
                if (!validItemIds.Contains(startingItems[i]))
                {
                    errors.Add(label + ".startingItems[" + i + "]: unknown item '" + startingItems[i] + "'.");
                }
            }

            if (!string.IsNullOrEmpty(signatureItemId))
            {
                if (!validItemIds.Contains(signatureItemId))
                {
                    errors.Add(label + ".signatureItemId: unknown item '" + signatureItemId + "'.");
                }
                else if (startingItems.Count > 0 && !startingItems.Contains(signatureItemId))
                {
                    errors.Add(label + ".signatureItemId: '" + signatureItemId + "' is not in startingItems.");
                }
            }

            InnateItemDef innate = null;
            Dictionary<string, object> innateObj = GetDict(obj, "innateItem");
            if (innateObj != null)
            {
                innate = ParseInnateItem(innateObj, label + ".innateItem", errors);
            }

            return new HeroDef(
                id, art, nameVi, nameEn, roleVi, roleEn,
                baseMaxHp, baseSpeed, initialShield,
                profile, startingItems, signatureItemId, innate);
        }

        private static BackpackProfileDef ParseBackpackProfile(
            Dictionary<string, object> obj,
            string label,
            List<string> errors)
        {
            if (obj == null)
            {
                return new BackpackProfileDef(4, 4, 4, 4, Array.Empty<GridCell>());
            }

            int width = GetInt(obj, "width", 4);
            int height = GetInt(obj, "height", 4);
            int startWidth = GetInt(obj, "startWidth", Math.Min(4, width));
            int startHeight = GetInt(obj, "startHeight", Math.Min(4, height));

            if (width < 1 || width > 6 || height < 1 || height > 6)
                errors.Add(label + ": width/height must be in [1..6].");
            if (startWidth < 1 || startWidth > width || startHeight < 1 || startHeight > height)
                errors.Add(label + ": startWidth/startHeight must lie inside bound.");

            List<GridCell> unlockSeq = ParseCells(obj, "unlockSequence", label, errors);
            var seen = new HashSet<GridCell>();
            for (int i = 0; i < unlockSeq.Count; i++)
            {
                GridCell c = unlockSeq[i];
                if (c.X < 0 || c.X >= width || c.Y < 0 || c.Y >= height)
                {
                    errors.Add(label + ".unlockSequence[" + i + "]: cell " + c + " out of bound.");
                }
                else if (c.X < startWidth && c.Y < startHeight)
                {
                    errors.Add(label + ".unlockSequence[" + i + "]: cell " + c + " inside start block.");
                }
                if (!seen.Add(c))
                {
                    errors.Add(label + ".unlockSequence[" + i + "]: duplicate cell " + c + ".");
                }
            }

            return new BackpackProfileDef(width, height, startWidth, startHeight, unlockSeq);
        }

        private static InnateItemDef ParseInnateItem(
            Dictionary<string, object> obj,
            string label,
            List<string> errors)
        {
            string id = GetString(obj, "id", "");
            if (!IsValidId(id)) errors.Add(label + ": invalid or empty id.");

            Dictionary<string, object> trigObj = GetDict(obj, "trigger");
            TriggerDef trigger = trigObj != null
                ? ParseTrigger(trigObj, label + ".trigger", errors)
                : new TriggerDef(TriggerKind.CombatStart, 0, 0);

            if (trigger.Kind == TriggerKind.Passive)
            {
                errors.Add(label + ".trigger: innate item cannot have Passive trigger.");
            }

            var effects = new List<EffectDef>();
            if (TryGetList(obj, "effects", out List<object> rawEffects))
            {
                for (int i = 0; i < rawEffects.Count; i++)
                {
                    if (rawEffects[i] is Dictionary<string, object> effObj)
                    {
                        effects.Add(ParseEffect(effObj, label + ".effects[" + i + "]", errors));
                    }
                }
            }

            ValidateTriggerAndEffects(trigger, effects, label, errors);

            string nameVi = GetString(obj, "nameVi", id);
            string nameEn = GetString(obj, "nameEn", id);
            string descVi = GetString(obj, "descVi", "");
            string descEn = GetString(obj, "descEn", "");

            return new InnateItemDef(id, trigger, effects, nameVi, nameEn, descVi, descEn);
        }

        private static MonsterDef ParseMonster(Dictionary<string, object> obj, int index, List<string> errors)
        {
            string id = GetString(obj, "id", "");
            string label = string.IsNullOrEmpty(id) ? "monsters[" + index + "]" : "Monster '" + id + "'";
            if (!IsValidId(id)) errors.Add(label + ": invalid or empty id.");

            string visual = GetString(obj, "visual", "swarmer");
            int defaultBodyHeight = string.Equals(visual, "brute", StringComparison.OrdinalIgnoreCase) ? 44 : 26;
            int bodyHeight = GetInt(obj, "bodyHeight", defaultBodyHeight);
            string nameVi = GetString(obj, "nameVi", id);
            string nameEn = GetString(obj, "nameEn", id);
            MonsterClassKind classification = ParseMonsterClass(GetString(obj, "classification", "regular"), label, errors);
            MonsterSizeKind sizeClass = ParseMonsterSize(GetString(obj, "sizeClass", "small"), label, errors);

            int baseMaxHp = HasKey(obj, "baseMaxHp")
                ? GetInt(obj, "baseMaxHp", 0)
                : GetInt(obj, "baseHp", 0);
            int baseSpeed = GetInt(obj, "baseSpeed", 0);
            string openerMoveId = GetString(obj, "openerMoveId", "");

            if (baseMaxHp < 1 || baseMaxHp > 100000)
                errors.Add(label + ".baseMaxHp: out of range [1..100000]: " + baseMaxHp);
            if (baseSpeed < 5 || baseSpeed > 60)
                errors.Add(label + ".baseSpeed: out of range [5..60]: " + baseSpeed);

            var moves = new List<MonsterMoveDef>();
            var movesById = new Dictionary<string, MonsterMoveDef>(StringComparer.Ordinal);
            if (TryGetList(obj, "moves", out List<object> rawMoves))
            {
                for (int i = 0; i < rawMoves.Count; i++)
                {
                    if (!(rawMoves[i] is Dictionary<string, object> mObj))
                    {
                        errors.Add(label + ".moves[" + i + "]: expected object.");
                        continue;
                    }
                    MonsterMoveDef move = ParseMonsterMove(mObj, label + ".moves[" + i + "]", errors);
                    if (movesById.ContainsKey(move.Id))
                    {
                        errors.Add(label + ".moves[" + i + "]: duplicate move ID '" + move.Id + "'.");
                    }
                    else
                    {
                        movesById[move.Id] = move;
                    }
                    moves.Add(move);
                }
            }

            if (moves.Count < 1 || moves.Count > 8)
            {
                errors.Add(label + ".moves: must contain 1..8 moves (got " + moves.Count + ").");
            }

            bool hasAttackMove = false;
            bool hasReachableAttack = false;
            for (int i = 0; i < moves.Count; i++)
            {
                MonsterMoveDef m = moves[i];
                if (m.HitCount >= 1) hasAttackMove = true;
                if (m.HitCount >= 1 && (m.Weight > 0 || string.Equals(m.Id, openerMoveId, StringComparison.Ordinal)))
                {
                    hasReachableAttack = true;
                }

                if (!string.IsNullOrEmpty(m.LinkedNextMoveId))
                {
                    if (string.Equals(m.LinkedNextMoveId, m.Id, StringComparison.Ordinal))
                    {
                        errors.Add(label + ".moves['" + m.Id + "']: self-link is invalid.");
                    }
                    else if (!movesById.TryGetValue(m.LinkedNextMoveId, out MonsterMoveDef linked))
                    {
                        errors.Add(label + ".moves['" + m.Id + "']: unknown linkedNextMoveId '" + m.LinkedNextMoveId + "'.");
                    }
                    else
                    {
                        if (linked.HitCount == 0)
                        {
                            errors.Add(label + ".moves['" + m.Id + "']: linkedNextMoveId cannot point to a windup.");
                        }
                        if (linked.HitCount >= 1) hasReachableAttack = true;
                    }
                }

                if (m.TargetMode == MoveTargetMode.SameAsMove)
                {
                    if (string.IsNullOrEmpty(m.SameAsMoveId) || !movesById.TryGetValue(m.SameAsMoveId, out MonsterMoveDef targetRef))
                    {
                        errors.Add(label + ".moves['" + m.Id + "']: invalid sameAsMoveId '" + m.SameAsMoveId + "'.");
                    }
                    else if (targetRef.TargetMode == MoveTargetMode.AllHeroes || targetRef.TargetMode == MoveTargetMode.None)
                    {
                        errors.Add(label + ".moves['" + m.Id + "']: sameAsMoveId cannot reference AllHeroes or targetless move.");
                    }
                }
            }

            // Detect link cycles
            for (int i = 0; i < moves.Count; i++)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal);
                string cur = moves[i].Id;
                while (!string.IsNullOrEmpty(cur) && movesById.TryGetValue(cur, out MonsterMoveDef md))
                {
                    if (!visited.Add(cur))
                    {
                        errors.Add(label + ".moves: linkedNext cycle detected at '" + cur + "'.");
                        break;
                    }
                    cur = md.LinkedNextMoveId;
                }
            }

            if (!hasAttackMove || !hasReachableAttack)
            {
                errors.Add(label + ": monster must have at least one reachable move with hitCount >= 1.");
            }

            if (!string.IsNullOrEmpty(openerMoveId) && !movesById.ContainsKey(openerMoveId))
            {
                errors.Add(label + ".openerMoveId: unknown move '" + openerMoveId + "'.");
            }

            var bossPhases = new List<BossPhaseDef>();
            List<object> rawPhases = null;
            if (!TryGetList(obj, "bossPhases", out rawPhases))
            {
                TryGetList(obj, "phases", out rawPhases);
            }
            if (rawPhases != null && rawPhases.Count > 0)
            {
                int expectedStart = 0;
                for (int i = 0; i < rawPhases.Count; i++)
                {
                    if (!(rawPhases[i] is Dictionary<string, object> pObj))
                    {
                        errors.Add(label + ".bossPhases[" + i + "]: expected object.");
                        continue;
                    }
                    int startIdx = GetInt(pObj, "startIndex", -1);
                    int endIdx = GetInt(pObj, "endIndexInclusive", -1);
                    List<string> allowed = ParseStringList(pObj, "allowedMoveIds");
                    if (allowed.Count == 0) allowed = ParseStringList(pObj, "moves");

                    if (startIdx != expectedStart)
                    {
                        errors.Add(label + ".bossPhases[" + i + "]: expected startIndex " + expectedStart + ", got " + startIdx);
                    }
                    bool isLast = i == rawPhases.Count - 1;
                    if (!isLast && endIdx < startIdx)
                    {
                        errors.Add(label + ".bossPhases[" + i + "]: invalid endIndexInclusive " + endIdx);
                    }
                    if (isLast && endIdx != -1)
                    {
                        errors.Add(label + ".bossPhases[" + i + "]: last phase must be open-ended (-1).");
                    }
                    expectedStart = endIdx + 1;

                    for (int j = 0; j < allowed.Count; j++)
                    {
                        if (!movesById.ContainsKey(allowed[j]))
                        {
                            errors.Add(label + ".bossPhases[" + i + "]: unknown move '" + allowed[j] + "'.");
                        }
                    }

                    bossPhases.Add(new BossPhaseDef(startIdx, endIdx, allowed));
                }
            }

            return new MonsterDef(
                id, visual, bodyHeight, nameVi, nameEn,
                classification, sizeClass, baseMaxHp, baseSpeed,
                openerMoveId, moves, bossPhases);
        }

        private static MonsterMoveDef ParseMonsterMove(Dictionary<string, object> obj, string label, List<string> errors)
        {
            string id = GetString(obj, "id", "");
            if (string.IsNullOrEmpty(id)) errors.Add(label + ": empty move id.");

            string nameVi = GetString(obj, "nameVi", id);
            string nameEn = GetString(obj, "nameEn", id);
            MoveGlyphKind glyph = ParseMoveGlyph(GetString(obj, "glyph", "attack"), label, errors);
            DeliveryKind delivery = ParseDelivery(GetString(obj, "delivery", "swing"), label, errors);
            int hitCount = GetInt(obj, "hitCount", 1);
            int baseDamage = GetInt(obj, "baseDamage", 0);

            string targetModeRaw = GetString(obj, "targetMode", hitCount == 0 ? "None" : "RandomHero");
            MoveTargetMode targetMode = ParseMoveTargetMode(targetModeRaw, out string sameAsMoveId, label, errors);
            string explicitSameAs = GetString(obj, "sameAsMoveId", "");
            if (!string.IsNullOrEmpty(explicitSameAs)) sameAsMoveId = explicitSameAs;

            int weight = GetInt(obj, "weight", 0);
            bool heavy = GetBool(obj, "heavy", false);
            string linkedNext = HasKey(obj, "linkedNextMoveId")
                ? GetString(obj, "linkedNextMoveId", "")
                : GetString(obj, "linkedNext", "");

            if (hitCount < 0 || hitCount > 8)
                errors.Add(label + ".hitCount: out of range [0..8]: " + hitCount);
            if (hitCount == 0 && baseDamage != 0)
                errors.Add(label + ".baseDamage: windup (hitCount 0) must have baseDamage 0.");
            if (hitCount >= 1 && (baseDamage < 1 || baseDamage > 100000))
                errors.Add(label + ".baseDamage: out of range [1..100000]: " + baseDamage);
            if (weight < 0 || weight > 100)
                errors.Add(label + ".weight: out of range [0..100]: " + weight);

            return new MonsterMoveDef(
                id, nameVi, nameEn, glyph, delivery, hitCount, baseDamage,
                targetMode, sameAsMoveId, weight, heavy, linkedNext);
        }

        private static TriggerDef ParseTrigger(Dictionary<string, object> obj, string label, List<string> errors)
        {
            string kindStr = GetString(obj, "kind", "Passive");
            TriggerKind kind;
            switch (kindStr)
            {
                case "Passive": kind = TriggerKind.Passive; break;
                case "Cooldown": kind = TriggerKind.Cooldown; break;
                case "Reaction": kind = TriggerKind.Reaction; break;
                case "CombatStart": kind = TriggerKind.CombatStart; break;
                default:
                    errors.Add(label + ".kind: unknown trigger kind '" + kindStr + "'.");
                    kind = TriggerKind.Passive;
                    break;
            }

            int n = GetInt(obj, "n", 0);
            int s = GetInt(obj, "s", 0);
            string evKindStr = GetString(obj, "eventKind", "");
            ReactionEventKind evKind = ReactionEventKind.None;
            if (!string.IsNullOrEmpty(evKindStr))
            {
                switch (evKindStr)
                {
                    case "ItemFired": evKind = ReactionEventKind.ItemFired; break;
                    case "StatusApplied": evKind = ReactionEventKind.StatusApplied; break;
                    case "StatusConsumed": evKind = ReactionEventKind.StatusConsumed; break;
                    case "Damage": evKind = ReactionEventKind.Damage; break;
                    case "UnitDowned": evKind = ReactionEventKind.UnitDowned; break;
                    default:
                        errors.Add(label + ".eventKind: unknown reaction eventKind '" + evKindStr + "'.");
                        break;
                }
            }

            string evStatus = GetString(obj, "eventStatus", "");
            string evSideStr = GetString(obj, "eventSide", "");
            ReactionSideFilter evSide = ParseReactionSide(evSideStr, label, errors);

            return new TriggerDef(kind, n, s, evKind, evStatus, evSide);
        }

        private static ReactionSideFilter ParseReactionSide(string raw, string label, List<string> errors)
        {
            if (string.IsNullOrEmpty(raw)) return ReactionSideFilter.Any;
            switch (raw.ToLowerInvariant())
            {
                case "any": return ReactionSideFilter.Any;
                case "hero":
                case "ally": return ReactionSideFilter.Hero;
                case "enemy": return ReactionSideFilter.Enemy;
                case "self":
                case "own_hero":
                case "ownhero": return ReactionSideFilter.OwnHero;
                case "other_hero":
                case "otherhero": return ReactionSideFilter.OtherHero;
                default:
                    errors.Add(label + ".eventSide: unknown side filter '" + raw + "'.");
                    return ReactionSideFilter.Any;
            }
        }

        private static EffectDef ParseEffect(Dictionary<string, object> obj, string label, List<string> errors)
        {
            string kindStr = GetString(obj, "kind", "");
            EffectKind kind;
            switch (kindStr)
            {
                case "Damage": kind = EffectKind.Damage; break;
                case "ApplyStatus": kind = EffectKind.ApplyStatus; break;
                case "ConsumeDetonate": kind = EffectKind.ConsumeDetonate; break;
                case "ShieldGain": kind = EffectKind.ShieldGain; break;
                case "Heal": kind = EffectKind.Heal; break;
                default:
                    errors.Add(label + ".kind: unknown effect kind '" + kindStr + "'.");
                    kind = EffectKind.Damage;
                    break;
            }

            string targetStr = GetString(obj, "target", "");
            TargetKind target;
            if (string.IsNullOrEmpty(targetStr))
            {
                target = (kind == EffectKind.ShieldGain || kind == EffectKind.Heal)
                    ? TargetKind.Self
                    : TargetKind.FirstEnemy;
            }
            else
            {
                switch (targetStr)
                {
                    case "FirstEnemy": target = TargetKind.FirstEnemy; break;
                    case "AllEnemies": target = TargetKind.AllEnemies; break;
                    case "Self": target = TargetKind.Self; break;
                    case "AllHeroes": target = TargetKind.AllHeroes; break;
                    case "LowestHpHero": target = TargetKind.LowestHpHero; break;
                    default:
                        errors.Add(label + ".target: unknown target '" + targetStr + "'.");
                        target = TargetKind.FirstEnemy;
                        break;
                }
            }

            int baseDmg = HasKey(obj, "base") ? GetInt(obj, "base", 0) : GetInt(obj, "base_", 0);
            string status = GetString(obj, "status", "");
            int amount = GetInt(obj, "amount", 0);
            int units = GetInt(obj, "units", 0);
            int perUnit = GetInt(obj, "perUnit", 0);

            switch (kind)
            {
                case EffectKind.Damage:
                    if (baseDmg < 1 || baseDmg > 100000)
                        errors.Add(label + ".base: Damage base must be in [1..100000], got " + baseDmg);
                    break;
                case EffectKind.ApplyStatus:
                    if (string.IsNullOrEmpty(status))
                        errors.Add(label + ".status: ApplyStatus requires non-empty status.");
                    if (amount < 1 || amount > 100000)
                        errors.Add(label + ".amount: ApplyStatus amount must be in [1..100000], got " + amount);
                    break;
                case EffectKind.ConsumeDetonate:
                    if (string.IsNullOrEmpty(status))
                        errors.Add(label + ".status: ConsumeDetonate requires non-empty status.");
                    if (units < 1)
                        errors.Add(label + ".units: ConsumeDetonate units must be >= 1, got " + units);
                    if (perUnit < 1 || perUnit > 1000)
                        errors.Add(label + ".perUnit: ConsumeDetonate perUnit must be in [1..1000], got " + perUnit);
                    break;
                case EffectKind.ShieldGain:
                case EffectKind.Heal:
                    if (amount < 1 || amount > 100000)
                        errors.Add(label + ".amount: " + kind + " amount must be in [1..100000], got " + amount);
                    break;
            }

            return new EffectDef(kind, target, baseDmg, status, amount, units, perUnit);
        }

        private static SynergyRuleDef ParseSynergyRule(Dictionary<string, object> obj, string label, List<string> errors)
        {
            List<string> targetTags = ParseStringList(obj, "targetTags");
            string kindStr = GetString(obj, "kind", "");
            SynergyKind kind;
            switch (kindStr)
            {
                case "DamageFlat": kind = SynergyKind.DamageFlat; break;
                case "DamagePercent": kind = SynergyKind.DamagePercent; break;
                case "CooldownReduction": kind = SynergyKind.CooldownReduction; break;
                case "StatusAmount": kind = SynergyKind.StatusAmount; break;
                default:
                    errors.Add(label + ".kind: unknown synergy kind '" + kindStr + "'.");
                    kind = SynergyKind.DamageFlat;
                    break;
            }

            int amount = GetInt(obj, "amount", 0);
            string statusId = GetString(obj, "statusId", "");
            if (amount == 0) errors.Add(label + ".amount: synergy amount cannot be zero.");

            switch (kind)
            {
                case SynergyKind.DamageFlat:
                    if (amount < -100000 || amount > 100000)
                        errors.Add(label + ".amount: DamageFlat out of range [-100000..100000].");
                    break;
                case SynergyKind.DamagePercent:
                    if (amount < -1000 || amount > 100000)
                        errors.Add(label + ".amount: DamagePercent out of range [-1000..100000].");
                    break;
                case SynergyKind.CooldownReduction:
                    if (amount < -10 || amount > 10)
                        errors.Add(label + ".amount: CooldownReduction out of range [-10..10].");
                    break;
                case SynergyKind.StatusAmount:
                    if (string.IsNullOrEmpty(statusId))
                        errors.Add(label + ".statusId: StatusAmount requires non-empty statusId.");
                    break;
            }

            return new SynergyRuleDef(targetTags, kind, amount, statusId);
        }

        private static void ValidateTriggerAndEffects(
            TriggerDef trigger,
            List<EffectDef> effects,
            string label,
            List<string> errors)
        {
            if (trigger.Kind == TriggerKind.Passive)
            {
                if (effects.Count > 0) errors.Add(label + ": Passive item cannot have effects.");
                if (trigger.N != 0) errors.Add(label + ": Passive item must have n == 0.");
            }
            else
            {
                if (effects.Count < 1 || effects.Count > MaxEffectsPerItem)
                {
                    errors.Add(label + ".effects: non-passive item must have 1.." + MaxEffectsPerItem + " effects.");
                }
                if (trigger.Kind == TriggerKind.Cooldown)
                {
                    if (trigger.N < 1 || trigger.N > 20)
                        errors.Add(label + ".trigger.n: Cooldown n must be in [1..20].");
                    if (trigger.S < 0 || trigger.S > 20)
                        errors.Add(label + ".trigger.s: Cooldown s must be in [0..20].");
                }
                else if (trigger.Kind == TriggerKind.Reaction)
                {
                    if (trigger.N < 1 || trigger.N > 20)
                        errors.Add(label + ".trigger.n: Reaction n must be in [1..20].");
                    if (trigger.EventKind == ReactionEventKind.None)
                        errors.Add(label + ".trigger.eventKind: Reaction requires valid eventKind.");
                }
                else if (trigger.Kind == TriggerKind.CombatStart)
                {
                    if (trigger.N != 0)
                        errors.Add(label + ".trigger.n: CombatStart must have n == 0.");
                }
            }
        }

        private static void ValidateFootprintCells(List<GridCell> cells, string label, List<string> errors)
        {
            if (cells.Count < 1 || cells.Count > 9)
            {
                errors.Add(label + ".cells: footprint must contain 1..9 cells.");
                return;
            }

            var cellSet = new HashSet<GridCell>();
            for (int i = 0; i < cells.Count; i++)
            {
                GridCell c = cells[i];
                if (c.X < 0 || c.X >= 3 || c.Y < 0 || c.Y >= 3)
                {
                    errors.Add(label + ".cells[" + i + "]: cell " + c + " outside 3x3 box.");
                }
                if (!cellSet.Add(c))
                {
                    errors.Add(label + ".cells[" + i + "]: duplicate cell " + c + ".");
                }
            }

            // BFS connectivity check (shared edges)
            var visited = new HashSet<GridCell>();
            var queue = new Queue<GridCell>();
            queue.Enqueue(cells[0]);
            visited.Add(cells[0]);
            while (queue.Count > 0)
            {
                GridCell cur = queue.Dequeue();
                GridCell[] neighbors =
                {
                    new GridCell(cur.X + 1, cur.Y),
                    new GridCell(cur.X - 1, cur.Y),
                    new GridCell(cur.X, cur.Y + 1),
                    new GridCell(cur.X, cur.Y - 1)
                };
                for (int k = 0; k < neighbors.Length; k++)
                {
                    if (cellSet.Contains(neighbors[k]) && visited.Add(neighbors[k]))
                    {
                        queue.Enqueue(neighbors[k]);
                    }
                }
            }

            if (visited.Count != cellSet.Count)
            {
                errors.Add(label + ".cells: footprint cells are not edge-connected.");
            }
        }

        private static TierKind ParseTier(string raw, string label, List<string> errors)
        {
            switch (raw)
            {
                case "White": return TierKind.White;
                case "Green": return TierKind.Green;
                case "Blue": return TierKind.Blue;
                case "Purple": return TierKind.Purple;
                case "Gold": return TierKind.Gold;
                case "Red": return TierKind.Red;
                default:
                    errors.Add(label + ".tier: unknown tier '" + raw + "'.");
                    return TierKind.White;
            }
        }

        private static MonsterClassKind ParseMonsterClass(string raw, string label, List<string> errors)
        {
            switch (raw.ToLowerInvariant())
            {
                case "regular": return MonsterClassKind.Regular;
                case "elite": return MonsterClassKind.Elite;
                case "boss": return MonsterClassKind.Boss;
                default:
                    errors.Add(label + ".classification: unknown classification '" + raw + "'.");
                    return MonsterClassKind.Regular;
            }
        }

        private static MonsterSizeKind ParseMonsterSize(string raw, string label, List<string> errors)
        {
            switch (raw.ToLowerInvariant())
            {
                case "small": return MonsterSizeKind.Small;
                case "medium": return MonsterSizeKind.Medium;
                case "large": return MonsterSizeKind.Large;
                case "boss": return MonsterSizeKind.Boss;
                default:
                    errors.Add(label + ".sizeClass: unknown sizeClass '" + raw + "'.");
                    return MonsterSizeKind.Small;
            }
        }

        private static MoveGlyphKind ParseMoveGlyph(string raw, string label, List<string> errors)
        {
            switch (raw.ToLowerInvariant())
            {
                case "attack": return MoveGlyphKind.Attack;
                case "windup": return MoveGlyphKind.Windup;
                default:
                    errors.Add(label + ".glyph: unknown glyph '" + raw + "'.");
                    return MoveGlyphKind.Attack;
            }
        }

        private static DeliveryKind ParseDelivery(string raw, string label, List<string> errors)
        {
            switch (raw.ToLowerInvariant())
            {
                case "swing": return DeliveryKind.Swing;
                case "throw": return DeliveryKind.Throw;
                default:
                    errors.Add(label + ".delivery: unknown delivery '" + raw + "'.");
                    return DeliveryKind.Swing;
            }
        }

        private static MoveTargetMode ParseMoveTargetMode(
            string raw,
            out string parsedSameAsMoveId,
            string label,
            List<string> errors)
        {
            parsedSameAsMoveId = string.Empty;
            if (string.IsNullOrEmpty(raw)) return MoveTargetMode.None;
            if (raw.StartsWith("SameAsMove(", StringComparison.Ordinal) && raw.EndsWith(")", StringComparison.Ordinal))
            {
                parsedSameAsMoveId = raw.Substring("SameAsMove(".Length, raw.Length - "SameAsMove(".Length - 1);
                return MoveTargetMode.SameAsMove;
            }
            switch (raw)
            {
                case "None": return MoveTargetMode.None;
                case "RandomHero": return MoveTargetMode.RandomHero;
                case "AllHeroes": return MoveTargetMode.AllHeroes;
                case "SameAsMove": return MoveTargetMode.SameAsMove;
                default:
                    errors.Add(label + ".targetMode: unknown targetMode '" + raw + "'.");
                    return MoveTargetMode.RandomHero;
            }
        }

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            char first = id[0];
            if (first < 'a' || first > 'z') return false;
            for (int i = 1; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        private static List<GridCell> ParseCells(Dictionary<string, object> obj, string key, string label, List<string> errors)
        {
            var result = new List<GridCell>();
            if (!TryGetList(obj, key, out List<object> raw)) return result;
            for (int i = 0; i < raw.Count; i++)
            {
                if (raw[i] is Dictionary<string, object> cObj)
                {
                    result.Add(new GridCell(GetInt(cObj, "x", 0), GetInt(cObj, "y", 0)));
                }
                else
                {
                    errors.Add(label + "." + key + "[" + i + "]: expected {x,y}.");
                }
            }
            return result;
        }

        private static List<string> ParseStringList(Dictionary<string, object> obj, string key)
        {
            var list = new List<string>();
            if (TryGetList(obj, key, out List<object> raw))
            {
                for (int i = 0; i < raw.Count; i++)
                {
                    if (raw[i] is string s) list.Add(s);
                }
            }
            return list;
        }

        private static bool HasKey(Dictionary<string, object> obj, string key) =>
            obj != null && obj.ContainsKey(key);

        private static Dictionary<string, object> GetDict(Dictionary<string, object> obj, string key) =>
            obj != null && obj.TryGetValue(key, out object val) ? val as Dictionary<string, object> : null;

        private static bool TryGetList(Dictionary<string, object> obj, string key, out List<object> list)
        {
            if (obj != null && obj.TryGetValue(key, out object val) && val is List<object> l)
            {
                list = l;
                return true;
            }
            list = null;
            return false;
        }

        private static string GetString(Dictionary<string, object> obj, string key, string fallback) =>
            obj != null && obj.TryGetValue(key, out object val) && val is string s ? s : fallback;

        private static int GetInt(Dictionary<string, object> obj, string key, int fallback)
        {
            if (obj != null && obj.TryGetValue(key, out object val) && val is long l)
            {
                return (int)l;
            }
            return fallback;
        }

        private static bool GetBool(Dictionary<string, object> obj, string key, bool fallback) =>
            obj != null && obj.TryGetValue(key, out object val) && val is bool b ? b : fallback;
    }
}
