using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public class LruArrayCache<TKey, TValue> : XruArrayCache<TKey, TValue> {
        public LruArrayCache(int sets, int ways) : base(sets, ways) {
        }

        protected override int ReplacementOffset {
            get => ways_ - 1;
        }
    }
}
