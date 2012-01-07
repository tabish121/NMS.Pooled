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

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    public class GenericKeyedObjectPool<K, V> : KeyedObjectPool<K, V> where V : class
    {
        private bool testOnBorrow = false;

        public bool TestOnBorrow
        {
            get { return testOnBorrow; }
            set { this.testOnBorrow = value; }
        }

        public V BorrowObject(K key)
        {
            return null;
        }

        public void ReturnObject(K key, V borrowed)
        {
        }

        public void InvalidateObject(K key, V borrowed)
        {
        }

        public void AddObject(K key)
        {
        }

        public void Clear()
        {
        }

        public void Clear(K key)
        {
        }

        public int IdleCount
        {
            get { return 0; }
        }

        public int ActiveCount
        {
            get { return 0; }
        }

        public int KeyedActiveCount(K key)
        {
            return 0;
        }

        public int KeyedIdleCount(K key)
        {
            return 0;
        }

        public void Close()
        {
        }

        public void Dispose()
        {
            Close();
        }
    }
}

