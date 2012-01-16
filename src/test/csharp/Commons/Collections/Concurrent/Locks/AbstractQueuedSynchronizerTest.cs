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

            public new int TryAcquireShared(int ignore)
            {
                return IsSignalled() ? 1 : -1;
            }

            public new bool TryReleaseShared(int ignore)
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
        }

        private void InterruptedSyncRunnable(Object state)
        {
            try
            {
                Mutex sync = state as Mutex;
                sync.AcquireInterruptibly(1);
                ThreadShouldThrow();
            }
            catch(ThreadInterruptedException)
            {
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

//        [Test]
//        public void TestIsQueued()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(InterruptedSyncRunnable);
//            Thread t2 = new Thread(InterruptibleSyncRunnable);
//            try
//            {
//                Assert.IsFalse(sync.IsQueued(t1));
//                Assert.IsFalse(sync.IsQueued(t2));
//                sync.Acquire(1);
//                t1.Start(sync);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.IsQueued(t1));
//                t2.Start(sync);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.IsQueued(t1));
//                Assert.IsTrue(sync.IsQueued(t2));
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(sync.IsQueued(t1));
//                Assert.IsTrue(sync.IsQueued(t2));
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(sync.IsQueued(t1));
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(sync.IsQueued(t2));
//                t1.Join();
//                t2.Join();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestGetFirstQueuedThread()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(new InterruptedSyncRunnable(sync));
//            Thread t2 = new Thread(new InterruptibleSyncRunnable(sync));
//            try
//            {
//                Assert.IsNull(sync.getFirstQueuedThread());
//                sync.Acquire(1);
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.AreEqual(t1, sync.getFirstQueuedThread());
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.AreEqual(t1, sync.getFirstQueuedThread());
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.AreEqual(t2, sync.getFirstQueuedThread());
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsNull(sync.getFirstQueuedThread());
//                t1.Join();
//                t2.Join();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestHasContended()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(new InterruptedSyncRunnable(sync));
//            Thread t2 = new Thread(new InterruptibleSyncRunnable(sync));
//            try
//            {
//                Assert.IsFalse(sync.hasContended());
//                sync.Acquire(1);
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.hasContended());
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.hasContended());
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.hasContended());
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.hasContended());
//                t1.Join();
//                t2.Join();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestGetQueuedThreads()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(new InterruptedSyncRunnable(sync));
//            Thread t2 = new Thread(new InterruptibleSyncRunnable(sync));
//            try
//            {
//                Assert.IsTrue(sync.getQueuedThreads().IsEmpty());
//                sync.Acquire(1);
//                Assert.IsTrue(sync.getQueuedThreads().IsEmpty());
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getQueuedThreads().Contains(t1));
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getQueuedThreads().Contains(t1));
//                Assert.IsTrue(sync.getQueuedThreads().Contains(t2));
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(sync.getQueuedThreads().Contains(t1));
//                Assert.IsTrue(sync.getQueuedThreads().Contains(t2));
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getQueuedThreads().IsEmpty());
//                t1.Join();
//                t2.Join();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        } 
//    
//        [Test]
//        public void TestGetExclusiveQueuedThreads()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(new InterruptedSyncRunnable(sync));
//            Thread t2 = new Thread(new InterruptibleSyncRunnable(sync));
//            try
//            {
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().IsEmpty());
//                sync.Acquire(1);
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().IsEmpty());
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().Contains(t1));
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().Contains(t1));
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().Contains(t2));
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(sync.getExclusiveQueuedThreads().Contains(t1));
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().Contains(t2));
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.getExclusiveQueuedThreads().IsEmpty());
//                t1.Join();
//                t2.Join();
//            }
//            catch (Exception)
//            {
//                UnexpectedException();
//            }
//        } 
//
//        [Test]
//        public void TestGetSharedQueuedThreads()
//        {
//            Mutex sync = new Mutex();
//            Thread t1 = new Thread(new InterruptedSyncRunnable(sync));
//            Thread t2 = new Thread(new InterruptibleSyncRunnable(sync));
//            try {
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                sync.Acquire(1);
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                t1.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.GetSharedQueuedThreads().IsEmpty());
//                t1.Join();
//                t2.Join();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestInterruptedException2()
//        {
//            Mutex sync = new Mutex();
//            sync.Acquire(1);
//            Thread t = new Thread(new Runnable() {
//                        public void run() {
//                            try {
//                 sync.TryAcquireNanos(1, MEDIUM_DELAY_MS * 1000 * 1000);
//                 threadShouldThrow();
//                 } catch(ThreadInterruptedException){}
//             }
//             });
//            try
//            {
//                t.Start();
//                t.Interrupt();
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestTryAcquireWhenSynced()
//        {
//            Mutex sync = new Mutex();
//            sync.Acquire(1);
//            Thread t = new Thread(new Runnable() {
//                    public void run() {
//                        ThreadAssertFalse(sync.TryAcquire(1));
//            }
//            });
//
//            try
//            {
//                t.Start();
//                t.Join();
//                sync.Release(1);
//            }
//            catch(Exception)
//            {
//                UnexpectedException();
//            }
//        } 
//
//        [Test]
//        public void TestAcquireNanosTimeout()
//        {
//            Mutex sync = new Mutex();
//            sync.Acquire(1);
//            Thread t = new Thread(new Runnable() {
//                    public void run() {
//             try {
//                            ThreadAssertFalse(sync.TryAcquireNanos(1, 1000 * 1000));
//                        } catch (Exception) {
//                            ThreadUnexpectedException();
//                        }
//            }
//            });
//            try {
//                t.Start();
//                t.Join();
//                sync.Release(1);
//            } catch(Exception){
//                UnexpectedException();
//            }
//        } 
//
//        [Test]
//        public void TestGetState()
//        {
//            Mutex sync = new Mutex();
//            sync.Acquire(1);
//            Assert.IsTrue(sync.IsHeldExclusively);
//            sync.Release(1);
//            Assert.IsFalse(sync.IsHeldExclusively);
//            Thread t = new Thread(new Runnable() {
//            public void run() {
//                sync.Acquire(1);
//                try {
//                    Thread.Sleep(SMALL_DELAY_MS);
//                }
//                catch(Exception) {
//                    ThreadUnexpectedException();
//                }
//                sync.Release(1);
//            }
//            });
//
//            try
//            {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsTrue(sync.IsHeldExclusively);
//                t.Join();
//                Assert.IsFalse(sync.IsHeldExclusively);
//            } catch(Exception){
//                UnexpectedException();
//            }
//        }
//    
//        [Test]
//        public void TestAcquireInterruptibly1()
//        {
//            Mutex sync = new Mutex();
//            sync.Acquire(1);
//            Thread t = new Thread(new InterruptedSyncRunnable(sync));
//            try {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t.Interrupt();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Release(1);
//                t.Join();
//            } catch(Exception){
//                UnexpectedException();
//            }
//        } 
//    
//        [Test]
//        public void TestAcquireInterruptibly2()
//        {
//            Mutex sync = new Mutex();
//            try {
//                sync.AcquireInterruptibly(1);
//            } catch(Exception) {
//                UnexpectedException();
//            }
//            Thread t = new Thread(new InterruptedSyncRunnable(sync));
//            try {
//                t.Start();
//                t.Interrupt();
//                Assert.IsTrue(sync.IsHeldExclusively);
//                t.Join();
//            } catch(Exception){
//                UnexpectedException();
//            }
//        }

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
                Assert.IsFalse(c.Await(SHORT_DELAY_MS) == 0);
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

//        [Test]
//        public void TestAwait()
//        {
//            Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//            Thread t = new Thread(new Runnable() {
//                public void run() {
//                    try {
//                        sync.Acquire(1);
//                        c.Await();
//                        sync.Release(1);
//                    }
//                    catch(InterruptedException)
//                    {
//                        ThreadUnexpectedException();
//                    }
//                }
//            });
//
//            try
//            {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                c.Signal();
//                sync.Release(1);
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception)
//            {
//                UnexpectedException();
//            }
//        }

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

//        [Test]
//        public void TestHasWaiters()
//        {
//            Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//            Thread t = new Thread(new Runnable() {
//                public void run() {
//                    try {
//                        sync.Acquire(1);
//                        ThreadAssertFalse(sync.HasWaiters(c));
//                        threadAssertEquals(0, sync.getWaitQueueLength(c));
//                        c.Await();
//                        sync.Release(1);
//                    }
//                    catch(InterruptedException)
//                    {
//                        ThreadUnexpectedException();
//                    }
//                }
//            });
//
//            try
//            {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsTrue(sync.HasWaiters(c));
//                Assert.AreEqual(1, sync.getWaitQueueLength(c));
//                c.Signal();
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsFalse(sync.HasWaiters(c));
//                Assert.AreEqual(0, sync.getWaitQueueLength(c));
//                sync.Release(1);
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestGetWaitQueueLength()
//        {
//            Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//            Thread t1 = new Thread(new Runnable() {
//                public void run()
//                {
//                    try
//                    {
//                        sync.Acquire(1);
//                        ThreadAssertFalse(sync.HasWaiters(c));
//                        threadAssertEquals(0, sync.getWaitQueueLength(c));
//                        c.Await();
//                        sync.Release(1);
//                    }
//                    catch(InterruptedException)
//                    {
//                        ThreadUnexpectedException();
//                    }
//                }
//         });
//    
//        Thread t2 = new Thread(new Runnable()
//                                  {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            threadAssertTrue(sync.HasWaiters(c));
//                            threadAssertEquals(1, sync.getWaitQueueLength(c));
//                            c.Await();
//                            sync.Release(1);
//             }
//             catch(InterruptedException) {
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//
//            try {
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsTrue(sync.HasWaiters(c));
//                Assert.AreEqual(2, sync.getWaitQueueLength(c));
//                c.SignalAll();
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsFalse(sync.HasWaiters(c));
//                Assert.AreEqual(0, sync.getWaitQueueLength(c));
//                sync.Release(1);
//                t1.Join(SHORT_DELAY_MS);
//                t2.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t1.isAlive());
//                Assert.IsFalse(t2.isAlive());
//            }
//            catch (Exception) {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestGetWaitingThreads()
//        {
//     Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//     Thread t1 = new Thread(new Runnable() {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            threadAssertTrue(sync.getWaitingThreads(c).IsEmpty());
//                            c.Await();
//                            sync.Release(1);
//             }
//             catch(InterruptedException) {
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//    
//     Thread t2 = new Thread(new Runnable() {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            ThreadAssertFalse(sync.getWaitingThreads(c).IsEmpty());
//                            c.Await();
//                            sync.Release(1);
//             }
//             catch(InterruptedException) {
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//    
//            try {
//                sync.Acquire(1);
//                Assert.IsTrue(sync.getWaitingThreads(c).IsEmpty());
//                sync.Release(1);
//                t1.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsTrue(sync.HasWaiters(c));
//                Assert.IsTrue(sync.getWaitingThreads(c).Contains(t1));
//                Assert.IsTrue(sync.getWaitingThreads(c).Contains(t2));
//                c.SignalAll();
//                sync.Release(1);
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                Assert.IsFalse(sync.HasWaiters(c));
//                Assert.IsTrue(sync.getWaitingThreads(c).IsEmpty());
//                sync.Release(1);
//                t1.Join(SHORT_DELAY_MS);
//                t2.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t1.isAlive());
//                Assert.IsFalse(t2.isAlive());
//            }
//            catch (Exception) {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAwaitUnInterruptibly()
//        {
//     Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//     Thread t = new Thread(new Runnable() { 
//         public void run() {
//                        sync.Acquire(1);
//                        c.AwaitUnInterruptibly();
//                        sync.Release(1);
//         }
//         });
//    
//            try {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t.Interrupt();
//                sync.Acquire(1);
//                c.Signal();
//                sync.Release(1);
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception) {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAwaitInterrupt()
//        {
//     Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//     Thread t = new Thread(new Runnable() {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            c.Await();
//                            sync.Release(1);
//                            threadShouldThrow();
//             }
//             catch(ThreadInterruptedException) {
//                        }
//         }
//         });
//    
//            try {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t.Interrupt();
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception) {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAwaitNanosInterrupt()
//        {
//     Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//     Thread t = new Thread(new Runnable() {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            c.AwaitNanos(1000 * 1000 * 1000); // 1 sec
//                            sync.Release(1);
//                            threadShouldThrow();
//             }
//             catch(ThreadInterruptedException) {
//                        }
//         }
//         });
//    
//            try {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t.Interrupt();
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception) {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAwaitUntilInterrupt()
//        {
//            Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//            Thread t = new Thread(new Runnable() {
//         public void run() {
//             try {
//             sync.Acquire(1);
//                            java.util.Date d = new java.util.Date();
//                            c.AwaitUntil(new java.util.Date(d.getTime() + 10000));
//                            sync.Release(1);
//                            threadShouldThrow();
//             }
//             catch(ThreadInterruptedException) {
//                        }
//         }
//         });
//    
//            try {
//                t.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                t.Interrupt();
//                t.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t.isAlive());
//            }
//            catch (Exception)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestSignalAll()
//        {
//            Mutex sync = new Mutex();
//            AbstractQueuedSynchronizer.ConditionObject c = sync.NewCondition();
//            Thread t1 = new Thread(new Runnable() {
//                public void run() {
//                    try {
//                        sync.Acquire(1);
//                        c.Await();
//                        sync.Release(1);
//                    }
//                    catch(InterruptedException)
//                    {
//                         ThreadUnexpectedException();
//                    }
//                }
//            });
//    
//            Thread t2 = new Thread(new Runnable()
//            {
//                public void run()
//                {
//                    try {
//                        sync.Acquire(1);
//                            c.Await();
//                            sync.Release(1);
//             }
//             catch(InterruptedException) {
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//
//            try
//            {
//                t1.Start();
//                t2.Start();
//                Thread.Sleep(SHORT_DELAY_MS);
//                sync.Acquire(1);
//                c.SignalAll();
//                sync.Release(1);
//                t1.Join(SHORT_DELAY_MS);
//                t2.Join(SHORT_DELAY_MS);
//                Assert.IsFalse(t1.isAlive());
//                Assert.IsFalse(t2.isAlive());
//            }
//            catch (Exception)
//            {
//                UnexpectedException();
//            }
//        }

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

//        [Test]
//        public void TestGetStateWithReleaseShared()
//        {
//     BooleanLatch l = new BooleanLatch();
//     Assert.IsFalse(l.isSignalled());
//     l.ReleaseShared(0);
//     Assert.IsTrue(l.isSignalled());
//        }
//
//        [Test]
//        public void TestReleaseShared()
//        {
//     BooleanLatch l = new BooleanLatch();
//     Assert.IsFalse(l.isSignalled());
//     l.ReleaseShared(0);
//     Assert.IsTrue(l.isSignalled());
//     l.ReleaseShared(0);
//     Assert.IsTrue(l.isSignalled());
//        }
//
//        [Test]
//        public void TestAcquireSharedInterruptibly()
//        {
//     BooleanLatch l = new BooleanLatch();
//    
//     Thread t = new Thread(new Runnable() {
//         public void run() {
//             try {
//                            ThreadAssertFalse(l.isSignalled());
//             l.AcquireSharedInterruptibly(0);
//                            threadAssertTrue(l.isSignalled());
//             } catch(InterruptedException){
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//            try
//            {
//                t.Start();
//                Assert.IsFalse(l.isSignalled());
//                Thread.Sleep(SHORT_DELAY_MS);
//                l.ReleaseShared(0);
//                Assert.IsTrue(l.isSignalled());
//                t.Join();
//            }
//            catch (InterruptedException)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAsquireSharedTimed()
//        {
//     BooleanLatch l = new BooleanLatch();
//
//     Thread t = new Thread(new Runnable() {
//         public void run() {
//             try {
//                            ThreadAssertFalse(l.isSignalled());
//             threadAssertTrue(l.TryAcquireSharedNanos(0, MEDIUM_DELAY_MS* 1000 * 1000));
//                            threadAssertTrue(l.isSignalled());
//    
//             } catch(InterruptedException){
//                            ThreadUnexpectedException();
//                        }
//         }
//         });
//            try
//            {
//                t.Start();
//                Assert.IsFalse(l.isSignalled());
//                Thread.Sleep(SHORT_DELAY_MS);
//                l.ReleaseShared(0);
//                Assert.IsTrue(l.isSignalled());
//                t.Join();
//            }
//            catch (InterruptedException)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAcquireSharedInterruptiblyInterruptedException()
//        {
//            BooleanLatch l = new BooleanLatch();
//            Thread t = new Thread(new Runnable() {
//                    public void run() {
//                        try {
//                            ThreadAssertFalse(l.isSignalled());
//                            l.AcquireSharedInterruptibly(0);
//                            threadShouldThrow();
//                        } catch(ThreadInterruptedException){}
//                    }
//                });
//     t.Start();
//            try
//            {
//                Assert.IsFalse(l.isSignalled());
//                t.Interrupt();
//                t.Join();
//            }
//            catch (InterruptedException)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAcquireSharedNanosInterruptedException()
//        {
//            BooleanLatch l = new BooleanLatch();
//            Thread t = new Thread(new Runnable() {
//                    public void run() {
//                        try {
//                            ThreadAssertFalse(l.isSignalled());
//                            l.TryAcquireSharedNanos(0, SMALL_DELAY_MS* 1000 * 1000);
//                            threadShouldThrow();                        
//                        } catch(ThreadInterruptedException){}
//                    }
//                });
//            t.Start();
//            try
//            {
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(l.isSignalled());
//                t.Interrupt();
//                t.Join();
//            }
//            catch (InterruptedException)
//            {
//                UnexpectedException();
//            }
//        }
//
//        [Test]
//        public void TestAcquireSharedNanosTimeout()
//        {
//            BooleanLatch l = new BooleanLatch();
//            Thread t = new Thread(new Runnable()
//            {
//                    public void run() {
//                        try {
//                            ThreadAssertFalse(l.isSignalled());
//                            ThreadAssertFalse(l.TryAcquireSharedNanos(0, SMALL_DELAY_MS* 1000 * 1000));
//                        } catch(InterruptedException ie){
//                            ThreadUnexpectedException();
//                        }
//                    }
//                });
//            t.Start();
//            try
//            {
//                Thread.Sleep(SHORT_DELAY_MS);
//                Assert.IsFalse(l.isSignalled());
//                t.Join();
//            }
//            catch (InterruptedException)
//            {
//                UnexpectedException();
//            }
//        }

    }
}

