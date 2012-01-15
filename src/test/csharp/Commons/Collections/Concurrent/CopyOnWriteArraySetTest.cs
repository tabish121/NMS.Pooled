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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    [TestFixture]
    public class CopyOnWriteArraySetTest : ConcurrencyTestCase
    {
        static CopyOnWriteArraySet<Object> PopulatedSet(int n)
        {
            CopyOnWriteArraySet<Object> a = new CopyOnWriteArraySet<Object>();
            Assert.IsTrue(a.IsEmpty());

            for (int i = 0; i < n; ++i)
            {
                a.Add(i);
            }

            Assert.IsFalse(a.IsEmpty());
            Assert.AreEqual(n, a.Size());
            return a;
        }

        [Test]
        public void TestConstructor()
        {
            CopyOnWriteArraySet<Object> a = new CopyOnWriteArraySet<Object>();
            Assert.IsTrue(a.IsEmpty());
        }

        [Test]
        public void TestConstructor3()
        {
            Object[] ints = new Object[SIZE];
            for (int i = 0; i < SIZE-1; ++i)
            {
                ints[i] = i;
            }
            CopyOnWriteArraySet<Object> a = new CopyOnWriteArraySet<Object>(Arrays.AsList(ints));
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.IsTrue(a.Contains(ints[i]));
            }
        }

        [Test]
        public void TestAddAll()
        {
             CopyOnWriteArraySet<Object> full = PopulatedSet(3);
             ArrayList<Object> v = new ArrayList<Object>();
             v.Add(three);
             v.Add(four);
             v.Add(five);
             full.AddAll(v);
             Assert.AreEqual(6, full.Size());
        }

        [Test]
        public void TestAddAll2()
        {
             CopyOnWriteArraySet<Object> full = PopulatedSet(3);
             ArrayList<Object> v = new ArrayList<Object>();
             v.Add(3);
             v.Add(4);
             v.Add(1); // will not add this element
             full.AddAll(v);
             Assert.AreEqual(5, full.Size());
        }
    
        [Test]
        public void TestAdd2()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            full.Add(1);
            Assert.AreEqual(3, full.Size());
        }

        [Test]
        public void TestAdd3()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            full.Add(three);
            Assert.IsTrue(full.Contains(three));
        }

        [Test]
        public void TestClear()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            full.Clear();
            Assert.AreEqual(0, full.Size());
        }
    
        [Test]
        public void TestContains()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Assert.IsTrue(full.Contains(1));
            Assert.IsFalse(full.Contains(five));
        }
    
        [Test]
        public void TestEquals()
        {
            CopyOnWriteArraySet<Object> a = PopulatedSet(3);
            CopyOnWriteArraySet<Object> b = PopulatedSet(3);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(b.Equals(a));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            a.Add(m1);
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(b.Equals(a));
            b.Add(m1);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(b.Equals(a));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    
        [Test]
        public void TestContainsAll()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(1);
            v.Add(2);
            Assert.IsTrue(full.ContainsAll(v));
            v.Add(6);
            Assert.IsFalse(full.ContainsAll(v));
        }

        [Test]
        public void TestIsEmpty()
        {
            CopyOnWriteArraySet<Object> empty = new CopyOnWriteArraySet<Object>();
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Assert.IsTrue(empty.IsEmpty());
            Assert.IsFalse(full.IsEmpty());
        }

        [Test]
        public void TestIterator()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Iterator<Object> i = full.Iterator();
            int j;
            for(j = 0; i.HasNext; j++)
            {
                Assert.AreEqual(j, i.Next());
            }
            Assert.AreEqual(3, j);
        }

        [Test]
        public void TestIteratorRemove()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Iterator<Object> it = full.Iterator();
            it.Next();
            try
            {
                it.Remove();
                ShouldThrow();
            }
            catch (NotSupportedException)
            {}
        }

        [Test]
        public void TestToString()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            String s = full.ToString();
            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(s.IndexOf(i.ToString()) >= 0);
            }
        }

        [Test]
        public void TestRemoveAll()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(1);
            v.Add(2);
            full.RemoveAll(v);
            Assert.AreEqual(1, full.Size());
        }

        [Test]
        public void TestRemove()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            full.Remove(1);
            Assert.IsFalse(full.Contains(one));
            Assert.AreEqual(2, full.Size());
        }

        [Test]
        public void TestSize()
        {
            CopyOnWriteArraySet<Object> empty = new CopyOnWriteArraySet<Object>();
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Assert.AreEqual(3, full.Size());
            Assert.AreEqual(0, empty.Size());
        }

        [Test]
        public void TestToArray()
        {
            CopyOnWriteArraySet<Object> full = PopulatedSet(3);
            Object[] o = full.ToArray();
            Assert.AreEqual(3, o.Length);
            Assert.AreEqual(0, o[0]);
            Assert.AreEqual(1, o[1]);
            Assert.AreEqual(2, o[2]);
        }
    }
}

