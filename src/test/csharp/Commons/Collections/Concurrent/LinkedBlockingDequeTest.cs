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
    public class LinkedBlockingDequeTest : ConcurrencyTestCase
    {
        private LinkedBlockingDeque<String> PopulatedDeque(int n)
        {
            LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(n);
            Assert.IsTrue(q.IsEmpty());
            for(int i = 0; i < n; i++)
            {
                Assert.IsTrue(q.Offer(i.ToString()));
            }
            Assert.IsFalse(q.IsEmpty());
            Assert.AreEqual(0, q.RemainingCapacity());
            Assert.AreEqual(n, q.Size());
            return q;
        }

        [Test]
        public void TestConstructor1()
        {
            Assert.AreEqual(SIZE, new LinkedBlockingDeque<String>(SIZE).RemainingCapacity());
            Assert.AreEqual(Int32.MaxValue, new LinkedBlockingDeque<String>().RemainingCapacity());
        }

        [Test]
        public void TestConstructor2()
        {
            try
            {
                new LinkedBlockingDeque<String>(0);
                ShouldThrow();
            }
            catch (ArgumentException) {}
        }

        [Test]
        public void TestConstructor3() {
            try
            {
                Collection<String> nullCollection = null;
                new LinkedBlockingDeque<String>(nullCollection);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestConstructor4()
        {
            try
            {
                ArrayList<String> strings = new ArrayList<String>();
                strings.Add(null);
                new LinkedBlockingDeque<String>(strings);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestConstructor5()
        {
            try
            {
                ArrayList<String> strings = new ArrayList<String>();
                for (int i = 0; i < SIZE-1; ++i)
                {
                    strings.Add(i.ToString());
                }
                strings.Add(null);
                new LinkedBlockingDeque<String>(strings);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestConstructor6()
        {
            ArrayList<String> strings = new ArrayList<String>();
            for (int i = 0; i < SIZE; ++i)
            {
                strings.Add(i.ToString());
            }
            LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(strings);
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(strings.Get(i), q.Poll());
            }
        }

        [Test]
        public void TestEmptyFull()
        {
            LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(2);
            Assert.IsTrue(q.IsEmpty());
            Assert.AreEqual(2, q.RemainingCapacity(), "should have room for 2");
            q.Add(one);
            Assert.IsFalse(q.IsEmpty());
            q.Add(two);
            Assert.IsFalse(q.IsEmpty());
            Assert.AreEqual(0, q.RemainingCapacity());
            Assert.IsFalse(q.Offer(three));
        }

        [Test]
        public void TestRemainingCapacity()
        {
            LinkedBlockingDeque<String> q = PopulatedDeque(SIZE);
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(i, q.RemainingCapacity());
                Assert.AreEqual(SIZE-i, q.Size());
                q.Remove();
            }
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(SIZE-i, q.RemainingCapacity());
                Assert.AreEqual(i, q.Size());
                q.Add(i.ToString());
            }
        }

        [Test]
        public void TestOfferNull()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>();
                q.Offer(null);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestAddNull()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>();
                q.Add(null);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestOffer()
        {
            LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(1);
            Assert.IsTrue(q.Offer(zero));
            Assert.IsFalse(q.Offer(one));
        }

        [Test]
        public void TestAdd()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
                for (int i = 0; i < SIZE; ++i)
                {
                    Assert.IsTrue(q.Add(i.ToString()));
                }
                Assert.AreEqual(0, q.RemainingCapacity());
                q.Add(SIZE.ToString());
            }
            catch (IllegalStateException)
            {
            }
        }
    
        [Test]
        public void TestAddAll1()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(1);
                q.AddAll(null);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestAddAllSelf()
        {
            try
            {
                LinkedBlockingDeque<String> q = PopulatedDeque(SIZE);
                q.AddAll(q);
                ShouldThrow();
            }
            catch (ArgumentException) {}
        }
    
        [Test]
        public void TestAddAll2()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
                ArrayList<String> strings = new ArrayList<String>();
                strings.Add(null);
                strings.Add(null);
                strings.Add(null);
                q.AddAll(strings);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestAddAll3()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
                ArrayList<String> strings = new ArrayList<String>();
                for (int i = 0; i < SIZE-1; ++i)
                {
                    strings.Add(i.ToString());
                }
                strings.Add(null);
                q.AddAll(strings);
                ShouldThrow();
            }
            catch (NullReferenceException) {}
        }

        [Test]
        public void TestAddAll4()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(1);
                ArrayList<String> strings = new ArrayList<String>();
                for (int i = 0; i < SIZE-1; ++i)
                {
                    strings.Add(i.ToString());
                }
                q.AddAll(strings);
                ShouldThrow();
            }
            catch (IllegalStateException) {}
        }

        [Test]
        public void TestAddAll5()
        {
            ArrayList<String> empty = new ArrayList<String>();
            ArrayList<String> strings = new ArrayList<String>(SIZE);

            for (int i = 0; i < SIZE; ++i)
            {
                strings.Add(i.ToString());
            }
            LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
            Assert.IsFalse(q.AddAll(empty));
            Assert.IsTrue(q.AddAll(strings));
            for (int i = 0; i < SIZE; ++i)
            {
                Assert.AreEqual(strings.Get(i), q.Poll());
            }
        }
    
        [Test]
        public void TestPutNull()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
                q.Put(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception)
            {
                UnexpectedException();
            }
        }

        [Test]
        public void TestPut()
        {
            try
            {
                LinkedBlockingDeque<String> q = new LinkedBlockingDeque<String>(SIZE);
                for (int i = 0; i < SIZE; ++i)
                {
                     q.Put(i.ToString());
                     Assert.IsTrue(q.Contains(i.ToString()));
                 }
                 Assert.AreEqual(0, q.RemainingCapacity());
            }
            catch (Exception)
            {
                UnexpectedException();
            }
        }
    }
}

