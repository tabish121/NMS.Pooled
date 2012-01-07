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
    public class ArrayList<E> : AbstractList<E>, List<E> where E : class
    {
        private int firstIndex;
        private int size;
        private E[] array;

        /// <summary>
        /// Initializes a new instance of the ArrayList with initial capacity of ten.
        /// </summary>
        public ArrayList() : this(10)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArrayList with initial capacity given.
        /// </summary>
        public ArrayList(int capacity) : base()
        {
            if (capacity < 0)
            {
                throw new ArgumentException();
            }
            firstIndex = size = 0;
            array = NewElementArray(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the ArrayList copying all the elements of the given
        /// collection into this ArrayList.
        /// </summary>
        public ArrayList(Collection<E> collection) : base()
        {
            firstIndex = 0;
            E[] objects = collection.ToArray();
            size = objects.Length;

            array = NewElementArray(size + (size / 10));
            System.Array.Copy(objects, array, size);
            modCount = 1;
        }

        private E[] NewElementArray(int size)
        {
            return new E[size];
        }

        public override void Add(int location, E element)
        {
            if (location < 0 || location > size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Indeix out of range: {0}, Size: {1}", location, size));
            }

            if (location == 0)
            {
                if (firstIndex == 0)
                {
                    GrowAtFront(1);
                }
                array[--firstIndex] = element;
            }
            else if (location == size)
            {
                if (firstIndex + size == array.Length)
                {
                    GrowAtEnd(1);
                }
                array[firstIndex + size] = element;
            }
            else
            {
                // must be case: (0 < location && location < size)
                if (size == array.Length)
                {
                    GrowForInsert(location, 1);
                }
                else if (firstIndex + size == array.Length ||
                         (firstIndex > 0 && location < size / 2))
                {
                    System.Array.Copy(array, firstIndex, array, --firstIndex, location);
                }
                else
                {
                    int index = location + firstIndex;
                    System.Array.Copy(array, index, array, index + 1, size - location);
                }

                array[location + firstIndex] = element;
            }
    
            size++;
            modCount++;
        }

        public override bool Add(E element)
        {
            if (firstIndex + size == array.Length)
            {
                GrowAtEnd(1);
            }
            array[firstIndex + size] = element;
            size++;
            modCount++;
            return true;
        }

        public override bool AddAll(int location, Collection<E> collection)
        {
            if (collection.Size() == 0)
            {
                return false;
            }

            if (location < 0 || location > size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Indeix out of range: {0}, Size: {1}", location, size));
            }

            E[] dumparray = collection.ToArray();
            int growSize = dumparray.Length;

            if (location == 0)
            {
                GrowAtFront(growSize);
                firstIndex -= growSize;
            }
            else if (location == size)
            {
                if (firstIndex + size > array.Length - growSize)
                {
                    GrowAtEnd(growSize);
                }
            }
            else
            {
                // must be case: (0 < location && location < size)
                if (array.Length - size < growSize)
                {
                    GrowForInsert(location, growSize);
                }
                else if (firstIndex + size > array.Length - growSize ||
                         (firstIndex > 0 && location < size / 2))
                {
                    int newFirst = firstIndex - growSize;
                    if (newFirst < 0)
                    {
                        int index = location + firstIndex;
                        System.Array.Copy(array, index, array, index - newFirst,
                                size - location);
                        newFirst = 0;
                    }
                    System.Array.Copy(array, firstIndex, array, newFirst, location);
                    firstIndex = newFirst;
                }
                else
                {
                    int index = location + firstIndex;
                    System.Array.Copy(array, index, array, index + growSize, size - location);
                }
            }
    
            System.Array.Copy(dumparray, 0, this.array, location + firstIndex, growSize);
            size += growSize;
            modCount++;
            return true;
        }

        public override bool AddAll(Collection<E> collection)
        {
            E[] dumpArray = collection.ToArray();
            if (dumpArray.Length == 0)
            {
                return false;
            }

            if (dumpArray.Length > array.Length - (firstIndex + size))
            {
                GrowAtEnd(dumpArray.Length);
            }

            System.Array.Copy(dumpArray, 0, this.array, firstIndex + size, dumpArray.Length);
            size += dumpArray.Length;
            modCount++;
            return true;
        }

        public override void Clear()
        {
            if (size != 0)
            {
                array = NewElementArray(size);
                firstIndex = size = 0;
                modCount++;
            }
        }

        public override bool Contains(E element)
        {
            int lastIndex = firstIndex + size;
            if (element != null)
            {
                for (int i = firstIndex; i < lastIndex; i++)
                {
                    if (element.Equals(array[i]))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = firstIndex; i < lastIndex; i++)
                {
                    if (array[i] == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void EnsureCapacity(int minimumCapacity)
        {
            if (array.Length < minimumCapacity)
            {
                GrowAtEnd(minimumCapacity - array.Length);
            }
        }

        public override E Get(int location)
        {
            if (location < 0 || location >= size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Indeix out of range: {0}, Size: {1}", location, size));
            }

            return array[firstIndex + location];
        }

        public override int IndexOf(E element)
        {
            int lastIndex = firstIndex + size;
            if (element != null)
            {
                for (int i = firstIndex; i < lastIndex; i++)
                {
                    if (element.Equals(array[i]))
                    {
                        return i - firstIndex;
                    }
                }
            }
            else
            {
                for (int i = firstIndex; i < lastIndex; i++)
                {
                    if (array[i] == null)
                    {
                        return i - firstIndex;
                    }
                }
            }

            return -1;
        }

        public override bool IsEmpty()
        {
            return size == 0;
        }

        public override int LastIndexOf(E element)
        {
            int lastIndex = firstIndex + size;
            if (element != null)
            {
                for (int i = lastIndex - 1; i >= firstIndex; i--)
                {
                    if (element.Equals(array[i]))
                    {
                        return i - firstIndex;
                    }
                }
            }
            else
            {
                for (int i = lastIndex - 1; i >= firstIndex; i--)
                {
                    if (array[i] == null)
                    {
                        return i - firstIndex;
                    }
                }
            }
            return -1;
        }

        public override E Remove(int location)
        {
            E result;

            if (location < 0 || location >= size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Indeix out of range: {0}, Size: {1}", location, size));
            }

            if (location == 0)
            {
                result = array[firstIndex];
                array[firstIndex++] = null;
                if ((size-1) == 0)
                {
                    firstIndex = 0;
                }
            }
            else if (location == size - 1)
            {
                int lastIndex = firstIndex + size - 1;
                result = array[lastIndex];
                array[lastIndex] = null;
            }
            else
            {
                int elementIndex = firstIndex + location;
                result = array[elementIndex];
                if (location < size / 2)
                {
                    System.Array.Copy(array, firstIndex, array, firstIndex + 1,
                                     location);
                    array[firstIndex++] = null;
                }
                else
                {
                    System.Array.Copy(array, elementIndex + 1, array,
                                     elementIndex, size - location - 1);
                    array[firstIndex+size-1] = null;
                }
            }
            size--;

            modCount++;
            return result;
        }
    
        public override bool Remove(E element)
        {
            int location = IndexOf(element);
            if (location >= 0)
            {
                Remove(location);
                return true;
            }
            return false;
        }

        protected override void RemoveRange(int start, int end)
        {
            if (start < 0)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Start indeix out of range: {0}, Size: {1}", start, size));
            }
            else if (end > size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("End indeix out of range: {0}, Size: {1}", end, size));
            }
            else if (start > end)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Start index greater than end index: {0}, end: {1}", start, end));
            }
    
            if (start == end)
            {
                return;
            }

            if (end == size)
            {
                Arrays.Fill(array, firstIndex + start, firstIndex + size, null);
            }
            else if (start == 0)
            {
                Arrays.Fill(array, firstIndex, firstIndex + end, null);
                firstIndex += end;
            }
            else
            {
                System.Array.Copy(array, firstIndex + end, array, firstIndex + start, size - end);
                int lastIndex = firstIndex + size;
                int newLast = lastIndex + start - end;
                Arrays.Fill(array, newLast, lastIndex, null);
            }
            size -= end - start;
            modCount++;
        }
    
        public override E Set(int location, E element)
        {
            if (location < 0 || location >= size)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Indeix out of range: {0}, Size: {1}", location, size));
            }

            E result = array[firstIndex + location];
            array[firstIndex + location] = element;
            return result;
        }
    
        public override int Size()
        {
            return size;
        }
    
        public override E[] ToArray()
        {
            E[] result = new E[size];
            System.Array.Copy(array, firstIndex, result, 0, size);
            return result;
        }

        public void TrimToSize()
        {
            E[] newArray = NewElementArray(size);
            System.Array.Copy(array, firstIndex, newArray, 0, size);
            array = newArray;
            firstIndex = 0;
            modCount = 0;
        }

        #region Private Implementation Methods

        private void GrowAtEnd(int required)
        {
            if (array.Length - size >= required)
            {
                if (size != 0)
                {
                    System.Array.Copy(array, firstIndex, array, 0, size);
                    int start = size < firstIndex ? firstIndex : size;
                    Arrays.Fill(array, start, array.Length, null);
                }
                firstIndex = 0;
            }
            else
            {
                int increment = size / 2;
                if (required > increment)
                {
                    increment = required;
                }
                if (increment < 12)
                {
                    increment = 12;
                }
                E[] newArray = NewElementArray(size + increment);
                if (size != 0)
                {
                    System.Array.Copy(array, firstIndex, newArray, 0, size);
                    firstIndex = 0;
                }
                array = newArray;
            }
        }

        private void GrowAtFront(int required)
        {
            if (array.Length - size >= required)
            {
                int newFirst = array.Length - size;
                if (size != 0)
                {
                    System.Array.Copy(array, firstIndex, array, newFirst, size);
                    int lastIndex = firstIndex + size;
                    int length = lastIndex > newFirst ? newFirst : lastIndex;
                    Arrays.Fill(array, firstIndex, length, null);
                }
                firstIndex = newFirst;
            }
            else
            {
                int increment = size / 2;
                if (required > increment)
                {
                    increment = required;
                }
                if (increment < 12)
                {
                    increment = 12;
                }
                E[] newArray = NewElementArray(size + increment);
                if (size != 0)
                {
                    System.Array.Copy(array, firstIndex, newArray, increment, size);
                }
                firstIndex = newArray.Length - size;
                array = newArray;
            }
        }

        private void GrowForInsert(int location, int required)
        {
            int increment = size / 2;

            if (required > increment)
            {
                increment = required;
            }

            if (increment < 12)
            {
                increment = 12;
            }

            E[] newArray = NewElementArray(size + increment);
            int newFirst = increment - required;
            // Copy elements after location to the new array skipping inserted
            // elements
            System.Array.Copy(array, location + firstIndex, newArray, newFirst
                    + location + required, size - location);
            // Copy elements before location to the new array from firstIndex
            System.Array.Copy(array, firstIndex, newArray, newFirst, location);
            firstIndex = newFirst;
            array = newArray;
        }

        #endregion

    }
}

