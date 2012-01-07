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
using System.Threading;

using NUnit.Framework;

using Apache.NMS.Pooled.Commons.Pool.Impl;
using Apache.NMS.Pooled.Commons.Collections;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public abstract class TestObjectPool
    {
        /// <summary>
        /// Create an ObjectPool with the specified factory.  The pool should be in a default
        /// configuration and conform to the expected behaviors described in ObjectPool.
        /// Generally speaking there should be no limits on the various object counts.
        /// throws NotSupportedException if the pool being tested does not follow pool contracts.
        /// </summary>
        protected abstract ObjectPool<Object> MakeEmptyPool(PoolableObjectFactory<Object> factory);
    
        [Test]
        public void TestClosedPoolBehavior()
        {
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(new MethodCallPoolableObjectFactory());
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            Object o1 = pool.BorrowObject();
            Object o2 = pool.BorrowObject();
    
            pool.Close();
    
            try
            {
                pool.AddObject();
                Assert.Fail("A closed pool must throw an IllegalStateException when AddObject is called.");
            }
            catch (InvalidOperationException)
            {
            }
    
            try
            {
                pool.BorrowObject();
                Assert.Fail("A closed pool must throw an IllegalStateException when BorrowObject is called.");
            }
            catch (InvalidOperationException)
            {
            }
    
            // The following should not throw exceptions just because the pool is closed.
            if (pool.IdleCount >= 0)
            {
                Assert.AreEqual(0, pool.IdleCount, "A closed pool shouldn't have any idle objects.");
            }
            if (pool.ActiveCount >= 0)
            {
                Assert.AreEqual(2, pool.ActiveCount, "A closed pool should still keep count of active objects.");
            }

            pool.ReturnObject(o1);

            if (pool.IdleCount >= 0)
            {
                Assert.AreEqual(0, pool.IdleCount, "RturnObject should not Add items back into the idle object pool for a closed pool.");
            }
            if (pool.ActiveCount >= 0)
            {
                Assert.AreEqual(1, pool.ActiveCount, "A closed pool should still keep count of active objects.");
            }

            pool.InvalidateObject(o2);

            if (pool.IdleCount >= 0)
            {
                Assert.AreEqual(0, pool.IdleCount, "invalidateObject must not Add items back into the idle object pool.");
            }
            if (pool.ActiveCount >= 0)
            {
                Assert.AreEqual(0, pool.ActiveCount, "A closed pool should still keep count of active objects.");
            }

            pool.Clear();
            pool.Close();
        }

        private readonly Int32 ZERO = 0;
        private readonly Int32 ONE = 1;
    
        [Test]
        public void TestPOFAddObjectUsage()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch(NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
    
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(0, pool.IdleCount);
            // AddObject should make a new object, pasivate it and put it in the pool
            pool.AddObject();
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(1, pool.IdleCount);
            expectedMethods.Add(new MethodCall("CreateObject").SetReturned(ZERO));
            expectedMethods.Add(new MethodCall("SuspendObject", ZERO));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            //// Test exception handling of AddObject
            Reset(pool, factory, expectedMethods);
    
            // makeObject Exceptions should be propagated to client code from AddObject
            factory.MakeObjectFail = true;
            try
            {
                pool.AddObject();
                Assert.Fail("Expected AddObject to propagate makeObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("CreateObject"));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);

            Clear(factory, expectedMethods);
    
            // passivateObject Exceptions should be propagated to client code from AddObject
            factory.MakeObjectFail = false;
            factory.SuspendObjectFail = true;
            try
            {
                pool.AddObject();
                Assert.Fail("Expected AddObject to propagate passivateObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("CreateObject").SetReturned(ONE));
            expectedMethods.Add(new MethodCall("SuspendObject", ONE));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
        }
    
        [Test]
        public void TestPOFBorrowObjectUsages()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            if (pool is GenericObjectPool<Object>)
            {
                ((GenericObjectPool<Object>) pool).TestOnBorrow = true;
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;
    
            // Test correct behavior code paths
    
            // existing idle object should be activated and validated
            pool.AddObject();
            Clear(factory, expectedMethods);
            obj = pool.BorrowObject();
            expectedMethods.Add(new MethodCall("ActivateObject", ZERO));
            expectedMethods.Add(new MethodCall("ValidateObject", ZERO).SetReturned(true));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
            pool.ReturnObject(obj);
    
            // Test exception handling of BorrowObject
            Reset(pool, factory, expectedMethods);
    
            // makeObject Exceptions should be propagated to client code from BorrowObject
            factory.MakeObjectFail = true;
            try
            {
                obj = pool.BorrowObject();
                Assert.Fail("Expected BorrowObject to propagate makeObject exception.");
            }
            catch (MethodAccessException)
            {
                // expected
            }
            expectedMethods.Add(new MethodCall("CreateObject"));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);

            // when activateObject Assert.Fails in BorrowObject, a new object should be borrowed/created
            Reset(pool, factory, expectedMethods);
            pool.AddObject();
            Clear(factory, expectedMethods);
    
            factory.ActivateObjectFail = true;
            expectedMethods.Add(new MethodCall("ActivateObject", obj));
            try
            {
                obj = pool.BorrowObject();
                Assert.Fail("Expecting NoSuchElementException");
            }
            catch (NoSuchElementException)
            {
                // Expected - newly created object will also Assert.Fail to activate
            }
            // Idle object Assert.Fails activation, new one created, also Assert.Fails
            expectedMethods.Add(new MethodCall("CreateObject").SetReturned(ONE));
            expectedMethods.Add(new MethodCall("ActivateObject", ONE));
            RemoveDestroyObjectCall(factory.MethodCalls); // The exact timing of destroyObject is flexible here.
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // when validateObject Assert.Fails in BorrowObject, a new object should be borrowed/created
            Reset(pool, factory, expectedMethods);
            pool.AddObject();
            Clear(factory, expectedMethods);
    
            factory.ValidateObjectFail = true;
            expectedMethods.Add(new MethodCall("ActivateObject", ZERO));
            expectedMethods.Add(new MethodCall("ValidateObject", ZERO));
            try
            {
                obj = pool.BorrowObject();
            }
            catch (NoSuchElementException)
            {
                // Expected - newly created object will also Assert.Fail to validate
            }
            // Idle object is activated, but Assert.Fails validation.
            // New instance is created, activated and then Assert.Fails validation
            expectedMethods.Add(new MethodCall("CreateObject").SetReturned(ONE));
            expectedMethods.Add(new MethodCall("ActivateObject", ONE));
            expectedMethods.Add(new MethodCall("ValidateObject", ONE));
            RemoveDestroyObjectCall(factory.MethodCalls); // The exact timing of destroyObject is flexible here.
            // Second activate and validate are missing from expectedMethods
            Assert.IsTrue(factory.MethodCalls.ContainsAll(expectedMethods));
        }
    
        [Test]
        public void TestPOFReturnObjectUsages()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;
    
            // Test correct behavior code paths
            obj = pool.BorrowObject();
            Clear(factory, expectedMethods);
    
            // returned object should be passivated
            pool.ReturnObject(obj);
            // StackObjectPool, SoftReferenceObjectPool also validate on return
            expectedMethods.Add(new MethodCall("SuspendObject", obj));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // Test exception handling of RturnObject
            Reset(pool, factory, expectedMethods);
            pool.AddObject();
            pool.AddObject();
            pool.AddObject();
            Assert.AreEqual(3, pool.IdleCount);
            // passivateObject should swallow exceptions and not Add the object to the pool
            obj = pool.BorrowObject();
            pool.BorrowObject();
            Assert.AreEqual(1, pool.IdleCount);
            Assert.AreEqual(2, pool.ActiveCount);
            Clear(factory, expectedMethods);
            factory.SuspendObjectFail = true;
            pool.ReturnObject(obj);
            expectedMethods.Add(new MethodCall("SuspendObject", obj));
            RemoveDestroyObjectCall(factory.MethodCalls); // The exact timing of destroyObject is flexible here.
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
            Assert.AreEqual(1, pool.IdleCount);   // Not returned
            Assert.AreEqual(1, pool.ActiveCount); // But not in active count
    
            // destroyObject should swallow exceptions too
            Reset(pool, factory, expectedMethods);
            obj = pool.BorrowObject();
            Clear(factory, expectedMethods);
            factory.SuspendObjectFail = true;
            factory.DestroyObjectFail = true;
            pool.ReturnObject(obj);
        }
    
        [Test]
        public void TestPOFInvalidateObjectUsages()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
            Object obj;
    
            // Test correct behavior code paths
    
            obj = pool.BorrowObject();
            Clear(factory, expectedMethods);
    
            // invalidated object should be destroyed
            pool.InvalidateObject(obj);
            expectedMethods.Add(new MethodCall("DestroyObject", obj));
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
    
            // Test exception handling of invalidateObject
            Reset(pool, factory, expectedMethods);
            obj = pool.BorrowObject();
            Clear(factory, expectedMethods);
            factory.DestroyObjectFail = true;
            try
            {
                pool.InvalidateObject(obj);
                Assert.Fail("Expecting destroy exception to propagate");
            }
            catch (MethodAccessException)
            {
                // Expected
            }
            Thread.Sleep(250); // could be defered
            RemoveDestroyObjectCall(factory.MethodCalls);
            Assert.AreEqual(expectedMethods, factory.MethodCalls);
        }
    
        [Test]
        public void TestPOFClearUsages()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
    
            // Test correct behavior code paths
            PoolUtils.PreFill(pool, 5);
            pool.Clear();
    
            // Test exception handling Clear should swallow destory object Assert.Failures
            Reset(pool, factory, expectedMethods);
            factory.DestroyObjectFail = true;
            PoolUtils.PreFill(pool, 5);
            pool.Clear();
        }
    
        [Test]
        public void TestPOFCloseUsages()
        {
            MethodCallPoolableObjectFactory factory = new MethodCallPoolableObjectFactory();
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            List<MethodCall> expectedMethods = new ArrayList<MethodCall>();
    
            // Test correct behavior code paths
            PoolUtils.PreFill(pool, 5);
            pool.Close();

            // Test exception handling close should swallow Assert.Failures
            try
            {
                pool = MakeEmptyPool(factory);
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            Reset(pool, factory, expectedMethods);
            factory.DestroyObjectFail = true;
            PoolUtils.PreFill(pool, 5);
            pool.Close();
        }
    
        [Test]
        public void TestToString()
        {
            ObjectPool<Object> pool;
            try
            {
                pool = MakeEmptyPool(new MethodCallPoolableObjectFactory());
            }
            catch (NotSupportedException)
            {
                return; // test not supported
            }
            pool.ToString();
        }
    
        internal static void RemoveDestroyObjectCall(List<MethodCall> calls)
        {
            Iterator<MethodCall> iter = calls.Iterator();
            while (iter.HasNext)
            {
                MethodCall call = iter.Next();
                if ("DestroyObject".Equals(call.Name))
                {
                    iter.Remove();
                }
            }
        }
    
        private static void Reset(ObjectPool<Object> pool, MethodCallPoolableObjectFactory factory, List<MethodCall> expectedMethods)
        {
            pool.Clear();
            Clear(factory, expectedMethods);
            factory.Reset();
        }
    
        private static void Clear(MethodCallPoolableObjectFactory factory, List<MethodCall> expectedMethods)
        {
            factory.MethodCalls.Clear();
            expectedMethods.Clear();
        }

    }
}

