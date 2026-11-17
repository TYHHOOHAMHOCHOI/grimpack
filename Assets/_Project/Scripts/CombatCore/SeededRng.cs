using System;
using System.Collections.Generic;
using System.Text;

namespace Project05.CombatCore
{
    /// <summary>Snapshot of one named RNG stream.</summary>
    public sealed class RngStreamSnapshot
    {
        public string Name { get; }
        public ulong State { get; }
        public ulong Increment { get; }

        public RngStreamSnapshot(string name, ulong state, ulong increment)
        {
            Name = name ?? string.Empty;
            State = state;
            Increment = increment;
        }
    }

    /// <summary>Full serializable snapshot of a SeededRng instance.</summary>
    public sealed class RngSnapshot
    {
        public ulong Seed { get; }
        public int Version { get; }
        public IReadOnlyList<RngStreamSnapshot> Streams { get; }

        public RngSnapshot(ulong seed, int version, IReadOnlyList<RngStreamSnapshot> streams)
        {
            Seed = seed;
            Version = version;
            Streams = streams ?? Array.Empty<RngStreamSnapshot>();
        }
    }

    /// <summary>
    /// Deterministic PCG-XSH-RR 32-bit output generator with integer rejection sampling.
    /// Zero floating point, zero System.Random.
    /// </summary>
    public sealed class RngStream
    {
        private ulong _state;
        private ulong _inc;

        public string Name { get; }
        internal ulong State => _state;
        internal ulong Increment => _inc;

        internal RngStream(string name, ulong state, ulong increment)
        {
            Name = name;
            _state = state;
            _inc = increment | 1UL;
        }

        public uint NextUInt32()
        {
            ulong oldState = _state;
            _state = unchecked(oldState * 6364136223846793005UL + _inc);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>
        /// Draws a uniformly distributed integer in [0, bound) using 64-bit rejection threshold L = 2^32 - (2^32 mod bound).
        /// </summary>
        public int NextBounded(int bound)
        {
            if (bound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Bound must be at least 1.");
            }

            if (bound == 1)
            {
                NextUInt32();
                return 0;
            }

            ulong b = (ulong)bound;
            ulong threshold = 4294967296UL - (4294967296UL % b);

            while (true)
            {
                ulong word = NextUInt32();
                if (word < threshold)
                {
                    return (int)(word % b);
                }
            }
        }

        /// <summary>
        /// Draws a candidate index in [0, weights.Count) proportional to non-negative integer weights.
        /// Validates all weights before consuming any RNG words.
        /// </summary>
        public int NextWeighted(IReadOnlyList<int> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                throw new ArgumentException("Weights must be non-empty.", nameof(weights));
            }

            ulong total = 0UL;
            for (int i = 0; i < weights.Count; i++)
            {
                int w = weights[i];
                if (w < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), "Weights must be non-negative.");
                }
                total += (ulong)w;
                if (total > 4294967296UL)
                {
                    throw new OverflowException("Total weight exceeds 2^32.");
                }
            }

            if (total == 0UL)
            {
                throw new ArgumentException("Total weight must be greater than zero.", nameof(weights));
            }

            ulong ticket;
            if (total == 4294967296UL)
            {
                ticket = NextUInt32();
            }
            else
            {
                ulong threshold = 4294967296UL - (4294967296UL % total);
                while (true)
                {
                    ulong word = NextUInt32();
                    if (word < threshold)
                    {
                        ticket = word % total;
                        break;
                    }
                }
            }

            ulong cumulative = 0UL;
            for (int i = 0; i < weights.Count; i++)
            {
                int w = weights[i];
                if (w <= 0) continue;
                cumulative += (ulong)w;
                if (ticket < cumulative)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }

        public RngStream Clone() => new RngStream(Name, _state, _inc);

        internal void CopyFrom(RngStream source)
        {
            _state = source._state;
            _inc = source._inc;
        }

        internal void RestoreState(ulong state, ulong increment)
        {
            _state = state;
            _inc = increment;
        }
    }

    /// <summary>
    /// Run-owned deterministic RNG with independent named substreams ("doors", "drops", "intents").
    /// </summary>
    public sealed class SeededRng
    {
        public const int CurrentVersion = 1;
        public static readonly string[] StreamNames = { "doors", "drops", "intents" };

        private readonly RngStream[] _streams;

        public ulong Seed { get; private set; }

        public SeededRng(ulong seed)
        {
            Seed = seed;
            _streams = new RngStream[StreamNames.Length];
            for (int i = 0; i < StreamNames.Length; i++)
            {
                _streams[i] = DeriveStream(seed, StreamNames[i]);
            }
        }

        private SeededRng(ulong seed, RngStream[] streams)
        {
            Seed = seed;
            _streams = streams;
        }

        public RngStream GetStream(string name)
        {
            if (name != null)
            {
                for (int i = 0; i < _streams.Length; i++)
                {
                    if (string.Equals(_streams[i].Name, name, StringComparison.Ordinal))
                    {
                        return _streams[i];
                    }
                }
            }

            throw new ArgumentException("Unknown RNG stream: " + (name ?? "<null>"), nameof(name));
        }

        public RngSnapshot CaptureSnapshot()
        {
            var snapshots = new RngStreamSnapshot[_streams.Length];
            for (int i = 0; i < _streams.Length; i++)
            {
                snapshots[i] = new RngStreamSnapshot(_streams[i].Name, _streams[i].State, _streams[i].Increment);
            }
            return new RngSnapshot(Seed, CurrentVersion, snapshots);
        }

        public bool TryRestore(RngSnapshot snapshot, out string error)
        {
            if (snapshot == null)
            {
                error = "Snapshot is null.";
                return false;
            }

            if (snapshot.Version != CurrentVersion)
            {
                error = "Unsupported RNG snapshot version: " + snapshot.Version;
                return false;
            }

            if (snapshot.Streams == null || snapshot.Streams.Count != StreamNames.Length)
            {
                error = "Snapshot stream count mismatch.";
                return false;
            }

            var matched = new RngStreamSnapshot[StreamNames.Length];
            for (int i = 0; i < snapshot.Streams.Count; i++)
            {
                RngStreamSnapshot entry = snapshot.Streams[i];
                if (entry == null || string.IsNullOrEmpty(entry.Name))
                {
                    error = "Malformed stream entry in snapshot.";
                    return false;
                }

                int slot = -1;
                for (int j = 0; j < StreamNames.Length; j++)
                {
                    if (string.Equals(StreamNames[j], entry.Name, StringComparison.Ordinal))
                    {
                        slot = j;
                        break;
                    }
                }

                if (slot < 0)
                {
                    error = "Unexpected stream name in snapshot: " + entry.Name;
                    return false;
                }

                if (matched[slot] != null)
                {
                    error = "Duplicate stream name in snapshot: " + entry.Name;
                    return false;
                }

                if ((entry.Increment & 1UL) == 0UL)
                {
                    error = "Invalid PCG increment (must be odd) for stream: " + entry.Name;
                    return false;
                }

                matched[slot] = entry;
            }

            for (int i = 0; i < matched.Length; i++)
            {
                if (matched[i] == null)
                {
                    error = "Missing stream in snapshot: " + StreamNames[i];
                    return false;
                }
            }

            Seed = snapshot.Seed;
            for (int i = 0; i < _streams.Length; i++)
            {
                _streams[i].RestoreState(matched[i].State, matched[i].Increment);
            }

            error = null;
            return true;
        }

        public SeededRng Clone()
        {
            var copies = new RngStream[_streams.Length];
            for (int i = 0; i < _streams.Length; i++)
            {
                copies[i] = _streams[i].Clone();
            }
            return new SeededRng(Seed, copies);
        }

        /// <summary>
        /// A stream derived from the run seed exactly like the named substreams, but owned by its caller and outside the
        /// snapshot set (item-fusion.md Rule 4: the "fusion" stream never touches "doors"). The name must not be one of
        /// <see cref="StreamNames"/>.
        /// </summary>
        public static RngStream DeriveSideStream(ulong seed, string streamName)
        {
            if (string.IsNullOrEmpty(streamName) || Array.IndexOf(StreamNames, streamName) >= 0)
                throw new ArgumentException("A side stream needs its own name: " + (streamName ?? "<null>"), nameof(streamName));
            return DeriveStream(seed, streamName);
        }

        private static RngStream DeriveStream(ulong seed, string streamName)
        {
            ulong h1 = SplitMix64(seed ^ 0x9E3779B97F4A7C15UL);
            ulong h2 = SplitMix64(seed ^ 0xBF58476D1CE4E5B9UL);

            byte[] bytes = Encoding.ASCII.GetBytes(streamName);
            for (int i = 0; i < bytes.Length; i++)
            {
                h1 = SplitMix64(h1 ^ ((ulong)bytes[i] + (ulong)(i * 0x100)));
                h2 = SplitMix64(h2 ^ (((ulong)bytes[i] << 8) + (ulong)i));
            }

            ulong inc = h2 | 1UL;
            var stream = new RngStream(streamName, 0UL, inc);
            stream.RestoreState(unchecked(h1 + inc), inc);
            stream.NextUInt32();
            return stream;
        }

        private static ulong SplitMix64(ulong z)
        {
            z = unchecked(z + 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            return z ^ (z >> 31);
        }
    }
}
