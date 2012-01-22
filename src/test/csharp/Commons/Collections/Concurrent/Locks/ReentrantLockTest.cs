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
    public class ReentrantLockTest : ConcurrencyTestCase
    {
        private void InterruptibleLockRunnable(object state)
        {
            ReentrantLock locker = state as ReentrantLock;

            try
            {
                locker.LockInterruptibly();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        private void InterruptedLockRunnable(object state)
        {
            ReentrantLock locker = state as ReentrantLock;
            try
            {
                locker.LockInterruptibly();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        private class PublicReentrantLock : ReentrantLock
        {
            public PublicReentrantLock() : base()
            {
            }

            public Collection<Thread> GetAtQueuedThreads()
            {
                return base.QueuedThreads;
            }

            public Collection<Thread> GetAtWaitingThreads(Condition c)
            {
                return base.GetWaitingThreads(c);
            }
        }

        [Test]
        public void TestConstructor()
        {
            ReentrantLock rl = new ReentrantLock();
            Assert.IsFalse(rl.IsFair);
            ReentrantLock r2 = new ReentrantLock(true);
            Assert.True(r2.IsFair);
        }

        [Test]
        public void TestLock()
        {
            ReentrantLock rl = new ReentrantLock();
            rl.Lock();
            Assert.IsTrue(rl.IsLocked);
            rl.UnLock();
        }

        [Test]
        public void TestFairLock()
        {
            ReentrantLock rl = new ReentrantLock(true);
            rl.Lock();
            Assert.IsTrue(rl.IsLocked);
            rl.UnLock();
        }

        [Test]
        public void TestUnlockThreadStateException()
        {
            ReentrantLock rl = new ReentrantLock();
            try
            {
                rl.UnLock();
                ShouldThrow();
            }
            catch(ThreadStateException)
            {
            }
        }

        [Test]
        public void TestTryLock()
        {
            ReentrantLock rl = new ReentrantLock();
            Assert.IsTrue(rl.TryLock());
            Assert.IsTrue(rl.IsLocked);
            rl.UnLock();
        }

        [Test]
        public void TestHasQueuedThreads()
        {
            ReentrantLock locker = new ReentrantLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsFalse(locker.HasQueuedThreads);
                locker.Lock();
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.HasQueuedThreads);
                locker.UnLock();
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
        public void TestGetQueueLength()
        {
            ReentrantLock locker = new ReentrantLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.AreEqual(0, locker.QueueLength);
                locker.Lock();
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(2, locker.QueueLength);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                locker.UnLock();
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
        public void TestGetQueueLengthFair()
        {
            ReentrantLock locker = new ReentrantLock(true);
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);
            try
            {
                Assert.AreEqual(0, locker.QueueLength);
                locker.Lock();
                t1.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                t2.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(2, locker.QueueLength);
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.AreEqual(1, locker.QueueLength);
                locker.UnLock();
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
        public void TestHasQueuedThreadNRE()
        {
            ReentrantLock sync = new ReentrantLock();
            try
            {
                sync.HasQueuedThread(null);
                ShouldThrow();
            }
            catch (NullReferenceException)
            {
            }
        }

        public void TestHasQueuedThread()
        {
            ReentrantLock sync = new ReentrantLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsFalse(sync.HasQueuedThread(t1));
                Assert.IsFalse(sync.HasQueuedThread(t2));
                sync.Lock();
                t1.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThread(t1));
                t2.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(sync.HasQueuedThread(t1));
                Assert.IsTrue(sync.HasQueuedThread(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(sync.HasQueuedThread(t1));
                Assert.IsTrue(sync.HasQueuedThread(t2));
                sync.UnLock();
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

        public void TestGetQueuedThreads()
        {
            PublicReentrantLock locker = new PublicReentrantLock();
            Thread t1 = new Thread(InterruptedLockRunnable);
            Thread t2 = new Thread(InterruptibleLockRunnable);

            try
            {
                Assert.IsTrue(locker.GetAtQueuedThreads().IsEmpty());
                locker.Lock();
                Assert.IsTrue(locker.GetAtQueuedThreads().IsEmpty());
                t1.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.GetAtQueuedThreads().Contains(t1));
                t2.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.GetAtQueuedThreads().Contains(t1));
                Assert.IsTrue(locker.GetAtQueuedThreads().Contains(t2));
                t1.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsFalse(locker.GetAtQueuedThreads().Contains(t1));
                Assert.IsTrue(locker.GetAtQueuedThreads().Contains(t2));
                locker.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.GetAtQueuedThreads().IsEmpty());
                t1.Join();
                t2.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestInterruptedException2Runnable(Object state)
        {
            ReentrantLock locker = state as ReentrantLock;
            try
            {
                locker.TryLock(MEDIUM_DELAY_MS);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestInterruptedException2()
        {
            ReentrantLock locker = new ReentrantLock();
            locker.Lock();
            Thread t = new Thread(TestInterruptedException2Runnable);

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

        private void TestTryLockWhenLockedRunnable(Object state)
        {
            ReentrantLock locker = state as ReentrantLock;
            ThreadAssertFalse(locker.TryLock());
        }

        [Test]
        public void TestTryLockWhenLocked()
        {
            ReentrantLock locker = new ReentrantLock();
            locker.Lock();
            Thread t = new Thread(TestTryLockWhenLockedRunnable);

            try
            {
                t.Start(locker);
                t.Join();
                locker.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestTryLockTimeoutRunnable(Object state)
        {
            ReentrantLock locker = state as ReentrantLock;

            try
            {
                ThreadAssertFalse(locker.TryLock(1));
            }
            catch (Exception e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestTryLockTimeout()
        {
            ReentrantLock locker = new ReentrantLock();
            locker.Lock();
            Thread t = new Thread(TestTryLockTimeoutRunnable);

            try
            {
                t.Start(locker);
                t.Join();
                locker.UnLock();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestGetHoldCount()
        {
            ReentrantLock locker = new ReentrantLock();
            for(int i = 1; i <= SIZE; i++)
            {
                locker.Lock();
                Assert.AreEqual(i,locker.HoldCount);
            }

            for(int i = SIZE; i > 0; i--)
            {
                locker.UnLock();
                Assert.AreEqual(i-1,locker.HoldCount);
            }
        }

        private void TestIsLockedRunnable(Object state)
        {
            ReentrantLock locker = state as ReentrantLock;
            locker.Lock();
            try
            {
                Thread.Sleep(SMALL_DELAY_MS);
            }
            catch(Exception e)
            {
                ThreadUnexpectedException(e);
            }
            locker.UnLock();
        }

        [Test]
        public void TestIsLocked()
        {
            ReentrantLock locker = new ReentrantLock();
            locker.Lock();
            Assert.IsTrue(locker.IsLocked);
            locker.UnLock();
            Assert.IsFalse(locker.IsLocked);
            Thread t = new Thread(TestIsLockedRunnable);

            try
            {
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                Assert.IsTrue(locker.IsLocked);
                t.Join();
                Assert.IsFalse(locker.IsLocked);
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestLockInterruptibly1()
        {
            ReentrantLock locker = new ReentrantLock();
            locker.Lock();
            Thread t = new Thread(InterruptedLockRunnable);
            try
            {
                t.Start(locker);
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.UnLock();
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestLockInterruptibly2()
        {
            ReentrantLock locker = new ReentrantLock();
            try
            {
                locker.LockInterruptibly();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }

            Thread t = new Thread(InterruptedLockRunnable);

            try
            {
                t.Start(locker);
                t.Interrupt();
                Assert.IsTrue(locker.IsLocked);
                Assert.IsTrue(locker.IsHeldByCurrentThread);
                t.Join();
            }
            catch(Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAwaitIllegalMonitor()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
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
        public void TestSignalIllegalMonitor()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
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
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            try
            {
                locker.Lock();
                long t = c.Await(1);
                Assert.IsTrue(t <= 0);
                locker.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAwaitTimeout()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            try
            {
                locker.Lock();
                c.Await(SHORT_DELAY_MS);
                locker.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestAwaitUntilTimeout()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            try
            {
                locker.Lock();
                c.AwaitUntil(DateTime.Now.AddMilliseconds(100));
                locker.UnLock();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestAwaitRunnable(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAwait()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);
            Thread t = new Thread(TestAwaitRunnable);

            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                c.Signal();
                locker.UnLock();
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
            ReentrantLock locker = new ReentrantLock();
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
            ReentrantLock locker = new ReentrantLock();
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
            PublicReentrantLock locker = new PublicReentrantLock();
            try
            {
                locker.GetWaitingThreads(null);
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
        public void TestHasWaitersIAE()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = (locker.NewCondition());
            ReentrantLock locker2 = new ReentrantLock();
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
            ReentrantLock locker = new ReentrantLock();
            Condition c = (locker.NewCondition());
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
            ReentrantLock locker = new ReentrantLock();
            Condition c = (locker.NewCondition());
            ReentrantLock locker2 = new ReentrantLock();
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
            ReentrantLock locker = new ReentrantLock();
            Condition c = (locker.NewCondition());
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
        public void TestGetWaitingThreadsAE()
        {
            PublicReentrantLock locker = new PublicReentrantLock();
            Condition c = (locker.NewCondition());
            PublicReentrantLock locker2 = new PublicReentrantLock();
            try
            {
                locker2.GetWaitingThreads(c);
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
            PublicReentrantLock locker = new PublicReentrantLock();
            Condition c = (locker.NewCondition());
            try
            {
                locker.GetWaitingThreads(c);
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
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertFalse(locker.HasWaiters(c));
                ThreadAssertEquals(0, locker.GetWaitQueueLength(c));
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestHasWaiters()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t = new Thread(TestHasWaitersRunnable);
    
            try
            {
                t.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.AreEqual(1, locker.GetWaitQueueLength(c));
                c.Signal();
                locker.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.AreEqual(0, locker.GetWaitQueueLength(c));
                locker.UnLock();
                t.Join(SHORT_DELAY_MS);
                Assert.IsFalse(t.IsAlive);
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestGetWaitQueueLengthRunnable1(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertFalse(locker.HasWaiters(c));
                ThreadAssertEquals(0, locker.GetWaitQueueLength(c));
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestGetWaitQueueLengthRunnable2(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertTrue(locker.HasWaiters(c));
                ThreadAssertEquals(1, locker.GetWaitQueueLength(c));
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitQueueLength()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t1 = new Thread(TestGetWaitQueueLengthRunnable1);
            Thread t2 = new Thread(TestGetWaitQueueLengthRunnable2);
    
            try
            {
                t1.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.AreEqual(2, locker.GetWaitQueueLength(c));
                c.SignalAll();
                locker.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.AreEqual(0, locker.GetWaitQueueLength(c));
                locker.UnLock();
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

        private void TestGetWaitingThreadsRunnable1(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertTrue(locker.GetWaitingThreads(c).IsEmpty());
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestGetWaitingThreadsRunnable2(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertFalse(locker.GetWaitingThreads(c).IsEmpty());
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestGetWaitingThreads()
        {
            PublicReentrantLock locker = new PublicReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t1 = new Thread(TestGetWaitingThreadsRunnable1);
            Thread t2 = new Thread(TestGetWaitingThreadsRunnable2);
    
            try
            {
                locker.Lock();
                Assert.IsTrue(locker.GetWaitingThreads(c).IsEmpty());
                locker.UnLock();
                t1.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsTrue(locker.HasWaiters(c));
                Assert.IsTrue(locker.GetWaitingThreads(c).Contains(t1));
                Assert.IsTrue(locker.GetWaitingThreads(c).Contains(t2));
                c.SignalAll();
                locker.UnLock();
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                Assert.IsFalse(locker.HasWaiters(c));
                Assert.IsTrue(locker.GetWaitingThreads(c).IsEmpty());
                locker.UnLock();
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

//        /** A helper class for uninterruptible wait tests */
//        class UninterruptableThread
//        {
//            private ReentrantLock locker;
//            private Condition c;
//
//            public volatile bool canAwake = false;
//            public volatile bool interrupted = false;
//            public volatile bool lockStarted = false;
//
//            public UninterruptableThread(ReentrantLock locker, Condition c)
//            {
//                this.locker = locker;
//                this.c = c;
//            }
//
//            //synchronized
//            public void run()
//            {
//                locker.Lock();
//                lockStarted = true;
//
//                while (!canAwake)
//                {
//                    c.AwaitUnInterruptibly();
//                }
//
//                //interrupted = Thread.IsInterrupted;
//                locker.UnLock();
//            }
//        }
//
//        public void TestAwaitUninterruptibly()
//        {
//            ReentrantLock locker = new ReentrantLock();
//            Condition c = locker.NewCondition();
//            UninterruptableThread thread = new UninterruptableThread(lock, c);
//
//            try {
//                thread.Start();
//
//                while (!thread.lockStarted) {
//                    Thread.Sleep(100);
//                }
//
//                locker.Lock();
//                try {
//                    thread.Interrupt();
//                    thread.canAwake = true;
//                    c.Signal();
//                } finally {
//                    locker.UnLock();
//                }
//
//                thread.Join();
//                Assert.IsTrue(thread.interrupted);
//                Assert.IsFalse(thread.IsAlive);
//            } catch (Exception ex) {
//                UnexpectedException();
//            }
//        }

        private void TestAwaitInterruptRunnable(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.Await();
                locker.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitInterrupt()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);
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

        private void TestAwaitTimedInterruptRunnable(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.Await(1000); // 1 sec
                locker.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitTimedInterrupt()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);
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

        private void TestAwaitUntilInterruptRunnable(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.AwaitUntil(DateTime.Now.AddMilliseconds(10000));
                locker.UnLock();
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
            }
        }

        [Test]
        public void TestAwaitUntilInterrupt()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);
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

        private void TestSignalAllRunnable1(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestSignalAllRunnable2(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                c.Await();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestSignalAll()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t1 = new Thread(TestSignalAllRunnable1);
            Thread t2 = new Thread(TestSignalAllRunnable2);
    
            try
            {
                t1.Start(data);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                c.SignalAll();
                locker.UnLock();
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

        private void TestAwaitLockCountRunnable1(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                ThreadAssertEquals(1, locker.HoldCount);
                c.Await();
                ThreadAssertEquals(1, locker.HoldCount);
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        private void TestAwaitLockCountRunnable2(Object state)
        {
            Pair data = state as Pair;
            ReentrantLock locker = data.first as ReentrantLock;
            Condition c = data.second as Condition;

            try
            {
                locker.Lock();
                locker.Lock();
                ThreadAssertEquals(2, locker.HoldCount);
                c.Await();
                ThreadAssertEquals(2, locker.HoldCount);
                locker.UnLock();
                locker.UnLock();
            }
            catch(ThreadInterruptedException e)
            {
                ThreadUnexpectedException(e);
            }
        }

        [Test]
        public void TestAwaitLockCount()
        {
            ReentrantLock locker = new ReentrantLock();
            Condition c = locker.NewCondition();
            Pair data = new Pair(locker, c);

            Thread t1 = new Thread(TestAwaitLockCountRunnable1);
            Thread t2 = new Thread(TestAwaitLockCountRunnable2);

            try
            {
                t1.Start(data);
                t2.Start(data);
                Thread.Sleep(SHORT_DELAY_MS);
                locker.Lock();
                c.SignalAll();
                locker.UnLock();
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
            ReentrantLock locker = new ReentrantLock();
            String us = locker.ToString();
            Assert.IsTrue(us.IndexOf("Unlocked") >= 0);
            locker.Lock();
            String ls = locker.ToString();
            Assert.IsTrue(ls.IndexOf("Locked") >= 0);
        }
    }
}

