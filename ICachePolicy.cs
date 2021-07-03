using System;
using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {

    public interface ICachePolicy {
        internal ICachePolicyImpl<TKey, TValue> MakeInstance<TKey, TValue>(int sets, int ways);
    }

    public interface ICachePolicyImpl<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {
    }
}
