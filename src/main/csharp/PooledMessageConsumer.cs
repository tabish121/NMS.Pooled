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

namespace Apache.NMS.Pooled
{
    public class PooledMessageConsumer : IMessageConsumer
    {
        private readonly PooledSession session;
        private readonly IMessageConsumer consumer;

        public PooledMessageConsumer(PooledSession session, IMessageConsumer consumer)
        {
            this.consumer = consumer;
            this.session = session;
        }

        public IMessageConsumer Consumer
        {
            get { return this.consumer; }
        }

        public IMessage Receive()
        {
            return this.consumer.Receive();
        }

        public IMessage Receive(TimeSpan timeout)
        {
            return this.consumer.Receive(timeout);
        }

        public IMessage ReceiveNoWait()
        {
            return this.consumer.ReceiveNoWait();
        }

        public void Close()
        {
            this.session.OnMessageConsumerClosed(this.consumer);
            this.consumer.Close();
        }

        public void Dispose()
        {
            this.session.OnMessageConsumerClosed(this.consumer);
            this.consumer.Dispose();
        }

        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.consumer.ConsumerTransformer; }
            set { this.consumer.ConsumerTransformer = value; }
        }

        public event MessageListener Listener
        {
            add { this.consumer.Listener += value; }
            remove { this.consumer.Listener -= value; }
        }

        public override string ToString()
        {
            return "PooledConsumer { " + Consumer + " }";
        }
    }
}

