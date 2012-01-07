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

using NUnit.Framework;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public abstract class TestKeyedObjectPoolFactory
    {
        protected KeyedObjectPoolFactory<Object,Object> MakeFactory()
        {
            return MakeFactory(CreateObjectFactory());
        }

        protected abstract KeyedObjectPoolFactory<Object,Object> MakeFactory(KeyedPoolableObjectFactory<Object,Object> objectFactory);

        private sealed class InternalKeyedPoolableObjectFactory : KeyedPoolableObjectFactory<Object, Object>
        {
            private readonly MethodCallPoolableObjectFactory wrapped =
                new MethodCallPoolableObjectFactory();

            public Object CreateObject(Object key)
            {
                return this.wrapped.CreateObject();
            }

            public void DestroyObject(Object key, Object obj)
            {
                this.wrapped.DestroyObject(obj);
            }

            public bool ValidateObject(Object key, Object obj)
            {
                return this.wrapped.ValidateObject(obj);
            }

            public void ActivateObject(Object key, Object obj)
            {
                this.wrapped.ActivateObject(obj);
            }

            public void SuspendObject(Object key, Object obj)
            {
                this.wrapped.SuspendObject(obj);
            }
        }

        protected static KeyedPoolableObjectFactory<Object,Object> CreateObjectFactory()
        {
            return new InternalKeyedPoolableObjectFactory();
        }
    
        [Test]
        public void TestCreatePool()
        {
            KeyedObjectPoolFactory<Object,Object> factory;
            try
            {
                factory = MakeFactory();
            }
            catch (NotSupportedException)
            {
                return;
            }
            KeyedObjectPool<Object,Object> pool = factory.CreatePool();
            pool.Close();
        }
    
        [Test]
        public void TestToString()
        {
            KeyedObjectPoolFactory<Object,Object> factory;
            try
            {
                factory = MakeFactory();
            }
            catch (NotSupportedException)
            {
                return;
            }
            factory.ToString();
        }

    }
}

