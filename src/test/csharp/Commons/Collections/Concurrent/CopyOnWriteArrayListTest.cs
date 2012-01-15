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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    [TestFixture]
    public class CopyOnWriteArrayListTest : ConcurrencyTestCase
    {
        static CopyOnWriteArrayList<Object> PopulatedArray(int n)
        {
            CopyOnWriteArrayList<Object> a = new CopyOnWriteArrayList<Object>();
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
            CopyOnWriteArrayList<Object> a = new CopyOnWriteArrayList<Object>();
            Assert.IsTrue(a.IsEmpty());
        }

        [Test]
        public void TestConstructor2()
        {
            Object[] ints = new Object[SIZE];

            for (int i = 0; i < SIZE-1; ++i)
            {
                ints[i] = i;
            }
            CopyOnWriteArrayList<Object> a = new CopyOnWriteArrayList<Object>(ints);

            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(ints[i], a.Get(i));
            }
        }
    
        [Test]
        public void TestConstructor3()
        {
            Object[] ints = new Object[SIZE];
            for (int i = 0; i < SIZE-1; ++i)
            {
                ints[i] = i;
            }
            CopyOnWriteArrayList<Object> a = new CopyOnWriteArrayList<Object>(Arrays.AsList(ints));
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(ints[i], a.Get(i));
            }
        }
    
        [Test]
        public void TestAddAll()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(three);
            v.Add(four);
            v.Add(five);
            full.AddAll(v);
            Assert.AreEqual(6, full.Size());
        }

        [Test]
        public void TestAddAllAbsent()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(3);
            v.Add(4);
            v.Add(1); // will not Add this element
            full.AddAllAbsent(v);
            Assert.AreEqual(5, full.Size());
        }

        [Test]
        public void TestAddIfAbsent()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            full.AddIfAbsent(1);
            Assert.AreEqual(SIZE, full.Size());
        }

        [Test]
        public void TestAddIfAbsent2()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            full.AddIfAbsent(three);
            Assert.IsTrue(full.Contains(three));
        }

        [Test]
        public void TestClear()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            full.Clear();
            Assert.AreEqual(0, full.Size());
        }

        [Test]
        public void TestClone()
        {
            CopyOnWriteArrayList<Object> l1 = PopulatedArray(SIZE);
            CopyOnWriteArrayList<Object> l2 = (CopyOnWriteArrayList<Object>)(l1.Clone());
            Assert.AreEqual(l1, l2);
            l1.Clear();
            Assert.IsFalse(l1.Equals(l2));
        }

        [Test]
        public void TestContains()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.IsTrue(full.Contains(1));
            Assert.IsFalse(full.Contains(5));
        }

        [Test]
        public void TestAddIndex()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            full.Add(0, m1);
            Assert.AreEqual(4, full.Size());
            Assert.AreEqual(m1, full.Get(0));
            Assert.AreEqual(zero, full.Get(1).ToString());

            full.Add(2, m2);
            Assert.AreEqual(5, full.Size());
            Assert.AreEqual(m2, full.Get(2));
            Assert.AreEqual(two, full.Get(4).ToString());
        }

        [Test]
        public void TestEquals()
        {
            CopyOnWriteArrayList<Object> a = PopulatedArray(3);
            CopyOnWriteArrayList<Object> b = PopulatedArray(3);
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
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(1);
            v.Add(2);
            Assert.IsTrue(full.ContainsAll(v));
            v.Add(6);
            Assert.IsFalse(full.ContainsAll(v));
        }

        [Test]
        public void TestGet()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.AreEqual(0, full.Get(0));
        }

        [Test]
        public void TestIndexOf()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.AreEqual(1, full.IndexOf(1));
            Assert.AreEqual(-1, full.IndexOf("puppies"));
        }

        [Test]
        public void TestIndexOf2()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.AreEqual(1, full.IndexOf(1, 0));
            Assert.AreEqual(-1, full.IndexOf(1, 2));
        }

        [Test]
        public void TestIsEmpty()
        {
            CopyOnWriteArrayList<Object> empty = new CopyOnWriteArrayList<Object>();
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            Assert.IsTrue(empty.IsEmpty());
            Assert.IsFalse(full.IsEmpty());
        }
    
        [Test]
        public void TestIterator()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            Iterator<Object> i = full.Iterator();
            int j;
            for(j = 0; i.HasNext; j++)
            {
                Assert.AreEqual(j, i.Next());
            }
            Assert.AreEqual(SIZE, j);
        }

        [Test]
        public void TestIteratorRemove()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
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
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            String s = full.ToString();
            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(s.IndexOf(i.ToString()) >= 0);
            }
        }
    
        [Test]
        public void TestLastIndexOf1()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            full.Add(one);
            full.Add(three);
            Assert.AreEqual(3, full.LastIndexOf(one));
            Assert.AreEqual(-1, full.LastIndexOf(six));
        }

        [Test]
        public void TestLastIndexOf2()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            full.Add(one);
            full.Add(three);
            Assert.AreEqual(3, full.LastIndexOf(one, 4));
            Assert.AreEqual(-1, full.LastIndexOf(three, 3));
        }

        [Test]
        public void TestListIterator1()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            ListIterator<Object> i = full.ListIterator();
            int j;
            for(j = 0; i.HasNext; j++)
            {
                Assert.AreEqual(j, i.Next());
            }
            Assert.AreEqual(SIZE, j);
        }
    
        [Test]
        public void TestListIterator2()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            ListIterator<Object> i = full.ListIterator(1);
            int j;
            for(j = 0; i.HasNext; j++)
            {
                Assert.AreEqual(j+1, i.Next());
            }
            Assert.AreEqual(2, j);
        }

        [Test]
        public void TestRemove()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.AreEqual(two, full.Remove(2).ToString());
            Assert.AreEqual(2, full.Size());
        }

        [Test]
        public void TestRemoveAll()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add(1);
            v.Add(2);
            full.RemoveAll(v);
            Assert.AreEqual(1, full.Size());
        }

        [Test]
        public void TestSet()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Assert.AreEqual(two, full.Set(2, four).ToString());
            Assert.AreEqual(4.ToString(), full.Get(2));
        }

        [Test]
        public void TestSize()
        {
            CopyOnWriteArrayList<Object> empty = new CopyOnWriteArrayList<Object>();
            CopyOnWriteArrayList<Object> full = PopulatedArray(SIZE);
            Assert.AreEqual(SIZE, full.Size());
            Assert.AreEqual(0, empty.Size());
        }
    
        [Test]
        public void TestToArray()
        {
            CopyOnWriteArrayList<Object> full = PopulatedArray(3);
            Object[] o = full.ToArray();
            Assert.AreEqual(3, o.Length);
            Assert.AreEqual(0, o[0]);
            Assert.AreEqual(1, o[1]);
            Assert.AreEqual(2, o[2]);
        }

        [Test]
        public void TestSubList()
        {
            CopyOnWriteArrayList<Object> a = PopulatedArray(10);
            Assert.IsTrue(a.SubList(1,1).IsEmpty());

            for(int j = 0; j < 9; ++j)
            {
                for(int i = j ; i < 10; ++i)
                {
                    List<Object> b = a.SubList(j,i);
                    for(int k = j; k < i; ++k)
                    {
                        Assert.AreEqual(k, b.Get(k-j));
                    }
                }
            }

            List<Object> s = a.SubList(2, 5);
            Assert.AreEqual(s.Size(), 3);
            s.Set(2, m1);
            Assert.AreEqual(a.Get(4), m1);
            s.Clear();
            Assert.AreEqual(a.Size(), 7);
        }

        [Test]
        public void TestGet1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Get(-1);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestGet2IndexOutOfBoundsException()
        {
            try {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.Add("asdad");
                c.Get(100);
                ShouldThrow();
            } catch(IndexOutOfRangeException){}
        }

        [Test]
        public void TestSet1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Set(-1, "qwerty");
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestSet2()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.Add("asdad");
                c.Set(100, "qwerty");
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestAdd1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add(-1,"qwerty");
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestAdd2IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.Add("asdasdasd");
                c.Add(100, "qwerty");
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }
    
        [Test]
        public void TestRemove1IndexOutOfBounds()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Remove(-1);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }
    
        [Test]
        public void TestRemove2IndexOutOfBounds()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.Add("adasdasd");
                c.Remove(100);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestAddAll1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.AddAll(-1, new ArrayList<Object>());
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }
    
        [Test]
        public void TestAddAll2IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.Add("asdasdasd");
                c.AddAll(100, new ArrayList<Object>());
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestListIterator1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.ListIterator(-1);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }
    
        [Test]
        public void TestListIterator2IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("adasd");
                c.Add("asdasdas");
                c.ListIterator(100);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestSubList1IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.SubList(-1,100);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }
    
        [Test]
        public void TestSubList2IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.Add("asdasd");
                c.SubList(1,100);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

        [Test]
        public void TestSubList3IndexOutOfBoundsException()
        {
            try
            {
                CopyOnWriteArrayList<Object> c = new CopyOnWriteArrayList<Object>();
                c.SubList(3,1);
                ShouldThrow();
            }
            catch(IndexOutOfRangeException)
            {}
        }

    }
}

