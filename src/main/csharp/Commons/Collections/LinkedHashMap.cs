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
    public class LinkedHashMap<K, V> : HashMap<K, V>, Map<K, V> where K : class where V : class
    {
        #region Internal Implementation

        private readonly bool accessOrder;
        private LinkedHashMapEntry head;
        private LinkedHashMapEntry tail;

        private class AbstractMapIterator
        {
            protected int expectedModCount;
            protected LinkedHashMapEntry futureEntry;
            protected LinkedHashMapEntry currentEntry;
            protected readonly LinkedHashMap<K, V> associatedMap;

            public AbstractMapIterator(LinkedHashMap<K, V> parent)
            {
                this.expectedModCount = parent.modCount;
                this.futureEntry = parent.head;
                this.associatedMap = parent;
            }
    
            public bool HasNext
            {
                get { return (futureEntry != null); }
            }

            private void CheckConcurrentMod()
            {
                if (expectedModCount != associatedMap.modCount)
                {
                    throw new ConcurrentModificationException();
                }
            }

            public void MakeNext()
            {
                CheckConcurrentMod();
                if (!HasNext)
                {
                    throw new NoSuchElementException();
                }
                currentEntry = futureEntry;
                futureEntry = futureEntry.chainForward;
            }
    
            public void Remove()
            {
                CheckConcurrentMod();
                if (currentEntry==null)
                {
                    throw new IllegalStateException();
                }
                associatedMap.RemoveEntry(currentEntry);
                LinkedHashMapEntry lhme =  currentEntry;
                LinkedHashMapEntry p = lhme.chainBackward;
                LinkedHashMapEntry n = lhme.chainForward;
                LinkedHashMap<K, V> lhm = associatedMap;
                if (p != null)
                {
                    p.chainForward = n;
                    if (n != null)
                    {
                        n.chainBackward = p;
                    }
                    else
                    {
                        lhm.tail = p;
                    }
                }
                else
                {
                    lhm.head = n;
                    if (n != null)
                    {
                        n.chainBackward = null;
                    }
                    else
                    {
                        lhm.tail = null;
                    }
                }
                currentEntry = null;
                expectedModCount++;
            }
        }
    
        private class EntryIterator : AbstractMapIterator, Iterator<Entry<K, V>>
        {
            public EntryIterator(LinkedHashMap<K, V> parent) : base(parent)
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
            public KeyIterator (LinkedHashMap<K, V> parent) : base(parent)
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
            public ValueIterator (LinkedHashMap<K, V> parent) : base(parent)
            {
            }
    
            public V Next()
            {
                MakeNext();
                return currentEntry.Value;
            }
        }
    
        protected sealed class LinkedHashMapEntrySet : HashMapEntrySet
        {
            public LinkedHashMapEntrySet(LinkedHashMap<K, V> lhm) : base(lhm)
            {
            }

            public override Iterator<Entry<K, V>> Iterator()
            {
                return new EntryIterator((LinkedHashMap<K, V>) HashMap);
            }
        }

        protected sealed class LinkedHashMapEntry : HashMapEntry
        {
            internal LinkedHashMapEntry chainForward;
            internal LinkedHashMapEntry chainBackward;

            public LinkedHashMapEntry(K theKey, V theValue) : base(theKey, theValue)
            {
                chainForward = null;
                chainBackward = null;
            }

            public LinkedHashMapEntry(K theKey, int hash) : base(theKey, hash)
            {
                chainForward = null;
                chainBackward = null;
            }

            public override Object Clone()
            {
                LinkedHashMapEntry entry = (LinkedHashMapEntry) base.Clone();
                entry.chainBackward = chainBackward;
                entry.chainForward = chainForward;
                LinkedHashMapEntry lnext = (LinkedHashMapEntry) entry.next;
                if (lnext != null)
                {
                    entry.next = (LinkedHashMapEntry) lnext.Clone();
                }
                return entry;
            }
        }

        protected override HashMapEntry[] NewElementArray(int s)
        {
            return new LinkedHashMapEntry[s];
        }

        protected override Entry<K, V> CreateEntry(K key, int index, V value)
        {
            LinkedHashMapEntry m = new LinkedHashMapEntry(key, value);
            m.next = elementData[index];
            elementData[index] = m;
            LinkEntry(m);
            return m;
        }

        protected override Entry<K, V> CreateHashedEntry(K key, int index, int hash)
        {
            LinkedHashMapEntry m = new LinkedHashMapEntry(key, hash);
            m.next = elementData[index];
            elementData[index] = m;
            LinkEntry(m);
            return m;
        }

        protected void LinkEntry(LinkedHashMapEntry entry)
        {
            if (ReferenceEquals(tail, entry))
            {
                return;
            }
    
            if (head == null)
            {
                // Check if the map is empty
                head = tail = entry;
                return;
            }
    
            // we need to link the new entry into either the head or tail
            // of the chain depending on if the LinkedHashMap is accessOrder or not
            LinkedHashMapEntry p = entry.chainBackward;
            LinkedHashMapEntry n = entry.chainForward;

            if (p == null)
            {
                if (n != null)
                {
                    // The entry must be the head but not the tail
                    if (accessOrder)
                    {
                        head = n;
                        n.chainBackward = null;
                        entry.chainBackward = tail;
                        entry.chainForward = null;
                        tail.chainForward = entry;
                        tail = entry;
                    }
                }
                else
                {
                    // This is a new entry
                    entry.chainBackward = tail;
                    entry.chainForward = null;
                    tail.chainForward = entry;
                    tail = entry;
                }
                return;
            }
    
            if (n == null)
            {
                // The entry must be the tail so we can't get here
                return;
            }
    
            // The entry is neither the head nor tail
            if (accessOrder)
            {
                p.chainForward = n;
                n.chainBackward = p;
                entry.chainForward = null;
                entry.chainBackward = tail;
                tail.chainForward = entry;
                tail = entry;
            }
        }

        private sealed class KeySetCollection : AbstractSet<K>
        {
            private readonly LinkedHashMap<K, V> parent;

            public KeySetCollection(LinkedHashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override bool Contains(K obj)
            {
                return parent.ContainsKey(obj);
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
                if (parent.ContainsKey(key))
                {
                    parent.Remove(key);
                    return true;
                }
                return false;
            }

            public override Iterator<K> Iterator()
            {
                return new KeyIterator(parent);
            }
        };

        private class ValuesCollection : AbstractCollection<V>
        {
            private readonly LinkedHashMap<K, V> parent;

            public ValuesCollection(LinkedHashMap<K, V> parent) : base()
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

        #endregion

        /// <summary>
        /// Create a new empty LinkedHashMap instance.
        /// </summary>
        public LinkedHashMap() : base()
        {
            this.accessOrder = false;
            this.head = null;
        }

        /// <summary>
        /// Creates a new LinkedHashMap with the given initial capacity and default load factor.
        /// </summary>
        public LinkedHashMap(int capacity) : base(capacity)
        {
            this.accessOrder = false;
            this.head = null;
        }

        /// <summary>
        /// Creates a new LinkedHashMap with the given initial capacity and load factor.
        /// </summary>
        public LinkedHashMap(int capacity, float loadFactor) : base(capacity, loadFactor)
        {
            this.accessOrder = false;
            this.head = null;
            this.tail = null;
        }

        /// <summary>
        /// Creates a new LinkedHashMap with the given initial capacity, load factor and iteration order.
        /// If order is true then the map is ordered based on the last access, and if false the map is
        /// ordered based on the order that entries are inserted.
        /// </summary>
        public LinkedHashMap(int capacity, float loadFactor, bool order) : base(capacity, loadFactor)
        {
            this.accessOrder = order;
            this.head = null;
            this.tail = null;
        }

        /// <summary>
        /// Creates a new LinkedHashMap instance that contians all the mappings of the given map.
        /// </summary>
        public LinkedHashMap(Map<K, V> otherMap) : base()
        {
            this.accessOrder = false;
            this.head = null;
            this.tail = null;

            PutAll(otherMap);
        }

        public override bool ContainsValue(V val)
        {
            LinkedHashMapEntry entry = head;
            if (null == val)
            {
                while (null != entry)
                {
                    if (null == entry.Value)
                    {
                        return true;
                    }
                    entry = entry.chainForward;
                }
            }
            else
            {
                while (null != entry)
                {
                    if (val.Equals(entry.Value))
                    {
                        return true;
                    }
                    entry = entry.chainForward;
                }
            }
            return false;
        }

        public override V Get(K key)
        {
            LinkedHashMapEntry m;
            if (key == null)
            {
                m = (LinkedHashMapEntry) FindNullKeyEntry();
            }
            else
            {
                int hash = key.GetHashCode();
                int index = (hash & 0x7FFFFFFF) % elementData.Length;
                m = (LinkedHashMapEntry) FindNonNullKeyEntry(key, index, hash);
            }

            if (m == null)
            {
                return null;
            }

            if (accessOrder && tail != m)
            {
                LinkedHashMapEntry p = m.chainBackward;
                LinkedHashMapEntry n = m.chainForward;
                n.chainBackward = p;
                if (p != null)
                {
                    p.chainForward = n;
                }
                else
                {
                    head = n;
                }
                m.chainForward = null;
                m.chainBackward = tail;
                tail.chainForward = m;
                tail = m;
            }

            return m.Value;
        }

        public override V Put(K key, V val)
        {
            V result = PutImpl(key, val);
    
            if (RemoveEldestEntry(head))
            {
                Remove(head.Key);
            }
    
            return result;
        }

        protected override V PutImpl(K key, V val)
        {
            LinkedHashMapEntry entry;
            if (elementCount == 0)
            {
                head = tail = null;
            }

            if (key == null)
            {
                entry = (LinkedHashMapEntry) FindNullKeyEntry();
                if (entry == null)
                {
                    modCount++;
                    // Check if we need to remove the oldest entry. The check
                    // includes accessOrder since an accessOrder LinkedHashMap does
                    // not record the oldest member in 'head'.
                    if (++elementCount > threshold)
                    {
                        Rehash();
                    }
                    entry = (LinkedHashMapEntry) CreateHashedEntry(null, 0, 0);
                }
                else
                {
                    LinkEntry(entry);
                }
            }
            else
            {
                int hash = key.GetHashCode();
                int index = (hash & 0x7FFFFFFF) % elementData.Length;
                entry = (LinkedHashMapEntry) FindNonNullKeyEntry(key, index, hash);
                if (entry == null)
                {
                    modCount++;
                    if (++elementCount > threshold)
                    {
                        Rehash();
                        index = (hash & 0x7FFFFFFF) % elementData.Length;
                    }
                    entry = (LinkedHashMapEntry) CreateHashedEntry(key, index, hash);
                }
                else
                {
                    LinkEntry(entry);
                }
            }

            V result = entry.Value;
            entry.Value = val;
            return result;
        }

        public override Set<Entry<K, V>> EntrySet()
        {
            return new LinkedHashMapEntrySet(this);
        }

        public override Set<K> KeySet()
        {
            if (keySet == null)
            {
                keySet = new KeySetCollection(this);
            }
            return keySet;
        }

        public override Collection<V> Values()
        {
            if (valuesCollection == null)
            {
                valuesCollection = new ValuesCollection(this);
            }
            return valuesCollection;
        }

        public override V Remove(K key)
        {
            LinkedHashMapEntry entry = (LinkedHashMapEntry) RemoveEntry(key);
            if (entry == null)
            {
                return null;
            }

            LinkedHashMapEntry p = entry.chainBackward;
            LinkedHashMapEntry n = entry.chainForward;

            if (p != null)
            {
                p.chainForward = n;
            }
            else
            {
                head = n;
            }

            if (n != null)
            {
                n.chainBackward = p;
            }
            else
            {
                tail = p;
            }

            return entry.Value;
        }

        /// <summary>
        /// This method is queried from the put and putAll methods to check if the eldest member
        /// of the map should be deleted before adding the new member.  If this map was created
        /// with accessOrder = true, then the result of RemoveEldestEntry is assumed to be false.
        /// </summary>
        protected virtual bool RemoveEldestEntry(Entry<K, V> eldest)
        {
            return false;
        }

        public override void Clear()
        {
            base.Clear();
            head = tail = null;
        }
    }
}

