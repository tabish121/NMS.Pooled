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
using System.Threading;

using NUnit.Framework;

using Apache.NMS.Pooled.Commons.Collections;

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    [TestFixture]
    public class TestGenericObjectPool : TestBaseObjectPool
    {
        protected GenericObjectPool<Object> pool = null;

        protected override ObjectPool<Object> MakeEmptyPool(int mincap)
        {
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(new SimpleFactory());
            pool.MaxTotal = mincap;
            pool.MaxIdle = mincap;
            return pool;
        }

        protected override ObjectPool<Object> MakeEmptyPool(PoolableObjectFactory<Object> factory)
        {
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            return pool;
        }

        protected override Object GetNthObject(int n)
        {
            return n.ToString();
        }

        [SetUp]
        public void SetUp()
        {
            pool = new GenericObjectPool<Object>(new SimpleFactory());
        }
    
        [TearDown]
        public void TearDown()
        {
            pool.Clear();
            pool.Close();
            pool = null;
        }

        [Test, Timeout(60000)]
        public void TestWhenExhaustedFail()
        {
            pool.MaxTotal = 1;
            pool.BlockWhenExhausted = false;
            Object obj1 = pool.BorrowObject();
            Assert.IsNotNull(obj1);

            try
            {
                pool.BorrowObject();
                Assert.Fail("Expected NoSuchElementException");
            }
            catch(NoSuchElementException)
            {
            }

            pool.ReturnObject(obj1);
            Assert.AreEqual(1, pool.IdleCount);
            pool.Close();
        }

        [Test, Timeout(60000)]
        public void TestWhenExhaustedBlock()
        {
            pool.MaxTotal = 1;
            pool.BlockWhenExhausted = true;
            pool.MaxWait = 10L;
            Object obj1 = pool.BorrowObject();
            Assert.IsNotNull(obj1);
            try
            {
                pool.BorrowObject();
                Assert.Fail("Expected NoSuchElementException");
            }
            catch(NoSuchElementException)
            {
            }
            pool.ReturnObject(obj1);
            pool.Close();
        }

        [Test, Timeout(60000)]
        public void TestEvictWhileEmpty()
        {
            pool.Evict();
            pool.Evict();
            pool.Close();
        }

        /*
         * Tests AddObject contention between EnsureMinIdle triggered by
         * the Evictor with minIdle > 0 and BorrowObject.
         */
        [Test, Timeout(60000)]
        public void TestEvictAddObjects()
        {
            SimpleFactory factory = new SimpleFactory();
            factory.SetMakeLatency(300);
            factory.SetMaxTotal(2);
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.MaxTotal = 2;
            pool.MinIdle = 1;
            pool.BorrowObject(); // numActive = 1, numIdle = 0
            // Create a test thread that will run once and try a borrow after
            // 150ms fixed delay
            TestThread borrower = new TestThread(pool, 1, 150, false);
            Thread borrowerThread = new Thread(borrower.Run);
            // Set Evictor to run in 100 ms - will create idle instance
            pool.TimeBetweenEvictionRunsMillis = 100;
            borrowerThread.Start();  // Off to the races
            borrowerThread.Join();
            Assert.IsTrue(!borrower.Failed);
            pool.Close();
        }

        [Test, Timeout(60000)]
        public void TestEvictLIFO()
        {
            CheckEvict(true);
        }

        [Test, Timeout(60000)]
        public void TestEvictFIFO()
        {
            CheckEvict(false);
        }
    
        public void CheckEvict(bool lifo)
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);

            pool.SoftMinEvictableIdleTimeMillis = 10;
            pool.MinIdle = 2;
            pool.TestWhileIdle = true;
            pool.Lifo = lifo;
            PoolUtils.PreFill(pool, 5);
            pool.Evict();
            factory.SetEvenValid(false);
            factory.SetOddValid(false);
            factory.SetThrowExceptionOnActivate(true);
            pool.Evict();
            PoolUtils.PreFill(pool, 5);
            factory.SetThrowExceptionOnActivate(false);
            factory.SetThrowExceptionOnSuspend(true);
            pool.Evict();
            factory.SetThrowExceptionOnSuspend(false);
            factory.SetEvenValid(true);
            factory.SetOddValid(true);
            Thread.Sleep(125);
            pool.Evict();

            Assert.AreEqual(2, pool.IdleCount);

            pool.Close();
        }

        /*
         * Test to make sure Evictor visits least recently used objects first, regardless of FIFO/LIFO
         */
        [Test, Timeout(60000)]
        public void TestEvictionOrder()
        {
            CheckEvictionOrder(false);
            CheckEvictionOrder(true);
        }

        private void CheckEvictionOrder(bool lifo)
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.NumTestsPerEvictionRun = 2;
            pool.MinEvictableIdleTimeMillis = 100;
            pool.Lifo = lifo;

            for (int i = 0; i < 5; i++)
            {
                pool.AddObject();
                Thread.Sleep(100);
            }

            // Order, oldest to youngest, is "0", "1", ...,"4"
            pool.Evict(); // Should Evict "0" and "1"
            Object obj = pool.BorrowObject();
            Assert.IsTrue(!obj.Equals("0"), "oldest not Evicted");
            Assert.IsTrue(!obj.Equals("1"), "second oldest not Evicted");
            // 2 should be next out for FIFO, 4 for LIFO
            Assert.AreEqual(lifo ? "4" : "2" , obj, "Wrong instance returned");
            
            // Two Eviction runs in sequence
            factory = new SimpleFactory();
            pool = new GenericObjectPool<Object>(factory);
            pool.NumTestsPerEvictionRun = 2;
            pool.MinEvictableIdleTimeMillis = 100;
            pool.Lifo = lifo;

            for (int i = 0; i < 5; i++)
            {
                pool.AddObject();
                Thread.Sleep(100);
            }

            pool.Evict(); // Should Evict "0" and "1"
            pool.Evict(); // Should Evict "2" and "3"
            obj = pool.BorrowObject();
            Assert.AreEqual("4", obj, "Wrong instance remaining in pool");
        }

        [Test, Timeout(60000)]
        public void TestExceptionOnPassivateDuringReturn()
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            Object obj = pool.BorrowObject();
            factory.SetThrowExceptionOnSuspend(true);
            pool.ReturnObject(obj);
            Assert.AreEqual(0,pool.IdleCount);
            pool.Close();
        }

        [Test, Timeout(60000)]
        public void TestExceptionOnDestroyDuringBorrow()
        {
            SimpleFactory factory = new SimpleFactory();
            factory.SetThrowExceptionOnDestroy(true);
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.TestOnBorrow = true;
            pool.BorrowObject();
            factory.SetValid(false); // Make validation fail on next borrow attempt
            try
            {
                pool.BorrowObject();
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
            }

            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
        }

        [Test, Timeout(60000)]
        public void TestExceptionOnDestroyDuringReturn()
        {
            SimpleFactory factory = new SimpleFactory();
            factory.SetThrowExceptionOnDestroy(true);
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.TestOnReturn = true;
            Object obj1 = pool.BorrowObject();
            pool.BorrowObject();
            factory.SetValid(false); // Make validation fail
            pool.ReturnObject(obj1);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
        }

        [Test, Timeout(60000)]
        public void TestExceptionOnActivateDuringBorrow()
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            Object obj1 = pool.BorrowObject();
            Object obj2 = pool.BorrowObject();
            pool.ReturnObject(obj1);
            pool.ReturnObject(obj2);
            factory.SetThrowExceptionOnActivate(true);
            factory.SetEvenValid(false);

            // Activation will now throw every other time
            // First attempt throws, but loop continues and second succeeds
            Object obj = pool.BorrowObject();
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
    
            pool.ReturnObject(obj);
            factory.SetValid(false);
            // Validation will now fail on activation when BorrowObject returns
            // an idle instance, and then when attempting to create a new instance
            try
            {
                obj1 = pool.BorrowObject();
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
            }
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
        }

        [Test, Timeout(60000)]
        public void TestNegativeMaxTotal()
        {
            pool.MaxTotal = -1;
            pool.BlockWhenExhausted = false;
            Object obj = pool.BorrowObject();
            Assert.AreEqual(GetNthObject(0),obj);
            pool.ReturnObject(obj);
        }

        [Test, Timeout(60000)]
        public void TestMaxIdle()
        {
            pool.MaxTotal = 100;
            pool.MaxIdle = 8;
            Object[] active = new Object[100];
            for(int i=0;i<100;i++)
            {
                active[i] = pool.BorrowObject();
            }
            Assert.AreEqual(100, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
            for(int i=0;i<100;i++)
            {
                pool.ReturnObject(active[i]);
                Assert.AreEqual(99 - i,pool.ActiveCount);
                Assert.AreEqual((i < 8 ? i+1 : 8),pool.IdleCount);
            }
        }

        [Test, Timeout(60000)]
        public void TestMaxIdleZero()
        {
            pool.MaxTotal = 100;
            pool.MaxIdle = 0;
            Object[] active = new Object[100];
            for(int i=0;i<100;i++)
            {
                active[i] = pool.BorrowObject();
            }
            Assert.AreEqual(100,pool.ActiveCount);
            Assert.AreEqual(0,pool.IdleCount);
            for(int i=0;i<100;i++)
            {
                pool.ReturnObject(active[i]);
                Assert.AreEqual(99 - i,pool.ActiveCount);
                Assert.AreEqual(0, pool.IdleCount);
            }
        }
    
        [Test, Timeout(60000)]
        public void TestMaxTotal()
        {
            pool.MaxTotal = 3;
            pool.BlockWhenExhausted = false;

            pool.BorrowObject();
            pool.BorrowObject();
            pool.BorrowObject();
            try
            {
                pool.BorrowObject();
                Assert.Fail("Expected NoSuchElementException");
            }
            catch(NoSuchElementException)
            {
            }
        }

        [Test, Timeout(60000)]
        public void TestTimeoutNoLeak()
        {
            pool.MaxTotal = 2;
            pool.MaxWait = 10;
            pool.BlockWhenExhausted = true;
            Object obj = pool.BorrowObject();
            Object obj2 = pool.BorrowObject();
            try
            {
                pool.BorrowObject();
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
            }

            pool.ReturnObject(obj2);
            pool.ReturnObject(obj);
    
            obj = pool.BorrowObject();
            obj2 = pool.BorrowObject();
        }
    
        [Test, Timeout(60000)]
        public void TestMaxTotalZero()
        {
            pool.MaxTotal = 0;
            pool.BlockWhenExhausted = false;
    
            try
            {
                pool.BorrowObject();
                Assert.Fail("Expected NoSuchElementException");
            }
            catch(NoSuchElementException)
            {
            }
        }

        [Test, Timeout(60000)]
        public void TestMaxTotalUnderLoad()
        {
            // Config
            int numThreads = 199; // And main thread makes a round 200.
            int numIter = 20;
            int delay = 25;
            int maxTotal = 10;
    
            SimpleFactory factory = new SimpleFactory();
            factory.SetMaxTotal(maxTotal);
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.MaxTotal = maxTotal;
            pool.BlockWhenExhausted = true;
            pool.TimeBetweenEvictionRunsMillis = -1;
    
            // Start threads to borrow objects
            TestThread[] threads = new TestThread[numThreads];
            for(int i=0;i<numThreads;i++)
            {
                // Factor of 2 on iterations so main thread does work whilst other
                // threads are running. Factor of 2 on delay so average delay for
                // other threads == actual delay for main thread
                threads[i] = new TestThread(pool, numIter * 2, delay * 2);
                Thread t = new Thread(threads[i].Run);
                t.Start();
            }

            // Give the threads a chance to start doing some work
            try
            {
                Thread.Sleep(5000);
            }
            catch(ThreadInterruptedException)
            {
            }
            
            for (int i = 0; i < numIter; i++)
            {
                Object obj = null;
                try
                {
                    try
                    {
                        Thread.Sleep(delay);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }

                    obj = pool.BorrowObject();

                    // Under load, observed _numActive > _maxTotal
                    if (pool.ActiveCount > pool.MaxTotal)
                    {
                        throw new IllegalStateException("Too many active objects");
                    }

                    try
                    {
                        Thread.Sleep(delay);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }
                }
                catch (Exception)
                {
                    Assert.Fail("Exception on borrow");
                }
                finally
                {
                    if (obj != null)
                    {
                        try
                        {
                            pool.ReturnObject(obj);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
    
            for (int i = 0; i < numThreads; i++)
            {
                while (!(threads[i]).Complete)
                {
                    try
                    {
                        Thread.Sleep(500);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }
                }

                if(threads[i].Failed)
                {
                    Assert.Fail("Thread "+i+" failed: "+threads[i].Error.ToString());
                }
            }
        }

        [Test, Timeout(60000)]
        public void TestSettersAndGetters()
        {
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(new SimpleFactory());
            {
                pool.MaxTotal = 123;
                Assert.AreEqual(123, pool.MaxTotal);
            }
            {
                pool.MaxIdle = 12;
                Assert.AreEqual(12, pool.MaxIdle);
            }
            {
                pool.MaxWait = 1234L;
                Assert.AreEqual(1234L, pool.MaxWait);
            }
            {
                pool.MinEvictableIdleTimeMillis = 12345L;
                Assert.AreEqual(12345L, pool.MinEvictableIdleTimeMillis);
            }
            {
                pool.NumTestsPerEvictionRun = 11;
                Assert.AreEqual(11, pool.NumTestsPerEvictionRun);
            }
            {
                pool.TestOnBorrow = true;
                Assert.IsTrue(pool.TestOnBorrow);
                pool.TestOnBorrow = false;
                Assert.IsTrue(!pool.TestOnBorrow);
            }
            {
                pool.TestOnReturn = true;
                Assert.IsTrue(pool.TestOnReturn);
                pool.TestOnReturn = false;
                Assert.IsTrue(!pool.TestOnReturn);
            }
            {
                pool.TestWhileIdle = true;
                Assert.IsTrue(pool.TestWhileIdle);
                pool.TestWhileIdle = false;
                Assert.IsTrue(!pool.TestWhileIdle);
            }
            {
                pool.TimeBetweenEvictionRunsMillis = 11235L;
                Assert.AreEqual(11235L,pool.TimeBetweenEvictionRunsMillis);
            }
            {
                pool.SoftMinEvictableIdleTimeMillis = 12135L;
                Assert.AreEqual(12135L,pool.SoftMinEvictableIdleTimeMillis);
            }
            {
                pool.BlockWhenExhausted = true;
                Assert.AreEqual(true, pool.BlockWhenExhausted);
                pool.BlockWhenExhausted = false;
                Assert.AreEqual(false, pool.BlockWhenExhausted);
            }
        }
        
        [Test, Timeout(60000)]
        public void TestDefaultConfiguration()
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            AssertConfiguration(new GenericObjectPoolConfig(), pool);
        }

        [Test, Timeout(60000)]
        public void TestSetConfig()
        {
            GenericObjectPoolConfig expected = new GenericObjectPoolConfig();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(new SimpleFactory());
            AssertConfiguration(expected, pool);

            expected.MaxTotal = 2;
            expected.MaxIdle = 3;
            expected.MaxWait = 5L;
            expected.MinEvictableIdleTimeMillis = 7L;
            expected.NumTestsPerEvictionRun = 9;
            expected.TestOnBorrow = true;
            expected.TestOnReturn = true;
            expected.TestWhileIdle = true;
            expected.TimeBetweenEvictionRunsMillis = 11L;
            expected.BlockWhenExhausted = false;

            pool.Config = expected;

            AssertConfiguration(expected, pool);
        }

        [Test, Timeout(60000)]
        public void TestStartAndStopEvictor()
        {
            // set up pool without Evictor
            pool.MaxIdle = 6;
            pool.MaxTotal = 6;
            pool.NumTestsPerEvictionRun = 6;
            pool.MinEvictableIdleTimeMillis = 100L;
    
            for(int j = 0; j < 2; j++)
            {
                // populate the pool
                {
                    Object[] active = new Object[6];
                    for (int i = 0; i < 6; i++)
                    {
                        active[i] = pool.BorrowObject();
                    }

                    for(int i=0;i<6;i++)
                    {
                        pool.ReturnObject(active[i]);
                    }
                }
        
                // note that it stays populated
                Assert.AreEqual(6, pool.IdleCount, "Should have 6 idle");
        
                // start the Evictor
                pool.TimeBetweenEvictionRunsMillis = 50L;
                
                // wait a second (well, .2 seconds)
                try
                {
                    Thread.Sleep(200);
                }
                catch(ThreadInterruptedException)
                {
                }
                
                // assert that the Evictor has cleared out the pool
                Assert.AreEqual(0, pool.IdleCount, "Should have 0 idle");

                // stop the Evictor
// TODO                pool.StartEvictor(0L);
            }
        }
    
        [Test, Timeout(60000)]
        public void TestEvictionWithNegativeNumTests()
        {
            // when numTestsPerEvictionRun is negative, it represents a fraction of the idle objects to test
            pool.MaxIdle = 6;
            pool.MaxTotal = 6;
            pool.NumTestsPerEvictionRun = -2;
            pool.MinEvictableIdleTimeMillis = 50L;
            pool.TimeBetweenEvictionRunsMillis = 100L;
    
            Object[] active = new Object[6];
            for(int i=0;i<6;i++)
            {
                active[i] = pool.BorrowObject();
            }
            for(int i=0;i<6;i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(100);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount <= 6, "Should at most 6 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(100);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount <= 3, "Should at most 3 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(100);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount <= 2, "Should be at most 2 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(100);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.AreEqual(0, pool.IdleCount, "Should be zero idle, found " + pool.IdleCount);
        }
    
        [Test, Timeout(60000)]
        public void TestEviction()
        {
            pool.MaxIdle = 500;
            pool.MaxTotal = 500;
            pool.NumTestsPerEvictionRun = 100;
            pool.MinEvictableIdleTimeMillis = 250L;
            pool.TimeBetweenEvictionRunsMillis = 500;
            pool.TestWhileIdle = true;

            Object[] active = new Object[500];
            for(int i=0;i<500;i++)
            {
                active[i] = pool.BorrowObject();
            }
            for(int i=0;i<500;i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(1000);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 500, "Should be less than 500 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 400, "Should be less than 400 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 300, "Should be less than 300 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 200, "Should be less than 200 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 100, "Should be less than 100 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.AreEqual(0, pool.IdleCount, "Should be zero idle, found " + pool.IdleCount);
    
            for(int i=0;i<500;i++)
            {
                active[i] = pool.BorrowObject();
            }
            for(int i=0;i<500;i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(1000);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 500, "Should be less than 500 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 400, "Should be less than 400 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 300, "Should be less than 300 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 200, "Should be less than 200 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount < 100, "Should be less than 100 idle, found " + pool.IdleCount);
            try
            {
                Thread.Sleep(600);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.AreEqual(0,pool.IdleCount, "Should be zero idle, found " + pool.IdleCount);
        }

        class TimeTest : BasePoolableObjectFactory<TimeTest>
        {
            private readonly DateTime createTime;

            public TimeTest()
            {
                createTime = DateTime.Now;
            }

            public override TimeTest CreateObject()
            {
                return new TimeTest();
            }

            public DateTime CreateTime
            {
                get { return createTime; }
            }
        }

        [Test, Timeout(60000)]
        public void TestEvictionSoftMinIdle()
        {
            GenericObjectPool<TimeTest> pool =
                new GenericObjectPool<TimeTest>(new TimeTest());

            pool.MaxIdle = 5;
            pool.MaxTotal = 5;
            pool.NumTestsPerEvictionRun = 5;
            pool.MinEvictableIdleTimeMillis =3000;
            pool.SoftMinEvictableIdleTimeMillis = 1000;
            pool.MinIdle = 2;

            TimeTest[] active = new TimeTest[5];
            DateTime[] creationTime = new DateTime[5] ;
            for(int i=0;i<5;i++)
            {
                active[i] = pool.BorrowObject();
                creationTime[i] = active[i].CreateTime;
            }
            
            for(int i=0;i<5;i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            // Soft Evict all but minIdle(2)
            Thread.Sleep(1500);
            pool.Evict();
            Assert.AreEqual(2, pool.IdleCount, "Idle count different than expected.");
    
            // Hard Evict the rest.
            Thread.Sleep(2000);
            pool.Evict();
            Assert.AreEqual(0, pool.IdleCount, "Idle count different than expected.");
        }

        private class InvalidFactory : BasePoolableObjectFactory<Object>
        {
            public override Object CreateObject()
            {
                return new Object();
            }

            public override bool ValidateObject(Object obj)
            {
                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException)
                {
                }
                return false;
            }
        }

        private class EvictThread
        {
            private readonly GenericObjectPool<Object> pool;

            public EvictThread(GenericObjectPool<Object> pool)
            {
                this.pool = pool;
            }

            public void Run()
            {
                try
                {
                    pool.Evict();
                }
                catch (Exception)
                {
                }
            }
        };

        [Test, Timeout(60000)]
        public void TestEvictionInvalid()
        {
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(new InvalidFactory());

            pool.MaxIdle = 1;
            pool.MaxTotal = 1;
            pool.TestOnBorrow = false;
            pool.TestOnReturn = false;
            pool.TestWhileIdle = true;
            pool.MinEvictableIdleTimeMillis = 100000;
            pool.NumTestsPerEvictionRun = 1;

            Object p = pool.BorrowObject();
            pool.ReturnObject(p);
            
            // Run Eviction in a separate thread
            EvictThread evictor = new EvictThread(pool);
            Thread t = new Thread(evictor.Run);
            t.Start();
    
            // Sleep to make sure Evictor has started
            Thread.Sleep(300);
            
            try
            {
                p = pool.BorrowObject(1);
            }
            catch (NoSuchElementException)
            {
            }
    
            // Make sure Evictor has finished
            Thread.Sleep(1000);
            
            // Should have an empty pool
            Assert.AreEqual(0, pool.IdleCount, "Idle count different than expected.");
            Assert.AreEqual(0, pool.ActiveCount, "Total count different than expected.");
        }
    
        [Test, Timeout(60000)]
        public void TestMinIdle()
        {
            pool.MaxIdle = 500;
            pool.MinIdle = 5;
            pool.MaxTotal = 10;
            pool.NumTestsPerEvictionRun = 0;
            pool.MinEvictableIdleTimeMillis = 50L;
            pool.TimeBetweenEvictionRunsMillis = 100L;
            pool.TestWhileIdle = true;
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}

            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);

            Object[] active = new Object[5];
            active[0] = pool.BorrowObject();
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}

            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);
    
            for(int i=1 ; i<5 ; i++)
            {
                active[i] = pool.BorrowObject();
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);
    
            for(int i=0 ; i<5 ; i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 10, "Should be 10 idle, found " + pool.IdleCount);
        }
    
        [Test, Timeout(60000)]
        public void TestMinIdleMaxTotal()
        {
            pool.MaxIdle = 500;
            pool.MinIdle = 5;
            pool.MaxTotal = 10;
            pool.NumTestsPerEvictionRun = 0;
            pool.MinEvictableIdleTimeMillis = 50L;
            pool.TimeBetweenEvictionRunsMillis = 100L;
            pool.TestWhileIdle = true;

            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}

            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);
    
            Object[] active = new Object[10];
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);
    
            for(int i=0 ; i<5 ; i++)
            {
                active[i] = pool.BorrowObject();
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 5, "Should be 5 idle, found " + pool.IdleCount);
    
            for(int i=0 ; i<5 ; i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 10, "Should be 10 idle, found " + pool.IdleCount);
    
            for(int i=0 ; i<10 ; i++)
            {
                active[i] = pool.BorrowObject();
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 0, "Should be 0 idle, found " + pool.IdleCount);
    
            for(int i=0 ; i<10 ; i++)
            {
                pool.ReturnObject(active[i]);
            }
    
            try
            {
                Thread.Sleep(150);
            }
            catch(ThreadInterruptedException)
            {}
            Assert.IsTrue(pool.IdleCount == 10, "Should be 10 idle, found " + pool.IdleCount);
        }

        /*
         * Kicks off <numThreads> test threads, each of which will go through
         * <iterations> borrow-return cycles with random delay times <= delay
         * in between.
         */
        public void RunTestThreads(int numThreads, int iterations, int delay)
        {
            TestThread[] threads = new TestThread[numThreads];
            for(int i=0;i<numThreads;i++)
            {
                threads[i] = new TestThread(pool,iterations,delay);
                Thread t = new Thread(threads[i].Run);
                t.Start();
            }

            for(int i=0;i<numThreads;i++)
            {
                while(!(threads[i]).Complete)
                {
                    try
                    {
                        Thread.Sleep(500);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }
                }

                if(threads[i].Failed)
                {
                    Assert.Fail("Thread "+i+" failed: "+threads[i].Error.ToString());
                }
            }
        }

        [Test, Timeout(60000)]
        public void TestThreaded1()
        {
            pool.MaxTotal = 15;
            pool.MaxIdle = 15;
            pool.MaxWait = 1000L;
            RunTestThreads(20, 100, 50);
        }
    
        /*
         * Verifies that maxTotal is not exceeded when factory destroyObject
         * has high latency, testOnReturn is set and there is high incidence of
         * validation failures. 
         */
        [Test, Timeout(60000)]
        public void TestMaxTotalInvariant()
        {
            int maxTotal = 15;
            SimpleFactory factory = new SimpleFactory();
            factory.SetEvenValid(false);     // Every other validation fails
            factory.SetDestroyLatency(100);  // Destroy takes 100 ms
            factory.SetMaxTotal(maxTotal); // (makes - destroys) bound
            factory.SetValidationEnabled(true);
            pool = new GenericObjectPool<Object>(factory);
            pool.MaxTotal = maxTotal;
            pool.MaxIdle = -1;
            pool.TestOnReturn = true;
            pool.MaxWait = 1000L;
            RunTestThreads(5, 10, 50);
        }
    
        [Test, Timeout(60000)]
        public void TestConcurrentBorrowAndEvict()
        {
            pool.MaxTotal = 1;
            pool.AddObject();

            for (int i=0; i<5000; i++)
            {
                ConcurrentBorrowAndEvictThread oneRun = new ConcurrentBorrowAndEvictThread(pool, true);
                ConcurrentBorrowAndEvictThread twoRun = new ConcurrentBorrowAndEvictThread(pool, false);

                Thread one = new Thread(oneRun.Run);
                Thread two = new Thread(twoRun.Run);

                one.Start();
                two.Start();
                one.Join();
                two.Join();

                pool.ReturnObject(oneRun.obj);
            }
        }

        private class ConcurrentBorrowAndEvictThread
        {
            private bool borrow;
            public Object obj;
            private readonly GenericObjectPool<Object> pool;

            public ConcurrentBorrowAndEvictThread(GenericObjectPool<Object> pool, bool borrow)
            {
                this.borrow = borrow;
                this.pool = pool;
            }
    
            public void Run()
            {
                try
                {
                    if (borrow)
                    {
                        obj = pool.BorrowObject();
                    }
                    else
                    {
                        pool.Evict();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        [Test, Timeout(60000)]
        public void TestFIFO()
        {
            Object o = null;
            pool.Lifo = false;
            pool.AddObject(); // "0"
            pool.AddObject(); // "1"
            pool.AddObject(); // "2"
            Assert.AreEqual("0", pool.BorrowObject(), "Oldest");
            Assert.AreEqual("1", pool.BorrowObject(), "Middle");
            Assert.AreEqual("2", pool.BorrowObject(), "Youngest");
            o = pool.BorrowObject();
            Assert.AreEqual("3", o, "new-3");
            pool.ReturnObject(o);
            Assert.AreEqual(o, pool.BorrowObject(), "returned-3");
            Assert.AreEqual("4", pool.BorrowObject(), "new-4");
        }
    
        [Test, Timeout(60000)]
        public void TestLIFO()
        {
            Object o = null;
            pool.Lifo = true;
            pool.AddObject(); // "0"
            pool.AddObject(); // "1"
            pool.AddObject(); // "2"
            Assert.AreEqual("2", pool.BorrowObject(), "Youngest");
            Assert.AreEqual("1", pool.BorrowObject(), "Middle");
            Assert.AreEqual("0", pool.BorrowObject(), "Oldest");
            o = pool.BorrowObject();
            Assert.AreEqual("3", o, "new-3");
            pool.ReturnObject(o);
            Assert.AreEqual(o, pool.BorrowObject(), "returned-3");
            Assert.AreEqual("4", pool.BorrowObject(), "new-4");
        }
    
        [Test, Timeout(60000)]
        public void TestAddObject()
        {
            Assert.AreEqual(0, pool.IdleCount, "should be zero idle");
            pool.AddObject();
            Assert.AreEqual(1, pool.IdleCount, "should be one idle");
            Assert.AreEqual(0, pool.ActiveCount, "should be zero active");
            Object obj = pool.BorrowObject();
            Assert.AreEqual(0, pool.IdleCount, "should be zero idle");
            Assert.AreEqual(1, pool.ActiveCount, "should be one active");
            pool.ReturnObject(obj);
            Assert.AreEqual(1, pool.IdleCount, "should be one idle");
            Assert.AreEqual(0, pool.ActiveCount, "should be zero active");
        }

        /*
         * Note: This test relies on timing for correct execution. There *should* be
         * enough margin for this to work correctly on most (all?) systems but be
         * aware of this if you see a failure of this test.
         */
        [Test, Timeout(60000)]
        public void TestBorrowObjectFairness()
        {
            // Config
            int numThreads = 30;
            int maxTotal = 10;
    
            pool.MaxTotal = maxTotal;
            pool.BlockWhenExhausted = true;
            pool.TimeBetweenEvictionRunsMillis = -1;

            // Start threads to borrow objects
            TestThread[] threads = new TestThread[numThreads];
            for(int i=0;i<numThreads;i++)
            {
                threads[i] = new TestThread(pool, 1, 2000, false, (i % maxTotal).ToString());
                Thread t = new Thread(threads[i].Run);
                t.Start();
                // Short delay to ensure threads start in correct order
                try
                {
                    Thread.Sleep(50);
                }
                catch (ThreadInterruptedException e)
                {
                    Assert.Fail(e.ToString());
                }
            }
    
            // Wait for threads to finish
            for(int i=0;i<numThreads;i++)
            {
                while(!(threads[i]).Complete)
                {
                    try
                    {
                        Thread.Sleep(500);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }
                }

                if(threads[i].Failed)
                {
                    Assert.Fail("Thread "+i+" failed: "+threads[i].Error.ToString());
                }
            }
        }
        
        /**
         * On first borrow, first object fails validation, second object is OK.
         * Subsequent borrows are OK. This was POOL-152.
         */
        [Test, Timeout(60000)]
        public void TestBrokenFactoryShouldNotBlockPool()
        {
            int maxTotal = 1;
            
            SimpleFactory factory = new SimpleFactory();
            factory.SetMaxTotal(maxTotal);
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);
            pool.MaxTotal = maxTotal;
            pool.BlockWhenExhausted = true;
            pool.TestOnBorrow = true;
            
            // First borrow object will need to create a new object which will fail
            // validation.
            Object obj = null;
            Exception ex = null;
            factory.SetValid(false);
            try
            {
                obj = pool.BorrowObject();
            }
            catch (Exception e)
            {
                ex = e;
            }

            // Failure expected
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex is NoSuchElementException);
            Assert.IsNull(obj);
    
            // Configure factory to create valid objects so subsequent borrows work
            factory.SetValid(true);
            
            // Subsequent borrows should be OK
            try
            {
                obj = pool.BorrowObject();
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            Assert.IsNotNull(obj);

            try
            {
                pool.ReturnObject(obj);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }
    
        /*
         * Very simple test thread that just tries to borrow an object from
         * the provided pool returns it after a wait
         */
        private class WaitingTestThread
        {
            private readonly GenericObjectPool<Object> _pool;
            private readonly long _pause;
            private Exception _thrown;


            public WaitingTestThread(GenericObjectPool<Object> pool, long pause)
            {
                _pool = pool;
                _pause = pause;
                _thrown = null;
            }

            public Exception Thrown
            {
                get { return this._thrown; }
            }

            public void Run()
            {
                try
                {
                    Object obj = _pool.BorrowObject();
                    Thread.Sleep(TimeSpan.FromMilliseconds(_pause));
                    _pool.ReturnObject(obj);
                }
                catch (Exception e)
                {
                    _thrown = e;
                }
            }
        }

        [Test, Timeout(60000)]
        public void TestMaxWaitMultiThreaded()
        {
            long maxWait = 500; // wait for connection
            long holdTime = 2 * maxWait; // how long to hold connection
            int threads = 10; // number of threads to grab the object initially

            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory);

            pool.BlockWhenExhausted = true;
            pool.MaxWait = maxWait;
            pool.MaxTotal = threads;

            // Create enough threads so half the threads will have to wait
            WaitingTestThread[] wtt = new WaitingTestThread[threads * 2];
            Thread[] threadsArray = new Thread[threads * 2];
            for(int i=0; i < wtt.Length; i++)
            {
                wtt[i] = new WaitingTestThread(pool, holdTime);
                threadsArray[i] = new Thread(wtt[i].Run);
            }

            for(int i=0; i < wtt.Length; i++)
            {
                threadsArray[i].Start();
            }

            int failed = 0;
            for(int i=0; i < wtt.Length; i++)
            {
                threadsArray[i].Join();
                if (wtt[i].Thrown != null)
                {
                    failed++;
                }
            }

            Assert.AreEqual(wtt.Length/2, failed, "Expected half the threads to fail");
        }

        /*
         * Test the following scenario:
         *   Thread 1 borrows an instance
         *   Thread 2 starts to borrow another instance before thread 1 returns its instance
         *   Thread 1 returns its instance while thread 2 is validating its newly created instance
         * The test verifies that the instance created by Thread 2 is not leaked.
         */
        [Test, Timeout(60000)]
        public void TestMakeConcurrentWithReturn()
        {
            SimpleFactory factory = new SimpleFactory();
            GenericObjectPool<Object> pool = new GenericObjectPool<Object>(factory); 
            pool.TestOnBorrow = true;
            factory.SetValid(true);
            // Borrow and return an instance, with a short wait
            WaitingTestThread thread1 = new WaitingTestThread(pool, 200);
            Thread t = new Thread(thread1.Run);
            t.Start();
            Thread.Sleep(50); // wait for validation to succeed
            // Slow down validation and borrow an instance
            factory.SetValidateLatency(400);
            Object instance = pool.BorrowObject();
            // Now make sure that we have not leaked an instance
            Assert.AreEqual(factory.GetMakeCounter(), pool.IdleCount + 1);
            pool.ReturnObject(instance);
            Assert.AreEqual(factory.GetMakeCounter(), pool.IdleCount);
        }

        private void AssertConfiguration(GenericObjectPoolConfig expected, GenericObjectPool<Object> actual)
        {
            Assert.AreEqual(expected.TestOnBorrow, actual.TestOnBorrow, "testOnBorrow");
            Assert.AreEqual(expected.TestOnReturn, actual.TestOnReturn, "testOnReturn");
            Assert.AreEqual(expected.TestWhileIdle, actual.TestWhileIdle, "testWhileIdle");
            Assert.AreEqual(expected.BlockWhenExhausted, actual.BlockWhenExhausted, "whenExhaustedAction");
            Assert.AreEqual(expected.MaxTotal, actual.MaxTotal, "maxTotal");
            Assert.AreEqual(expected.MaxIdle, actual.MaxIdle, "maxIdle");
            Assert.AreEqual(expected.MaxWait, actual.MaxWait, "maxWait");
            Assert.AreEqual(expected.MinEvictableIdleTimeMillis, actual.MinEvictableIdleTimeMillis, "minEvictableIdleTimeMillis");
            Assert.AreEqual(expected.NumTestsPerEvictionRun, actual.NumTestsPerEvictionRun, "numTestsPerEvictionRun");
            Assert.AreEqual(expected.TimeBetweenEvictionRunsMillis, actual.TimeBetweenEvictionRunsMillis, "timeBetweenEvictionRunsMillis");
        }

        public class SimpleFactory : PoolableObjectFactory<Object>
        {
            private Mutex syncRoot = new Mutex();

            public SimpleFactory() : this(true)
            {
            }

            public SimpleFactory(bool valid) : this(valid,valid)
            {
            }

            public SimpleFactory(bool evalid, bool ovalid) : base()
            {
                evenValid = evalid;
                oddValid = ovalid;
            }

            public void SetValid(bool valid)
            {
                lock(syncRoot)
                {
                    SetEvenValid(valid);
                    SetOddValid(valid);
                }
            }

            public void SetEvenValid(bool valid)
            {
                lock(syncRoot)
                {
                    evenValid = valid;
                }
            }

            public void SetOddValid(bool valid)
            {
                lock(syncRoot)
                {
                    oddValid = valid;
                }
            }

            public void SetThrowExceptionOnSuspend(bool val)
            {
                lock(syncRoot)
                {
                    exceptionOnSuspend = val;
                }
            }

            public void SetMaxTotal(int maxTotal)
            {
                lock(syncRoot)
                {
                    this.maxTotal = maxTotal;
                }
            }

            public void SetDestroyLatency(long destroyLatency)
            {
                lock(syncRoot)
                {
                    this.destroyLatency = destroyLatency;
                }
            }

            public void SetMakeLatency(long makeLatency)
            {
                lock(syncRoot)
                {
                    this.makeLatency = makeLatency;
                }
            }

            public void SetValidateLatency(long validateLatency)
            {
                lock(syncRoot)
                {
                    this.validateLatency = validateLatency;
                }
            }

            public virtual Object CreateObject()
            {
                long waitLatency;
                lock(syncRoot)
                {
                    activeCount++;
                    if (activeCount > maxTotal)
                    {
                        throw new IllegalStateException(
                            "Too many active instances: " + activeCount);
                    }
                    waitLatency = makeLatency;
                }

                if (waitLatency > 0)
                {
                    DoWait(waitLatency);
                }

                int counter;

                lock(syncRoot)
                {
                    counter = makeCounter++;
                }

                return counter.ToString();
            }

            public virtual void DestroyObject(Object obj)
            {
                long waitLatency;
                bool throwIt;
                lock(syncRoot)
                {
                    waitLatency = destroyLatency;
                    throwIt = exceptionOnDestroy;
                }

                if (waitLatency > 0)
                {
                    DoWait(waitLatency);
                }

                lock(syncRoot)
                {
                    activeCount--;
                }

                if (throwIt)
                {
                    throw new Exception();
                }
            }

            public virtual bool ValidateObject(Object obj)
            {
                bool validate;
                bool evenTest;
                bool oddTest;
                long waitLatency;
                int counter;

                lock(syncRoot)
                {
                    validate = enableValidation;
                    evenTest = evenValid;
                    oddTest = oddValid;
                    counter = validateCounter++;
                    waitLatency = validateLatency;
                }

                if (waitLatency > 0)
                {
                    DoWait(waitLatency);
                }

                if (validate)
                {
                    return counter%2 == 0 ? evenTest : oddTest; 
                }
                else
                {
                    return true;
                }
            }

            public virtual void ActivateObject(Object obj)
            {
                bool throwIt;
                bool evenTest;
                bool oddTest;
                int counter;
                lock(syncRoot)
                {
                    throwIt = exceptionOnActivate;
                    evenTest = evenValid;
                    oddTest = oddValid;
                    counter = validateCounter++;
                }
                if (throwIt)
                {
                    if (!(counter%2 == 0 ? evenTest : oddTest))
                    {
                        throw new Exception();
                    }
                }
            }

            public virtual void SuspendObject(Object obj)
            {
                bool throwIt;
                lock(syncRoot)
                {
                    throwIt = exceptionOnSuspend;
                }

                if (throwIt)
                {
                    throw new Exception();
                }
            }

            int makeCounter = 0;
            int validateCounter = 0;
            int activeCount = 0;
            bool evenValid = true;
            bool oddValid = true;
            bool exceptionOnSuspend = false;
            bool exceptionOnActivate = false;
            bool exceptionOnDestroy = false;
            bool enableValidation = true;
            long destroyLatency = 0;
            long makeLatency = 0;
            long validateLatency = 0;
            int maxTotal = Int32.MaxValue;
    
            public bool IsThrowExceptionOnActivate()
            {
                lock(syncRoot)
                {
                    return exceptionOnActivate;
                }
            }

            public void SetThrowExceptionOnActivate(bool b)
            {
                lock(syncRoot)
                {
                    exceptionOnActivate = b;
                }
            }

            public void SetThrowExceptionOnDestroy(bool b)
            {
                lock(syncRoot)
                {
                    exceptionOnDestroy = b;
                }
            }
    
            public bool IsValidationEnabled()
            {
                lock(syncRoot)
                {
                    return enableValidation;
                }
            }
    
            public void SetValidationEnabled(bool b)
            {
                lock(syncRoot)
                {
                    enableValidation = b;
                }
            }
            
            public int GetMakeCounter()
            {
                lock(syncRoot)
                {
                    return makeCounter;
                }
            }
            
            private void DoWait(long latency)
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(latency));
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }

        protected override bool IsLifo()
        {
            return true;
        }
    
        protected override bool IsFifo()
        {
            return false;
        }

        public class TestThread
        {
            private readonly Random _random = new Random();
    
            // Thread config items
            private readonly ObjectPool<Object> _pool;
            private readonly int _iter;
            private readonly int _delay;
            private readonly bool _randomDelay;
            private readonly Object _expectedObject;

            private volatile bool _complete = false;
            private volatile bool _failed = false;
            private volatile Exception _error;
    
            public TestThread(ObjectPool<Object> pool) : this(pool, 100, 50, true, null)
            {
            }

            public TestThread(ObjectPool<Object> pool, int iter) : this(pool, iter, 50, true, null)
            {
            }
    
            public TestThread(ObjectPool<Object> pool, int iter, int delay) : this(pool, iter, delay, true, null)
            {
            }
    
            public TestThread(ObjectPool<Object> pool, int iter, int delay, bool randomDelay) :
                this(pool, iter, delay, randomDelay, null)
            {
            }
    
            public TestThread(ObjectPool<Object> pool, int iter, int delay, bool randomDelay, Object obj)
            {
                _pool = pool;
                _iter = iter;
                _delay = delay;
                _randomDelay = randomDelay;
                _expectedObject = obj;
            }
    
            public bool Complete
            {
                get { return _complete; }
            }

            public bool Failed
            {
                get { return _failed; }
            }

            public Exception Error
            {
                get { return this._error; }
            }
    
            public void Run()
            {
                for(int i = 0; i < _iter; i++)
                {
                    long delay = _randomDelay ? (long)_random.Next(_delay) : _delay;
                    try
                    {
                        Thread.Sleep((int)delay);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }

                    Object obj = null;
                    try
                    {
                        obj = _pool.BorrowObject();
                    }
                    catch(Exception e)
                    {
                        _error = e;
                        _failed = true;
                        _complete = true;
                        break;
                    }
    
                    if (_expectedObject != null && !_expectedObject.Equals(obj))
                    {
                        _error = new Exception("Expected: "+_expectedObject+ " found: "+obj);
                        _failed = true;
                        _complete = true;
                        break;
                    }

                    try
                    {
                        Thread.Sleep((int)delay);
                    }
                    catch(ThreadInterruptedException)
                    {
                    }

                    try
                    {
                        _pool.ReturnObject(obj);
                    }
                    catch(Exception e)
                    {
                        _error = e;
                        _failed = true;
                        _complete = true;
                        break;
                    }
                }

                _complete = true;
            }
        }
    }
}

