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

using Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    /// <summary>
    /// A hash table supporting full concurrency of retrievals and adjustable
    /// expected concurrency for updates. Even though all operations are thread-safe,
    /// retrieval operations do not entail locking, and there is not any support
    /// for locking the entire table in a way that prevents all access.
    /// </summary>
    public class ConcurrentHashMap<K, V> : AbstractMap<K, V>, ConcurrentMap<K, V> where K : class where V : class
    {
        /*
         * The basic strategy is to subdivide the table among Segments,
         * each of which itself is a concurrently readable hash table.
         */

        #region Constants used throughout this class

        /// <summary>
        /// The default initial capacity for this table, used when not otherwise
        /// specified in a constructor.
        /// </summary>
        static readonly int DEFAULT_INITIAL_CAPACITY = 16;
    
        /// <summary>
        /// The default load factor for this table, used when not otherwise specified
        /// in a constructor.
        /// </summary>
        static readonly float DEFAULT_LOAD_FACTOR = 0.75f;

        /// <summary>
        /// The default concurrency level for this table, used when not otherwise specified
        /// in a constructor.
        /// </summary>
        static readonly int DEFAULT_CONCURRENCY_LEVEL = 16;

        /// <summary>
        /// The maximum capacity, used if a higher value is implicitly specified by either
        /// of the constructors with arguments.  MUST be a power of two <= 1<<30 to ensure
        /// that entries are indexable using ints.
        /// </summary>
        static readonly int MAXIMUM_CAPACITY = 1 << 30;

        /// <summary>
        /// The maximum number of segments to allow; used to bound constructor arguments.
        /// </summary>
        static readonly int MAX_SEGMENTS = 1 << 16; // slightly conservative

        /// <summary>
        /// Number of unsynchronized retries in size and containsValue methods before
        /// resorting to locking. This is used to avoid unbounded retries if tables undergo
        /// continuous modification which would make it impossible to obtain an accurate
        /// result.
        /// </summary>
        static readonly int RETRIES_BEFORE_LOCK = 2;

        #endregion

        #region Member data for this class

        /// <summary>
        /// Mask value for indexing into segments. The upper bits of a key's hash
        /// code are used to choose the segment.
        /// </summary>
        private readonly int segmentMask;

        /// <summary>
        /// Shift value for indexing within segments.
        /// </summary>
        private readonly int segmentShift;

        /// <summary>
        /// The segments, each of which is a specialized hash table
        /// </summary>
        private readonly Segment[] segments;
    
        private Set<Entry<K,V>> entrySet;
        private Collection<V> values;

        #endregion

        #region Utility Functions

        /// <summary>
        /// Applies a supplemental hash function to a given hashCode, which defends against
        /// poor quality hash functions.  This is critical because ConcurrentHashMap uses
        /// power-of-two length hash tables, that otherwise encounter collisions for
        /// hashCodes that do not differ in lower or upper bits.
        /// </summary>
        private static int Hash(int h)
        {
            uint uh = (uint)h;

            // Spread bits to regularize both segment and index locations,
            // using variant of single-word Wang/Jenkins hash.
            uh += (uh <<  15) ^ 0xffffcd7d;
            uh ^= (uh >> 10);
            uh += (uh <<   3);
            uh ^= (uh >>  6);
            uh += (uh <<   2) + (uh << 14);
            return (int)(uh ^ ((uint)uh >> 16));
        }
    
        /// <summary>
        /// Returns the segment that should be used for key with given hash
        /// </summary>
        private Segment SegmentFor(int hash)
        {
            return segments[((uint)hash >> segmentShift) & segmentMask];
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// ConcurrentHashMap list entry. Note that this is never exported out as a
        /// user-visible Map.Entry.
        /// <para>
        /// Because the value field is volatile, not readonly, it is legal wrt the memory
        /// model for an unsynchronized reader to see null instead of initial value when
        /// read via a data race.  Although a reordering leading to this is not likely to
        /// ever actually occur, the Segment.ReadValueUnderLock method is used as a backup
        /// in case a null (pre-initialized) value is ever seen in an unsynchronized access
        /// method.
        /// </para>
        /// </summary>
        private sealed class HashEntry
        {
            public readonly K key;
            public readonly int hash;
            public V value;
            public readonly HashEntry next;
    
            public HashEntry(K key, int hash, HashEntry next, V value)
            {
                this.key = key;
                this.hash = hash;
                this.next = next;
                this.value = value;
            }
    
            public static HashEntry[] NewArray(int i)
            {
                return new HashEntry[i];
            }
        }
    
        /// <summary>
        /// Segments are specialized versions of hash tables.  This subclasses from
        /// ReentrantLock opportunistically, just to simplify some locking and avoid
        /// separate construction.
        /// </summary>
        private class Segment : ReentrantLock
        {
            /*
             * Segments maintain a table of entry lists that are ALWAYS
             * kept in a consistent state, so can be read without locking.
             * Next fields of nodes are immutable (final).  All list
             * additions are performed at the front of each bin. This
             * makes it easy to check changes, and also fast to traverse.
             * When nodes would otherwise be changed, new nodes are
             * created to replace them. This works well for hash tables
             * since the bin lists tend to be short. (The average length
             * is less than two for the default load factor threshold.)
             *
             * Read operations can thus proceed without locking, but rely
             * on selected uses of volatiles to ensure that completed
             * write operations performed by other threads are
             * noticed. For most purposes, the "count" field, tracking the
             * number of elements, serves as that volatile variable
             * ensuring visibility.  This is convenient because this field
             * needs to be read in many read operations anyway:
             *
             *   - All (unsynchronized) read operations must first read the
             *     "count" field, and should not look at table entries if
             *     it is 0.
             *
             *   - All (synchronized) write operations should write to
             *     the "count" field after structurally changing any bin.
             *     The operations must not take any action that could even
             *     momentarily cause a concurrent read operation to see
             *     inconsistent data. This is made easier by the nature of
             *     the read operations in Map. For example, no operation
             *     can reveal that the table has grown but the threshold
             *     has not yet been updated, so there are no atomicity
             *     requirements for this with respect to reads.
             *
             * As a guide, all critical volatile reads and writes to the
             * count field are marked in code comments.
             */
    
            /// <summary>
            /// The number of elements in this segment's region.
            /// </summary>
            public volatile int count;
    
            /// <summary>
            /// Number of updates that alter the size of the table. This is used during
            /// bulk-read methods to make sure they see a consistent snapshot: If modCounts
            /// change during a traversal of segments computing size or checking containsValue,
            /// then we might have an inconsistent view of state so (usually) must retry.
            /// </summary>
            public int modCount;
    
            /// <summary>
            /// The table is rehashed when its size exceeds this threshold. (The value of
            /// this field is always (int)(capacity * loadFactor).
            /// </summary>
            public int threshold;
    
            /// <summary>
            /// The per-segment table.
            /// </summary>
            public volatile HashEntry[] table;
    
            /// <summary>
            /// The load factor for the hash table.  Even though this value is same for all
            /// segments, it is replicated to avoid needing links to outer object.
            /// </summary>
            public readonly float loadFactor;

            public Segment(int initialCapacity, float lf)
            {
                loadFactor = lf;
                SetTable(HashEntry.NewArray(initialCapacity));
            }
    
            public static Segment[] NewArray(int i)
            {
                return new Segment[i];
            }
    
            /// <summary>
            /// Sets table to new HashEntry array.  Call only while holding lock or in constructor.
            /// </summary>
            public void SetTable(HashEntry[] newTable)
            {
                threshold = (int)(newTable.Length * loadFactor);
                table = newTable;
            }
    
            /// <summary>
            /// Returns properly casted first entry of bin for given hash.
            /// </summary>
            public HashEntry GetFirst(int hash)
            {
                HashEntry[] tab = table;
                return tab[hash & (tab.Length - 1)];
            }

            /// <summary>
            /// Reads value field of an entry under lock. Called if value field ever appears
            /// to be null. This is possible only if a compiler happens to reorder a HashEntry
            /// initialization with its table assignment, which is legal under memory model
            /// but is not known to ever occur.
            /// </summary>
            public V ReadValueUnderLock(HashEntry e)
            {
                Lock();
                try
                {
                    return e.value;
                }
                finally
                {
                    UnLock();
                }
            }

            public V Get(Object key, int hash)
            {
                if (count != 0)
                {
                    // read-volatile
                    HashEntry e = GetFirst(hash);
                    while (e != null)
                    {
                        if (e.hash == hash && key.Equals(e.key))
                        {
                            V v = e.value;
                            if (v != null)
                            {
                                return v;
                            }
                            return ReadValueUnderLock(e); // recheck
                        }
                        e = e.next;
                    }
                }

                return null;
            }
    
            public bool ContainsKey(Object key, int hash)
            {
                if (count != 0)
                {
                    // read-volatile
                    HashEntry e = GetFirst(hash);
                    while (e != null)
                    {
                        if (e.hash == hash && key.Equals(e.key))
                        {
                            return true;
                        }
                        e = e.next;
                    }
                }

                return false;
            }
    
            public bool ContainsValue(Object value)
            {
                if (count != 0)
                {
                    // read-volatile
                    HashEntry[] tab = table;
                    int len = tab.Length;
                    for (int i = 0 ; i < len; i++)
                    {
                        for (HashEntry e = tab[i]; e != null; e = e.next)
                        {
                            V v = e.value;
                            if (v == null)
                            {
                                // recheck
                                v = ReadValueUnderLock(e);
                            }
                            if (value.Equals(v))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
    
            public bool Replace(K key, int hash, V oldValue, V newValue)
            {
                Lock();
                try
                {
                    HashEntry e = GetFirst(hash);
                    while (e != null && (e.hash != hash || !key.Equals(e.key)))
                    {
                        e = e.next;
                    }
    
                    bool replaced = false;
                    if (e != null && oldValue.Equals(e.value))
                    {
                        replaced = true;
                        e.value = newValue;
                    }
                    return replaced;
                }
                finally
                {
                    UnLock();
                }
            }
    
            public V Replace(K key, int hash, V newValue)
            {
                Lock();
                try
                {
                    HashEntry e = GetFirst(hash);
                    while (e != null && (e.hash != hash || !key.Equals(e.key)))
                    {
                        e = e.next;
                    }
    
                    V oldValue = null;
                    if (e != null)
                    {
                        oldValue = e.value;
                        e.value = newValue;
                    }
                    return oldValue;
                }
                finally
                {
                    UnLock();
                }
            }

            public V Put(K key, int hash, V value, bool onlyIfAbsent)
            {
                Lock();
                try
                {
                    int c = count;
                    if (c++ > threshold) // ensure capacity
                    {
                        Rehash();
                    }
                    HashEntry[] tab = table;
                    int index = hash & (tab.Length - 1);
                    HashEntry first = tab[index];
                    HashEntry e = first;
                    while (e != null && (e.hash != hash || !key.Equals(e.key)))
                    {
                        e = e.next;
                    }
    
                    V oldValue;
                    if (e != null)
                    {
                        oldValue = e.value;
                        if (!onlyIfAbsent)
                        {
                            e.value = value;
                        }
                    }
                    else
                    {
                        oldValue = null;
                        ++modCount;
                        tab[index] = new HashEntry(key, hash, first, value);
                        count = c; // write-volatile
                    }
                    return oldValue;
                }
                finally
                {
                    UnLock();
                }
            }
    
            public void Rehash()
            {
                HashEntry[] oldTable = table;
                int oldCapacity = oldTable.Length;
                if (oldCapacity >= MAXIMUM_CAPACITY)
                {
                    return;
                }
    
                /*
                 * Reclassify nodes in each list to new Map.  Because we are
                 * using power-of-two expansion, the elements from each bin
                 * must either stay at same index, or move with a power of two
                 * offset. We eliminate unnecessary node creation by catching
                 * cases where old nodes can be reused because their next
                 * fields won't change. Statistically, at the default
                 * threshold, only about one-sixth of them need cloning when
                 * a table doubles. The nodes they replace will be garbage
                 * collectable as soon as they are no longer referenced by any
                 * reader thread that may be in the midst of traversing table
                 * right now.
                 */
    
                HashEntry[] newTable = HashEntry.NewArray(oldCapacity<<1);
                threshold = (int)(newTable.Length * loadFactor);
                int sizeMask = newTable.Length - 1;
                for (int i = 0; i < oldCapacity ; i++)
                {
                    // We need to guarantee that any existing reads of old Map can
                    //  proceed. So we cannot yet null out each bin.
                    HashEntry e = oldTable[i];
    
                    if (e != null)
                    {
                        HashEntry next = e.next;
                        int idx = e.hash & sizeMask;
    
                        //  Single node on list
                        if (next == null)
                        {
                            newTable[idx] = e;
                        }
                        else
                        {
                            // Reuse trailing consecutive sequence at same slot
                            HashEntry lastRun = e;
                            int lastIdx = idx;
                            for (HashEntry last = next; last != null; last = last.next)
                            {
                                int k = last.hash & sizeMask;
                                if (k != lastIdx)
                                {
                                    lastIdx = k;
                                    lastRun = last;
                                }
                            }
                            newTable[lastIdx] = lastRun;
    
                            // Clone all remaining nodes
                            for (HashEntry p = e; p != lastRun; p = p.next)
                            {
                                int k = p.hash & sizeMask;
                                HashEntry n = newTable[k];
                                newTable[k] = new HashEntry(p.key, p.hash, n, p.value);
                            }
                        }
                    }
                }
                table = newTable;
            }
    
            /**
             * Remove; match on key only if value null, else match both.
             */
            public V Remove(K key, int hash, Object value)
            {
                Lock();
                try
                {
                    int c = count - 1;
                    HashEntry[] tab = table;
                    int index = hash & (tab.Length - 1);
                    HashEntry first = tab[index];
                    HashEntry e = first;
                    while (e != null && (e.hash != hash || !key.Equals(e.key)))
                    {
                        e = e.next;
                    }
    
                    V oldValue = null;
                    if (e != null)
                    {
                        V v = e.value;
                        if (value == null || value.Equals(v))
                        {
                            oldValue = v;
                            // All entries following removed node can stay
                            // in list, but all preceding ones need to be
                            // cloned.
                            ++modCount;
                            HashEntry newFirst = e.next;
                            for (HashEntry p = first; p != e; p = p.next)
                            {
                                newFirst = new HashEntry(p.key, p.hash, newFirst, p.value);
                            }
                            tab[index] = newFirst;
                            count = c; // write-volatile
                        }
                    }
                    return oldValue;
                }
                finally
                {
                    UnLock();
                }
            }
    
            public void Clear()
            {
                if (count != 0)
                {
                    Lock();
                    try
                    {
                        HashEntry[] tab = table;
                        for (int i = 0; i < tab.Length ; i++)
                        {
                            tab[i] = null;
                        }
                        ++modCount;
                        count = 0; // write-volatile
                    }
                    finally
                    {
                        UnLock();
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates a new, empty map with the specified initial capacity, load factor
        /// and concurrency level.
        /// </summary>
        /// <param name='initialCapacity'>
        /// The initial capacity. The implementation performs internal sizing to
        /// accommodate this many elements.
        /// </param>
        /// <param name='loadFactor'>
        /// The load factor threshold, used to control resizing. Resizing may be performed
        /// when the average number of elements per bin exceeds this threshold.
        /// </param>
        /// <param name='concurrencyLevel'>
        /// The estimated number of concurrently updating threads. The implementation
        /// performs internal sizing to try to accommodate this many threads.
        /// </param>
        /// <exception cref='ArgumentException'>
        /// Is thrown if the initial capacity is negative or the load factor or
        /// concurrencyLevel are nonpositive.
        /// </exception>
        public ConcurrentHashMap(int initialCapacity, float loadFactor, int concurrencyLevel)
        {
            if (!(loadFactor > 0) || initialCapacity < 0 || concurrencyLevel <= 0)
            {
                throw new ArgumentException();
            }
    
            if (concurrencyLevel > MAX_SEGMENTS)
            {
                concurrencyLevel = MAX_SEGMENTS;
            }

            // Find power-of-two sizes best matching arguments
            int sshift = 0;
            int ssize = 1;
            while (ssize < concurrencyLevel)
            {
                ++sshift;
                ssize <<= 1;
            }

            segmentShift = 32 - sshift;
            segmentMask = ssize - 1;
            this.segments = Segment.NewArray(ssize);

            if (initialCapacity > MAXIMUM_CAPACITY)
            {
                initialCapacity = MAXIMUM_CAPACITY;
            }

            int c = initialCapacity / ssize;
            if (c * ssize < initialCapacity)
            {
                ++c;
            }

            int cap = 1;
            while (cap < c)
            {
                cap <<= 1;
            }

            for (int i = 0; i < this.segments.Length; ++i)
            {
                this.segments[i] = new Segment(cap, loadFactor);
            }
        }

        /// <summary>
        /// Creates a new, empty map with the specified initial capacity and load
        /// factor and with the default concurrencyLevel (16).
        /// </summary>
        /// <param name='initialCapacity'>
        /// The implementation performs internal sizing to accommodate this many elements.
        /// </param>
        /// <param name='loadFactor'>
        /// The load factor threshold, used to control resizing. Resizing may be performed
        /// when the average number of elements per bin exceeds this threshold.
        /// </param>
        /// <exception cref='ArgumentException'>
        /// Is thrown if the initial capacity is negative or the load factor is nonpositive.
        /// </exception>
        public ConcurrentHashMap(int initialCapacity, float loadFactor) :
            this(initialCapacity, loadFactor, DEFAULT_CONCURRENCY_LEVEL)
        {
        }

        /// <summary>
        /// Creates a new, empty map with the specified initial capacity, and with
        /// default load factor (0.75) and concurrencyLevel (16).
        /// </summary>
        /// <param name='initialCapacity'>
        /// The implementation performs internal sizing to accommodate this many elements.
        /// </param>
        /// <exception cref='ArgumentException'>
        /// Is thrown if the initial capacity is negative.
        /// </exception>
        public ConcurrentHashMap(int initialCapacity) :
            this(initialCapacity, DEFAULT_LOAD_FACTOR, DEFAULT_CONCURRENCY_LEVEL)
        {
        }

        /// <summary>
        /// Creates a new, empty map with the default initial capacity (16), and with
        /// default load factor (0.75) and concurrencyLevel (16).
        /// </summary>
        public ConcurrentHashMap() :
            this(DEFAULT_INITIAL_CAPACITY, DEFAULT_LOAD_FACTOR, DEFAULT_CONCURRENCY_LEVEL)
        {
        }
    
        /// <summary>
        /// Creates a new map with the same mappings as the given map. The map is created
        /// with a capacity of 1.5 times the number of mappings in the given map or 16
        /// (whichever is greater), and a default load factor (0.75) and concurrencyLevel (16).
        /// </summary>
        public ConcurrentHashMap(Map<K, V> source) :
            this(Math.Max((int) (source.Size() / DEFAULT_LOAD_FACTOR) + 1, DEFAULT_INITIAL_CAPACITY),
                 DEFAULT_LOAD_FACTOR, DEFAULT_CONCURRENCY_LEVEL)
        {
            PutAll(source);
        }

        public override bool IsEmpty()
        {
            Segment[] segments = this.segments;

            /*
             * We keep track of per-segment modCounts to avoid ABA
             * problems in which an element in one segment was added and
             * in another removed during traversal, in which case the
             * table was never actually empty at any point. Note the
             * similar use of modCounts in the Size() and ContainsValue()
             * methods, which are the only other methods also susceptible
             * to ABA problems.
             */
            int[] mc = new int[segments.Length];
            int mcsum = 0;

            for (int i = 0; i < segments.Length; ++i)
            {
                if (segments[i].count != 0)
                {
                    return false;
                }
                else
                {
                    mcsum += mc[i] = segments[i].modCount;
                }
            }

            // If mcsum happens to be zero, then we know we got a snapshot
            // before any modifications at all were made.  This is
            // probably common enough to bother tracking.
            if (mcsum != 0)
            {
                for (int i = 0; i < segments.Length; ++i)
                {
                    if (segments[i].count != 0 || mc[i] != segments[i].modCount)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the number of key-value mappings in this map.  If the map contains
        /// more than Int32.MaxValue elements, returns Int32.MaxValue.
        /// </summary>
        public override int Size()
        {
            Segment[] segments = this.segments;
            long sum = 0;
            long check = 0;
            int[] mc = new int[segments.Length];

            // Try a few times to get accurate count. On failure due to
            // continuous async changes in table, resort to locking.
            for (int k = 0; k < RETRIES_BEFORE_LOCK; ++k)
            {
                check = 0;
                sum = 0;
                int mcsum = 0;
                for (int i = 0; i < segments.Length; ++i)
                {
                    sum += segments[i].count;
                    mcsum += mc[i] = segments[i].modCount;
                }

                if (mcsum != 0)
                {
                    for (int i = 0; i < segments.Length; ++i)
                    {
                        check += segments[i].count;
                        if (mc[i] != segments[i].modCount)
                        {
                            check = -1; // force retry
                            break;
                        }
                    }
                }

                if (check == sum)
                {
                    break;
                }
            }

            if (check != sum)
            {
                // Resort to locking all segments
                sum = 0;
                for (int i = 0; i < segments.Length; ++i)
                {
                    segments[i].Lock();
                }
                for (int i = 0; i < segments.Length; ++i)
                {
                    sum += segments[i].count;
                }
                for (int i = 0; i < segments.Length; ++i)
                {
                    segments[i].UnLock();
                }
            }

            if (sum > Int32.MaxValue)
            {
                return Int32.MaxValue;
            }
            else
            {
                return (int)sum;
            }
        }

        public override V Get(K key)
        {
            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Get(key, hash);
        }

        public override bool ContainsKey(K key)
        {
            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).ContainsKey(key, hash);
        }

        public override bool ContainsValue(V value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }
    
            // See explanation of modCount use above
    
            Segment[] segments = this.segments;
            int[] mc = new int[segments.Length];

            // Try a few times without locking
            for (int k = 0; k < RETRIES_BEFORE_LOCK; ++k)
            {
                int mcsum = 0;
                for (int i = 0; i < segments.Length; ++i)
                {
                    mcsum += mc[i] = segments[i].modCount;
                    if (segments[i].ContainsValue(value))
                    {
                        return true;
                    }
                }

                bool cleanSweep = true;
                if (mcsum != 0)
                {
                    for (int i = 0; i < segments.Length; ++i)
                    {
                        if (mc[i] != segments[i].modCount)
                        {
                            cleanSweep = false;
                            break;
                        }
                    }
                }
                if (cleanSweep)
                {
                    return false;
                }
            }

            // Resort to locking all segments
            for (int i = 0; i < segments.Length; ++i)
            {
                segments[i].Lock();
            }

            bool found = false;
            try
            {
                for (int i = 0; i < segments.Length; ++i)
                {
                    if (segments[i].ContainsValue(value))
                    {
                        found = true;
                        break;
                    }
                }
            }
            finally
            {
                for (int i = 0; i < segments.Length; ++i)
                {
                    segments[i].UnLock();
                }
            }

            return found;
        }

        public override V Put(K key, V value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }

            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Put(key, hash, value, false);
        }

        public virtual V PutIfAbsent(K key, V value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }

            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Put(key, hash, value, true);
        }

        public override void PutAll(Map<K, V> map)
        {
            Iterator<Entry<K, V>> iter = map.EntrySet().Iterator();
            while (iter.HasNext)
            {
                Entry<K, V> entry = iter.Next();
                Put(entry.Key, entry.Value);
            }
        }

        public override V Remove(K key)
        {
            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Remove(key, hash, null);
        }

        public virtual bool Remove(K key, V value)
        {
            int hash = Hash(key.GetHashCode());
            if (value == null)
            {
                return false;
            }

            return SegmentFor(hash).Remove(key, hash, value) != null;
        }

        public virtual bool Replace(K key, V oldValue, V newValue)
        {
            if (oldValue == null || newValue == null)
            {
                throw new NullReferenceException();
            }

            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Replace(key, hash, oldValue, newValue);
        }

        public virtual V Replace(K key, V value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }
            int hash = Hash(key.GetHashCode());
            return SegmentFor(hash).Replace(key, hash, value);
        }

        public override void Clear()
        {
            for (int i = 0; i < segments.Length; ++i)
            {
                segments[i].Clear();
            }
        }

        /// <summary>
        /// Returns a Set view of the keys contained in this map. The set is backed
        /// by the map, so changes to the map are reflected in the set, and vice-versa.
        /// The set supports element removal, which removes the corresponding mapping
        /// from this map, via the Iterator.Remove, Set.Remove, RemoveAll, RetainAll,
        /// and Clear operations.  It does not support the Add or AddAll operations.
        /// <para>
        /// The view's iterator is a "weakly consistent" iterator that will never
        /// throw ConcurrentModificationException, and guarantees to traverse elements
        /// as they existed upon construction of the iterator, and may (but is not
        /// guaranteed to) reflect any modifications subsequent to construction.
        /// </para>
        /// </summary>
        public override Set<K> KeySet()
        {
            Set<K> ks = keySet;
            return (ks != null) ? ks : (keySet = new KeySetImpl(this));
        }

        /// <summary>
        /// Returns a Collection view of the values contained in this map. The collection
        /// is backed by the map, so changes to the map are reflected in the collection,
        /// and vice-versa.  The collection supports element removal, which removes the
        /// corresponding mapping from this map, via the Iterator.Remove, Set.Remove,
        /// RemoveAll, RetainAll, and Clear operations.  It does not support the Add or
        /// AddAll operations.
        /// <para>
        /// The view's iterator is a "weakly consistent" iterator that will never
        /// throw ConcurrentModificationException, and guarantees to traverse elements
        /// as they existed upon construction of the iterator, and may (but is not
        /// guaranteed to) reflect any modifications subsequent to construction.
        /// </para>
        /// </summary>
        public override Collection<V> Values()
        {
            Collection<V> vs = values;
            return (vs != null) ? vs : (values = new ValuesImpl(this));
        }

        /// <summary>
        /// Returns a Set view of the mappings contained in this map. The set is
        /// backed by the map, so changes to the map are reflected in the set, and
        /// vice-versa.  The set supports element removal, which removes the corresponding
        /// mapping from this map, via the Iterator.Remove, Set.Remove, RemoveAll, RetainAll,
        /// and Clear operations.  It does not support the Add or AddAll operations.
        /// <para>
        /// The view's iterator is a "weakly consistent" iterator that will never
        /// throw ConcurrentModificationException, and guarantees to traverse elements
        /// as they existed upon construction of the iterator, and may (but is not
        /// guaranteed to) reflect any modifications subsequent to construction.
        /// </para>
        /// </summary>
        public override Set<Entry<K,V>> EntrySet()
        {
            Set<Entry<K,V>> es = entrySet;
            return (es != null) ? es : (entrySet = new EntrySetImpl(this));
        }

        #region Iterator support Classes and Methods.

        private abstract class HashIterator
        {
            protected int nextSegmentIndex;
            protected int nextTableIndex;
            protected HashEntry[] currentTable;
            protected HashEntry nextEntry;
            protected HashEntry lastReturned;

            protected readonly ConcurrentHashMap<K, V> parent;

            public HashIterator(ConcurrentHashMap<K, V> parent)
            {
                this.parent = parent;
                this.nextSegmentIndex = parent.segments.Length - 1;
                this.nextTableIndex = -1;

                Advance();
            }
    
            void Advance()
            {
                if (nextEntry != null && (nextEntry = nextEntry.next) != null)
                {
                    return;
                }
    
                while (nextTableIndex >= 0)
                {
                    if ((nextEntry = currentTable[nextTableIndex--]) != null)
                    {
                        return;
                    }
                }
    
                while (nextSegmentIndex >= 0)
                {
                    Segment seg = parent.segments[nextSegmentIndex--];
                    if (seg.count != 0)
                    {
                        currentTable = seg.table;
                        for (int j = currentTable.Length - 1; j >= 0; --j)
                        {
                            if ( (nextEntry = currentTable[j]) != null)
                            {
                                nextTableIndex = j - 1;
                                return;
                            }
                        }
                    }
                }
            }
    
            public bool HasNext
            {
                get { return nextEntry != null; }
            }
    
            protected HashEntry NextEntry()
            {
                if (nextEntry == null)
                {
                    throw new NoSuchElementException();
                }

                lastReturned = nextEntry;
                Advance();
                return lastReturned;
            }
    
            public void Remove()
            {
                if (lastReturned == null)
                {
                    throw new IllegalStateException();
                }
                parent.Remove(lastReturned.key);
                lastReturned = null;
            }
        }
    
        private class KeyIterator : HashIterator, Iterator<K>
        {
            public KeyIterator(ConcurrentHashMap<K, V> parent) : base(parent)
            {
            }

            public K Next()
            {
                return base.NextEntry().key;
            }
        }

        private class ValueIterator : HashIterator, Iterator<V>
        {
            public ValueIterator(ConcurrentHashMap<K, V> parent) : base(parent)
            {
            }

            public V Next()
            {
                return base.NextEntry().value;
            }
        }
    
        /// <summary>
        /// Custom Entry class used by EntryIterator.Next(), that relays SetValue changes
        /// to the underlying map.
        /// </summary>
        private class WriteThroughEntry : MapEntry<K,V>
        {
            protected readonly ConcurrentHashMap<K, V> parent;

            public WriteThroughEntry(ConcurrentHashMap<K, V> parent, K k, V v) : base(k,v)
            {
                this.parent = parent;
            }
    
            /// <summary>
            /// Set our entry's value and write through to the map.
            /// </summary>
            public override V Value
            {
                set
                {
                    if (value == null)
                    {
                        throw new NullReferenceException();
                    }

                    base.Value = value;
                    parent.Put(Key, value);
                }
            }
        }
    
        private class EntryIterator : HashIterator, Iterator<Entry<K,V>>
        {
            public EntryIterator(ConcurrentHashMap<K, V> parent) : base(parent)
            {
            }

            public Entry<K,V> Next()
            {
                HashEntry e = base.NextEntry();
                return new WriteThroughEntry(parent, e.key, e.value);
            }
        }
    
        private class KeySetImpl : AbstractSet<K>
        {
            protected readonly ConcurrentHashMap<K, V> parent;

            public KeySetImpl(ConcurrentHashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override Iterator<K> Iterator()
            {
                return new KeyIterator(parent);
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override bool IsEmpty()
            {
                return parent.IsEmpty();
            }

            public override bool Contains(K o)
            {
                return parent.ContainsKey(o);
            }

            public override bool Remove(K o)
            {
                return parent.Remove(o) != null;
            }

            public override void Clear()
            {
                parent.Clear();
            }
        }
    
        private class ValuesImpl : AbstractCollection<V>
        {
            protected readonly ConcurrentHashMap<K, V> parent;

            public ValuesImpl(ConcurrentHashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override Iterator<V> Iterator()
            {
                return new ValueIterator(parent);
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override bool IsEmpty()
            {
                return parent.IsEmpty();
            }

            public override bool Contains(V o)
            {
                return parent.ContainsValue(o);
            }

            public override void Clear()
            {
                parent.Clear();
            }
        }
    
        private class EntrySetImpl : AbstractSet<Entry<K,V>>
        {
            protected readonly ConcurrentHashMap<K, V> parent;

            public EntrySetImpl(ConcurrentHashMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override Iterator<Entry<K,V>> Iterator()
            {
                return new EntryIterator(parent);
            }

            public override bool Contains(Entry<K, V> entry)
            {
                V v = parent.Get(entry.Key);
                return v != null && v.Equals(entry.Value);
            }

            public override bool Remove(Entry<K, V> e)
            {
                return parent.Remove(e.Key, e.Value);
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override bool IsEmpty()
            {
                return parent.IsEmpty();
            }

            public override void Clear()
            {
                parent.Clear();
            }
        }

        #endregion
    }
}

