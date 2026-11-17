using System;
using System.Collections;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI.Hud;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project05.CombatScene
{
    /// <summary>
    /// Plays a committed <see cref="CombatTrace"/> as a flowing fight rather than a slide show: every presented event becomes a
    /// beat on a <see cref="PlaybackTimeline"/> (actions overlap, hits of one action follow closely), the HUD takes the
    /// snapshot of each beat, and <see cref="CombatFx"/> adds the wind-up and lunge, the smear and spark, knock-back, blink,
    /// floating numbers and the detonation shake. Speed changes apply at once and keep the progress. Playback never alters
    /// simulation state or RNG.
    /// </summary>
    public sealed class CombatPlaybackController : MonoBehaviour
    {
        [Tooltip("Pacing of beats at 1x; 2x and 4x divide it.")]
        [SerializeField] private PlaybackPacing pacing = new PlaybackPacing();

        [Tooltip("Pause on each landing hit at 1x and 2x, in seconds, so impacts read.")]
        [SerializeField] private float hitStop = 0.045f;

        private CombatHudView _hud;
        private CombatFx _fx;

        /// <summary>The reactions layer (set by the scene before playback).</summary>
        public Project05.UI.Motion.FeedbackFx Feedback { get; set; }
        private ItemAvatarFx _avatars;
        private BackpackFx _backpack;
        private Func<long, RectTransform> _plateOf;
        private Func<string, (Sprite plate, Sprite glyph)> _itemSprites;
        private GameCatalog _catalog;
        private PreparedEncounter _prepared;
        private CombatResult _result;
        private Coroutine _routine;

        private int _room = 1;
        private int _roomCount = 8;
        private int _currency = 120;
        private int _speedIndex;
        private bool _skipRequested;
        private bool _paused;

        public bool IsPlaying { get; private set; }
        public int SpeedIndex => _speedIndex;

        public event Action<CombatResult> PlaybackCompleted;

        public void SetPaused(bool paused) => _paused = paused;

        public void SetSpeedIndex(int index) => HandleSpeedRequested(index);

        public void Initialize(CombatHudView hud)
        {
            _hud = hud;
            if (_hud != null)
            {
                _fx = _hud.GetComponent<CombatFx>();
                if (_fx == null) _fx = _hud.gameObject.AddComponent<CombatFx>();
                _fx.Initialize(_hud);
                _fx.Feedback = Feedback;
                HudSpriteSet set = _hud.Sprites;
                _avatars = _hud.GetComponent<ItemAvatarFx>() ?? _hud.gameObject.AddComponent<ItemAvatarFx>();
                _avatars.Initialize(_fx, set.vfxTrail, set.vfxTrailFoe, set.vfxMote, set.vfxMoteFoe);
                _backpack = _hud.GetComponent<BackpackFx>() ?? _hud.gameObject.AddComponent<BackpackFx>();
                _backpack.Initialize(set.dimMask);
            }

            if (_hud != null && _hud.Speed != null)
            {
                _hud.Speed.SpeedRequested -= HandleSpeedRequested;
                _hud.Speed.SpeedRequested += HandleSpeedRequested;
            }
        }

        private void OnDestroy()
        {
            if (_hud != null && _hud.Speed != null)
            {
                _hud.Speed.SpeedRequested -= HandleSpeedRequested;
            }
        }

        private void Update()
        {
            if (!IsPlaying || _paused) return;
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleSpeedRequested((_speedIndex + 1) % 4);
            }
        }

        public void Play(GameCatalog catalog, PreparedEncounter prepared, CombatResult result, int room, int roomCount, int currency)
        {
            StopPlayback();
            _catalog = catalog;
            _prepared = prepared;
            _result = result;
            _room = room;
            _roomCount = roomCount;
            _currency = currency;
            _skipRequested = false;

            if (result == null || result.Trace == null || result.Trace.Events.Count == 0)
            {
                IsPlaying = false;
                PlaybackCompleted?.Invoke(result);
                return;
            }

            if (_speedIndex == 3)
            {
                // Skip selected before start: apply final snapshot immediately.
                ApplyFinalSnapshot();
                IsPlaying = false;
                PlaybackCompleted?.Invoke(result);
                return;
            }

            IsPlaying = true;
            _routine = StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// Gives playback what it needs to show items: the backpack plate of a simulated item instance, and the 1x1 tier plate
        /// and glyph of an item definition for its avatar.
        /// </summary>
        public void SetPresentation(Func<long, RectTransform> plateOf, Func<string, (Sprite plate, Sprite glyph)> itemSprites)
        {
            _plateOf = plateOf;
            _itemSprites = itemSprites;
        }

        public void StopPlayback()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            IsPlaying = false;
            _skipRequested = false;
            if (_fx != null) _fx.Clear();
            if (_avatars != null) _avatars.Clear();
            if (_backpack != null) _backpack.Clear();
            if (_hud != null) _hud.DrawReadouts = true;
        }

        public void HandleSpeedRequested(int index)
        {
            if (_paused) return;
            _speedIndex = Mathf.Clamp(index, 0, 3);
            if (_speedIndex == 3 && IsPlaying)
            {
                _skipRequested = true;
            }
        }

        private IEnumerator PlayRoutine()
        {
            CombatTrace trace = _result.Trace;
            if (_hud != null) _hud.DrawReadouts = false;     // numbers come from CombatFx, pooled, not rebuilt each beat

            if (trace.Baseline != null && _hud != null)
            {
                _hud.Apply(CombatHudMapper.MapCombatSnapshot(
                    _catalog, _prepared, trace.Baseline, null, _room, _roomCount, _currency, _speedIndex, false));
                UpdateCooldowns(trace.Baseline);
            }

            List<CombatEventRecord> events = BuildBeats(trace, out List<PlaybackBeat> beats);
            float[] times = PlaybackTimeline.Schedule(beats, pacing, 0);
            int[] order = PlaybackTimeline.PlayOrder(times);
            float clock = 0f;          // playback time in 1x seconds, so a speed change keeps the progress
            float stopUntil = -1f;     // hit-stop, in real seconds
            long appliedSeq = -1;
            int next = 0;

            while (next < order.Length)
            {
                if (_skipRequested) break;
                if (!_paused && Time.unscaledTime >= stopUntil) clock += Time.unscaledDeltaTime * Speed;

                while (next < order.Length && times[order[next]] <= clock && !_skipRequested)
                {
                    int i = order[next];
                    bool landed = Present(trace, events, beats, i, ref appliedSeq);
                    if (landed && _speedIndex < 2 && hitStop > 0f) stopUntil = Time.unscaledTime + hitStop;
                    next++;
                }

                yield return null;
            }

            // Let the last avatar finish and the last numbers fade before the result shows.
            float settle = (pacing.exit + pacing.handoff + 0.3f) / Speed;
            while (settle > 0f && !_skipRequested)
            {
                if (!_paused) settle -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (_fx != null) _fx.Clear();
            if (_avatars != null) _avatars.Clear();
            if (_backpack != null) _backpack.Clear();
            ApplyFinalSnapshot();
            if (_hud != null) _hud.DrawReadouts = true;
            _routine = null;
            IsPlaying = false;
            PlaybackCompleted?.Invoke(_result);
        }

        private float Speed => PlaybackTimeline.Speeds[Mathf.Clamp(_speedIndex, 0, 2)];

        /// <summary>The presented events and, for each, its beat: kind, archetype, and the beat that caused it.</summary>
        private List<CombatEventRecord> BuildBeats(CombatTrace trace, out List<PlaybackBeat> beats)
        {
            var events = new List<CombatEventRecord>();
            beats = new List<PlaybackBeat>();
            int lastEnemyStart = -1;
            for (int i = 0; i < trace.Events.Count; i++)
            {
                CombatEventRecord ev = trace.Events[i];
                if (!ShouldPresentEvent(ev)) continue;
                var beat = new PlaybackBeat { Kind = KindOf(ev), Source = -1, Archetype = Archetype.Swing };
                int index = events.Count;
                switch (beat.Kind)
                {
                    case BeatKind.ActionStart:
                        beat.Enemy = ev.Actor.IsValid && ev.Actor.Side == UnitSide.Enemy;
                        lastEnemyStart = beat.Enemy ? index : -1;
                        break;
                    case BeatKind.ItemFire:
                        beat.Archetype = ArchetypeOf(_catalog?.FindItem(ev.Origin.ItemDefId));
                        break;
                    default:
                        beat.Source = SourceOf(ev, events, beats, lastEnemyStart);
                        break;
                }

                events.Add(ev);
                beats.Add(beat);
            }

            return events;
        }

        /// <summary>The beat that caused an effect: the latest fire of the same item, or the acting enemy's start.</summary>
        private static int SourceOf(CombatEventRecord ev, List<CombatEventRecord> events, List<PlaybackBeat> beats, int lastEnemyStart)
        {
            if (ev.Origin != null && ev.Origin.Kind == OriginKind.Item)
            {
                for (int j = events.Count - 1; j >= 0; j--)
                {
                    if (beats[j].Kind == BeatKind.ItemFire && events[j].Origin != null && events[j].Origin.ItemInstanceId == ev.Origin.ItemInstanceId) return j;
                }
            }
            else if (ev.Origin != null && ev.Origin.Kind == OriginKind.EnemyMove && lastEnemyStart >= 0)
            {
                return lastEnemyStart;
            }

            // a fall belongs to whatever hit before it
            if (ev.Kind == CombatEventKind.UnitDowned && beats.Count > 0) return beats[beats.Count - 1].Source;
            return -1;
        }

        /// <summary>The effect archetype of an item (art bible 5.11), from what it does.</summary>
        public static Archetype ArchetypeOf(ItemDef def)
        {
            if (def == null) return Archetype.Pulse;
            bool damage = false, status = false, detonate = false, support = false, ranged = false, weapon = false;
            foreach (EffectDef e in def.Effects)
            {
                switch (e.Kind)
                {
                    case EffectKind.Damage: damage = true; break;
                    case EffectKind.ApplyStatus: status = true; break;
                    case EffectKind.ConsumeDetonate:
                    case EffectKind.DetonateAll: detonate = true; break;
                    default: support = true; break;
                }
            }

            foreach (string tag in def.Tags)
            {
                if (tag == "ranged") ranged = true;
                if (tag == "weapon" || tag == "shield") weapon = true;
            }

            if (detonate) return Archetype.Detonate;
            if (damage) return ranged || !weapon ? Archetype.Throw : Archetype.Swing;
            if (status) return Archetype.Status;
            if (support) return Archetype.Ring;
            return Archetype.Pulse;
        }

        /// <summary>Applies the snapshot of one beat (never an older one) and plays its motion. True when a hit landed.</summary>
        private bool Present(CombatTrace trace, List<CombatEventRecord> events, List<PlaybackBeat> beats, int index, ref long appliedSeq)
        {
            CombatEventRecord ev = events[index];
            PlaybackBeat beat = beats[index];
            if (_hud != null && ev.Seq > appliedSeq)
            {
                appliedSeq = ev.Seq;
                int snapIdx = Mathf.Clamp((int)ev.Seq, 0, trace.SnapshotsBySeq.Count - 1);
                bool idleToggle = ((int)(Time.unscaledTime * 2.2f) & 1) == 1;   // idle breathes on time, not per event
                CombatStateSnapshot snap = trace.SnapshotsBySeq[snapIdx];
                _hud.Apply(CombatHudMapper.MapCombatSnapshot(
                    _catalog, _prepared, snap, ev, _room, _roomCount, _currency, _speedIndex, idleToggle));
                UpdateCooldowns(snap);
            }

            if (_fx == null) return false;
            _fx.Speed = Speed;
            _fx.Feedback = Feedback;
            if (_avatars != null) _avatars.Speed = Speed;
            if (_backpack != null) _backpack.Speed = Speed;

            switch (beat.Kind)
            {
                case BeatKind.ActionStart:
                    if (beat.Enemy && ev.Actor.IsValid)
                    {
                        bool hasTarget = FirstTarget(events, beats, index, out UnitRef target);
                        _fx.Lunge(false, ev.Actor.Slot, hasTarget, pacing.ready, pacing.popIn + pacing.readHold, pacing.handoff, 6f);
                        if (_avatars != null)
                        {
                            Sprite glyph = (_hud.Slot(false, ev.Actor.Slot) as EnemySlotView)?.IntentSprite;
                            _avatars.EnemyGlyph(ev.Actor.Slot, glyph, beat.Archetype, hasTarget, hasTarget ? target.Slot : 0, pacing);
                        }
                    }

                    return false;
                case BeatKind.ItemFire:
                    PresentItem(events, beats, index);
                    return false;
                case BeatKind.Hit:
                case BeatKind.Detonation:
                    if (!ev.Target.IsValid) return false;
                    bool byHero = AttackerIsHero(events, beats, index);
                    Archetype source = beat.Source >= 0 ? beats[beat.Source].Archetype : Archetype.Swing;
                    bool firstOfSource = !HasEarlierLanding(beats, index);
                    _fx.Hit(ev.Target.Side == UnitSide.Hero, ev.Target.Slot, ev.ResolvedAmount, byHero,
                        firstOfSource && source == Archetype.Swing, beat.Kind == BeatKind.Detonation);
                    return ev.ResolvedAmount > 0;
                case BeatKind.Down:
                    if (ev.Target.IsValid) _fx.Down(ev.Target.Side == UnitSide.Hero, ev.Target.Slot);
                    return false;
                default:
                    if ((ev.Kind == CombatEventKind.ShieldGained || ev.Kind == CombatEventKind.Healed) && ev.Target.IsValid)
                    {
                        _fx.Gain(ev.Target.Side == UnitSide.Hero, ev.Target.Slot, ev.ResolvedAmount);
                    }

                    return false;
            }
        }

        /// <summary>A hero item fires: its plate swells in the backpack, its avatar pops over the hero and performs.</summary>
        private void PresentItem(List<CombatEventRecord> events, List<PlaybackBeat> beats, int index)
        {
            CombatEventRecord ev = events[index];
            Archetype kind = beats[index].Archetype;
            if (_backpack != null && _plateOf != null) _backpack.Fire(_plateOf(ev.Origin.ItemInstanceId));

            bool hasTarget = FirstTarget(events, beats, index, out UnitRef target);
            int heroSlot = ev.Origin.HeroSlot >= 0 ? ev.Origin.HeroSlot : 0;
            if (kind == Archetype.Swing)
            {
                // the hero winds up while the avatar is read, and strikes with it
                _fx.Lunge(true, heroSlot, hasTarget, pacing.popIn + pacing.readHold, pacing.Travel(kind), pacing.handoff, 6f);
            }

            if (_avatars == null) return;
            (Sprite plate, Sprite glyph) sprites = _itemSprites != null ? _itemSprites(ev.Origin.ItemDefId) : (null, null);
            ItemDef def = _catalog?.FindItem(ev.Origin.ItemDefId);
            _avatars.HeroItem(heroSlot, kind, sprites.plate, sprites.glyph, hasTarget, hasTarget && target.Side == UnitSide.Hero,
                hasTarget ? target.Slot : heroSlot, RingColour(def), pacing);
        }

        private static Color32 RingColour(ItemDef def)
        {
            switch (def != null ? def.Tier.ToString().ToLowerInvariant() : "white")
            {
                case "green": return new Color32(0x3C, 0xB8, 0x78, 0xFF);
                case "blue": return new Color32(0x3E, 0x9B, 0xE8, 0xFF);
                case "purple": return new Color32(0x9A, 0x5C, 0xEB, 0xFF);
                case "gold": return new Color32(0xE6, 0xB4, 0x22, 0xFF);
                case "red": return new Color32(0xE5, 0x26, 0x3F, 0xFF);
                default: return new Color32(0xDD, 0xE3, 0xEE, 0xFF);
            }
        }

        /// <summary>Dims every charging item plate by the share of its cooldown still to go.</summary>
        private void UpdateCooldowns(CombatStateSnapshot snap)
        {
            if (_backpack == null || _plateOf == null || snap == null || snap.Heroes == null) return;
            foreach (HeroStateView hero in snap.Heroes)
            {
                if (hero == null) continue;
                foreach (ItemCooldownView cd in hero.ItemCooldowns)
                {
                    if (cd.TriggerKind != TriggerKind.Cooldown || cd.EffectiveCooldown <= 0) continue;
                    float remaining = cd.IsReady ? 0f : Mathf.Clamp01((float)cd.ActionsRemaining / cd.EffectiveCooldown);
                    _backpack.SetCooldown(_plateOf(cd.InstanceId), remaining);
                }
            }
        }

        private static BeatKind KindOf(CombatEventRecord ev)
        {
            switch (ev.Kind)
            {
                case CombatEventKind.ActionStarted: return BeatKind.ActionStart;
                case CombatEventKind.ItemFired: return BeatKind.ItemFire;
                case CombatEventKind.Damage: return ev.ResolvedAmount > 0 ? BeatKind.Hit : BeatKind.Minor;
                case CombatEventKind.DetonationDamage: return BeatKind.Detonation;
                case CombatEventKind.UnitDowned: return BeatKind.Down;
                default: return BeatKind.Minor;
            }
        }

        /// <summary>The first target an item fire or an enemy action reaches, if any.</summary>
        private static bool FirstTarget(List<CombatEventRecord> events, List<PlaybackBeat> beats, int source, out UnitRef target)
        {
            for (int i = source + 1; i < events.Count; i++)
            {
                if (beats[i].Kind == BeatKind.ActionStart) break;
                if (beats[i].Source == source && events[i].Target.IsValid
                    && (beats[i].Kind == BeatKind.Hit || beats[i].Kind == BeatKind.Detonation || beats[i].Kind == BeatKind.Minor))
                {
                    target = events[i].Target;
                    return true;
                }
            }

            target = UnitRef.None;
            return false;
        }

        private static bool HasEarlierLanding(List<PlaybackBeat> beats, int index)
        {
            int source = beats[index].Source;
            if (source < 0) return false;
            for (int i = source + 1; i < index; i++)
            {
                if (beats[i].Source == source && (beats[i].Kind == BeatKind.Hit || beats[i].Kind == BeatKind.Detonation)) return true;
            }

            return false;
        }

        /// <summary>The side that caused a hit: the source's actor (hero items count as heroes).</summary>
        private static bool AttackerIsHero(List<CombatEventRecord> events, List<PlaybackBeat> beats, int index)
        {
            int source = beats[index].Source;
            if (source >= 0) return beats[source].Kind == BeatKind.ItemFire || !beats[source].Enemy;
            if (events[index].Actor.IsValid) return events[index].Actor.Side == UnitSide.Hero;
            return events[index].Target.Side != UnitSide.Hero;
        }

        private void ApplyFinalSnapshot()
        {
            if (_hud == null || _result == null || _result.Trace == null) return;
            var snaps = _result.Trace.SnapshotsBySeq;
            if (snaps == null || snaps.Count == 0) return;

            CombatStateSnapshot finalSnap = snaps[snaps.Count - 1];
            CombatHudData data = CombatHudMapper.MapCombatSnapshot(
                _catalog, _prepared, finalSnap, null, _room, _roomCount, _currency, _speedIndex, false);
            _hud.Apply(data);
        }

        private static bool ShouldPresentEvent(CombatEventRecord ev)
        {
            if (ev == null) return false;
            switch (ev.Kind)
            {
                case CombatEventKind.IntentsBound:
                case CombatEventKind.CombatEnded:
                    return false;
                default:
                    return true;
            }
        }
    }
}
