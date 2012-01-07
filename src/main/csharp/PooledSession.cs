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
using System.Collections;
using System.Collections.Generic;
using Apache.NMS;

namespace Apache.NMS.Pooled
{
    public class PooledSession : ISession
    {
        private ISession session;
        private SessionPool sessionPool;
        private bool ignoreClose = false;
        private IMessageProducer messageProducer = null;
        private bool transactional = false;
        private bool netTxSession = false;

        private readonly LinkedList<IMessageConsumer> consumers = new LinkedList<IMessageConsumer>();
        private readonly LinkedList<IQueueBrowser> browsers = new LinkedList<IQueueBrowser>();

        public PooledSession(ISession session, SessionPool sessionPool)
        {
            this.session = session;
            this.sessionPool = sessionPool;
            this.transactional = session.Transacted;
            this.netTxSession = session is INetTxSession;
        }

        public ISession InternalSession
        {
            get
            {
                if (session == null)
                {
                    throw new IllegalStateException("The PooledSession is closed");
                }
                
                return this.session;
            }
        }

        public IMessageProducer CreateProducer()
        {
            return new PooledProducer(MessageProducer, null);
        }

        public IMessageProducer CreateProducer(IDestination destination)
        {
            return new PooledProducer(MessageProducer, destination);
        }

        public IMessageConsumer CreateConsumer(IDestination destination)
        {
            return AddMessageConsumer(this.session.CreateConsumer(destination));
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector)
        {
            return AddMessageConsumer(this.session.CreateConsumer(destination, selector));
        }

        public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
        {
            return AddMessageConsumer(this.session.CreateConsumer(destination, selector, noLocal));
        }

        public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
        {
            return AddMessageConsumer(this.session.CreateDurableConsumer(destination, name, selector, noLocal));
        }

        public void DeleteDurableConsumer(string name)
        {
            this.session.DeleteDurableConsumer(name);
        }

        public IQueueBrowser CreateBrowser(IQueue queue)
        {
            return AddQueueBrowser(this.session.CreateBrowser(queue));
        }

        public IQueueBrowser CreateBrowser(IQueue queue, string selector)
        {
            return AddQueueBrowser(this.session.CreateBrowser(queue, selector));
        }

        public IQueue GetQueue(string name)
        {
            return this.session.GetQueue(name);
        }

        public ITopic GetTopic(string name)
        {
            return this.session.GetTopic(name);
        }

        public ITemporaryQueue CreateTemporaryQueue()
        {
            return this.session.CreateTemporaryQueue();
        }

        public ITemporaryTopic CreateTemporaryTopic()
        {
            return this.session.CreateTemporaryTopic();
        }

        public void DeleteDestination(IDestination destination)
        {
            this.session.DeleteDestination(destination);
        }

        public IMessage CreateMessage()
        {
            return this.session.CreateMessage();
        }

        public ITextMessage CreateTextMessage()
        {
            return this.session.CreateTextMessage();
        }

        public ITextMessage CreateTextMessage(string text)
        {
            return this.session.CreateTextMessage(text);
        }

        public IMapMessage CreateMapMessage()
        {
            return this.session.CreateMapMessage();
        }

        public IObjectMessage CreateObjectMessage(object body)
        {
            return this.session.CreateObjectMessage(body);
        }

        public IBytesMessage CreateBytesMessage()
        {
            return this.session.CreateBytesMessage();
        }

        public IBytesMessage CreateBytesMessage(byte[] body)
        {
            return this.session.CreateBytesMessage(body);
        }

        public IStreamMessage CreateStreamMessage()
        {
            return this.session.CreateStreamMessage();
        }

        public void Close()
        {
            if (netTxSession)
            {
//                // We check if the NetTxSession is actually participating in a DTC
//                // Transaction and if so we must do the close async otherwise we would
//                // block on the Close call.
//                NetTxSession txSession = session as NetTxSession;
//                if (txSession.TransactionContext.InNetTransaction)
//                {
//                    Tracer.Debug("PS: Closed while in a Transaction, waiting for completion: " + session);
//                    txSession.TransactionContext.AddSynchronization(new NetTxCloseSynchronization(this));
//                }
//                else
//                {
                    DoClose();
//                }
            }
            else
            {
                DoClose();
            }
        }

        public ConsumerTransformerDelegate ConsumerTransformer
        {
            get { return this.session.ConsumerTransformer; }
            set { this.session.ConsumerTransformer = value; }
        }

        public ProducerTransformerDelegate ProducerTransformer
        {
            get { return this.session.ProducerTransformer; }
            set { this.session.ProducerTransformer = value; }
        }

        public event SessionTxEventDelegate TransactionStartedListener
        {
            add { this.session.TransactionStartedListener += value; }
            remove { this.session.TransactionStartedListener += value; }
        }

        public event SessionTxEventDelegate TransactionCommittedListener
        {
            add { this.session.TransactionCommittedListener += value; }
            remove { this.session.TransactionCommittedListener += value; }
        }

        public event SessionTxEventDelegate TransactionRolledBackListener
        {
            add { this.session.TransactionRolledBackListener += value; }
            remove { this.session.TransactionRolledBackListener += value; }
        }

        public void Recover()
        {
            this.session.Recover();
        }

        public void Commit()
        {
            this.session.Commit();
        }

        public void Rollback()
        {
            this.session.Rollback();
        }

        public TimeSpan RequestTimeout
        {
            get { return this.session.RequestTimeout; }
            set { this.session.RequestTimeout = value; }
        }

        public bool Transacted
        {
            get { return this.session.Transacted; }
        }

        public AcknowledgementMode AcknowledgementMode
        {
            get { return this.session.AcknowledgementMode; }
        }

        public bool IgnoreClose
        {
            get { return this.ignoreClose; }
            set { this.ignoreClose = value; }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public override String ToString()
        {
            return "PooledSession { " + this.session + " }";
        }

        private IMessageProducer MessageProducer
        {
            get
            {
                if (this.messageProducer == null)
                {
                    this.messageProducer = InternalSession.CreateProducer();
                }

                return this.messageProducer;
            }
        }

        private IMessageConsumer AddMessageConsumer(IMessageConsumer consumer)
        {
            lock(consumers)
            {
                consumers.AddLast(consumer);
            }

            return new PooledMessageConsumer(this, consumer);
        }

        internal void OnMessageConsumerClosed(IMessageConsumer consumer)
        {
            lock(consumers)
            {
                consumers.Remove(consumer);
            }
        }

        private IQueueBrowser AddQueueBrowser(IQueueBrowser browser)
        {
            lock(browsers)
            {
                browsers.AddLast(browser);
            }

            return new PooledQueueBrowser(this, browser);
        }

        internal void OnQueueBrowserClosed(IQueueBrowser browser)
        {
            lock(browsers)
            {
                browsers.Remove(browser);
            }
        }

        private void DoClose()
        {
            bool invalidate = false;
            try
            {
                // Close any consumers and browsers that may have been created.
                foreach (IMessageConsumer consumer in this.consumers)
                {
                    consumer.Close();
                }
                foreach (IQueueBrowser browser in this.browsers)
                {
                    browser.Close();
                }

                // For a Session in a LocalTransaction we Rollback, messages will be redelivered.
                if (transactional)
                {
                    try
                    {
                        Tracer.Debug("Rolling Back closed Transactional Session.");
                        InternalSession.Rollback();
                    }
                    catch (NMSException e)
                    {
                        invalidate = true;
                        Tracer.Warn("PS: Caught exception trying Rollback() when putting session back into the pool, will invalidate. " + e.Message);
                    }
                }
            }
            catch (NMSException ex)
            {
                invalidate = true;
                Tracer.Warn("PS: Caught exception trying close() when putting session back into the pool, will invalidate. " + ex.Message);
            }
            finally
            {
                consumers.Clear();
                browsers.Clear();
            }

            // An error occured so we don't know the session state which means we can't put
            // it back into the pool, instead we ensure it gets closed and invalidate it so
            // a new Session instance will get added to the pool when a new one needed.
            if (invalidate)
            {
                if (session != null)
                {
                    try
                    {
                        Tracer.Debug("Closing invalidated session object");
                        session.Close();
                    }
                    catch (NMSException e)
                    {
                        Tracer.Debug("PS: Ignoring exception on close as discarding session: " + e.Message);
                    }

                    session = null;
                }

                sessionPool.InvalidateSession(this);
            }
            else
            {
                sessionPool.ReturnSession(this);
            }
        }

//        class NetTxCloseSynchronization : ISynchronization
//        {
//            private PooledSession session;
//
//            public NetTxCloseSynchronization(PooledSession session)
//            {
//                this.session = session;
//            }
//
//            public void BeforeEnd()
//            {
//            }
//
//            public void AfterCommit()
//            {
//                Tracer.DebugFormat("{0} tx was commited, Closing.", session);
//                session.DoClose();
//            }
//
//            public void AfterRollback()
//            {
//                Tracer.DebugFormat("{0} tx was rolled back, Closing.", session);
//                session.DoClose();
//            }
//        }
    }
}

