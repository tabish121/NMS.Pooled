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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks
{
    /// <summary>
    /// A synchronizer that may be exclusively owned by a thread.  This class provides
    /// a basis for creating locks and related synchronizers that may entail a notion
    /// of ownership.  The AbstractOwnableSynchronizer class itself does not manage or
    /// use this information. However, subclasses and tools may use appropriately
    /// maintained values to help control and monitor access and provide diagnostics.
    /// </summary>
    public class AbstractOwnableSynchronizer
    {
        private Thread exclusiveOwnerThread;

        protected AbstractOwnableSynchronizer()
        {
        }

        /// <summary>
        /// Gets or sets the exclusive owner thread for this Synchronizer.
        /// </summary>
        protected Thread ExclusiveOwnerThread
        {
            get { return this.exclusiveOwnerThread; }
            set { this.exclusiveOwnerThread = value; }
        }
    }
}

