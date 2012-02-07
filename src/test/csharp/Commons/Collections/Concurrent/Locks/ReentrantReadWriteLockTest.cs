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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks
{
    [TestFixture]
    public class ReentrantReadWriteLockTest : ConcurrencyTestCase
    {
        private void InterruptibleLockRunnable(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;

            try
            {
                locker.WriteLock.LockInterruptibly();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        private void InterruptedLockRunnable(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;

            try
            {
                locker.WriteLock.LockInterruptibly();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        private class PublicReentrantReadWriteLock : ReentrantReadWriteLock
        {
            public PublicReentrantReadWriteLock() : base()
            {
            }

            public Collection<Thread> AccessQueuedThreads
            {
                get { return base.QueuedThreads; }
            }

            public Collection<Thread> AccessGetWaitingThreads(Condition c)
            {
                return base.GetWaitingThreads(c);
            }
        }
    
        [Test]
        public void TestConstructor()
        {
            ReentrantReadWriteLock rl = new ReentrantReadWriteLock();
            Assert.IsFalse(rl.IsFair);
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.AreEqual(0, rl.ReadLockCount);
            ReentrantReadWriteLock r2 = new ReentrantReadWriteLock(true);
            Assert.IsTrue(r2.IsFair);
            Assert.IsFalse(r2.IsWriteLocked);
            Assert.AreEqual(0, r2.ReadLockCount);
        }
    
        [Test]
        public void TestLock()
        {
            ReentrantReadWriteLock rl = new ReentrantReadWriteLock();
            rl.WriteLock.Lock();
            Assert.IsTrue(rl.IsWriteLocked);
            Assert.IsTrue(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
            rl.WriteLock.UnLock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
            rl.ReadLock.Lock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(1, rl.ReadLockCount);
            rl.ReadLock.UnLock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
        }

        [Test]
        public void TestFairLock()
        {
            ReentrantReadWriteLock rl = new ReentrantReadWriteLock(true);
            rl.WriteLock.Lock();
            Assert.IsTrue(rl.IsWriteLocked);
            Assert.IsTrue(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
            rl.WriteLock.UnLock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
            rl.ReadLock.Lock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(1, rl.ReadLockCount);
            rl.ReadLock.UnLock();
            Assert.IsFalse(rl.IsWriteLocked);
            Assert.IsFalse(rl.IsWriteLockedByCurrentThread);
            Assert.AreEqual(0, rl.ReadLockCount);
        }
    
        [Test]
        public void TestGetWriteHoldCount()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            for(int i = 1; i <= SIZE; i++)
            {
                locker.WriteLock.Lock();
                Assert.AreEqual(i, locker.WriteHoldCount);
            }
            for(int i = SIZE; i > 0; i--)
            {
                locker.WriteLock.UnLock();
                Assert.AreEqual(i-1, locker.WriteHoldCount);
            }
        }
    
        [Test]
        public void TestUnlockThreadStateException()
        {
            ReentrantReadWriteLock rl = new ReentrantReadWriteLock();
            try
            {
                rl.WriteLock.UnLock();
                ShouldThrow();
            }
            catch(ThreadStateException)
            {
            }
        }

        private void TestWriteLockInterruptiblyInterruptedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.WriteLock.LockInterruptibly();
                locker.WriteLock.UnLock();
                locker.WriteLock.LockInterruptibly();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestWriteLockInterruptiblyInterrupted()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Thread t = new Thread(TestWriteLockInterruptiblyInterruptedRun);
            try
            {
                locker.WriteLock.Lock();
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.UnLock();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteTryLockInterruptedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.WriteLock.TryLock(1000);
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestWriteTryLockInterrupted()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestWriteTryLockInterruptedRun);
            try
            {
                t.Start(locker);
                t.Interrupt();
                locker.WriteLock.UnLock();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadLockInterruptiblyInterruptedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.ReadLock.LockInterruptibly();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestReadLockInterruptiblyInterrupted()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestReadLockInterruptiblyInterruptedRun);

            try
            {
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.UnLock();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadTryLockInterruptedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.ReadLock.TryLock(1000);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestReadTryLockInterrupted()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestReadTryLockInterruptedRun);

            try
            {
                t.Start(locker);
                t.Interrupt();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteTryLockWhenLockedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertFalse(locker.WriteLock.TryLock());
        }

        [Test]
        public void TestWriteTryLockWhenLocked()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestWriteTryLockWhenLockedRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadTryLockWhenLockedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertFalse(locker.ReadLock.TryLock());
        }

        [Test]
        public void TestReadTryLockWhenLocked()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestReadTryLockWhenLockedRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestMultipleReadLocksRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertTrue(locker.ReadLock.TryLock());
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestMultipleReadLocks()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.ReadLock.Lock();
            Thread t = new Thread(TestMultipleReadLocksRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.ReadLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteAfterMultipleReadLocksRun1(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.ReadLock.Lock();
            locker.ReadLock.UnLock();
        }

        private void TestWriteAfterMultipleReadLocksRun2(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.WriteLock.Lock();
            locker.WriteLock.UnLock();
        }

        [Test]
        public void TestWriteAfterMultipleReadLocks()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.ReadLock.Lock();
            Thread t1 = new Thread(TestWriteAfterMultipleReadLocksRun1);
            Thread t2 = new Thread(TestWriteAfterMultipleReadLocksRun2);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.ReadLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadAfterWriteLockRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.ReadLock.Lock();
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestReadAfterWriteLock()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestReadAfterWriteLockRun);
            Thread t2 = new Thread(TestReadAfterWriteLockRun);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestReadHoldingWriteLock()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Assert.IsTrue(locker.ReadLock.TryLock());
            locker.ReadLock.UnLock();
            locker.WriteLock.UnLock();
        }

        private void TestReadHoldingWriteLock2Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.ReadLock.Lock();
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestReadHoldingWriteLock2()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestReadHoldingWriteLock2Run);
            Thread t2 = new Thread(TestReadHoldingWriteLock2Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadHoldingWriteLock3Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.WriteLock.Lock();
            locker.WriteLock.UnLock();
        }

        [Test]
        public void TestReadHoldingWriteLock3()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestReadHoldingWriteLock3Run);
            Thread t2 = new Thread(TestReadHoldingWriteLock3Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
    
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteHoldingWriteLock4Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.WriteLock.Lock();
            locker.WriteLock.UnLock();
        }

        [Test]
        public void TestWriteHoldingWriteLock4()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestWriteHoldingWriteLock4Run);
            Thread t2 = new Thread(TestWriteHoldingWriteLock4Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                locker.WriteLock.Lock();
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                locker.WriteLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestReadHoldingWriteLockFair()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.WriteLock.Lock();
            Assert.IsTrue(locker.ReadLock.TryLock());
            locker.ReadLock.UnLock();
            locker.WriteLock.UnLock();
        }

        private void TestReadHoldingWriteLockFair2Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.ReadLock.Lock();
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestReadHoldingWriteLockFair2()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestReadHoldingWriteLockFair2Run);
            Thread t2 = new Thread(TestReadHoldingWriteLockFair2Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadHoldingWriteLockFair3Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.WriteLock.Lock();
            locker.WriteLock.UnLock();
        }

        [Test]
        public void TestReadHoldingWriteLockFair3()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestReadHoldingWriteLockFair3Run);
            Thread t2 = new Thread(TestReadHoldingWriteLockFair3Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.ReadLock.Lock();
                locker.ReadLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteHoldingWriteLockFair4Run(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            locker.WriteLock.Lock();
            locker.WriteLock.UnLock();
        }

        [Test]
        public void TestWriteHoldingWriteLockFair4()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.WriteLock.Lock();
            Thread t1 = new Thread(TestWriteHoldingWriteLockFair4Run);
            Thread t2 = new Thread(TestWriteHoldingWriteLockFair4Run);
    
            try
            {
                t1.Start(locker);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.IsWriteLockedByCurrentThread);
                Assert.IsTrue(locker.WriteHoldCount == 1);
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.WriteHoldCount == 2);
                locker.WriteLock.UnLock();
                locker.WriteLock.Lock();
                locker.WriteLock.UnLock();
                locker.WriteLock.UnLock();
                t1.Join(MEDIUM_DELAY_MS);
                t2.Join(MEDIUM_DELAY_MS);
                Assert.IsTrue(!t1.IsAlive);
                Assert.IsTrue(!t2.IsAlive);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestTryLockWhenReadLockedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertTrue(locker.ReadLock.TryLock());
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestTryLockWhenReadLocked()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.ReadLock.Lock();
            Thread t = new Thread(TestTryLockWhenReadLockedRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.ReadLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteTryLockWhenReadLockedRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertFalse(locker.WriteLock.TryLock());
        }

        [Test]
        public void TestWriteTryLockWhenReadLocked()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.ReadLock.Lock();
            Thread t = new Thread(TestWriteTryLockWhenReadLockedRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.ReadLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestTryLockWhenReadLockedFairRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertTrue(locker.ReadLock.TryLock());
            locker.ReadLock.UnLock();
        }

        [Test]
        public void TestTryLockWhenReadLockedFair()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.ReadLock.Lock();
            Thread t = new Thread(TestTryLockWhenReadLockedFairRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.ReadLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteTryLockWhenReadLockedFairRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            ThreadAssertFalse(locker.WriteLock.TryLock());
        }

        [Test]
        public void TestWriteTryLockWhenReadLockedFair()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock(true);
            locker.ReadLock.Lock();
            Thread t = new Thread(TestWriteTryLockWhenReadLockedFairRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.ReadLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteTryLockTimeoutRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                ThreadAssertFalse(locker.WriteLock.TryLock(1));
            }
            catch (Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestWriteTryLockTimeout()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestWriteTryLockTimeoutRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadTryLockTimeoutRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                ThreadAssertFalse(locker.ReadLock.TryLock(1));
            }
            catch (Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestReadTryLockTimeout()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            locker.WriteLock.Lock();
            Thread t = new Thread(TestReadTryLockTimeoutRun);

            try
            {
                t.Start(locker);
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestWriteLockInterruptiblyRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.WriteLock.LockInterruptibly();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestWriteLockInterruptibly()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            try
            {
                locker.WriteLock.LockInterruptibly();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }

            Thread t = new Thread(TestWriteLockInterruptiblyRun);

            try
            {
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestReadLockInterruptiblyRun(object state)
        {
            ReentrantReadWriteLock locker = state as ReentrantReadWriteLock;
            try
            {
                locker.ReadLock.LockInterruptibly();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestReadLockInterruptibly()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            try
            {
                locker.WriteLock.LockInterruptibly();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
            Thread t = new Thread(TestReadLockInterruptiblyRun);

            try
            {
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                t.Join();
                locker.WriteLock.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestAwaitThreadStateException()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            try
            {
                c.Await();
                ShouldThrow();
            }
            catch (ThreadStateException)
            {
            }
            catch (Exception)
            {
                ShouldThrow();
            }
        }
    
        [Test]
        public void TestSignalThreadStateException()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            try
            {
                c.Signal();
                ShouldThrow();
            }
            catch (ThreadStateException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestAwaitNanosTimeout()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            try
            {
                locker.WriteLock.Lock();
                long t = c.Await(100);
                Assert.IsTrue(t <= 0);
                locker.WriteLock.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestAwaitTimeout()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            try
            {
                locker.WriteLock.Lock();
                c.Await(150);
                locker.WriteLock.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestAwaitUntilTimeout()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            try
            {
                locker.WriteLock.Lock();
                c.AwaitUntil(DateTime.Now.AddMilliseconds(250));
                locker.WriteLock.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAwait()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t = new Thread(TestAwaitRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                c.Signal();
                locker.WriteLock.UnLock();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
//        class UnInterruptableThread extends Thread
//        {
//            private Lock lock;
//            private Condition c;
//    
//            public volatile boolean canAwake = false;
//            public volatile boolean Interrupted = false;
//            public volatile boolean lockStarted = false;
//    
//            public UnInterruptableThread(Lock lock, Condition c) {
//                this.lock = lock;
//                this.c = c;
//            }
//    
//            public synchronized void run()
//            {
//                locker.Lock();
//                lockStarted = true;
//    
//                while (!canAwake) {
//                    c.AwaitUnInterruptibly();
//                }
//    
//                Interrupted = isInterrupted();
//                locker.UnLock();
//            }
//        }
//    
//        [Test]
//        public void TestAwaitUnInterruptibly()
//        {
//            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
//            Condition c = locker.WriteLock.NewCondition();
//            UnInterruptableThread thread = new UnInterruptableThread(locker.WriteLock, c);
//    
//            try
//            {
//                thread.Start();
//    
//                while (!thread.lockStarted)
//                {
//                    Thread.Sleep(100);
//                }
//    
//                locker.WriteLock.Lock();
//                try
//                {
//                    thread.Interrupt();
//                    thread.canAwake = true;
//                    c.Signal();
//                }
//                finally
//                {
//                    locker.WriteLock.UnLock();
//                }
//    
//                thread.Join();
//                Assert.IsTrue(thread.Interrupted);
//                Assert.IsFalse(thread.IsAlive);
//            }
//            catch (Exception e)
//            {
//                UnexpectedException(e);
//            }
//        }

        private void TestAwaitInterruptRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                c.Await();
                locker.WriteLock.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitInterrupt()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestAwaitInterruptRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitTimedInterruptRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                c.Await(SHORT_DELAY_MS * 2 * 1000000);
                locker.WriteLock.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitTimedInterrupt()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestAwaitTimedInterruptRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitUntilInterruptRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                c.AwaitUntil(DateTime.Now.AddMilliseconds(10000));
                locker.WriteLock.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitUntilInterrupt()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestAwaitUntilInterruptRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestSignalAllRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestSignalAll()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t1 = new Thread(TestSignalAllRun);
            Thread t2 = new Thread(TestSignalAllRun);
    
            try
            {
                t1.Start(data);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                c.SignalAll();
                locker.WriteLock.UnLock();
                t1.Join(SHORT_DELAY_MS);
                t2.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t1.IsAlive);
                Assert.IsFalse(t2.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestHasQueuedThreads()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsFalse(locker.HasQueuedThreads);
                locker.WriteLock.Lock();
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(locker.HasQueuedThreads);
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestHasQueuedThreadNRE()
        {
            ReentrantReadWriteLock sync = new ReentrantReadWriteLock();
            try
            {
                sync.HasQueuedThread(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
        }
    
        [Test]
        public void TestHasQueuedThread()
        {
            ReentrantReadWriteLock sync = new ReentrantReadWriteLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsFalse(sync.HasQueuedThread(t1));
                Assert.IsFalse(sync.HasQueuedThread(t2));
                sync.WriteLock.Lock();
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThread(t1));
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThread(t1));
                Assert.IsTrue(sync.HasQueuedThread(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.HasQueuedThread(t1));
                Assert.IsTrue(sync.HasQueuedThread(t2));
                sync.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.HasQueuedThread(t1));
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.HasQueuedThread(t2));
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetQueueLength()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.AreEqual(0, locker.QueueLength);
                locker.WriteLock.Lock();
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(2, locker.QueueLength);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(0, locker.QueueLength);
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetQueuedThreads()
        {
            PublicReentrantReadWriteLock locker = new PublicReentrantReadWriteLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsTrue(locker.AccessQueuedThreads.IsEmpty());
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.AccessQueuedThreads.IsEmpty());
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.AccessQueuedThreads.Contains(t1));
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.AccessQueuedThreads.Contains(t1));
                Assert.IsTrue(locker.AccessQueuedThreads.Contains(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(locker.AccessQueuedThreads.Contains(t1));
                Assert.IsTrue(locker.AccessQueuedThreads.Contains(t2));
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.AccessQueuedThreads.IsEmpty());
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestHasWaitersNRE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            try
            {
                locker.HasWaiters(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitQueueLengthNRE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            try
            {
                locker.GetWaitQueueLength(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitingThreadsNRE()
        {
            PublicReentrantReadWriteLock locker = new PublicReentrantReadWriteLock();
            try
            {
                locker.AccessGetWaitingThreads(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestHasWaitersAE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            ReentrantReadWriteLock locker2 = new ReentrantReadWriteLock();
            try
            {
                locker2.HasWaiters(c);
                ShouldThrow();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestHasWaitersTSE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            try
            {
                locker.HasWaiters(c);
                ShouldThrow();
            }
            catch (ThreadStateException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitQueueLengthAE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            ReentrantReadWriteLock locker2 = new ReentrantReadWriteLock();
            try
            {
                locker2.GetWaitQueueLength(c);
                ShouldThrow();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitQueueLengthTSE()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            try
            {
                locker.GetWaitQueueLength(c);
                ShouldThrow();
            }
            catch (ThreadStateException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitingThreadsIAE()
        {
            PublicReentrantReadWriteLock locker = new PublicReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            PublicReentrantReadWriteLock locker2 = new PublicReentrantReadWriteLock();
            try
            {
                locker2.AccessGetWaitingThreads(c);
                ShouldThrow();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestGetWaitingThreadsIMSE()
        {
            PublicReentrantReadWriteLock locker = new PublicReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            try
            {
                locker.AccessGetWaitingThreads(c);
                ShouldThrow();
            }
            catch (ThreadStateException)
            {
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestHasWaitersRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                ThreadAssertFalse(locker.HasWaiters(c));
                ThreadAssertEquals(0, locker.GetWaitQueueLength(c));
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestHasWaiters()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestHasWaitersRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.AreEqual(1, locker.GetWaitQueueLength(c));
                c.Signal();
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.AreEqual(0, locker.GetWaitQueueLength(c));
                locker.WriteLock.UnLock();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestGetWaitQueueLengthRun(object state)
        {
            Pair data = state as Pair;
            ReentrantReadWriteLock locker = data.first as ReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                ThreadAssertFalse(locker.HasWaiters(c));
                ThreadAssertEquals(0, locker.GetWaitQueueLength(c));
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitQueueLength()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            Condition c = (locker.WriteLock.NewCondition());
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestGetWaitQueueLengthRun);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.AreEqual(1, locker.GetWaitQueueLength(c));
                c.Signal();
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.AreEqual(0, locker.GetWaitQueueLength(c));
                locker.WriteLock.UnLock();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestGetWaitingThreadsRun1(object state)
        {
            Pair data = state as Pair;
            PublicReentrantReadWriteLock locker = data.first as PublicReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                ThreadAssertTrue(locker.AccessGetWaitingThreads(c).IsEmpty());
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestGetWaitingThreadsRun2(object state)
        {
            Pair data = state as Pair;
            PublicReentrantReadWriteLock locker = data.first as PublicReentrantReadWriteLock;
            Condition c = data.second as Condition;

            try
            {
                locker.WriteLock.Lock();
                ThreadAssertFalse(locker.AccessGetWaitingThreads(c).IsEmpty());
                c.Await();
                locker.WriteLock.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitingThreads()
        {
            PublicReentrantReadWriteLock locker = new PublicReentrantReadWriteLock();
            Condition c = locker.WriteLock.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t1 = new Thread(TestGetWaitingThreadsRun1);
            Thread t2 = new Thread(TestGetWaitingThreadsRun2);
    
            try
            {
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.AccessGetWaitingThreads(c).IsEmpty());
                locker.WriteLock.UnLock();
                t1.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.IsTrue(locker.AccessGetWaitingThreads(c).Contains(t1));
                Assert.IsTrue(locker.AccessGetWaitingThreads(c).Contains(t2));
                c.SignalAll();
                locker.WriteLock.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.WriteLock.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.IsTrue(locker.AccessGetWaitingThreads(c).IsEmpty());
                locker.WriteLock.UnLock();
                t1.Join(SHORT_DELAY_MS);
                t2.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t1.IsAlive);
                Assert.IsFalse(t2.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    
        [Test]
        public void TestToString()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            String us = locker.ToString();
            Assert.IsTrue(us.IndexOf("Write locks = 0") >= 0);
            Assert.IsTrue(us.IndexOf("Read locks = 0") >= 0);
            locker.WriteLock.Lock();
            String ws = locker.ToString();
            Assert.IsTrue(ws.IndexOf("Write locks = 1") >= 0);
            Assert.IsTrue(ws.IndexOf("Read locks = 0") >= 0);
            locker.WriteLock.UnLock();
            locker.ReadLock.Lock();
            locker.ReadLock.Lock();
            String rs = locker.ToString();
            Assert.IsTrue(rs.IndexOf("Write locks = 0") >= 0);
            Assert.IsTrue(rs.IndexOf("Read locks = 2") >= 0);
        }
    
        [Test]
        public void TestReadLockToString()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            String us = locker.ReadLock.ToString();
            Assert.IsTrue(us.IndexOf("Read locks = 0") >= 0);
            locker.ReadLock.Lock();
            locker.ReadLock.Lock();
            String rs = locker.ReadLock.ToString();
            Assert.IsTrue(rs.IndexOf("Read locks = 2") >= 0);
        }
    
        [Test]
        public void TestWriteLockToString()
        {
            ReentrantReadWriteLock locker = new ReentrantReadWriteLock();
            String us = locker.WriteLock.ToString();
            Assert.IsTrue(us.IndexOf("Unlocked") >= 0);
            locker.WriteLock.Lock();
            String ls = locker.WriteLock.ToString();
            Assert.IsTrue(ls.IndexOf("Locked") >= 0);
        }
    }
}

