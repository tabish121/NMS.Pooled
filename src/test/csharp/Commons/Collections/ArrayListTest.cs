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
    public class ArrayListTest
    {
        List<Object> alist;

        private static Object[] objArray;

        private class ShrinkOnSize : AbstractCollection<String>
        {
            public bool shrink = true;
            public readonly ArrayList<String> backingList = new ArrayList<String>();

            public ShrinkOnSize() : base()
            {
            }

            public ShrinkOnSize(String[] array) : base()
            {
                foreach(String element in array)
                {
                    backingList.Add(element);
                }
            }

            public override int Size()
            {
                int result = backingList.Size();
                if(shrink)
                {
                    Iterator<String> iter = backingList.Iterator();

                    iter.Next();
                    iter.Remove();
                }

                return result;
            }

            public override String[] ToArray()
            {
                shrink = false;
                return backingList.ToArray();
            }

            public override Iterator<String> Iterator()
            {
                return backingList.Iterator();
            }
        }

        static ArrayListTest()
        {
            objArray = new Object[100];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i.ToString();
            }
        }

        private class ArrayListExtend : ArrayList<Object>
        {
            private int size = 0;

            public ArrayListExtend() : base(10)
            {
            }

            public override bool Add(Object o)
            {
                size++;
                return base.Add(o);
            }
    
            public override int Size()
            {
                return size;
            }
        }
    
        public class MockArrayList : ArrayList<Object>
        {
            public override int Size()
            {
                return 0;
            }

            protected override void RemoveRange(int start, int end)
            {
                base.RemoveRange(start, end);
            }
        }

        [SetUp]
        public void SetUp()
        {
            alist = new ArrayList<Object>();
            for (int i = 0; i < objArray.Length; i++)
            {
                alist.Add(objArray[i]);
            }
        }

        [Test]
        public void TestConstructor()
        {
            ArrayList<Object> array = new ArrayList<Object>();
            Assert.IsTrue(array.Size() == 0);
            Assert.IsTrue(array.IsEmpty());
            Assert.IsFalse(array.Contains("test"));
        }

        [Test]
        public void TestConstructorI()
        {
            ArrayList<Object> array = new ArrayList<Object>();
            Assert.AreEqual(0, array.Size(), "Incorrect arrayList created");

            array = new ArrayList<Object>(0);
            Assert.AreEqual(0, array.Size(), "Incorrect arrayList created");
    
            try
            {
                array = new ArrayList<Object>(-1);
                Assert.Fail("Should throw ArgumentException");
            }
            catch (ArgumentException)
            {
            }
        }

        [Test]
        public void TestConstructorLCollection()
        {
            ArrayList<Object> al = new ArrayList<Object>(Arrays.AsList(objArray));
            Assert.IsTrue(al.Size() == objArray.Length,
                "arrayList created from collection has incorrect size");
            for (int counter = 0; counter < objArray.Length; counter++)
            {
                Assert.IsTrue(al.Get(counter) == objArray[counter],
                    "arrayList created from collection has incorrect elements");
            }
        }

        [Test]
        public void TestConstructorWithConcurrentCollection()
        {
            Collection<String> collection = new ShrinkOnSize(new String[]{"A", "B", "C", "D"});
            ArrayList<String> list = new ArrayList<String>(collection);
            Assert.IsFalse(list.Contains(null));
        }

        [Test]
        public void TestAdd()
        {
            Object o;
            alist.Add(50, o = new Object());
            Assert.IsTrue(alist.Get(50) == o, "Assert.Assert.Failed to add Object");
            Assert.IsTrue(alist.Get(51) == objArray[50] && (alist.Get(52) == objArray[51]),
                          "Assert.Assert.Failed to fix up list after insert");
            Object oldItem = alist.Get(25);
            alist.Add(25, null);
            Assert.IsNull(alist.Get(25), "Should have returned null");
            Assert.IsTrue(alist.Get(26) == oldItem, "Should have returned the old item from slot 25");

            alist.Add(0, o = new Object());
            Assert.AreEqual(alist.Get(0), o, "Assert.Assert.Failed to add Object");
            Assert.AreEqual(alist.Get(1), objArray[0]);
            Assert.AreEqual(alist.Get(2), objArray[1]);

            oldItem = alist.Get(0);
            alist.Add(0, null);
            Assert.IsNull(alist.Get(0), "Should have returned null");
            Assert.AreEqual(alist.Get(1), oldItem,
                            "Should have returned the old item from slot 0");
    
            try
            {
                alist.Add(-1, new Object());
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (IndexOutOfRangeException e)
            {
                Assert.IsNotNull(e.Message);
            }

            try
            {
                alist.Add(-1, null);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (IndexOutOfRangeException e)
            {
                // Expected
                Assert.IsNotNull(e.Message);
            }

            try
            {
                alist.Add(alist.Size() + 1, new Object());
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (IndexOutOfRangeException e)
            {
                Assert.IsNotNull(e.Message);
            }
    
            try
            {
                alist.Add(alist.Size() + 1, null);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (IndexOutOfRangeException e)
            {
                Assert.IsNotNull(e.Message);
            }
        }

        [Test]
        public void TestSize()
        {
            Assert.AreEqual(100, alist.Size(), "Returned incorrect size for exiting list");
            Assert.AreEqual(0, new ArrayList<Object>().Size(), "Returned incorrect size for new list");
        }

        [Test]
        public void TestToString()
        {
            ArrayList<Object> l = new ArrayList<Object>(1);
            l.Add(l);
            String result = l.ToString();
            Assert.IsTrue(result.IndexOf("(this") > -1, "should contain self ref");
        }

        [Test]
        public void TestToArray()
        {
            alist.Set(25, null);
            alist.Set(75, null);
            Object[] obj = alist.ToArray();
            Assert.AreEqual(objArray.Length, obj.Length, "Returned array of incorrect size");

            for (int i = 0; i < obj.Length; i++)
            {
                if ((i == 25) || (i == 75))
                {
                    Assert.IsNull(obj[i], "Should be null at: " + i + " but instead got: " + obj[i]);
                }
                else
                {
                    Assert.IsTrue(obj[i] == objArray[i], "Returned incorrect array: " + i);
                }
            }
        }

        [Test]
        public void TestTrimToSize()
        {
            for (int i = 99; i > 24; i--)
            {
                alist.Remove(i);
            }
            ((ArrayList<Object>) alist).TrimToSize();
            Assert.AreEqual(25, alist.Size(), "Returned incorrect size after trim");
            for (int i = 0; i < alist.Size(); i++)
            {
                Assert.IsTrue(alist.Get(i) == objArray[i], "Trimmed list contained incorrect elements");
            }
            ArrayList<Object> v = new ArrayList<Object>();
            v.Add("a");
            ArrayList<Object> al = new ArrayList<Object>(v);
            Iterator<Object> it = al.Iterator();
            al.TrimToSize();
            try
            {
                it.Next();
                Assert.Fail("should throw a ConcurrentModificationException");
            }
            catch (ConcurrentModificationException)
            {
            }
        }

        [Test]
        public void TestAddAll()
        {
            ArrayList<Object> list = new ArrayList<Object>();
            list.Add("one");
            list.Add("two");
            Assert.AreEqual(2, list.Size());

            list.Remove(0);
            Assert.AreEqual(1, list.Size());

            ArrayList<Object> collection = new ArrayList<Object>();
            collection.Add("1");
            collection.Add("2");
            collection.Add("3");
            Assert.AreEqual(3, collection.Size());
    
            list.AddAll(0, collection);
            Assert.AreEqual(4, list.Size());
    
            list.Remove(0);
            list.Remove(0);
            Assert.AreEqual(2, list.Size());

            collection.Add("4");
            collection.Add("5");
            collection.Add("6");
            collection.Add("7");
            collection.Add("8");
            collection.Add("9");
            collection.Add("10");
            collection.Add("11");
            collection.Add("12");
    
            Assert.AreEqual(12, collection.Size());
    
            list.AddAll(0, collection);
            Assert.AreEqual(14, list.Size());
        }

        [Test]
        public void TestAddAllWithConcurrentCollection()
        {
            ArrayList<String> list = new ArrayList<String>();
            list.AddAll(new ShrinkOnSize(new String[]{"A", "B", "C", "D"}));
            Assert.IsFalse(list.Contains(null));
        }

        [Test]
        public void TestAddAllAtPositionWithConcurrentCollection()
        {
            ArrayList<String> list = new ArrayList<String>(
                    Arrays.AsList(new String[]{"A", "B", "C", "D"}));

            Collection<String> array = new ShrinkOnSize(new String[]{"E", "F", "G", "H"});
            Assert.IsNotNull(array);
            Assert.IsTrue(array.Size() == 4);
            Assert.IsNotNull(array as Collection<String>);
            list.AddAll(3, array);
            Assert.IsFalse(list.Contains(null));
        }

        [Test]
        public void TestOverrideSize()
        {
            ArrayList<Object> testlist = new MockArrayList();
            // though size is overriden, it should passed without exception
            testlist.Add("test_0");
            testlist.Add("test_1");
            testlist.Add("test_2");
            testlist.Add(1, "test_3");
            testlist.Get(1);
            testlist.Remove(2);
            testlist.Set(1, "test_4");
        }

        [Test]
        public void TestSubclassing()
        {
            ArrayListExtend a = new ArrayListExtend();

            for (int i = 0; i < 100; i++)
            {
                a.Add(new Object());
            }
        }

    }
}

