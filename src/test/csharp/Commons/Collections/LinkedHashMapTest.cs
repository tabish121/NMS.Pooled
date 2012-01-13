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
    public class LinkedHashMapTest
    {
        private LinkedHashMap<Object, Object> hm;
        private const int hmSize = 1000;

        private static Object[] objArray;
        private static Object[] objArray2;

        static LinkedHashMapTest()
        {
            objArray = new Object[hmSize];
            objArray2 = new Object[hmSize];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
                objArray2[i] = objArray[i].ToString();
            }
        }

        private class CacheMap : LinkedHashMap<Object, Object>
        {
            protected override bool RemoveEldestEntry(Entry<Object, Object> e)
            {
                return Size() > 5;
            }
        }

        private class MockMapNull : AbstractMap<Object, Object>
        {
            public override Set<Entry<Object, Object>> EntrySet()
            {
                return null;
            }

            public override int Size()
            {
                return 10;
            }
        }

        [Test]
        public void TestConstructor()
        {
            LinkedHashMap<Object, Object> hm2 = new LinkedHashMap<Object, Object>();
            Assert.AreEqual(0, hm2.Size(), "Created incorrect LinkedHashMap");
        }

        [Test]
        public void TestConstructorI()
        {
            LinkedHashMap<Object, Object> hm2 = new LinkedHashMap<Object, Object>(5);
            Assert.AreEqual(0, hm2.Size(), "Created incorrect LinkedHashMap");
            try
            {
                new LinkedHashMap<Object, Object>(-1);
                Assert.Fail("Failed to throw ArgumentException for initial capacity < 0");
            }
            catch (ArgumentException)
            {
            }

            LinkedHashMap<Object, Object> empty = new LinkedHashMap<Object, Object>(0);
            Assert.IsNull(empty.Get("nothing"), "Empty LinkedHashMap access");
            empty.Put("something", "here");
            Assert.IsTrue(empty.Get("something").Equals("here"), "cannot get element");
        }

        [Test]
        public void TestConstructorIF()
        {
            LinkedHashMap<Object, Object> hm2 = new LinkedHashMap<Object, Object>(5, (float) 0.5);
            Assert.AreEqual(0, hm2.Size(), "Created incorrect LinkedHashMap");
            try
            {
                new LinkedHashMap<Object, Object>(0, 0);
                Assert.Fail("Failed to throw ArgumentException for initial load factor <= 0");
            }
            catch (ArgumentException)
            {
            }
            LinkedHashMap<Object, Object> empty = new LinkedHashMap<Object, Object>(0, 0.75f);
            Assert.IsNull(empty.Get("nothing"), "Empty hashtable access");
            empty.Put("something", "here");
            Assert.IsTrue(empty.Get("something").Equals("here"), "cannot get element");
        }

        [Test]
        public void TestConstructorOfMap()
        {
            Map<Object, Object> myMap = new HashMap<Object, Object>();
            for (int counter = 0; counter < hmSize; counter++)
            {
                myMap.Put(objArray2[counter], objArray[counter]);
            }
            LinkedHashMap<Object, Object> hm2 = new LinkedHashMap<Object, Object>(myMap);
            for (int counter = 0; counter < hmSize; counter++)
            {
                Assert.IsTrue(hm.Get(objArray2[counter]) == hm2.Get(objArray2[counter]),
                    "Failed to construct correct LinkedHashMap");
            }
        }

        [Test]
        public void TestGet()
        {
            Assert.IsNull(hm.Get("T"), "Get returned non-null for non existent key");
            hm.Put("T", "HELLO");
            Assert.AreEqual("HELLO", hm.Get("T"), "Get returned incorecct value for existing key");

            LinkedHashMap<Object, Object> m = new LinkedHashMap<Object, Object>();
            m.Put(null, "test");
            Assert.AreEqual("test", m.Get(null), "Failed with null key");
            Assert.IsNull(m.Get(0), "Failed with missing key matching null hash");
        }

        [Test]
        public void TestPut()
        {
            hm.Put("KEY", "VALUE");
            Assert.AreEqual("VALUE", hm.Get("KEY"), "Failed to install key/value pair");

            LinkedHashMap<Object, Object> m = new LinkedHashMap<Object, Object>();
            m.Put(0, "short");
            m.Put(null, "test");
            m.Put(0, "int");
            Assert.AreEqual("int", m.Get(0), "Failed adding to bucket containing null");
            Assert.AreEqual("int", m.Get(0), "Failed adding to bucket containing null2");
        }

        [Test]
        public void TestPutAllInMap()
        {
            LinkedHashMap<Object, Object> hm2 = new LinkedHashMap<Object, Object>();
            hm2.PutAll(hm);
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(hm2.Get(i.ToString()).Equals((i)), "Failed to Clear all elements");
            }
        }

        [Test]
        public void TestPutAllMapWithNull()
        {
            LinkedHashMap<Object, Object> linkedHashMap = new LinkedHashMap<Object, Object>();
            try
            {
                linkedHashMap.PutAll(new MockMapNull());
                Assert.Fail("Should throw NullReferenceException");
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                linkedHashMap = new LinkedHashMap<Object, Object>(new MockMapNull());
                Assert.Fail("Should throw NullReferenceException");
            }
            catch (NullReferenceException)
            {
            }
        }

        [Test]
        public void TestEntrySet()
        {
            Set<Entry<Object, Object>> s = hm.EntrySet();
            Iterator<Entry<Object, Object>> i = s.Iterator();
            Assert.IsTrue(hm.Size() == s.Size(), "Returned set of incorrect Size");
            while (i.HasNext)
            {
                Entry<Object, Object> m = (Entry<Object, Object>) i.Next();
                Assert.IsTrue(hm.ContainsKey(m.Key) && hm.ContainsValue(m.Value), "Returned incorrect entry set");
            }
        }

        [Test]
        public void TestKeySet()
        {
            Set<Object> s = hm.KeySet();
            Assert.IsTrue(s.Size() == hm.Size(), "Returned set of incorrect Size()");
            for (int i = 0; i < objArray.Length; i++)
            {
                Assert.IsTrue(s.Contains(objArray[i].ToString()), "Returned set does not contain all keys");
            }

            LinkedHashMap<Object, Object> m = new LinkedHashMap<Object, Object>();
            m.Put(null, "test");
            Assert.IsTrue(m.KeySet().Contains(null), "Failed with null key");
            Assert.IsNull(m.KeySet().Iterator().Next(), "Failed with null key");

            Map<Object, Object> map = new LinkedHashMap<Object, Object>(101);
            map.Put(1, "1");
            map.Put(102, "102");
            map.Put(203, "203");
            Iterator<Object> it = map.KeySet().Iterator();
            int remove1 = (int) it.Next();
            Assert.IsTrue(it.HasNext);
            it.Remove();
            int remove2 = (int) it.Next();
            it.Remove();
            ArrayList<Object> list = new ArrayList<Object>(Arrays.AsList(new Object[] { 1, 102, 203 }));
            list.Remove((Object) remove1);
            list.Remove((Object) remove2);
            Assert.IsTrue(it.Next().Equals(list.Get(0)), "Wrong result");
            Assert.AreEqual(1, map.Size(), "Wrong Size");
            Assert.IsTrue(map.KeySet().Iterator().Next().Equals(list.Get(0)), "Wrong contents");

            Map<Object, Object> map2 = new LinkedHashMap<Object, Object>(101);
            map2.Put(1, "1");
            map2.Put(4, "4");
            Iterator<Object> it2 = map2.KeySet().Iterator();
            int remove3 = (int) it2.Next();
            int Next;
            if ((int)remove3 == 1)
            {
                Next = 4;
            }
            else
            {
                Next = 1;
            }

            Assert.IsTrue(it2.HasNext);
            it2.Remove();
            Assert.IsTrue(it2.Next().Equals(Next), "Wrong result 2");
            Assert.AreEqual(1, map2.Size(), "Wrong Size 2");
            Assert.IsTrue(map2.KeySet().Iterator().Next().Equals(Next), "Wrong contents 2");
        }

        [Test]
        public void TestValues()
        {
            Collection<Object> c = hm.Values();
            Assert.IsTrue(c.Size() == hm.Size(), "Returned collection of incorrect Size()");
            for (int i = 0; i < objArray.Length; i++)
            {
                Assert.IsTrue(c.Contains(objArray[i]), "Returned collection does not contain all keys");
            }

            LinkedHashMap<Object, Object> myLinkedHashMap = new LinkedHashMap<Object, Object>();
            for (int i = 0; i < 100; i++)
            {
                myLinkedHashMap.Put(objArray2[i], objArray[i]);
            }
            Collection<Object> values = myLinkedHashMap.Values();
//            new Support_UnmodifiableCollectionTest(
//                    "Test Returned Collection From LinkedHashMap.Values()", values)
//                    .runTest();
            values.Remove(0);
            Assert.IsTrue(!myLinkedHashMap.ContainsValue(0),
                    "Removing from the values collection should remove from the original map");
        }

        [Test]
        public void TestRemoveObject()
        {
            int Size = hm.Size();
            int y = 9;
            int x = ((int) hm.Remove(y.ToString()));
            Assert.IsTrue(x.Equals(9), "Remove returned incorrect value");
            Assert.IsNull(hm.Get(9), "Failed to remove given key");
            Assert.IsTrue(hm.Size() == (Size - 1), "Failed to decrement Size");
            Assert.IsNull(hm.Remove("LCLCLC"), "Remove of non-existent key returned non-null");

            LinkedHashMap<Object, Object> m = new LinkedHashMap<Object, Object>();
            m.Put(null, "test");
            Assert.IsNull(m.Remove(0), "Failed with same hash as null");
            Assert.AreEqual("test", m.Remove(null), "Failed with null key");
        }

        [Test]
        public void TestClear()
        {
            hm.Clear();
            Assert.AreEqual(0, hm.Size(), "Clear failed to reset Size");
            for (int i = 0; i < hmSize; i++)
            {
                Assert.IsNull(hm.Get(objArray2[i]), "Failed to Clear all elements");
            }
        }

        [Test]
        public void TestClone()
        {
            LinkedHashMap<Object, Object> hm2 = (LinkedHashMap<Object, Object>) hm.Clone();
            Assert.IsTrue(hm2 != hm, "Clone answered equivalent LinkedHashMap");

            for (int counter = 0; counter < hmSize; counter++)
            {
                Assert.IsTrue(hm.Get(objArray2[counter]) == hm2.Get(objArray2[counter]),
                              "Clone answered unequal LinkedHashMap");
            }

            LinkedHashMap<Object, Object> map = new LinkedHashMap<Object, Object>();
            map.Put("key", "value");

            // get the KeySet() and Values() on the original Map
            Set<Object> keys = map.KeySet();
            Collection<Object> values = map.Values();
            Assert.AreEqual("value", values.Iterator().Next(), "Values() does not work");
            Assert.AreEqual("key", keys.Iterator().Next(), "KeySet() does not work");
            AbstractMap<Object, Object> map2 = (AbstractMap<Object, Object>) map.Clone();
            map2.Put("key", "value2");
            Collection<Object> values2 = map2.Values();
            Assert.IsTrue(values2 != values, "Values() is identical");

            // Values() and KeySet() on the Cloned() map should be different
            Assert.AreEqual("value2", values2.Iterator().Next(), "Values() was not Cloned");
            map2.Clear();
            map2.Put("key2", "value3");
            Set<Object> key2 = map2.KeySet();
            Assert.IsTrue(key2 != keys, "KeySet() is identical");
            Assert.AreEqual("key2", key2.Iterator().Next(), "KeySet() was not Cloned");
        }

        [Test]
        public void TestCloneMock()
        {
            LinkedHashMap<Object, Object> hashMap = new MockMap();
            String value = "value a";
            hashMap.Put("key", value);
            MockMap cloneMap = (MockMap) hashMap.Clone();
            Assert.AreEqual(value, cloneMap.Get("key"));
            Assert.AreEqual(hashMap, cloneMap);
            Assert.AreEqual(1, cloneMap.Num);

            hashMap.Put("key", "value b");
            Assert.IsFalse(hashMap.Equals(cloneMap));
        }

        class MockMap : LinkedHashMap<Object, Object>
        {
            private int num;

            public int Num
            {
                get { return num; }
            }

            public override Object Put(Object k, Object v)
            {
                num++;
                return base.Put(k, v);
            }

            protected override bool RemoveEldestEntry(Entry<Object, Object> e)
            {
                return num > 1;
            }
        }

        [Test]
        public void TestContainsKeyObject()
        {
             Assert.IsTrue(hm.ContainsKey(876.ToString()), "Returned false for valid key");
             Assert.IsTrue(!hm.ContainsKey("KKDKDKD"), "Returned true for invalid key");

             LinkedHashMap<Object, Object> m = new LinkedHashMap<Object, Object>();
             m.Put(null, "test");

             Assert.IsTrue(m.ContainsKey(null), "Failed with null key");
             Assert.IsTrue(!m.ContainsKey(0), "Failed with missing key matching null hash");
        }

        [Test]
        public void TestContainsValueObject()
        {
            Assert.IsTrue(hm.ContainsValue(875), "Returned false for valid value");
            Assert.IsTrue(!hm.ContainsValue(-9), "Returned true for invalid valie");
        }

        [Test]
        public void TestIsEmpty()
        {
             Assert.IsTrue(new LinkedHashMap<Object, Object>().IsEmpty(), "Returned false for new map");
             Assert.IsTrue(!hm.IsEmpty(), "Returned true for non-empty");
        }

        [Test]
        public void TestSize()
        {
            Assert.IsTrue(hm.Size() == (objArray.Length + 2), "Returned incorrect Size");
        }

        [Test]
        public void TestOrderedEntrySet()
        {
            int i;
            int sz = 100;
            LinkedHashMap<Object, Object> lhm = new LinkedHashMap<Object, Object>();
            for (i = 0; i < sz; i++)
            {
                lhm.Put(i, i.ToString());
            }

            Set<Entry<Object, Object>> s1 = lhm.EntrySet();
            Iterator<Entry<Object, Object>> it1 = s1.Iterator();
            Assert.IsTrue(lhm.Size() == s1.Size(), "Returned set of incorrect Size 1");
            for (i = 0; it1.HasNext; i++)
            {
                Entry<Object, Object> m = it1.Next();
                int jj = (int) m.Key;
                Assert.IsTrue(jj == i, "Returned incorrect entry set 1");
            }

            LinkedHashMap<Object, Object> lruhm = new LinkedHashMap<Object, Object>(200, .75f, true);
            for (i = 0; i < sz; i++)
            {
                lruhm.Put(i, i.ToString());
            }

            Set<Entry<Object, Object>> s3 = lruhm.EntrySet();
            Iterator<Entry<Object, Object>> it3 = s3.Iterator();
            Assert.IsTrue(lruhm.Size() == s3.Size(), "Returned set of incorrect Size 2");
            for (i = 0; i < sz && it3.HasNext; i++)
            {
                Entry<Object, Object> m = it3.Next();
                int jj = (int) m.Key;
                Assert.IsTrue(jj == i, "Returned incorrect entry set 2");
            }

            /* fetch the even numbered entries to affect traversal order */
            int p = 0;
            for (i = 0; i < sz; i += 2)
            {
                String ii = (String) lruhm.Get(i);
                p = p + Int32.Parse(ii);
            }
            Assert.AreEqual(2450, p, "invalid sum of even numbers");

            Set<Entry<Object, Object>> s2 = lruhm.EntrySet();
            Iterator<Entry<Object, Object>> it2 = s2.Iterator();
            Assert.IsTrue(lruhm.Size() == s2.Size(), "Returned set of incorrect Size 3");
            for (i = 1; i < sz && it2.HasNext; i += 2)
            {
                Entry<Object, Object> m = (Entry<Object, Object>) it2.Next();
                int jj = (int) m.Key;
                Assert.IsTrue(jj == i, "Returned incorrect entry set 3");
            }

            for (i = 0; i < sz && it2.HasNext; i += 2)
            {
                Entry<Object, Object> m = (Entry<Object, Object>) it2.Next();
                int jj = (int) m.Key;
                Assert.IsTrue(jj == i, "Returned incorrect entry set 4");
            }

            Assert.IsTrue(!it2.HasNext, "Entries left to iterate on");
        }

        [Test]
        public void TestOrderedKeySet()
        {
            int i;
            int sz = 100;
            LinkedHashMap<Object, Object> lhm = new LinkedHashMap<Object, Object>();
            for (i = 0; i < sz; i++)
            {
                lhm.Put(i, i.ToString());
            }

            Set<Object> s1 = lhm.KeySet();
            Iterator<Object> it1 = s1.Iterator();
            Assert.IsTrue(lhm.Size() == s1.Size(), "Returned set of incorrect Size");
            for (i = 0; it1.HasNext; i++)
            {
                int jj = (int) it1.Next();
                Assert.IsTrue(jj == i, "Returned incorrect entry set");
            }

            LinkedHashMap<Object, Object> lruhm = new LinkedHashMap<Object, Object>(200, .75f, true);
            for (i = 0; i < sz; i++)
            {
                lruhm.Put(i, i.ToString());
            }

            Set<Object> s3 = lruhm.KeySet();
            Iterator<Object> it3 = s3.Iterator();
            Assert.IsTrue(lruhm.Size() == s3.Size(), "Returned set of incorrect Size");
            for (i = 0; i < sz && it3.HasNext; i++)
            {
                int jj = (int) it3.Next();
                Assert.IsTrue(jj == i, "Returned incorrect entry set");
            }

            /* fetch the even numbered entries to affect traversal order */
            int p = 0;
            for (i = 0; i < sz; i += 2)
            {
                String ii = (String) lruhm.Get(i);
                p = p + Int32.Parse(ii);
            }

            Assert.AreEqual(2450, p, "invalid sum of even numbers");

            Set<Object> s2 = lruhm.KeySet();
            Iterator<Object> it2 = s2.Iterator();
            Assert.IsTrue(lruhm.Size() == s2.Size(), "Returned set of incorrect Size");
            for (i = 1; i < sz && it2.HasNext; i += 2)
            {
                int jj = (int) it2.Next();
                Assert.IsTrue(jj == i, "Returned incorrect entry set");
            }

            for (i = 0; i < sz && it2.HasNext; i += 2)
            {
                int jj = (int) it2.Next();
                Assert.IsTrue(jj == i, "Returned incorrect entry set");
            }

            Assert.IsTrue(!it2.HasNext, "Entries left to iterate on");
        }

        [Test]
        public void TestOrderedValues()
        {
            int i;
            int sz = 100;
            LinkedHashMap<Object, Object> lhm = new LinkedHashMap<Object, Object>();
            for (i = 0; i < sz; i++)
            {
                lhm.Put(i, (i * 2));
            }

            Collection<Object> s1 = lhm.Values();
            Iterator<Object> it1 = s1.Iterator();
            Assert.IsTrue(lhm.Size() == s1.Size(), "Returned set of incorrect Size 1");
            for (i = 0; it1.HasNext; i++)
            {
                int jj = (int) it1.Next();
                Assert.IsTrue(jj == i * 2, "Returned incorrect entry set 1");
            }

            LinkedHashMap<Object, Object> lruhm = new LinkedHashMap<Object, Object>(200, .75f, true);
            for (i = 0; i < sz; i++)
            {
                lruhm.Put(i, (i * 2));
            }

            Collection<Object> s3 = lruhm.Values();
            Iterator<Object> it3 = s3.Iterator();
            Assert.IsTrue(lruhm.Size() == s3.Size(), "Returned set of incorrect Size");
            for (i = 0; i < sz && it3.HasNext; i++)
            {
                int jj = (int) it3.Next();
                Assert.IsTrue(jj == i * 2, "Returned incorrect entry set");
            }

            // fetch the even numbered entries to affect traversal order
            int p = 0;
            for (i = 0; i < sz; i += 2)
            {
                int ii = (int) lruhm.Get(i);
                p = p + ii;
            }

            Assert.IsTrue(p == 2450 * 2, "invalid sum of even numbers");

            Collection<Object> s2 = lruhm.Values();
            Iterator<Object> it2 = s2.Iterator();
            Assert.IsTrue(lruhm.Size() == s2.Size(), "Returned set of incorrect Size");
            for (i = 1; i < sz && it2.HasNext; i += 2)
            {
                int jj = (int) it2.Next();
                Assert.IsTrue(jj == i * 2, "Returned incorrect entry set");
            }

            for (i = 0; i < sz && it2.HasNext; i += 2)
            {
                int jj = (int) it2.Next();
                Assert.IsTrue(jj == i * 2, "Returned incorrect entry set");
            }

            Assert.IsTrue(!it2.HasNext, "Entries left to iterate on");
        }

        [Test]
        public void TestRemoveEldest()
        {
            int i;
            int sz = 10;
            CacheMap lhm = new CacheMap();
            for (i = 0; i < sz; i++)
            {
                int ii = i;
                lhm.Put(ii, i * 2);
            }

            Collection<Object> s1 = lhm.Values();
            Iterator<Object> it1 = s1.Iterator();
            Assert.IsTrue(lhm.Size() == s1.Size(), "Returned set of incorrect Size 1");
            for (i = 5; it1.HasNext; i++)
            {
                int jj = (int) it1.Next();
                Assert.IsTrue(jj == i * 2, "Returned incorrect entry set 1");
            }
            Assert.IsTrue(!it1.HasNext, "Entries left in map");
        }

        [SetUp]
        public void SetUp()
        {
            hm = new LinkedHashMap<Object, Object>();
            for (int i = 0; i < objArray.Length; i++)
            {
                hm.Put(objArray2[i], objArray[i]);
            }
            hm.Put("test", null);
            hm.Put(null, "test");
        }
    }
}

