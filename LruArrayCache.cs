using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LruArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue>(int sets, int ways) {
            return new LruArrayCacheImpl<TKey, TValue>(sets, ways);
        }
    }

    class LruArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LruArrayCacheImpl(int sets, int ways) : base(sets, ways) {
        }

        override public void Add(TKey key, TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            var offsetIndex = setBegin;
            var itemIndex = int.MaxValue;
            int setOffset = 0;

            while (setOffset < Ways) {
                offsetIndex = setBegin + setOffset;
                itemIndex = offsetArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    offsetArray[offsetIndex] = itemIndex;
                    break;
                }

                if (itemArray[itemIndex].Key.Equals(key)) {
                    offsetIndex = setBegin + setOffset;
                    itemIndex = offsetArray[offsetIndex];
                    break;
                }

                ++setOffset;
            }

            RotateSetOffsets(set, setOffset);
            itemArray[itemIndex] = KeyValuePair.Create(key, value);
            return;
        }

    }
}
