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

//        public void TestRemoveObject()
//        {
//            Object key = new Object();
//            Object value = new Object();
//
//            AbstractMap map1 = new HashMap(0);
//            map1.Put("key", value);
//            Assert.AreSame("HashMap(0)", map1.Remove("key"), value);
//
//            AbstractMap map4 = new IdentityHashMap(1);
//            map4.Put(key, value);
//            Assert.AreSame("IdentityHashMap", map4.Remove(key), value);
//
//            AbstractMap map5 = new LinkedHashMap(122);
//            map5.Put(key, value);
//            Assert.AreSame("LinkedHashMap", map5.Remove(key), value);
//    
//            AbstractMap map6 = new TreeMap(new Comparator() {
//                // Bogus comparator
//                public int compare(Object object1, Object object2) {
//                    return 0;
//                }
//            });
//            map6.Put(key, value);
//            Assert.AreSame("TreeMap", map6.Remove(key), value);
//    
//            AbstractMap map7 = new WeakHashMap();
//            map7.Put(key, value);
//            Assert.AreSame("WeakHashMap", map7.Remove(key), value);
//    
//            AbstractMap aSpecialMap = new MyMap();
//            aSpecialMap.Put(specialKey, specialValue);
//            Object valueOut = aSpecialMap.Remove(specialKey);
//            Assert.AreSame("MyMap", valueOut, specialValue);
//        }

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

//        public void TestContainsKey()
//        {
//            AbstractMap map = new AMT();
//    
//            Assert.IsFalse(map.ContainsKey("k"));
//            Assert.IsFalse(map.ContainsKey(null));
//    
//            map.Put("k", "v");
//            map.Put("key", null);
//            map.Put(null, "value");
//            map.Put(null, null);
//    
//            Assert.IsTrue(map.ContainsKey("k"));
//            Assert.IsTrue(map.ContainsKey("key"));
//            Assert.IsTrue(map.ContainsKey(null));
//        }
//    
//        /**
//         * @tests java.util.AbstractMap#ContainsValue(Object)
//         */
//        public void test_containValue()
//        {
//            AbstractMap map = new AMT();
//    
//            Assert.IsFalse(map.ContainsValue("v"));
//            Assert.IsFalse(map.ContainsValue(null));
//
//            map.Put("k", "v");
//            map.Put("key", null);
//            map.Put(null, "value");
//    
//            Assert.IsTrue(map.ContainsValue("v"));
//            Assert.IsTrue(map.ContainsValue("value"));
//            Assert.IsTrue(map.ContainsValue(null));
//        }
//    
//        /**
//         * @tests java.util.AbstractMap#Get(Object)
//         */
//        public void test_Get()
//        {
//            AbstractMap map = new AMT();
//            assertNull(map.Get("key"));
//            assertNull(map.Get(null));
//    
//            map.Put("k", "v");
//            map.Put("key", null);
//            map.Put(null, "value");
//    
//            Assert.AreEqual("v", map.Get("k"));
//            assertNull(map.Get("key"));
//            Assert.AreEqual("value", map.Get(null));
//        }

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

//        /**
//         * @tests java.util.AbstractMap#Clone()
//         */
//        public void test_Clone()
//        {
//            class MyMap : AbstractMap, Cloneable
//            {
//                private Map map = new HashMap();
//
//                public Set entrySet() {
//                    return map.entrySet();
//                }
//    
//                public Object Put(Object key, Object value) {
//                    return map.Put(key, value);
//                }
//    
//                public Map GetMap() {
//                    return map;
//                }
//
//                public Object Clone() {
//                    try {
//                        return super.Clone();
//                    } catch (CloneNotSupportedException e) {
//                        return null;
//                    }
//                }
//            }
//            ;
//            MyMap map = new MyMap();
//            map.Put("one", "1");
//            Map.Entry entry = (Map.Entry) map.entrySet().iterator().next();
//            Assert.IsTrue("entry not added", entry.GetKey() == "one"
//                    && entry.GetValue() == "1");
//            MyMap mapClone = (MyMap) map.Clone();
//            Assert.IsTrue("Clone not shallow", map.GetMap() == mapClone.GetMap());
//        }
//    
//        public class AMT extends AbstractMap {
//    
//            // Very crude AbstractMap implementation
//            Vector Values = new Vector();
//    
//            Vector keys = new Vector();
//    
//            public Set entrySet() {
//                return new AbstractSet() {
//                    public Iterator iterator() {
//                        return new Iterator() {
//                            int index = 0;
//    
//                            public bool hasNext() {
//                                return index < Values.size();
//                            }
//    
//                            public Object next() {
//                                if (index < Values.size()) {
//                                    Map.Entry me = new Map.Entry() {
//                                        Object v = Values.elementAt(index);
//    
//                                        Object k = keys.elementAt(index);
//
//                                        public Object GetKey() {
//                                            return k;
//                                        }
//
//                                        public Object GetValue() {
//                                            return v;
//                                        }
//
//                                        public Object setValue(Object value) {
//                                            return null;
//                                        }
//                                    };
//                                    index++;
//                                    return me;
//                                }
//                                return null;
//                            }
//
//                            public void Remove() {
//                            }
//                        };
//                    }
//    
//                    public int size() {
//                        return Values.size();
//                    }
//                };
//            }
//    
//            public Object Put(Object k, Object v) {
//                keys.add(k);
//                Values.add(v);
//                return v;
//            }
//        }
//    
//        /**
//         * @tests {@link java.util.AbstractMap#PutAll(Map)}
//         */
//        public void test_PutAllLMap() {
//            Hashtable ht = new Hashtable();
//            AMT amt = new AMT();
//            ht.Put("this", "that");
//            amt.PutAll(ht);
//            Assert.AreEqual("Should be equal", amt, ht);
//        }

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

//        public void testNullsOnViews() {
//            Map<String, String> nullHostile = new Hashtable<String, String>();
//    
//            nullHostile.Put("a", "apple");
//            testNullsOnView(nullHostile.entrySet());
//
//            nullHostile.Put("a", "apple");
//            testNullsOnView(nullHostile.KeySet());
//    
//            nullHostile.Put("a", "apple");
//            testNullsOnView(nullHostile.Values());
//        }

        private void testNullsOnView(Collection<Object> view)
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

            Set<Object> setOfNull = CollectionUtils.Singleton<Object>(null);
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

