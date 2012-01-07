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
    public sealed class Arrays
    {
        private class ArrayList<E> : AbstractList<E>, List<E> where E : class
        {
            private readonly E[] array;
    
            public ArrayList(E[] storage)
            {
                if (storage == null)
                {
                    throw new NullReferenceException();
                }

                array = storage;
            }

            public override bool Contains(E element)
            {
                if (element != null)
                {
                    foreach (E e in array)
                    {
                        if (element.Equals(e))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (E e in array)
                    {
                        if (e == null)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
    
            public override E Get(int location)
            {
                return array[location];
            }

            public override int IndexOf(E element)
            {
                if (element != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (element.Equals(array[i]))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i] == null)
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }
    
            public override int LastIndexOf(E element)
            {
                if (element != null)
                {
                    for (int i = array.Length - 1; i >= 0; i--)
                    {
                        if (element.Equals(array[i]))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    for (int i = array.Length - 1; i >= 0; i--)
                    {
                        if (array[i] == null)
                        {
                            return i;
                        }
                    }
                }
                return -1;
            }
    
            public override E Set(int location, E element)
            {
                try
                {
                    E result = array[location];
                    array[location] = element;
                    return result;
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new InvalidCastException();
                }
            }

            public override int Size()
            {
                return array.Length;
            }
    
            public override E[] ToArray()
            {
                return (E[]) array.Clone();
            }

        }

        private Arrays()
        {
        }

        public static List<E> AsList<E>(E[] array) where E : class
        {
            return new ArrayList<E>(array);
        }

        public static void Fill(Array array, object value)
        {
            for(int i = 0; i < array.Length; ++i)
            {
                array.SetValue(value, i);
            }
        }

        public static void Fill(Array array, int start, int end, object value)
        {
            CheckBounds(array.Length, start, end);
            for (int i = start; i < end; i++)
            {
                array.SetValue(value, i);
            }
        }

        private static void CheckBounds(int arrLength, int start, int end)
        {
            if (start > end)
            {
                throw new ArgumentException(
                    String.Format("Start index [{0}] is greater than end index [{1}]", start, end));
            }

            if (start < 0)
            {
                throw new ArgumentException(
                    String.Format("Array start index is out of range: {0}", start));
            }

            if (end > arrLength)
            {
                throw new ArgumentException(
                    String.Format("Array end index is out of range: {0}", end));
            }
        }
    }
}

