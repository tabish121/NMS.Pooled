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
    public class HashSet<E> : AbstractSet<E>, Set<E>, ICloneable where E : class
    {
        private readonly HashMap<E, HashSet<E>> backingMap;

        /// <summary>
        /// Create a new empty HashSet instance.
        /// </summary>
        public HashSet() : this(new HashMap<E, HashSet<E>>())
        {
        }

        /// <summary>
        /// Create a new empty HashSet instance with the given capacity.
        /// </summary>
        public HashSet(int capacity) : this(new HashMap<E, HashSet<E>>(capacity))
        {
        }

        /// <summary>
        /// Create a new empty HashSet instance with the given capacity and load factor.
        /// </summary>
        public HashSet(int capacity, float loadFactor) : this(new HashMap<E, HashSet<E>>(capacity, loadFactor))
        {
        }

        /// <summary>
        /// Create a new HashSet instance contianing only the unique elements from the specified collection.
        /// </summary>
        public HashSet(Collection<E> collection) : this(new HashMap<E, HashSet<E>>(collection.Size() < 6 ? 11 : collection.Size() * 2))
        {
            Iterator<E> iter = collection.Iterator();
            while (iter.HasNext)
            {
                Add(iter.Next());
            }
        }

        protected HashSet(HashMap<E, HashSet<E>> backingMap)
        {
            this.backingMap = backingMap;
        }

        /// <summary>
        /// Adds the specified object to this HashSet if not already present.
        /// </summary>
        public override bool Add(E obj)
        {
            return backingMap.Put(obj, this) == null;
        }

        public override void Clear()
        {
            backingMap.Clear();
        }
    
        public virtual Object Clone()
        {
            HashSet<E> clone = new HashSet<E>((HashMap<E, HashSet<E>>) backingMap.Clone());
            return clone;
        }

        public override bool Contains(E obj)
        {
            return backingMap.ContainsKey(obj);
        }

        public override bool IsEmpty()
        {
            return backingMap.IsEmpty();
        }
    
        public override Iterator<E> Iterator()
        {
            return backingMap.KeySet().Iterator();
        }

        public override bool Remove(E obj)
        {
            return backingMap.Remove(obj) != null;
        }
    
        /// <summary>
        /// Returns the number of elements in this HashSet.
        /// </summary>
        public override int Size()
        {
            return backingMap.Size();
        }
    }
}

