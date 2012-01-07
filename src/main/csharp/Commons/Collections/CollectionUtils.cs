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
    public sealed class CollectionUtils
    {
        private sealed class EmptyListImpl<E> : AbstractList<E> where E : class
        {
            public override bool Contains(E element)
            {
                return false;
            }

            public override int Size()
            {
                return 0;
            }
    
            public override E Get(int location)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private sealed class DoNothingIterator<E> : Iterator<E> where E : class
        {
            public bool HasNext
            {
                get { return false; }
            }

            public E Next()
            {
                throw new NoSuchElementException();
            }

            public void Remove()
            {
                throw new NotSupportedException();
            }
        }

        private sealed class EmptySetImpl<E> : AbstractSet<E> where E : class
        {
            public override bool Contains(E element)
            {
                return false;
            }

            public override int Size()
            {
                return 0;
            }

            public override Iterator<E> Iterator()
            {
                return new DoNothingIterator<E>();
            }
        }

        private sealed class EmptyMapImpl<K, V> : AbstractMap<K, V> where K : class where V : class
        {
            private static Set<Entry<K, V>> entries = new EmptySetImpl<Entry<K, V>>();
            private static Set<K> keys = new EmptySetImpl<K>();
            private static List<V> vals = new EmptyListImpl<V>();

            public override bool ContainsKey(K key)
            {
                return false;
            }
    
            public override bool ContainsValue(V value)
            {
                return false;
            }
    
            public override Set<Entry<K, V>> EntrySet()
            {
                return entries;
            }
    
            public override V Get(K key)
            {
                return null;
            }

            public override Set<K> KeySet()
            {
                return keys;
            }

            public override Collection<V> Values()
            {
                return vals;
            }
        }

        public static readonly List<Object> EMPTY_LIST = new EmptyListImpl<Object>();
        public static readonly Set<Object> EMPTY_SET = new EmptySetImpl<Object>();
        public static readonly Map<Object, Object> EMPTY_MAP = new EmptyMapImpl<Object, Object>();

        private sealed class SingletonSetImpl<E> : AbstractSet<E> where E : class
        {
            private readonly E element;

            public SingletonSetImpl(E element)
            {
                this.element = element;
            }
    
            public override bool Contains(E element)
            {
                return element == null ? this.element == null : this.element.Equals(element);
            }
    
            public override int Size()
            {
                return 1;
            }
    
            public override Iterator<E> Iterator()
            {
                return new SetIterator(this.element);
            }

            private sealed class SetIterator : Iterator<E>
            {
                private bool hasNext = true;
                private readonly E element;

                public SetIterator(E element)
                {
                    this.element = element;
                }

                public bool HasNext
                {
                    get { return hasNext; }
                }
    
                public E Next()
                {
                    if (hasNext)
                    {
                        hasNext = false;
                        return element;
                    }

                    throw new NoSuchElementException();
                }
    
                public void Remove()
                {
                    throw new NotSupportedException();
                }
            }
        }

        private sealed class SingletonListImpl<E> : AbstractList<E> where E : class
        {
            private readonly E element;

            public SingletonListImpl(E element)
            {
                this.element = element;
            }

            public override bool Contains(E element)
            {
                return element == null ? this.element == null : this.element.Equals(element);
            }

            public override E Get(int location)
            {
                if (location == 0)
                {
                    return element;
                }

                throw new IndexOutOfRangeException();
            }
    
            public override int Size()
            {
                return 1;
            }
        }

        public static List<E> EmptyList<E>() where E : class
        {
            return new EmptyListImpl<E>();
        }

        public static Set<E> EmptySet<E>() where E : class
        {
            return new EmptySetImpl<E>();
        }

        public static Map<K, V> EmptyMap<K, V>() where K : class where V : class
        {
            return new EmptyMapImpl<K, V>();
        }

        /// <summary>
        /// Returns a set containing the specified element. The set cannot be modified.
        /// </summary>
        public static Set<E> Singleton<E>(E instance) where E : class
        {
            return new SingletonSetImpl<E>(instance);
        }

        /// <summary>
        /// Returns a list containing the specified element. The list cannot be modified.
        /// </summary>
        public static List<E> SingletonList<E>(E element) where E : class
        {
            return new SingletonListImpl<E>(element);
        }

        /// <summary>
        /// Returns a set containing the specified element. The set cannot be modified.
        /// </summary>
        public static Set<E> SingletonSet<E>(E element) where E : class
        {
            return new SingletonSetImpl<E>(element);
        }

    }
}

