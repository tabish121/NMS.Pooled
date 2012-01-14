/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
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
using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Collections
{
    [TestFixture]
    public class LinkedHashSetTest
    {
        private LinkedHashSet<Object> hs;
        private static Object[] objArray;

        static LinkedHashSetTest()
        {
            objArray = new Object[1000];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
            }
        }

        [Test]
        public void TestConstructor()
        {
            LinkedHashSet<Object> hs2 = new LinkedHashSet<Object>();
            Assert.AreEqual(0, hs2.Size(), "Created incorrect LinkedHashSet");
        }

        [Test]
        public void TestConstructorI()
        {
            LinkedHashSet<Object> hs2 = new LinkedHashSet<Object>(5);
            Assert.AreEqual(0, hs2.Size(), "Created incorrect LinkedHashSet");
            try
            {
                new LinkedHashSet<Object>(-1);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Failed to throw ArgumentException for capacity < 0");
        }

        [Test]
        public void TestConstructorIF()
        {
            LinkedHashSet<Object> hs2 = new LinkedHashSet<Object>(5, (float) 0.5);
            Assert.AreEqual(0, hs2.Size(), "Created incorrect LinkedHashSet");

            try
            {
                new LinkedHashSet<Object>(0, 0);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Failed to throw ArgumentException for initial load factor <= 0");
        }

        [Test]
        public void TestConstructorCollection()
        {
            LinkedHashSet<Object> hs2 = new LinkedHashSet<Object>(Arrays.AsList(objArray));
            for (int counter = 0; counter < objArray.Length; counter++)
            {
                Assert.IsTrue(hs.Contains(objArray[counter]),
                              "LinkedHashSet does not contain correct elements");
            }
            Assert.IsTrue(hs2.Size() == objArray.Length, "LinkedHashSet created from collection incorrect size");
        }

        [Test]
        public void TestAddObject()
        {
            int size = hs.Size();
            hs.Add(8);
            Assert.IsTrue(hs.Size() == size, "Added element already contained by set");
            hs.Add(-9);
            Assert.IsTrue(hs.Size() == size + 1, "Failed to increment set size after Add");
            Assert.IsTrue(hs.Contains(-9), "Failed to Add element to set");
        }

        [Test]
        public void TestClear()
        {
            Set<Object> orgSet = (Set<Object>) hs.Clone();
            hs.Clear();
            Iterator<Object> i = orgSet.Iterator();
            Assert.AreEqual(0, hs.Size(), "Returned non-zero size after Clear");

            while (i.HasNext)
            {
                Assert.IsTrue(!hs.Contains(i.Next()), "Failed to Clear set");
            }
        }

        [Test]
        public void TestClone()
        {
            LinkedHashSet<Object> hs2 = (LinkedHashSet<Object>) hs.Clone();
            Assert.IsTrue(hs != hs2, "Clone returned an equivalent LinkedHashSet");
            Assert.IsTrue(hs.Equals(hs2), "Clone did not return an equal LinkedHashSet");
        }

        [Test]
        public void TestContainsObject()
        {
            Assert.IsTrue(hs.Contains(objArray[90]), "Returned false for valid object");
            Assert.IsTrue(!hs.Contains(new Object()), "Returned true for invalid Object");
    
            LinkedHashSet<Object> s = new LinkedHashSet<Object>();
            s.Add(null);
            Assert.IsTrue(s.Contains(null), "Cannot handle null");
        }

        [Test]
        public void TestIsEmpty()
        {
            Assert.IsTrue(new LinkedHashSet<Object>().IsEmpty(), "Empty set returned false");
            Assert.IsTrue(!hs.IsEmpty(), "Non-empty set returned true");
        }

        [Test]
        public void TestIterator()
        {
            Iterator<Object> i = hs.Iterator();
            int x = 0;
            int j;
            for (j = 0; i.HasNext; j++)
            {
                Object oo = i.Next();
                if (oo != null)
                {
                    int ii = (int) oo;
                    Assert.IsTrue(ii == j, "Incorrect element found");
                }
                else
                {
                    Assert.IsTrue(hs.Contains(oo), "Cannot find null");
                }
                ++x;
            }
            Assert.IsTrue(hs.Size() == x, "Returned iteration of incorrect size");
    
            LinkedHashSet<Object> s = new LinkedHashSet<Object>();
            s.Add(null);
            Assert.IsNull(s.Iterator().Next(), "Cannot handle null");
        }

        [Test]
        public void test_RemoveLjava_lang_Object()
        {
            int size = hs.Size();
            hs.Remove((Object) 98);
            Assert.IsTrue(!hs.Contains(98), "Failed to Remove element");
            Assert.IsTrue(hs.Size() == size - 1, "Failed to decrement set size");
    
            LinkedHashSet<Object> s = new LinkedHashSet<Object>();
            s.Add(null);
            Assert.IsTrue(s.Remove(null), "Cannot handle null");
        }

        [Test]
        public void TestSize()
        {
            Assert.IsTrue(hs.Size() == (objArray.Length + 1), "Returned incorrect size");
            hs.Clear();
            Assert.AreEqual(0, hs.Size(), "Cleared set returned non-zero size");
        }

        [SetUp]
        protected void SetUp()
        {
            hs = new LinkedHashSet<Object>();

            for (int i = 0; i < objArray.Length; i++)
            {
                hs.Add(objArray[i]);
            }

            hs.Add(null);
        }
    }
}

