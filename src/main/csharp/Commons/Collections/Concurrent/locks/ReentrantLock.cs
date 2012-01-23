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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks
{
    /// <summary>
    /// <para>
    /// A reentrant mutual exclusion Lock with the same basic behavior and
    /// semantics as the standard .NET locks but with extended capabilites.
    /// </para>
    /// <para>
    /// A ReentrantLock is owned by the thread last successfully locking, but
    /// not yet unlocking it.  A Thread invoking lock will return, successfully
    /// acquiring the lock, when the lock is not owned by another thread.  The
    /// the method will return immediately if the current thread already owns
    /// the lock.  The method IsHeldByCurrentThread will return whether the
    /// current thread is the lock holder.
    /// </para>
    /// </summary>
    public class ReentrantLock : Lock
    {
        /// <summary>
        /// The Sync object that is the real implementation of this Lock class.
        /// </summary>
        private readonly Sync sync;

        #region Sync implementation, the real meat of this lock class.

        private abstract class Sync : AbstractQueuedSynchronizer
        {
            public abstract void Lock();

            /// <summary>
            /// Performs non-fair TryLock.  TryAcquire is implemented in subclasses,
            /// but both need nonfair try for trylock method.
            /// </summary>
            public bool NonfairTryAcquire(int acquires)
            {
                Thread current = Thread.CurrentThread;
                int c = State;
                if (c == 0)
                {
                    if (CompareAndSetState(0, acquires))
                    {
                        ExclusiveOwnerThread = current;
                        return true;
                    }
                }
                else if (current == ExclusiveOwnerThread)
                {
                    int nextc = c + acquires;
                    if (nextc < 0) // overflow
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }
                    State = nextc;
                    return true;
                }
                return false;
            }
    
            protected override bool TryRelease(int releases)
            {
                int c = State - releases;
                if (Thread.CurrentThread != ExclusiveOwnerThread)
                {
                    throw new ThreadStateException();
                }

                bool free = false;
                if (c == 0)
                {
                    free = true;
                    ExclusiveOwnerThread = null;
                }

                State = c;
                return free;
            }
    
            protected override bool IsHeldExclusively()
            {
                // While we must in general read state before owner,
                // we don't need to do so to check if current thread is owner
                return ExclusiveOwnerThread == Thread.CurrentThread;
            }

            public ConditionObject NewCondition()
            {
                return new ConditionObject(this);
            }
    
            // Methods relayed from outer class

            public bool HeldExclusively
            {
                get { return this.IsHeldExclusively(); }
            }
    
            public Thread Owner
            {
                get { return State == 0 ? null : ExclusiveOwnerThread; }
            }

            public int HoldCount
            {
                get { return IsHeldExclusively() ? State : 0; }
            }

            public bool Locked
            {
                get { return State != 0; }
            }
        }

        private sealed class NotFairSync : Sync
        {
            /// <summary>
            /// Performs lock.  Try immediate barge, backing up to normal acquire on failure
            /// </summary>
            public override void Lock()
            {
                if (CompareAndSetState(0, 1))
                {
                    ExclusiveOwnerThread = Thread.CurrentThread;
                }
                else
                {
                    Acquire(1);
                }
            }

            protected override bool TryAcquire(int acquires)
            {
                return NonfairTryAcquire(acquires);
            }
        }

        private sealed class FairSync : Sync
        {
            public override void Lock()
            {
                Acquire(1);
            }
    
            /// <summary>
            /// Fair version of tryAcquire.  Don't grant access unless recursive call
            /// or no waiters or is first.
            /// </summary>
            protected override bool TryAcquire(int acquires)
            {
                Thread current = Thread.CurrentThread;
                int c = State;
                if (c == 0)
                {
                    if (!HasQueuedPredecessors() && CompareAndSetState(0, acquires))
                    {
                        ExclusiveOwnerThread = current;
                        return true;
                    }
                }
                else if (current == ExclusiveOwnerThread)
                {
                    int nextc = c + acquires;
                    if (nextc < 0)
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }

                    State = nextc;

                    return true;
                }

                return false;
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the ReentrantLock class in the non-fair mode.
        /// </summary>
        public ReentrantLock() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReentrantLock class, if the parameter is
        /// true then the lock is in Fair mode, otherwise it is in Non-Fair mode.
        /// </summary>
        public ReentrantLock(bool fair) : base()
        {
            if (fair)
            {
                this.sync = new FairSync();
            }
            else
            {
                this.sync = new NotFairSync();
            }
        }

        public void Lock()
        {
            sync.Lock();
        }

        public void LockInterruptibly()
        {
            sync.AcquireInterruptibly(1);
        }

        public bool TryLock()
        {
            return sync.NonfairTryAcquire(1);
        }

        public bool TryLock(long millisecs)
        {
            return sync.TryAcquire(1, millisecs);
        }

        public bool TryLock(TimeSpan duration)
        {
            return sync.TryAcquire(1, duration);
        }

        public void UnLock()
        {
            sync.Release(1);
        }

        public Condition NewCondition()
        {
            return sync.NewCondition();
        }

        public int HoldCount
        {
            get { return sync.HoldCount; }
        }

        public bool IsHeldByCurrentThread
        {
            get { return sync.HeldExclusively; }
        }

        public bool IsLocked
        {
            get { return sync.Locked; }
        }

        /// <summary>
        /// Returns true if the Lock has its fairness value set to true.
        /// </summary>
        public bool IsFair
        {
            get { return sync is FairSync; }
        }

        protected Thread Owner
        {
            get { return sync.Owner; }
        }

        public bool HasQueuedThreads
        {
            get { return sync.HasQueuedThreads; }
        }

        public bool HasQueuedThread(Thread thread)
        {
            return sync.IsQueued(thread);
        }
    
        public int QueueLength
        {
            get { return sync.QueueLength; }
        }
    
        protected Collection<Thread> QueuedThreads
        {
            get { return sync.QueuedThreads; }
        }

        public bool HasWaiters(Condition condition)
        {
            if (condition == null)
            {
                throw new NullReferenceException();
            }

            if (!(condition is AbstractQueuedSynchronizer.ConditionObject))
            {
                throw new ArgumentException("not owner");
            }

            return sync.HasWaiters((AbstractQueuedSynchronizer.ConditionObject)condition);
        }
    
        public int GetWaitQueueLength(Condition condition)
        {
            if (condition == null)
            {
                throw new NullReferenceException();
            }

            if (!(condition is AbstractQueuedSynchronizer.ConditionObject))
            {
                throw new ArgumentException("not owner");
            }

            return sync.GetWaitQueueLength((AbstractQueuedSynchronizer.ConditionObject)condition);
        }
    
        public Collection<Thread> GetWaitingThreads(Condition condition)
        {
            if (condition == null)
            {
                throw new NullReferenceException();
            }

            if (!(condition is AbstractQueuedSynchronizer.ConditionObject))
            {
                throw new ArgumentException("not owner");
            }

            return sync.GetWaitingThreads((AbstractQueuedSynchronizer.ConditionObject)condition);
        }

        public override String ToString()
        {
            Thread o = sync.Owner;
            return base.ToString() + ((o == null) ? "[Unlocked]" :
                                                    "[Locked by thread " + o.Name + "]");
        }
    }
}

