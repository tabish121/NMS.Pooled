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
    /// AbstractList is an abstract implementation of the List interface, optimized
    /// for a backing store which supports random access. This implementation does
    /// not support adding or replacing. A subclass must implement the abstract
    /// methods Get() and Size(), and to create a modifiable List it's necessary to
    /// override the Add() method that currently throws an NotSupportedException.
    /// </summary>
    public abstract class AbstractList<E> : AbstractCollection<E>, List<E> where E : class
    {
        protected int modCount;

        #region List Iterators

        private class SimpleListIterator : Iterator<E>
        {
            protected int numLeft;
            protected int expectedModCount;
            protected int lastPosition = -1;
            protected AbstractList<E> parent;

            public SimpleListIterator(AbstractList<E> parent) : base()
            {
                this.parent = parent;
                this.numLeft = parent.Size();
                this.expectedModCount = parent.modCount;
            }

            public bool HasNext
            {
                get { return numLeft > 0; }
            }
    
            public E Next()
            {
                if (expectedModCount != parent.modCount)
                {
                    throw new ConcurrentModificationException();
                }

                try
                {
                    int index = parent.Size() - numLeft;
                    E result = parent.Get(index);
                    lastPosition = index;
                    numLeft--;
                    return result;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new NoSuchElementException();
                }
            }
    
            public void Remove()
            {
                if (lastPosition == -1)
                {
                    throw new IllegalStateException();
                }
                if (expectedModCount != parent.modCount)
                {
                    throw new ConcurrentModificationException();
                }
    
                try
                {
                    if (lastPosition == parent.Size() - numLeft)
                    {
                        numLeft--; // we're removing after a call to previous()
                    }
                    parent.Remove(lastPosition);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ConcurrentModificationException();
                }

                expectedModCount = parent.modCount;
                lastPosition = -1;
            }
        }

        private class FullListIterator : SimpleListIterator, ListIterator<E>
        {
            public FullListIterator(int start, AbstractList<E> parent) : base(parent)
            {
                if (start < 0 || start > numLeft)
                {
                    throw new IndexOutOfRangeException();
                }
                numLeft -= start;
            }
    
            public void Add(E element)
            {
                if (expectedModCount != parent.modCount)
                {
                    throw new ConcurrentModificationException();
                }
    
                try
                {
                    parent.Add(parent.Size() - numLeft, element);
                    expectedModCount = parent.modCount;
                    lastPosition = -1;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new NoSuchElementException();
                }
            }
    
            public bool HasPrevious
            {
                get { return numLeft < parent.Size(); }
            }
    
            public int NextIndex
            {
                get { return parent.Size() - numLeft; }
            }

            public E Previous()
            {
                if (expectedModCount != parent.modCount)
                {
                    throw new ConcurrentModificationException();
                }
    
                try
                {
                    int index = parent.Size() - numLeft - 1;
                    E result = parent.Get(index);
                    numLeft++;
                    lastPosition = index;
                    return result;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new NoSuchElementException();
                }
            }
    
            public int PreviousIndex
            {
                get { return parent.Size() - numLeft - 1; }
            }
    
            public void Set(E element)
            {
                if (expectedModCount != parent.modCount)
                {
                    throw new ConcurrentModificationException();
                }
    
                try
                {
                    parent.Set(lastPosition, element);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new IllegalStateException();
                }
            }
        }

        #endregion

        #region Implementation of a SubList class

        private class SubAbstractList : AbstractList<E>
        {
            private readonly AbstractList<E> fullList;
            private int offset;
            private int size;

            private class SubAbstractListIterator : ListIterator<E>
            {
                private readonly SubAbstractList subList;
                private readonly ListIterator<E> iterator;
                private int start;
                private int end;

                public SubAbstractListIterator(ListIterator<E> it, SubAbstractList list, int offset, int length) : base()
                {
                    iterator = it;
                    subList = list;
                    start = offset;
                    end = start + length;
                }
    
                public void Add(E element)
                {
                    iterator.Add(element);
                    subList.SizeChanged(true);
                    end++;
                }
    
                public bool HasNext
                {
                    get { return iterator.NextIndex < end; }
                }
    
                public bool HasPrevious
                {
                    get { return iterator.PreviousIndex >= start; }
                }

                public E Next()
                {
                    if (iterator.NextIndex < end)
                    {
                        return iterator.Next();
                    }
                    throw new NoSuchElementException();
                }
    
                public int NextIndex
                {
                    get { return iterator.NextIndex - start; }
                }
    
                public E Previous()
                {
                    if (iterator.PreviousIndex >= start)
                    {
                        return iterator.Previous();
                    }
                    throw new NoSuchElementException();
                }
    
                public int PreviousIndex
                {
                    get
                    {
                        int previous = iterator.PreviousIndex;
                        if (previous >= start)
                        {
                            return previous - start;
                        }
                        return -1;
                    }
                }

                public void Remove()
                {
                    iterator.Remove();
                    subList.SizeChanged(false);
                    end--;
                }

                public void Set(E element)
                {
                    iterator.Set(element);
                }
            }
    
            public SubAbstractList(AbstractList<E> list, int start, int end) : base()
            {
                fullList = list;
                modCount = fullList.modCount;
                offset = start;
                size = end - start;
            }

            public override void Add(int location, E element)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location <= size)
                    {
                        fullList.Add(location + offset, element);
                        size++;
                        modCount = fullList.modCount;
                    }
                    else
                    {
                        throw new IndexOutOfRangeException();
                    }
                }
                else
                {
                    throw new ConcurrentModificationException();
                }
            }
    
            public override bool AddAll(int location, Collection<E> collection)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location <= size)
                    {
                        bool result = fullList.AddAll(location + offset, collection);
                        if (result)
                        {
                            size += collection.Size();
                            modCount = fullList.modCount;
                        }
                        return result;
                    }
                    throw new IndexOutOfRangeException();
                }
                throw new ConcurrentModificationException();
            }
    
            public override bool AddAll(Collection<E> collection)
            {
                if (modCount == fullList.modCount)
                {
                    bool result = fullList.AddAll(offset + size, collection);
                    if (result)
                    {
                        size += collection.Size();
                        modCount = fullList.modCount;
                    }
                    return result;
                }
                throw new ConcurrentModificationException();
            }
    
            public override E Get(int location)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location < size)
                    {
                        return fullList.Get(location + offset);
                    }
                    throw new IndexOutOfRangeException();
                }
                throw new ConcurrentModificationException();
            }
    
            public override Iterator<E> Iterator()
            {
                return ListIterator(0);
            }
    
            public override ListIterator<E> ListIterator(int location)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location <= size)
                    {
                        return new SubAbstractListIterator(fullList
                                .ListIterator(location + offset), this, offset,
                                size);
                    }
                    throw new IndexOutOfRangeException();
                }
                throw new ConcurrentModificationException();
            }

            public override E Remove(int location)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location < size)
                    {
                        E result = fullList.Remove(location + offset);
                        size--;
                        modCount = fullList.modCount;
                        return result;
                    }
                    throw new IndexOutOfRangeException();
                }
                throw new ConcurrentModificationException();
            }
    
            protected override void RemoveRange(int start, int end)
            {
                if (start != end)
                {
                    if (modCount == fullList.modCount)
                    {
                        fullList.RemoveRange(start + offset, end + offset);
                        size -= end - start;
                        modCount = fullList.modCount;
                    }
                    else
                    {
                        throw new ConcurrentModificationException();
                    }
                }
            }
    
            public override E Set(int location, E element)
            {
                if (modCount == fullList.modCount)
                {
                    if (0 <= location && location < size)
                    {
                        return fullList.Set(location + offset, element);
                    }
                    throw new IndexOutOfRangeException();
                }
                throw new ConcurrentModificationException();
            }
    
            public override int Size()
            {
                if (modCount == fullList.modCount)
                {
                    return size;
                }
                throw new ConcurrentModificationException();
            }

            internal void SizeChanged(bool increment)
            {
                if (increment)
                {
                    size++;
                }
                else
                {
                    size--;
                }
                modCount = fullList.modCount;
            }
        }

        #endregion

        protected AbstractList() : base()
        {
        }

        public virtual void Add(int location, E element)
        {
            throw new NotSupportedException();
        }

        public override bool Add(E element)
        {
            Add(Size(), element);
            return true;
        }

        public virtual bool AddAll(int location, Collection<E> collection)
        {
            Iterator<E> it = collection.Iterator();
            while (it.HasNext)
            {
                Add(location++, it.Next());
            }
            return !collection.IsEmpty();
        }

        public override void Clear()
        {
            RemoveRange(0, Size());
        }

        public override bool Equals(object that)
        {
            if (ReferenceEquals(this, that))
            {
                return true;
            }

            if (that is List<E>)
            {
                List<E> list = (List<E>) that;
                if (list.Size() != Size())
                {
                    return false;
                }

                Iterator<E> it1 = Iterator(), it2 = list.Iterator();
                while (it1.HasNext)
                {
                    E e1 = it1.Next(), e2 = it2.Next();
                    if (!(e1 == null ? e2 == null : e1.Equals(e2)))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public abstract E Get(int location);

        public override int GetHashCode()
        {
            int result = 1;
            Iterator<E> it = Iterator();
            while (it.HasNext)
            {
                E element = it.Next();
                result = (31 * result) + (element == null ? 0 : element.GetHashCode());
            }
            return result;
        }

        public virtual int IndexOf(E element)
        {
            ListIterator<E> it = ListIterator();
            if (element != null)
            {
                while (it.HasNext)
                {
                    if (element.Equals(it.Next()))
                    {
                        return it.PreviousIndex;
                    }
                }
            }
            else
            {
                while (it.HasNext)
                {
                    if (it.Next() == null)
                    {
                        return it.PreviousIndex;
                    }
                }
            }
            return -1;
        }

        public override Iterator<E> Iterator()
        {
            return new SimpleListIterator(this);
        }

        public virtual int LastIndexOf(E element)
        {
            ListIterator<E> it = ListIterator(Size());
            if (element != null)
            {
                while (it.HasPrevious)
                {
                    if (element.Equals(it.Previous()))
                    {
                        return it.NextIndex;
                    }
                }
            }
            else
            {
                while (it.HasPrevious)
                {
                    if (it.Previous() == null)
                    {
                        return it.NextIndex;
                    }
                }
            }
            return -1;
        }

        public virtual ListIterator<E> ListIterator()
        {
            return ListIterator(0);
        }

        public virtual ListIterator<E> ListIterator(int location)
        {
            return new FullListIterator(location, this);
        }

        public virtual E Remove(int location)
        {
            throw new NotSupportedException();
        }

        protected virtual void RemoveRange(int start, int end)
        {
            Iterator<E> it = ListIterator(start);
            for (int i = start; i < end; i++)
            {
                it.Next();
                it.Remove();
            }
        }

        public virtual E Set(int location, E element)
        {
            throw new NotSupportedException();
        }

        public virtual List<E> SubList(int start, int end)
        {
            if (0 <= start && end <= Size())
            {
                if (start <= end)
                {
                    return new SubAbstractList(this, start, end);
                }

                throw new ArgumentException();
            }

            throw new IndexOutOfRangeException();
        }
    }
}

