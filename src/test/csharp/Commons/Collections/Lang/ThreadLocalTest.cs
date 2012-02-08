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
using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Lang
{
    [TestFixture]
    public class ThreadLocalTest
    {
        private Object THREADVALUE;

        [Test]
        public void TestConstructor()
        {
            new ThreadLocal<Object>();
        }

        private class TestThreadLocalInit : ThreadLocal<String>
        {
            protected override String InitialValue
            {
                get { return "initial"; }
            }
        }

        [Test]
        public void TestRemove()
        {
            ThreadLocal<String> tl = new TestThreadLocalInit();

            Assert.AreEqual("initial", tl.Value);
            tl.Value = "fixture";
            Assert.AreEqual("fixture", tl.Value);
            tl.Remove();
            Assert.AreEqual("initial", tl.Value);
        }

        private class TestThreadLocalGet : ThreadLocal<Object>
        {
            private readonly Object initialValue;

            public TestThreadLocalGet(Object initialValue) : base()
            {
                this.initialValue = initialValue;
            }

            protected override Object InitialValue
            {
                get { return initialValue; }
            }
        }

        private void TestGetRunnable(object state)
        {
            ThreadLocal<Object> local = state as ThreadLocal<Object>;
            THREADVALUE = local.Value;
        }

        [Test]
        public void TestGet()
        {
            ThreadLocal<Object> l = new ThreadLocal<Object>();
            Assert.IsNull(l.Value, "ThreadLocal's initial value is null");

            // The ThreadLocal has to run once for each thread that touches the
            // ThreadLocal
            Object INITIAL_VALUE = "'foo'";
            ThreadLocal<Object> tl = new TestThreadLocalGet(INITIAL_VALUE);

            Assert.IsTrue(tl.Value == INITIAL_VALUE,
                "ThreadLocal's initial value should be " + INITIAL_VALUE + " but is " + tl.Value);

            Thread t = new Thread(TestGetRunnable);

            // Wait for the other Thread assign what it observes as the value of the variable
            t.Start(tl);
            try
            {
                t.Join();
            }
            catch (ThreadInterruptedException)
            {
                Assert.Fail("Interrupted!!");
            }

            Assert.IsTrue(THREADVALUE == INITIAL_VALUE,
                "ThreadLocal's initial value in other Thread should be " + INITIAL_VALUE);
        }

        private void TestSetRunnable(object state)
        {
            ThreadLocal<Object> local = state as ThreadLocal<Object>;
            THREADVALUE = local.Value;
        }

        [Test]
        public void TestSet()
        {
            Object OBJ = new Object();
            ThreadLocal<Object> l = new ThreadLocal<Object>();
            l.Value = OBJ;
            Assert.IsTrue(l.Value == OBJ , "ThreadLocal's initial value is " + OBJ);

            Thread t = new Thread(TestSetRunnable);

            // Wait for the other Thread assign what it observes as the value of the variable
            t.Start(l);
            try
            {
                t.Join();
            }
            catch (ThreadInterruptedException)
            {
                Assert.Fail("Interrupted!!");
            }

            // ThreadLocal is not inherited, so the other Thread should see it as null
            Assert.IsNull(THREADVALUE, "ThreadLocal's value in other Thread should be null");
        }
    }
}

