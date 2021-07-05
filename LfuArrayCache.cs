﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public class LfuArrayCache<TKey, TValue> : XfuArrayCache<TKey, TValue> {
        public LfuArrayCache(int sets, int ways) : base(sets, ways) {
            Clear();
        }

        protected override int ReplacementOffset {
            get => ways_ - 1;
        }
    }
}
