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
    public class ReentrantReadWriteLock : ReadWriteLock
    {
        // Inner class providing readlock
        private readonly ReentrantReadWriteLock.ReadLockImpl readerLock;
        // Inner class providing writelock
        private readonly ReentrantReadWriteLock.WriteLockImpl writerLock;
        // Performs all synchronization mechanics
        internal readonly Sync sync;

        /// <summary>
        /// Creates a new ReentrantReadWriteLock with default (nonfair) ordering properties.
        /// </summary>
        public ReentrantReadWriteLock() : this(false)
        {
        }

        /// <summary>
        /// Creates a new ReentrantReadWriteLock with given (fairness) ordering properties.
        /// </summary>
        public ReentrantReadWriteLock(bool fair)
        {
            sync = fair ? (Sync) new FairSync() : (Sync) new NonfairSync();
            readerLock = new ReadLockImpl(this);
            writerLock = new WriteLockImpl(this);
        }
    
        public Lock WriteLock
        {
            get { return writerLock; }
        }

        public Lock ReadLock
        {
            get { return readerLock; }
        }

        /// <summary>
        /// Synchronization implementation for ReentrantReadWriteLock. Subclassed
        /// into fair and nonfair versions.
        /// </summary>
        internal abstract class Sync : AbstractQueuedSynchronizer
        {
            /*
             * Read vs write count extraction constants and functions.
             * Lock state is logically divided into two unsigned shorts:
             * The lower one representing the exclusive (writer) lock hold count,
             * and the upper the shared (reader) hold count.
             */
    
            protected static readonly int SHARED_SHIFT   = 16;
            protected static readonly int SHARED_UNIT    = (1 << SHARED_SHIFT);
            protected static readonly int MAX_COUNT      = (1 << SHARED_SHIFT) - 1;
            protected static readonly int EXCLUSIVE_MASK = (1 << SHARED_SHIFT) - 1;

            /// <summary>
            /// Returns the number of shared holds represented in count.
            /// </summary>
            internal static int SharedCount(int c)
            {
                return c >> SHARED_SHIFT;
            }

            /// <summary>
            /// Returns the number of exclusive holds represented in count.
            /// </summary>
            internal static int ExclusiveCount(int c)
            {
                return c & EXCLUSIVE_MASK;
            }

            /// <summary>
            /// A counter for per-thread read hold counts. Maintained as a ThreadLocal;
            /// cached in cachedHoldCounter
            /// </summary>
            protected sealed class HoldCounter
            {
                public int count = 0;

                // Use id, not reference, to avoid garbage retention
                public readonly long tid = Thread.CurrentThread.ManagedThreadId;
            }

            /// <summary>
            /// Thread local hold counter.
            /// </summary>
            protected class ThreadLocalHoldCounter : ThreadLocal<HoldCounter>
            {
                protected override HoldCounter InitialValue
                {
                    get { return new HoldCounter(); }
                }
            }
    
            /*
             * The number of reentrant read locks held by current thread.
             * Initialized only in constructor and readObject.
             * Removed whenever a thread's read hold count drops to 0.
             */
            private ThreadLocalHoldCounter readHolds;
    
            /*
             * The hold count of the last thread to successfully Acquire
             * readLock. This saves ThreadLocal lookup in the common case
             * where the next thread to release is the last one to
             * Acquire. This is non-volatile since it is just used
             * as a heuristic, and would be great for threads to cache.
             *
             * Can outlive the Thread for which it is caching the read
             * hold count, but avoids garbage retention by not retaining a
             * reference to the Thread.
             *
             * Accessed via a benign data race; relies on the memory
             * model's readonly field and out-of-thin-air guarantees.
             */
            private HoldCounter cachedHoldCounter;
    
            /*
             * firstReader is the first thread to have Acquired the read lock.
             * firstReaderHoldCount is firstReader's hold count.
             *
             * <p>More precisely, firstReader is the unique thread that last
             * changed the shared count from 0 to 1, and has not released the
             * read lock since then; null if there is no such thread.
             *
             * <p>Cannot cause garbage retention unless the thread terminated
             * without relinquishing its read locks, since tryReleaseShared
             * sets it to null.
             *
             * <p>Accessed via a benign data race; relies on the memory
             * model's out-of-thin-air guarantees for references.
             *
             * <p>This allows tracking of read holds for uncontended read
             * locks to be very cheap.
             */
            private Thread firstReader = null;
            private int firstReaderHoldCount;

            public Sync()
            {
                readHolds = new ThreadLocalHoldCounter();
                //state = State; // ensures visibility of readHolds
            }
    
            /*
             * Acquires and releases use the same code for fair and
             * nonfair locks, but differ in whether/how they allow barging
             * when queues are non-empty.
             */
    
            /**
             * Returns true if the current thread, when trying to Acquire
             * the read lock, and otherwise eligible to do so, should block
             * because of policy for overtaking other waiting threads.
             */
            protected abstract bool ReaderShouldBlock { get; }

            /**
             * Returns true if the current thread, when trying to Acquire
             * the write lock, and otherwise eligible to do so, should block
             * because of policy for overtaking other waiting threads.
             */
            protected abstract bool WriterShouldBlock { get; }

            /*
             * Note that tryRelease and tryAcquire can be called by
             * Conditions. So it is possible that their arguments contain
             * both read and write holds that are all released during a
             * condition wait and re-established in tryAcquire.
             */

            protected internal override bool TryRelease(int releases)
            {
                if (!IsHeldExclusively())
                {
                    throw new ThreadStateException();
                }
                int nextc = State - releases;
                bool free = ExclusiveCount(nextc) == 0;
                if (free)
                {
                    ExclusiveOwnerThread = null;
                }
                State = nextc;
                return free;
            }
    
            protected internal override bool TryAcquire(int Acquires)
            {
                /*
                 * Walkthrough:
                 * 1. If read count nonzero or write count nonzero
                 *    and owner is a different thread, fail.
                 * 2. If count would saturate, fail. (This can only
                 *    happen if count is already nonzero.)
                 * 3. Otherwise, this thread is eligible for lock if
                 *    it is either a reentrant Acquire or
                 *    queue policy allows it. If so, update state
                 *    and set owner.
                 */
                Thread current = Thread.CurrentThread;
                int c = State;
                int w = ExclusiveCount(c);
                if (c != 0)
                {
                    // (Note: if c != 0 and w == 0 then shared count != 0)
                    if (w == 0 || current != ExclusiveOwnerThread)
                    {
                        return false;
                    }
                    if (w + ExclusiveCount(Acquires) > MAX_COUNT)
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }
                    // Reentrant Acquire
                    State = c + Acquires;
                    return true;
                }
                if (WriterShouldBlock || !CompareAndSetState(c, c + Acquires))
                {
                    return false;
                }

                ExclusiveOwnerThread = current;
                return true;
            }
    
            protected internal override bool TryReleaseShared(int unused)
            {
                Thread current = Thread.CurrentThread;
                if (firstReader == current)
                {
                    // assert firstReaderHoldCount > 0;
                    if (firstReaderHoldCount == 1)
                    {
                        firstReader = null;
                    }
                    else
                    {
                        firstReaderHoldCount--;
                    }
                }
                else
                {
                    HoldCounter rh = cachedHoldCounter;
                    if (rh == null || rh.tid != current.ManagedThreadId)
                        rh = readHolds.Value;
                    int count = rh.count;
                    if (count <= 1) {
                        readHolds.Remove();
                        if (count <= 0)
                            throw UnmatchedUnlockException();
                    }
                    --rh.count;
                }

                for (;;)
                {
                    int c = State;
                    int nextc = c - SHARED_UNIT;
                    if (CompareAndSetState(c, nextc))
                    {
                        // Releasing the read lock has no effect on readers,
                        // but it may allow waiting writers to proceed if
                        // both read and write locks are now free.
                        return nextc == 0;
                    }
                }
            }
    
            private ThreadStateException UnmatchedUnlockException()
            {
                return new ThreadStateException(
                    "attempt to unlock read lock, not locked by current thread");
            }
    
            protected internal override int TryAcquireShared(int unused)
            {
                /*
                 * Walkthrough:
                 * 1. If write lock held by another thread, fail.
                 * 2. Otherwise, this thread is eligible for
                 *    lock wrt state, so ask if it should block
                 *    because of queue policy. If not, try
                 *    to grant by CASing state and updating count.
                 *    Note that step does not check for reentrant
                 *    Acquires, which is postponed to full version
                 *    to avoid having to check hold count in
                 *    the more typical non-reentrant case.
                 * 3. If step 2 fails either because thread
                 *    apparently not eligible or CAS fails or count
                 *    saturated, chain to version with full retry loop.
                 */
                Thread current = Thread.CurrentThread;
                int c = State;
                if (ExclusiveCount(c) != 0 && ExclusiveOwnerThread != current)
                {
                    return -1;
                }
                int r = SharedCount(c);
                if (!ReaderShouldBlock && r < MAX_COUNT && CompareAndSetState(c, c + SHARED_UNIT))
                {
                    if (r == 0)
                    {
                        firstReader = current;
                        firstReaderHoldCount = 1;
                    }
                    else if (firstReader == current)
                    {
                        firstReaderHoldCount++;
                    }
                    else
                    {
                        HoldCounter rh = cachedHoldCounter;
                        if (rh == null || rh.tid != current.ManagedThreadId)
                        {
                            cachedHoldCounter = rh = readHolds.Value;
                        }
                        else if (rh.count == 0)
                        {
                            readHolds.Value = rh;
                        }
                        rh.count++;
                    }
                    return 1;
                }
                return FullTryAcquireShared(current);
            }
    
            /**
             * Full version of Acquire for reads, that handles CAS misses
             * and reentrant reads not dealt with in tryAcquireShared.
             */
            public int FullTryAcquireShared(Thread current)
            {
                /*
                 * This code is in part redundant with that in
                 * tryAcquireShared but is simpler overall by not
                 * complicating tryAcquireShared with interactions between
                 * retries and lazily reading hold counts.
                 */
                HoldCounter rh = null;
                for (;;) {
                    int c = State;
                    if (ExclusiveCount(c) != 0)
                    {
                        if (ExclusiveOwnerThread != current)
                        {
                            return -1;
                        }
                        // else we hold the exclusive lock; blocking here
                        // would cause deadlock.
                    }
                    else if (ReaderShouldBlock)
                    {
                        // Make sure we're not acquiring read lock reentrantly
                        if (firstReader == current)
                        {
                            // assert firstReaderHoldCount > 0;
                        }
                        else
                        {
                            if (rh == null)
                            {
                                rh = cachedHoldCounter;
                                if (rh == null || rh.tid != current.ManagedThreadId)
                                {
                                    rh = readHolds.Value;
                                    if (rh.count == 0)
                                    {
                                        readHolds.Remove();
                                    }
                                }
                            }

                            if (rh.count == 0)
                            {
                                return -1;
                            }
                        }
                    }

                    if (SharedCount(c) == MAX_COUNT)
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }
                    if (CompareAndSetState(c, c + SHARED_UNIT))
                    {
                        if (SharedCount(c) == 0)
                        {
                            firstReader = current;
                            firstReaderHoldCount = 1;
                        }
                        else if (firstReader == current)
                        {
                            firstReaderHoldCount++;
                        }
                        else
                        {
                            if (rh == null)
                            {
                                rh = cachedHoldCounter;
                            }

                            if (rh == null || rh.tid != current.ManagedThreadId)
                            {
                                rh = readHolds.Value;
                            }
                            else if (rh.count == 0)
                            {
                                readHolds.Value = rh;
                            }
                            rh.count++;
                            cachedHoldCounter = rh; // cache for release
                        }
                        return 1;
                    }
                }
            }
    
            /**
             * Performs TryLock for write, enabling barging in both modes.
             * This is identical in effect to tryAcquire except for lack
             * of calls to writerShouldBlock.
             */
            public bool TryWriteLock()
            {
                Thread current = Thread.CurrentThread;
                int c = State;
                if (c != 0)
                {
                    int w = ExclusiveCount(c);
                    if (w == 0 || current != ExclusiveOwnerThread)
                    {
                        return false;
                    }

                    if (w == MAX_COUNT)
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }
                }

                if (!CompareAndSetState(c, c + 1))
                {
                    return false;
                }

                ExclusiveOwnerThread = current;

                return true;
            }
    
            /**
             * Performs TryLock for read, enabling barging in both modes.
             * This is identical in effect to tryAcquireShared except for
             * lack of calls to ReaderShouldBlock.
             */
            public bool TryReadLock()
            {
                Thread current = Thread.CurrentThread;
                for (;;)
                {
                    int c = State;
                    if (ExclusiveCount(c) != 0 && ExclusiveOwnerThread != current)
                    {
                        return false;
                    }

                    int r = SharedCount(c);

                    if (r == MAX_COUNT)
                    {
                        throw new Exception("Maximum lock count exceeded");
                    }

                    if (CompareAndSetState(c, c + SHARED_UNIT))
                    {
                        if (r == 0)
                        {
                            firstReader = current;
                            firstReaderHoldCount = 1;
                        }
                        else if (firstReader == current)
                        {
                            firstReaderHoldCount++;
                        }
                        else
                        {
                            HoldCounter rh = cachedHoldCounter;
                            if (rh == null || rh.tid != current.ManagedThreadId)
                            {
                                cachedHoldCounter = rh = readHolds.Value;
                            }
                            else if (rh.count == 0)
                            {
                                readHolds.Value = rh;
                            }
                            rh.count++;
                        }
                        return true;
                    }
                }
            }
    
            protected internal override bool IsHeldExclusively()
            {
                // While we must in general read state before owner,
                // we don't need to do so to check if current thread is owner
                return ExclusiveOwnerThread == Thread.CurrentThread;
            }

            // Methods relayed to outer class

            public ConditionObject NewCondition()
            {
                return new AbstractQueuedSynchronizer.ConditionObject(this);
            }
    
            public Thread Owner
            {
                // Must read state before owner to ensure memory consistency
                get { return ((ExclusiveCount(State) == 0) ? null : ExclusiveOwnerThread); }
            }

            public int ReadLockCount
            {
                get { return SharedCount(State); }
            }

            public bool IsWriteLocked
            {
                get { return ExclusiveCount(State) != 0; }
            }

            public int WriteHoldCount
            {
                get { return IsHeldExclusively() ? ExclusiveCount(State) : 0; }
            }
    
            public int ReadHoldCount
            {
                get
                {
                    if (ReadLockCount == 0)
                    {
                        return 0;
                    }

                    Thread current = Thread.CurrentThread;
                    if (firstReader == current)
                    {
                        return firstReaderHoldCount;
                    }
    
                    HoldCounter rh = cachedHoldCounter;
                    if (rh != null && rh.tid == current.ManagedThreadId)
                    {
                        return rh.count;
                    }

                    int count = readHolds.Value.count;
                    if (count == 0)
                    {
                        readHolds.Remove();
                    }

                    return count;
                }
            }

            public int Count
            {
                get { return State; }
            }
        }
    
        /**
         * Nonfair version of Sync
         */
        private sealed class NonfairSync : Sync
        {
            protected override bool WriterShouldBlock
            {
                get { return false; } // writers can always barge
            }

            protected override bool ReaderShouldBlock
            {
                /* As a heuristic to avoid indefinite writer starvation,
                 * block if the thread that momentarily appears to be head
                 * of queue, if one exists, is a waiting writer.  This is
                 * only a probabilistic effect since a new reader will not
                 * block if there is a waiting writer behind other enabled
                 * readers that have not yet drained from the queue.
                 */
                get { return ApparentlyFirstQueuedIsExclusive(); }
            }
        }
    
        /**
         * Fair version of Sync
         */
        private sealed class FairSync : Sync
        {
            protected override bool WriterShouldBlock
            {
                get { return HasQueuedPredecessors(); }
            }

            protected override bool ReaderShouldBlock
            {
                get { return HasQueuedPredecessors(); }
            }
        }
    
        /**
         * The lock returned by method {@link ReentrantReadWriteLock#readLock}.
         */
        public sealed class ReadLockImpl : Lock
        {
            private readonly Sync sync;
    
            /**
             * Constructor for use by subclasses
             *
             * @param lock the outer lock object
             * @throws NullReferenceException if the lock is null
             */
            public ReadLockImpl(ReentrantReadWriteLock parent)
            {
                sync = parent.sync;
            }
    
            /**
             * Acquires the read lock.
             *
             * <p>Acquires the read lock if the write lock is not held by
             * another thread and returns immediately.
             *
             * <p>If the write lock is held by another thread then
             * the current thread becomes disabled for thread scheduling
             * purposes and lies dormant until the read lock has been Acquired.
             */
            public void Lock()
            {
                sync.AcquireShared(1);
            }
    
            /**
             * Acquires the read lock unless the current thread is
             * {@linkplain Thread#interrupt interrupted}.
             *
             * <p>Acquires the read lock if the write lock is not held
             * by another thread and returns immediately.
             *
             * <p>If the write lock is held by another thread then the
             * current thread becomes disabled for thread scheduling
             * purposes and lies dormant until one of two things happens:
             *
             * <ul>
             *
             * <li>The read lock is Acquired by the current thread; or
             *
             * <li>Some other thread {@linkplain Thread#interrupt interrupts}
             * the current thread.
             *
             * </ul>
             *
             * <p>If the current thread:
             *
             * <ul>
             *
             * <li>has its interrupted status set on entry to this method; or
             *
             * <li>is {@linkplain Thread#interrupt interrupted} while
             * acquiring the read lock,
             *
             * </ul>
             *
             * then {@link InterruptedException} is thrown and the current
             * thread's interrupted status is cleared.
             *
             * <p>In this implementation, as this method is an explicit
             * interruption point, preference is given to responding to
             * the interrupt over normal or reentrant acquisition of the
             * lock.
             *
             * @throws InterruptedException if the current thread is interrupted
             */
            public void LockInterruptibly()
            {
                sync.AcquireSharedInterruptibly(1);
            }
    
            /**
             * Acquires the read lock only if the write lock is not held by
             * another thread at the time of invocation.
             *
             * <p>Acquires the read lock if the write lock is not held by
             * another thread and returns immediately with the value
             * {@code true}. Even when this lock has been set to use a
             * fair ordering policy, a call to {@code TryLock()}
             * <em>will</em> immediately Acquire the read lock if it is
             * available, whether or not other threads are currently
             * waiting for the read lock.  This &quot;barging&quot; behavior
             * can be useful in certain circumstances, even though it
             * breaks fairness. If you want to honor the fairness setting
             * for this lock, then use {@link #TryLock(long, TimeUnit)
             * TryLock(0, TimeUnit.SECONDS) } which is almost equivalent
             * (it also detects interruption).
             *
             * <p>If the write lock is held by another thread then
             * this method will return immediately with the value
             * {@code false}.
             *
             * @return {@code true} if the read lock was Acquired
             */
            public bool TryLock()
            {
                return sync.TryReadLock();
            }

            /**
             * Acquires the read lock if the write lock is not held by
             * another thread within the given waiting time and the
             * current thread has not been {@linkplain Thread#interrupt
             * interrupted}.
             *
             * <p>Acquires the read lock if the write lock is not held by
             * another thread and returns immediately with the value
             * {@code true}. If this lock has been set to use a fair
             * ordering policy then an available lock <em>will not</em> be
             * Acquired if any other threads are waiting for the
             * lock. This is in contrast to the {@link #TryLock()}
             * method. If you want a timed {@code TryLock} that does
             * permit barging on a fair lock then combine the timed and
             * un-timed forms together:
             *
             * <pre>if (lock.TryLock() || lock.TryLock(timeout, unit) ) { ... }
             * </pre>
             *
             * <p>If the write lock is held by another thread then the
             * current thread becomes disabled for thread scheduling
             * purposes and lies dormant until one of three things happens:
             *
             * <ul>
             *
             * <li>The read lock is Acquired by the current thread; or
             *
             * <li>Some other thread {@linkplain Thread#interrupt interrupts}
             * the current thread; or
             *
             * <li>The specified waiting time elapses.
             *
             * </ul>
             *
             * <p>If the read lock is Acquired then the value {@code true} is
             * returned.
             *
             * <p>If the current thread:
             *
             * <ul>
             *
             * <li>has its interrupted status set on entry to this method; or
             *
             * <li>is {@linkplain Thread#interrupt interrupted} while
             * acquiring the read lock,
             *
             * </ul> then {@link InterruptedException} is thrown and the
             * current thread's interrupted status is cleared.
             *
             * <p>If the specified waiting time elapses then the value
             * {@code false} is returned.  If the time is less than or
             * equal to zero, the method will not wait at all.
             *
             * <p>In this implementation, as this method is an explicit
             * interruption point, preference is given to responding to
             * the interrupt over normal or reentrant acquisition of the
             * lock, and over reporting the elapse of the waiting time.
             *
             * @param timeout the time to wait for the read lock
             * @param unit the time unit of the timeout argument
             * @return {@code true} if the read lock was Acquired
             * @throws InterruptedException if the current thread is interrupted
             * @throws NullReferenceException if the time unit is null
             *
             */
            public bool TryLock(long timeout)
            {
                return sync.TryAcquireShared(1, timeout);
            }

            public bool TryLock(TimeSpan timeout)
            {
                return sync.TryAcquireShared(1, timeout);
            }

            /**
             * Attempts to release this lock.
             *
             * <p> If the number of readers is now zero then the lock
             * is made available for write lock attempts.
             */
            public void UnLock()
            {
                sync.ReleaseShared(1);
            }
    
            /// <summary>
            /// Throws a NotSupportedException since Read Locks can't have Conditions.
            /// </summary>
            public Condition NewCondition()
            {
                throw new NotSupportedException();
            }
    
            /**
             * Returns a string identifying this lock, as well as its lock state.
             * The state, in brackets, includes the String {@code "Read locks ="}
             * followed by the number of held read locks.
             *
             * @return a string identifying this lock, as well as its lock state
             */
            public override String ToString()
            {
                int r = sync.ReadLockCount;
                return base.ToString() + "[Read locks = " + r + "]";
            }
        }
    
        /**
         * The lock returned by method {@link ReentrantReadWriteLock#writeLock}.
         */
        public class WriteLockImpl : Lock
        {
            private readonly Sync sync;
    
            /**
             * Constructor for use by subclasses
             *
             * @param lock the outer lock object
             * @throws NullReferenceException if the lock is null
             */
            public WriteLockImpl(ReentrantReadWriteLock parent)
            {
                sync = parent.sync;
            }
    
            /**
             * Acquires the write lock.
             *
             * <p>Acquires the write lock if neither the read nor write lock
             * are held by another thread
             * and returns immediately, setting the write lock hold count to
             * one.
             *
             * <p>If the current thread already holds the write lock then the
             * hold count is incremented by one and the method returns
             * immediately.
             *
             * <p>If the lock is held by another thread then the current
             * thread becomes disabled for thread scheduling purposes and
             * lies dormant until the write lock has been Acquired, at which
             * time the write lock hold count is set to one.
             */
            public void Lock()
            {
                sync.Acquire(1);
            }
    
            /**
             * Acquires the write lock unless the current thread is
             * {@linkplain Thread#interrupt interrupted}.
             *
             * <p>Acquires the write lock if neither the read nor write lock
             * are held by another thread
             * and returns immediately, setting the write lock hold count to
             * one.
             *
             * <p>If the current thread already holds this lock then the
             * hold count is incremented by one and the method returns
             * immediately.
             *
             * <p>If the lock is held by another thread then the current
             * thread becomes disabled for thread scheduling purposes and
             * lies dormant until one of two things happens:
             *
             * <ul>
             *
             * <li>The write lock is Acquired by the current thread; or
             *
             * <li>Some other thread {@linkplain Thread#interrupt interrupts}
             * the current thread.
             *
             * </ul>
             *
             * <p>If the write lock is Acquired by the current thread then the
             * lock hold count is set to one.
             *
             * <p>If the current thread:
             *
             * <ul>
             *
             * <li>has its interrupted status set on entry to this method;
             * or
             *
             * <li>is {@linkplain Thread#interrupt interrupted} while
             * acquiring the write lock,
             *
             * </ul>
             *
             * then {@link InterruptedException} is thrown and the current
             * thread's interrupted status is cleared.
             *
             * <p>In this implementation, as this method is an explicit
             * interruption point, preference is given to responding to
             * the interrupt over normal or reentrant acquisition of the
             * lock.
             *
             * @throws InterruptedException if the current thread is interrupted
             */
            public void LockInterruptibly()
            {
                sync.AcquireInterruptibly(1);
            }
    
            /**
             * Acquires the write lock only if it is not held by another thread
             * at the time of invocation.
             *
             * <p>Acquires the write lock if neither the read nor write lock
             * are held by another thread
             * and returns immediately with the value {@code true},
             * setting the write lock hold count to one. Even when this lock has
             * been set to use a fair ordering policy, a call to
             * {@code TryLock()} <em>will</em> immediately Acquire the
             * lock if it is available, whether or not other threads are
             * currently waiting for the write lock.  This &quot;barging&quot;
             * behavior can be useful in certain circumstances, even
             * though it breaks fairness. If you want to honor the
             * fairness setting for this lock, then use {@link
             * #TryLock(long, TimeUnit) TryLock(0, TimeUnit.SECONDS) }
             * which is almost equivalent (it also detects interruption).
             *
             * <p> If the current thread already holds this lock then the
             * hold count is incremented by one and the method returns
             * {@code true}.
             *
             * <p>If the lock is held by another thread then this method
             * will return immediately with the value {@code false}.
             *
             * @return {@code true} if the lock was free and was Acquired
             * by the current thread, or the write lock was already held
             * by the current thread; and {@code false} otherwise.
             */
            public bool TryLock()
            {
                return sync.TryWriteLock();
            }
    
            /**
             * Acquires the write lock if it is not held by another thread
             * within the given waiting time and the current thread has
             * not been {@linkplain Thread#interrupt interrupted}.
             *
             * <p>Acquires the write lock if neither the read nor write lock
             * are held by another thread
             * and returns immediately with the value {@code true},
             * setting the write lock hold count to one. If this lock has been
             * set to use a fair ordering policy then an available lock
             * <em>will not</em> be Acquired if any other threads are
             * waiting for the write lock. This is in contrast to the {@link
             * #TryLock()} method. If you want a timed {@code TryLock}
             * that does permit barging on a fair lock then combine the
             * timed and un-timed forms together:
             *
             * <pre>if (lock.TryLock() || lock.TryLock(timeout, unit) ) { ... }
             * </pre>
             *
             * <p>If the current thread already holds this lock then the
             * hold count is incremented by one and the method returns
             * {@code true}.
             *
             * <p>If the lock is held by another thread then the current
             * thread becomes disabled for thread scheduling purposes and
             * lies dormant until one of three things happens:
             *
             * <ul>
             *
             * <li>The write lock is Acquired by the current thread; or
             *
             * <li>Some other thread {@linkplain Thread#interrupt interrupts}
             * the current thread; or
             *
             * <li>The specified waiting time elapses
             *
             * </ul>
             *
             * <p>If the write lock is Acquired then the value {@code true} is
             * returned and the write lock hold count is set to one.
             *
             * <p>If the current thread:
             *
             * <ul>
             *
             * <li>has its interrupted status set on entry to this method;
             * or
             *
             * <li>is {@linkplain Thread#interrupt interrupted} while
             * acquiring the write lock,
             *
             * </ul>
             *
             * then {@link InterruptedException} is thrown and the current
             * thread's interrupted status is cleared.
             *
             * <p>If the specified waiting time elapses then the value
             * {@code false} is returned.  If the time is less than or
             * equal to zero, the method will not wait at all.
             *
             * <p>In this implementation, as this method is an explicit
             * interruption point, preference is given to responding to
             * the interrupt over normal or reentrant acquisition of the
             * lock, and over reporting the elapse of the waiting time.
             *
             * @param timeout the time to wait for the write lock
             * @param unit the time unit of the timeout argument
             *
             * @return {@code true} if the lock was free and was Acquired
             * by the current thread, or the write lock was already held by the
             * current thread; and {@code false} if the waiting time
             * elapsed before the lock could be Acquired.
             *
             * @throws InterruptedException if the current thread is interrupted
             * @throws NullReferenceException if the time unit is null
             *
             */
            public bool TryLock(long timeout)
            {
                return sync.TryAcquire(1, timeout);
            }

            public bool TryLock(TimeSpan timeout)
            {
                return sync.TryAcquire(1, timeout);
            }

            /**
             * Attempts to release this lock.
             *
             * <p>If the current thread is the holder of this lock then
             * the hold count is decremented. If the hold count is now
             * zero then the lock is released.  If the current thread is
             * not the holder of this lock then {@link
             * ThreadStateException} is thrown.
             *
             * @throws ThreadStateException if the current thread does not
             * hold this lock.
             */
            public void UnLock()
            {
                sync.Release(1);
            }
    
            /**
             * Returns a {@link Condition} instance for use with this
             * {@link Lock} instance.
             * <p>The returned {@link Condition} instance supports the same
             * usages as do the {@link Object} monitor methods ({@link
             * Object#wait() wait}, {@link Object#notify notify}, and {@link
             * Object#notifyAll notifyAll}) when used with the built-in
             * monitor lock.
             *
             * <ul>
             *
             * <li>If this write lock is not held when any {@link
             * Condition} method is called then an {@link
             * ThreadStateException} is thrown.  (Read locks are
             * held independently of write locks, so are not checked or
             * affected. However it is essentially always an error to
             * invoke a condition waiting method when the current thread
             * has also Acquired read locks, since other threads that
             * could unblock it will not be able to Acquire the write
             * lock.)
             *
             * <li>When the condition {@linkplain Condition#await() waiting}
             * methods are called the write lock is released and, before
             * they return, the write lock is reAcquired and the lock hold
             * count restored to what it was when the method was called.
             *
             * <li>If a thread is {@linkplain Thread#interrupt interrupted} while
             * waiting then the wait will terminate, an {@link
             * InterruptedException} will be thrown, and the thread's
             * interrupted status will be cleared.
             *
             * <li> Waiting threads are signalled in FIFO order.
             *
             * <li>The ordering of lock reacquisition for threads returning
             * from waiting methods is the same as for threads initially
             * acquiring the lock, which is in the default case not specified,
             * but for <em>fair</em> locks favors those threads that have been
             * waiting the longest.
             *
             * </ul>
             *
             * @return the Condition object
             */
            public Condition NewCondition()
            {
                return sync.NewCondition();
            }
    
            /**
             * Returns a string identifying this lock, as well as its lock
             * state.  The state, in brackets includes either the String
             * {@code "Unlocked"} or the String {@code "Locked by"}
             * followed by the {@linkplain Thread#getName name} of the owning thread.
             *
             * @return a string identifying this lock, as well as its lock state
             */
            public override String ToString()
            {
                Thread o = sync.Owner;
                return base.ToString() + ((o == null) ? "[Unlocked]" :
                                                        "[Locked by thread " + o.Name + "]");
            }
    
            /**
             * Queries if this write lock is held by the current thread.
             * Identical in effect to {@link
             * ReentrantReadWriteLock#isWriteLockedByCurrentThread}.
             *
             * @return {@code true} if the current thread holds this lock and
             *         {@code false} otherwise
             * @since 1.6
             */
            public bool IsHeldByCurrentThread()
            {
                return sync.IsHeldExclusively();
            }
    
            /**
             * Queries the number of holds on this write lock by the current
             * thread.  A thread has a hold on a lock for each lock action
             * that is not matched by an unlock action.  Identical in effect
             * to {@link ReentrantReadWriteLock#getWriteHoldCount}.
             *
             * @return the number of holds on this lock by the current thread,
             *         or zero if this lock is not held by the current thread
             * @since 1.6
             */
            public int HoldCount
            {
                get { return sync.WriteHoldCount; }
            }
        }
    
        // Instrumentation and status
    
        /**
         * Returns {@code true} if this lock has fairness set true.
         *
         * @return {@code true} if this lock has fairness set true
         */
        public bool IsFair
        {
            get { return sync is FairSync; }
        }
    
        /**
         * Returns the thread that currently owns the write lock, or
         * {@code null} if not owned. When this method is called by a
         * thread that is not the owner, the return value reflects a
         * best-effort approximation of current lock status. For example,
         * the owner may be momentarily {@code null} even if there are
         * threads trying to Acquire the lock but have not yet done so.
         * This method is designed to facilitate construction of
         * subclasses that provide more extensive lock monitoring
         * facilities.
         *
         * @return the owner, or {@code null} if not owned
         */
        protected Thread Owner
        {
            get { return sync.Owner; }
        }
    
        /**
         * Queries the number of read locks held for this lock. This
         * method is designed for use in monitoring system state, not for
         * synchronization control.
         * @return the number of read locks held.
         */
        public int ReadLockCount
        {
            get { return sync.ReadLockCount; }
        }
    
        /**
         * Queries if the write lock is held by any thread. This method is
         * designed for use in monitoring system state, not for
         * synchronization control.
         *
         * @return {@code true} if any thread holds the write lock and
         *         {@code false} otherwise
         */
        public bool IsWriteLocked
        {
            get { return sync.IsWriteLocked; }
        }
    
        /**
         * Queries if the write lock is held by the current thread.
         *
         * @return {@code true} if the current thread holds the write lock and
         *         {@code false} otherwise
         */
        public bool IsWriteLockedByCurrentThread
        {
            get { return sync.IsHeldExclusively(); }
        }
    
        /**
         * Queries the number of reentrant write holds on this lock by the
         * current thread.  A writer thread has a hold on a lock for
         * each lock action that is not matched by an unlock action.
         *
         * @return the number of holds on the write lock by the current thread,
         *         or zero if the write lock is not held by the current thread
         */
        public int WriteHoldCount
        {
            get { return sync.WriteHoldCount; }
        }
    
        /**
         * Queries the number of reentrant read holds on this lock by the
         * current thread.  A reader thread has a hold on a lock for
         * each lock action that is not matched by an unlock action.
         *
         * @return the number of holds on the read lock by the current thread,
         *         or zero if the read lock is not held by the current thread
         * @since 1.6
         */
        public int ReadHoldCount
        {
            get { return sync.ReadHoldCount; }
        }
    
        /**
         * Returns a collection containing threads that may be waiting to
         * Acquire the write lock.  Because the actual set of threads may
         * change dynamically while constructing this result, the returned
         * collection is only a best-effort estimate.  The elements of the
         * returned collection are in no particular order.  This method is
         * designed to facilitate construction of subclasses that provide
         * more extensive lock monitoring facilities.
         *
         * @return the collection of threads
         */
        protected Collection<Thread> getQueuedWriterThreads()
        {
            return sync.ExclusiveQueuedThreads;
        }
    
        /**
         * Returns a collection containing threads that may be waiting to
         * Acquire the read lock.  Because the actual set of threads may
         * change dynamically while constructing this result, the returned
         * collection is only a best-effort estimate.  The elements of the
         * returned collection are in no particular order.  This method is
         * designed to facilitate construction of subclasses that provide
         * more extensive lock monitoring facilities.
         *
         * @return the collection of threads
         */
        protected Collection<Thread> getQueuedReaderThreads()
        {
            return sync.SharedQueuedThreads;
        }
    
        /**
         * Queries whether any threads are waiting to Acquire the read or
         * write lock. Note that because cancellations may occur at any
         * time, a {@code true} return does not guarantee that any other
         * thread will ever Acquire a lock.  This method is designed
         * primarily for use in monitoring of the system state.
         *
         * @return {@code true} if there may be other threads waiting to
         *         Acquire the lock
         */
        public bool HasQueuedThreads
        {
            get { return sync.HasQueuedThreads; }
        }
    
        /**
         * Queries whether the given thread is waiting to Acquire either
         * the read or write lock. Note that because cancellations may
         * occur at any time, a {@code true} return does not guarantee
         * that this thread will ever Acquire a lock.  This method is
         * designed primarily for use in monitoring of the system state.
         *
         * @param thread the thread
         * @return {@code true} if the given thread is queued waiting for this lock
         * @throws NullReferenceException if the thread is null
         */
        public bool HasQueuedThread(Thread thread)
        {
            return sync.IsQueued(thread);
        }
    
        /**
         * Returns an estimate of the number of threads waiting to Acquire
         * either the read or write lock.  The value is only an estimate
         * because the number of threads may change dynamically while this
         * method traverses internal data structures.  This method is
         * designed for use in monitoring of the system state, not for
         * synchronization control.
         *
         * @return the estimated number of threads waiting for this lock
         */
        public int QueueLength
        {
            get { return sync.QueueLength; }
        }
    
        /**
         * Returns a collection containing threads that may be waiting to
         * Acquire either the read or write lock.  Because the actual set
         * of threads may change dynamically while constructing this
         * result, the returned collection is only a best-effort estimate.
         * The elements of the returned collection are in no particular
         * order.  This method is designed to facilitate construction of
         * subclasses that provide more extensive monitoring facilities.
         *
         * @return the collection of threads
         */
        protected Collection<Thread> QueuedThreads
        {
            get { return sync.QueuedThreads; }
        }
    
        /**
         * Queries whether any threads are waiting on the given condition
         * associated with the write lock. Note that because timeouts and
         * interrupts may occur at any time, a {@code true} return does
         * not guarantee that a future {@code signal} will awaken any
         * threads.  This method is designed primarily for use in
         * monitoring of the system state.
         *
         * @param condition the condition
         * @return {@code true} if there are any waiting threads
         * @throws ThreadStateException if this lock is not held
         * @throws ArgumentException if the given condition is
         *         not associated with this lock
         * @throws NullReferenceException if the condition is null
         */
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
    
        /**
         * Returns an estimate of the number of threads waiting on the
         * given condition associated with the write lock. Note that because
         * timeouts and interrupts may occur at any time, the estimate
         * serves only as an upper bound on the actual number of waiters.
         * This method is designed for use in monitoring of the system
         * state, not for synchronization control.
         *
         * @param condition the condition
         * @return the estimated number of waiting threads
         * @throws ThreadStateException if this lock is not held
         * @throws ArgumentException if the given condition is
         *         not associated with this lock
         * @throws NullReferenceException if the condition is null
         */
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
    
        /**
         * Returns a collection containing those threads that may be
         * waiting on the given condition associated with the write lock.
         * Because the actual set of threads may change dynamically while
         * constructing this result, the returned collection is only a
         * best-effort estimate. The elements of the returned collection
         * are in no particular order.  This method is designed to
         * facilitate construction of subclasses that provide more
         * extensive condition monitoring facilities.
         *
         * @param condition the condition
         * @return the collection of threads
         * @throws ThreadStateException if this lock is not held
         * @throws ArgumentException if the given condition is
         *         not associated with this lock
         * @throws NullReferenceException if the condition is null
         */
        protected Collection<Thread> GetWaitingThreads(Condition condition)
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
    
        /**
         * Returns a string identifying this lock, as well as its lock state.
         * The state, in brackets, includes the String {@code "Write locks ="}
         * followed by the number of reentrantly held write locks, and the
         * String {@code "Read locks ="} followed by the number of held
         * read locks.
         *
         * @return a string identifying this lock, as well as its lock state
         */
        public override String ToString()
        {
            int c = sync.Count;
            int w = Sync.ExclusiveCount(c);
            int r = Sync.SharedCount(c);

            return base.ToString() + "[Write locks = " + w + ", Read locks = " + r + "]";
        }
    }
}

