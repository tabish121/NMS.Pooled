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
using Apache.NMS.Pooled.Commons.Pool;
using Apache.NMS.Pooled.Commons.Collections;
using Apache.NMS.Pooled.Util;

namespace Apache.NMS.Pooled
{
    public class SessionPool
    {
        private readonly ConnectionPool connectionPool;
        private readonly SessionKey sessionKey;
        private readonly ObjectPool<PooledSession> sessionPool;
        private readonly Atomic<bool> closed = new Atomic<bool>(false);

        public SessionPool(ConnectionPool pool, SessionKey key, ObjectPool<PooledSession> sessionPool)
        {
            this.connectionPool = pool;
            this.sessionKey = key;
            this.sessionPool = sessionPool;
        }

        public void Close()
        {
            if (closed.CompareAndSet(false, true))
            {
                sessionPool.Clear();
            }
        }

        public PooledSession BorrowSession()
        {
            try
            {
                return this.sessionPool.BorrowObject();
            }
            catch (NMSException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new NMSException("Error while borrowing a Session", e);
            }
        }

        public void ReturnSession(PooledSession session)
        {
            try
            {
                connectionPool.OnSessionReturned(session);
            }
            catch (NMSException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new NMSException("Error while returning a Session", e);
            }
            finally
            {
                if (!connectionPool.IsClosed)
                {
                    sessionPool.ReturnObject(session);
                }
            }
        }

        public void InvalidateSession(PooledSession session)
        {
            try
            {
                connectionPool.OnSessionInvalidated(session);

                // Ensure the bad session is closed.
                session.InternalSession.Close();
            }
            catch (NMSException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new NMSException("Error while invalidating a Session", e);
            }
        }

        protected IConnection Connection
        {
            get { return connectionPool.Connection; }
        }
    }
}

