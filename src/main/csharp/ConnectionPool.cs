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
using Apache.NMS;
using Apache.NMS.Util;
using System.Collections;
using System.Collections.Specialized;
using Apache.NMS.Pooled.Commons.Pool;
using Apache.NMS.Pooled.Util;

namespace Apache.NMS.Pooled
{
    public class ConnectionPool
    {
        private IConnection connection;
        private readonly Atomic<bool> started = new Atomic<bool>(false);
        private readonly Atomic<bool> closed = new Atomic<bool>(false);
        private readonly Mutex mutex = new Mutex();
        private int references = 0;
        private bool failed = false;
        private bool expired = false;
        private readonly KeyedObjectPool<SessionKey, SessionPool> sessions;
        private readonly ArrayList loanedSessions = ArrayList.Synchronized(new ArrayList());
        private DateTime lastUsed = DateTime.Now;
        private DateTime firstUsed = DateTime.Now;
        private TimeSpan idleTimeout = TimeSpan.FromSeconds(30);
        private TimeSpan expiryTimeout = TimeSpan.Zero;

        public ConnectionPool(IConnection connection)
        {
            this.connection = connection;

            // When not using the Failover transport we need to know when the conneection
            // encounters an error so that it can get shutdown and not used again.
            this.connection.ExceptionListener += OnException;
        }

        public IConnection Connection
        {
            get { return this.connection; }
        }

        /// <summary>
        /// Gets or sets the idle timeout which controls how long a Connection can sit
        /// unreferenced before its considered no longer usable.  Once a Connection has
        /// exceeded this value it and all its cache resources are closed.  A value of
        /// TimeSpan.Zero disables this check.
        /// </summary>
        public TimeSpan IdleTimeout
        {
            get { return this.idleTimeout; }
            set { this.idleTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the expiry timeout which sets a limit on how long a Connection
        /// should be allowed to live before it is considered expired and closed.  For long
        /// lived connections setting an expiration can reduce overhead by occasionlly
        /// closing down and freeing all the resources that may no longer be in use but are
        /// still cached.
        /// </summary>
        public TimeSpan ExpiryTimeout
        {
            get { return this.expiryTimeout; }
            set { this.expiryTimeout = value; }
        }

        public void IncrementReferenceCount()
        {
            Interlocked.Increment(ref references);
            lastUsed = DateTime.Now;
        }

        public void DecrementReferenceCount()
        {
            Interlocked.Decrement(ref references);
            lastUsed = DateTime.Now;

            if (references == 0)
            {
                Tracer.Info("ConnectionPool has no references: " + this);
                ExpirationCheck();

                // We close out all our loaned sessions here because we assume that
                // when there are no more references to the Connection that there
                // should be no users of the Sessions either.
                foreach (PooledSession session in this.loanedSessions)
                {
                    try
                    {
                        session.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
                this.loanedSessions.Clear();

                // We should add the ability clear a connection's temp
                // destinations into NMS API.
            }
        }

        public bool IsStarted
        {
            get { return this.started.Value; }
        }

        public bool IsClosed
        {
            get { return this.closed.Value; }
        }

        public void Close()
        {
            if (closed.CompareAndSet(false, true))
            {
                try
                {
//                    this.sessionPool.Close();
                }
                catch (Exception)
                {
                }
                finally
                {
                    try
                    {
                        this.connection.Close();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        connection = null;
                    }
                }
            }
        }

        public void Start()
        {
            if (this.started.CompareAndSet(false, true))
            {
                try
                {
                    Tracer.Info("ConnectionPool is starting Connection: " + this.connection);
                    this.connection.Start();
                }
                catch (Exception e)
                {
                    Tracer.Warn("ConnectionPool error starting Connection: " + e.Message);
                    this.started.Value = false;
                    throw;
                }
            }
        }

        public ISession CreateSession(AcknowledgementMode mode)
        {
            // Later on a cache of SessionPool's based on the Ack mode could
            // be created to allow for use of all the different Ack types.

//            PooledSession session = sessionPool.BorrowSession();
//            this.loanedSessions.Add(session);
//            return session;
            return null;
        }

        internal bool ExpirationCheck()
        {
            lock(mutex)
            {
                if (connection == null)
                {
                    return true;
                }

                if (this.expired)
                {
                    if (this.references == 0)
                    {
                        Close();
                    }
                    return true;
                }

                if (this.failed ||
                    (!idleTimeout.Equals(TimeSpan.Zero) && DateTime.Now > lastUsed + idleTimeout) ||
                     !expiryTimeout.Equals(TimeSpan.Zero) && DateTime.Now > firstUsed + expiryTimeout)
                {
                    this.expired = true;
                    if (this.references == 0)
                    {
                        Close();
                    }
                    return true;
                }
                return false;
            }
        }

        public override string ToString()
        {
            return string.Format("[ConnectionPool: Connection={0}, IsStarted={1}]", Connection, IsStarted);
        }

        internal void OnSessionReturned(PooledSession session)
        {
            this.loanedSessions.Remove(session);
        }

        internal void OnSessionInvalidated(PooledSession session)
        {
            this.loanedSessions.Remove(session);
        }

        protected void OnException(Exception e)
        {
            this.failed = true;
        }
    }
}

