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

using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public class TestBaseObjectPool : TestObjectPool
    {
        protected ObjectPool<Object> basePool = null;

        protected virtual ObjectPool<Object> MakeEmptyPool(int mincapacity)
        {
            if (this.GetType() != typeof(TestBaseObjectPool))
            {
                Assert.Fail("Subclasses of TestBaseObjectPool must reimplement this method.");
            }

            throw new NotSupportedException("BaseObjectPool isn't a complete implementation.");
        }
    
        protected override ObjectPool<Object> MakeEmptyPool(PoolableObjectFactory<Object> factory)
        {
            if (this.GetType() != typeof(TestBaseObjectPool))
            {
                Assert.Fail("Subclasses of TestBaseObjectPool must reimplement this method.");
            }
            throw new NotSupportedException("BaseObjectPool isn't a complete implementation.");
        }

        protected virtual Object GetNthObject(int n)
        {
            if (this.GetType() != typeof(TestBaseObjectPool))
            {
                Assert.Fail("Subclasses of TestBaseObjectPool must reimplement this method.");
            }
            throw new NotSupportedException("BaseObjectPool isn't a complete implementation.");
        }
    
        protected virtual bool IsLifo()
        {
            if (this.GetType() != typeof(TestBaseObjectPool))
            {
                Assert.Fail("Subclasses of TestBaseObjectPool must reimplement this method.");
            }
            return false;
        }
    
        protected virtual bool IsFifo()
        {
            if (this.GetType() != typeof(TestBaseObjectPool))
            {
                Assert.Fail("Subclasses of TestBaseObjectPool must reimplement this method.");
            }
            return false;
        }

        class InternalObjectPool : BaseObjectPool<Object>
        {
            public override Object BorrowObject()
            {
                return null;
            }

            public override void ReturnObject(Object obj)
            {
            }

            public override void InvalidateObject(Object obj)
            {
            }
        }

        [Test]
        public void TestUnsupportedOperations()
        {
            if (!GetType().Equals(typeof(TestBaseObjectPool)))
            {
                return; // skip redundant tests
            }

            ObjectPool<Object> pool = new InternalObjectPool();

            Assert.IsTrue(pool.IdleCount < 0, "Negative expected.");
            Assert.IsTrue(pool.ActiveCount < 0, "Negative expected.");

            try
            {
                pool.Clear();
                Assert.Fail("Expected NotSupportedException");
            }
            catch(NotSupportedException)
            {
                // expected
            }

            try
            {
                pool.AddObject();
                Assert.Fail("Expected NotSupportedException");
            }
            catch(NotSupportedException)
            {
                // expected
            }
        }
    
        [Test]
        public void testClose()
        {
            ObjectPool<Object> pool = new InternalObjectPool();

            pool.Close();
            pool.Close(); // should not error as of Pool 2.0.
        }
    
        [Test]
        public void testBaseBorrow()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Assert.AreEqual(GetNthObject(0), basePool.BorrowObject());
            Assert.AreEqual(GetNthObject(1), basePool.BorrowObject());
            Assert.AreEqual(GetNthObject(2), basePool.BorrowObject());
        }
    
        [Test]
        public void testBaseAddObject()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }

            try
            {
                Assert.AreEqual(0, basePool.IdleCount);
                Assert.AreEqual(0, basePool.ActiveCount);
                basePool.AddObject();
                Assert.AreEqual(1, basePool.IdleCount);
                Assert.AreEqual(0, basePool.ActiveCount);
                Object obj = basePool.BorrowObject();
                Assert.AreEqual(GetNthObject(0), obj);
                Assert.AreEqual(0, basePool.IdleCount);
                Assert.AreEqual(1, basePool.ActiveCount);
                basePool.ReturnObject(obj);
                Assert.AreEqual(1, basePool.IdleCount);
                Assert.AreEqual(0, basePool.ActiveCount);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if one of those calls is unsupported
            }
        }
    
        [Test]
        public void testBaseBorrowReturn()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }

            Object obj0 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(0),obj0);
            Object obj1 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(1),obj1);
            Object obj2 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(2),obj2);
            basePool.ReturnObject(obj2);
            obj2 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(2),obj2);
            basePool.ReturnObject(obj1);
            obj1 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(1),obj1);
            basePool.ReturnObject(obj0);
            basePool.ReturnObject(obj2);
            obj2 = basePool.BorrowObject();

            if (IsLifo())
            {
                Assert.AreEqual(GetNthObject(2),obj2);
            }
            if (IsFifo())
            {
                Assert.AreEqual(GetNthObject(0),obj2);
            }

            obj0 = basePool.BorrowObject();
            if (IsLifo())
            {
                Assert.AreEqual(GetNthObject(0),obj0);
            }
            if (IsFifo())
            {
                Assert.AreEqual(GetNthObject(2),obj0);
            }
        }
    
        [Test]
        public void testBaseNumActiveNumIdle()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            Object obj0 = basePool.BorrowObject();
            Assert.AreEqual(1,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            Object obj1 = basePool.BorrowObject();
            Assert.AreEqual(2,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            basePool.ReturnObject(obj1);
            Assert.AreEqual(1,basePool.ActiveCount);
            Assert.AreEqual(1,basePool.IdleCount);
            basePool.ReturnObject(obj0);
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(2,basePool.IdleCount);
        }
    
        [Test]
        public void testBaseClear()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            Object obj0 = basePool.BorrowObject();
            Object obj1 = basePool.BorrowObject();
            Assert.AreEqual(2,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            basePool.ReturnObject(obj1);
            basePool.ReturnObject(obj0);
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(2,basePool.IdleCount);
            basePool.Clear();
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            Object obj2 = basePool.BorrowObject();
            Assert.AreEqual(GetNthObject(2),obj2);
        }
    
        [Test]
        public void testBaseInvalidateObject()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            Object obj0 = basePool.BorrowObject();
            Object obj1 = basePool.BorrowObject();
            Assert.AreEqual(2,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            basePool.InvalidateObject(obj0);
            Assert.AreEqual(1,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
            basePool.InvalidateObject(obj1);
            Assert.AreEqual(0,basePool.ActiveCount);
            Assert.AreEqual(0,basePool.IdleCount);
        }

        [Test]
        public void TestBaseClosePool()
        {
            try
            {
                basePool = MakeEmptyPool(3);
            }
            catch(NotSupportedException)
            {
                return; // skip this test if unsupported
            }

            Object obj = basePool.BorrowObject();
            basePool.ReturnObject(obj);

            basePool.Close();
            try
            {
                basePool.BorrowObject();
                Assert.Fail("Expected IllegalStateException");
            }
            catch(InvalidOperationException)
            {
                // expected
            }
        }
    }
}

