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
    /// <summary>
    /// LinkedHashSet is a variant of HashSet. Its entries are kept in a doubly-linked
    /// list. The iteration order is the order in which entries were inserted.
    /// </summary>
    public class LinkedHashSet<E> : HashSet<E>, Set<E>, ICloneable where E : class
    {
        /// <summary>
        /// Create a new empty HashSet instance.
        /// </summary>
        public LinkedHashSet() : base(new LinkedHashMap<E, HashSet<E>>())
        {
        }

        /// <summary>
        /// Create a new empty HashSet instance with the given capacity.
        /// </summary>
        public LinkedHashSet(int capacity) : base(new LinkedHashMap<E, HashSet<E>>(capacity))
        {
        }

        /// <summary>
        /// Create a new empty HashSet instance with the given capacity and load factor.
        /// </summary>
        public LinkedHashSet(int capacity, float loadFactor) : base(new LinkedHashMap<E, HashSet<E>>(capacity, loadFactor))
        {
        }

        /// <summary>
        /// Create a new HashSet instance contianing only the unique elements from the specified collection.
        /// </summary>
        public LinkedHashSet(Collection<E> collection) : base(new LinkedHashMap<E, HashSet<E>>(collection.Size() < 6 ? 11 : collection.Size() * 2))
        {
            Iterator<E> iter = collection.Iterator();
            while (iter.HasNext)
            {
                Add(iter.Next());
            }
        }
    }
}

