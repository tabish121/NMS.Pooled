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
    public class ConcurrentHashMapTest : ConcurrencyTestCase
    {
        private static ConcurrentHashMap<Object, Object> map5()
        {
            ConcurrentHashMap<Object, Object> map = new ConcurrentHashMap<Object, Object>(5);
            Assert.IsTrue(map.IsEmpty());
            map.Put(one, "A");
            map.Put(two, "B");
            map.Put(three, "C");
            map.Put(four, "D");
            map.Put(five, "E");
            Assert.IsFalse(map.IsEmpty());
            Assert.AreEqual(5, map.Size());
            return map;
        }

        [Test]
        public void TestClear()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            map.Clear();
            Assert.AreEqual(map.Size(), 0);
        }

        [Test]
        public void TestEquals()
        {
            ConcurrentHashMap<Object, Object> map1 = map5();
            ConcurrentHashMap<Object, Object> map2 = map5();
            Assert.AreEqual(map1, map2);
            Assert.AreEqual(map2, map1);
            map1.Clear();
            Assert.IsFalse(map1.Equals(map2));
            Assert.IsFalse(map2.Equals(map1));
        }

        [Test]
        public void TestContainsKey()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.IsTrue(map.ContainsKey(one));
            Assert.IsFalse(map.ContainsKey(zero));
        }

        [Test]
        public void TestContainsValue()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.IsTrue(map.ContainsValue("A"));
            Assert.IsFalse(map.ContainsValue("Z"));
        }

        [Test]
        public void TestGet()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.AreEqual("A", (String)map.Get(one));
            ConcurrentHashMap<Object, Object> empty = new ConcurrentHashMap<Object, Object>();
            Assert.IsNull(map.Get("anything"));
            Assert.IsNull(empty.Get("anything"));
        }
    
        [Test]
        public void TestIsEmpty()
        {
            ConcurrentHashMap<Object, Object> empty = new ConcurrentHashMap<Object, Object>();
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.IsTrue(empty.IsEmpty());
            Assert.IsFalse(map.IsEmpty());
        }

        [Test]
        public void TestKeySet()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Set<Object> s = map.KeySet();
            Assert.AreEqual(5, s.Size());
            Assert.IsTrue(s.Contains(one));
            Assert.IsTrue(s.Contains(two));
            Assert.IsTrue(s.Contains(three));
            Assert.IsTrue(s.Contains(four));
            Assert.IsTrue(s.Contains(five));
        }
    
        [Test]
        public void TestKeySetToArray()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Set<Object> s = map.KeySet();
            Object[] ar = s.ToArray();
            Assert.IsTrue(s.ContainsAll(Arrays.AsList(ar)));
            Assert.AreEqual(5, ar.Length);
            ar[0] = m10;
            Assert.IsFalse(s.ContainsAll(Arrays.AsList(ar)));
        }
    
        [Test]
        public void TestValuesToArray()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Collection<Object> v = map.Values();
            Object[] ar = v.ToArray();
            ArrayList<Object> s = new ArrayList<Object>(Arrays.AsList(ar));
            Assert.AreEqual(5, ar.Length);
            Assert.IsTrue(s.Contains("A"));
            Assert.IsTrue(s.Contains("B"));
            Assert.IsTrue(s.Contains("C"));
            Assert.IsTrue(s.Contains("D"));
            Assert.IsTrue(s.Contains("E"));
        }
    
        [Test]
        public void TestEntrySetToArray()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Set<Entry<Object, Object>> s = map.EntrySet();
            Object[] ar = s.ToArray();
            Assert.AreEqual(5, ar.Length);
            for (int i = 0; i < 5; ++i)
            {
                Assert.IsTrue(map.ContainsKey(((Entry<Object, Object>)(ar[i])).Key));
                Assert.IsTrue(map.ContainsValue(((Entry<Object, Object>)(ar[i])).Value));
            }
        }
    
        [Test]
        public void TestValues()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Collection<Object> s = map.Values();
            Assert.AreEqual(5, s.Size());
            Assert.IsTrue(s.Contains("A"));
            Assert.IsTrue(s.Contains("B"));
            Assert.IsTrue(s.Contains("C"));
            Assert.IsTrue(s.Contains("D"));
            Assert.IsTrue(s.Contains("E"));
        }
    
        [Test]
        public void TestEntrySet()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Set<Entry<Object, Object>> s = map.EntrySet();
            Assert.AreEqual(5, s.Size());
            Iterator<Entry<Object, Object>> it = s.Iterator();
            while (it.HasNext)
            {
                Entry<Object, Object> e = it.Next();
                Assert.IsTrue(
                   (e.Key.Equals(one) && e.Value.Equals("A")) ||
                   (e.Key.Equals(two) && e.Value.Equals("B")) ||
                   (e.Key.Equals(three) && e.Value.Equals("C")) ||
                   (e.Key.Equals(four) && e.Value.Equals("D")) ||
                   (e.Key.Equals(five) && e.Value.Equals("E")));
            }
        }
    
        [Test]
        public void TestPutAll()
        {
            ConcurrentHashMap<Object, Object> empty = new ConcurrentHashMap<Object, Object>();
            ConcurrentHashMap<Object, Object> map = map5();
            empty.PutAll(map);
            Assert.AreEqual(5, empty.Size());
            Assert.IsTrue(empty.ContainsKey(one));
            Assert.IsTrue(empty.ContainsKey(two));
            Assert.IsTrue(empty.ContainsKey(three));
            Assert.IsTrue(empty.ContainsKey(four));
            Assert.IsTrue(empty.ContainsKey(five));
        }
    
        [Test]
        public void TestPutIfAbsent()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            map.PutIfAbsent(six, "Z");
            Assert.IsTrue(map.ContainsKey(six));
        }
    
        [Test]
        public void TestPutIfAbsent2()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.AreEqual("A", map.PutIfAbsent(one, "Z"));
        }
    
        [Test]
        public void TestReplace()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.IsNull(map.Replace(six, "Z"));
            Assert.IsFalse(map.ContainsKey(six));
        }
    
        [Test]
        public void TestReplace2()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.IsNotNull(map.Replace(one, "Z"));
            Assert.AreEqual("Z", map.Get(one));
        }

        [Test]
        public void TestReplaceValue()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.AreEqual("A", map.Get(one));
            Assert.IsFalse(map.Replace(one, "Z", "Z"));
            Assert.AreEqual("A", map.Get(one));
        }
    
        [Test]
        public void TestReplaceValue2()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            Assert.AreEqual("A", map.Get(one));
            Assert.IsTrue(map.Replace(one, "A", "Z"));
            Assert.AreEqual("Z", map.Get(one));
        }
    
        [Test]
        public void TestRemove()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            map.Remove(five);
            Assert.AreEqual(4, map.Size());
            Assert.IsFalse(map.ContainsKey(five));
        }
    
        [Test]
        public void TestRemove2()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            map.Remove(five, "E");
            Assert.AreEqual(4, map.Size());
            Assert.IsFalse(map.ContainsKey(five));
            map.Remove(four, "A");
            Assert.AreEqual(4, map.Size());
            Assert.IsTrue(map.ContainsKey(four));
        }
    
        [Test]
        public void TestSize()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            ConcurrentHashMap<Object, Object> empty = new ConcurrentHashMap<Object, Object>();
            Assert.AreEqual(0, empty.Size());
            Assert.AreEqual(5, map.Size());
        }
    
        [Test]
        public void TestToString()
        {
            ConcurrentHashMap<Object, Object> map = map5();
            String s = map.ToString();
            for (int i = 1; i <= 5; ++i)
            {
                Assert.IsTrue(s.IndexOf(i.ToString()) >= 0);
            }
        }        
    
        [Test]
        public void TestConstructor1()
        {
            try
            {
                new ConcurrentHashMap<Object, Object>(-1,0,1);
                ShouldThrow();
            }
            catch(ArgumentException)
            {
            }
        }
    
        [Test]
        public void TestConstructor2()
        {
            try
            {
                new ConcurrentHashMap<Object, Object>(1,0,-1);
                ShouldThrow();
            }
            catch(ArgumentException)
            {
            }
        }
    
        [Test]
        public void TestConstructor3()
        {
            try
            {
                new ConcurrentHashMap<Object, Object>(-1);
                ShouldThrow();
            }
            catch(ArgumentException)
            {
            }
        }
    
        [Test]
        public void TestGetNullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Get(null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestContainsKey_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.ContainsKey(null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestContainsValue_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.ContainsValue(null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }

        [Test]
        public void TestPut1_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Put(null, "whatever");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestPut2NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Put("whatever", null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestPutIfAbsent1_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.PutIfAbsent(null, "whatever");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestReplace_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Replace(null, "whatever");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestReplaceValue_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Replace(null, one, "whatever");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestPutIfAbsent2_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.PutIfAbsent("whatever", null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestReplace2_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Replace("whatever", null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestReplaceValue2_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Replace("whatever", null, "A");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestReplaceValue3_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Replace("whatever", one, null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestRemove1_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Put("sadsdf", "asdads");
                c.Remove(null);
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestRemove2_NullReferenceException()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Put("sadsdf", "asdads");
                c.Remove(null, "whatever");
                ShouldThrow();
            }
            catch(NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestRemove3()
        {
            try
            {
                ConcurrentHashMap<Object, Object> c = new ConcurrentHashMap<Object, Object>(5);
                c.Put("sadsdf", "asdads");
                Assert.IsFalse(c.Remove("sadsdf", null));
            }
            catch(NullReferenceException)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TestSetValueWriteThrough()
        {
            ConcurrentHashMap<Object, Object> map = new ConcurrentHashMap<Object, Object>(2, 5.0f, 1);
            Assert.IsTrue(map.IsEmpty());
            for (int i = 0; i < 20; i++)
            {
                map.Put(i, i);
            }
            Assert.IsFalse(map.IsEmpty());
            Entry<Object, Object> entry1 = map.EntrySet().Iterator().Next();

            Console.WriteLine("Entry.Key={0}, Entry.Value={1}", entry1.Key, entry1.Value);

            // assert that entry1 is not 16
            Assert.IsTrue(!entry1.Key.Equals(16), "entry is 16, test not valid");

            // remove 16 (a different key) from map
            // which just happens to cause entry1 to be cloned in map
            map.Remove(16);

            Console.WriteLine("Entry.Key={0}, Entry.Value={1}", entry1.Key, entry1.Value);

            entry1.Value = "XYZ";

            Console.WriteLine("Entry.Key={0}, Entry.Value={1}", entry1.Key, entry1.Value);
            
            Assert.IsTrue(map.ContainsValue("XYZ")); // fails
        }
    }
}

