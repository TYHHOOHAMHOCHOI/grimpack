using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    public sealed class SynergyLink
    {
        public long SourceInstanceId { get; }
        public string SourceDefId { get; }
        public GridCell SourceAnchor { get; }
        public int RuleIndex { get; }
        public long TargetInstanceId { get; }
        public SynergyKind Kind { get; }
        public int Amount { get; }
        public string StatusId { get; }
        public GridCell SeamSourceCell { get; }
        public GridCell SeamTargetCell { get; }

        public SynergyLink(
            long sourceInstanceId,
            string sourceDefId,
            GridCell sourceAnchor,
            int ruleIndex,
            long targetInstanceId,
            SynergyKind kind,
            int amount,
            string statusId,
            GridCell seamSourceCell,
            GridCell seamTargetCell)
        {
            SourceInstanceId = sourceInstanceId;
            SourceDefId = sourceDefId ?? string.Empty;
            SourceAnchor = sourceAnchor;
            RuleIndex = ruleIndex;
            TargetInstanceId = targetInstanceId;
            Kind = kind;
            Amount = amount;
            StatusId = statusId ?? string.Empty;
            SeamSourceCell = seamSourceCell;
            SeamTargetCell = seamTargetCell;
        }
    }

    public sealed class ItemSynergySummary
    {
        private readonly Dictionary<string, int> _statusBonuses;

        public long TargetInstanceId { get; }
        public int DamageFlatTotal { get; }
        public int DamagePercentTotal { get; }
        public int CooldownReductionTotal { get; }
        public IReadOnlyList<SynergyLink> IncomingLinks { get; }

        internal ItemSynergySummary(
            long targetInstanceId,
            int damageFlatTotal,
            int damagePercentTotal,
            int cooldownReductionTotal,
            Dictionary<string, int> statusBonuses,
            IReadOnlyList<SynergyLink> incomingLinks)
        {
            TargetInstanceId = targetInstanceId;
            DamageFlatTotal = damageFlatTotal;
            DamagePercentTotal = damagePercentTotal;
            CooldownReductionTotal = cooldownReductionTotal;
            _statusBonuses = statusBonuses;
            IncomingLinks = incomingLinks ?? Array.Empty<SynergyLink>();
        }

        /// <summary>Extra stacks for a status: rules for that status plus rules for every status (empty id).</summary>
        public int GetStatusAmountBonus(string statusId)
        {
            if (statusId == null || _statusBonuses == null) return 0;
            int val = _statusBonuses.TryGetValue(statusId, out int v) ? v : 0;
            if (statusId.Length > 0 && _statusBonuses.TryGetValue(string.Empty, out int all)) val += all;
            return val;
        }

        /// <summary>Extra detonation value per stack for a status kind (DetonateValue rules for it and for every kind).</summary>
        public int GetDetonateValueBonus(string statusId)
        {
            if (_statusBonuses == null) return 0;
            int val = _statusBonuses.TryGetValue(DetonateKeys.For(statusId), out int v) ? v : 0;
            if (!string.IsNullOrEmpty(statusId) && _statusBonuses.TryGetValue(DetonateKeys.For(string.Empty), out int all)) val += all;
            return val;
        }
    }

    public sealed class GridSynergyView
    {
        private readonly Dictionary<long, ItemSynergySummary> _byTargetId;

        public IReadOnlyList<ItemSynergySummary> Items { get; }
        public IReadOnlyList<SynergyLink> AllLinks { get; }

        public GridSynergyView(IReadOnlyList<ItemSynergySummary> items, IReadOnlyList<SynergyLink> allLinks)
        {
            Items = items ?? Array.Empty<ItemSynergySummary>();
            AllLinks = allLinks ?? Array.Empty<SynergyLink>();
            _byTargetId = new Dictionary<long, ItemSynergySummary>(Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                _byTargetId[Items[i].TargetInstanceId] = Items[i];
            }
        }

        public ItemSynergySummary FindForItem(long instanceId) =>
            _byTargetId.TryGetValue(instanceId, out ItemSynergySummary summary) ? summary : null;
    }

    public sealed class SynergyPreviewResult
    {
        public GridOpCode Code { get; }
        public GridSynergyView ProjectedView { get; }
        public IReadOnlyList<SynergyLink> GainedLinks { get; }
        public IReadOnlyList<SynergyLink> LostLinks { get; }

        public SynergyPreviewResult(
            GridOpCode code,
            GridSynergyView projectedView,
            IReadOnlyList<SynergyLink> gainedLinks,
            IReadOnlyList<SynergyLink> lostLinks)
        {
            Code = code;
            ProjectedView = projectedView;
            GainedLinks = gainedLinks ?? Array.Empty<SynergyLink>();
            LostLinks = lostLinks ?? Array.Empty<SynergyLink>();
        }
    }

    /// <summary>
    /// Pure read-only adjacency and synergy evaluator (System 10).
    /// </summary>
    public static class AdjacencySynergyEvaluator
    {
        public static GridSynergyView Evaluate(ArrangementGridView heroGrid, GameCatalog catalog)
        {
            if (heroGrid == null || catalog == null)
            {
                return new GridSynergyView(Array.Empty<ItemSynergySummary>(), Array.Empty<SynergyLink>());
            }

            IReadOnlyList<PlacedItemView> items = heroGrid.Items;
            var incomingPerTarget = new List<SynergyLink>[items.Count];
            for (int i = 0; i < items.Count; i++) incomingPerTarget[i] = new List<SynergyLink>();

            for (int t = 0; t < items.Count; t++)
            {
                PlacedItemView targetView = items[t];
                ItemDef targetDef = catalog.FindItem(targetView.DefinitionId);
                if (targetDef == null) continue;

                for (int s = 0; s < items.Count; s++)
                {
                    if (s == t) continue;
                    PlacedItemView sourceView = items[s];
                    ItemDef sourceDef = catalog.FindItem(sourceView.DefinitionId);
                    if (sourceDef == null || sourceDef.Synergy.Count == 0) continue;

                    if (!TryFindSeam(sourceView.OccupiedCells, targetView.OccupiedCells, out GridCell seamSrc, out GridCell seamDst))
                    {
                        continue;
                    }

                    for (int r = 0; r < sourceDef.Synergy.Count; r++)
                    {
                        SynergyRuleDef rule = sourceDef.Synergy[r];
                        if (!TagMatches(rule, targetDef) || !IsApplicable(rule, targetDef)) continue;

                        incomingPerTarget[t].Add(new SynergyLink(
                            sourceView.InstanceId,
                            sourceDef.Id,
                            sourceView.AnchorCell,
                            r,
                            targetView.InstanceId,
                            rule.Kind,
                            rule.Amount,
                            rule.StatusId,
                            seamSrc,
                            seamDst));
                    }
                }

                incomingPerTarget[t].Sort(CompareLinksBySourceOrder);
            }

            var summaries = new List<ItemSynergySummary>(items.Count);
            var allLinks = new List<SynergyLink>();

            for (int t = 0; t < items.Count; t++)
            {
                List<SynergyLink> links = incomingPerTarget[t];
                int flat = 0;
                int pct = 0;
                int cdr = 0;
                Dictionary<string, int> statusBonuses = null;

                for (int k = 0; k < links.Count; k++)
                {
                    SynergyLink link = links[k];
                    allLinks.Add(link);
                    switch (link.Kind)
                    {
                        case SynergyKind.DamageFlat: flat += link.Amount; break;
                        case SynergyKind.DamagePercent: pct += link.Amount; break;
                        case SynergyKind.CooldownReduction: cdr += link.Amount; break;
                        case SynergyKind.StatusAmount:
                        case SynergyKind.DetonateValue:
                            if (statusBonuses == null) statusBonuses = new Dictionary<string, int>(StringComparer.Ordinal);
                            string key = link.Kind == SynergyKind.DetonateValue ? DetonateKeys.For(link.StatusId) : (link.StatusId ?? string.Empty);
                            statusBonuses.TryGetValue(key, out int cur);
                            statusBonuses[key] = cur + link.Amount;
                            break;
                    }
                }

                summaries.Add(new ItemSynergySummary(
                    items[t].InstanceId, flat, pct, cdr, statusBonuses, links));
            }

            allLinks.Sort(CompareLinksGlobalOrder);
            return new GridSynergyView(summaries, allLinks);
        }

        public static SynergyPreviewResult PreviewPlacement(
            ArrangementGridView heroGrid,
            ItemDef item,
            long existingInstanceId,
            int originX,
            int originY,
            int orientation,
            GameCatalog catalog)
        {
            if (heroGrid == null || item == null || catalog == null)
            {
                return new SynergyPreviewResult(GridOpCode.ContractViolation, null, Array.Empty<SynergyLink>(), Array.Empty<SynergyLink>());
            }

            if (orientation < 0 || orientation > 3)
            {
                return new SynergyPreviewResult(GridOpCode.InvalidOrientation, null, Array.Empty<SynergyLink>(), Array.Empty<SynergyLink>());
            }

            IReadOnlyList<GridCell> rotated = BackpackArrangement.RotateFootprint(item.Cells, orientation);
            var occupied = new List<GridCell>(rotated.Count);
            for (int i = 0; i < rotated.Count; i++)
            {
                int cx = originX + rotated[i].X;
                int cy = originY + rotated[i].Y;
                if (cx < 0 || cx >= heroGrid.Width || cy < 0 || cy >= heroGrid.Height)
                {
                    return new SynergyPreviewResult(GridOpCode.OutOfBounds, null, Array.Empty<SynergyLink>(), Array.Empty<SynergyLink>());
                }
                if (!heroGrid.IsOpen(cx, cy))
                {
                    return new SynergyPreviewResult(GridOpCode.LockedCell, null, Array.Empty<SynergyLink>(), Array.Empty<SynergyLink>());
                }
                occupied.Add(new GridCell(cx, cy));
            }

            // Check overlap against items other than existingInstanceId
            for (int i = 0; i < heroGrid.Items.Count; i++)
            {
                PlacedItemView other = heroGrid.Items[i];
                if (other.InstanceId == existingInstanceId) continue;
                for (int a = 0; a < occupied.Count; a++)
                {
                    for (int b = 0; b < other.OccupiedCells.Count; b++)
                    {
                        if (occupied[a] == other.OccupiedCells[b])
                        {
                            return new SynergyPreviewResult(GridOpCode.Occupied, null, Array.Empty<SynergyLink>(), Array.Empty<SynergyLink>());
                        }
                    }
                }
            }

            GridSynergyView beforeView = Evaluate(heroGrid, catalog);

            long previewId = existingInstanceId > 0 ? existingInstanceId : -999999L;
            var projectedItems = new List<PlacedItemView>(heroGrid.Items.Count + 1);
            for (int i = 0; i < heroGrid.Items.Count; i++)
            {
                PlacedItemView other = heroGrid.Items[i];
                if (other.InstanceId != existingInstanceId)
                {
                    projectedItems.Add(other);
                }
            }

            GridCell anchor = BackpackArrangement.ComputeAnchor(occupied);
            projectedItems.Add(new PlacedItemView(
                previewId, item.Id, heroGrid.Container, originX, originY, orientation, anchor, 0, occupied));
            projectedItems.Sort((a, b) =>
            {
                int cmpY = a.AnchorCell.Y.CompareTo(b.AnchorCell.Y);
                if (cmpY != 0) return cmpY;
                int cmpX = a.AnchorCell.X.CompareTo(b.AnchorCell.X);
                return cmpX != 0 ? cmpX : a.InstanceId.CompareTo(b.InstanceId);
            });

            var reindexed = new List<PlacedItemView>(projectedItems.Count);
            for (int i = 0; i < projectedItems.Count; i++)
            {
                PlacedItemView p = projectedItems[i];
                reindexed.Add(new PlacedItemView(
                    p.InstanceId, p.DefinitionId, p.Container,
                    p.OriginX, p.OriginY, p.Orientation,
                    p.AnchorCell, i, p.OccupiedCells));
            }

            var openMask = new bool[heroGrid.Width, heroGrid.Height];
            for (int y = 0; y < heroGrid.Height; y++)
            {
                for (int x = 0; x < heroGrid.Width; x++)
                {
                    openMask[x, y] = heroGrid.IsOpen(x, y);
                }
            }

            var projectedGrid = new ArrangementGridView(heroGrid.Container, heroGrid.Width, heroGrid.Height, openMask, reindexed);
            GridSynergyView afterView = Evaluate(projectedGrid, catalog);

            var gained = new List<SynergyLink>();
            var lost = new List<SynergyLink>();

            for (int i = 0; i < afterView.AllLinks.Count; i++)
            {
                SynergyLink l = afterView.AllLinks[i];
                if (!ContainsLinkKey(beforeView.AllLinks, l)) gained.Add(l);
            }
            for (int i = 0; i < beforeView.AllLinks.Count; i++)
            {
                SynergyLink l = beforeView.AllLinks[i];
                if (!ContainsLinkKey(afterView.AllLinks, l)) lost.Add(l);
            }

            return new SynergyPreviewResult(GridOpCode.Ok, afterView, gained, lost);
        }

        private static bool ContainsLinkKey(IReadOnlyList<SynergyLink> list, SynergyLink key)
        {
            for (int i = 0; i < list.Count; i++)
            {
                SynergyLink o = list[i];
                if (o.SourceInstanceId == key.SourceInstanceId &&
                    o.RuleIndex == key.RuleIndex &&
                    o.TargetInstanceId == key.TargetInstanceId)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryFindSeam(
            IReadOnlyList<GridCell> sourceCells,
            IReadOnlyList<GridCell> targetCells,
            out GridCell bestSrc,
            out GridCell bestDst)
        {
            bool found = false;
            bestSrc = default;
            bestDst = default;

            for (int i = 0; i < sourceCells.Count; i++)
            {
                GridCell s = sourceCells[i];
                for (int j = 0; j < targetCells.Count; j++)
                {
                    GridCell t = targetCells[j];
                    int dist = Math.Abs(s.X - t.X) + Math.Abs(s.Y - t.Y);
                    if (dist != 1) continue;

                    if (!found || CompareSeam(s, t, bestSrc, bestDst) < 0)
                    {
                        found = true;
                        bestSrc = s;
                        bestDst = t;
                    }
                }
            }

            return found;
        }

        private static int CompareSeam(GridCell s1, GridCell t1, GridCell s2, GridCell t2)
        {
            int c = s1.Y.CompareTo(s2.Y);
            if (c != 0) return c;
            c = s1.X.CompareTo(s2.X);
            if (c != 0) return c;
            c = t1.Y.CompareTo(t2.Y);
            return c != 0 ? c : t1.X.CompareTo(t2.X);
        }

        private static bool TagMatches(SynergyRuleDef rule, ItemDef target)
        {
            if (rule.TargetTags.Count == 0) return true;
            for (int i = 0; i < rule.TargetTags.Count; i++)
            {
                if (target.HasTag(rule.TargetTags[i])) return true;
            }
            return false;
        }

        private static bool IsApplicable(SynergyRuleDef rule, ItemDef target)
        {
            switch (rule.Kind)
            {
                case SynergyKind.DamageFlat:
                case SynergyKind.DamagePercent:
                    return target.DealsDamage;
                case SynergyKind.CooldownReduction:
                    return target.HasCooldown;
                case SynergyKind.StatusAmount:
                    return target.AppliesStatus(rule.StatusId);
                case SynergyKind.DetonateValue:
                    return target.Detonates;
                default:
                    return false;
            }
        }

        private static int CompareLinksBySourceOrder(SynergyLink a, SynergyLink b)
        {
            int c = a.SourceAnchor.Y.CompareTo(b.SourceAnchor.Y);
            if (c != 0) return c;
            c = a.SourceAnchor.X.CompareTo(b.SourceAnchor.X);
            if (c != 0) return c;
            c = a.SourceInstanceId.CompareTo(b.SourceInstanceId);
            return c != 0 ? c : a.RuleIndex.CompareTo(b.RuleIndex);
        }

        private static int CompareLinksGlobalOrder(SynergyLink a, SynergyLink b)
        {
            int c = CompareLinksBySourceOrder(a, b);
            return c != 0 ? c : a.TargetInstanceId.CompareTo(b.TargetInstanceId);
        }
    }
}
