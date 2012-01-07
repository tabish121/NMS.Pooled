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
    public class PooledProducer : IMessageProducer
    {
        private readonly IMessageProducer producer;
        private readonly IDestination destination;

        private MsgDeliveryMode deliveryMode;
        private bool disableMessageID;
        private bool disableMessageTimestamp;
        private MsgPriority priority;
        private TimeSpan timeToLive;

        public PooledProducer(IMessageProducer producer, IDestination destination)
        {
            this.producer = producer;
            this.destination = destination;

            this.deliveryMode = producer.DeliveryMode;
            this.disableMessageID = producer.DisableMessageID;
            this.disableMessageTimestamp = producer.DisableMessageTimestamp;
            this.priority = producer.Priority;
            this.timeToLive = producer.TimeToLive;
        }

        public void Send(IMessage message)
        {
            Send(destination, message, DeliveryMode, Priority, TimeToLive);
        }

        public void Send(IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            Send(destination, message, deliveryMode, priority, timeToLive);
        }

        public void Send(IDestination destination, IMessage message)
        {
            Send(destination, message, DeliveryMode, Priority, TimeToLive);
        }

        public void Send(IDestination destination, IMessage message, MsgDeliveryMode deliveryMode, MsgPriority priority, TimeSpan timeToLive)
        {
            if (destination == null)
            {
                destination = this.destination;
            }

            IMessageProducer producer = this.MessageProducer;

            lock(producer)
            {
                producer.Send(destination, message, deliveryMode, priority, timeToLive);
            }
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return MessageProducer.ProducerTransformer; }
            set { MessageProducer.ProducerTransformer = value; }
        }

        public MsgDeliveryMode DeliveryMode
        {
            get { return this.deliveryMode; }
            set { this.deliveryMode = value; }
        }

        public TimeSpan TimeToLive
        {
            get { return this.timeToLive; }
            set { this.timeToLive = value; }
        }

        public TimeSpan RequestTimeout
        {
            get { return MessageProducer.RequestTimeout; }
            set { MessageProducer.RequestTimeout = value; }
        }

        public MsgPriority Priority
        {
            get { return this.priority; }
            set { this.priority = value; }
        }

        public bool DisableMessageID
        {
            get { return this.disableMessageID; }
            set { this.disableMessageID = value; }
        }

        public bool DisableMessageTimestamp
        {
            get { return this.disableMessageTimestamp; }
            set { this.disableMessageTimestamp = value; }
        }

        public IMessage CreateMessage()
        {
            return MessageProducer.CreateMessage();
        }

        public ITextMessage CreateTextMessage()
        {
            return MessageProducer.CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            return MessageProducer.CreateTextMessage(text);
        }

        public IMapMessage CreateMapMessage()
        {
            return MessageProducer.CreateMapMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            return MessageProducer.CreateObjectMessage(body);
        }

        public IBytesMessage CreateBytesMessage()
        {
            return MessageProducer.CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            return MessageProducer.CreateBytesMessage(body);
        }

        public IStreamMessage CreateStreamMessage()
        {
            return MessageProducer.CreateStreamMessage();
        }

        internal IMessageProducer MessageProducer
        {
            get { return this.producer; }
        }

        public override String ToString()
        {
            return "PooledProducer { " + MessageProducer + " }";
        }
    }
}

