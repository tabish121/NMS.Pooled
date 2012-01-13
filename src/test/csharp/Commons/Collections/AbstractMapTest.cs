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
    public class AbstractMapTest
    {
        private static readonly String specialKey = "specialKey";
        private static readonly String specialValue = "specialValue";

        // The impl of MyMap is not realistic, but serves to create a type
        // that uses the default Remove behavior.
        class MyMap : AbstractMap<Object, Object>
        {
            private readonly Set<Entry<Object, Object>> mySet = new HashSet<Entry<Object, Object>>(1);

            private class MyEntry : MapEntry<Object, Object>
            {
                public MyEntry() : base(null, null)
                {
                }

                public override Object Key
                {
                    get { return specialKey; }
                }

                public override Object Value
                {
                    get { return specialValue; }
                    set {}
                }
            }

            public MyMap() : base()
            {
                mySet.Add(new MyEntry());
            }
    
            public override Object Put(Object key, Object value)
            {
                return null;
            }

            public override Set<Entry<Object, Object>> EntrySet()
            {
                return mySet;
            }
        }

        private class MocAbstractMap<K, V> : AbstractMap<K, V> where K : class where V : class
        {
            public override Set<Entry<K, V>> EntrySet()
            {
                Set<Entry<K, V>> xset = new MySet<Entry<K, V>>();
                return xset;
            }

            public class MySet<E> : HashSet<E> where E : class
            {
                public override void Clear()
                {
                    throw new NotSupportedException();
                }
            }
        }

        [Test]
        public void TestKeySet()
        {
            AbstractMap<Object, Object> map1 = new HashMap<Object, Object>(0);
            Assert.AreSame(map1.KeySet(), map1.KeySet(), "HashMap(0)");

            AbstractMap<Object, Object> map2 = new HashMap<Object, Object>(10);
            Assert.AreSame(map2.KeySet(), map2.KeySet(), "HashMap(10)");

            Map<Object, Object> map3 = CollectionUtils.EMPTY_MAP;
            Assert.AreSame(map3.KeySet(), map3.KeySet(), "EMPTY_MAP");

            AbstractMap<Object, Object> map5 = new LinkedHashMap<Object, Object>(122);
            Assert.AreSame(map5.KeySet(), map5.KeySet(), "LinkedHashMap");
        }

        [Test]
        public void TestRemoveObject()
        {
            Object key = new Object();
            Object val = new Object();

            AbstractMap<Object, Object> map1 = new HashMap<Object, Object>(0);
            map1.Put("key", val);
            Assert.AreSame(map1.Remove("key"), val, "HashMap(0)");

            AbstractMap<Object, Object> map5 = new LinkedHashMap<Object, Object>(122);
            map5.Put(key, val);
            Assert.AreSame(map5.Remove(key), val, "LinkedHashMap");

            AbstractMap<Object, Object> aSpecialMap = new MyMap();
            aSpecialMap.Put(specialKey, specialValue);
            Object valueOut = aSpecialMap.Remove(specialKey);
            Assert.AreSame(valueOut, specialValue, "MyMap");
        }

        [Test]
        public void TestClear()
        {
            // normal Clear()
            AbstractMap<Object, Object> map = new HashMap<Object, Object>();
            map.Put(1, 1);
            map.Clear();
            Assert.IsTrue(map.IsEmpty());

            // Special entrySet return a Set with no Clear method.
            AbstractMap<Object, Object> myMap = new MocAbstractMap<Object, Object>();
            try
            {
                myMap.Clear();
                Assert.Fail("Should throw NotSupportedException");
            }
            catch (NotSupportedException)
            {
            }
        }

        [Test]
        public void TestContainsKey()
        {
            AbstractMap<Object, Object> map = new AMT();

            Assert.IsFalse(map.ContainsKey("k"));
            Assert.IsFalse(map.ContainsKey(null));
    
            map.Put("k", "v");
            map.Put("key", null);
            map.Put(null, "value");
            map.Put(null, null);
    
            Assert.IsTrue(map.ContainsKey("k"));
            Assert.IsTrue(map.ContainsKey("key"));
            Assert.IsTrue(map.ContainsKey(null));
        }

        [Test]
        public void TestContainValue()
        {
            AbstractMap<Object, Object> map = new AMT();
    
            Assert.IsFalse(map.ContainsValue("v"));
            Assert.IsFalse(map.ContainsValue(null));

            map.Put("k", "v");
            map.Put("key", null);
            map.Put(null, "value");
    
            Assert.IsTrue(map.ContainsValue("v"));
            Assert.IsTrue(map.ContainsValue("value"));
            Assert.IsTrue(map.ContainsValue(null));
        }

        [Test]
        public void TestGet()
        {
            AbstractMap<Object, Object> map = new AMT();
            Assert.IsNull(map.Get("key"));
            Assert.IsNull(map.Get(null));
    
            map.Put("k", "v");
            map.Put("key", null);
            map.Put(null, "value");

            Assert.AreEqual("v", map.Get("k"));
            Assert.IsNull(map.Get("key"));
            Assert.AreEqual("value", map.Get(null));
        }

        [Test]
        public void TestValues()
        {
            AbstractMap<Object, Object> map1 = new HashMap<Object, Object>(0);
            Assert.AreSame(map1.Values(), map1.Values(), "HashMap(0)");
    
            AbstractMap<Object, Object> map2 = new HashMap<Object, Object>(10);
            Assert.AreSame(map2.Values(), map2.Values(), "HashMap(10)");

            Map<Object, Object> map3 = CollectionUtils.EMPTY_MAP;
            Assert.AreSame(map3.Values(), map3.Values(), "EMPTY_MAP");

            AbstractMap<Object, Object> map5 = new LinkedHashMap<Object, Object>(122);
            Assert.AreSame(map5.Values(), map5.Values(), "IdentityHashMap");
        }

        class MyClonableMap : AbstractMap<Object, Object>, ICloneable
        {
            private Map<Object, Object> map = new HashMap<Object, Object>();

            public override Set<Entry<Object, Object>> EntrySet()
            {
                return map.EntrySet();
            }

            public override Object Put(Object key, Object val)
            {
                return map.Put(key, val);
            }

            public Map<Object, Object> GetMap()
            {
                return map;
            }

            public override object Clone()
            {
                try
                {
                    return (MyClonableMap) base.MemberwiseClone();
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
        }

        [Test]
        public void TestClone()
        {
            MyClonableMap map = new MyClonableMap();
            map.Put("one", "1");
            Entry<Object, Object> entry = (Entry<Object, Object>) map.EntrySet().Iterator().Next();
            Assert.IsTrue(entry.Key.Equals("one") , "entry not added");
            Assert.IsTrue(entry.Value.Equals("1"), "entry not added");
            MyClonableMap mapClone = (MyClonableMap) map.Clone();
            Assert.IsTrue(map.GetMap() == mapClone.GetMap(), "Clone not shallow");
        }

        public class AMT : AbstractMap<Object, Object>
        {
            ArrayList<Object> values = new ArrayList<Object>();
            ArrayList<Object> keys = new ArrayList<Object>();

            class AMTEntry : MapEntry<Object, Object>
            {
                private readonly Object v;
                private readonly Object k;

                public AMTEntry(int index, AMT parent) : base(null, null)
                {
                    this.v = parent.values.Get(index);
                    this.k = parent.keys.Get(index);
                }

                public override Object Key
                {
                    get { return k; }
                }

                public override Object Value
                {
                    get { return v; }
                    set { }
                }
            };

            class AMTSetIterator : Iterator<Entry<Object, Object>>
            {
                private int index = 0;
                private readonly AMT parent;

                public AMTSetIterator(AMT parent)
                {
                    this.parent = parent;
                }

                public bool HasNext
                {
                    get { return index < parent.values.Size(); }
                }

                public Entry<Object, Object> Next()
                {
                    if (index < parent.values.Size())
                    {
                        Entry<Object, Object> me = new AMTEntry(index, parent);
                        index++;
                        return me;
                    }
                    return null;
                }

                public void Remove()
                {
                }
            }

            class AMTSet : AbstractSet<Entry<Object, Object>>
            {
                private readonly AMT parent;

                public AMTSet(AMT parent)
                {
                    this.parent = parent;
                }

                public override Iterator<Entry<Object, Object>> Iterator()
                {
                    return new AMTSetIterator(parent);
                }

                public override int Size()
                {
                    return parent.values.Size();
                }
            }

            public override Set<Entry<Object, Object>> EntrySet()
            {
                return new AMTSet(this);
            }

            public override Object Put(Object k, Object v)
            {
                keys.Add(k);
                values.Add(v);
                return v;
            }
        }

        [Test]
        public void TestPutAllLMap()
        {
            Map<Object, Object> ht = new HashMap<Object, Object>();
            Map<Object, Object> amt = new AMT();
            ht.Put("this", "that");
            amt.PutAll(ht);
            Assert.AreEqual(amt, ht, "Should be equal");
        }

        [Test]
        public void TestEqualsWithNullValues()
        {
            Map<String, String> a = new HashMap<String, String>();
            a.Put("a", null);
            a.Put("b", null);

            Map<String, String> b = new HashMap<String, String>();
            a.Put("c", "cat");
            a.Put("d", "dog");
    
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(b.Equals(a));
        }

        [Test]
        public void TestNullsOnViews()
        {
            Map<String, String> nullHostile = new HashMap<String, String>();
    
            nullHostile.Put("a", "apple");
            TestNullsOnView(nullHostile.EntrySet());

            nullHostile.Put("a", "apple");
            TestNullsOnView(nullHostile.KeySet());

            nullHostile.Put("a", "apple");
            TestNullsOnView(nullHostile.Values());
        }

        private void TestNullsOnView(Collection<String> view)
        {
            try
            {
                Assert.IsFalse(view.Contains(null));
            }
            catch (NullReferenceException)
            {
            }
    
            try
            {
                Assert.IsFalse(view.Remove(null));
            }
            catch (NullReferenceException)
            {
            }

            Set<String> setOfNull = CollectionUtils.Singleton<String>(null);
            Assert.IsFalse(view.Equals(setOfNull));

            try
            {
                Assert.IsFalse(view.RemoveAll(setOfNull));
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                Assert.IsTrue(view.RetainAll(setOfNull));
            }
            catch (NullReferenceException)
            {
            }
        }

        private void TestNullsOnView(Collection<Entry<String, String>> view)
        {
            try
            {
                Assert.IsFalse(view.Contains(null));
            }
            catch (NullReferenceException)
            {
            }
    
            try
            {
                Assert.IsFalse(view.Remove(null));
            }
            catch (NullReferenceException)
            {
            }

            Set<Entry<String, String>> setOfNull = CollectionUtils.Singleton<Entry<String, String>>(null);
            Assert.IsFalse(view.Equals(setOfNull));

            try
            {
                Assert.IsFalse(view.RemoveAll(setOfNull));
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                Assert.IsTrue(view.RetainAll(setOfNull));
            }
            catch (NullReferenceException)
            {
            }
        }

    }
}

