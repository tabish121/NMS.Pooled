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

namespace Apache.NMS.Pooled.Commons.Collections
{
    [TestFixture]
    public class AbstractCollectionTest
    {
        private class TestAddAbstractCollection<E> : AbstractCollection<E> where E : class
        {
            public override Iterator<E> Iterator()
            {
                Assert.Fail("iterator should not get called");
                return null;
            }

            public override int Size()
            {
                Assert.Fail("size should not get called");
                return 0;
            }
        }

        private class TestAddAllAbstractCollection<E> : TestAddAbstractCollection<E> where E : class
        {
            private readonly Collection<E> source;

            public TestAddAllAbstractCollection(Collection<E> source)
            {
                this.source = source;
            }

            public override bool Add(E element)
            {
                Assert.IsTrue(source.Contains(element));
                return true;
            }
        }

        private class TestContainsAbstractCollection<E> : TestAddAbstractCollection<E> where E : class
        {
            private readonly Collection<E> source;

            public TestContainsAbstractCollection(Collection<E> source)
            {
                this.source = source;
            }

            public override bool Contains(E element)
            {
                Assert.IsTrue(source.Contains(element));
                return true;
            }
        }

        private class TestIsEmptyCollection<E> : AbstractCollection<E> where E : class
        {
            public bool sizeCalled = false;

            public override Iterator<E> Iterator()
            {
                Assert.Fail("iterator should not get called");
                return null;
            }

            public override int Size()
            {
                sizeCalled = true;
                return 0;
            }
        }

        private class TestToStringCollection<E> : AbstractCollection<E> where E : class
        {
            public override Iterator<E> Iterator()
            {
                return null;
            }

            public override int Size()
            {
                return 1;
            }
        }

        [Test]
        public void TestAddAnObject()
        {
            TestAddAbstractCollection<String> ac = new TestAddAbstractCollection<String>();

            try
            {
                ac.Add(null);
            }
            catch (NotSupportedException)
            {
            }
        }

        [Test]
        public void TestAddAllCollection()
        {
            Collection<String> fixtures = Arrays.AsList(new String[] {"0", "1", "2"});
            TestAddAllAbstractCollection<String> ac = new TestAddAllAbstractCollection<String>(fixtures);
            Assert.IsTrue(ac.AddAll(fixtures));
        }

        [Test]
        public void TestContainsAllCollection()
        {
            Collection<String> fixtures = Arrays.AsList(new String[] {"0", "1", "2"});
            TestContainsAbstractCollection<String> ac = new TestContainsAbstractCollection<String>(fixtures);
            Assert.IsTrue(ac.ContainsAll(fixtures));
        }

        [Test]
        public void TestIsEmpty()
        {
            TestIsEmptyCollection<String> ac = new TestIsEmptyCollection<String>();
            Assert.IsTrue(ac.IsEmpty());
            Assert.IsTrue(ac.sizeCalled);
        }

        private class TestRemoveAllCollectionImpl : AbstractCollection<String>
        {
            private String[] removed = null;

            public TestRemoveAllCollectionImpl(String[] removed)
            {
                this.removed = removed;
            }

            private class MyIterator : Iterator<String>
            {
                String[] removed;
                readonly String[] values = new String[] {"0", "1", "2"};
                int index = 0;

                public MyIterator(String[] removed)
                {
                    this.removed = removed;
                }

                public bool HasNext
                {
                    get { return index < values.Length; }
                }

                public String Next()
                {
                    return values[index++];
                }

                public void Remove()
                {
                    removed[index - 1] = values[index - 1];
                }
            }

            public override Iterator<String> Iterator()
            {
                return new MyIterator(removed);
            }

            public override int Size()
            {
                return 3;
            }
        }

        [Test]
        public void TestRemoveAllCollection() {
            String[] removed = new String[3];
            TestRemoveAllCollectionImpl ac = new TestRemoveAllCollectionImpl(removed);
            Assert.IsTrue(ac.RemoveAll(Arrays.AsList(new String[]{"0", "1", "2"})));
            foreach (String r in removed)
            {
                if (!"0".Equals(r) && !"1".Equals(r) && !"2".Equals(r))
                {
                    Assert.Fail("an unexpected element was removed");
                }
            }
        }

        private class TestRetainAllCollectionImpl : AbstractCollection<String>
        {
            private String[] removed = null;

            public TestRetainAllCollectionImpl(String[] removed)
            {
                this.removed = removed;
            }

            private class MyIterator : Iterator<String>
            {
                String[] removed;
                readonly String[] values = new String[] {"0", "1", "2"};
                int index = 0;

                public MyIterator(String[] removed)
                {
                    this.removed = removed;
                }

                public bool HasNext
                {
                    get { return index < values.Length; }
                }

                public String Next()
                {
                    return values[index++];
                }

                public void Remove()
                {
                    removed[index - 1] = values[index - 1];
                }
            }

            public override Iterator<String> Iterator()
            {
                return new MyIterator(removed);
            }

            public override int Size()
            {
                return 3;
            }
        }

        [Test]
        public void TestRetainAllCollection()
        {
            String[] removed = new String[1];
            TestRetainAllCollectionImpl ac = new TestRetainAllCollectionImpl(removed);
            Assert.IsTrue(ac.RetainAll(Arrays.AsList(new String[]{"1", "2"})));
            Assert.AreEqual("0", removed[0]);
        }

        private class TestToArrayCollection : AbstractCollection<String>
        {
            private class MyIterator : Iterator<String>
            {
                readonly String[] values = new String[] {"0", "1", "2"};
                int index = 0;

                public bool HasNext
                {
                    get { return index < values.Length; }
                }

                public String Next()
                {
                    return values[index++];
                }

                public void Remove()
                {
                    Assert.Fail("remove should not get called");
                }
            }

            public override Iterator<String> Iterator()
            {
                return new MyIterator();
            }

            public override int Size()
            {
                return 3;
            }
        }

        [Test]
        public void TestToArray()
        {
            TestToArrayCollection ac = new TestToArrayCollection();
            String[] array = ac.ToArray();
            Assert.AreEqual(3, array.Length);
            foreach (String o in array)
            {
                if (!"0".Equals(o) && !"1".Equals(o) && !"2".Equals(o))
                {
                    Assert.Fail("an unexpected element was removed");
                }
            }
        }

        [Test]
        public void TestToString()
        {
            TestToStringCollection<String> c = new TestToStringCollection<String>();
            try
            {
                // AbstractCollection.toString() doesn't verify
                // whether Iterator() returns null value or not
                c.ToString();
                Assert.Fail("No expected NullPointerException");
            }
            catch (NullReferenceException)
            {
            }
        }

    }
}

