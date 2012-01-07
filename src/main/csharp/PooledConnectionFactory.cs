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

namespace Apache.NMS.Pooled
{
    public class PooledConnectionFactory : IConnectionFactory, IStoppable
    {
        private IConnectionFactory factory;
        private ConnectionPool connection = null;
        private readonly Mutex mutex = new Mutex();
        private Atomic<bool> stopped = new Atomic<bool>(false);
        private bool blockIfSessionPoolIsFull = true;
        private int maxActive = 100;

        public PooledConnectionFactory()
        {
            this.factory = NMSConnectionFactory.CreateConnectionFactory(null, null);
        }

        public PooledConnectionFactory(Uri brokerUri)
        {
            this.factory = NMSConnectionFactory.CreateConnectionFactory(brokerUri, null);
        }

        public PooledConnectionFactory(IConnectionFactory factory)
        {
            this.factory = factory;
        }

        public IConnectionFactory ConnectionFactory
        {
            get { return this.factory; }
            set { this.factory = value; }
        }

        public IConnection CreateConnection()
        {
            return this.CreateConnection(null, null);
        }

        public IConnection CreateConnection(string username, string password)
        {
            if (this.connection == null)
            {
                lock(mutex)
                {
                    if (this.connection == null)
                    {
                        IConnection conn = factory.CreateConnection(username, password);
//                        this.connection = new ConnectionPool(conn, this.maxActive);
                    }
                }
            }
            else
            {
                // There might be a Connection but it could have expired or idle'd out.
                if (this.connection.ExpirationCheck())
                {
                    lock(mutex)
                    {
                        IConnection conn = factory.CreateConnection(username, password);
//                        this.connection = new ConnectionPool(conn, this.maxActive);
                    }
                }
            }

            return new PooledConnection(this.connection);
        }

        public Uri BrokerUri
        {
            get { return this.factory.BrokerUri; }
            set { this.factory.BrokerUri = value; }
        }

        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.factory.RedeliveryPolicy; }
            set { this.factory.RedeliveryPolicy = value; }
        }

        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.factory.ConsumerTransformer; }
            set { this.factory.ConsumerTransformer = value; }
        }

        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.factory.ProducerTransformer; }
            set { this.factory.ProducerTransformer = value; }
        }

        public bool BlockIfSessionPoolIsFull
        {
            get { return this.blockIfSessionPoolIsFull; }
            set { this.blockIfSessionPoolIsFull = value; }
        }

        public void Stop()
        {
            if (this.stopped.CompareAndSet(false, true))
            {
                try
                {
                    this.connection.Close();
                }
                catch (Exception e)
                {
                    Tracer.Warn("PCF: Caught exception while closing Connection: " + e.Message);
                }
                this.connection = null;
            }
        }

        public int MaxActive
        {
            get { return this.maxActive; }
            set { this.maxActive = value; }
        }
    }
}

