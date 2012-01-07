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
using Apache.NMS;
using Apache.NMS.Util;

namespace Apache.NMS.Pooled
{
    public class PooledConnection : IConnection
    {
        private ConnectionPool pool;
        private Atomic<bool> stopped = new Atomic<bool>(false);

        public PooledConnection(ConnectionPool pool)
        {
            this.pool = pool;
            this.pool.IncrementReferenceCount();
        }

        public IConnection Connection
        {
            get { return this.pool.Connection; }
        }

        public ISession CreateSession()
        {
            return this.pool.CreateSession(AcknowledgementMode.AutoAcknowledge);
        }

        public ISession CreateSession(AcknowledgementMode acknowledgementMode)
        {
            return this.pool.CreateSession(acknowledgementMode);
        }

        public void Close()
        {
            if (this.pool != null)
            {
                this.pool.DecrementReferenceCount();
                this.pool = null;
            }
        }

        public bool IsStarted
        {
            get { return this.pool.IsStarted; }
        }

        public void Start()
        {
            AssertNotClosed();
            this.pool.Start();
        }

        public void Stop()
        {
            this.stopped.Value = true;
        }

        public event ExceptionListener ExceptionListener
        {
            add { this.pool.Connection.ExceptionListener += value; }
            remove { this.pool.Connection.ExceptionListener -= value; }
        }

        public event ConnectionInterruptedListener ConnectionInterruptedListener
        {
            add { this.pool.Connection.ConnectionInterruptedListener += value; }
            remove { this.pool.Connection.ConnectionInterruptedListener -= value; }
        }

        public event ConnectionResumedListener ConnectionResumedListener
        {
            add { this.pool.Connection.ConnectionResumedListener += value; }
            remove { this.pool.Connection.ConnectionResumedListener -= value; }
        }

        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.pool.Connection.ConsumerTransformer; }
            set { this.pool.Connection.ConsumerTransformer = value; }
        }

        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.pool.Connection.ProducerTransformer; }
            set { this.pool.Connection.ProducerTransformer = value; }
        }

        public TimeSpan RequestTimeout
        {
            get { return this.pool.Connection.RequestTimeout; }
            set { this.pool.Connection.RequestTimeout = value; }
        }

        public AcknowledgementMode AcknowledgementMode
        {
            get { return this.pool.Connection.AcknowledgementMode; }
            set { this.pool.Connection.AcknowledgementMode = value; }
        }

        public string ClientId
        {
            get { return this.pool.Connection.ClientId; }
            set { this.pool.Connection.ClientId = value; }
        }

        public IRedeliveryPolicy RedeliveryPolicy
        {
            get { return this.pool.Connection.RedeliveryPolicy; }
            set { this.pool.Connection.RedeliveryPolicy = value; }
        }

        public IConnectionMetaData MetaData
        {
            get { return this.pool.Connection.MetaData; }
        }

        public void PurgeTempDestinations()
        {
            AssertNotClosed();
            this.Connection.PurgeTempDestinations();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        protected void AssertNotClosed()
        {
            if (this.stopped.Value || this.pool == null) {
                throw new IllegalStateException("The PooledConnection is already closed");
            }
        }

        public override string ToString ()
        {
            return "PooledConnection { " + this.pool + " }";
        }
    }
}

