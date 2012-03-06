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
    public class AbstractQueuedSynchronizerTest : ConcurrencyTestCase
    {
        internal class Mutex : AbstractQueuedSynchronizer
        {
            protected override bool IsHeldExclusively()
            {
                return State == 1;
            }

            public bool AccessIsHeldExclusively()
            {
                return IsHeldExclusively();
            }

            protected override bool TryAcquire(int acquires)
            {
                Assert.IsTrue(acquires == 1);
                return CompareAndSetState(0, 1);
            }

            public bool AccessTryAcquire(int acquires)
            {
                return TryAcquire(acquires);
            }

            protected override bool TryRelease(int releases)
            {
                if (State == 0)
                {
                    throw new ThreadStateException();
                }
                State = 0;
                return true;
            }
    
            public AbstractQueuedSynchronizer.ConditionObject NewCondition()
            {
                return new AbstractQueuedSynchronizer.ConditionObject(this);
            }
        }

        private class BooleanLatch : AbstractQueuedSynchronizer
        {
            public bool IsSignalled()
            {
                return State != 0;
            }

            protected override int TryAcquireShared(int ignore)
            {
                return IsSignalled() ? 1 : -1;
            }

            protected override bool TryReleaseShared(int ignore)
            {
                State = 1;
                return true;
            }
        }

        private void InterruptibleSyncRunnable(Object state)
        {
            try
            {
                Mutex sync = state as Mutex;
                sync.AcquireInterruptibly(1);
            }
            catch(ThreadInterruptedException)
            {
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void InterruptedSyncRunnable(Object state)
        {
            try
            {
                Mutex sync = state as Mutex;
                sync.AcquireInterruptibly(1);
                ThreadShouldThrow("InterruptedSyncRunnable should have been interrupted in Acquire");
            }
            catch(ThreadInterruptedException)
            {
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void TestIsHeldExclusively()
        {
            Mutex rl = new Mutex();
            Assert.IsFalse(rl.AccessIsHeldExclusively());
        }

        [Test]
        public void TestAcquire()
        {
            Mutex rl = new Mutex();
            rl.Acquire(1);
            Assert.IsTrue(rl.AccessIsHeldExclusively());
            rl.Release(1);
            Assert.IsFalse(rl.AccessIsHeldExclusively());
        }

        [Test]
        public void TestTryAcquire()
        {
            Mutex rl = new Mutex();
            Assert.IsTrue(rl.AccessTryAcquire(1));
            Assert.IsTrue(rl.AccessIsHeldExclusively());
            rl.Release(1);
        }

        [Test]
        public void TestHasQueuedThreads()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsFalse(sync.HasQueuedThreads);
                sync.Acquire(1);
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThreads);
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThreads);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThreads);
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.HasQueuedThreads);
                t1.Join();
                t2.Join();
            }
            catch(AssertionException)
            {
                throw;
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestIsQueuedNPE()
        {
            Mutex sync = new Mutex();
            try
            {
                sync.IsQueued(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
        }

        [Test]
        public void TestIsQueued()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsFalse(sync.IsQueued(t1));
                Assert.IsFalse(sync.IsQueued(t2));
                sync.Acquire(1);
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.IsQueued(t1));
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.IsQueued(t1));
                Assert.IsTrue(sync.IsQueued(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.IsQueued(t1));
                Assert.IsTrue(sync.IsQueued(t2));
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.IsQueued(t1));
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.IsQueued(t2));
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestGetFirstQueuedThread()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsNull(sync.FirstQueuedThread);
                sync.Acquire(1);
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(t1, sync.FirstQueuedThread);
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(t1, sync.FirstQueuedThread);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(t2, sync.FirstQueuedThread);
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsNull(sync.FirstQueuedThread);
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestHasContended()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsFalse(sync.HasContended);
                sync.Acquire(1);
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasContended);
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasContended);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasContended);
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasContended);
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
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsTrue(sync.QueuedThreads.IsEmpty());
                sync.Acquire(1);
                Assert.IsTrue(sync.QueuedThreads.IsEmpty());
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.QueuedThreads.Contains(t1));
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.QueuedThreads.Contains(t1));
                Assert.IsTrue(sync.QueuedThreads.Contains(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.QueuedThreads.Contains(t1));
                Assert.IsTrue(sync.QueuedThreads.Contains(t2));
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.QueuedThreads.IsEmpty());
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        } 
    
        [Test]
        public void TestGetExclusiveQueuedThreads()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try
            {
                Assert.IsTrue(sync.ExclusiveQueuedThreads.IsEmpty());
                sync.Acquire(1);
                Assert.IsTrue(sync.ExclusiveQueuedThreads.IsEmpty());
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.ExclusiveQueuedThreads.Contains(t1));
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.ExclusiveQueuedThreads.Contains(t1));
                Assert.IsTrue(sync.ExclusiveQueuedThreads.Contains(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.ExclusiveQueuedThreads.Contains(t1));
                Assert.IsTrue(sync.ExclusiveQueuedThreads.Contains(t2));
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.ExclusiveQueuedThreads.IsEmpty());
                t1.Join();
                t2.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        } 

        [Test]
        public void TestGetSharedQueuedThreads()
        {
            Mutex sync = new Mutex();
            Thread t1 = new Thread(InterruptedSyncRunnable);
            Thread t2 = new Thread(InterruptibleSyncRunnable);
            try {
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                sync.Acquire(1);
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                t1.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                t2.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.SharedQueuedThreads.IsEmpty());
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestThreadInterruptedException2Rannable(Object state)
        {
            try
            {
                Mutex sync = state as Mutex;
                sync.TryAcquire(1, MEDIUM_DELAY_MS);
                ThreadShouldThrow("TestThreadInterruptedException2Rannable should have been interrupted");
            }
            catch(ThreadInterruptedException)
            {
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestThreadInterruptedException2()
        {
            Mutex sync = new Mutex();
            sync.Acquire(1);
            Thread t = new Thread(TestThreadInterruptedException2Rannable);

            try
            {
                t.Start(sync);
                t.Interrupt();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestTryAcquireWhenSyncedRunnable(object state)
        {
            Mutex sync = state as Mutex;
            ThreadAssertFalse(sync.AccessTryAcquire(1));
        }

        [Test]
        public void TestTryAcquireWhenSynced()
        {
            Mutex sync = new Mutex();
            sync.Acquire(1);
            Thread t = new Thread(TestTryAcquireWhenSyncedRunnable);

            try
            {
                t.Start(sync);
                t.Join();
                sync.Release(1);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAcquireTimedTimeoutRunnable(object state)
        {
            Mutex sync = state as Mutex;

            try
            {
                ThreadAssertFalse(sync.TryAcquire(1, 1000));
            }
            catch (Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireTimedTimeout()
        {
            Mutex sync = new Mutex();
            sync.Acquire(1);
            Thread t = new Thread(TestAcquireTimedTimeoutRunnable);

            try
            {
                t.Start(sync);
                t.Join();
                sync.Release(1);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestGetStateRunnable(object state)
        {
            Mutex sync = state as Mutex;

            sync.Acquire(1);
            try
            {
                Thread.Sleep(SMALL_DELAY_MS);
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
            sync.Release(1);
        }

        [Test]
        public void TestGetState()
        {
            Mutex sync = new Mutex();
            sync.Acquire(1);
            Assert.IsTrue(sync.AccessIsHeldExclusively());
            sync.Release(1);
            Assert.IsFalse(sync.AccessIsHeldExclusively());
            Thread t = new Thread(TestGetStateRunnable);

            try
            {
                t.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.AccessIsHeldExclusively());
                t.Join();
                Assert.IsFalse(sync.AccessIsHeldExclusively());
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireInterruptibly1()
        {
            Mutex sync = new Mutex();
            sync.Acquire(1);
            Thread t = new Thread(InterruptedSyncRunnable);

            try
            {
                t.Start(sync);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Release(1);
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireInterruptibly2()
        {
            Mutex sync = new Mutex();
            try
            {
                sync.AcquireInterruptibly(1);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }

            Thread t = new Thread(InterruptedSyncRunnable);
            try
            {
                t.Start(sync);
                t.Interrupt();
                Assert.IsTrue(sync.AccessIsHeldExclusively());
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestOwns()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Mutex sync2 = new Mutex();
            Assert.IsTrue(sync.Owns(c));
            Assert.IsFalse(sync2.Owns(c));
        }

        [Test]
        public void TestAwaitThreadStateException()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            try
            {
                c.Await();
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
        public void TestSignalThreadStateException()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
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
        public void TestAwaitTimeout()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            try
            {
                sync.Acquire(1);
                Assert.IsFalse(c.Await(SHORT_DELAY_MS) > 0);
                sync.Release(1);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAwaitUntilTimeout()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();

            try
            {
                sync.Acquire(1);
                DateTime deadline = DateTime.Now;
                Assert.IsFalse(c.AwaitUntil(deadline.AddSeconds(5)));
                sync.Release(1);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitRunnable(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAwait()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);
            Thread t = new Thread(TestAwaitRunnable);

            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                c.Signal();
                sync.Release(1);
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestHasWaitersNRE()
        {
            Mutex sync = new Mutex();
            try
            {
                sync.HasWaiters(null);
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
            Mutex sync = new Mutex();
            try
            {
                sync.GetWaitQueueLength(null);
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
            Mutex sync = new Mutex();
            try
            {
                sync.GetWaitingThreads(null);
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
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());
            Mutex sync2 = new Mutex();

            try
            {
                sync2.HasWaiters(c);
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
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());

            try
            {
                sync.HasWaiters(c);
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
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());
            Mutex sync2 = new Mutex();

            try
            {
                sync2.GetWaitQueueLength(c);
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
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());
            try
            {
                sync.GetWaitQueueLength(c);
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
        public void TestGetWaitingThreadsAE()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());
            Mutex sync2 = new Mutex();

            try
            {
                sync2.GetWaitingThreads(c);
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
        public void TestGetWaitingThreadsTSE()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = (sync.NewCondition());

            try
            {
                sync.GetWaitingThreads(c);
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

        private void TestHasWaitersRunnable(Object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c = data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                ThreadAssertFalse(sync.HasWaiters(c));
                ThreadAssertEquals(0, sync.GetWaitQueueLength(c));
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestHasWaiters()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);
            Thread t = new Thread(TestHasWaitersRunnable);

            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsTrue(sync.HasWaiters(c));
                Assert.AreEqual(1, sync.GetWaitQueueLength(c));
                c.Signal();
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsFalse(sync.HasWaiters(c));
                Assert.AreEqual(0, sync.GetWaitQueueLength(c));
                sync.Release(1);
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception)
            {
                UnexpectedException();
            }
        }

        private void TestGetWaitQueueLengthRunnable1(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c = data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                ThreadAssertFalse(sync.HasWaiters(c));
                ThreadAssertEquals(0, sync.GetWaitQueueLength(c));
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestGetWaitQueueLengthRunnable2(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c = data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                ThreadAssertTrue(sync.HasWaiters(c));
                ThreadAssertEquals(1, sync.GetWaitQueueLength(c));
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitQueueLength()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t1 = new Thread(TestGetWaitQueueLengthRunnable1);
            Thread t2 = new Thread(TestGetWaitQueueLengthRunnable2);

            try
            {
                t1.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsTrue(sync.HasWaiters(c));
                Assert.AreEqual(2, sync.GetWaitQueueLength(c));
                c.SignalAll();
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsFalse(sync.HasWaiters(c));
                Assert.AreEqual(0, sync.GetWaitQueueLength(c));
                sync.Release(1);
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

        private void TestGetWaitingThreadsRunnable1(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                ThreadAssertTrue(sync.GetWaitingThreads(c).IsEmpty());
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException)
            {
                ThreadUnexpectedException();
            }
        }

        private void TestGetWaitingThreadsRunnable2(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                ThreadAssertFalse(sync.GetWaitingThreads(c).IsEmpty());
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitingThreads()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t1 = new Thread(TestGetWaitingThreadsRunnable1);
            Thread t2 = new Thread(TestGetWaitingThreadsRunnable2);

            try
            {
                sync.Acquire(1);
                Assert.IsTrue(sync.GetWaitingThreads(c).IsEmpty());
                sync.Release(1);
                t1.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsTrue(sync.HasWaiters(c));
                Assert.IsTrue(sync.GetWaitingThreads(c).Contains(t1));
                Assert.IsTrue(sync.GetWaitingThreads(c).Contains(t2));
                c.SignalAll();
                sync.Release(1);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                Assert.IsFalse(sync.HasWaiters(c));
                Assert.IsTrue(sync.GetWaitingThreads(c).IsEmpty());
                sync.Release(1);
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

        private void TestAwaitUnInterruptiblyRunnable(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            sync.Acquire(1);
            c.AwaitUnInterruptibly();
            sync.Release(1);
        }

        [Test]
        public void TestAwaitUnInterruptibly()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t = new Thread(TestAwaitUnInterruptiblyRunnable);

            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                sync.Acquire(1);
                c.Signal();
                sync.Release(1);
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitInterruptRunnable(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.Await();
                sync.Release(1);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitInterrupt()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);
            Thread t = new Thread(TestAwaitInterruptRunnable);

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

        private void TestAwaitTimedInterruptRunnable(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.Await(1000); // 1 sec
                sync.Release(1);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitTimedInterrupt()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t = new Thread(TestAwaitTimedInterruptRunnable);

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

        private void TestAwaitUntilInterruptRunnable(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.AwaitUntil(DateTime.Now.AddMilliseconds(10000));
                sync.Release(1);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitUntilInterrupt()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t = new Thread(TestAwaitUntilInterruptRunnable);
    
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

        private void TestSignalAllRunnable1(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestSignalAllRunnable2(object state)
        {
            Pair data = state as Pair;
            Mutex sync = data.first as Mutex;
            AbstractQueuedSynchronizer.ConditionObject c =
                data.second as AbstractQueuedSynchronizer.ConditionObject;

            try
            {
                sync.Acquire(1);
                c.Await();
                sync.Release(1);
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestSignalAll()
        {
            Mutex sync = new Mutex();
            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
            Pair data = new Pair(sync, c);

            Thread t1 = new Thread(TestSignalAllRunnable1);
            Thread t2 = new Thread(TestSignalAllRunnable2);

            try
            {
                t1.Start(data);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                sync.Acquire(1);
                c.SignalAll();
                sync.Release(1);
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
            Mutex sync = new Mutex();
            String us = sync.ToString();
            Assert.IsTrue(us.IndexOf("State = 0") >= 0);
            sync.Acquire(1);
            String ls = sync.ToString();
            Assert.IsTrue(ls.IndexOf("State = 1") >= 0);
        }

        [Test]
        public void TestGetStateWithReleaseShared()
        {
            BooleanLatch l = new BooleanLatch();
            Assert.IsFalse(l.IsSignalled());
            l.ReleaseShared(0);
            Assert.IsTrue(l.IsSignalled());
        }

        [Test]
        public void TestReleaseShared()
        {
            BooleanLatch l = new BooleanLatch();
            Assert.IsFalse(l.IsSignalled());
            l.ReleaseShared(0);
            Assert.IsTrue(l.IsSignalled());
            l.ReleaseShared(0);
            Assert.IsTrue(l.IsSignalled());
        }

        private void TestAcquireSharedInterruptiblyRannable(Object state)
        {
            BooleanLatch l = state as BooleanLatch;

            try
            {
                ThreadAssertFalse(l.IsSignalled());
                l.AcquireSharedInterruptibly(0);
                ThreadAssertTrue(l.IsSignalled());
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireSharedInterruptibly()
        {
            BooleanLatch l = new BooleanLatch();

            Thread t = new Thread(TestAcquireSharedInterruptiblyRannable);

            try
            {
                t.Start(l);
                Assert.IsFalse(l.IsSignalled());
                Thread.Sleep(SHORT_DELAY_MS);
                l.ReleaseShared(0);
                Assert.IsTrue(l.IsSignalled());
                t.Join();
            }
            catch (ThreadInterruptedException e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAsquireSharedTimedRannable(Object state)
        {
            BooleanLatch l = state as BooleanLatch;

            try
            {
                ThreadAssertFalse(l.IsSignalled());
                ThreadAssertTrue(l.TryAcquireShared(0, MEDIUM_DELAY_MS));
                ThreadAssertTrue(l.IsSignalled());
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAsquireSharedTimed()
        {
            BooleanLatch l = new BooleanLatch();

            Thread t = new Thread(TestAsquireSharedTimedRannable);
            try
            {
                t.Start(l);
                Assert.IsFalse(l.IsSignalled());
                Thread.Sleep(SHORT_DELAY_MS);
                l.ReleaseShared(0);
                Assert.IsTrue(l.IsSignalled());
                t.Join();
            }
            catch (ThreadInterruptedException e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAcquireSharedInterruptiblyInterruptedExceptionRannable(Object state)
        {
            BooleanLatch l = state as BooleanLatch;

            try
            {
                ThreadAssertFalse(l.IsSignalled());
                l.AcquireSharedInterruptibly(0);
                ThreadAssertTrue(l.IsSignalled());
            }
            catch(ThreadInterruptedException)
            {
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireSharedInterruptiblyInterruptedException()
        {
            BooleanLatch l = new BooleanLatch();

            Thread t = new Thread(TestAcquireSharedInterruptiblyInterruptedExceptionRannable);

            t.Start(l);

            try
            {
                Assert.IsFalse(l.IsSignalled());
                t.Interrupt();
                t.Join();
            }
            catch (ThreadInterruptedException)
            {
                UnexpectedException();
            }
        }

        private void TestAcquireSharedTimedInterruptedExceptionRannable(Object state)
        {
            BooleanLatch l = state as BooleanLatch;

            try
            {
                ThreadAssertFalse(l.IsSignalled());
                l.TryAcquireShared(0, SMALL_DELAY_MS);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireSharedTimedInterruptedException()
        {
            BooleanLatch l = new BooleanLatch();
            Thread t = new Thread(TestAcquireSharedTimedInterruptedExceptionRannable);
            t.Start(l);

            try
            {
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(l.IsSignalled());
                t.Interrupt();
                t.Join();
            }
            catch (ThreadInterruptedException e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAcquireSharedTimedTimeoutRannable(Object state)
        {
            BooleanLatch l = state as BooleanLatch;

            try
            {
                ThreadAssertFalse(l.IsSignalled());
                ThreadAssertFalse(l.TryAcquireShared(0, SMALL_DELAY_MS));
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAcquireSharedTimedTimeout()
        {
            BooleanLatch l = new BooleanLatch();
            Thread t = new Thread(TestAcquireSharedTimedTimeoutRannable);
            t.Start(l);

            try
            {
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(l.IsSignalled());
                t.Join();
            }
            catch (ThreadInterruptedException e)
            {
                UnexpectedException(e);
            }
        }
    }
}

