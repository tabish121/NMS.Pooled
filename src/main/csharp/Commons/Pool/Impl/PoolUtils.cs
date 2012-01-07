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
using System.Runtime;

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    public sealed class PoolUtils
    {
        private PoolUtils()
        {
        }

        /// <summary>
        /// Should the supplied Exception be re-thrown (eg if it is an instance of
        /// one of the Throwables that should never be swallowed). Used by the pool
        /// error handling for operations that throw exceptions that normally need to
        /// be ignored.
        /// </summary>
        public static void CheckRethrow(Exception e)
        {
            if (e is ThreadAbortException)
            {
                throw e as ThreadAbortException;
            }

            // All other instances of Exceptions will be silently swallowed
            // New instances should be added here as needed.
        }

        public static void PreFill(ObjectPool<Object> pool, int count)
        {
            if (pool == null)
            {
                throw new ArgumentException("pool must not be null.");
            }
            
            for (int i = 0; i < count; i++)
            {
                pool.AddObject();
            }
        }

        public static void PreFill<K,V>(KeyedObjectPool<K,V> keyedPool, K key, int count) where V : class
        {
            if (keyedPool == null)
            {
                throw new ArgumentException("keyedPool must not be null.");
            }

            if (key == null)
            {
                throw new ArgumentException("key must not be null.");
            }

            for (int i = 0; i < count; i++)
            {
                keyedPool.AddObject(key);
            }
        }

    }
}

