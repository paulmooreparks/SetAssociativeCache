using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public class MruArrayCache<TKey, TValue> : XruArrayCache<TKey, TValue> {
        public MruArrayCache(int sets, int ways) : base(sets, ways) {
        }

        protected override int ReplacementOffset {
            get => 0;
        }
    }
}
