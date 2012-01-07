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

namespace Apache.NMS.Pooled.Commons.Collections.Concurrent.Locks
{
    public interface Lock
    {
        /// <summary>
        /// Attempts to acquire the Lock, waiting if the Lock is not available at
        /// the time of calling.  If the Thread blocks and is later interrupted this
        /// method will not honor the interruption request.
        /// </summary>
        void Lock();

        /// <summary>
        /// Attempts to acquire the Lock, waiting if the Lock is not available at
        /// the time of calling.  If the Thread blocks and is later interrupted this
        /// method will honor the interruption request and throw an exception of
        /// type ThreadInterruptedException.
        /// </summary>
        void LockInterruptibly();

        /// <summary>
        /// Tries to acquire the lock an succeeds only if it is free at the time of
        /// calling this method.  Returns true if successful, false otherwise.
        /// </summary>
        bool TryLock();

        /// <summary>
        /// Tries to acquire the lock waiting the given time if necessary before
        /// giving up and returning.  If the method returns true then the Loack was
        /// available otherwise it could not be acquired in the given time.  If the
        /// Thread is interrupted while waiting for the Lock a
        /// ThreadInterruptedException is thrown.
        /// </summary>
        bool TryLock(long millisecs);

        /// <summary>
        /// Tries to acquire the lock waiting the given time if necessary before
        /// giving up and returning.  If the method returns true then the Loack was
        /// available otherwise it could not be acquired in the given time.  If the
        /// Thread is interrupted while waiting for the Lock a
        /// ThreadInterruptedException is thrown.
        /// </summary>
        bool TryLock(TimeSpan duration);

        /// <summary>
        /// Release the Lock.  Typically only the thread that acquired the Lock is
        /// allowed to unlock the Lock object however this behavior can be
        /// implementation specific.
        /// </summary>
        void Unlock();

        /// <summary>
        /// Returns a new Condition instance that is bound to this Lock instance.
        /// Before waiting on the condition the lock must be held by the current
        /// thread. A call to Condition.Await() will atomically release the lock
        /// before waiting and re-acquire the lock before the wait returns.
        /// </summary>
        Condition NewCondition();
    }
}

