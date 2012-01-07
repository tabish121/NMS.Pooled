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
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Apache.NMS.Util;
using Apache.NMS.Pooled.Commons.Collections;
using Apache.NMS.Pooled.Commons.Collections.Concurrent;

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    public class GenericObjectPool<T> : BaseObjectPool<T> where T : class
    {
        private readonly PoolableObjectFactory<T> factory;

        // Public Properties
        private int maxTotal = GenericObjectPoolConfig.DEFAULT_MAX_TOTAL;
        private bool blockWhenExhausted = GenericObjectPoolConfig.DEFAULT_BLOCK_WHEN_EXHAUSTED;
        private long maxWait = GenericObjectPoolConfig.DEFAULT_MAX_WAIT;
        private int maxIdle = GenericObjectPoolConfig.DEFAULT_MAX_IDLE;
        private int minIdle = GenericObjectPoolConfig.DEFAULT_MIN_IDLE;
        private bool testOnBorrow = GenericObjectPoolConfig.DEFAULT_TEST_ON_BORROW;
        private bool testOnReturn = GenericObjectPoolConfig.DEFAULT_TEST_ON_RETURN;
        private long timeBetweenEvictionRunsMillis = GenericObjectPoolConfig.DEFAULT_TIME_BETWEEN_EVICTION_RUNS_MILLIS;
        private int numTestsPerEvictionRun = GenericObjectPoolConfig.DEFAULT_NUM_TESTS_PER_EVICTION_RUN;
        private long minEvictableIdleTimeMillis = GenericObjectPoolConfig.DEFAULT_MIN_EVICTABLE_IDLE_TIME_MILLIS;
        private long softMinEvictableIdleTimeMillis = GenericObjectPoolConfig.DEFAULT_SOFT_MIN_EVICTABLE_IDLE_TIME_MILLIS;
        private bool lifo = GenericObjectPoolConfig.DEFAULT_LIFO;
        private Iterator<PooledObject<T>> evictionIterator = null;

        // Private Implementation Properties
        private readonly IDictionary allObjects = Hashtable.Synchronized(new Hashtable());
        private readonly LinkedBlockingDeque<PooledObject<T>> idleObjects = new LinkedBlockingDeque<PooledObject<T>>();
        private int createCount = 0;
        private Timer evictionTimer = null;
        private readonly Object evictionLock = new Object();

        public GenericObjectPool(PoolableObjectFactory<T> factory) :
            this(factory, new GenericObjectPoolConfig())
        {
        }

        public GenericObjectPool(PoolableObjectFactory<T> factory, GenericObjectPoolConfig config) : base()
        {
            this.factory = factory;
            Config = config;
        }

        #region Property Accessors

        /// <summary>
        /// Gets or sets the maximum number of object that can be allocated by the pool
        /// (loaned out to clients, or in an idle state awaiting a client) at a given time.
        /// When set to a negative value the pool size is essentially unlimited.
        /// </summary>
        public int MaxTotal
        {
            get { return maxTotal; }
            set { this.maxTotal = value; }
        }

        /// <summary>
        /// Indicates whether the pool should block when a call to BorrowObject is made but
        /// the maximum number of active objects has already been loaned out to clients.
        /// </summary>
        public bool BlockWhenExhausted
        {
            get { return blockWhenExhausted; }
            set { this.blockWhenExhausted = value; }
        }

        /// <summary>
        /// Indicates the maximum amount of time that the BorrowObject method should wait
        /// before it throws an exception if the blockWhenExhausted option is enabled and.
        /// If set to a negative value then this BorrowObject method would block forever,
        /// otherwise the time indicates the wait delay in milliseconds.
        /// </summary>
        public long MaxWait
        {
            get { return maxWait; }
            set { this.maxWait = value; }
        }

        /// <summary>
        /// Indicates the maximum number of objects that are allowed to be idle in the
        /// pool at any given time.  When this limit is exceeded and objects are returned
        /// to the pool they are destroyed.  Setting this value to low on a heavily loaded
        /// system can result in many objects being quickly created and destroyed as Threads
        /// contend to borrow and return objects to the pool.
        /// </summary>
        public int MaxIdle
        {
            get { return maxIdle; }
            set { this.maxIdle = value; }
        }

        /// <summary>
        /// Indicates the minimum number of objects allowed in the pool before the
        /// evictor thread (when enabled) will spawn new objects.  The creation process
        /// is limited by the result of (numActive + numIdle) being less then maxActive.
        /// </summary>
        public int MinIdle
        {
            get { return this.minIdle; }
            set { this.minIdle = value; }
        }

        /// <summary>
        /// Indicates if the PoolableObjectFactory method ValidateObject should be
        /// called when an object is borrowed from this Pool.  If the validate step
        /// is enabled and the test fails the object is discarded and another attempt
        /// to borrow is made.
        /// </summary>
        public bool TestOnBorrow
        {
            get { return testOnBorrow; }
            set { this.testOnBorrow = value; }
        }

        /// <summary>
        /// Indicates if the PoolableObjectFactory method ValidateObject should be
        /// called when an object is returned to this Pool.
        /// </summary>
        public bool TestOnReturn
        {
            get { return testOnReturn; }
            set { this.testOnReturn = value; }
        }

        /// <summary>
        /// Indicates the number of Milliseconds to delay between each run of the Idle
        /// object eviction task.  If set to a negative value then the eviction task
        /// is never run.
        /// </summary>
        public long TimeBetweenEvictionRunsMillis
        {
            get { return timeBetweenEvictionRunsMillis; }
            set
            {
                this.timeBetweenEvictionRunsMillis = value;
                StartEvictor(value);
            }
        }

        /// <summary>
        /// Indicates the maximum number of objects to inspect during each execution of
        /// the idle object eviction task.
        /// </summary>
        public int NumTestsPerEvictionRun
        {
            get { return numTestsPerEvictionRun; }
            set { this.numTestsPerEvictionRun = value; }
        }

        /// <summary>
        /// Inidicates the minimum amount of time an object may sit idle in the pool
        /// before it is eligible for eviction by the idle object eviction task.
        /// When set to a negative value no objects will ever be elligable for
        /// eviction based on idle time.
        /// </summary>
        public long MinEvictableIdleTimeMillis
        {
            get { return minEvictableIdleTimeMillis; }
            set { this.minEvictableIdleTimeMillis = value; }
        }

        /// <summary>
        /// Indicates the minimum amount of time an object may sit idle in the pool
        /// before it is eligible for eviction by the idle object evictor (if any),
        /// with the extra condition that at least "minIdle" object instances remain
        /// in the pool. When non-positive, no objects will be evicted from the pool
        /// due to idle time alone.  This value is superseded by the value of
        /// MinEvictableIdleTimeMillis if it is set to a non-negative value.
        /// </summary>
        public long SoftMinEvictableIdleTimeMillis
        {
            get { return softMinEvictableIdleTimeMillis; }
            set { this.softMinEvictableIdleTimeMillis = value; }
        }

        /// <summary>
        /// Indicates whether the idle object eviction task should validate idle objects.
        /// If enabled and an object fails to validate it is evicted from the pool and
        /// the PoolableObjectFactory is used to destroy the object.
        /// </summary>
        private bool testWhileIdle = GenericObjectPoolConfig.DEFAULT_TEST_WHILE_IDLE;
        public bool TestWhileIdle
        {
            get { return testWhileIdle; }
            set { this.testWhileIdle = value; }
        }

        /// <summary>
        /// Indicates the Lifo or Fifo behavior of this Pool.  True means that BorrowObject
        /// returns the most recently used ("last in") idle object in the pool (if there are
        /// idle instances available). False means that the pool behaves as a FIFO queue,
        /// objects are taken from the idle object pool in the order that they are returned
        /// to the pool.
        /// </summary>
        public bool Lifo
        {
            get { return lifo; }
            set { this.lifo = value; }
        }

        /// <summary>
        /// Sets the config for this object using a GenericObjectConfig instance.
        /// </summary>
        public GenericObjectPoolConfig Config
        {
            set
            {
                MaxIdle = value.MaxIdle;
                MinIdle = value.MinIdle;
                MaxTotal = value.MaxTotal;
                MaxWait = value.MaxWait;
                BlockWhenExhausted = value.BlockWhenExhausted;
                TestOnBorrow = value.TestOnBorrow;
                TestOnReturn = value.TestOnReturn;
                TestWhileIdle = value.TestWhileIdle;
                NumTestsPerEvictionRun = value.NumTestsPerEvictionRun;
                MinEvictableIdleTimeMillis = value.MinEvictableIdleTimeMillis;
                TimeBetweenEvictionRunsMillis = value.TimeBetweenEvictionRunsMillis;
                SoftMinEvictableIdleTimeMillis = value.SoftMinEvictableIdleTimeMillis;
                Lifo = value.Lifo;
            }
        }

        public PoolableObjectFactory<T> Factory
        {
            get { return this.factory; }
        }

        #endregion

        public override T BorrowObject()
        {
            return this.BorrowObject(this.maxWait);
        }

        public T BorrowObject(TimeSpan maxWaitTime)
        {
            return this.BorrowObject((long)maxWaitTime.TotalMilliseconds);
        }

        public T BorrowObject(long maxWaitMillisecs)
        {
            CheckClosed();

            PooledObject<T> p = null;
            bool blockWhenExhausted = this.blockWhenExhausted;
            bool create;

            while (p == null)
            {
                create = false;
                if (blockWhenExhausted)
                {
                    p = idleObjects.PollFirst();
                    if (p == null)
                    {
                        create = true;
                        p = Create();
                    }

                    if (p == null)
                    {
                        if (maxWaitMillisecs < 0)
                        {
                            p = idleObjects.TakeFirst();
                        }
                        else
                        {
                            p = idleObjects.PollFirst(TimeSpan.FromMilliseconds(maxWaitMillisecs));
                        }
                    }

                    if (p == null)
                    {
                        throw new NoSuchElementException("Timeout waiting for idle object");
                    }

                    if (!p.Allocate())
                    {
                        p = null;
                    }
                }
                else
                {
                    p = idleObjects.PollFirst();
                    if (p == null)
                    {
                        create = true;
                        p = Create();
                    }

                    if (p == null)
                    {
                        throw new NoSuchElementException("Pool exhausted");
                    }

                    if (!p.Allocate())
                    {
                        p = null;
                    }
                }
    
                if (p != null)
                {
                    try
                    {
                        factory.ActivateObject(p.TheObject);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            Destroy(p);
                        }
                        catch
                        {
                            // Ignore - activation failure is more important
                        }
                        p = null;
                        if (create)
                        {
                            NoSuchElementException nsee = new NoSuchElementException(
                                    "Unable to activate object", e);
                            throw nsee;
                        }
                    }

                    if (p != null && TestOnBorrow)
                    {
                        bool validate = false;
                        Exception validationThrowable = null;
                        try
                        {
                            validate = factory.ValidateObject(p.TheObject);
                        }
                        catch (Exception ex)
                        {
                            PoolUtils.CheckRethrow(ex);
                        }
                        if (!validate)
                        {
                            try
                            {
                                Destroy(p);
                            }
                            catch
                            {
                                // Ignore - validation failure is more important
                            }
                            p = null;
                            if (create)
                            {
                                NoSuchElementException nsee = new NoSuchElementException(
                                        "Unable to validate object", validationThrowable);
                                throw nsee;
                            }
                        }
                    }
                }
            }

            return p.TheObject;
        }

        public override void ReturnObject(T obj)
        {
            PooledObject<T> p = allObjects[obj] as PooledObject<T>;

            if (p == null)
            {
                throw new IllegalStateException("Returned object not currently part of this pool");
            }

            if (TestOnReturn)
            {
                if (!factory.ValidateObject(obj))
                {
                    try
                    {
                        Destroy(p);
                    }
                    catch
                    {
                    }
                    return;
                }
            }
    
            try
            {
                factory.SuspendObject(obj);
            }
            catch
            {
                try
                {
                    Destroy(p);
                }
                catch
                {
                }

                return;
            }
    
            if (!p.Deallocate())
            {
                throw new IllegalStateException("Object has already been retured to this pool");
            }
    
            int maxIdle = MaxIdle;
            if (IsClosed || maxIdle > -1 && maxIdle <= idleObjects.Size())
            {
                try
                {
                    Destroy(p);
                }
                catch
                {
                }
            }
            else
            {
                if (Lifo)
                {
                    idleObjects.AddFirst(p);
                }
                else
                {
                    idleObjects.AddLast(p);
                }
            }
        }

        public override void InvalidateObject(T borrowed)
        {
            PooledObject<T> p = allObjects[borrowed] as PooledObject<T>;

            if (p == null)
            {
                throw new IllegalStateException("Object not currently part of this pool");
            }

            Destroy(p);
        }

        public override void AddObject()
        {
            CheckClosed();
            if (factory == null)
            {
                throw new IllegalStateException("Cannot add objects without a factory.");
            }
            PooledObject<T> p = Create();
            AddIdleObject(p);
        }

        public override void Clear()
        {
            PooledObject<T> p = idleObjects.Poll();

            while (p != null)
            {
                try
                {
                    Destroy(p);
                }
                catch
                {
                }

                p = idleObjects.Poll();
            }
        }

        public override int IdleCount
        {
            get { return this.idleObjects.Size(); }
        }

        public override int ActiveCount
        {
            get { return this.allObjects.Count - this.idleObjects.Size(); }
        }

        public override void Close()
        {
            if (!closed.CompareAndSet(false, true))
            {
                StartEvictor(-1L);
                base.Close();
                Clear();
            }
        }

        #region Pivate Implementation Methods

        public void Evict()
        {
            CheckClosed();

            if (idleObjects.Size() == 0)
            {
                return;
            }
    
            PooledObject<T> underTest = null;

            lock(evictionLock)
            {
                bool testWhileIdle = TestWhileIdle;
                long idleEvictTime = Int64.MaxValue;
                long idleSoftEvictTime = Int64.MaxValue;
    
                if (MinEvictableIdleTimeMillis > 0)
                {
                    idleEvictTime = MinEvictableIdleTimeMillis;
                }
                if (SoftMinEvictableIdleTimeMillis > 0)
                {
                    idleSoftEvictTime = SoftMinEvictableIdleTimeMillis;
                }
    
                for (int i = 0, m = NumTests; i < m; i++)
                {
                    if (evictionIterator == null || !evictionIterator.HasNext)
                    {
                        if (Lifo)
                        {
                            evictionIterator = idleObjects.DescendingIterator();
                        }
                        else
                        {
                            evictionIterator = idleObjects.Iterator();
                        }
                    }
                    if (!evictionIterator.HasNext)
                    {
                        // Pool exhausted, nothing to do here
                        return;
                    }
    
                    try
                    {
                        underTest = evictionIterator.Next();
                    }
                    catch (NoSuchElementException)
                    {
                        // Object was borrowed in another thread
                        // Don't count this as an eviction test so reduce i;
                        i--;
                        evictionIterator = null;
                        continue;
                    }

                    if (!underTest.StartEvictionTest())
                    {
                        // Object was borrowed in another thread
                        // Don't count this as an eviction test so reduce i;
                        i--;
                        continue;
                    }
    
                    if (idleEvictTime < underTest.IdleTime.TotalMilliseconds ||
                        (idleSoftEvictTime < underTest.IdleTime.TotalMilliseconds && MinIdle < idleObjects.Size()))
                    {
                        Destroy(underTest);
                    }
                    else
                    {
                        if (testWhileIdle)
                        {
                            bool active = false;
                            try
                            {
                                factory.ActivateObject(underTest.TheObject);
                                active = true;
                            }
                            catch
                            {
                                Destroy(underTest);
                            }

                            if (active)
                            {
                                if (!factory.ValidateObject(underTest.TheObject))
                                {
                                    Destroy(underTest);
                                }
                                else
                                {
                                    try
                                    {
                                        factory.SuspendObject(underTest.TheObject);
                                    }
                                    catch
                                    {
                                        Destroy(underTest);
                                    }
                                }
                            }
                        }
                        underTest.EndEvictionTest(idleObjects);
                    }
                }
            }
        }

        private PooledObject<T> Create()
        {
            int localMaxTotal = MaxTotal;
            long newCreateCount = Interlocked.Increment(ref createCount);

            if (localMaxTotal > -1 && newCreateCount > localMaxTotal || newCreateCount > Int32.MaxValue)
            {
                Interlocked.Decrement(ref createCount);
                return null;
            }
    
            T t = null;
            try
            {
                t = factory.CreateObject();
            }
            catch
            {
                Interlocked.Decrement(ref createCount);
                throw;
            }
    
            PooledObject<T> p = new PooledObject<T>(t);
            allObjects[t] = p;

            return p;
        }

        private void Destroy(PooledObject<T> target)
        {
            target.Invalidate();

            idleObjects.Remove(target);
            allObjects.Remove(target.TheObject);

            try
            {
                factory.DestroyObject(target.TheObject);
            }
            finally
            {
                Interlocked.Decrement(ref createCount);
            }
        }

        private void AddIdleObject(PooledObject<T> obj)
        {
            if (obj != null)
            {
                factory.SuspendObject(obj.TheObject);
                if (Lifo)
                {
                    idleObjects.AddFirst(obj);
                }
                else
                {
                    idleObjects.AddLast(obj);
                }
            }
        }

        /// <summary>
        /// Performs a check to determine if the pool has dropped below the configured
        /// number of idle objects, if so it creates enough to return the pool to the
        /// minimum amount of idle objects.
        /// </summary>
        private void EnsureMinIdle()
        {
            int minIdle = MinIdle;
            if (minIdle < 1)
            {
                return;
            }
    
            while (idleObjects.Size() < minIdle)
            {
                PooledObject<T> p = Create();
                if (p == null)
                {
                    break;
                }

                if (Lifo)
                {
                    idleObjects.AddFirst(p);
                }
                else
                {
                    idleObjects.AddLast(p);
                }
            }
        }

        protected void StartEvictor(long delay)
        {
            lock(evictionLock)
            {
                if (null != evictionTimer)
                {
                    evictionTimer.Dispose();
                    evictionTimer = null;
                }

                if (delay > 0)
                {
                    evictionTimer = new Timer(new TimerCallback(EvictionCycle), null, delay, delay);
                }
            }
        }

        /**
         * Returns pool info including {@link #getNumActive()},
         * {@link #getNumIdle()} and a list of objects idle in the pool with their
         * idle times.
         *
         * @return string containing debug information
         */
        internal String DebugInfo()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("Active: ").Append(ActiveCount).Append("\n");
            buf.Append("Idle: ").Append(IdleCount).Append("\n");
            buf.Append("Idle Objects:\n");
            Iterator<PooledObject<T>> iterator = idleObjects.Iterator();

            while (iterator.HasNext)
            {
                buf.Append("\t").Append(iterator.Next().ToString());
            }
            return buf.ToString();
        }

        private int NumTests
        {
            get
            {
                if (numTestsPerEvictionRun >= 0)
                {
                    return Math.Min(numTestsPerEvictionRun, idleObjects.Size());
                }
                else
                {
                    return (int) (Math.Ceiling(idleObjects.Size() /
                                    Math.Abs((double) numTestsPerEvictionRun)));
                }
            }
        }

        #endregion

        #region Timer Callback Methods

        private void EvictionCycle(object state)
        {
            try
            {
                Evict();
            }
            catch
            {
            }

            try
            {
                EnsureMinIdle();
            }
            catch
            {
            }
        }

        #endregion
    }
}

