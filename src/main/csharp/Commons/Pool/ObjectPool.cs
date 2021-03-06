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

namespace Apache.NMS.Pooled.Commons.Pool
{
    /// <summary>
    /// The interface that defines a simple Object pool which requires only methods
    /// BorrowObject, ReturnObject, and InvalidateObject to be implement, all others
    /// may throw NotSupportedException.
    /// </summary>
    public interface ObjectPool<T> : IDisposable where T : class
    {
        T BorrowObject();

        void ReturnObject(T borrowed);

        void InvalidateObject(T borrowed);

        void AddObject();

        void Clear();

        int IdleCount { get; }

        int ActiveCount { get; }

        void Close();
    }
}

