using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rant
{
    /// <summary>
    /// Represents a non-linear random number generator.
    /// </summary>
    public class RNG
    {
        private static readonly ulong[] Table =
        {
            0x78601daa5225473d, 0x21d0a62df46ee118, 0xdcf4f088ea63c826, 0x8b509ad5d20d2223, 0x36d7e1354010ad6a, 0xceb515261afffce4, 0x1546c29edf0dc626, 0xc8b67f24d6cf5a39,
            0x46d520ebd22344c9, 0x36d0ff0b68507522, 0xa8567c0883f5e5d2, 0xb9c5b11b91b965b8, 0x7927e34e27fcd89e, 0x74fa1d2dfa24372b, 0x9e4324a0ee16b0e7, 0x0e677c9baf70e016,
            0xe5d59b272bb7b445, 0x743d63fc26d7e3aa, 0x750880c5f5a4707e, 0xb36e62dd2caa7fde, 0x606c8dc97617cc2e, 0xf09a2e086d148387, 0x84c2bc508e19c20a, 0x83a592c8477ff525,
            0xe2f50642dee01a7b, 0x77de323275f73aff, 0x5b8f5e6e73a28b1f, 0x9452249863063810, 0xc96e461a3eace646, 0x389d30b5c53e0d73, 0x80b2f00c43973999, 0x30a299aa889508cf,
            0xf4b1c46101061b27, 0x34ef2c34f55d8b54, 0xb46bb2089bff2880, 0x1a31ed66094b564c, 0xb877b575c296313e, 0x9da055cb05da778c, 0x1693fd96d5430359, 0xc1415ca2a1f488d3,
            0x9bc8b205553867b9, 0xed2cb34d9b612804, 0xd13b13dd35323c4b, 0x91a9828309316040, 0x5876f27c34037317, 0xf106bf028861af01, 0x1348b5b17c5cb59b, 0x156e2b52366a3666,
            0xddfad475ca1a9353, 0xf0993c4253bd8285, 0x0bb378c881a002c7, 0x62b72d156abddc09, 0x4142216203284aa1, 0x0850e8b16b9a63cf, 0xfc6576222e997db5, 0xd56f15f499e5dae1,
            0x8452b64ce6a918e1, 0x3cc1be80a621ecc0, 0x0f92f6b30c6fd230, 0x36bc3ec68b534fff, 0x3a50dead9054f02b, 0x4f99ff059bbe644b, 0x18b2d0bef3306b9d, 0xe48e412b9f554268,
            0x8ae03d6eb9609c86, 0x9704f36b23ea3b62, 0x3d7e3f21d412232c, 0x701daff720a409c7, 0x19ad2ca2612fa587, 0xb0a207da9dc6cd24, 0xcb4d86ea80e7fe70, 0x9902dafb46f5a9db,
            0x8cc881a8eb59de7a, 0x44892c768f48b167, 0x34df3c57543efd39, 0x1a26c1b71eaa7fb6, 0x2aad0a990e79db79, 0xe2efa307b565b94f, 0x58ccc96016ded1b5, 0x839c4900f4cbfba0,
            0x45b21b9b7ea54cae, 0xafb0dbd0f0a55d61, 0x7d281d2a022fd41c, 0x23585629fd7df3f5, 0x6e2c1436afd6b116, 0x0749d1b3258f9856, 0x074378368bdc7c27, 0xe065dcbc77e08f2e,
            0x514277b28ea7c87f, 0x74b31f36f1b73565, 0xb8b57f2e0e212ff1, 0x13578fe63c488f42, 0x7f50bc7dbab4cb41, 0x0a97125b82d934cc, 0x46771fc315312337, 0x69b509d564b844a2,
            0x0836782a09edb4d2, 0x8dcab74319d80b49, 0x34cb16e408a2993b, 0x5bfafc2dabb9852d, 0xa24cde29bdd6e4e8, 0x69833a7df572f780, 0xf8b36883d1fb1d8c, 0x7a4d7d643d7f1abf,
            0x9ed1f0ffdf49a585, 0x0b5ad27766ec8502, 0x9965f5cf2fff529e, 0x73d5af944d11db1b, 0xba1ea69f7590d436, 0x0c6ae447213ca18a, 0x6ce3d7467318c79c, 0x64b1cd76d557ecb2,
            0x8e88a61072604d0f, 0xf349be99621c1b69, 0x236e332a77d4aa81, 0x011a4f03b3c616b8, 0xf9562a6b215dd87a, 0x3cea52e04eba6ef8, 0x49474f553d69fae0, 0xb71f9d19ed4993ea,
            0xc23bdce4e51dbc40, 0xb6cdd9c794efd633, 0xddb5cac61cf93894, 0xaa8a6ccee770fb73, 0xaf8cb2b348fa81fd, 0x1fcfbe5872af9d97, 0x85c721ac5dd9c080, 0xbf6c4fc914803b17,
            0x33dcf4426c22a9a1, 0x0687a35124f41b0e, 0xd4ee8834c1aa4e08, 0xa7fb5625ece435a2, 0xdfc9bcd0b097e0e8, 0x04181d1b57fe568f, 0x91ce1e9a241ffe36, 0x1c30d69b99a13ad4,
            0x7d98147c13c5dd0e, 0x282f203b0a9b3111, 0xbfbcc21afc60ce88, 0x398bd5979a68759d, 0xb6a1b2ed0b287e6d, 0x41fe4db0d67c088b, 0x13f6a4401f8f1c65, 0xa41ec7114e6295ba,
            0x23ec10f10aeda257, 0x7b455c1eeb2a5eeb, 0xcc809d51181b2e96, 0x2cadfa5e387ecd22, 0x9acb036bc3bc7bba, 0x3a1cfa0ac756399c, 0x3c9b19fc74c6a7fe, 0xb59724435358131b,
            0x01f980fe7cc83839, 0x52db8729cd3921af, 0x46eb44c99bfdb13a, 0xcf824dd90fb3982a, 0xf13480f49f389e3e, 0xdc982580ee1e272e, 0x00f2e72f1778bda7, 0x62965fd82afb40e2,
            0x35597261e9342735, 0x65bc09e16e60d711, 0x9c97d0b44a4cc670, 0x042ab711ef2f7796, 0x8dcc14cc29b59484, 0xc2a11d9754e2bc3c, 0x3969c637fe95b6df, 0xd6bdbcdee7892dfd,
            0x7dafba8aba322ca7, 0xc676765bf7e8a6df, 0xe19d7dbd0859baba, 0xb9fee5375b45ccb0, 0x44daefa72a1cb73e, 0x0fd9401b72111b7d, 0x2b8095c5918a5df1, 0x3e8c8706a1e5727e,
            0x99d29f0bf11f5872, 0x39263e430e76eb40, 0x586c8ce409be94e1, 0xf68847b1b5a6818f, 0xfcd10c7e50ebfd26, 0x803e27d0e2e9390c, 0xccbf8491a232c2a7, 0x54cfec9b6b95a533,
            0xadc28600eafae490, 0xbdd8b8470504a937, 0x97518fc35ede2ad3, 0x52bc4f0bf49ba700, 0xcd26ccd93ccd5526, 0xbc640f39124ad994, 0xf0570e686665e54f, 0xd627e01291c48df6,
            0x7564fefb49faa4f7, 0x6cc49c23638459f6, 0x4a5802147e845ae6, 0x03d1da199d9cd3bc, 0x2d555f3bbab3b5a4, 0x8e1bea8f796bc483, 0x7dc745220149c9f2, 0x6b145188141428e8,
            0xfd8f3a9c8efc6f34, 0xde36d5e4b5378688, 0x933f830bae631bc5, 0x4be99bcc01c681d0, 0xdc7c74d7df45bef0, 0x7061864ecfe65f40, 0x57ad43490628e5aa, 0xf795124bd01bd0e7,
            0x5879d6ac0656089f, 0x93334a1ccce500e7, 0x13638bc5f3cf7ee2, 0xc5ab6e40212cf75e, 0x147ddc06cd43a201, 0xc57626c7d1ca88b1, 0x754e73627da577a1, 0x72c56ce24b945beb,
            0x834af1f29cee88c1, 0x376aa97203059110, 0x9cabcb2a0bcea230, 0xa3f0534453164db4, 0x1d41d30ce85d2ebe, 0x224a96b4ba4d7680, 0x7fe58ef36422aa72, 0x3df83e5413f34302,
            0x8743ce0bbc3fe899, 0x915f5f76ea124da4, 0x037763b9f2a31554, 0x97c23afcb9dacf29, 0x9b9ecd5577516232, 0xffa591aa4165d31c, 0xeffaef5da747e0e7, 0x3a46d11384dfd2c4,
            0x976255b07dcba443, 0x434b30b3c8ffea7a, 0x079f1c11defd8b2d, 0xd6a9b889ae1c0a70, 0xca3e98abb2c0409a, 0xfd7900bc5922fced, 0xf124639ccfd1d26b, 0x7e5ce17bd2019b90,
            0x04029e4b58d2d1b5, 0xe2295d61a85fd012, 0x081eafea16c37513, 0x363ada331ed8fcbc, 0x962465ec560b08d3, 0x80c848a65d14ad68, 0xe8da599afcd5006d, 0x61a6d1b75517d513,
            0x22d45ccb8be9e32a, 0x2289686b10b01f78, 0xb851936366ec178c, 0xf03f146455ca74c3, 0x2be7cadd1f62f2a7, 0x01d211fe2b138898, 0xe3ec3a6c9f7f8792, 0xfb68fcf8b8e2f20b
        };
        private readonly List<SG> _sg;

        /// <summary>
        /// The current seed.
        /// </summary>
        public long Seed
        {
            get { return _sg[_sg.Count - 1].Seed; }
            set { _sg[_sg.Count - 1].Seed = value; }
        }

        /// <summary>
        /// The current generation.
        /// </summary>
        public long Generation
        {
            get { return _sg[_sg.Count - 1].Generation; }
            set { _sg[_sg.Count - 1].Generation = value; }
        }

        // ReSharper disable once InconsistentNaming
        private class SG
        {
            public long Seed, Generation;

            public SG(long s, long g)
            {
                Seed = s;
                Generation = g;
            }
        }

        /// <summary>
        /// Creates a new RNG instance with the specified seed.
        /// </summary>
        /// <param name="seed">The seed for the generator.</param>
        public RNG(long seed)
        {
            _sg = new List<SG> { new SG(seed, 0) };
        }

        /// <summary>
        /// Creates a new RNG instance with the specified seed and generation.
        /// </summary>
        /// <param name="seed">The seed for the generator.</param>
        /// <param name="generation">The generation to start at.</param>
        public RNG(long seed, long generation)
        {
            _sg = new List<SG> {new SG(seed, generation)};
        }

        /// <summary>
        /// Creates a new RNG instance seeded with the system tick count.
        /// </summary>
        public RNG()
        {
            _sg = new List<SG> { new SG(Environment.TickCount, 0) };
        }

        /// <summary>
        /// Calculates the raw 64-bit value for a given seed/generation pair.
        /// </summary>
        /// <param name="s">The seed.</param>
        /// <param name="g">The generation.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetRaw(long s, long g)
        {
            unchecked
            {
                ulong
                    hash = 0,
                    ss = (ulong)s,
                    gg = (ulong)g;
                for (int i = 0; i < 8; i++)
                {
                    hash += 31 * Table[((ss ^ hash) >> (i * 8)) & 0xff].RotR(i) + 47 * Table[((gg ^ hash) >> (i * 8)) & 0xff].RotL(i) + 11;
                    hash *= 6364136223846793005;
                }
                return (long)hash;
            }
        }

        /// <summary>
        /// Calculates the raw 64-bit value for a given generation.
        /// </summary>
        /// <param name="g">The generation.</param>
        /// <returns></returns>
        public long this[int g]
        {
            get
            {
                return GetRaw(Seed, g);
            }
        }

        /// <summary>
        /// Calculates the raw 64-bit value for the next generation, and increases the current generation by 1.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long NextRaw()
        {
            return GetRaw(Seed, Generation++);
        }

        /// <summary>
        /// Calculates the raw 64-bit value for the previous generation, and decreases the current generation by 1.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long PrevRaw()
        {
            return GetRaw(Seed, --Generation);
        }

        /// <summary>
        /// Sets the current generation to zero.
        /// </summary>
        public void Reset()
        {
            Generation = 0;
        }

        /// <summary>
        /// Sets the seed to the specified value and the current generation to zero.
        /// </summary>
        /// <param name="newSeed">The new seed to apply to the generator.</param>
        public void Reset(long newSeed)
        {
            Generation = 0;
            Seed = newSeed;
        }

        /// <summary>
        /// Creates a new branch at the specified generation.
        /// </summary>
        /// <param name="generation">The generation to branch from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RNG Branch(long generation)
        {
            _sg.Add(new SG(GetRaw(Seed, generation), 0));
            return this;
        }

        /// <summary>
        /// The current branching depth of the generator.
        /// </summary>
        public int Depth
        {
            get { return _sg.Count; }
        }

        /// <summary>
        /// Removes the topmost branch and resumes generation on the next one down.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Merge()
        {
            if (_sg.Count > 1)
            {
                _sg.RemoveAt(_sg.Count - 1);
            }
            else
            {
                throw new InvalidOperationException("Cannot merge the base branch.");
            }
        }

        /// <summary>
        /// Calculates a 32-bit, non-negative integer for the current generation.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek()
        {
            return (int)GetRaw(Seed, Generation) & 0x7FFFFFFF;
        }

        /// <summary>
        /// Calculates the 32-bitnon-negative integer for the specified generation.
        /// </summary>
        /// <param name="generation">The generation to peek at.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekAt(long generation)
        {
            return (int)GetRaw(Seed, generation) & 0x7FFFFFFF;
        }

        /// <summary>
        /// Returns a double-precision floating point number between 0 and 1, and advances the generation by 1.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble()
        {
            return Math.Abs(NextRaw() / Double.MaxValue);
        }

        /// <summary>
        /// Calculates a 32-bit, non-negative integer from the next generation and increases the current generation by 1.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next()
        {
            return (int)NextRaw() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Calculates a 32-bit, non-negative integer from the previous generation and decreases the current generation by 1.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Prev()
        {
            return (int)PrevRaw() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Calculates a 32-bit integer between 0 and a specified upper bound for the current generation and increases the current generation by 1.
        /// </summary>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int max)
        {
            if (max == 0) return 0;
            return (int)(NextRaw() & 0x7FFFFFFF) % max;
        }

        /// <summary>
        /// Calculates a 32-bit integer between 0 and a specified upper bound from the previous generation and decreases the current generation by 1.
        /// </summary>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Prev(int max)
        {
            if (max == 0) return 0;
            return (int)(PrevRaw() & 0x7FFFFFFF) % max;
        }

        /// <summary>
        /// Calculates a 32-bit integer between 0 and a specified upper bound for the current generation.
        /// </summary>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek(int max)
        {
            if (max == 0) return 0;
            return ((int)GetRaw(Seed, Generation) & 0x7FFFFFFF) % max;
        }

        /// <summary>
        /// Calculates a 32-bit integer between 0 and a specified upper bound for the specified generation.
        /// </summary>
        /// <param name="generation">The generation whose value to calculate.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekAt(long generation, int max)
        {
            if (max == 0) return 0;
            return ((int)GetRaw(Seed, generation) & 0x7FFFFFFF) % max;
        }

        /// <summary>
        /// Calculates a 32-bit integer between the specified minimum and maximum values for the current generation, and increases the current generation by 1.
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int min, int max)
        {
            if (max == 0) return 0;
            if (min >= max)
            {
                throw new ArgumentException("Min must be less than max.");
            }

            return (((int)NextRaw() & 0x7FFFFFFF) - min) % (max - min) + min;
        }

        /// <summary>
        /// Calculates a 32-bit integer between the specified minimum and maximum values for the previous generation, and decreases the current generation by 1.
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Prev(int min, int max)
        {
            if (max == 0) return 0;
            if (min >= max)
            {
                throw new ArgumentException("Min must be less than max.");
            }

            return (((int)PrevRaw() & 0x7FFFFFFF) - min) % (max - min) + min;
        }

        /// <summary>
        /// Calculates a 32-bit integer between the specified minimum and maximum values for the current generation.
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Peek(int min, int max)
        {
            if (max == 0) return 0;
            if (min >= max)
            {
                throw new ArgumentException("Min must be less than max.");
            }

            return (((int)GetRaw(Seed, Generation) & 0x7FFFFFFF) - min) % (max - min) + min;
        }

        /// <summary>
        /// Calculates a 32-bit integer between the specified minimum and maximum values for the specified generation.
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <param name="generation">The generation whose value to calculate.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekAt(int generation, int min, int max)
        {
            if (max == 0) return 0;
            if (min >= max)
            {
                throw new ArgumentException("Min must be less than max.");
            }

            return (((int)GetRaw(Seed, generation) & 0x7FFFFFFF) - min) % (max - min) + min;
        }
    }
}