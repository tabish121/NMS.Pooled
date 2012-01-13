/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Apache.NMS.Pooled.Commons.Collections
{
    public class HashMap<K, V> : AbstractMap<K, V>, Map<K, V> where K : class where V : class
    {
        #region HashMap private implementation data and classes

        // Actual count of entries
        protected int elementCount;

        // The internal data structure to hold Entries
        protected HashMapEntry[] elementData;

        // modification count, to keep track of structural modifications between the
        // HashMap and the iterator
        protected int modCount = 0;

        // default size that an HashMap created using the default constructor would have.
        private static readonly int DEFAULT_SIZE = 16;

        // maximum ratio of (stored elements)/(storage size) which does not lead to rehash
        protected readonly float loadFactor;

        // maximum number of elements that can be put in this map before having to rehash
        protected int threshold;

        private class KeySetCollection : AbstractSet<K>
        {
            private readonly HashMap<K, V> parent;

            public KeySetCollection(HashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override bool Contains(K key)
            {
                return parent.ContainsKey(key);
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override void Clear()
            {
                parent.Clear();
            }

            public override bool Remove(K key)
            {
                Entry<K, V> entry = parent.RemoveEntry(key);
                return entry != null;
            }

            public override Iterator<K> Iterator()
            {
                return new KeyIterator(parent);
            }
        }

        private class ValuesCollection : AbstractCollection<V>
        {
            private readonly HashMap<K, V> parent;

            public ValuesCollection(HashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override bool Contains(V val)
            {
                return parent.ContainsValue(val);
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override void Clear()
            {
                parent.Clear();
            }

            public override Iterator<V> Iterator()
            {
                return new ValueIterator(parent);
            }
        }

        // An Entry class use to track data in the Hash
        protected class HashMapEntry : MapEntry<K, V>, ICloneable
        {
            public int origKeyHash;
            public HashMapEntry next;

            public HashMapEntry(K theKey, int hash) : base(theKey, null)
            {
                this.origKeyHash = hash;
            }

            public HashMapEntry(K theKey, V theValue) : base(theKey, theValue)
            {
                origKeyHash = (theKey == null ? 0 : ComputeHashCode(theKey));
            }

            public override Object Clone()
            {
                HashMapEntry entry = (HashMapEntry) base.Clone();
                if (next != null)
                {
                    entry.next = next.Clone() as HashMapEntry;
                }
                return entry;
            }
        }

        private class AbstractMapIterator
        {
            private int position = 0;
            protected int expectedModCount;
            protected HashMapEntry futureEntry;
            protected HashMapEntry currentEntry;
            protected HashMapEntry prevEntry;
    
            protected readonly HashMap<K, V> associatedMap;

            public AbstractMapIterator(HashMap<K, V> parent)
            {
                associatedMap = parent;
                expectedModCount = parent.modCount;
                futureEntry = null;
            }

            public virtual bool HasNext
            {
                get
                {
                    if (futureEntry != null)
                    {
                        return true;
                    }

                    while (position < associatedMap.elementData.Length)
                    {
                        if (associatedMap.elementData[position] == null)
                        {
                            position++;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            void CheckConcurrentMod()
            {
                if (expectedModCount != associatedMap.modCount)
                {
                    throw new ConcurrentModificationException();
                }
            }
    
            protected void MakeNext()
            {
                CheckConcurrentMod();
                if (!HasNext)
                {
                    throw new NoSuchElementException();
                }

                if (futureEntry == null)
                {
                    currentEntry = associatedMap.elementData[position++];
                    futureEntry = currentEntry.next;
                    prevEntry = null;
                }
                else
                {
                    if (currentEntry!=null)
                    {
                        prevEntry = currentEntry;
                    }
                    currentEntry = futureEntry;
                    futureEntry = futureEntry.next;
                }
            }
    
            public void Remove()
            {
                CheckConcurrentMod();
                if (currentEntry==null)
                {
                    throw new IllegalStateException();
                }

                if(prevEntry==null)
                {
                    int index = currentEntry.origKeyHash & (associatedMap.elementData.Length - 1);
                    associatedMap.elementData[index] = associatedMap.elementData[index].next;
                }
                else
                {
                    prevEntry.next = currentEntry.next;
                }
                currentEntry = null;
                expectedModCount++;
                associatedMap.modCount++;
                associatedMap.elementCount--;
            }
        }

        private class EntryIterator : AbstractMapIterator, Iterator<Entry<K, V>>
        {
            public EntryIterator(HashMap<K, V> parent) : base(parent)
            {
            }
    
            public Entry<K, V> Next()
            {
                MakeNext();
                return currentEntry;
            }
        }

        private class KeyIterator : AbstractMapIterator, Iterator<K>
        {
            public KeyIterator(HashMap<K, V> parent) : base(parent)
            {
            }
    
            public K Next()
            {
                MakeNext();
                return currentEntry.Key;
            }
        }

        private class ValueIterator : AbstractMapIterator, Iterator<V>
        {
            public ValueIterator(HashMap<K, V> parent) : base(parent)
            {
            }

            public V Next()
            {
                MakeNext();
                return currentEntry.Value;
            }
        }

        protected class HashMapEntrySet : AbstractSet<Entry<K, V>>
        {
            private readonly HashMap<K, V> associatedMap;
    
            public HashMapEntrySet(HashMap<K, V> parent) : base()
            {
                associatedMap = parent;
            }
    
            internal HashMap<K, V> HashMap
            {
                get { return associatedMap; }
            }
    
            public override int Size()
            {
                return associatedMap.elementCount;
            }
    
            public override void Clear()
            {
                associatedMap.Clear();
            }
    
            public override bool Remove(Entry<K, V> target)
            {
                Entry<K, V> entry = associatedMap.GetEntry(target.Key);
                if (ValuesEq(entry, target))
                {
                    associatedMap.RemoveEntry(entry);
                    return true;
                }
                return false;
            }
    
            public override bool Contains(Entry<K, V> target)
            {
                Entry<K, V> entry = associatedMap.GetEntry(target.Key);
                return ValuesEq(entry, target);
            }

            private static bool ValuesEq(Entry<K, V> entry, Entry<K, V> target)
            {
                return (entry != null) &&
                       ((entry.Value == null) ? (target.Value == null) :
                        (AreEqualValues(entry.Value, target.Value)));
            }
    
            public override Iterator<Entry<K, V>> Iterator()
            {
                return new EntryIterator(associatedMap);
            }
        }

        protected virtual HashMapEntry[] NewElementArray(int s)
        {
            return new HashMapEntry[s];
        }

        /// <summary>
        /// Calculates the capacity of storage required for storing given number of elements
        /// </summary>
        private static int CalculateCapacity(int x)
        {
            if (x >= 1 << 30)
            {
                return 1 << 30;
            }

            if (x == 0)
            {
                return 16;
            }

            x = x -1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;

            return x + 1;
        }

        /// <summary>
        /// Computes the threshold for rehashing the Map.
        /// </summary>
        private void ComputeThreshold()
        {
            threshold = (int) (elementData.Length * loadFactor);
        }

        private Entry<K, V> GetEntry(K key)
        {
            Entry<K, V> m;
            if (key == null)
            {
                m = FindNullKeyEntry();
            }
            else
            {
                int hash = ComputeHashCode(key);
                int index = hash & (elementData.Length - 1);
                m = FindNonNullKeyEntry(key, index, hash);
            }
            return m;
        }

        protected virtual Entry<K,V> FindNonNullKeyEntry(K key, int index, int keyHash)
        {
            HashMapEntry m = elementData[index];
            while (m != null && (m.origKeyHash != keyHash || !AreEqualKeys(key, m.Key)))
            {
                m = m.next;
            }
            return m;
        }
    
        protected virtual Entry<K,V> FindNullKeyEntry()
        {
            HashMapEntry m = elementData[0];
            while (m != null && m.Key != null)
            {
                m = m.next;
            }
            return m;
        }

        protected virtual Entry<K, V> CreateEntry(K key, int index, V value)
        {
            HashMapEntry entry = new HashMapEntry(key, value);
            entry.next = elementData[index];
            elementData[index] = entry;
            return entry;
        }
    
        protected virtual Entry<K,V> CreateHashedEntry(K key, int index, int hash)
        {
            HashMapEntry entry = new HashMapEntry(key,hash);
            entry.next = elementData[index];
            elementData[index] = entry;
            return entry;
        }

        protected static int ComputeHashCode(K key)
        {
            return key.GetHashCode();
        }
    
        protected static bool AreEqualKeys(K key1, K key2)
        {
            return ReferenceEquals(key1, key2) || key1.Equals(key2);
        }

        protected static bool AreEqualValues(V value1, V value2)
        {
            return ReferenceEquals(value1, value2) || value1.Equals(value2);
        }

        protected void Rehash(int capacity)
        {
            int length = CalculateCapacity((capacity == 0 ? 1 : capacity << 1));

            HashMapEntry[] newData = NewElementArray(length);
            for (int i = 0; i < elementData.Length; i++)
            {
                HashMapEntry entry = elementData[i];
                elementData[i] = null;
                while (entry != null)
                {
                    int index = entry.origKeyHash & (length - 1);
                    HashMapEntry next = entry.next;
                    entry.next = newData[index];
                    newData[index] = entry;
                    entry = next;
                }
            }
            elementData = newData;
            ComputeThreshold();
        }
    
        protected void Rehash()
        {
            Rehash(elementData.Length);
        }

        protected void RemoveEntry(Entry<K, V> entry)
        {
            HashMapEntry ientry = entry as HashMapEntry;
            int index = ientry.origKeyHash & (elementData.Length - 1);
            HashMapEntry m = elementData[index];
            if (ReferenceEquals(m, ientry))
            {
                elementData[index] = ientry.next;
            }
            else
            {
                while (m.next != ientry)
                {
                    m = m.next;
                }
                m.next = ientry.next;

            }
            modCount++;
            elementCount--;
        }
    
        protected Entry<K, V> RemoveEntry(K key)
        {
            int index = 0;
            HashMapEntry entry;
            HashMapEntry last = null;
            if (key != null)
            {
                int hash = ComputeHashCode(key);
                index = hash & (elementData.Length - 1);
                entry = elementData[index];
                while (entry != null && !(entry.origKeyHash == hash && AreEqualKeys(key, entry.Key)))
                {
                    last = entry;
                    entry = entry.next;
                }
            }
            else
            {
                entry = elementData[0];
                while (entry != null && entry.Key != null)
                {
                    last = entry;
                    entry = entry.next;
                }
            }

            if (entry == null)
            {
                return null;
            }

            if (last == null)
            {
                elementData[index] = entry.next;
            }
            else
            {
                last.next = entry.next;
            }
            modCount++;
            elementCount--;
            return entry;
        }

        #endregion

        /// <summary>
        /// Creates a new HashMap instance with the default capacity and a load factor of 0.75.
        /// </summary>
        public HashMap() : this(DEFAULT_SIZE)
        {
        }

        /// <summary>
        /// Creates a new HashMap instance with the given capacity and a load factor of 0.75.
        /// </summary>
        public HashMap(int capacity) : this(capacity, 0.75f)
        {
        }

        /// <summary>
        /// Creates a new HashMap instance with the given capacity and load factor.
        /// </summary>
        public HashMap(int capacity, float loadFactor) : base()
        {
            if (capacity >= 0 && loadFactor > 0)
            {
                capacity = CalculateCapacity(capacity);
                elementCount = 0;
                elementData = NewElementArray(capacity);
                this.loadFactor = loadFactor;
                ComputeThreshold();
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Constructs a new HashMap instance containing the mappings from the specified map.
        /// </summary>
        public HashMap(Map<K, V> map) : this(CalculateCapacity(map.Size()))
        {
            PutAllImpl(map);
        }

        /// <summary>
        /// Removes all mappings from this hash map, leaving it empty.
        /// </summary>
        public override void Clear()
        {
            if (elementCount > 0)
            {
                elementCount = 0;
                Arrays.Fill(elementData, null);
                modCount++;
            }
        }

        /// <summary>
        /// Returns whether this map contains the specified key.
        /// </summary>
        public override bool ContainsKey(K key)
        {
            Entry<K, V> m = GetEntry(key);
            return m != null;
        }

        /// <summary>
        /// Returns whether this map contains the specified value.
        /// </summary>
        public override bool ContainsValue(V val)
        {
            if (val != null)
            {
                for (int i = 0; i < elementData.Length; i++)
                {
                    HashMapEntry entry = elementData[i];
                    while (entry != null)
                    {
                        if (AreEqualValues(val, entry.Value))
                        {
                            return true;
                        }
                        entry = entry.next;
                    }
                }
            }
            else
            {
                for (int i = 0; i < elementData.Length; i++)
                {
                    HashMapEntry entry = elementData[i];
                    while (entry != null)
                    {
                        if (entry.Value == null)
                        {
                            return true;
                        }
                        entry = entry.next;
                    }
                }
            }
            return false;
        }

        public override Set<Entry<K, V>> EntrySet()
        {
            return new HashMapEntrySet(this);
        }

        public override V Get(K key)
        {
            Entry<K, V> m = GetEntry(key);
            if (m != null)
            {
                return m.Value;
            }
            return null;
        }

        public override bool IsEmpty()
        {
            return elementCount == 0;
        }

        public override Set<K> KeySet()
        {
            if (keySet == null)
            {
                keySet = new KeySetCollection(this);
            }
            return keySet;
        }

        public override V Put(K key, V val)
        {
            return PutImpl(key, val);
        }

        protected virtual V PutImpl(K key, V val)
        {
            Entry<K, V> entry;
            if(key == null)
            {
                entry = FindNullKeyEntry();
                if (entry == null)
                {
                    modCount++;
                    entry = CreateHashedEntry(null, 0, 0);
                    if (++elementCount > threshold)
                    {
                        Rehash();
                    }
                }
            }
            else
            {
                int hash = ComputeHashCode(key);
                int index = hash & (elementData.Length - 1);
                entry = FindNonNullKeyEntry(key, index, hash);
                if (entry == null)
                {
                    modCount++;
                    entry = CreateHashedEntry(key, index, hash);
                    if (++elementCount > threshold)
                    {
                        Rehash();
                    }
                }
            }
    
            V result = entry.Value;
            entry.Value = val;
            return result;
        }

        public override void PutAll(Map<K, V> map)
        {
            if (!map.IsEmpty())
            {
                PutAllImpl(map);
            }
        }

        protected virtual void PutAllImpl(Map<K, V> map)
        {
            int capacity = elementCount + map.Size();
            if (capacity > threshold)
            {
                Rehash(capacity);
            }

            Iterator<Entry<K, V>> iter = map.EntrySet().Iterator();
            while (iter.HasNext)
            {
                Entry<K, V> entry = iter.Next();
                PutImpl(entry.Key, entry.Value);
            }
        }

        public override V Remove(K key)
        {
            Entry<K, V> entry = RemoveEntry(key);
            if (entry != null)
            {
                return entry.Value;
            }
            return null;
        }

        public override int Size()
        {
            return elementCount;
        }

        public override Collection<V> Values()
        {
            if (valuesCollection == null)
            {
                valuesCollection = new ValuesCollection(this);
            }
            return valuesCollection;
        }

        public override Object Clone()
        {
            try
            {
                HashMap<K, V> map = (HashMap<K, V>) base.Clone();
                map.elementCount = 0;
                map.elementData = NewElementArray(elementData.Length);
                map.PutAll(this);
                return map;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

    }
}

