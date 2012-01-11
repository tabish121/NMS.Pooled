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
    public class HashMapTest
    {
        private class MockMap : AbstractMap<Object, Object>
        {
            public override Set<Entry<Object, Object>> EntrySet()
            {
                return null;
            }

            public override int Size()
            {
                return 0;
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
    
        private interface MockInterface
        {
            String MockMethod();
        }
    
        private class MockClass : MockInterface
        {
            public String MockMethod()
            {
                return "This is a MockClass";
            }
        }

        private HashMap<Object, Object> hm;
    
        private static readonly int hmSize = 1000;
    
        private static Object[] objArray;

        static Object[] objArray2;

        static HashMapTest()
        {
            objArray = new Object[hmSize];
            objArray2 = new Object[hmSize];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
                objArray2[i] = objArray[i].ToString();
            }
        }

        [Test]
        public void TestConstructor()
        {
            // Test for method java.util.HashMap()
           // new Support_MapTest2(new HashMap()).runTest();

            HashMap<Object, Object> hm2 = new HashMap<Object, Object>();
            Assert.AreEqual(0, hm2.Size(), "Created incorrect HashMap");
        }

        [Test]
        public void TestConstructorI()
        {
            HashMap<Object, Object> hm2 = new HashMap<Object, Object>(5);
            Assert.AreEqual(0, hm2.Size(), "Created incorrect HashMap");
            try
            {
                 new HashMap<Object, Object>(-1);
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail("Assert.Failed to throw ArgumentException for initial capacity < 0");
        
            HashMap<Object, Object> empty = new HashMap<Object, Object>(0);
            Assert.IsNull(empty.Get("nothing"), "Empty hashmap access");
            empty.Put("something", "here");
            Assert.IsTrue(empty.Get("something").Equals("here"),  "cannot get element");
        }

        [Test]
        public void TestConstructorIF()
        {
            HashMap<Object, Object> hm2 = new HashMap<Object, Object>(5, (float) 0.5);
            Assert.AreEqual(0, hm2.Size(), "Created incorrect HashMap");
            try
            {
                new HashMap<Object, Object>(0, 0);
            }
            catch (ArgumentException)
            {
                return;
            }
            Assert.Fail("Assert.Failed to throw ArgumentException for initial load factor <= 0");

            HashMap<Object, Object> empty = new HashMap<Object, Object>(0, 0.75f);
            Assert.IsNull(empty.Get("nothing"), "Empty hashtable access");
            empty.Put("something", "here");
            Assert.IsTrue(empty.Get("something").Equals("here"), "cannot get element");
        }

        [Test]
        public void TestConstructorMap()
        {
            Map<Object, Object> myMap = new LinkedHashMap<Object, Object>();
            for (int counter = 0; counter < hmSize; counter++)
            {
                myMap.Put(objArray2[counter], objArray[counter]);
            }
            HashMap<Object, Object> hm2 = new HashMap<Object, Object>(myMap);
            for (int counter = 0; counter < hmSize; counter++)
            {
                Assert.IsTrue(hm.Get(objArray2[counter]) == hm2.Get(objArray2[counter]), "Assert.Failed to construct correct HashMap");
            }

            try
            {
                Map<Object, Object> mockMap = new MockMap();
                hm = new HashMap<Object, Object>(mockMap);
                Assert.Fail("Should throw NullReferenceException");
            }
            catch (NullReferenceException)
            {
                   //empty
            }

            HashMap<Object, Object> map = new HashMap<Object, Object>();
            map.Put("a", "a");
            SubMap<Object, Object> map2 = new SubMap<Object, Object>(map);
            Assert.IsTrue(map2.ContainsKey("a"));
            Assert.IsTrue(map2.ContainsValue("a"));
        }

        [Test]
        public void TestClear()
        {
            hm.Clear();
            Assert.AreEqual(0, hm.Size(), "Clear Assert.Failed to reset Size");
            for (int i = 0; i < hmSize; i++)
            {
                Assert.IsNull(hm.Get(objArray2[i]), "Assert.Failed to Clear all elements");
            }

            // Check Clear on a large loaded map of Int32 keys
            HashMap<Object, String> map = new HashMap<Object, String>();
            for (int i = -32767; i < 32768; i++)
            {
                map.Put(i, "foobar");
            }
            map.Clear();
            Assert.AreEqual(0, hm.Size(), "Assert.Failed to reset Size on large integer map");
            for (int i = -32767; i < 32768; i++)
            {
                Assert.IsNull(map.Get(i), "Assert.Failed to Clear integer map values");
            }
        }

//        [Test]
//        public void TestClone()
//        {
//            // Test for method java.lang.Object java.util.HashMap.Clone()
//            HashMap<Object, Object> hm2 = (HashMap<Object, Object>) hm.Clone();
//            Assert.IsTrue("Clone answered equivalent HashMap", hm2 != hm);
//            for (int counter = 0; counter < hmSize; counter++)
//            {
//                 Assert.IsTrue(hm.Get(objArray2[counter]) == hm2.Get(objArray2[counter]), "Clone answered unequal HashMap");
//            }
//
//            HashMap<Object, Object> map = new HashMap<Object, Object>();
//            map.Put("key", "value");
//            // get the KeySet() and Values() on the original Map
//            Set<Object> keys = map.KeySet();
//            Collection<Object> values = map.Values();
//            Assert.AreEqual("Values() does not work", "value", values.Iterator().Next());
//            Assert.AreEqual("KeySet() does not work", "key", keys.Iterator().Next());
//            AbstractMap<Object, Object> map2 = (AbstractMap<Object, Object>) map.Clone();
//            map2.Put("key", "value2");
//            Collection<Object> values2 = map2.Values();
//            Assert.IsTrue("Values() is identical", values2 != values);
//            // Values() and KeySet() on the Cloned() map should be different
//            Assert.AreEqual("Values() was not Cloned", "value2", values2.Iterator().Next());
//            map2.Clear();
//            map2.Put("key2", "value3");
//            Set<Object> key2 = map2.KeySet();
//            Assert.IsTrue("KeySet() is identical", key2 != keys);
//            Assert.AreEqual("KeySet() was not Cloned", "key2", key2.Iterator().Next());
//
//            // regresion test for HARMONY-4603
//            HashMap<Object, Object> hashmap = new HashMap<Object, Object>();
//            MockClonable mock = new MockClonable(1);
//            hashmap.Put(1, mock);
//            Assert.AreEqual(1, ((MockClonable) hashmap.Get(1)).i);
//            HashMap<Object, Object> hm3 = (HashMap<Object, Object>)hashmap.Clone();
//            Assert.AreEqual(1, ((MockClonable) hm3.Get(1)).i);
//            mock.i = 0;
//            Assert.AreEqual(0, ((MockClonable) hashmap.Get(1)).i);
//            Assert.AreEqual(0, ((MockClonable) hm3.Get(1)).i);
//         }

        [Test]
        public void TestContainsKey()
        {
            Assert.IsTrue(hm.ContainsKey(876.ToString()), "Returned false for valid key");
            Assert.IsTrue(!hm.ContainsKey("KKDKDKD"), "Returned true for invalid key");

            HashMap<Object, Object> m = new HashMap<Object, Object>();
            m.Put(null, "test");
            Assert.IsTrue(m.ContainsKey(null), "Assert.Failed with null key");
            Assert.IsTrue(!m.ContainsKey(0), "Assert.Failed with missing key matching null hash");
        }

        [Test]
        public void TestContainsValue()
        {
            Assert.IsTrue(hm.ContainsValue(875), "Returned false for valid value");
            Assert.IsTrue(!hm.ContainsValue(-9), "Returned true for invalid valie");
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

            Iterator<Entry<Object, Object>> iter = s.Iterator();
            s.Remove(iter.Next());
            Assert.AreEqual(1001, s.Size());
        }

        [Test]
        public void TestGet()
        {
             Assert.IsNull(hm.Get("T"), "Get returned non-null for non existent key");
             hm.Put("T", "HELLO");
             Assert.AreEqual("HELLO", hm.Get("T"), "Get returned incorrect value for existing key");

             HashMap<Object, Object> m = new HashMap<Object, Object>();
             m.Put(null, "test");
             Assert.AreEqual("test", m.Get(null), "Assert.Failed with null key");
             Assert.IsNull(m.Get(0), "Assert.Failed with missing key matching null hash");

             ReusableKey k = new ReusableKey();
             HashMap<Object, Object> map = new HashMap<Object, Object>();
             k.Key = 1;
             map.Put(k, "value1");

             k.Key = 18;
             Assert.IsNull(map.Get(k));

             k.Key = 17;
             Assert.IsNull(map.Get(k));
         }

        [Test]
        public void TestIsEmpty()
        {
            Assert.IsTrue(new HashMap<Object, Object>().IsEmpty(), "Returned false for new map");
            Assert.IsTrue(!hm.IsEmpty(), "Returned true for non-empty");
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

            HashMap<Object, Object> m = new HashMap<Object, Object>();
            m.Put(null, "test");
            Assert.IsTrue(m.KeySet().Contains(null), "Assert.Failed with null key");
            Assert.IsNull(m.KeySet().Iterator().Next(), "Assert.Failed with null key");

            Map<Object, Object> map = new HashMap<Object, Object>(101);
            map.Put(1, "1");
            map.Put(102, "102");
            map.Put(203, "203");
            Iterator<Object> it = map.KeySet().Iterator();
            Object remove1 = it.Next();
            Assert.IsTrue(it.HasNext);
            it.Remove();
            Object remove2 = (Int32) it.Next();
            it.Remove();
            ArrayList<Object> list = new ArrayList<Object>(Arrays.AsList(new Object[] {1, 102, 203 }));
            list.Remove(remove1);
            list.Remove(remove2);
            Assert.IsTrue(it.Next().Equals(list.Get(0)), "Wrong result");
            Assert.AreEqual(1, map.Size(), "Wrong Size");
            Assert.IsTrue(map.KeySet().Iterator().Next().Equals(list.Get(0)), "Wrong contents");

            Map<Object, Object> map2 = new HashMap<Object, Object>(101);
            map2.Put(1, "1");
            map2.Put(4, "4");
            Iterator<Object> it2 = map2.KeySet().Iterator();
            Object remove3 = it2.Next();
            Object Next;

            if ((int) remove3 == 1)
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
        public void TestPut2()
        {
            hm.Put("KEY", "VALUE");
            Assert.AreEqual("VALUE", hm.Get("KEY"), "Assert.Failed to install key/value pair");

            HashMap<Object,Object> m = new HashMap<Object,Object>();
            m.Put(0, "short");
            m.Put(null, "test");
            m.Put(0, "int");
            Assert.AreEqual("int", m.Get(0), "Assert.Failed adding to bucket containing null");
            Assert.AreEqual("int", m.Get(0), "Assert.Failed adding to bucket containing null2");

            // Check my actual key instance is returned
            HashMap<Object, String> map = new HashMap<Object, String>();
            for (int i = -32767; i < 32768; i++)
            {
                map.Put(i, "foobar");
            }
            Object myKey = 0;
            // Put a new value at the old key position
            map.Put(myKey, "myValue");
            Assert.IsTrue(map.ContainsKey(myKey));
            Assert.AreEqual("myValue", map.Get(myKey));
            bool found = false;
            for (Iterator<Object> itr = map.KeySet().Iterator(); itr.HasNext;)
            {
                Object key = itr.Next();
                if (found = key == myKey)
                {
                    break;
                }
            }

            Assert.IsFalse(found, "Should not find new key instance in hashmap");

            // Add a new key instance and check it is returned
            Assert.IsNotNull(map.Remove(myKey));
            map.Put(myKey, "myValue");
            Assert.IsTrue(map.ContainsKey(myKey));
            Assert.AreEqual(map.Get(myKey), "myValue");
            for (Iterator<Object> itr = map.KeySet().Iterator(); itr.HasNext;)
            {
                Object key = itr.Next();
                if (found = key == myKey)
                {
                    break;
                }
            }
            Assert.IsTrue(found, "Did not find new key instance in hashmap");

            // Ensure keys with identical hashcode are stored separately
            HashMap<Object,Object> objmap = new HashMap<Object, Object>();
            for (int i = 0; i < 32768; i++)
            {
                objmap.Put(i, "foobar");
            }
            // Put non-equal object with same hashcode
            MyKey aKey = new MyKey();
            Assert.IsNull(objmap.Put(aKey, "value"));
            Assert.IsNull(objmap.Remove(new MyKey()));
            Assert.AreEqual(objmap.Get(0), "foobar");
            Assert.AreEqual(objmap.Get(aKey), "value");
        }

        private class MyKey
        {
            public MyKey()
            {
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void TestPutAll()
        {
            HashMap<Object, Object> hm2 = new HashMap<Object, Object>();
            hm2.PutAll(hm);

            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(hm2.Get(i.ToString()).Equals((i)), "Assert.Failed to Clear all elements");
            }

            Map<Object, Object> mockMap = new MockMap();
            hm2 = new HashMap<Object, Object>();
            hm2.PutAll(mockMap);
            Assert.AreEqual(0, hm2.Size(), "Size should be 0");
        }

        [Test]
        public void TestPutAllMockMapNull()
        {
            HashMap<Object, Object> hashMap = new HashMap<Object, Object>();
            try
            {
                hashMap.PutAll(new MockMapNull());
                Assert.Fail("Should throw NullReferenceException");
            }
            catch (NullReferenceException)
            {
                // expected.
            }

            try
            {
                hashMap = new HashMap<Object, Object>(new MockMapNull());
                Assert.Fail("Should throw NullReferenceException");
            }
            catch (NullReferenceException)
            {
                // expected.
            }
        }

        [Test]
        public void TestRemove()
        {
            int Size = hm.Size();
            Object y = 9;
            Object x = hm.Remove(y.ToString());
            Assert.IsTrue(x.Equals(9), "Remove returned incorrect value");
            Assert.IsNull(hm.Get(9), "Assert.Failed to Remove given key");
            Assert.IsTrue(hm.Size() == (Size - 1), "Assert.Failed to decrement Size");
            Assert.IsNull(hm.Remove("LCLCLC"), "Remove of non-existent key returned non-null");

            HashMap<Object, Object> m = new HashMap<Object, Object>();
            m.Put(null, "test");
            Assert.IsNull(m.Remove(0), "Assert.Failed with same hash as null");
            Assert.AreEqual("test", m.Remove(null), "Assert.Failed with null key");

            HashMap<Object, Object> map = new HashMap<Object, Object>();
            for (int i = 0; i < 32768; i++)
            {
                map.Put(i, "const");
            }
            Object[] values = new Object[32768];
            for (int i = 0; i < 32768; i++)
            {
                values[i] = new Object();
                map.Put(i, values[i]);
            }
            for (int i = 32767; i >= 0; i--)
            {
                Assert.AreEqual(values[i], map.Remove(i), "Assert.Failed to Remove same value");
            }

            // Ensure keys with identical hashcode are Removed properly
            map = new HashMap<Object, Object>();
            for (int i = -32767; i < 32768; i++)
            {
                map.Put(i, "foobar");
            }
            // Remove non equal object with same hashcode
            Assert.IsNull(map.Remove(new MyKey()));
            Assert.AreEqual("foobar", map.Get(0));
            map.Remove(0);
            Assert.IsNull(map.Get(0));
        }

        [Test]
        public void TestRehash()
        {
            // This map should rehash on adding the ninth element.
            HashMap<MyKey, Object> hm = new HashMap<MyKey, Object>(10, 0.5f);

            // Ordered set of keys.
            MyKey[] keyOrder = new MyKey[9];
            for (int i = 0; i < keyOrder.Length; i++)
            {
                keyOrder[i] = new MyKey();
            }

            // Store eight elements
            for (int i = 0; i < 8; i++)
            {
                hm.Put(keyOrder[i], i);
            }
            // Check expected ordering (inverse of adding order)
            MyKey[] returnedKeys = hm.KeySet().ToArray();
            for (int i = 0; i < 8; i++) {

                Assert.AreSame(keyOrder[i], returnedKeys[7 - i]);
            }

            // The Next Put causes a rehash
            hm.Put(keyOrder[8], 8);
            // Check expected new ordering (adding order)
            returnedKeys = hm.KeySet().ToArray();
            for (int i = 0; i < 9; i++)
            {
                Assert.AreSame(keyOrder[i], returnedKeys[i]);
            }
        }

        [Test]
        public void TestSize()
        {
            Assert.IsTrue(hm.Size() == (objArray.Length + 2), "Returned incorrect Size");
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

            HashMap<Object, Object> myHashMap = new HashMap<Object, Object>();
            for (int i = 0; i < 100; i++)
            {
                myHashMap.Put(objArray2[i], objArray[i]);
            }

            Collection<Object> values = myHashMap.Values();
//            new Support_UnmodifiableCollectionTest(
//                 "Test Returned Collection From HashMap.Values()", values)
//                 .runTest();
            values.Remove(0);
            Assert.IsTrue(!myHashMap.ContainsValue(0),
                "Removing from the values collection should Remove from the original map");
        }

        [Test]
        public void TestToString()
        {
            HashMap<Object, Object> m = new HashMap<Object, Object>();
            m.Put(m, m);
            String result = m.ToString();
            Assert.IsTrue(result.IndexOf("(this") > -1, "should contain self ref");
        }

        private class ReusableKey
        {
            private int key = 0;

            public int Key
            {
                get { return key; }
                set { this.key = value; }
            }

            public override int GetHashCode()
            {
                return key;
            }

            public override bool Equals(Object o)
            {
                if (o == this)
                {
                    return true;
                }

                if (!(o is ReusableKey))
                {
                    return false;
                }

                return key == ((ReusableKey) o).key;
            }
        }

        [Test]
        public void TestMapEntryHashCode()
        {
            HashMap<Object, Object> map = new HashMap<Object, Object>(10);
            Object key = 1;
            Object val = 2;
            map.Put(key, val);
            int expected = key.GetHashCode() ^ val.GetHashCode();
            Assert.AreEqual(expected, map.GetHashCode());
            key = 4;
            val = 8;
            map.Put(key, val);
            expected += key.GetHashCode() ^ val.GetHashCode();
            Assert.AreEqual(expected, map.GetHashCode());
        }

        class MockClonable : ICloneable
        {
            public int i;

            public MockClonable(int i)
            {
                this.i = i;
            }

            public Object Clone()
            {
                return new MockClonable(i);
            }
        }

        [Test]
        public void TestEntrySet2()
        {
            HashMap<Object, Object> map = new HashMap<Object, Object>();
            map.Put(1, "ONE");

            Set<Entry<Object, Object>> EntrySet = map.EntrySet();
            Iterator<Entry<Object, Object>> e = EntrySet.Iterator();
            Object real = e.Next();
            Entry<Object, Object> copyEntry = new MockEntry();
            Assert.AreEqual(real, copyEntry);
            Assert.IsTrue(EntrySet.Contains(copyEntry));

            EntrySet.Remove(copyEntry);
            Assert.IsFalse(EntrySet.Contains(copyEntry));
        }

        private class MockEntry : MapEntry<Object, Object>
        {
            public MockEntry() : base(1)
            {
            }

            public override Object Value
            {
                get { return "ONE"; }
                set { }
            }
        }

        [SetUp]
        public void SetUp()
        {
            hm = new HashMap<object, Object>();
            for (int i = 0; i < objArray.Length; i++)
            {
                hm.Put(objArray2[i], objArray[i]);
            }
            hm.Put("test", null);
            hm.Put(null, "test");
        }

        class SubMap<K, V> : HashMap<K, V> where K : class where V : class
        {
            public SubMap(Map<K, V> m) : base(m)
            {
            }

            public override V Put(K key, V val)
            {
                throw new NotSupportedException();
            }
        }
    }
}

