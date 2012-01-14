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
using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Collections
{
    [TestFixture]
    public class HashSetTest
    {
        private HashSet<Object> hs;
        private static Object[] objArray;

        static HashSetTest()
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
            HashSet<Object> hs2 = new HashSet<Object>();
            Assert.AreEqual(0, hs2.Size(), "Created incorrect HashSet");
        }

        [Test]
        public void TestConstructorI()
        {
            HashSet<Object> hs2 = new HashSet<Object>(5);
            Assert.AreEqual(0, hs2.Size(), "Created incorrect HashSet");

            try
            {
                new HashSet<Object>(-1);
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
            HashSet<Object> hs2 = new HashSet<Object>(5, (float) 0.5);
            Assert.AreEqual(0, hs2.Size(), "Created incorrect HashSet");

            try
            {
                new HashSet<Object>(0, 0);
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
            HashSet<Object> hs2 = new HashSet<Object>(Arrays.AsList(objArray));
            for (int counter = 0; counter < objArray.Length; counter++)
            {
                Assert.IsTrue(hs.Contains(objArray[counter]), "HashSet does not contain correct elements");
            }
            Assert.IsTrue(hs2.Size() == objArray.Length, "HashSet created from collection incorrect size");
        }

        [Test]
        public void TestAddObject()
        {
            int size = hs.Size();
            hs.Add(8);
            Assert.IsTrue(hs.Size() == size, "Added element already contained by set");
            hs.Add(-9);
            Assert.IsTrue(hs.Size() == size + 1, "Failed to increment set size after add");
            Assert.IsTrue(hs.Contains(-9), "Failed to add element to set");
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
            HashSet<Object> hs2 = (HashSet<Object>) hs.Clone();
            Assert.IsTrue(hs != hs2, "Clone returned an equivalent HashSet");
            Assert.IsTrue(hs.Equals(hs2), "Clone did not return an equal HashSet");
        }

        [Test]
        public void TestContainsObject()
        {
            Assert.IsTrue(hs.Contains(objArray[90]), "Returned false for valid object");
            Assert.IsTrue(!hs.Contains(new Object()), "Returned true for invalid Object");

            HashSet<Object> s = new HashSet<Object>();
            s.Add(null);
            Assert.IsTrue(s.Contains(null), "Cannot handle null");
         }

        [Test]
        public void TestIsEmpty()
        {
            Assert.IsTrue(new HashSet<Object>().IsEmpty(), "Empty set returned false");
            Assert.IsTrue(!hs.IsEmpty(), "Non-empty set returned true");
        }

        [Test]
        public void TestIterator()
        {
            Iterator<Object> i = hs.Iterator();
            int x = 0;
            while (i.HasNext)
            {
                Assert.IsTrue(hs.Contains(i.Next()), "Failed to iterate over all elements");
                ++x;
            }

            Assert.IsTrue(hs.Size() == x, "Returned iteration of incorrect size");
            HashSet<Object> s = new HashSet<Object>();
            s.Add(null);
            Assert.IsNull(s.Iterator().Next(), "Cannot handle null");
        }

        [Test]
        public void TestRemoveObject()
        {
            int size = hs.Size();
            hs.Remove(98);
            Assert.IsTrue(!hs.Contains(98), "Failed to remove element");
            Assert.IsTrue(hs.Size() == size - 1, "Failed to decrement set size");

            HashSet<Object> s = new HashSet<Object>();
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

        [Test]
        public void TestToString()
        {
            HashSet<Object> s = new HashSet<Object>();
            s.Add(s);
            String result = s.ToString();
            Assert.IsTrue(result.IndexOf("(this") > -1, "should contain self ref");
        }

        [SetUp]
        public void SetUp()
        {
            hs = new HashSet<Object>();
            for (int i = 0; i < objArray.Length; i++)
            {
                hs.Add(objArray[i]);
            }
            hs.Add(null);
        }

    }
}

