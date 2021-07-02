using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LfuArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue> (int capacity, int sets) {
            return new LfuArrayCacheImpl<TKey, TValue>(capacity, sets);
        }
    }

    class LfuArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LfuArrayCacheImpl(int sets, int ways) : base(sets, ways) {
        }

        override public void Add(TKey key, TValue value) {
            var offsetIndex = FindOffsetIndex(key);
            var itemIndex = offsetArray[offsetIndex];
            itemArray[itemIndex] = KeyValuePair.Create(key, value);
        }

    }
}
