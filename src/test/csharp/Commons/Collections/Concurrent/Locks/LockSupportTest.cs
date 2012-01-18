/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
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
using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks
{
    [TestFixture]
    public class LockSupportTest : ConcurrencyTestCase
    {
        private readonly Mutex mutex = new Mutex();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private void TestParkRunnable()
        {
            try
            {
                LockSupport.Park();
            }
            catch (Exception)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestPark()
        {
            Thread t = new Thread(TestParkRunnable);
            try
            {
                t.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                LockSupport.UnPark(t);
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestParkRunnable2()
        {
            try
            {
                Thread.Sleep(SHORT_DELAY_MS);
                LockSupport.Park();
            }
            catch (Exception)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestPark2()
        {
            Thread t = new Thread(TestParkRunnable2);
            try
            {
                t.Start();
                LockSupport.UnPark(t);
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        [Test]
        public void TestPark3()
        {
            Thread t = new Thread(TestParkRunnable);

            try
            {
                t.Start();
                Thread.Sleep(SHORT_DELAY_MS);
                t.Interrupt();
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestParkRunnable4()
        {
            try
            {
                mutex.WaitOne();
                LockSupport.Park();
            }
            catch (Exception)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestPark4()
        {
            mutex.WaitOne();

            Thread t = new Thread(TestParkRunnable4);

            try
            {
                t.Start();
                t.Interrupt();
                mutex.ReleaseMutex();
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestParkNanosRunnable()
        {
            try
            {
                LockSupport.ParkNanos(1000);
            }
            catch (Exception)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestParkNanos()
        {
            Thread t = new Thread(TestParkNanosRunnable);

            try
            {
                t.Start();
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }

        private void TestParkUntilRunnable()
        {
            try
            {
                LockSupport.ParkUntil(DateTime.Now.AddMilliseconds(100));
            }
            catch (Exception)
            {
                ThreadUnexpectedException();
            }
        }

        [Test]
        public void TestParkUntil()
        {
            Thread t = new Thread(TestParkUntilRunnable);

            try
            {
                t.Start();
                t.Join();
            }
            catch (Exception e)
            {
                UnexpectedException(e);
            }
        }
    }
}

