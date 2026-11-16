using System;
using System.Collections;
using Project05.CombatCore;
using Project05.UI.Hud;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project05.CombatScene
{
    /// <summary>
    /// Steps through a committed <see cref="CombatTrace"/> and feeds <see cref="CombatStateSnapshot"/>
    /// views to <see cref="CombatHudView"/> according to the selected speed (1x, 2x, 4x, Skip).
    /// Playback never alters simulation state or RNG.
    /// </summary>
    public sealed class CombatPlaybackController : MonoBehaviour
    {
        private CombatHudView _hud;
        private GameCatalog _catalog;
        private PreparedEncounter _prepared;
        private CombatResult _result;
        private Coroutine _routine;

        private int _room = 1;
        private int _roomCount = 8;
        private int _currency = 120;
        private int _speedIndex;
        private bool _skipRequested;

        public bool IsPlaying { get; private set; }
        public int SpeedIndex => _speedIndex;

        public event Action<CombatResult> PlaybackCompleted;

        public void Initialize(CombatHudView hud)
        {
            _hud = hud;
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
            if (!IsPlaying) return;
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

        public void StopPlayback()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            IsPlaying = false;
            _skipRequested = false;
        }

        public void HandleSpeedRequested(int index)
        {
            _speedIndex = Mathf.Clamp(index, 0, 3);
            if (_speedIndex == 3 && IsPlaying)
            {
                _skipRequested = true;
            }
        }

        private IEnumerator PlayRoutine()
        {
            CombatTrace trace = _result.Trace;

            // Show baseline first
            if (trace.Baseline != null && _hud != null)
            {
                CombatHudData baselineHud = CombatHudMapper.MapCombatSnapshot(
                    _catalog, _prepared, trace.Baseline, null, _room, _roomCount, _currency, _speedIndex, false);
                _hud.Apply(baselineHud);
            }

            for (int i = 0; i < trace.Events.Count; i++)
            {
                if (_skipRequested)
                {
                    ApplyFinalSnapshot();
                    break;
                }

                CombatEventRecord ev = trace.Events[i];
                if (!ShouldPresentEvent(ev)) continue;

                int snapIdx = Mathf.Clamp((int)ev.Seq, 0, trace.SnapshotsBySeq.Count - 1);
                CombatStateSnapshot snap = trace.SnapshotsBySeq[snapIdx];

                if (_hud != null)
                {
                    CombatHudData hudData = CombatHudMapper.MapCombatSnapshot(
                        _catalog, _prepared, snap, ev, _room, _roomCount, _currency, _speedIndex, (i % 2) == 1);
                    _hud.Apply(hudData);
                }

                float wait = GetStepDuration(ev);
                float elapsed = 0f;
                while (elapsed < wait && !_skipRequested)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            ApplyFinalSnapshot();
            _routine = null;
            IsPlaying = false;
            PlaybackCompleted?.Invoke(_result);
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

        private float GetStepDuration(CombatEventRecord ev)
        {
            float baseDur;
            switch (_speedIndex)
            {
                case 1: baseDur = 0.20f; break;
                case 2: baseDur = 0.09f; break;
                case 3: return 0f;
                default: baseDur = 0.40f; break;
            }

            if (ev == null) return baseDur;
            switch (ev.Kind)
            {
                case CombatEventKind.ActionStarted:
                case CombatEventKind.ActionSkipped:
                case CombatEventKind.StatusExpired:
                    return baseDur * 0.45f;
                case CombatEventKind.DetonationDamage:
                    // Art Bible 7.5: Detonation peak plays slower for readability unless 4x/Skip.
                    return _speedIndex == 0 ? 0.65f : baseDur * 1.4f;
                case CombatEventKind.Damage:
                case CombatEventKind.ItemFired:
                    return baseDur;
                default:
                    return baseDur * 0.7f;
            }
        }
    }
}
