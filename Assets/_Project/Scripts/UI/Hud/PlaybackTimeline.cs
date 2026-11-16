using System;
using System.Collections.Generic;

namespace Project05.UI.Hud
{
    /// <summary>What a playback beat shows; the timeline only needs to know how it is paced.</summary>
    public enum BeatKind
    {
        /// <summary>A unit begins its action: ready lift for a hero, act pose for an enemy (whose glyph then flies).</summary>
        ActionStart,

        /// <summary>A hero item fires: its avatar pops in over the hero's head, holds, then performs its archetype.</summary>
        ItemFire,

        /// <summary>A hit lands (damage).</summary>
        Hit,

        /// <summary>A detonation lands.</summary>
        Detonation,

        /// <summary>Shield, heal, a status applied or consumed: lands without an impact.</summary>
        Minor,

        /// <summary>A unit falls.</summary>
        Down,
    }

    /// <summary>The six effect archetypes of art bible 5.11; each has its own motion and timing.</summary>
    public enum Archetype
    {
        /// <summary>Self buffs and passives: the avatar bobs.</summary>
        Pulse,

        /// <summary>Melee damage: a stepped arc toward the target, smear and spark.</summary>
        Swing,

        /// <summary>Ranged damage: hops to the target, trail and spark.</summary>
        Throw,

        /// <summary>Heal and shield: a ring grows on the target.</summary>
        Ring,

        /// <summary>Apply a status: a mote travels to the target.</summary>
        Status,

        /// <summary>Consume stacks for a burst.</summary>
        Detonate,
    }

    /// <summary>One playback beat: its kind, the archetype of its source, and which beat it belongs to.</summary>
    public struct PlaybackBeat
    {
        /// <summary>How the beat is paced.</summary>
        public BeatKind Kind;

        /// <summary>For an <see cref="BeatKind.ItemFire"/> or an enemy action: the archetype it performs.</summary>
        public Archetype Archetype;

        /// <summary>For an effect: the index of the beat that caused it (the item fire, or the enemy's action start); -1 if none.</summary>
        public int Source;

        /// <summary>For an action start: the actor is an enemy (its intent glyph flies to the target).</summary>
        public bool Enemy;
    }

    /// <summary>
    /// Pacing of combat playback at 1x, in seconds (art bible 5.10.6, slowed by the user on 2026-11-16 so every item can be
    /// read before it acts). Data, so it can be tuned in the scene without code.
    /// </summary>
    [Serializable]
    public sealed class PlaybackPacing
    {
        /// <summary>Hero ready lift (eyes narrow) or enemy act pose, before the first avatar or glyph.</summary>
        public float ready = 0.2f;

        /// <summary>Avatar pop-in.</summary>
        public float popIn = 0.1f;

        /// <summary>The avatar holds over the head before it acts, so the player sees which item fired.</summary>
        public float readHold = 0.18f;

        /// <summary>Travel time per archetype until contact: Pulse, Swing, Throw, Ring, Status, Detonate.</summary>
        public float[] travel = { 0.16f, 0.24f, 0.28f, 0.3f, 0.24f, 0.32f };

        /// <summary>Avatar exit after contact.</summary>
        public float exit = 0.12f;

        /// <summary>Share of the travel that has to pass before the next avatar may pop in (the next overlaps the rest).</summary>
        public float overlap = 0.6f;

        /// <summary>Gap between hits of the same source (multi-hit, cleave).</summary>
        public float hitGap = 0.13f;

        /// <summary>Time between the last landing of an action and the next action's start.</summary>
        public float handoff = 0.4f;

        /// <summary>Delay before a fall shows, after the hit that caused it.</summary>
        public float downDelay = 0.08f;

        /// <summary>Gap after a beat that has no source (a status expiring).</summary>
        public float minorGap = 0.08f;

        /// <summary>Travel time of an archetype.</summary>
        public float Travel(Archetype a) => travel != null && (int)a < travel.Length ? travel[(int)a] : 0.24f;

        /// <summary>Time from an avatar's pop-in to its contact.</summary>
        public float Contact(Archetype a) => popIn + readHold + Travel(a);

        /// <summary>Start-to-start time of two avatars of one action.</summary>
        public float Cadence(Archetype a) => popIn + readHold + Travel(a) * overlap;
    }

    /// <summary>
    /// Turns playback beats into start times. Inside an action the next avatar pops in while the previous one is still
    /// travelling; effects land at their avatar's contact; the next action starts a hand-off after the last landing.
    /// Times can be out of trace order (an avatar may pop in before the previous one lands); the caller plays beats in time
    /// order. Engine-free and deterministic, so the pacing can be tested.
    /// </summary>
    public static class PlaybackTimeline
    {
        /// <summary>Speed multipliers for 1x, 2x, 4x.</summary>
        public static readonly float[] Speeds = { 1f, 2f, 4f };

        /// <summary>Start time of every beat, in seconds from the start of playback.</summary>
        public static float[] Schedule(IReadOnlyList<PlaybackBeat> beats, PlaybackPacing p, int speedIndex)
        {
            float k = 1f / Speeds[Math.Max(0, Math.Min(2, speedIndex))];
            int n = beats.Count;
            var times = new float[n];
            var contact = new float[n];
            var lastLanding = new float[n];
            for (int i = 0; i < n; i++) lastLanding[i] = float.NegativeInfinity;

            float cursor = 0f;     // when the next avatar of the current action may pop in
            float latest = 0f;     // latest time used so far
            bool first = true;

            for (int i = 0; i < n; i++)
            {
                PlaybackBeat b = beats[i];
                float t;
                switch (b.Kind)
                {
                    case BeatKind.ActionStart:
                        t = first ? 0f : latest + p.handoff * k;
                        first = false;
                        cursor = t + p.ready * k;
                        if (b.Enemy)
                        {
                            contact[i] = cursor + p.Contact(b.Archetype) * k;
                            cursor = contact[i];
                        }

                        break;
                    case BeatKind.ItemFire:
                        t = cursor;
                        first = false;
                        contact[i] = t + p.Contact(b.Archetype) * k;
                        cursor = t + p.Cadence(b.Archetype) * k;
                        break;
                    default:
                        int s = b.Source;
                        if (s >= 0 && s < i)
                        {
                            if (b.Kind == BeatKind.Down)
                            {
                                t = Math.Max(contact[s], lastLanding[s]) + p.downDelay * k;
                            }
                            else
                            {
                                t = Math.Max(contact[s], lastLanding[s] + p.hitGap * k);
                                lastLanding[s] = t;
                            }
                        }
                        else
                        {
                            t = Math.Max(cursor, latest);
                            cursor = t + p.minorGap * k;
                        }

                        break;
                }

                times[i] = t;
                if (t > latest) latest = t;
            }

            return times;
        }

        /// <summary>Beat indices in the order they play: by time, ties kept in trace order.</summary>
        public static int[] PlayOrder(float[] times)
        {
            var order = new int[times.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => times[a] != times[b] ? times[a].CompareTo(times[b]) : a.CompareTo(b));
            return order;
        }
    }
}
