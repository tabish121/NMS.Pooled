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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

using Apache.NMS.Pooled.Commons.Collections.Concurrent;

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    /// <summary>
    /// A wrapper class for object in a Pool that adds state information
    /// necessary to properly track the object over its lifetime in the
    /// Pool it resides in.
    /// </summary>
    public sealed class PooledObject<T> : IComparable<PooledObject<T>>
    {
        private T theObject;
        private PooledObjectState state;
        private DateTime creationTime = DateTime.Now;
        private DateTime lastBorrowedTime = DateTime.Now;
        private DateTime lastReturnedTime = DateTime.Now;
        private readonly Mutex syncRoot = new Mutex();

        public PooledObject(T theObject)
        {
            this.theObject = theObject;
        }

        public Object SyncRoot
        {
            get { return this.syncRoot; }
        }

        /// <summary>
        /// The underlying object that is being pooled.
        /// </summary>
        public T TheObject
        {
            get { return theObject; }
        }

        /// <summary>
        /// The creation time for the object being pooled.
        /// </summary>
        public DateTime CreationTime
        {
            get { return this.creationTime; }
        }

        /// <summary>
        /// The last borrowed time for the object being pooled.
        /// </summary>
        public DateTime LastBorrowedTime
        {
            get { return this.lastBorrowedTime; }
        }

        /// <summary>
        /// The last returned time for the object being pooled.
        /// </summary>
        public DateTime LastReturnedTime
        {
            get { return this.lastReturnedTime; }
        }

        /// <summary>
        /// Gets the total time that the pooled object last spent in the active
        /// state.  If its currently active then this method will return successively
        /// larger values.
        /// </summary>
        public TimeSpan ActiveTime
        {
            get
            {
                DateTime returnedTime = lastReturnedTime;
                DateTime borrowedTime = lastBorrowedTime;

                if (returnedTime > borrowedTime)
                {
                    return returnedTime - borrowedTime;
                }
                else
                {
                    return DateTime.Now - borrowedTime;
                }
            }
        }

        /// <summary>
        /// Gets the total idle time for the pooled object.
        /// </summary>
        public TimeSpan IdleTime
        {
            get { return DateTime.Now - this.lastReturnedTime; }
        }

        public int CompareTo(PooledObject<T> other)
        {
            if (LastReturnedTime.Equals(other.LastReturnedTime))
            {
                return RuntimeHelpers.GetHashCode(this) - RuntimeHelpers.GetHashCode(other);
            }

            return LastReturnedTime.CompareTo(other.LastReturnedTime);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append("Object: ");
            result.Append(theObject.ToString());
            result.Append(", State: ");
            result.Append(state.ToString());

            return result.ToString();
        }
    
        public bool StartEvictionTest()
        {
            lock(this.syncRoot)
            {
                if (state == PooledObjectState.IDLE)
                {
                    state = PooledObjectState.MAINTAIN_EVICTION;
                    return true;
                }
            }

            return false;
        }
    
        public bool EndEvictionTest(LinkedBlockingDeque<PooledObject<T>> idleQueue)
        {
            lock(this.syncRoot)
            {
                if (state == PooledObjectState.MAINTAIN_EVICTION)
                {
                    state = PooledObjectState.IDLE;
                    return true;
                }
                else if (state == PooledObjectState.MAINTAIN_EVICTION_RETURN_TO_HEAD)
                {
                    state = PooledObjectState.IDLE;
                    idleQueue.OfferFirst(this);
                }
            }

            return false;
        }

        public bool Allocate()
        {
            lock(this.syncRoot)
            {
                if (state == PooledObjectState.IDLE)
                {
                    state = PooledObjectState.ALLOCATED;
                    lastBorrowedTime = DateTime.Now;
                    return true;
                }
                else if (state == PooledObjectState.MAINTAIN_EVICTION)
                {
                    state = PooledObjectState.MAINTAIN_EVICTION_RETURN_TO_HEAD;
                    return false;
                }
            }

            return false;
        }
    
        public bool Deallocate()
        {
            lock(this.syncRoot)
            {
                if (state == PooledObjectState.ALLOCATED)
                {
                    state = PooledObjectState.IDLE;
                    lastReturnedTime = DateTime.Now;
                    return true;
                }
            }

            return false;
        }
    
        public void Invalidate()
        {
            lock(this.syncRoot)
            {
                state = PooledObjectState.INVALID;
            }
        }

    }
}

