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

using Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    public class LinkedBlockingDeque<E> : AbstractQueue<E>, BlockingDeque<E> where E : class
    {
        #region Private implementation region

        /** Doubly-linked list node class */
        private sealed class Node<T>
        {
            internal T item;

            /**
             * One of:
             * - the real predecessor Node
             * - this Node, meaning the predecessor is tail
             * - null, meaning there is no predecessor
             */
            internal Node<T> prev;

            /**
             * One of:
             * - the real successor Node
             * - this Node, meaning the successor is head
             * - null, meaning there is no successor
             */
            internal Node<T> next;

            internal Node(T x, Node<T> p, Node<T> n)
            {
                item = x;
                prev = p;
                next = n;
            }
        }

        /// <summary>
        /// Pointer to first node.
        /// Invariant: (first == null AND last == null) ||
        ///            (first.prev == null AND first.item != null)
        /// </summary>
        Node<E> first;

        /// <summary>
        /// Pointer to last node.
        /// Invariant: (first == null AND last == null) ||
        ///            (last.next == null AND last.item != null)
        /// </summary>
        Node<E> last;
    
        /** Number of items in the deque */
        private int count;
    
        /** Maximum number of items in the deque */
        private readonly int capacity;
    
        /** Main lock guarding all access */
        private readonly ReentrantLock mutex = new ReentrantLock();

        /// Condition for waiting takes
        private readonly Condition notEmpty;
    
        /// Condition for waiting puts
        private readonly Condition notFull;

        /// <summary>
        /// Links e as the first element, or returns false is the Deque is full.
        /// </summary>
        private bool LinkFirst(E e)
        {
            if (count >= capacity) return false;

            Node<E> f = first;
            Node<E> x = new Node<E>(e, null, f);
            first = x;

            if (last == null)
            {
                last = x;
            }
            else
            {
                f.prev = x;
            }
            ++count;
            notEmpty.Signal();
            return true;
        }

        /// <summary>
        /// Links e as last element, or returns false if the Deqye full.
        /// </summary>
        private bool LinkLast(E e)
        {
            if (count >= capacity) return false;

            Node<E> l = last;
            Node<E> x = new Node<E>(e, l, null);
            last = x;

            if (first == null)
            {
                first = x;
            }
            else
            {
                l.next = x;
            }

            ++count;
            notEmpty.Signal();
            return true;
        }

        /// <summary>
        /// Removes and returns first element, or null if empty.
        /// </summary>
        private E UnlinkFirst()
        {
            Node<E> f = first;

            if (f == null) return null;

            Node<E> n = f.next;
            E item = f.item;
            f.item = null;
            f.next = f; // help GC
            first = n;

            if (n == null)
            {
                last = null;
            }
            else
            {
                n.prev = null;
            }

            --count;
            notFull.Signal();
            return item;
        }
        
        /// <summary>
        /// Removes and returns last element, or null if empty.
        /// </summary>
        private E UnlinkLast()
        {
            Node<E> l = last;

            if (l == null) return null;

            Node<E> p = l.prev;
            E item = l.item;
            l.item = null;
            l.prev = l; // help GC
            last = p;

            if (p == null)
            {
                first = null;
            }
            else
            {
                p.next = null;
            }

            --count;
            notFull.Signal();
            return item;
        }
        
        /// <summary>
        /// Unlink the Node specified x from the Deque
        /// </summary>
        void Unlink(Node<E> x)
        {
            Node<E> p = x.prev;
            Node<E> n = x.next;

            if (p == null)
            {
                UnlinkFirst();
            }
            else if (n == null)
            {
                UnlinkLast();
            }
            else
            {
                p.next = n;
                n.prev = p;
                x.item = null;
                // Don't mess with x's links.  They may still be in use by an iterator.
                --count;
                notFull.Signal();
            }
        }

        #endregion

        public LinkedBlockingDeque() : this(Int32.MaxValue)
        {
        }

        public LinkedBlockingDeque(int capacity) : base()
        {
            this.notEmpty = mutex.NewCondition();
            this.notFull = mutex.NewCondition();

            if (capacity <= 0)
            {
                throw new ArgumentException();
            }
            this.capacity = capacity;
        }

        public LinkedBlockingDeque(Collection<E> c) : this(Int32.MaxValue)
        {
            mutex.Lock();
            try
            {
                Iterator<E> iterator = c.Iterator();
                while(iterator.HasNext)
                {
                    E e = iterator.Next();

                    if (e == null) throw new NullReferenceException();
                    if (!LinkLast(e))
                    {
                        throw new IllegalStateException("Deque full");
                    }
                }
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public LinkedBlockingDeque(System.Collections.Generic.ICollection<E> c) : this(Int32.MaxValue)
        {
            mutex.Lock();
            try
            {
                foreach(E e in c)
                {
                    if (e == null) throw new NullReferenceException();
                    if (!LinkLast(e))
                    {
                        throw new IllegalStateException("Deque full");
                    }
                }
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual void AddFirst(E e)
        {
            if (!OfferFirst(e))
            {
                throw new IllegalStateException("Deque full");
            }
        }
    
        public virtual void AddLast(E e)
        {
            if (!OfferLast(e))
            {
                throw new IllegalStateException("Deque full");
            }
        }

        public virtual bool OfferFirst(E e)
        {
            if (e == null) throw new NullReferenceException();

            mutex.Lock();
            try
            {
                return LinkFirst(e);
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual bool OfferLast(E e)
        {
            if (e == null) throw new NullReferenceException();

            mutex.Lock();
            try
            {
                return LinkLast(e);
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual void PutFirst(E e)
        {
            if (e == null) throw new NullReferenceException();

            mutex.Lock();
            try
            {
                while (!LinkFirst(e))
                {
                    notFull.Await();
                }
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual void PutLast(E e)
        {
            if (e == null) throw new NullReferenceException();

            mutex.Lock();
            try
            {
                while (!LinkLast(e))
                {
                    notFull.Await();
                }
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual bool OfferFirst(E e, int timeout)
        {
            return OfferFirst(e, TimeSpan.FromMilliseconds(timeout));
        }

        public virtual bool OfferFirst(E e, TimeSpan timeout)
        {
            if (e == null) throw new NullReferenceException();

            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            mutex.LockInterruptibly();
            try
            {
                while (!LinkFirst(e))
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return false;
                    }

                    notFull.Await(timeout);

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

                return true;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual bool OfferLast(E e, int timeout)
        {
            return OfferLast(e, TimeSpan.FromMilliseconds(timeout));
        }

        public virtual bool OfferLast(E e, TimeSpan timeout)
        {
            if (e == null) throw new NullReferenceException();

            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            mutex.LockInterruptibly();
            try
            {
                while (!LinkLast(e))
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return false;
                    }

                    notFull.Await(timeout);

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

                return true;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E RemoveFirst()
        {
            E x = PollFirst();
            if (x == null) throw new NullReferenceException();
            return x;
        }
    
        public virtual E RemoveLast()
        {
            E x = PollLast();
            if (x == null) throw new NullReferenceException();
            return x;
        }

        public virtual E PollFirst()
        {
            mutex.Lock();
            try
            {
                return UnlinkFirst();
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E PollLast()
        {
            mutex.Lock();
            try
            {
                return UnlinkLast();
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E TakeFirst()
        {
            mutex.Lock();
            try
            {
                E x;
                while ( (x = UnlinkFirst()) == null)
                {
                    notEmpty.Await();
                }
                return x;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E TakeLast()
        {
            mutex.Lock();
            try
            {
                E x;
                while ( (x = UnlinkLast()) == null)
                {
                    notEmpty.Await();
                }
                return x;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E PollFirst(int timeout)
        {
            return PollFirst(TimeSpan.FromMilliseconds(timeout));
        }

        public virtual E PollFirst(TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            mutex.LockInterruptibly();
            try
            {
                E x;
                while ( (x = UnlinkFirst()) == null)
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return null;
                    }

                    notEmpty.Await(timeout);

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
                return x;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E PollLast(int timeout)
        {
            return PollLast(TimeSpan.FromMilliseconds(timeout));
        }

        public virtual E PollLast(TimeSpan timeout)
        {
            DateTime deadline = DateTime.Now;

            if(timeout > TimeSpan.Zero)
            {
                deadline += timeout;
            }

            mutex.LockInterruptibly();
            try
            {
                E x;
                while ( (x = UnlinkLast()) == null)
                {
                    if (timeout == TimeSpan.Zero)
                    {
                        return null;
                    }

                    notEmpty.Await(timeout);

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
                return x;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E GetFirst()
        {
            E x = PeekFirst();
            if (x == null) throw new NoSuchElementException();
            return x;
        }

        public virtual E GetLast()
        {
            E x = PeekLast();
            if (x == null) throw new NoSuchElementException();
            return x;
        }

        public virtual E PeekFirst()
        {
            mutex.Lock();
            try
            {
                return (first == null) ? null : first.item;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual E PeekLast()
        {
            mutex.Lock();
            try
            {
                return (last == null) ? null : last.item;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual bool RemoveFirstOccurrence(E o)
        {
            if (o == null) return false;

            mutex.Lock();
            try
            {
                for (Node<E> p = first; p != null; p = p.next)
                {
                    if (o.Equals(p.item))
                    {
                        Unlink(p);
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual bool RemoveLastOccurrence(E o)
        {
            if (o == null) return false;

            mutex.Lock();
            try
            {
                for (Node<E> p = last; p != null; p = p.prev)
                {
                    if (o.Equals(p.item))
                    {
                        Unlink(p);
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        public virtual void Push(E e)
        {
            AddFirst(e);
        }
    
        public virtual E Pop()
        {
            return RemoveFirst();
        }

        #region Methods from BlockingQueue

        /// <summary>
        /// Inserts the specified element at the end of this deque unless it would
        /// violate capacity restrictions.  When using a capacity-restricted deque,
        /// it is generally preferable to use method {@link #offer(Object) offer}.
        /// This method is equivalent to calling AddLAst.
        /// </summary>
        public override bool Add(E e)
        {
            AddLast(e);
            return true;
        }

        public override bool Offer(E e)
        {
            return OfferLast(e);
        }
    
        public virtual void Put(E e)
        {
            PutLast(e);
        }

        public virtual bool Offer(E e, int timeout)
        {
            return OfferLast(e, TimeSpan.FromMilliseconds(timeout));
        }

        public virtual bool Offer(E e, TimeSpan timeout)
        {
            return OfferLast(e, timeout);
        }

        /// <summary>
        /// Retrieves and removes the head of the queue represented by this deque.
        /// This method differs from {@link #poll poll} only in that it throws an
        /// exception if this deque is empty.
        /// This method is equivalent to calling RemoveFirst().
        /// </summary>
        public override E Remove()
        {
            return RemoveFirst();
        }
    
        public override E Poll()
        {
            return PollFirst();
        }

        public virtual E Take()
        {
            return TakeFirst();
        }

        public virtual E Poll(int timeout)
        {
            return PollFirst(TimeSpan.FromMilliseconds(timeout));
        }

        public virtual E Poll(TimeSpan timeout)
        {
            return PollFirst(timeout);
        }

        /// <summary>
        /// Retrieves, but does not remove, the head of the queue represented by
        /// this deque.  This method differs from {@link #peek peek} only in that
        /// it throws an exception if this deque is empty.
        /// This method is equivalent to GetFirst.
        /// </summary>
        public override E Element()
        {
            return GetFirst();
        }

        public override E Peek()
        {
            return PeekFirst();
        }
    
        /**
         * Returns the number of additional elements that this deque can ideally
         * (in the absence of memory or resource constraints) accept without
         * blocking. This is always equal to the initial capacity of this deque
         * less the current size of this deque.
         *
         * Note that you cannot always tell if an attempt to insert an element
         * will succeed by inspecting remainingCapacity because it may be the
         * case that another thread is about to insert or remove an element.
         */
        public virtual int RemainingCapacity()
        {
            mutex.Lock();
            try
            {
                return capacity - count;
            }
            finally
            {
                mutex.UnLock();
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

            mutex.Lock();
            try
            {
                int n = Math.Min(maxElements, count);
                for (int i = 0; i < n; i++)
                {
                    c.Add(first.item);   // In this order, in case Add() throws.
                    UnlinkFirst();
                }
                return n;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        #endregion

        #region Methods from Collection

        /// <summary>
        /// Returns the number of elements in this deque.
        /// </summary>
        public override int Size()
        {
            mutex.Lock();
            try
            {
                return count;
            }
            finally
            {
                mutex.UnLock();
            }
        }
    
        /// <summary>
        /// Returns {@code true} if this deque contains the specified element.
        /// More formally, returns true if and only if this deque contains
        /// at least one element e such that o.Equals(e).
        /// </summary>
        public override bool Contains(E o)
        {
            if (o == null) return false;

            mutex.Lock();
            try
            {
                for (Node<E> p = first; p != null; p = p.next)
                {
                    if (o.Equals(p.item))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                mutex.UnLock();
            }
        }

        /// <summary>
        /// Returns an array containing all of the elements in this deque, in
        /// proper sequence (from first to last element).
        /// The returned array will be "safe" in that no references to it are
        /// maintained by this deque.  (In other words, this method must allocate
        /// a new array).  The caller is thus free to modify the returned array.
        /// This method acts as bridge between array-based and collection-based APIs.
        /// </summary>
        public override E[] ToArray()
        {
            mutex.Lock();
            try
            {
                E[] a = new E[count];
                int k = 0;
                for (Node<E> p = first; p != null; p = p.next)
                {
                    a[k++] = p.item;
                }
                return a;
            }
            finally
            {
                mutex.UnLock();
            }
        }
    
        public override String ToString()
        {
            mutex.Lock();
            try
            {
                return base.ToString();
            }
            finally
            {
                mutex.UnLock();
            }
        }
    
        public override void Clear()
        {
            mutex.Lock();
            try
            {
                for (Node<E> f = first; f != null; )
                {
                    f.item = null;
                    Node<E> n = f.next;
                    f.prev = null;
                    f.next = null;
                    f = n;
                }

                first = last = null;
                count = 0;
                notFull.Signal();
            }
            finally
            {
                mutex.UnLock();
            }
        }
    
        public override Iterator<E> Iterator()
        {
            return new Itr(this);
        }
    
        public virtual Iterator<E> DescendingIterator()
        {
            return new DescendingItr(this);
        }

        #endregion

        #region Iterator Classes

        /**
         * Base class for Iterators for LinkedBlockingDeque
         */
        private abstract class AbstractItr : Iterator<E>
        {
            Node<E> next;

            /**
             * nextItem holds on to item fields because once we claim that
             * an element exists in hasNext(), we must return item read
             * under lock (in advance()) even if it was in the process of
             * being removed when hasNext() was called.
             */
            E nextItem;
    
            /**
             * Node returned by most recent call to next. Needed by remove.
             * Reset to null if this element is deleted by a call to remove.
             */
            private Node<E> lastRet;

            protected LinkedBlockingDeque<E> parent;

            protected abstract Node<E> FirstNode();
            protected abstract Node<E> NextNode(Node<E> n);

            public AbstractItr(LinkedBlockingDeque<E> parent)
            {
                this.parent = parent;

                // set to initial position
                parent.mutex.Lock();
                try
                {
                    next = FirstNode();
                    nextItem = (next == null) ? null : next.item;
                }
                finally
                {
                    parent.mutex.UnLock();
                }
            }
    
            void Advance()
            {
                parent.mutex.Lock();
                try
                {
                    // assert next != null;
                    Node<E> s = NextNode(next);
                    if (s == next)
                    {
                        next = FirstNode();
                    }
                    else
                    {
                        // Skip over removed nodes.
                        // May be necessary if multiple interior Nodes are removed.
                        while (s != null && s.item == null)
                        {
                            s = NextNode(s);
                        }
                        next = s;
                    }

                    nextItem = (next == null) ? null : next.item;
                }
                finally
                {
                    parent.mutex.UnLock();
                }
            }
    
            public bool HasNext
            {
                get { return next != null; }
            }
    
            public E Next()
            {
                if (next == null) throw new NoSuchElementException();

                lastRet = next;
                E x = nextItem;
                Advance();
                return x;
            }
    
            public void Remove()
            {
                Node<E> n = lastRet;
                if (n == null) throw new IllegalStateException();

                lastRet = null;
                parent.mutex.Lock();
                try
                {
                    if (n.item != null)
                    {
                        parent.Unlink(n);
                    }
                }
                finally
                {
                    parent.mutex.UnLock();
                }
            }
        }

        /** Forward iterator */
        private sealed class Itr : AbstractItr
        {
            public Itr(LinkedBlockingDeque<E> parent) : base(parent) {}
            protected override Node<E> FirstNode() { return parent.first; }
            protected override Node<E> NextNode(Node<E> n) { return n.next; }
        }

        /** Descending iterator */
        private sealed class DescendingItr : AbstractItr
        {
            public DescendingItr(LinkedBlockingDeque<E> parent) : base(parent) {}
            protected override Node<E> FirstNode() { return parent.last; }
            protected override Node<E> NextNode(Node<E> n) { return n.prev; }
        }

        #endregion
    }
}

