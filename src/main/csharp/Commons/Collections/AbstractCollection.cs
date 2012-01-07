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
    public abstract class AbstractCollection<E> : Collection<E> where E : class
    {
        public AbstractCollection() : base()
        {
        }

        public abstract int Size();

        public abstract Iterator<E> Iterator();

        public virtual bool Add(E item)
        {
            throw new NotSupportedException();
        }

        public virtual bool AddAll(Collection<E> collection)
        {
            bool result = false;
            Iterator<E> iterator = collection.Iterator();
            while (iterator.HasNext)
            {
                if (Add(iterator.Next()))
                {
                    result = true;
                }
            }
            return result;
        }

        public virtual void Clear()
        {
            Iterator<E> iterator = Iterator();
            while (iterator.HasNext)
            {
                iterator.Next();
                iterator.Remove();
            }
        }

        public virtual bool Contains(E element)
        {
            Iterator<E> it = Iterator();
            if (element != null)
            {
                while (it.HasNext)
                {
                    if (element.Equals(it.Next()))
                    {
                        return true;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    if (it.Next() == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual bool ContainsAll(Collection<E> collection)
        {
            Iterator<E> it = collection.Iterator();
            while (it.HasNext)
            {
                if (!Contains(it.Next()))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual bool IsEmpty()
        {
            return Size() == 0;
        }

        public virtual bool Remove(E element)
        {
            Iterator<E> it = Iterator();
            if (element != null)
            {
                while (it.HasNext)
                {
                    if (element.Equals(it.Next()))
                    {
                        it.Remove();
                        return true;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    if (it.Next() == null)
                    {
                        it.Remove();
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual bool RemoveAll(Collection<E> collection)
        {
            bool result = false;
            Iterator<E> it = Iterator();
            while (it.HasNext)
            {
                if (collection.Contains(it.Next()))
                {
                    it.Remove();
                    result = true;
                }
            }
            return result;
        }

        public virtual bool RetainAll(Collection<E> collection)
        {
            bool result = false;
            Iterator<E> it = Iterator();
            while (it.HasNext)
            {
                if (!collection.Contains(it.Next()))
                {
                    it.Remove();
                    result = true;
                }
            }
            return result;
        }

        public virtual E[] ToArray()
        {
            int size = Size();
            int index = 0;
            Iterator<E> it = Iterator();
            E[] array = new E[size];
            while (index < size)
            {
                array[index++] = it.Next();
            }
            return array;
        }

        /// <summary>
        /// Returns the string representation of this Collection. The presentation
        /// has a specific format. It is enclosed by square brackets ("[]"). Elements
        /// are separated by ', ' (comma and space).
        /// </summary>
        public override String ToString()
        {
            if (IsEmpty())
            {
                return "[]";
            }

            StringBuilder buffer = new StringBuilder(Size() * 16);
            buffer.Append('[');
            Iterator<E> it = Iterator();
            while (it.HasNext)
            {
                E next = it.Next();
                if (!ReferenceEquals(next, this))
                {
                    buffer.Append(next);
                }
                else
                {
                    buffer.Append("(this Collection)");
                }

                if (it.HasNext)
                {
                    buffer.Append(", ");
                }
            }
            buffer.Append(']');
            return buffer.ToString();
        }
    }
}

