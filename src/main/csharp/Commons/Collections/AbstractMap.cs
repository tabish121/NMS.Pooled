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
using System.Text;

namespace Apache.NMS.Pooled.Commons.Collections
{
    public abstract class AbstractMap<K, V> : Map<K, V> where K : class where V : class
    {
        protected Set<K> keySet;
        protected Collection<V> valuesCollection;

        #region Entry Implementations for a Basic Map

        internal sealed class SimpleImmutableEntry : Entry<K, V>
        {
            private K key;
            private V val;

            public SimpleImmutableEntry(K theKey, V theValue)
            {
                this.key = theKey;
                this.val = theValue;
            }

            public K Key
            {
                get { return key; }
            }
    
            public V Value
            {
                get { return val; }
                set { throw new NotSupportedException(); }
            }

            public override bool Equals(Object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is Entry<K, V>)
                {
                    Entry<K, V> entry = obj as Entry<K, V>;
                    return (key == null ? entry.Key == null : key.Equals(entry.Key)) &&
                           (val == null ? entry.Value == null : val.Equals(entry.Value));
                }

                return false;
            }
    
            public override int GetHashCode()
            {
                return (key == null ? 0 : key.GetHashCode()) ^
                       (val == null ? 0 : val.GetHashCode());
            }
    
            public override String ToString()
            {
                return key + "=" + val;
            }

            public object Clone()
            {
                return base.MemberwiseClone();
            }
        }

        internal sealed class SimpleEntry : Entry<K, V>
        {
            private K key;
            private V val;

            public SimpleEntry(K theKey, V theValue)
            {
                this.key = theKey;
                this.val = theValue;
            }

            public K Key
            {
                get { return key; }
            }
    
            public V Value
            {
                get { return val; }
                set { this.val = value; }
            }

            public override bool Equals(Object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj is Entry<K, V>)
                {
                    Entry<K, V> entry = obj as Entry<K, V>;
                    return (key == null ? entry.Key == null : key.Equals(entry.Key)) &&
                           (val == null ? entry.Value == null : val.Equals(entry.Value));
                }

                return false;
            }
    
            public override int GetHashCode()
            {
                return (key == null ? 0 : key.GetHashCode()) ^
                       (val == null ? 0 : val.GetHashCode());
            }
    
            public override String ToString()
            {
                return key + "=" + val;
            }

            public object Clone()
            {
                return base.MemberwiseClone();
            }
        }

        private sealed class KeySetCollection : AbstractSet<K>
        {
            private AbstractMap<K, V> parent;

            private sealed class KeySetIterator : Iterator<K>
            {
                Iterator<Entry<K, V>> setIterator;

                public KeySetIterator(KeySetCollection parent) : base()
                {
                    this.setIterator = parent.parent.EntrySet().Iterator();
                }

                public bool HasNext
                {
                    get { return setIterator.HasNext; }
                }

                public K Next()
                {
                    return setIterator.Next().Key;
                }

                public void Remove()
                {
                    setIterator.Remove();
                }
            };

            public KeySetCollection(AbstractMap<K, V> parent) : base()
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

            public override Iterator<K> Iterator()
            {
                return new KeySetIterator(this);
            }
        }

        private sealed class ValuesCollection : AbstractCollection<V>
        {
            private AbstractMap<K, V> parent;

            private sealed class ValuesCollectionIterator : Iterator<V>
            {
                Iterator<Entry<K, V>> setIterator;

                public ValuesCollectionIterator(ValuesCollection parent) : base()
                {
                    this.setIterator = parent.parent.EntrySet().Iterator();
                }

                public bool HasNext
                {
                    get { return setIterator.HasNext; }
                }

                public V Next()
                {
                    return setIterator.Next().Value;
                }

                public void Remove()
                {
                    setIterator.Remove();
                }
            };

            public ValuesCollection(AbstractMap<K, V> parent) : base()
            {
                this.parent = parent;
            }

            public override int Size()
            {
                return parent.Size();
            }

            public override bool Contains(V obj)
            {
                return parent.ContainsValue(obj);
            }

            public override Iterator<V> Iterator()
            {
                return new ValuesCollectionIterator(this);
            }
        }

        #endregion

        /// <summary>
        /// Clear this instance of all contained mappings.  If the Map doesn't
        /// support clearing then an NotSupportedException is thrown.
        /// </summary>
        public virtual void Clear()
        {
            EntrySet().Clear();
        }

        /// <summary>
        /// Determine whether the Map contains the givn key mapping.
        /// </summary>
        public virtual bool ContainsKey(K key)
        {
            Iterator<Entry<K, V>> it = EntrySet().Iterator();

            if (key != null)
            {
                while (it.HasNext)
                {
                    if (key.Equals(it.Next().Key))
                    {
                        return true;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    if (it.Next().Key == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determine whether the Map contains the givn value.
        /// </summary>
        public virtual bool ContainsValue(V val)
        {
            Iterator<Entry<K, V>> it = EntrySet().Iterator();
            if (val != null)
            {
                while (it.HasNext)
                {
                    if (val.Equals(it.Next().Value))
                    {
                        return true;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    if (it.Next().Value == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a Set containing all of the mappings in this map.  Each mapping is
        /// an instance of Entry.  As the set is backed by this map, changes in one will
        /// be reflected in the other.
        /// </summary>
        public abstract Set<Entry<K, V>> EntrySet();

        /// <summary>
        /// Determines whether the specified Object is equal to the current Map instance.
        /// </summary>
        public override bool Equals(Object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is Map<K, V>)
            {
                Map<K, V> map = obj as Map<K, V>;
                if (Size() != map.Size())
                {
                    return false;
                }

                try
                {
                    Iterator<Entry<K, V>> it = EntrySet().Iterator();
                    while (it.HasNext)
                    {
                        Entry<K, V> entry = it.Next();

                        K key = entry.Key;
                        V mine = entry.Value;

                        V theirs = map.Get(key);
                        if (mine == null)
                        {
                            if (theirs != null || !map.ContainsKey(key))
                            {
                                return false;
                            }
                        }
                        else if (!mine.Equals(theirs))
                        {
                            return false;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the value of the mapping with the specified key.
        /// </summary>
        public virtual V Get(K key)
        {
            Iterator<Entry<K, V>> it = EntrySet().Iterator();
            if (key != null)
            {
                while (it.HasNext)
                {
                    Entry<K, V> entry = it.Next();
                    if (key.Equals(entry.Key))
                    {
                        return entry.Value;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    Entry<K, V> entry = it.Next();
                    if (entry.Key == null)
                    {
                        return entry.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the hash code for this object. Objects which are equal must return the
        /// same value for this method.
        /// </summary>
        public override int GetHashCode()
        {
            int result = 0;
            Iterator<Entry<K, V>> it = EntrySet().Iterator();
            while (it.HasNext)
            {
                result += it.Next().GetHashCode();
            }
            return result;
        }

        /// <summary>
        /// Determines whether this Map is empty.
        /// </summary>
        public virtual bool IsEmpty()
        {
            return Size() == 0;
        }

        /// <summary>
        /// Returns a set of the keys contained in this map. The set is backed by this map
        /// so changes to one are reflected by the other. The returned set does not support adding.
        /// </summary>
        public virtual Set<K> KeySet()
        {
            if (keySet == null)
            {
                keySet = new KeySetCollection(this);
            }
            return keySet;
        }

        public virtual V Put(K key, V val)
        {
            throw new NotSupportedException();
        }

        public virtual void PutAll(Map<K, V> map)
        {
            Iterator<Entry<K, V>> it = map.EntrySet().Iterator();
            while (it.HasNext)
            {
                Entry<K, V> entry = it.Next();
                Put(entry.Key, entry.Value);
            }
        }

        public virtual V Remove(K key)
        {
            Iterator<Entry<K, V>> it = EntrySet().Iterator();
            if (key != null)
            {
                while (it.HasNext)
                {
                    Entry<K, V> entry = it.Next();
                    if (key.Equals(entry.Key))
                    {
                        it.Remove();
                        return entry.Value;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    Entry<K, V> entry = it.Next();
                    if (entry.Key == null)
                    {
                        it.Remove();
                        return entry.Value;
                    }
                }
            }
            return null;
        }
    
        /// <summary>
        /// Returns the number of elements in this map.
        /// </summary>
        public virtual int Size()
        {
            return EntrySet().Size();
        }

        /// <summary>
        /// Returns the string representation of this Map.
        /// </summary>
        public override String ToString()
        {
            if (IsEmpty())
            {
                return "{}";
            }

            StringBuilder buffer = new StringBuilder(Size() * 28);
            buffer.Append('{');
            Iterator<Entry<K, V>> it = EntrySet().Iterator();
            while (it.HasNext)
            {
                Entry<K, V> entry = it.Next();
                K key = entry.Key;
                if (!ReferenceEquals(key, this))
                {
                    buffer.Append(key);
                }
                else
                {
                    buffer.Append("(this Map)");
                }
                buffer.Append('=');
                V val = entry.Value;
                if (!ReferenceEquals(val, this))
                {
                    buffer.Append(val);
                }
                else
                {
                    buffer.Append("(this Map)");
                }

                if (it.HasNext)
                {
                    buffer.Append(", ");
                }
            }

            buffer.Append('}');
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a collection of the values contained in this map. The collection is
        /// backed by this map so changes to one are reflected by the other. The collection
        /// supports remove, removeAll, retainAll and clear operations, and it does not
        /// support add or addAll operations.
        /// </summary>
        public virtual Collection<V> Values()
        {
            if (valuesCollection == null)
            {
                valuesCollection = new ValuesCollection(this);
            }
            return valuesCollection;
        }
    }
}

