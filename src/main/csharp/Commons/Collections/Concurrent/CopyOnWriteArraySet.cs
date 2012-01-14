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
using System.Threading;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    public class CopyOnWriteArraySet<E> : AbstractSet<E> where E : class
    {
        private readonly CopyOnWriteArrayList<E> list;
    
        /// <summary>
        /// Create an empty Set.
        /// </summary>
        public CopyOnWriteArraySet() : base()
        {
            list = new CopyOnWriteArrayList<E>();
        }
    
        /// <summary>
        /// Creates a set containing all of the elements of the specified collection.
        /// </summary>
        public CopyOnWriteArraySet(Collection<E> c)
        {
            list = new CopyOnWriteArrayList<E>();
            list.AddAllAbsent(c);
        }
    
        public override int Size()
        {
            return list.Size();
        }

        public override bool IsEmpty()
        {
            return list.IsEmpty();
        }

        public override bool Contains(E o)
        {
            return list.Contains(o);
        }
    
        public override E[] ToArray()
        {
            return list.ToArray();
        }

        public override void Clear()
        {
            list.Clear();
        }

        public override bool Remove(E o)
        {
            return list.Remove(o);
        }

        public override bool Add(E e)
        {
            return list.AddIfAbsent(e);
        }

        public override bool ContainsAll(Collection<E> c)
        {
            return list.ContainsAll(c);
        }

        public override bool AddAll(Collection<E> c)
        {
            return list.AddAllAbsent(c) > 0;
        }

        public override bool RemoveAll(Collection<E> c)
        {
            return list.RemoveAll(c);
        }

        public override bool RetainAll(Collection<E> c)
        {
            return list.RetainAll(c);
        }

        public override Iterator<E> Iterator()
        {
            return list.Iterator();
        }

        public override bool Equals(Object o)
        {
            if (ReferenceEquals(o, this))
            {
                return true;
            }

            if (!(o is Set<E>))
            {
                return false;
            }

            Set<E> set = (Set<E>)(o);
            Iterator<E> it = set.Iterator();

            // Uses O(n^2) algorithm that is only appropriate
            // for small sets, which CopyOnWriteArraySets should be.
    
            //  Use a single snapshot of underlying array
            Object[] elements = list.Array;
            int len = elements.Length;

            // Mark matched elements to avoid re-checking
            bool[] matched = new bool[len];
            int k = 0;
            outer: while (it.HasNext)
            {
                if (++k > len)
                {
                    return false;
                }
                E x = it.Next();
                for (int i = 0; i < len; ++i)
                {
                    if (!matched[i] && Eq(x, elements[i]))
                    {
                        matched[i] = true;
                        goto outer;
                    }
                }
                return false;
            }
            return k == len;
        }

        public override int GetHashCode()
        {
            return list.GetHashCode();
        }

        /// <summary>
        /// Null aware equality check.
        /// </summary>
        private static bool Eq(Object o1, Object o2)
        {
            return (o1 == null ? o2 == null : o1.Equals(o2));
        }
    }
}

