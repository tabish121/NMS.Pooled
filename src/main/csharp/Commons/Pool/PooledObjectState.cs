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
    public enum PooledObjectState
    {
        /// <summary>
        /// In the queue, not in use.
        /// </summary>
        IDLE,

        /// <summary>
        /// Object is in use.
        /// </summary>
        ALLOCATED,

        /// <summary>
        /// In the queue, currently being tested for possible eviction.
        /// </summary>
        MAINTAIN_EVICTION,

        /// <summary>
        /// Not in the queue, currently being tested for possible eviction. An
        /// attempt to borrow the object was made while being tested which removed it
        /// from the queue. It should be returned to the head of the queue once
        /// eviction testing completes.
        ///
        /// TODO: Consider allocating object and ignoring the result of the eviction test.
        /// </summary>
        MAINTAIN_EVICTION_RETURN_TO_HEAD,

        /// <summary>
        /// In the queue, currently being validated.
        /// </summary>
        MAINTAIN_VALIDATION,

        /// <summary>
        /// Not in queue, currently being validated. The object was borrowed while
        /// being validated and since testOnBorrow was configured, it was removed
        /// from the queue and pre-allocated. It should be allocated once validation
        /// completes.
        /// </summary>
        MAINTAIN_VALIDATION_PREALLOCATED,

        /// <summary>
        /// Not in queue, currently being validated. An attempt to borrow the object
        /// was made while previously being tested for eviction which removed it from
        /// the queue. It should be returned to the head of the queue once validation
        /// completes.
        /// </summary>
        MAINTAIN_VALIDATION_RETURN_TO_HEAD,

        /// <summary>
        /// Failed maintenance (e.g. eviction test or validation) and will be / has
        /// been destroyed
        /// </summary>
        INVALID
    }
}

