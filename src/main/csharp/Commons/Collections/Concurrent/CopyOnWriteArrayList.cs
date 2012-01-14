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
using System.Text;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    public class CopyOnWriteArrayList<E> : List<E>, ICloneable where E : class
    {
        private volatile E[] arr;
        private readonly Mutex arrayLock = new Mutex();
    
        /// <summary>
        /// Creates a new, empty instance of CopyOnWriteArrayList.
        /// </summary>
        public CopyOnWriteArrayList() : base()
        {
        }

        /// <summary>
        /// Creates a new instance of CopyOnWriteArrayList and fills it with the contents
        /// of the given Collection.
        /// </summary>
        public CopyOnWriteArrayList(Collection<E> c) : this(c.ToArray())
        {
        }
    
        /// <summary>
        /// Creates a new instance of CopyOnWriteArrayList and fills it with the contents
        /// of the given array.
        /// </summary>
        public CopyOnWriteArrayList(E[] array) : base()
        {
            int Size = array.Length;
            E[] data = NewElementArray(Size);
            for (int i = 0; i < Size; i++)
            {
                data[i] = array[i];
            }
            arr = data;
        }

        public bool Add(E e)
        {
            arrayLock.WaitOne();
            try
            {
                E[] data;
                E[] old = Data;
                int Size = old.Length;
                data = NewElementArray(Size + 1);
                System.Array.Copy(old, 0, data, 0, Size);
                data[Size] = e;
                Data = data;
                return true;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        public void Add(int index, E e)
        {
            arrayLock.WaitOne();
            try
            {
                E[] data;
                E[] old = Data;
                int size = old.Length;
                CheckIndexInclusive(index, size);
                data = NewElementArray(size+1);
                System.Array.Copy(old, 0, data, 0, index);
                data[index] = e;
                if (size > index)
                {
                    System.Array.Copy(old, index, data, index + 1, size - index);
                }
                Data = data;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }

        public bool AddAll(Collection<E> c)
        {
            Iterator<E> it = c.Iterator();
            int sSize = c.Size();
            arrayLock.WaitOne();
            try
            {
                int size = Size();
                E[] data;
                E[] old = Data;
                int nSize = size + sSize;
                data = NewElementArray(nSize);
                System.Array.Copy(old, 0, data, 0, size);
                while (it.HasNext)
                {
                    data[size++] = it.Next();
                }
                Data = data;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
            return true;
        }
    
        public bool AddAll(int index, Collection<E> c)
        {
            Iterator<E> it = c.Iterator();
            int sSize = c.Size();
            arrayLock.WaitOne();
            try
            {
                int size = Size();
                CheckIndexInclusive(index, size);
                E[] data;
                E[] old = Data;
                int nSize = size + sSize;
                data = NewElementArray(nSize);
                System.Array.Copy(old, 0, data, 0, index);
                int i = index;
                while (it.HasNext)
                {
                    data[i++] = (E) it.Next();
                }

                if (size > index)
                {
                    System.Array.Copy(old, index, data, index + sSize, size - index);
                }
                Data = data;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
            return true;
        }
    
        /// <summary>
        /// Adds to this CopyOnWriteArrayList all those elements from a given collection
        /// that are not yet part of the list.  Returns the number of elements actually
        /// added to this list.
        /// </summary>
        public int AddAllAbsent(Collection<E> c)
        {
            if (c.Size() == 0)
            {
                return 0;
            }

            arrayLock.WaitOne();
            try
            {
                E[] old = Data;
                int size = old.Length;
                E[] toAdd = NewElementArray(c.Size());
                int i = 0;
                for (Iterator<E> it = c.Iterator(); it.HasNext;)
                {
                    E o = (E) it.Next();
                    if (IndexOf(o) < 0)
                    {
                        toAdd[i++] = o;
                    }
                }
                E[] data = NewElementArray(size + i);
                System.Array.Copy(old, 0, data, 0, size);
                System.Array.Copy(toAdd, 0, data, size, i);
                Data = data;
                return i;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        /// <summary>
        /// Adds to this CopyOnWriteArrayList another element, given that this element is
        /// not yet part of the list.  Returns true if the element is added, otherwise false.
        /// </summary>
        public bool AddIfAbsent(E e)
        {
            arrayLock.WaitOne();
            try
            {
                E[] data;
                E[] old = Data;
                int size = old.Length;
                if (size != 0)
                {
                    if (IndexOf(e) >= 0)
                    {
                        return false;
                    }
                }
                data = NewElementArray(size + 1);
                System.Array.Copy(old, 0, data, 0, size);
                data[size] = e;
                Data = data;
                return true;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        public void Clear()
        {
            arrayLock.WaitOne();
            try
            {
                Data = NewElementArray(0);
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }

        public virtual Object Clone()
        {
            try
            {
                CopyOnWriteArrayList<E> thisClone = (CopyOnWriteArrayList<E>) base.MemberwiseClone();
                thisClone.Data = this.Data;
                return thisClone;
            }
            catch (NotSupportedException)
            {
                throw new ApplicationException("NotSupportedException is not expected here");
            }
        }

        public virtual bool Contains(E o)
        {
            return IndexOf(o) >= 0;
        }
    
        public virtual bool ContainsAll(Collection<E> c)
        {
            E[] data = Data;
            return ContainsAll(c, data, 0, data.Length);
        }

        public override bool Equals(Object o)
        {
            if (ReferenceEquals(o, this))
            {
                return true;
            }

            if (!(o is List<E>))
            {
                return false;
            }

            List<E> l = (List<E>) o;
            Iterator<E> it = l.ListIterator();
            Iterator<E> ourIt = ListIterator();
            while (it.HasNext)
            {
                if (!ourIt.HasNext)
                {
                    return false;
                }
                Object thisListElem = it.Next();
                Object anotherListElem = ourIt.Next();
                if (!(thisListElem == null ? anotherListElem == null : thisListElem.Equals(anotherListElem))) {
                    return false;
                }
            }

            if (ourIt.HasNext)
            {
                return false;
            }
            return true;
        }
    
        public E Get(int index)
        {
            E[] data = Data;
            return data[index];
        }

        public override int GetHashCode()
        {
            int hashCode = 1;
            Iterator<E> it = ListIterator();
            while (it.HasNext)
            {
                E obj = it.Next();
                hashCode = 31 * hashCode + (obj == null ? 0 : obj.GetHashCode());
            }
            return hashCode;
        }
    
        /// <summary>
        /// Returns the index of a given element, starting the search from a given
        /// position in the list.
        /// </summary>
        public int IndexOf(E e, int index)
        {
            E[] data = Data;
            return IndexOf(e, data, index, data.Length - index);
        }

        public int IndexOf(E o)
        {
            E[] data = Data;
            return IndexOf(o, data, 0, data.Length);
        }
    
        public bool IsEmpty()
        {
            return Size() == 0;
        }
    
        public Iterator<E> Iterator()
        {
            return new ListIteratorImpl(Data, 0);
        }
    
        /// <summary>
        /// Returns the last index of a given element, starting the search from a
        /// given position in the list and going backwards.
        /// </summary>
        public int LastIndexOf(E e, int index)
        {
            E[] data = Data;
            return LastIndexOf(e, data, 0, index);
        }
    
        public int LastIndexOf(E o)
        {
            E[] data = Data;
            return LastIndexOf(o, data, 0, data.Length);
        }
    
        public ListIterator<E> ListIterator()
        {
            return new ListIteratorImpl(Data, 0);
        }
    
        public ListIterator<E> ListIterator(int index)
        {
            E[] data = Data;
            CheckIndexInclusive(index, data.Length);
            return new ListIteratorImpl(data, index);
        }
    
        public E Remove(int index)
        {
            return RemoveRange(index, 1);
        }

        public bool Remove(E o)
        {
            arrayLock.WaitOne();
            try
            {
                int index = IndexOf(o);
                if (index == -1)
                {
                    return false;
                }
                Remove(index);
                return true;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        public bool RemoveAll(Collection<E> c)
        {
            arrayLock.WaitOne();
            try
            {
                return RemoveAll(c, 0, Data.Length) != 0;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        public bool RetainAll(Collection<E> c)
        {
            if (c == null)
            {
                throw new NullReferenceException();
            }
            arrayLock.WaitOne();
            try
            {
                return RetainAll(c, 0, Data.Length) != 0;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }

        public E Set(int index, E e)
        {
            arrayLock.WaitOne();
            try
            {
                int size = Size();
                CheckIndexExlusive(index, size);
                E[] data;
                data = NewElementArray(size);
                E[] oldArr = Data;
                System.Array.Copy(oldArr, 0, data, 0, size);
                E old = data[index];
                data[index] = e;
                Data = data;
                return old;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }
    
        public virtual int Size()
        {
            return Data.Length;
        }
    
        public virtual List<E> SubList(int fromIndex, int toIndex)
        {
            return new SubListImpl(this, fromIndex, toIndex);
        }

        public virtual E[] ToArray()
        {
            E[] data = Data;
            return ToArray(data, 0, data.Length);
        }
    
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("[");
    
            Iterator<E> it = ListIterator();
            while (it.HasNext)
            {
                sb.Append(it.Next());
                sb.Append(", ");
            }

            if (sb.Length > 1)
            {
                sb.Length = sb.Length - 2;
            }

            sb.Append("]");
            return sb.ToString();
        }

        #region Private and Package Private Members used to implement this Class.

        private E[] NewElementArray(int Size)
        {
            return new E[Size];
        }

        private E[] Data
        {
            get
            {
                if (arr == null)
                {
                    return NewElementArray(0);
                }
                return arr;
            }
            set { arr = value; }
        }

        private int RemoveAll(Collection<E> c, int start, int size)
        {
            int sSize = c.Size();
            if (sSize == 0)
            {
                return 0;
            }

            E[] old = Data;
            int arrSize = old.Length;
            if (arrSize == 0)
            {
                return 0;
            }

            E[] data = new E[size];
            int j = 0;
            for (int i = start; i < (start + size); i++)
            {
                if (!c.Contains(old[i]))
                {
                    data[j++] = old[i];
                }
            }

            if (j != size)
            {
                E[] result = NewElementArray(arrSize - (size - j));
                System.Array.Copy(old, 0, result, 0, start);
                System.Array.Copy(data, 0, result, start, j);
                System.Array.Copy(old, start + size, result, start + j, arrSize
                        - (start + size));
                Data = result;
                return (size - j);
            }
            return 0;
        }
    
        private int RetainAll(Collection<E> c, int start, int size)
        {
            E[] old = Data;
            if (size == 0)
            {
                return 0;
            }
            if (c.Size() == 0)
            {
                E[] data;
                if (size == old.Length)
                {
                    data = NewElementArray(0);
                }
                else
                {
                    data = NewElementArray(old.Length - size);
                    System.Array.Copy(old, 0, data, 0, start);
                    System.Array.Copy(old, start + size, data, start, old.Length - start - size);
                }
                Data = data;
                return size;
            }
            Object[] temp = new Object[size];
            int pos = 0;
            for (int i = start; i < (start + size); i++)
            {
                if (c.Contains(old[i])) {
                    temp[pos++] = old[i];
                }
            }

            if (pos == size)
            {
                return 0;
            }

            E[] updated = NewElementArray(pos + old.Length - size);
            System.Array.Copy(old, 0, updated, 0, start);
            System.Array.Copy(temp, 0, updated, start, pos);
            System.Array.Copy(old, start + size, updated, start + pos, old.Length - start - size);
            Data = updated;
            return (size - pos);
        }
    
        private E RemoveRange(int start, int size)
        {
            arrayLock.WaitOne();
            try
            {
                int sizeArr = Size();
                CheckIndexExlusive(start, sizeArr);
                CheckIndexInclusive(start + size, sizeArr);
                E[] data;
                data = NewElementArray(sizeArr - size);
                E[] oldArr = Data;
                System.Array.Copy(oldArr, 0, data, 0, start);
                E old = oldArr[start];
                if (sizeArr > (start + size))
                {
                    System.Array.Copy(oldArr, start + size, data, start, sizeArr
                            - (start + size));
                }
                Data = data;
                return old;
            }
            finally
            {
                arrayLock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Gets the internal data array in a non thread safe way.
        /// </summary>
        internal E[] Array
        {
            get { return arr; }
        }

        #endregion

        #region Static Methods used by the SubList and Iterator classes.

        /// <summary>
        /// Returns an array containing all of the elements in the specified
        /// range of the array in proper sequence.
        /// </summary>
        private static E[] ToArray(E[] data, int start, int size)
        {
            E[] result = new E[size];
            System.Array.Copy(data, start, result, 0, size);
            return result;
        }

        /// <summary>
        /// Returns an array containing all of the elements in the specified range
        /// of the array in proper sequence, stores the result in the array, specified
        /// by first parameter/
        /// </summary>
        private static E[] ToArray(E[] to, E[] data, int start, int size)
        {
            int l = data.Length;
            if (to.Length < l)
            {
                to = (E[]) System.Array.CreateInstance(to.GetType(), l);
            }
            else
            {
                if (to.Length > l)
                {
                    to[l] = null;
                }
            }
            System.Array.Copy(data, start, to, 0, size);
            return to;
        }
    
        /// <summary>
        /// Checks if the specified range of the array Contains all of the elements
        /// in the collection pssed to this method.
        /// </summary>
        private static bool ContainsAll(Collection<E> c, Object[] data, int start, int size)
        {
            if (size == 0)
            {
                return false;
            }

            Iterator<E> it = c.Iterator();
            while (it.HasNext)
            {
                Object next = it.Next();
                if (IndexOf(next, data, start, size) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the index in the specified range of the data array of the last
        /// occurrence of the specified element
        /// </summary>
        private static int LastIndexOf(Object o, Object[] data, int start, int size)
        {
            if (size == 0)
            {
                return -1;
            }

            if (o != null)
            {
                for (int i = start + size - 1; i > start - 1; i--)
                {
                    if (o.Equals(data[i]))
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = start + size - 1; i > start - 1; i--)
                {
                    if (data[i] == null)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index in the specified range of the data array of the first
        /// occurrence of the specified element.
        /// </summary>
        private static int IndexOf(Object o, Object[] data, int start, int size)
        {
            if (size == 0)
            {
                return -1;
            }

            if (o == null)
            {
                for (int i = start; i < start + size; i++)
                {
                    if (data[i] == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = start; i < start + size; i++)
                {
                    if (o.Equals(data[i]))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    
        /// <summary>
        /// Throws an IndexOutOfRangeException if index is out of the list bounds,
        /// including the last element.
        /// </summary>
        private static void CheckIndexInclusive(int index, int size)
        {
            if (index < 0 || index > size)
            {
                throw new IndexOutOfRangeException("Index is " + index + ", Size is " + size);
            }
        }
    
        /// <summary>
        /// Throws an IndexOutOfRangeException if index is out of the list bounds,
        /// excluding the last element.
        /// </summary>
        private static void CheckIndexExlusive(int index, int size)
        {
            if (index < 0 || index >= size)
            {
                throw new IndexOutOfRangeException("Index is " + index + ", Size is " + size);
            }
        }

        #endregion

        /// <summary>
        /// list Iterator implementation that, when created gets a snapshot of the list.
        /// This Iterator won't throw a ConcurrentModificationException since it has a
        /// snapshot of the original list at the time it was called.
        /// </summary>
        private class ListIteratorImpl : ListIterator<E>
        {
            private readonly E[] arr;
            private int current;
            private readonly int size;

            public ListIteratorImpl(E[] data, int current)
            {
                this.current = current;
                arr = data;
                size = data.Length;
            }

            public int Size
            {
                get { return size; }
            }

            public virtual void Add(E o)
            {
                throw new NotSupportedException("Unsupported operation add");
            }
    
            public virtual bool HasNext
            {
                get
                {
                    if (current < size)
                    {
                        return true;
                    }
                    return false;
                }
            }

            public virtual bool HasPrevious
            {
                get { return current > 0; }
            }

            public virtual E Next()
            {
                if (HasNext)
                {
                    return arr[current++];
                }
                throw new NoSuchElementException("pos is " + current + ", Size is " + Size);
            }
    
            public virtual int NextIndex
            {
                get { return current; }
            }
    
            public virtual E Previous()
            {
                if (HasPrevious)
                {
                    return arr[--current];
                }

                throw new NoSuchElementException("pos is " + (current-1) + ", Size is " + size);
            }
    
            public virtual int PreviousIndex
            {
                get { return current - 1; }
            }

            public virtual void Remove()
            {
                throw new NotSupportedException("Unsupported operation remove");
            }
    
            public virtual void Set(E o)
            {
                throw new NotSupportedException("Unsupported operation set");
            }
        }
    
        /// <summary>
        /// Keeps a state of Sublist implementation, Size and Array declared as readonly
        /// so we'll never get into an inconsistent state.
        /// </summary>
        private class SubListReadData
        {
            private readonly int size;
            private readonly E[] data;
    
            public SubListReadData(int size, E[] data)
            {
                this.size = size;
                this.data = data;
            }

            public int Size
            {
                get { return size; }
            }

            public E[] Data
            {
                get { return data; }
            }
        }

        private class SubListImpl : List<E>
        {
            private readonly CopyOnWriteArrayList<E> list;
            private volatile SubListReadData read;
            private readonly int start;
    
            public SubListImpl(CopyOnWriteArrayList<E> list, int fromIdx, int toIdx)
            {
                this.list = list;
                Object[] data = list.Data;
                int size = toIdx - fromIdx;
                CheckIndexExlusive(fromIdx, data.Length);
                CheckIndexInclusive(toIdx, data.Length);
                read = new SubListReadData(size, list.Data);
                start = fromIdx;
            }

            /// <summary>
            /// Throws ConcurrentModificationException when the list is structurally
            /// modified in the other way other than via this subList.  This method
            /// should be called under lock.
            /// </summary>
            private void CheckModifications()
            {
                if (read.Data != list.Data)
                {
                    throw new ConcurrentModificationException();
                }
            }

            public ListIterator<E> ListIterator(int startIdx)
            {
                return new SubListIterator(this, startIdx, read);
            }

            public E Set(int index, E obj)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckIndexExlusive(index, read.Size);
                    CheckModifications();
                    E result = list.Set(index + start, obj);
                    read = new SubListReadData(read.Size, list.Data);
                    return result;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }
    
            public E Get(int index)
            {
                SubListReadData data = read;
                if (data.Data != list.Data)
                {
                    list.arrayLock.WaitOne();
                    try
                    {
                        data = read;
                        if (data.Data != list.Data)
                        {
                            throw new ConcurrentModificationException();
                        }
                    }
                    finally
                    {
                        list.arrayLock.ReleaseMutex();
                    }
                }
                CheckIndexExlusive(index, data.Size);
                return data.Data[index + start];
            }

            public int Size()
            {
                return read.Size;
            }
    
            public E Remove(int index)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckIndexExlusive(index, read.Size);
                    CheckModifications();
                    E obj = list.Remove(index + start);
                    read = new SubListReadData(read.Size - 1, list.Data);
                    return obj;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }
    
            public void Add(int index, E element)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckIndexInclusive(index, read.Size);
                    CheckModifications();
                    list.Add(index + start, element);
                    read = new SubListReadData(read.Size + 1, list.Data);
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }

            public bool Add(E o)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    list.Add(start + read.Size, o);
                    read = new SubListReadData(read.Size + 1, list.Data);
                    return true;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }
    
            public bool AddAll(Collection<E> c)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    int d = list.Size();
                    list.AddAll(start + read.Size, c);
                    read = new SubListReadData(read.Size + (list.Size() - d), list.Data);
                    return true;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }
    
            public void Clear()
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    list.RemoveRange(start, read.Size);
                    read = new SubListReadData(0, list.Data);
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }

            public bool Contains(E o)
            {
                return IndexOf(o) != -1;
            }
    
            public bool ContainsAll(Collection<E> c)
            {
                SubListReadData b = read;
                return CopyOnWriteArrayList<E>.ContainsAll(c, b.Data, start, b.Size);
            }

            public int IndexOf(E o)
            {
                SubListReadData b = read;
                int ind = CopyOnWriteArrayList<E>.IndexOf(o, b.Data, start, b.Size) - start;
                return ind < 0 ? -1 : ind;
            }
    
            public bool IsEmpty()
            {
                return read.Size == 0;
            }
    
            public Iterator<E> Iterator()
            {
                return new SubListIterator(this, 0, read);
            }
    
            public int LastIndexOf(E o)
            {
                SubListReadData b = read;
                int ind = CopyOnWriteArrayList<E>.LastIndexOf(o, b.Data, start, b.Size) - start;
                return ind < 0 ? -1 : ind;
            }
    
            public ListIterator<E> ListIterator()
            {
                return new SubListIterator(this, 0, read);
            }
    
            public bool Remove(E o)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    int i = IndexOf(o);
                    if (i == -1)
                    {
                        return false;
                    }
                    bool result = list.Remove(i + start) != null;
                    if (result)
                    {
                        read = new SubListReadData(read.Size - 1, list.Data);
                    }
                    return result;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }
    
            public bool RemoveAll(Collection<E> c)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    int removed = list.RemoveAll(c, start, read.Size);
                    if (removed > 0)
                    {
                        read = new SubListReadData(read.Size - removed, list
                                .Data);
                        return true;
                    }
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
                return false;
            }

            public bool RetainAll(Collection<E> c)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckModifications();
                    int removed = list.RetainAll(c, start, read.Size);
                    if (removed > 0)
                    {
                        read = new SubListReadData(read.Size - removed, list
                                .Data);
                        return true;
                    }
                    return false;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }

            public List<E> SubList(int fromIndex, int toIndex)
            {
                return new SubListImpl(list, start + fromIndex, start + toIndex);
            }

            public E[] ToArray()
            {
                SubListReadData r = read;
                return CopyOnWriteArrayList<E>.ToArray(r.Data, start, r.Size);
            }

            public bool AddAll(int index, Collection<E> collection)
            {
                list.arrayLock.WaitOne();
                try
                {
                    CheckIndexInclusive(index, read.Size);
                    CheckModifications();
                    int d = list.Size();
                    bool rt = list.AddAll(index + start, collection);
                    read = new SubListReadData(read.Size + list.Size() - d, list
                            .Data);
                    return rt;
                }
                finally
                {
                    list.arrayLock.ReleaseMutex();
                }
            }

            /// <summary>
            /// Implementation of ListIterator used with the SubListImpl class that gets
            /// a snapshot of the SubList and never throws a ConcurrentModificationException
            /// </summary>
            private class SubListIterator : ListIteratorImpl
            {
                private readonly SubListImpl parent;
                private readonly SubListReadData dataR;

                public SubListIterator(SubListImpl parent, int index, SubListReadData d) :
                    base(d.Data, index + parent.start)
                {
                    this.parent = parent;
                    this.dataR = d;
                }

                public override int NextIndex
                {
                    get { return base.NextIndex - parent.start; }
                }

                public override int PreviousIndex
                {
                    get { return base.PreviousIndex - parent.start; }
                }

                public override bool HasNext
                {
                    get { return NextIndex < dataR.Size; }
                }

                public override bool HasPrevious
                {
                    get { return PreviousIndex > -1; }
                }
            }
        }
    }
}

