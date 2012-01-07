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

using Apache.NMS.Pooled.Commons.Pool.Impl;
using Apache.NMS.Pooled.Commons.Collections;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public abstract class TestKeyedObjectPool
    {
        /// <summary>
        /// Create an KeyedObjectPool with the specified factory.  The pool should be in a
        /// default configuration and conform to the expected behaviors described in KeyedObjectPool.
        /// Generally speaking there should be no limits on the various object counts.
        /// </summary>
        protected abstract KeyedObjectPool<Object,Object> MakeEmptyPool(KeyedPoolableObjectFactory<Object,Object> factory);

        protected readonly String KEY = "key";
        
        private KeyedObjectPool<Object,Object> pool = null;

        protected abstract Object MakeKey(int n);

        protected abstract bool IsFifo();

        protected abstract bool IsLifo();
        
        [TearDown]
        public void TearDown()
        {
            pool = null;
        }
        
        /**
         * Create an KeyedObjectPool instance that can contain at least mincapacity idle
         * and active objects, or throw IllegalArgumentException if such a pool cannot be
         * created.
         */
        protected abstract KeyedObjectPool<Object,Object> MakeEmptyPool(int mincapacity);
        
        /// <summary>
        /// Return what we expect to be the n'th object (zero indexed) created by the pool
        /// for the specified pool key.
        /// </summary>
        protected abstract Object GetNthObject(Object key, int n);

        private sealed class InternalKeyedPoolableObjectFactory : BaseKeyedPoolableObjectFactory<Object,Object>
        {
            public override Object CreateObject(Object key)
            {
                return new Object();
            }
        }

        [Test]
        public void TestClosedPoolBehavior()
        {
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(new InternalKeyedPoolableObjectFactory());
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
    
            Object o1 = pool.BorrowObject(KEY);
            Object o2 = pool.BorrowObject(KEY);
    
            pool.Close();
    
            try
            {
                pool.AddObject(KEY);
                Assert.Fail("A Closed pool must throw an IllegalStateException when AddObject is called.");
            }
            catch (IllegalStateException)
            {
                // expected
            }
    
            try
            {
                pool.BorrowObject(KEY);
                Assert.Fail("A Closed pool must throw an IllegalStateException when BorrowObject is called.");
            }
            catch (IllegalStateException)
            {
                // expected
            }
    
            // The following should not throw exceptions just because the pool is Closed.
            Assert.AreEqual(0, pool.KeyedIdleCount(KEY), "A Closed pool shouldn't have any idle objects.");
            Assert.AreEqual(0, pool.IdleCount, "A Closed pool shouldn't have any idle objects.");
            int count = pool.ActiveCount;
            count = pool.KeyedActiveCount(KEY);
            count++;
            pool.ReturnObject(KEY, o1);
            Assert.AreEqual(0, pool.KeyedIdleCount(KEY), "ReturnObject should not add items back into the idle object pool for a Closed pool.");
            Assert.AreEqual(0, pool.IdleCount, "ReturnObject should not add items back into the idle object pool for a Closed pool.");
            pool.InvalidateObject(KEY, o2);
            pool.Clear(KEY);
            pool.Clear();
            pool.Close();
        }

        private readonly Int32 ZERO = 0;
        private readonly Int32 ONE = 1;
    
        [Test]
        public void testKPOFAddObjectUsage()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
    
            // AddObject should make a new object, pasivate it and put it in the pool
            pool.AddObject(KEY);
            expectedMethods.Add(new MethodCall("makeObject", KEY).SetReturned(ZERO));
            expectedMethods.Add(new MethodCall("passivateObject", KEY, ZERO));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // Test exception handling of AddObject
            Reset(pool, factory, expectedMethods);

            // makeObject Exceptions should be propagated to client code from AddObject
            factory.CreateObjectFail = true;
            try
            {
                pool.AddObject(KEY);
                Assert.Fail("Expected AddObject to propagate makeObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("makeObject", KEY));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            Clear(factory, expectedMethods);
    
            // passivateObject Exceptions should be propagated to client code from AddObject
            factory.CreateObjectFail = false;
            factory.SuspendObjectFail = true;
            try
            {
                pool.AddObject(KEY);
                Assert.Fail("Expected AddObject to propagate passivateObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("makeObject", KEY).SetReturned(ONE));
            expectedMethods.Add(new MethodCall("passivateObject", KEY, ONE));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
        }

        [Test]
        public void TestKPOFBorrowObjectUsages()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;

            if (pool is GenericKeyedObjectPool<Object, Object>)
            {
                ((GenericKeyedObjectPool<Object,Object>) pool).TestOnBorrow = true;
            }
    
            // Test correct behavior code paths

            // existing idle object should be activated and validated
            pool.AddObject(KEY);
            Clear(factory, expectedMethods);
            obj = pool.BorrowObject(KEY);
            expectedMethods.Add(new MethodCall("activateObject", KEY, ZERO));
            expectedMethods.Add(new MethodCall("validateObject", KEY, ZERO).SetReturned(true));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
            pool.ReturnObject(KEY, obj);
    
            // Test exception handling of BorrowObject
            Reset(pool, factory, expectedMethods);
    
            // makeObject Exceptions should be propagated to client code from BorrowObject
            factory.CreateObjectFail = true;
            try
            {
                obj = pool.BorrowObject(KEY);
                Assert.Fail("Expected BorrowObject to propagate makeObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("makeObject", KEY));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);

            // when activateObject fails in BorrowObject, a new object should be borrowed/created
            Reset(pool, factory, expectedMethods);
            pool.AddObject(KEY);
            Clear(factory, expectedMethods);
    
            factory.ActivateObjectFail = true;
            expectedMethods.Add(new MethodCall("activateObject", KEY, obj));
            try
            {
                obj = pool.BorrowObject(KEY); 
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
                //Activate should fail
            }
            // After idle object fails validation, new on is created and activation
            // fails again for the new one.
            expectedMethods.Add(new MethodCall("makeObject", KEY).SetReturned(ONE));
            expectedMethods.Add(new MethodCall("activateObject", KEY, ONE));
            TestObjectPool.RemoveDestroyObjectCall(factory.MethodCalls); // The exact timing of destroyObject is flexible here.
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // when validateObject fails in BorrowObject, a new object should be borrowed/created
            Reset(pool, factory, expectedMethods);
            pool.AddObject(KEY);
            Clear(factory, expectedMethods);

            factory.ValidateObjectFail = true;
            // testOnBorrow is on, so this will throw when the newly created instance
            // fails validation
            try
            {
                obj = pool.BorrowObject(KEY);
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
                // expected
            }
            // Activate, then validate for idle instance
            expectedMethods.Add(new MethodCall("activateObject", KEY, ZERO));
            expectedMethods.Add(new MethodCall("validateObject", KEY, ZERO));
            // Make new instance, activate succeeds, validate fails
            expectedMethods.Add(new MethodCall("makeObject", KEY).SetReturned(ONE));
            expectedMethods.Add(new MethodCall("activateObject", KEY, ONE));
            expectedMethods.Add(new MethodCall("validateObject", KEY, ONE));
            TestObjectPool.RemoveDestroyObjectCall(factory.MethodCalls);
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
        }
    
        [Test]
        public void TestKPOFReturnObjectUsages()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;
    
            // Test correct behavior code paths
            obj = pool.BorrowObject(KEY);
            Clear(factory, expectedMethods);
    
            // returned object should be passivated
            pool.ReturnObject(KEY, obj);
            expectedMethods.Add(new MethodCall("passivateObject", KEY, obj));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // Test exception handling of ReturnObject
            Reset(pool, factory, expectedMethods);
    
            // passivateObject should swallow exceptions and not add the object to the pool
            pool.AddObject(KEY);
            pool.AddObject(KEY);
            pool.AddObject(KEY);
            Assert.AreEqual(3, pool.KeyedIdleCount(KEY));
            obj = pool.BorrowObject(KEY);
            obj = pool.BorrowObject(KEY);
            Assert.AreEqual(1, pool.KeyedIdleCount(KEY));
            Assert.AreEqual(2, pool.KeyedActiveCount(KEY));
            Clear(factory, expectedMethods);
            factory.SuspendObjectFail = true;
            pool.ReturnObject(KEY, obj);
            expectedMethods.Add(new MethodCall("passivateObject", KEY, obj));
            TestObjectPool.RemoveDestroyObjectCall(factory.MethodCalls); // The exact timing of destroyObject is flexible here.
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
            Assert.AreEqual(1, pool.KeyedIdleCount(KEY));   // Not added
            Assert.AreEqual(1, pool.KeyedActiveCount(KEY)); // But not active
    
            Reset(pool, factory, expectedMethods);
            obj = pool.BorrowObject(KEY);
            Clear(factory, expectedMethods);
            factory.SuspendObjectFail = true;
            factory.DestroyObjectFail = true;
            try
            {
                pool.ReturnObject(KEY, obj);
                if (!(pool is GenericKeyedObjectPool<Object, Object>))
                {
                    Assert.Fail("Expecting DestroyObject exception to be propagated");
                }
            }
            catch (MethodAccessException)
            {
                // Expected
            }
        }
    
        [Test]
        public void TestKPOFInvalidateObjectUsages()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;
    
            // Test correct behavior code paths
    
            obj = pool.BorrowObject(KEY);
            Clear(factory, expectedMethods);

            // invalidated object should be destroyed
            pool.InvalidateObject(KEY, obj);
            expectedMethods.Add(new MethodCall("destroyObject", KEY, obj));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // Test exception handling of InvalidateObject
            Reset(pool, factory, expectedMethods);
            obj = pool.BorrowObject(KEY);
            Clear(factory, expectedMethods);
            factory.DestroyObjectFail = true;
            try
            {
                pool.InvalidateObject(KEY, obj);
                Assert.Fail("Expecting destroy exception to propagate");
            }
            catch (MethodAccessException)
            {
                // Expected
            }
            Thread.Sleep(250);
            TestObjectPool.RemoveDestroyObjectCall(factory.MethodCalls);
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
        }
    
        [Test]
        public void TestKPOFClearUsages()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
    
            // Test correct behavior code paths
            PoolUtils.PreFill(pool, KEY, 5);
            pool.Clear();
    
            // Test exception handling Clear should swallow destory object failures
            Reset(pool, factory, expectedMethods);
            factory.DestroyObjectFail = true;
            PoolUtils.PreFill(pool, KEY, 5);
            pool.Clear();
        }
    
        [Test]
        public void TestKPOFCloseUsages()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            KeyedObjectPool<Object,Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();

            // Test correct behavior code paths
            PoolUtils.PreFill(pool, KEY, 5);
            pool.Close();
    
    
            // Test exception handling Close should swallow failures
            pool = MakeEmptyPool(factory);
            Reset(pool, factory, expectedMethods);
            factory.DestroyObjectFail = true;
            PoolUtils.PreFill(pool, KEY, 5);
            pool.Close();
        }
    
        [Test]
        public void TestToString()
        {
            FailingKeyedPoolableObjectFactory factory = new FailingKeyedPoolableObjectFactory();
            try
            {
                MakeEmptyPool(factory).ToString();
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
        }
        
        [Test]
        public void TestBaseBorrowReturn()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Object obj0 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,0),obj0);
            Object obj1 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,1),obj1);
            Object obj2 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,2),obj2);
            pool.ReturnObject(keya,obj2);
            obj2 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,2),obj2);
            pool.ReturnObject(keya,obj1);
            obj1 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,1),obj1);
            pool.ReturnObject(keya,obj0);
            pool.ReturnObject(keya,obj2);
            obj2 = pool.BorrowObject(keya);

            if (IsLifo())
            {
                Assert.AreEqual(GetNthObject(keya,2),obj2);
            }
            if (IsFifo())
            {
                Assert.AreEqual(GetNthObject(keya,0),obj2);
            }
            obj0 = pool.BorrowObject(keya);
            if (IsLifo())
            {
                Assert.AreEqual(GetNthObject(keya,0),obj0);
            }
            if (IsFifo())
            {
                Assert.AreEqual(GetNthObject(keya,2),obj0);
            }
        }
    
        [Test]
        public void TestBaseBorrow()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Object keyb = MakeKey(1);
            Assert.AreEqual(GetNthObject(keya,0),pool.BorrowObject(keya), "1");
            Assert.AreEqual(GetNthObject(keyb,0),pool.BorrowObject(keyb), "2");
            Assert.AreEqual(GetNthObject(keyb,1),pool.BorrowObject(keyb), "3");
            Assert.AreEqual(GetNthObject(keya,1),pool.BorrowObject(keya), "4");
            Assert.AreEqual(GetNthObject(keyb,2),pool.BorrowObject(keyb), "5");
            Assert.AreEqual(GetNthObject(keya,2),pool.BorrowObject(keya), "6");
        }
    
        [Test]
        public void TestBaseNumActiveNumIdle()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Object obj0 = pool.BorrowObject(keya);
            Assert.AreEqual(1,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Object obj1 = pool.BorrowObject(keya);
            Assert.AreEqual(2,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            pool.ReturnObject(keya,obj1);
            Assert.AreEqual(1,pool.KeyedActiveCount(keya));
            Assert.AreEqual(1,pool.KeyedIdleCount(keya));
            pool.ReturnObject(keya,obj0);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(2,pool.KeyedIdleCount(keya));
    
            Assert.AreEqual(0,pool.KeyedActiveCount("xyzzy12345"));
            Assert.AreEqual(0,pool.KeyedIdleCount("xyzzy12345"));
        }
    
        [Test]
        public void TestBaseNumActiveNumIdle2()
        {
            try
            {
                pool = MakeEmptyPool(6);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Object keyb = MakeKey(1);
            Assert.AreEqual(0,pool.ActiveCount);
            Assert.AreEqual(0,pool.IdleCount);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Assert.AreEqual(0,pool.KeyedActiveCount(keyb));
            Assert.AreEqual(0,pool.KeyedIdleCount(keyb));
    
            Object objA0 = pool.BorrowObject(keya);
            Object objB0 = pool.BorrowObject(keyb);
    
            Assert.AreEqual(2,pool.ActiveCount);
            Assert.AreEqual(0,pool.IdleCount);
            Assert.AreEqual(1,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Assert.AreEqual(1,pool.KeyedActiveCount(keyb));
            Assert.AreEqual(0,pool.KeyedIdleCount(keyb));
    
            Object objA1 = pool.BorrowObject(keya);
            Object objB1 = pool.BorrowObject(keyb);
    
            Assert.AreEqual(4,pool.ActiveCount);
            Assert.AreEqual(0,pool.IdleCount);
            Assert.AreEqual(2,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Assert.AreEqual(2,pool.KeyedActiveCount(keyb));
            Assert.AreEqual(0,pool.KeyedIdleCount(keyb));
    
            pool.ReturnObject(keya,objA0);
            pool.ReturnObject(keyb,objB0);
    
            Assert.AreEqual(2,pool.ActiveCount);
            Assert.AreEqual(2,pool.IdleCount);
            Assert.AreEqual(1,pool.KeyedActiveCount(keya));
            Assert.AreEqual(1,pool.KeyedIdleCount(keya));
            Assert.AreEqual(1,pool.KeyedActiveCount(keyb));
            Assert.AreEqual(1,pool.KeyedIdleCount(keyb));

            pool.ReturnObject(keya,objA1);
            pool.ReturnObject(keyb,objB1);
    
            Assert.AreEqual(0,pool.ActiveCount);
            Assert.AreEqual(4,pool.IdleCount);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(2,pool.KeyedIdleCount(keya));
            Assert.AreEqual(0,pool.KeyedActiveCount(keyb));
            Assert.AreEqual(2,pool.KeyedIdleCount(keyb));
        }
    
        [Test]
        public void TestBaseClear()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Object obj0 = pool.BorrowObject(keya);
            Object obj1 = pool.BorrowObject(keya);
            Assert.AreEqual(2,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            pool.ReturnObject(keya,obj1);
            pool.ReturnObject(keya,obj0);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(2,pool.KeyedIdleCount(keya));
            pool.Clear(keya);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Object obj2 = pool.BorrowObject(keya);
            Assert.AreEqual(GetNthObject(keya,2),obj2);
        }
    
        [Test]
        public void TestBaseInvalidateObject()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object keya = MakeKey(0);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            Object obj0 = pool.BorrowObject(keya);
            Object obj1 = pool.BorrowObject(keya);
            Assert.AreEqual(2,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            pool.InvalidateObject(keya,obj0);
            Assert.AreEqual(1,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
            pool.InvalidateObject(keya,obj1);
            Assert.AreEqual(0,pool.KeyedActiveCount(keya));
            Assert.AreEqual(0,pool.KeyedIdleCount(keya));
        }
    
        [Test]
        public void TestBaseAddObject()
        {
            try
            {
                pool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Object key = MakeKey(0);
            try
            {
                Assert.AreEqual(0,pool.IdleCount);
                Assert.AreEqual(0,pool.ActiveCount);
                Assert.AreEqual(0,pool.KeyedIdleCount(key));
                Assert.AreEqual(0,pool.KeyedActiveCount(key));
                pool.AddObject(key);
                Assert.AreEqual(1,pool.IdleCount);
                Assert.AreEqual(0,pool.ActiveCount);
                Assert.AreEqual(1,pool.KeyedIdleCount(key));
                Assert.AreEqual(0,pool.KeyedActiveCount(key));
                Object obj = pool.BorrowObject(key);
                Assert.AreEqual(GetNthObject(key,0),obj);
                Assert.AreEqual(0,pool.IdleCount);
                Assert.AreEqual(1,pool.ActiveCount);
                Assert.AreEqual(0,pool.KeyedIdleCount(key));
                Assert.AreEqual(1,pool.KeyedActiveCount(key));
                pool.ReturnObject(key,obj);
                Assert.AreEqual(1,pool.IdleCount);
                Assert.AreEqual(0,pool.ActiveCount);
                Assert.AreEqual(1,pool.KeyedIdleCount(key));
                Assert.AreEqual(0,pool.KeyedActiveCount(key));
            }
            catch(NotSupportedException)
            {
                return; // skip this test if one of those calls is unsupported
            }
        }
    
        private void Reset(KeyedObjectPool<Object,Object> pool, FailingKeyedPoolableObjectFactory factory, List<MethodCall> expectedMethods)
        {
            pool.Clear();
            Clear(factory, expectedMethods);
            factory.Reset();
        }
    
        private void Clear(FailingKeyedPoolableObjectFactory factory, List<MethodCall> expectedMethods)
        {
            factory.MethodCalls.Clear();
            expectedMethods.Clear();
        }
    
        protected class FailingKeyedPoolableObjectFactory : KeyedPoolableObjectFactory<Object,Object>
        {
            private readonly List<MethodCall> methodCalls = new ArrayList<MethodCall>();
            private int count = 0;
            private bool createObjectFail;
            private bool activateObjectFail;
            private bool validateObjectFail;
            private bool suspendObjectFail;
            private bool destroyObjectFail;
    
            public FailingKeyedPoolableObjectFactory()
            {
            }
    
            public void Reset()
            {
                count = 0;
                MethodCalls.Clear();
                CreateObjectFail = false;
                ActivateObjectFail = false;
                ValidateObjectFail = false;
                SuspendObjectFail = false;
                DestroyObjectFail = false;
            }
    
            public List<MethodCall> MethodCalls
            {
                get { return methodCalls; }
            }

            public int CurrentCount
            {
                get { return count; }
                set { this.count = value; }
            }

            public bool CreateObjectFail
            {
                get { return createObjectFail; }
                set { this.createObjectFail = value; }
            }

            public bool DestroyObjectFail
            {
                get { return destroyObjectFail; }
                set { this.destroyObjectFail = value; }
            }

            public bool ActivateObjectFail
            {
                get { return activateObjectFail; }
                set { this.activateObjectFail = value; }
            }

            public bool ValidateObjectFail
            {
                get { return validateObjectFail; }
                set { this.validateObjectFail = value; }
            }

            public bool SuspendObjectFail
            {
                get { return suspendObjectFail; }
                set { this.suspendObjectFail = value; }
            }

            public virtual Object CreateObject(Object key)
            {
                MethodCall call = new MethodCall("CreateObject", key);
                methodCalls.Add(call);
                int count = this.count++;
                if (createObjectFail)
                {
                    throw new MethodAccessException("CreateObject");
                }
                call.Returned = count;
                return count;
            }
    
            public void ActivateObject(Object key, Object obj)
            {
                methodCalls.Add(new MethodCall("ActivateObject", key, obj));
                if (activateObjectFail)
                {
                    throw new MethodAccessException("ActivateObject");
                }
            }
    
            public bool ValidateObject(Object key, Object obj)
            {
                MethodCall call = new MethodCall("ValidateObject", key, obj);
                methodCalls.Add(call);
                if (validateObjectFail)
                {
                    throw new MethodAccessException("ValidateObject");
                }
                call.Returned = true;
                return true;
            }
    
            public void SuspendObject(Object key, Object obj)
            {
                methodCalls.Add(new MethodCall("SuspendedObject", key, obj));
                if (suspendObjectFail)
                {
                    throw new MethodAccessException("SuspendedObject");
                }
            }

            public void DestroyObject(Object key, Object obj)
            {
                methodCalls.Add(new MethodCall("DestroyObject", key, obj));
                if (destroyObjectFail)
                {
                    throw new MethodAccessException("DestroyObject");
                }
            }
        }

    }
}

