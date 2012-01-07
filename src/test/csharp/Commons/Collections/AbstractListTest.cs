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
    public class AbstractListTest
    {
        private class SimpleList<E> : AbstractList<E> where E : class
        {
            ArrayList<E> arrayList;

            public SimpleList() : base()
            {
                this.arrayList = new ArrayList<E>();
            }

            public override E Get(int index)
            {
                return this.arrayList.Get(index);
            }

            public override void Add(int i, E o)
            {
                this.arrayList.Add(i, o);
            }

            public override E Remove(int i)
            {
                return this.arrayList.Remove(i);
            }

            public override int Size()
            {
                return this.arrayList.Size();
            }
        }

        private class MockArrayList<E> : AbstractList<E> where E : class
        {
            public ArrayList<E> list = new ArrayList<E>();

            public override E Remove(int idx)
            {
                modCount++;
                return list.Remove(idx);
            }

            public override E Get(int index)
            {
                return list.Get(index);
            }

            public override int Size()
            {
                return list.Size();
            }

            public override void Add(int idx, E o)
            {
                modCount += 10;
                list.Add(idx, o);
            }
        }

        private class MockRemoveFailureArrayList<E> : AbstractList<E> where E : class
        {
            public override E Get(int location)
            {
                return null;
            }

            public override int Size()
            {
                return 0;
            }

            public override E Remove(int idx)
            {
                modCount+=2;
                return null;
            }

            public int GetModCount()
            {
                return modCount;
            }
        }

        private class MockList<E> : AbstractList<E> where E : class
        {
            private ArrayList<E> list = null;

            public override E Get(int location)
            {
                return list.Get(location);
            }

            public override int Size()
            {
                return list.Size();
            }
        }

        [Test]
        public void TestGetHashCode()
        {
            List<String> list = new ArrayList<String>();
            list.Add("3");
            list.Add("15");
            list.Add("5");
            list.Add("1");
            list.Add("7");
            int hashCode = 1;
            Iterator<String> i = list.Iterator();

            while (i.HasNext)
            {
                String obj = i.Next();
                hashCode = 31 * hashCode + (obj == null ? 0 : obj.GetHashCode());
            }

            Assert.IsTrue(hashCode == list.GetHashCode(), "Incorrect hashCode returned.  Wanted: " + hashCode
                          + " got: " + list.GetHashCode());
        }

        [Test]
        public void TestIterator()
        {
            SimpleList<object> list = new SimpleList<object>();
            list.Add(new Object());
            list.Add(new Object());
            Iterator<object> it = list.Iterator();
            it.Next();
            it.Remove();
            it.Next();
        }

        [Test]
        public void TestListIterator()
        {
            Object tempValue;
            List<Object> list = new ArrayList<Object>();
            list.Add(3);
            list.Add(5);
            list.Add(5);
            list.Add(1);
            list.Add(7);
            ListIterator<Object> lit = list.ListIterator();
            Assert.IsTrue(!lit.HasPrevious, "Should not have previous");
            Assert.IsTrue(lit.HasNext, "Should have next");
            tempValue = lit.Next();
            Assert.IsTrue(tempValue.Equals(3),
                          "next returned wrong value.  Wanted 3, got: " + tempValue);
            tempValue = lit.Previous();

            SimpleList<Object> list2 = new SimpleList<Object>();
            list2.Add(new Object());
            ListIterator<Object> lit2 = list2.ListIterator();
            lit2.Add(new Object());
            lit2.Next();

            list = new MockArrayList<Object>();
            ListIterator<Object> it = list.ListIterator();
            it.Add("one");
            it.Add("two");
            Assert.AreEqual(2, list.Size());
        }

        [Test]
        public void TestIteratorNext()
        {
            MockArrayList<String> t = new MockArrayList<String>();
            t.list.Add("a");
            t.list.Add("b");
    
            Iterator<String> it = t.Iterator();

            while (it.HasNext)
            {
                it.Next();
            }

            try
            {
                it.Next();
                Assert.Fail("Should throw NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
            }

            t.Add("c");
            try
            {
                it.Remove();
                Assert.Fail("Should throw NoSuchElementException");
            }
            catch (ConcurrentModificationException)
            {
            }
         
            it = t.Iterator();
            try
            {
                it.Remove();
                Assert.Fail("Should throw IllegalStateException");
            }
            catch (IllegalStateException)
            {
            }

            Object value = it.Next();
            Assert.AreEqual("a", value);
        }

        [Test]
        public void TestSubListAddAll()
        {
            List<Object> mainList = new ArrayList<Object>();
            Object[] mainObjects = { "a", "b", "c" };
            mainList.AddAll(Arrays.AsList<Object>(mainObjects));
            List<Object> subList = mainList.SubList(1, 2);

            Assert.IsFalse(subList.Contains("a"), "subList should not contain \"a\"");
            Assert.IsFalse(subList.Contains("c"), "subList should not contain \"c\"");
            Assert.IsTrue(subList.Contains("b"), "subList should contain \"b\"");

            Object[] subObjects = { "one", "two", "three" };
            subList.AddAll(Arrays.AsList<Object>(subObjects));
            Assert.IsFalse(subList.Contains("a"), "subList should not contain \"a\"");
            Assert.IsFalse(subList.Contains("c"), "subList should not contain \"c\"");

            Object[] expected = { "b", "one", "two", "three" };
            ListIterator<Object> iter = subList.ListIterator();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(subList.Contains(expected[i]), "subList should contain " + expected[i]);
                Assert.IsTrue(iter.HasNext, "should be more elements");
                Assert.AreEqual(expected[i], iter.Next(), "element in incorrect position");
            }
        }

        [Test]
        public void TestRemove()
        {
            MockRemoveFailureArrayList<String> mrfal = new MockRemoveFailureArrayList<String>();
            Iterator<String> imrfal= mrfal.Iterator();
            imrfal.Next();
            imrfal.Remove();
            try
            {
                imrfal.Remove();
            }
            catch(ConcurrentModificationException)
            {
                Assert.Fail("Excepted to catch IllegalStateException not ConcurrentModificationException");
            }
            catch(IllegalStateException)
            {
            }
        }

        [Test]
        public void TestSubListExceptions()
        {
            List<String> holder = new ArrayList<String>(16);
            for (int i=0; i<10; i++)
            {
                holder.Add(i.ToString());
            }

            // parent change should cause sublist concurrentmodification fail
            List<String> sub = holder.SubList(0, holder.Size());
            holder.Add(11.ToString());

            try
            {
                sub.Size();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Add(12.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Add(0, 11.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Clear();
                Assert.Fail("Should throw ConcurrentModificationException.");
            } catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Contains(11.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            } catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Get(9);
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.IndexOf(10.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.IsEmpty();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Iterator();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.LastIndexOf(10.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.ListIterator();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.ListIterator(0);
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Remove(0);
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Remove(9);
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Set(0, 0.ToString());
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.Size();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.ToArray();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}
            try
            {
                sub.ToString();
                Assert.Fail("Should throw ConcurrentModificationException.");
            }
            catch (ConcurrentModificationException)
            {}

            holder.Clear();
        }

        [Test]
        public void TestIndexOf()
        {
            AbstractList<String> list = new MockArrayList<String>();
            String[] array = { "1", "2", "3", "4", "5" };
            list.AddAll(Arrays.AsList<String>(array));

            Assert.AreEqual(-1, list.IndexOf(0.ToString()), "find 0 in the list do not contain 0");
            Assert.AreEqual(2, list.IndexOf("3"), "did not return the right location of element 3");
            Assert.AreEqual(-1, list.IndexOf(null), "find null in the list do not contain null element");
            list.Add(null);
            Assert.AreEqual(5, list.IndexOf(null), "did not return the right location of element null");
        }

        [Test]
        public void TestLastIndexOf()
        {
            AbstractList<String> list = new MockArrayList<String>();
            String[] array = { "1", "2", "3", "4", "5", "5", "4", "3", "2", "1" };
            list.AddAll(Arrays.AsList<String>(array));

            Assert.AreEqual(-1, list.LastIndexOf(6.ToString()), "find 6 in the list do not contain 6");
            Assert.AreEqual(6, list.LastIndexOf(4.ToString()), "did not return the right location of element 4");
            Assert.AreEqual(-1, list.LastIndexOf(null), "find null in the list do not contain null element");
            list.Add(null);
            Assert.AreEqual(10, list.LastIndexOf(null), "did not return the right location of element null");
        }

        [Test]
        public void TestRemoveIndexed()
        {
            AbstractList<String> list = new MockList<String>();

            try
            {
                list.Remove(0);
                Assert.Fail("should throw NotSupportedException");
            }
            catch (NotSupportedException)
            {
            }
    
            try
            {
                list.Set(0, null);
                Assert.Fail("should throw NotSupportedException");
            }
            catch (NotSupportedException)
            {
            }
        }

    }
}

