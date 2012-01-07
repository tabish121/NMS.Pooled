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
    ///  An AbstractSet is an abstract implementation of the Set interface. This
    /// implementation does not support adding. A subclass must implement the
    /// abstract methods Iterator() and Size().
    /// </summary>
    public abstract class AbstractSet<E> : AbstractCollection<E>, Set<E> where E : class
    {
        public AbstractSet() : base()
        {
        }

        public virtual bool Equals(E element)
        {
            if (ReferenceEquals(this, element))
            {
                return true;
            }

            if (element is Set<E>)
            {
                Set<E> s = element as Set<E>;

                try
                {
                    return Size() == s.Size() && ContainsAll(s);
                }
                catch (NullReferenceException)
                {
                    return false;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            int result = 0;
            Iterator<E> it = Iterator();

            while (it.HasNext)
            {
                Object next = it.Next();
                result += next == null ? 0 : next.GetHashCode();
            }

            return result;
        }

        public override bool RemoveAll(Collection<E> collection)
        {
            bool result = false;
            if (Size() <= collection.Size())
            {
                Iterator<E> it = Iterator();
                while (it.HasNext)
                {
                    if (collection.Contains(it.Next()))
                    {
                        it.Remove();
                        result = true;
                    }
                }
            }
            else
            {
                Iterator<E> it = collection.Iterator();
                while (it.HasNext)
                {
                    result = Remove(it.Next()) || result;
                }
            }

            return result;
        }
    }
}

