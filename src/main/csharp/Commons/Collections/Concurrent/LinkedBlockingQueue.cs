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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    public class LinkedBlockingQueue<E> : AbstractQueue<E>, BlockingQueue<E> where E : class
    {
        #region Private implementation detials

        private class Node<T>
        {
            public T item;

            /**
             * One of:
             * - the real successor Node
             * - this Node, meaning the successor is head.next
             * - null, meaning there is no successor (this is the last node)
             */
            public Node<T> next;

            public Node(T x) { item = x; }
        }

        /** The capacity bound, or Integer.MAX_VALUE if none */
        private readonly int capacity;

        /** Current number of elements, updated Atomically */
        private long count;

        /**
         * Head of linked list.
         * Invariant: head.item == null
         */
        private Node<E> head;
    
        /**
         * Tail of linked list.
         * Invariant: last.next == null
         */
        private Node<E> last;

        /** Lock held by take, poll, etc */
        private readonly Mutex takeLock = new Mutex();
    
        /** Wait queue for waiting takes */
        private readonly AutoResetEvent notEmpty = new AutoResetEvent(false);

        /** Lock held by put, offer, etc */
        private readonly Mutex putLock = new Mutex();
    
        /** Wait queue for waiting puts */
        private readonly AutoResetEvent notFull = new AutoResetEvent(false);

        /**
         * Signals a waiting take. Called only from put/offer (which do not
         * otherwise ordinarily lock takeLock.)
         */
        private void SignalNotEmpty()
        {
            lock(takeLock)
            {
                notEmpty.Set();
            }
        }

        /**
         * Signals a waiting put. Called only from take/poll.
         */
        private void SignalNotFull()
        {
            lock(putLock)
            {
                notFull.Set();
            }
        }
    
        /**
         * Creates a node and links it at end of queue.
         */
        private void Enqueue(E x)
        {
            last = last.next = new Node<E>(x);
        }

        /**
         * Removes a node from head of queue.
         */
        private E Dequeue()
        {
            Node<E> h = head;
            Node<E> first = h.next;
            h.next = h; // help GC
            head = first;
            E x = first.item;
            first.item = null;
            return x;
        }

        /**
         * Lock to prevent both puts and takes.
         */
        private void FullyLock()
        {
            putLock.WaitOne();
            takeLock.WaitOne();
        }
    
        /**
         * Unlock to allow both puts and takes.
         */
        private void FullyUnlock()
        {
            takeLock.ReleaseMutex();
            putLock.ReleaseMutex();
        }

        private void Unlink(Node<E> p, Node<E> trail)
        {
            // p.next is not changed, to allow iterators that are
            // traversing p to maintain their weak-consistency guarantee.
            p.item = null;
            trail.next = p.next;

            if (last == p)
            {
                last = trail;
            }

            if ((int) Interlocked.Decrement(ref count) == capacity)
            {
                notEmpty.Set();
            }
        }

        #endregion

        public LinkedBlockingQueue() : this(Int32.MaxValue)
        {
        }

        public LinkedBlockingQueue(int capacity) : base()
        {
            if (capacity <= 0) throw new ArgumentException();
            this.capacity = capacity;
            last = head = new Node<E>(null);
        }

        public LinkedBlockingQueue(Collection<E> c) : this(Int32.MaxValue)
        {
            lock(putLock)
            {
                int n = 0;
                Iterator<E> iterator = c.Iterator();
                while(iterator.HasNext)
                {
                    E e = iterator.Next();

                    if (e == null)
                    {
                        throw new NullReferenceException();
                    }

                    if (n == capacity)
                    {
                        throw new IllegalStateException("Queue full");
                    }

                    Enqueue(e);
                    ++n;
                }

                // Never contended here, so no atomic op is needed.
                this.count = n;
            }
        }

        public override int Size()
        {
            return (int) Interlocked.Read(ref count);
        }

        public virtual int RemainingCapacity()
        {
            return capacity - (int) Interlocked.Read(ref count);
        }

        public virtual void Put(E e)
        {
            if (e == null) throw new NullReferenceException();

            // Note: convention in all put/take/etc is to preset local var
            // holding count negative to indicate failure unless set.
            int c = -1;
            Monitor.Enter(putLock);
            try
            {
                /*
                 * Note that count is used in wait guard even though it is
                 * not protected by lock. This works because count can
                 * only decrease at this point (all other puts are shut
                 * out by lock), and we (or some other waiting put) are
                 * signalled if it ever changes from capacity. Similarly
                 * for all other uses of count in other wait guards.
                 */
                while ((int) Interlocked.Read(ref count) == capacity)
                {
                    Monitor.Exit(putLock);
                    notFull.WaitOne();
                    Monitor.Enter(putLock);
                }

                Enqueue(e);
                c = (int) Interlocked.Increment(ref count);
                if (c + 1 < capacity)
                {
                    notFull.Set();
                }
            }
            finally
            {
                Monitor.Exit(putLock);
            }

            if (c == 0)
            {
                SignalNotEmpty();
            }
        }

        public virtual bool Offer(E e, int timeout)
        {
            return this.Offer(e, TimeSpan.FromMilliseconds(timeout));
        }

        public virtual bool Offer(E e, TimeSpan timeout)
        {
            if (e == null) throw new NullReferenceException();
            int c = -1;
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            Monitor.Enter(putLock);
            try
            {
                while ((int) Interlocked.Read(ref count) == capacity)
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return false;
                    }

                    Monitor.Exit(putLock);
                    notFull.WaitOne(timeout);
                    Monitor.Enter(putLock);

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

                Enqueue(e);
                c = (int) Interlocked.Increment(ref count);
                if (c + 1 < capacity)
                {
                    notFull.Set();
                }
            }
            finally
            {
                Monitor.Exit(putLock);
            }

            if (c == 0)
            {
                SignalNotEmpty();
            }

            return true;
        }

        public override bool Offer(E e)
        {
            if (e == null) throw new NullReferenceException();
            if ((int) Interlocked.Read(ref count) == capacity)
            {
                return false;
            }

            int c = -1;
            Monitor.Enter(putLock);
            try
            {
                if ((int) Interlocked.Read(ref count) < capacity)
                {
                    Enqueue(e);
                    c = (int) Interlocked.Increment(ref count);
                    if (c + 1 < capacity)
                    {
                        notFull.Set();
                    }
                }
            }
            finally
            {
                Monitor.Exit(putLock);
            }

            if (c == 0)
            {
                SignalNotEmpty();
            }

            return c >= 0;
        }

        public virtual E Take()
        {
            E x;
            int c = -1;

            Monitor.Enter(takeLock);
            try
            {
                while ((int) Interlocked.Read(ref count) == 0)
                {
                    Monitor.Exit(takeLock);
                    notEmpty.WaitOne();
                    Monitor.Enter(takeLock);
                }

                x = Dequeue();
                c = (int) Interlocked.Decrement(ref count);

                if (c > 1)
                {
                    notEmpty.Set();
                }
            }
            finally
            {
                Monitor.Exit(takeLock);
            }

            if (c == capacity)
            {
                SignalNotFull();
            }

            return x;
        }

        public virtual E Poll(int timeout)
        {
            return this.Poll(TimeSpan.FromMilliseconds(timeout));
        }

        public virtual E Poll(TimeSpan timeout)
        {
            E x = null;
            int c = -1;
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            Monitor.Enter(takeLock);
            try
            {
                while ((int) Interlocked.Read(ref count) == 0)
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return null;
                    }

                    Monitor.Exit(takeLock);
                    notEmpty.WaitOne(timeout);
                    Monitor.Enter(takeLock);

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

                x = Dequeue();
                c = (int) Interlocked.Decrement(ref count);

                if (c > 1)
                {
                    notEmpty.Set();
                }
            }
            finally
            {
                Monitor.Exit(takeLock);
            }

            if (c == capacity)
            {
                SignalNotFull();
            }

            return x;
        }

        public override E Poll()
        {
            if ((int) Interlocked.Read(ref count) == 0)
            {
                return null;
            }

            E x = null;
            int c = -1;

            Monitor.Enter(takeLock);
            try
            {
                if ((int) Interlocked.Read(ref count) > 0)
                {
                    x = Dequeue();
                    c = (int) Interlocked.Decrement(ref count);
                    if (c > 1)
                    {
                        Monitor.Pulse(takeLock);
                    }
                }
            }
            finally
            {
                Monitor.Exit(takeLock);
            }

            if (c == capacity)
            {
                SignalNotFull();
            }

            return x;
        }

        public override E Peek()
        {
            if ((int) Interlocked.Read(ref count) == 0)
            {
                return null;
            }

            Monitor.Enter(takeLock);
            try
            {
                Node<E> first = head.next;
                if (first == null)
                {
                    return null;
                }
                else
                {
                    return first.item;
                }
            }
            finally
            {
                Monitor.Exit(takeLock);
            }
        }

        public override bool Remove(E e)
        {
            if (e == null) return false;

            FullyLock();
            try
            {
                for (Node<E> trail = head, p = trail.next; p != null; trail = p, p = p.next)
                {
                    if (e.Equals(p.item))
                    {
                        Unlink(p, trail);
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                FullyUnlock();
            }
        }

        public override E[] ToArray()
        {
            FullyLock();
            try
            {
                int size = (int) Interlocked.Read(ref count);
                E[] a = new E[size];
                int k = 0;
                for (Node<E> p = head.next; p != null; p = p.next)
                {
                    a[k++] = p.item;
                }
                return a;
            }
            finally
            {
                FullyUnlock();
            }
        }

        public override String ToString()
        {
            FullyLock();
            try
            {
                return base.ToString();
            }
            finally
            {
                FullyUnlock();
            }
        }

        /// <summary>
        /// Atomically removes all of the elements from this queue.
        /// </summary>
        public override void Clear()
        {
            FullyLock();
            try
            {
                for (Node<E> p, h = head; (p = h.next) != null; h = p)
                {
                    h.next = h;
                    p.item = null;
                }
                head = last;

                if ((int) Interlocked.Exchange(ref count, 0) == capacity)
                {
                    notFull.Set();
                }
            }
            finally
            {
                FullyUnlock();
            }
        }

        public virtual int DrainTo(Collection<E> c)
        {
            return DrainTo(c, Int32.MaxValue);
        }

        public virtual int DrainTo(Collection<E> c, int maxElements)
        {
            if (c == null) throw new NullReferenceException();
            if (ReferenceEquals(c, this)) throw new ArgumentException();

            bool signalNotFull = false;
            Monitor.Enter(takeLock);
            try
            {
                int n = Math.Min(maxElements, (int) Interlocked.Read(ref count));
                // count provides visibility to first n Nodes
                Node<E> h = head;
                int i = 0;
                try
                {
                    while (i < n)
                    {
                        Node<E> p = h.next;
                        c.Add(p.item);
                        p.item = null;
                        h.next = h;
                        h = p;
                        ++i;
                    }
                    return n;

                }
                finally
                {
                    // Restore invariants even if c.add() threw
                    if (i > 0)
                    {
                        // assert h.item == null;
                        head = h;
                        signalNotFull = (int) Interlocked.Add(ref count, -i) == capacity;
                    }
                }
            }
            finally
            {
                Monitor.Exit(takeLock);
                if (signalNotFull) SignalNotFull();
            }
        }

        public override Iterator<E> Iterator()
        {
            return new LBQIterator(this);
        }

        #region Iteratr Class Definition

        private class LBQIterator : Iterator<E>
        {
            /*
             * Basic weakly-consistent iterator.  At all times hold the next
             * item to hand out so that if hasNext() reports true, we will
             * still have it to return even if lost race with a take etc.
             */
            private Node<E> current;
            private Node<E> lastRet;
            private E currentElement;
            private readonly LinkedBlockingQueue<E> parent;

            public LBQIterator(LinkedBlockingQueue<E> parent)
            {
                this.parent = parent;

                parent.FullyLock();
                try {
                    current = parent.head.next;
                    if (current != null)
                    {
                        currentElement = current.item;
                    }
                }
                finally
                {
                    parent.FullyUnlock();
                }
            }
    
            public bool HasNext
            {
                get { return current != null; }
            }
    
            /**
             * Unlike other traversal methods, iterators need to handle:
             * - dequeued nodes (p.next == p)
             * - interior removed nodes (p.item == null)
             */
            private Node<E> NextNode(Node<E> p)
            {
                Node<E> s = p.next;
                if (p == s)
                {
                    return parent.head.next;
                }

                // Skip over removed nodes.
                // May be necessary if multiple interior Nodes are removed.
                while (s != null && s.item == null)
                {
                    s = s.next;
                }
                return s;
            }
    
            public E Next()
            {
                parent.FullyLock();
                try
                {
                    if (current == null) throw new NoSuchElementException();
                    E x = currentElement;
                    lastRet = current;
                    current = NextNode(current);
                    currentElement = (current == null) ? null : current.item;
                    return x;
                }
                finally
                {
                    parent.FullyUnlock();
                }
            }
    
            public void Remove()
            {
                if (lastRet == null) throw new IllegalStateException();

                parent.FullyLock();
                try
                {
                    Node<E> node = lastRet;
                    lastRet = null;
                    for (Node<E> trail = parent.head, p = trail.next; p != null;
                         trail = p, p = p.next)
                    {
                        if (p == node)
                        {
                            parent.Unlink(p, trail);
                            break;
                        }
                    }
                }
                finally
                {
                    parent.FullyUnlock();
                }
            }
        }

        #endregion
    }
}

