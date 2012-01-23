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
using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent
{
    [TestFixture]
    public class ConcurrencyTestCase
    {
        /**
         * The number of elements to place in collections, arrays, etc.
         */
        protected static readonly int SIZE = 20;

        protected static readonly String zero = (0).ToString();
        protected static readonly String one = (1).ToString();
        protected static readonly String two = (2).ToString();
        protected static readonly String three  = (3).ToString();
        protected static readonly String four  = (4).ToString();
        protected static readonly String five  = (5).ToString();
        protected static readonly String six = (6).ToString();
        protected static readonly String seven = (7).ToString();
        protected static readonly String eight = (8).ToString();
        protected static readonly String nine = (9).ToString();

        protected static readonly Object m1 = -1;
        protected static readonly Object m2 = -2;
        protected static readonly Object m3 = -3;
        protected static readonly Object m4 = -4;
        protected static readonly Object m5 = -5;
        protected static readonly Object m6 = -6;
        protected static readonly Object m10 = -10;

        internal bool threadFailed = false;
        internal String threadFailedMessage = "";

        public static int SHORT_DELAY_MS;
        public static int SMALL_DELAY_MS;
        public static int MEDIUM_DELAY_MS;
        public static int LONG_DELAY_MS;

        protected class Pair
        {
            public readonly object first;
            public readonly object second;

            public Pair(object first, object second)
            {
                this.first = first;
                this.second = second;
            }
        }

        protected virtual int GetShortDelay()
        {
            return 100;
        }

        protected virtual void SetDelays()
        {
            SHORT_DELAY_MS = GetShortDelay();
            SMALL_DELAY_MS = SHORT_DELAY_MS * 5;
            MEDIUM_DELAY_MS = SHORT_DELAY_MS * 10;
            LONG_DELAY_MS = SHORT_DELAY_MS * 50;
        }

        [SetUp]
        public virtual void SetUp()
        {
            SetDelays();
            threadFailed = false;
        }

        [TearDown]
        public virtual void TearDown()
        {
            Assert.IsFalse(threadFailed, "A thread failed: " + threadFailedMessage);
        }

        /// <summary>
        /// Fails with message Shoulds the throw exception.
        /// </summary>
        public void ShouldThrow()
        {
            Assert.Fail("Should throw exception");
        }

        public void ThreadAssertTrue(bool b)
        {
            if (!b)
            {
                threadFailed = true;
                Assert.Fail("Thread assertion failed");
            }
        }

        public void ThreadAssertFalse(bool b)
        {
            if (b)
            {
                threadFailed = true;
                Assert.Fail("Thread assertion failed");
            }
        }

        public void ThreadAssertEquals(object a, object b)
        {
            if (a == null && b == null)
            {
                return;
            }

            if ((a != null && b == null) || (b != null && a == null))
            {
                threadFailed = true;
            }
            else if(a != null && !a.Equals(b))
            {
                threadFailed = true;
            }

            if (threadFailed)
            {
                Assert.Fail("Thread assertion failed");
            }
        }

        public void ThreadShouldThrow()
        {
            threadFailed = true;
            Assert.Fail("Thread should have thrown an Exception");
        }

        public void ThreadShouldThrow(String message)
        {
            threadFailed = true;
            threadFailedMessage = message;
            Assert.Fail(message);
        }

        public void ThreadUnexpectedException()
        {
            threadFailed = true;
        }

        public void ThreadUnexpectedException(Exception e)
        {
            threadFailed = true;
            threadFailedMessage = e.Message;
        }

        /// <summary>
        /// Fails with message Unexpected Exception.
        /// </summary>
        public void UnexpectedException()
        {
            Assert.Fail("Unexpected exception");
        }

        public void UnexpectedException(Exception e)
        {
            if (e is AssertionException)
            {
                throw e;
            }
            Assert.Fail("Unexpected exception: type[{0}] - {1}", e.GetType(), e.Message);
        }
    }
}

