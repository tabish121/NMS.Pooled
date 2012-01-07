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

using Apache.NMS.Util;

namespace Apache.NMS.Pooled.Commons.Pool
{
    /// <summary>
    /// A simple implementation of the ObjectPool interface that implements all the
    /// optional operations and either throws an exception or returns a value that
    /// indicates the method is not supported.
    /// </summary>
    public abstract class BaseObjectPool<T> : ObjectPool<T> where T : class
    {
        protected readonly Atomic<bool> closed = new Atomic<bool>(false);

        public abstract T BorrowObject();

        public abstract void ReturnObject(T borrowed);

        public abstract void InvalidateObject(T borrowed);

        public virtual void AddObject()
        {
            throw new NotSupportedException();
        }

        public virtual void Clear()
        {
            throw new NotSupportedException();
        }

        public virtual int IdleCount
        {
            get { return -1; }
        }

        public virtual int ActiveCount
        {
            get { return -1; }
        }

        public virtual void Close()
        {
            this.closed.Value = true;
        }

        public void Dispose()
        {
            Close();
        }

        public bool IsClosed
        {
            get { return closed.Value; }
        }

        protected void CheckClosed()
        {
            if (closed.Value)
            {
                throw new InvalidOperationException("The Pool is Closed");
            }
        }
    }
}

