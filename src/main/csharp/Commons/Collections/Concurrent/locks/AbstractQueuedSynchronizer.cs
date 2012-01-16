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
    /// Provides a framework for implementing blocking locks and related synchronizers
    /// (semaphores, events, etc) that rely on first-in-first-out (FIFO) wait queues.
    /// This class is designed to be a useful basis for most kinds of synchronizers that
    /// rely on a single atomic int value to represent state. Subclasses must define the
    /// protected methods that change this state, and which define what that state means
    /// in terms of this object being acquired or released.  Given these, the other methods
    /// in this class carry out all queuing and blocking mechanics. Subclasses can maintain
    /// other state fields, but only the atomically updated int value manipulated using
    /// property State, and method CompareAndSetState is tracked with respect to synchronization.
    /// Because the locks and conditions built from this class are based solely on a single
    /// atomicly updated int field the synchronization primitives defined using it can be
    /// .NET runtime neutral and work across the various frameworks including the Compact
    /// Framework versions.
    /// </summary>
    #pragma warning disable 420
    public class AbstractQueuedSynchronizer : AbstractOwnableSynchronizer
    {
        #region Internal Implementation

        sealed class Node
        {
            // Marker to indicate a node is waiting in shared mode
            public static readonly Node SHARED = new Node();
            // Marker to indicate a node is waiting in exclusive mode
            public static readonly Node EXCLUSIVE = null;
    
            // waitStatus value to indicate thread has cancelled
            public const int CANCELLED =  1;
            // waitStatus value to indicate successor's thread needs unparking
            public const int SIGNAL    = -1;
            // waitStatus value to indicate thread is waiting on condition
            public const int CONDITION = -2;
            // waitStatus value to indicate the next acquireShared should
            // unconditionally propagate
            public const int PROPAGATE = -3;
    
            /**
             * Status field, taking on only the values:
             *   SIGNAL:     The successor of this node is (or will soon be)
             *               blocked (via park), so the current node must
             *               unpark its successor when it releases or
             *               cancels. To avoid races, acquire methods must
             *               first indicate they need a signal,
             *               then retry the atomic acquire, and then,
             *               on failure, block.
             *   CANCELLED:  This node is cancelled due to timeout or interrupt.
             *               Nodes never leave this state. In particular,
             *               a thread with cancelled node never again blocks.
             *   CONDITION:  This node is currently on a condition queue.
             *               It will not be used as a sync queue node
             *               until transferred, at which time the status
             *               will be set to 0. (Use of this value here has
             *               nothing to do with the other uses of the
             *               field, but simplifies mechanics.)
             *   PROPAGATE:  A releaseShared should be propagated to other
             *               nodes. This is set (for head node only) in
             *               doReleaseShared to ensure propagation
             *               continues, even if other operations have
             *               since intervened.
             *   0:          None of the above
             *
             * The values are arranged numerically to simplify use.
             * Non-negative values mean that a node doesn't need to
             * signal. So, most code doesn't need to check for particular
             * values, just for sign.
             *
             * The field is initialized to 0 for normal sync nodes, and
             * CONDITION for condition nodes.  It is modified using CAS
             * (or when possible, unconditional volatile writes).
             */
            public volatile int waitStatus;
    
            /**
             * Link to predecessor node that current node/thread relies on
             * for checking waitStatus. Assigned during enqueing, and nulled
             * out (for sake of GC) only upon dequeuing.  Also, upon
             * cancellation of a predecessor, we short-circuit while
             * finding a non-cancelled one, which will always exist
             * because the head node is never cancelled: A node becomes
             * head only as a result of successful acquire. A
             * cancelled thread never succeeds in acquiring, and a thread only
             * cancels itself, not any other node.
             */
            public volatile Node prev;
    
            /**
             * Link to the successor node that the current node/thread
             * unparks upon release. Assigned during enqueuing, adjusted
             * when bypassing cancelled predecessors, and nulled out (for
             * sake of GC) when dequeued.  The enq operation does not
             * assign next field of a predecessor until after attachment,
             * so seeing a null next field does not necessarily mean that
             * node is at end of queue. However, if a next field appears
             * to be null, we can scan prev's from the tail to
             * double-check.  The next field of cancelled nodes is set to
             * point to the node itself instead of null, to make life
             * easier for isOnSyncQueue.
             */
            public volatile Node next;
    
            /**
             * The thread that enqueued this node.  Initialized on
             * construction and nulled out after use.
             */
            public volatile Thread thread;
    
            /**
             * Link to next node waiting on condition, or the special
             * value SHARED.  Because condition queues are accessed only
             * when holding in exclusive mode, we just need a simple
             * linked queue to hold nodes while they are waiting on
             * conditions. They are then transferred to the queue to
             * re-acquire. And because conditions can only be exclusive,
             * we save a field by using special value to indicate shared
             * mode.
             */
            public Node nextWaiter;
    
            /// <summary>
            /// Returns true if node is waiting in shared mode
            /// </summary>
            public bool IsShared()
            {
                return nextWaiter == SHARED;
            }
    
            /// <summary>
            /// Returns previous node, or throws NullReferenceException if null.  Use
            /// when predecessor cannot be null.  The null check could be elided, but
            /// is present to help the VM.
            /// </summary>
            public Node Predecessor()
            {
                Node p = prev;
                if (p == null)
                {
                    throw new NullReferenceException();
                }
                else
                {
                    return p;
                }
            }

            public Node()
            {
                // Used to establish initial head or SHARED marker
            }
    
            public Node(Thread thread, Node mode)
            {
                // Used by AddWaiter
                this.nextWaiter = mode;
                this.thread = thread;
            }

            public Node(Thread thread, int waitStatus)
            {
                // Used by Condition
                this.waitStatus = waitStatus;
                this.thread = thread;
            }
        }

        // Head of the wait queue, lazily initialized.  Except for initialization, it
        // is modified only via property Head.  Note: If head exists, its waitStatus is
        // guaranteed not to be CANCELLED.
        private volatile Node head;

        // Tail of the wait queue, lazily initialized.  Modified only via method enq to
        // add new wait node.
        private volatile Node tail;

        // The synchronization state.
        private volatile int state;

        // The number of nanoseconds for which it is faster to spin rather than to
        // use timed park. A rough estimate suffices to improve responsiveness with
        // very short timeouts.
        private const long spinForTimeoutThreshold = 1000L;

        #endregion

        protected AbstractQueuedSynchronizer() : base()
        {
        }

        /// <summary>
        /// Gets or sets the state as a volatile read / write.
        /// </summary>
        protected int State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        /// <summary>
        /// Atomically sets synchronization state to the given updated value if the
        /// current state value equals the expected value.  Returns true if successful.
        /// </summary>
        protected bool CompareAndSetState(int expect, int update)
        {
            return Interlocked.CompareExchange(ref state, update, expect) == expect;
        }

        /// <summary>
        /// Inserts node into queue, initializing if necessary.
        /// </summary>
        private Node Enqueue(Node node)
        {
            for (;;)
            {
                Node t = tail;
                if (t == null)
                {
                    // Must initialize
                    if (CompareAndSetHead(new Node()))
                    {
                        tail = head;
                    }
                }
                else
                {
                    node.prev = t;
                    if (CompareAndSetTail(t, node))
                    {
                        t.next = node;
                        return t;
                    }
                }
            }
        }

        /// <summary>
        /// Creates and enqueues node for current thread and given mode.  The Mode param can
        /// either be Node.EXCLUSIVE or Node.SHARED.
        /// </summary>
        private Node AddWaiter(Node mode)
        {
            Node node = new Node(Thread.CurrentThread, mode);
            // Try the fast path of enq; backup to full enq on failure
            Node pred = tail;

            if (pred != null)
            {
                node.prev = pred;
                if (CompareAndSetTail(pred, node))
                {
                    pred.next = node;
                    return node;
                }
            }

            Enqueue(node);
            return node;
        }

        /// <summary>
        /// Sets head of queue to be node, thus dequeuing. Called only by acquire methods.
        /// Also nulls out unused fields for sake of GC and to suppress unnecessary signals
        /// and traversals.
        /// </summary>
        private void SetHead(Node node)
        {
            head = node;
            node.thread = null;
            node.prev = null;
        }

        /// <summary>
        /// Wakes up node's successor, if one exists.
        /// </summary>
        private void UnparkSuccessor(Node node)
        {
            /*
             * If status is negative (i.e., possibly needing signal) try to clear
             * in anticipation of signalling.  It is OK if this fails or if status
             * is changed by waiting thread.
             */
            int ws = node.waitStatus;
            if (ws < 0)
            {
                CompareAndSetWaitStatus(node, ws, 0);
            }
    
            /*
             * Thread to unpark is held in successor, which is normally just the next
             * node.  But if cancelled or apparently null, traverse backwards from
             * tail to find the actual non-cancelled successor.
             */
            Node s = node.next;
            if (s == null || s.waitStatus > 0)
            {
                s = null;
                for (Node t = tail; t != null && t != node; t = t.prev)
                {
                    if (t.waitStatus <= 0)
                    {
                        s = t;
                    }
                }
            }

            if (s != null)
            {
                LockSupport.UnPark(s.thread);
            }
        }

        /// <summary>
        /// Release action for shared mode -- signal successor and ensure propagation.
        /// (Note: For exclusive mode, release just amounts to calling unparkSuccessor of
        /// head if it needs signal.)
        /// </summary>
        private void DoReleaseShared()
        {
            /*
             * Ensure that a release propagates, even if there are other in-progress
             * acquires/releases.  This proceeds in the usual way of trying to
             * UnparkSuccessor of head if it needs signal. But if it does not, status
             * is set to PROPAGATE to ensure that upon release, propagation continues.
             * Additionally, we must loop in case a new node is added while we are
             * doing this. Also, unlike other uses of UnparkSuccessor, we need to know
             * if CAS to reset status fails, if so rechecking.
             */
            for (;;)
            {
                Node h = head;
                if (h != null && h != tail)
                {
                    int ws = h.waitStatus;
                    if (ws == Node.SIGNAL)
                    {
                        if (!CompareAndSetWaitStatus(h, Node.SIGNAL, 0))
                        {
                            continue;            // loop to recheck cases
                        }
                        UnparkSuccessor(h);
                    }
                    else if (ws == 0 && !CompareAndSetWaitStatus(h, 0, Node.PROPAGATE))
                    {
                        continue;                // loop on failed CAS
                    }
                }

                // loop if head changed
                if (h == head)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Sets head of queue, and checks if successor may be waiting in shared mode,
        /// if so propagating if either propagate > 0 or PROPAGATE status was set.
        /// </summary>
        private void SetHeadAndPropagate(Node node, int propagate)
        {
            Node h = head; // Record old head for check below
            SetHead(node);

            /*
             * Try to signal next queued node if:
             *   Propagation was indicated by caller,
             *     or was recorded (as h.waitStatus) by a previous operation
             *     (note: this uses sign-check of waitStatus because
             *      PROPAGATE status may transition to SIGNAL.)
             * and
             *   The next node is waiting in shared mode,
             *     or we don't know, because it appears null
             *
             * The conservatism in both of these checks may cause
             * unnecessary wake-ups, but only when there are multiple
             * racing acquires/releases, so most need signals now or soon
             * anyway.
             */
            if (propagate > 0 || h == null || h.waitStatus < 0)
            {
                Node s = node.next;
                if (s == null || s.IsShared())
                {
                    DoReleaseShared();
                }
            }
        }

        /// <summary>
        /// Cancels an ongoing attempt to acquire.
        /// </summary>
        private void CancelAcquire(Node node)
        {
            // Ignore if node doesn't exist
            if (node == null)
            {
                return;
            }
    
            node.thread = null;
    
            // Skip cancelled predecessors
            Node pred = node.prev;
            while (pred.waitStatus > 0)
            {
                node.prev = pred = pred.prev;
            }
    
            // predNext is the apparent node to unsplice. CASes below will
            // fail if not, in which case, we lost race vs another cancel
            // or signal, so no further action is necessary.
            Node predNext = pred.next;
    
            // Can use unconditional write instead of CAS here.
            // After this atomic step, other Nodes can skip past us.
            // Before, we are free of interference from other threads.
            node.waitStatus = Node.CANCELLED;
    
            // If we are the tail, remove ourselves.
            if (node == tail && CompareAndSetTail(node, pred))
            {
                CompareAndSetNext(pred, predNext, null);
            }
            else
            {
                // If successor needs signal, try to set pred's next-link
                // so it will get one. Otherwise wake it up to propagate.
                int ws;
                if (pred != head &&
                    ((ws = pred.waitStatus) == Node.SIGNAL ||
                     (ws <= 0 && CompareAndSetWaitStatus(pred, ws, Node.SIGNAL))) && pred.thread != null)
                {
                    Node next = node.next;
                    if (next != null && next.waitStatus <= 0)
                    {
                        CompareAndSetNext(pred, predNext, next);
                    }
                }
                else
                {
                    UnparkSuccessor(node);
                }
    
                node.next = node; // help GC
            }
        }

        /// <summary>
        /// Checks and updates status for a node that failed to acquire.  Returns true if
        /// thread should block. This is the main signal control in all acquire loops.
        /// Requires that pred == node.prev.
        /// </summary>
        private static bool ShouldParkAfterFailedAcquire(Node pred, Node node)
        {
            int ws = pred.waitStatus;
            if (ws == Node.SIGNAL)
            {
                /*
                 * This node has already set status asking a release
                 * to signal it, so it can safely park.
                 */
                return true;
            }

            if (ws > 0)
            {
                // Predecessor was cancelled. Skip over predecessors and indicate retry.
                do
                {
                    node.prev = pred = pred.prev;
                }
                while (pred.waitStatus > 0);
                pred.next = node;
            }
            else
            {
                /*
                 * waitStatus must be 0 or PROPAGATE.  Indicate that we need a signal,
                 * but don't park yet.  Caller will need to retry to make sure it
                 * cannot acquire before parking.
                 */
                CompareAndSetWaitStatus(pred, ws, Node.SIGNAL);
            }
            return false;
        }

        // Convenience method to interrupt current thread.
        private static void SelfInterrupt()
        {
            Thread.CurrentThread.Interrupt();
        }

        // Convenience method to park and then check if interrupted
        private bool ParkAndCheckInterrupt()
        {
            LockSupport.Park();
            return LockSupport.Interrupted();
        }

        /// <summary>
        /// Acquires in exclusive uninterruptible mode for thread already in queue.
        /// Used by condition wait methods as well as Acquire.
        /// </summary>
        bool AcquireQueued(Node node, int arg)
        {
            bool failed = true;
            try
            {
                bool interrupted = false;
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head && TryAcquire(arg))
                    {
                        SetHead(node);
                        p.next = null; // help GC
                        failed = false;
                        return interrupted;
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) && ParkAndCheckInterrupt())
                    {
                        interrupted = true;
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }

        /// <summary>
        /// Acquires in exclusive interruptible mode.
        /// </summary>
        private void DoAcquireInterruptibly(int arg)
        {
            Node node = AddWaiter(Node.EXCLUSIVE);
            bool failed = true;
            try
            {
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head && TryAcquire(arg))
                    {
                        SetHead(node);
                        p.next = null; // help GC
                        failed = false;
                        return;
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) && ParkAndCheckInterrupt())
                    {
                        throw new ThreadInterruptedException();
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }

        /// <summary>
        /// Acquires in exclusive timed mode.
        /// </summary>
        private bool DoAcquireTimed(int arg, long timeout)
        {
            return DoAcquireTimed(arg, TimeSpan.FromMilliseconds(timeout));
        }

        /// <summary>
        /// Acquires in exclusive timed mode.
        /// </summary>
        private bool DoAcquireTimed(int arg, TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            Node node = AddWaiter(Node.EXCLUSIVE);
            bool failed = true;
            try
            {
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head && TryAcquire(arg))
                    {
                        SetHead(node);
                        p.next = null; // help GC
                        failed = false;
                        return true;
                    }

                    if (timeout == TimeSpan.Zero)
                    {
                        return false;
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) &&
                        timeout.TotalMilliseconds > spinForTimeoutThreshold)
                    {
                        LockSupport.Park(timeout);
                    }

                    DateTime awakeTime = DateTime.Now;
                    if(awakeTime > deadline)
                    {
                        timeout = TimeSpan.Zero;
                    }
                    else
                    {
                        timeout = deadline - awakeTime;
                    }

                    if (LockSupport.Interrupted())
                    {
                        throw new ThreadInterruptedException();
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }

        /// <summary>
        /// Acquires in shared uninterruptible mode.
        /// </summary>
        private void DoAcquireShared(int arg)
        {
            Node node = AddWaiter(Node.SHARED);
            bool failed = true;
            try
            {
                bool interrupted = false;
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head)
                    {
                        int r = TryAcquireShared(arg);
                        if (r >= 0)
                        {
                            SetHeadAndPropagate(node, r);
                            p.next = null; // help GC
                            if (interrupted)
                            {
                                SelfInterrupt();
                            }
                            failed = false;
                            return;
                        }
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) && ParkAndCheckInterrupt())
                    {
                        interrupted = true;
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }
    
        /// <summary>
        /// Acquires in shared interruptible mode.
        /// </summary>
        private void DoAcquireSharedInterruptibly(int arg)
        {
            Node node = AddWaiter(Node.SHARED);
            bool failed = true;
            try
            {
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head)
                    {
                        int r = TryAcquireShared(arg);
                        if (r >= 0)
                        {
                            SetHeadAndPropagate(node, r);
                            p.next = null; // help GC
                            failed = false;
                            return;
                        }
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) && ParkAndCheckInterrupt())
                    {
                        throw new ThreadInterruptedException();
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }

        /// <summary>
        /// Acquires in shared timed mode.
        /// </summary>
        private bool DoAcquireSharedTimed(int arg, long timeout)
        {
            return this.DoAcquireSharedTimed(arg, TimeSpan.FromMilliseconds(timeout));
        }

        /// <summary>
        /// Acquires in shared timed mode.
        /// </summary>
        private bool DoAcquireSharedTimed(int arg, TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            Node node = AddWaiter(Node.SHARED);
            bool failed = true;

            try
            {
                for (;;)
                {
                    Node p = node.Predecessor();
                    if (p == head)
                    {
                        int r = TryAcquireShared(arg);
                        if (r >= 0)
                        {
                            SetHeadAndPropagate(node, r);
                            p.next = null; // help GC
                            failed = false;
                            return true;
                        }
                    }

                    if (timeout == TimeSpan.Zero)
                    {
                        return false;
                    }

                    if (ShouldParkAfterFailedAcquire(p, node) &&
                        timeout.TotalMilliseconds > spinForTimeoutThreshold)
                    {
                        LockSupport.Park(timeout);
                    }

                    DateTime awakeTime = DateTime.Now;
                    if(awakeTime > deadline)
                    {
                        timeout = TimeSpan.Zero;
                    }
                    else
                    {
                        timeout = deadline - awakeTime;
                    }

                    if (LockSupport.Interrupted())
                    {
                        throw new ThreadInterruptedException();
                    }
                }
            }
            finally
            {
                if (failed)
                {
                    CancelAcquire(node);
                }
            }
        }

        #region Main Methods expoerted by the Synchronizer

        /**
         * Attempts to acquire in exclusive mode. This method should query
         * if the state of the object permits it to be acquired in the
         * exclusive mode, and if so to acquire it.
         *
         * <p>This method is always invoked by the thread performing
         * acquire.  If this method reports failure, the acquire method
         * may queue the thread, if it is not already queued, until it is
         * signalled by a release from some other thread. This can be used
         * to implement method {@link Lock#tryLock()}.
         *
         * <p>The default
         * implementation throws {@link UnsupportedOperationException}.
         *
         * @param arg the acquire argument. This value is always the one
         *        passed to an acquire method, or is the value saved on entry
         *        to a condition wait.  The value is otherwise uninterpreted
         *        and can represent anything you like.
         * @return {@code true} if successful. Upon success, this object has
         *         been acquired.
         * @throws ThreadStateException if acquiring would place this
         *         synchronizer in an illegal state. This exception must be
         *         thrown in a consistent fashion for synchronization to work
         *         correctly.
         * @throws UnsupportedOperationException if exclusive mode is not supported
         */
        protected virtual bool TryAcquire(int arg)
        {
            throw new NotSupportedException();
        }
    
        /**
         * Attempts to set the state to reflect a release in exclusive
         * mode.
         *
         * <p>This method is always invoked by the thread performing release.
         *
         * <p>The default implementation throws
         * {@link UnsupportedOperationException}.
         *
         * @param arg the release argument. This value is always the one
         *        passed to a release method, or the current state value upon
         *        entry to a condition wait.  The value is otherwise
         *        uninterpreted and can represent anything you like.
         * @return {@code true} if this object is now in a fully released
         *         state, so that any waiting threads may attempt to acquire;
         *         and {@code false} otherwise.
         * @throws ThreadStateException if releasing would place this
         *         synchronizer in an illegal state. This exception must be
         *         thrown in a consistent fashion for synchronization to work
         *         correctly.
         * @throws UnsupportedOperationException if exclusive mode is not supported
         */
        protected virtual bool TryRelease(int arg)
        {
            throw new NotSupportedException();
        }
    
        /**
         * Attempts to acquire in shared mode. This method should query if
         * the state of the object permits it to be acquired in the shared
         * mode, and if so to acquire it.
         *
         * <p>This method is always invoked by the thread performing
         * acquire.  If this method reports failure, the acquire method
         * may queue the thread, if it is not already queued, until it is
         * signalled by a release from some other thread.
         *
         * <p>The default implementation throws {@link
         * UnsupportedOperationException}.
         *
         * @param arg the acquire argument. This value is always the one
         *        passed to an acquire method, or is the value saved on entry
         *        to a condition wait.  The value is otherwise uninterpreted
         *        and can represent anything you like.
         * @return a negative value on failure; zero if acquisition in shared
         *         mode succeeded but no subsequent shared-mode acquire can
         *         succeed; and a positive value if acquisition in shared
         *         mode succeeded and subsequent shared-mode acquires might
         *         also succeed, in which case a subsequent waiting thread
         *         must check availability. (Support for three different
         *         return values enables this method to be used in contexts
         *         where acquires only sometimes act exclusively.)  Upon
         *         success, this object has been acquired.
         * @throws ThreadStateException if acquiring would place this
         *         synchronizer in an illegal state. This exception must be
         *         thrown in a consistent fashion for synchronization to work
         *         correctly.
         * @throws UnsupportedOperationException if shared mode is not supported
         */
        protected virtual int TryAcquireShared(int arg)
        {
            throw new NotSupportedException();
        }
    
        /**
         * Attempts to set the state to reflect a release in shared mode.
         *
         * <p>This method is always invoked by the thread performing release.
         *
         * <p>The default implementation throws
         * {@link UnsupportedOperationException}.
         *
         * @param arg the release argument. This value is always the one
         *        passed to a release method, or the current state value upon
         *        entry to a condition wait.  The value is otherwise
         *        uninterpreted and can represent anything you like.
         * @return {@code true} if this release of shared mode may permit a
         *         waiting acquire (shared or exclusive) to succeed; and
         *         {@code false} otherwise
         * @throws ThreadStateException if releasing would place this
         *         synchronizer in an illegal state. This exception must be
         *         thrown in a consistent fashion for synchronization to work
         *         correctly.
         * @throws UnsupportedOperationException if shared mode is not supported
         */
        protected virtual bool TryReleaseShared(int arg)
        {
            throw new NotSupportedException();
        }
    
        /// <summary>
        /// Returns true if synchronization is held exclusively with respect to the
        /// current (calling) thread.  This method is invoked upon each call to a
        /// non-waiting ConditionObject method.  (Waiting methods instead invoke Release.)
        /// The default implementation throws NotSupportedException.  This method is
        /// invoked internally only within ConditionObject methods, so need not be
        /// defined if conditions are not used.
        /// </summary>
        protected virtual bool IsHeldExclusively()
        {
            throw new NotSupportedException();
        }

        /**
         * Acquires in exclusive mode, ignoring interrupts.  Implemented
         * by invoking at least once {@link #tryAcquire},
         * returning on success.  Otherwise the thread is queued, possibly
         * repeatedly blocking and unblocking, invoking {@link
         * #tryAcquire} until success.  This method can be used
         * to implement method {@link Lock#lock}.
         *
         * @param arg the acquire argument.  This value is conveyed to
         *        {@link #tryAcquire} but is otherwise uninterpreted and
         *        can represent anything you like.
         */
        public void Acquire(int arg)
        {
            if (!TryAcquire(arg) && AcquireQueued(AddWaiter(Node.EXCLUSIVE), arg))
            {
                SelfInterrupt();
            }
        }
    
        /**
         * Acquires in exclusive mode, aborting if interrupted.
         * Implemented by first checking interrupt status, then invoking
         * at least once {@link #tryAcquire}, returning on
         * success.  Otherwise the thread is queued, possibly repeatedly
         * blocking and unblocking, invoking {@link #tryAcquire}
         * until success or the thread is interrupted.  This method can be
         * used to implement method {@link Lock#lockInterruptibly}.
         *
         * @param arg the acquire argument.  This value is conveyed to
         *        {@link #tryAcquire} but is otherwise uninterpreted and
         *        can represent anything you like.
         * @throws ThreadInterruptedException if the current thread is interrupted
         */
        public void AcquireInterruptibly(int arg)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            if (!TryAcquire(arg))
            {
                DoAcquireInterruptibly(arg);
            }
        }
    
        /**
         * Attempts to acquire in exclusive mode, aborting if interrupted,
         * and failing if the given timeout elapses.  Implemented by first
         * checking interrupt status, then invoking at least once {@link
         * #tryAcquire}, returning on success.  Otherwise, the thread is
         * queued, possibly repeatedly blocking and unblocking, invoking
         * {@link #tryAcquire} until success or the thread is interrupted
         * or the timeout elapses.  This method can be used to implement
         * method {@link Lock#tryLock(long, TimeUnit)}.
         *
         * @param arg the acquire argument.  This value is conveyed to
         *        {@link #tryAcquire} but is otherwise uninterpreted and
         *        can represent anything you like.
         * @param nanosTimeout the maximum number of nanoseconds to wait
         * @return {@code true} if acquired; {@code false} if timed out
         * @throws ThreadInterruptedException if the current thread is interrupted
         */
        public bool TryAcquire(int arg, long timeout)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            return TryAcquire(arg) || DoAcquireTimed(arg, TimeSpan.FromMilliseconds(timeout));
        }

        public bool TryAcquire(int arg, TimeSpan timeout)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            return TryAcquire(arg) || DoAcquireTimed(arg, timeout);
        }

        /**
         * Releases in exclusive mode.  Implemented by unblocking one or
         * more threads if {@link #tryRelease} returns true.
         * This method can be used to implement method {@link Lock#unlock}.
         *
         * @param arg the release argument.  This value is conveyed to
         *        {@link #tryRelease} but is otherwise uninterpreted and
         *        can represent anything you like.
         * @return the value returned from {@link #tryRelease}
         */
        public bool Release(int arg)
        {
            if (TryRelease(arg))
            {
                Node h = head;
                if (h != null && h.waitStatus != 0)
                {
                    UnparkSuccessor(h);
                }
                return true;
            }
            return false;
        }
    
        /**
         * Acquires in shared mode, ignoring interrupts.  Implemented by
         * first invoking at least once {@link #tryAcquireShared},
         * returning on success.  Otherwise the thread is queued, possibly
         * repeatedly blocking and unblocking, invoking {@link
         * #tryAcquireShared} until success.
         *
         * @param arg the acquire argument.  This value is conveyed to
         *        {@link #tryAcquireShared} but is otherwise uninterpreted
         *        and can represent anything you like.
         */
        public void AcquireShared(int arg)
        {
            if (TryAcquireShared(arg) < 0)
            {
                DoAcquireShared(arg);
            }
        }
    
        /**
         * Acquires in shared mode, aborting if interrupted.  Implemented
         * by first checking interrupt status, then invoking at least once
         * {@link #tryAcquireShared}, returning on success.  Otherwise the
         * thread is queued, possibly repeatedly blocking and unblocking,
         * invoking {@link #tryAcquireShared} until success or the thread
         * is interrupted.
         * @param arg the acquire argument
         * This value is conveyed to {@link #tryAcquireShared} but is
         * otherwise uninterpreted and can represent anything
         * you like.
         * @throws ThreadInterruptedException if the current thread is interrupted
         */
        public void AcquireSharedInterruptibly(int arg)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            if (TryAcquireShared(arg) < 0)
            {
                DoAcquireSharedInterruptibly(arg);
            }
        }
    
        /**
         * Attempts to acquire in shared mode, aborting if interrupted, and
         * failing if the given timeout elapses.  Implemented by first
         * checking interrupt status, then invoking at least once {@link
         * #tryAcquireShared}, returning on success.  Otherwise, the
         * thread is queued, possibly repeatedly blocking and unblocking,
         * invoking {@link #tryAcquireShared} until success or the thread
         * is interrupted or the timeout elapses.
         *
         * @param arg the acquire argument.  This value is conveyed to
         *        {@link #tryAcquireShared} but is otherwise uninterpreted
         *        and can represent anything you like.
         * @param nanosTimeout the maximum number of nanoseconds to wait
         * @return {@code true} if acquired; {@code false} if timed out
         * @throws ThreadInterruptedException if the current thread is interrupted
         */
        public bool TryAcquireShared(int arg, long timeout)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            return TryAcquireShared(arg) >= 0 || DoAcquireSharedTimed(arg, TimeSpan.FromMilliseconds(timeout));
        }

        public bool TryAcquireShared(int arg, TimeSpan timeout)
        {
            if (LockSupport.Interrupted())
            {
                throw new ThreadInterruptedException();
            }

            return TryAcquireShared(arg) >= 0 || DoAcquireSharedTimed(arg, timeout);
        }

        /**
         * Releases in shared mode.  Implemented by unblocking one or more
         * threads if {@link #tryReleaseShared} returns true.
         *
         * @param arg the release argument.  This value is conveyed to
         *        {@link #tryReleaseShared} but is otherwise uninterpreted
         *        and can represent anything you like.
         * @return the value returned from {@link #tryReleaseShared}
         */
        public bool ReleaseShared(int arg)
        {
            if (TryReleaseShared(arg))
            {
                DoReleaseShared();
                return true;
            }
            return false;
        }

        #endregion

        #region Queue Inspection Methods

        /**
         * Queries whether any threads are waiting to acquire. Note that
         * because cancellations due to interrupts and timeouts may occur
         * at any time, a {@code true} return does not guarantee that any
         * other thread will ever acquire.
         *
         * <p>In this implementation, this operation returns in
         * constant time.
         *
         * @return {@code true} if there may be other threads waiting to acquire
         */
        public bool HasQueuedThreads
        {
            get { return head != tail; }
        }
    
        /**
         * Queries whether any threads have ever contended to acquire this
         * synchronizer; that is if an acquire method has ever blocked.
         *
         * <p>In this implementation, this operation returns in
         * constant time.
         *
         * @return {@code true} if there has ever been contention
         */
        public bool HasContended
        {
            get { return head != null; }
        }
    
        /**
         * Returns the first (longest-waiting) thread in the queue, or
         * {@code null} if no threads are currently queued.
         *
         * <p>In this implementation, this operation normally returns in
         * constant time, but may iterate upon contention if other threads are
         * concurrently modifying the queue.
         *
         * @return the first (longest-waiting) thread in the queue, or
         *         {@code null} if no threads are currently queued
         */
        public Thread FirstQueuedThread
        {
            get { return (head == tail) ? null : FullGetFirstQueuedThread(); }
        }
    
        /// <summary>
        /// Private Version of FirstQueuedThread called when fast path fails.
        /// </summary>
        private Thread FullGetFirstQueuedThread()
        {
            /*
             * The first node is normally head.next. Try to get its
             * thread field, ensuring consistent reads: If thread
             * field is nulled out or s.prev is no longer head, then
             * some other thread(s) concurrently performed setHead in
             * between some of our reads. We try this twice before
             * resorting to traversal.
             */
            Node h, s;
            Thread st;
            if (((h = head) != null && (s = h.next) != null &&
                 s.prev == head && (st = s.thread) != null) ||
                ((h = head) != null && (s = h.next) != null &&
                 s.prev == head && (st = s.thread) != null))
            {
                return st;
            }

            /*
             * Head's next field might not have been set yet, or may have
             * been unset after setHead. So we must check to see if tail
             * is actually first node. If not, we continue on, safely
             * traversing from tail back to head to find first,
             * guaranteeing termination.
             */
    
            Node t = tail;
            Thread firstThread = null;
            while (t != null && t != head)
            {
                Thread tt = t.thread;
                if (tt != null)
                {
                    firstThread = tt;
                }
                t = t.prev;
            }
            return firstThread;
        }
    
        /**
         * Returns true if the given thread is currently queued.
         *
         * <p>This implementation traverses the queue to determine
         * presence of the given thread.
         *
         * @param thread the thread
         * @return {@code true} if the given thread is on the queue
         * @throws NullPointerException if the thread is null
         */
        public bool IsQueued(Thread thread)
        {
            if (thread == null)
            {
                throw new NullReferenceException();
            }
            for (Node p = tail; p != null; p = p.prev)
            {
                if (p.thread == thread)
                {
                    return true;
                }
            }
            return false;
        }
    
        /**
         * Returns {@code true} if the apparent first queued thread, if one
         * exists, is waiting in exclusive mode.  If this method returns
         * {@code true}, and the current thread is attempting to acquire in
         * shared mode (that is, this method is invoked from {@link
         * #TryAcquireShared}) then it is guaranteed that the current thread
         * is not the first queued thread.  Used only as a heuristic in
         * ReentrantReadWriteLock.
         */
        protected bool ApparentlyFirstQueuedIsExclusive()
        {
            Node h, s;
            return (h = head) != null && (s = h.next) != null && !s.IsShared() && s.thread != null;
        }
    
        /**
         * Queries whether any threads have been waiting to acquire longer
         * than the current thread.
         *
         * <p>An invocation of this method is equivalent to (but may be
         * more efficient than):
         *  <pre> {@code
         * getFirstQueuedThread() != Thread.currentThread() &&
         * hasQueuedThreads()}</pre>
         *
         * <p>Note that because cancellations due to interrupts and
         * timeouts may occur at any time, a {@code true} return does not
         * guarantee that some other thread will acquire before the current
         * thread.  Likewise, it is possible for another thread to win a
         * race to enqueue after this method has returned {@code false},
         * due to the queue being empty.
         *
         * <p>This method is designed to be used by a fair synchronizer to
         * avoid <a href="AbstractQueuedSynchronizer#barging">barging</a>.
         * Such a synchronizer's {@link #tryAcquire} method should return
         * {@code false}, and its {@link #tryAcquireShared} method should
         * return a negative value, if this method returns {@code true}
         * (unless this is a reentrant acquire).  For example, the {@code
         * tryAcquire} method for a fair, reentrant, exclusive mode
         * synchronizer might look like this:
         *
         *  <pre> {@code
         * protected bool tryAcquire(int arg) {
         *   if (IsHeldExclusively()) {
         *     // A reentrant acquire; increment hold count
         *     return true;
         *   } else if (hasQueuedPredecessors()) {
         *     return false;
         *   } else {
         *     // try to acquire normally
         *   }
         * }}</pre>
         *
         * @return {@code true} if there is a queued thread preceding the
         *         current thread, and {@code false} if the current thread
         *         is at the head of the queue or the queue is empty
         */
        protected bool HasQueuedPredecessors()
        {
            // The correctness of this depends on head being initialized
            // before tail and on head.next being accurate if the current
            // thread is first in queue.
            Node t = tail; // Read fields in reverse initialization order
            Node h = head;
            Node s;
            return h != t && ((s = h.next) == null || s.thread != Thread.CurrentThread);
        }

        #endregion

        #region Queue Diagnostic Methods

        /**
         * Returns an estimate of the number of threads waiting to
         * acquire.  The value is only an estimate because the number of
         * threads may change dynamically while this method traverses
         * internal data structures.  This method is designed for use in
         * monitoring system state, not for synchronization
         * control.
         *
         * @return the estimated number of threads waiting to acquire
         */
        public int QueueLength
        {
            get
            {
                int n = 0;
                for (Node p = tail; p != null; p = p.prev)
                {
                    if (p.thread != null)
                    {
                        ++n;
                    }
                }
                return n;
            }
        }

        /**
         * Returns a collection containing threads that may be waiting to
         * acquire.  Because the actual set of threads may change
         * dynamically while constructing this result, the returned
         * collection is only a best-effort estimate.  The elements of the
         * returned collection are in no particular order.  This method is
         * designed to facilitate construction of subclasses that provide
         * more extensive monitoring facilities.
         *
         * @return the collection of threads
         */
        public Collection<Thread> QueuedThreads
        {
            get
            {
                ArrayList<Thread> list = new ArrayList<Thread>();
                for (Node p = tail; p != null; p = p.prev)
                {
                    Thread t = p.thread;
                    if (t != null)
                    {
                        list.Add(t);
                    }
                }
                return list;
            }
        }

        /**
         * Returns a collection containing threads that may be waiting to
         * acquire in exclusive mode. This has the same properties
         * as {@link #getQueuedThreads} except that it only returns
         * those threads waiting due to an exclusive acquire.
         *
         * @return the collection of threads
         */
        public Collection<Thread> ExclusiveQueuedThreads
        {
            get
            {
                ArrayList<Thread> list = new ArrayList<Thread>();
                for (Node p = tail; p != null; p = p.prev)
                {
                    if (!p.IsShared())
                    {
                        Thread t = p.thread;
                        if (t != null)
                        {
                            list.Add(t);
                        }
                    }
                }
                return list;
            }
        }

        /**
         * Returns a collection containing threads that may be waiting to
         * acquire in shared mode. This has the same properties
         * as {@link #getQueuedThreads} except that it only returns
         * those threads waiting due to a shared acquire.
         *
         * @return the collection of threads
         */
        public Collection<Thread> SharedQueuedThreads
        {
            get
            {
                ArrayList<Thread> list = new ArrayList<Thread>();
                for (Node p = tail; p != null; p = p.prev)
                {
                    if (p.IsShared())
                    {
                        Thread t = p.thread;
                        if (t != null)
                        {
                            list.Add(t);
                        }
                    }
                }
                return list;
            }
        }

        /**
         * Returns a string identifying this synchronizer, as well as its state.
         * The state, in brackets, includes the String {@code "State ="}
         * followed by the current value of {@link #getState}, and either
         * {@code "nonempty"} or {@code "empty"} depending on whether the
         * queue is empty.
         *
         * @return a string identifying this synchronizer, as well as its state
         */
        public override String ToString()
        {
            int s = State;
            String q = HasQueuedThreads ? "non" : "";
            return base.ToString() + "[State = " + s + ", " + q + "empty queue]";
        }

        #endregion

        #region Condition Support Methods and Classes

        /// <summary>
        /// Returns true if a node, always one that was initially placed on a condition
        /// queue, is now waiting to reacquire on sync queue.
        /// </summary>
        private bool IsOnSyncQueue(Node node)
        {
            if (node.waitStatus == Node.CONDITION || node.prev == null)
            {
                return false;
            }

            if (node.next != null) // If has successor, it must be on queue
            {
                return true;
            }

            /*
             * node.prev can be non-null, but not yet on queue because
             * the CAS to place it on queue can fail. So we have to
             * traverse from tail to make sure it actually made it.  It
             * will always be near the tail in calls to this method, and
             * unless the CAS failed (which is unlikely), it will be
             * there, so we hardly ever traverse much.
             */
            return FindNodeFromTail(node);
        }
    
        /// <summary>
        /// Returns true if node is on sync queue by searching backwards from tail.
        /// Called only when needed by IsOnSyncQueue.
        /// </summary>
        private bool FindNodeFromTail(Node node)
        {
            Node t = tail;
            for (;;)
            {
                if (t == node)
                {
                    return true;
                }

                if (t == null)
                {
                    return false;
                }

                t = t.prev;
            }
        }
    
        /// <summary>
        /// Transfers a node from a condition queue onto sync queue.
        /// Returns true if successful.
        /// </summary>
        private bool TransferForSignal(Node node)
        {
            /*
             * If cannot change waitStatus, the node has been cancelled.
             */
            if (!CompareAndSetWaitStatus(node, Node.CONDITION, 0))
            {
                return false;
            }
    
            /*
             * Splice onto queue and try to set waitStatus of predecessor to
             * indicate that thread is (probably) waiting. If cancelled or
             * attempt to set waitStatus fails, wake up to resync (in which
             * case the waitStatus can be transiently and harmlessly wrong).
             */
            Node p = Enqueue(node);
            int ws = p.waitStatus;
            if (ws > 0 || !CompareAndSetWaitStatus(p, ws, Node.SIGNAL))
            {
                LockSupport.UnPark(node.thread);
            }
            return true;
        }
    
        /// <summary>
        /// Transfers node, if necessary, to sync queue after a cancelled wait.
        /// Returns true if thread was cancelled before being signalled.
        /// </summary>
        private bool TransferAfterCancelledWait(Node node)
        {
            if (CompareAndSetWaitStatus(node, Node.CONDITION, 0))
            {
                Enqueue(node);
                return true;
            }

            /*
             * If we lost out to a signal(), then we can't proceed
             * until it finishes its enq().  Cancelling during an
             * incomplete transfer is both rare and transient, so just
             * spin.
             */
            while (!IsOnSyncQueue(node))
            {
                Thread.SpinWait(1);
            }

            return false;
        }
    
        /**
         * Invokes release with current state value; returns saved state.
         * Cancels node and throws exception on failure.
         * @param node the condition node for this wait
         * @return previous sync state
         */
        private int FullyRelease(Node node)
        {
            bool failed = true;
            try
            {
                int savedState = State;
                if (Release(savedState))
                {
                    failed = false;
                    return savedState;
                }
                else
                {
                    throw new ThreadStateException();
                }
            }
            finally
            {
                if (failed)
                {
                    node.waitStatus = Node.CANCELLED;
                }
            }
        }

        /**
         * Queries whether the given ConditionObject
         * uses this synchronizer as its lock.
         *
         * @param condition the condition
         * @return <tt>true</tt> if owned
         * @throws NullPointerException if the condition is null
         */
        public bool Owns(ConditionObject condition)
        {
            if (condition == null)
            {
                throw new NullReferenceException();
            }
            return condition.IsOwnedBy(this);
        }
    
        /**
         * Queries whether any threads are waiting on the given condition
         * associated with this synchronizer. Note that because timeouts
         * and interrupts may occur at any time, a <tt>true</tt> return
         * does not guarantee that a future <tt>signal</tt> will awaken
         * any threads.  This method is designed primarily for use in
         * monitoring of the system state.
         *
         * @param condition the condition
         * @return <tt>true</tt> if there are any waiting threads
         * @throws ThreadStateException if exclusive synchronization
         *         is not held
         * @throws IllegalArgumentException if the given condition is
         *         not associated with this synchronizer
         * @throws NullPointerException if the condition is null
         */
        public bool HasWaiters(ConditionObject condition)
        {
            if (!Owns(condition))
            {
                throw new ThreadStateException("Not owner");
            }

            return condition.HasWaiters;
        }
    
        /**
         * Returns an estimate of the number of threads waiting on the
         * given condition associated with this synchronizer. Note that
         * because timeouts and interrupts may occur at any time, the
         * estimate serves only as an upper bound on the actual number of
         * waiters.  This method is designed for use in monitoring of the
         * system state, not for synchronization control.
         *
         * @param condition the condition
         * @return the estimated number of waiting threads
         * @throws ThreadStateException if exclusive synchronization
         *         is not held
         * @throws IllegalArgumentException if the given condition is
         *         not associated with this synchronizer
         * @throws NullPointerException if the condition is null
         */
        public int GetWaitQueueLength(ConditionObject condition)
        {
            if (!Owns(condition))
            {
                throw new ThreadStateException("Not owner");
            }

            return condition.WaitQueueLength;
        }
    
        /// <summary>
        /// Returns a collection containing those threads that may be waiting on the
        /// given condition associated with this synchronizer.  Because the actual
        /// set of threads may change dynamically while constructing this result,
        /// the returned collection is only a best-effort estimate.  The elements
        /// of the returned collection are in no particular order.
        /// </summary>
        /// <exception cref='ArgumentException'>
        /// Is thrown when the given condition is not associated with this synchronizer.
        /// </exception>
        /// <exception cref='ThreadStateException'>
        /// Is thrown when exclusive synchronization is not held.
        /// </exception>
        /// <exception cref='NullReferenceException'>
        /// Is thrown when the Condition object passed is null.
        /// </exception>
        public Collection<Thread> GetWaitingThreads(ConditionObject condition)
        {
            if (!Owns(condition))
            {
                throw new ArgumentException("Not owner");
            }
            return condition.WaitingThreads;
        }

        /// <summary>
        /// Condition implementation for a AbstractQueuedSynchronizer serving as
        /// the basis of a Lock implementation.
        /// </summary>
        public class ConditionObject : Condition
        {
            // First node of condition queue.
            private Node firstWaiter;
            // Last node of condition queue.
            private Node lastWaiter;
            // The synchronizer that owns this Condition.
            private AbstractQueuedSynchronizer parent;

            /// <summary>
            /// Creates a new <tt>ConditionObject</tt> instance.
            /// </summary>
            public ConditionObject(AbstractQueuedSynchronizer parent) : base()
            {
                this.parent = parent;
            }

            /// <summary>
            /// Adds a new waiter to wait queue. Return its new wait node.
            /// </summary>
            private Node AddConditionWaiter()
            {
                Node t = lastWaiter;
                // If lastWaiter is cancelled, clean out.
                if (t != null && t.waitStatus != Node.CONDITION)
                {
                    UnlinkCancelledWaiters();
                    t = lastWaiter;
                }
                Node node = new Node(Thread.CurrentThread, Node.CONDITION);
                if (t == null)
                {
                    firstWaiter = node;
                }
                else
                {
                    t.nextWaiter = node;
                }

                lastWaiter = node;
                return node;
            }
    
            /// <summary>
            /// Removes and transfers nodes until hit non-cancelled one or null.
            /// </summary>
            private void DoSignal(Node first)
            {
                do
                {
                    if ( (firstWaiter = first.nextWaiter) == null)
                    {
                        lastWaiter = null;
                    }
                    first.nextWaiter = null;
                }
                while (!parent.TransferForSignal(first) && (first = firstWaiter) != null);
            }
    
            /// <summary>
            /// Removes and transfers all nodes.
            /// </summary>
            private void DoSignalAll(Node first)
            {
                lastWaiter = firstWaiter = null;
                do
                {
                    Node next = first.nextWaiter;
                    first.nextWaiter = null;
                    parent.TransferForSignal(first);
                    first = next;
                }
                while (first != null);
            }
    
            /// <summary>
            /// Unlinks cancelled waiter nodes from condition queue.  Called only while
            /// holding lock. This is called when cancellation occurred during condition
            /// wait, and upon insertion of a new waiter when lastWaiter is seen to have
            /// been cancelled. This method is needed to avoid garbage retention in the
            /// absence of signals.  So even though it may require a full traversal, it
            /// comes into play only when timeouts or cancellations occur in the absence
            /// of signals. It traverses all nodes rather than stopping at a particular
            /// target to unlink all pointers to garbage nodes without requiring many
            /// re-traversals during cancellation storms.
            /// </summary>
            private void UnlinkCancelledWaiters()
            {
                Node t = firstWaiter;
                Node trail = null;
                while (t != null)
                {
                    Node next = t.nextWaiter;
                    if (t.waitStatus != Node.CONDITION)
                    {
                        t.nextWaiter = null;
                        if (trail == null)
                        {
                            firstWaiter = next;
                        }
                        else
                        {
                            trail.nextWaiter = next;
                        }

                        if (next == null)
                        {
                            lastWaiter = trail;
                        }
                    }
                    else
                    {
                        trail = t;
                    }

                    t = next;
                }
            }

            /// <summary>
            /// Moves the longest-waiting thread, if one exists, from the wait queue for this
            /// condition to the wait queue for the owning lock.
            /// </summary>
            public void Signal()
            {
                if (!parent.IsHeldExclusively())
                {
                    throw new ThreadStateException();
                }
                Node first = firstWaiter;
                if (first != null)
                {
                    DoSignal(first);
                }
            }
    
            /// <summary>
            /// Moves all threads from the wait queue for this condition to the wait
            /// queue for the owning lock.  Throws ThreadStateException if IsHeldExclusively
            /// returns false.
            /// </summary>
            public void SignalAll()
            {
                if (!parent.IsHeldExclusively())
                {
                    throw new ThreadStateException();
                }
                Node first = firstWaiter;
                if (first != null)
                {
                    DoSignalAll(first);
                }
            }
    
            /**
             * Implements uninterruptible condition wait.
             * <ol>
             * <li> Save lock state returned by {@link #getState}.
             * <li> Invoke {@link #release} with
             *      saved state as argument, throwing
             *      ThreadStateException if it fails.
             * <li> Block until signalled.
             * <li> Reacquire by invoking specialized version of
             *      {@link #acquire} with saved state as argument.
             * </ol>
             */
            public void AwaitUninterruptibly()
            {
                Node node = AddConditionWaiter();
                int savedState = parent.FullyRelease(node);
                bool interrupted = false;

                while (!parent.IsOnSyncQueue(node))
                {
                    LockSupport.Park();
                    if (LockSupport.Interrupted())
                    {
                        interrupted = true;
                    }
                }

                if (parent.AcquireQueued(node, savedState) || interrupted)
                {
                    SelfInterrupt();
                }
            }
    
            /*
             * For interruptible waits, we need to track whether to throw
             * ThreadInterruptedException, if interrupted while blocked on
             * condition, versus reinterrupt current thread, if
             * interrupted while blocked waiting to re-acquire.
             */
    
            // Mode meaning to reinterrupt on exit from wait
            private const int REINTERRUPT =  1;
            // Mode meaning to throw ThreadInterruptedException on exit from wait
            private const int THROW_IE    = -1;
    
            /// <summary>
            /// Checks for interrupt, returning THROW_IE if interrupted before signalled,
            /// REINTERRUPT if after signalled, or 0 if not interrupted.
            /// </summary>
            private int CheckInterruptWhileWaiting(Node node)
            {
                return LockSupport.Interrupted() ?
                    parent.TransferAfterCancelledWait(node) ? THROW_IE : REINTERRUPT : 0;
            }
    
            /// <summary>
            /// Throws ThreadInterruptedException, reinterrupts current thread, or
            /// does nothing, depending on mode.
            /// </summary>
            private void ReportInterruptAfterWait(int interruptMode)
            {
                if (interruptMode == THROW_IE)
                {
                    throw new ThreadInterruptedException();
                }
                else if (interruptMode == REINTERRUPT)
                {
                    SelfInterrupt();
                }
            }
    
            /// <summary>
            /// Implements interruptible condition wait.
            /// <list type="bullet">
            /// <item>If current thread is interrupted, throw ThreadInterruptedException.</item>
            /// <item>Save lock state returned by State</item>
            /// <item>Invoke Release with saved state as argument, throwing
            /// ThreadStateException if it fails.
            /// </item>
            /// <item>Block until signalled or interrupted.</item>
            /// <item>Reacquire by invoking specialized version of Acquire with saved State</item>
            /// <item>If interrupted while blocked in step 4, throw ThreadInterruptedException.</item>
            /// </list>
            /// Throws ThreadInterruptedException if the current thread is interrupted (and
            /// interruption of thread suspension is supported).
            /// </summary>
            public void Await()
            {
                if (LockSupport.Interrupted())
                {
                    throw new ThreadInterruptedException();
                }

                Node node = AddConditionWaiter();
                int savedState = parent.FullyRelease(node);
                int interruptMode = 0;

                while (!parent.IsOnSyncQueue(node))
                {
                    LockSupport.Park();
                    if ((interruptMode = CheckInterruptWhileWaiting(node)) != 0)
                    {
                        break;
                    }
                }

                if (parent.AcquireQueued(node, savedState) && interruptMode != THROW_IE)
                {
                    interruptMode = REINTERRUPT;
                }
                if (node.nextWaiter != null) // clean up if cancelled
                {
                    UnlinkCancelledWaiters();
                }
                if (interruptMode != 0)
                {
                    ReportInterruptAfterWait(interruptMode);
                }
            }
    
            /**
             * Implements timed condition wait.
             * <ol>
             * <li> If current thread is interrupted, throw ThreadInterruptedException.
             * <li> Save lock state returned by {@link #getState}.
             * <li> Invoke {@link #release} with
             *      saved state as argument, throwing
             *      ThreadStateException if it fails.
             * <li> Block until signalled, interrupted, or timed out.
             * <li> Reacquire by invoking specialized version of
             *      {@link #acquire} with saved state as argument.
             * <li> If interrupted while blocked in step 4, throw ThreadInterruptedException.
             * </ol>
             *
             * @param nanosTimeout the maximum time to wait, in nanoseconds
             * @return A value less than or equal to zero if the wait has
             * timed out; otherwise an estimate, that
             * is strictly less than the <tt>nanosTimeout</tt> argument,
             * of the time still remaining when this method returned.
             *
             * @throws ThreadInterruptedException if the current thread is interrupted (and
             * interruption of thread suspension is supported).
             */
            public int Await(int timeout)
            {
                return Await(TimeSpan.FromMilliseconds(timeout));
            }

            public int Await(TimeSpan timeout)
            {
                if (LockSupport.Interrupted())
                {
                    throw new ThreadInterruptedException();
                }

                TimeSpan originalTimeout = timeout;
                Node node = AddConditionWaiter();
                int savedState = parent.FullyRelease(node);

                DateTime deadline = DateTime.Now;

                if(timeout > TimeSpan.Zero)
                {
                    deadline += timeout;
                }

                int interruptMode = 0;

                while (!parent.IsOnSyncQueue(node))
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        parent.TransferAfterCancelledWait(node);
                        break;
                    }

                    LockSupport.Park(timeout);

                    if ((interruptMode = CheckInterruptWhileWaiting(node)) != 0)
                    {
                        break;
                    }
    
                    DateTime awakeTime = DateTime.Now;
                    if(awakeTime > deadline)
                    {
                        timeout = TimeSpan.Zero;
                    }
                    else
                    {
                        timeout = deadline - awakeTime;
                    }
                }

                if (parent.AcquireQueued(node, savedState) && interruptMode != THROW_IE)
                {
                    interruptMode = REINTERRUPT;
                }
                if (node.nextWaiter != null)
                {
                    UnlinkCancelledWaiters();
                }
                if (interruptMode != 0)
                {
                    ReportInterruptAfterWait(interruptMode);
                }

                return (int)(originalTimeout - timeout).TotalMilliseconds;
            }
    
            /**
             * Implements absolute timed condition wait.
             * <ol>
             * <li> If current thread is interrupted, throw ThreadInterruptedException.
             * <li> Save lock state returned by {@link #getState}.
             * <li> Invoke {@link #release} with
             *      saved state as argument, throwing
             *      ThreadStateException if it fails.
             * <li> Block until signalled, interrupted, or timed out.
             * <li> Reacquire by invoking specialized version of
             *      {@link #acquire} with saved state as argument.
             * <li> If interrupted while blocked in step 4, throw ThreadInterruptedException.
             * <li> If timed out while blocked in step 4, return false, else true.
             * </ol>
             *
             * @param deadline the absolute time to wait until
             * @return <tt>false</tt> if the deadline has
             * elapsed upon return, else <tt>true</tt>.
             *
             * @throws ThreadInterruptedException if the current thread is interrupted (and
             * interruption of thread suspension is supported).
             */
            public bool AwaitUntil(DateTime deadline)
            {
                if (LockSupport.Interrupted())
                {
                    throw new ThreadInterruptedException();
                }

                Node node = AddConditionWaiter();
                int savedState = parent.FullyRelease(node);
                bool timedout = false;
                int interruptMode = 0;

                while (!parent.IsOnSyncQueue(node))
                {
                    if (DateTime.Now > deadline)
                    {
                        timedout = parent.TransferAfterCancelledWait(node);
                        break;
                    }

                    LockSupport.ParkUntil(deadline);

                    if ((interruptMode = CheckInterruptWhileWaiting(node)) != 0)
                    {
                        break;
                    }
                }

                if (parent.AcquireQueued(node, savedState) && interruptMode != THROW_IE)
                {
                    interruptMode = REINTERRUPT;
                }
                if (node.nextWaiter != null)
                {
                    UnlinkCancelledWaiters();
                }
                if (interruptMode != 0)
                {
                    ReportInterruptAfterWait(interruptMode);
                }

                return !timedout;
            }

            /// <summary>
            /// Returns true if this condition was created by the given synchronization object.
            /// </summary>
            internal bool IsOwnedBy(AbstractQueuedSynchronizer sync)
            {
                return ReferenceEquals(sync, parent);
            }
    
            /**
             * Queries whether any threads are waiting on this condition.
             * Implements {@link AbstractQueuedSynchronizer#hasWaiters}.
             *
             * @return {@code true} if there are any waiting threads
             * @throws ThreadStateException if {@link #IsHeldExclusively}
             *         returns {@code false}
             */
            internal bool HasWaiters
            {
                get
                {
                    if (!parent.IsHeldExclusively())
                    {
                        throw new ThreadStateException();
                    }

                    for (Node w = firstWaiter; w != null; w = w.nextWaiter)
                    {
                        if (w.waitStatus == Node.CONDITION)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            /**
             * Returns an estimate of the number of threads waiting on
             * this condition.
             * Implements {@link AbstractQueuedSynchronizer#getWaitQueueLength}.
             *
             * @return the estimated number of waiting threads
             * @throws ThreadStateException if {@link #IsHeldExclusively}
             *         returns {@code false}
             */
            internal int WaitQueueLength
            {
                get
                {
                    if (!parent.IsHeldExclusively())
                    {
                        throw new ThreadStateException();
                    }
                    int n = 0;
                    for (Node w = firstWaiter; w != null; w = w.nextWaiter)
                    {
                        if (w.waitStatus == Node.CONDITION)
                        {
                            ++n;
                        }
                    }
                    return n;
                }
            }

            /**
             * Returns a collection containing those threads that may be
             * waiting on this Condition.
             * Implements {@link AbstractQueuedSynchronizer#getWaitingThreads}.
             *
             * @return the collection of threads
             * @throws ThreadStateException if {@link #IsHeldExclusively}
             *         returns {@code false}
             */
            internal Collection<Thread> WaitingThreads
            {
                get
                {
                    if (!parent.IsHeldExclusively())
                    {
                        throw new ThreadStateException();
                    }

                    ArrayList<Thread> list = new ArrayList<Thread>();
                    for (Node w = firstWaiter; w != null; w = w.nextWaiter)
                    {
                        if (w.waitStatus == Node.CONDITION)
                        {
                            Thread t = w.thread;
                            if (t != null)
                            {
                                list.Add(t);
                            }
                        }
                    }
                    return list;
                }
            }
        }

        #endregion

        #region Atomic State Modifiers

        /// <summary>
        /// Compares the and set head, called from Enqueue.  Return true if success.
        /// </summary>
        private bool CompareAndSetHead(Node update)
        {
            return Interlocked.CompareExchange(ref head, update, null) == null;
        }

        /// <summary>
        /// Compares the and set tail, called from Enqueue.  Return true if success.
        /// </summary>
        private bool CompareAndSetTail(Node expect, Node update)
        {
            return Interlocked.CompareExchange(ref tail, update, expect) == expect;
        }

        /// <summary>
        /// Compares the and set wait status of the given node.  Return true if success.
        /// </summary>
        private static bool CompareAndSetWaitStatus(Node node, int expect, int update)
        {
            return Interlocked.CompareExchange(ref node.waitStatus, update, expect) == expect;
        }

        /// <summary>
        /// Compares the and set next of the given node.  Return true if success.
        /// </summary>
        private static bool CompareAndSetNext(Node node, Node expect, Node update)
        {
            return Interlocked.CompareExchange(ref node.next, update, expect) == expect;
        }

        #endregion
    }
}

