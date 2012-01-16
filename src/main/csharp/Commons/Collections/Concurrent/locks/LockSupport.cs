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
    /// Lock support methods.  This class associates, with each thread that uses it,
    /// a permit (in the sense of the Semaphore class). A call to park will return
    /// immediately if the permit is available, consuming it in the process; otherwise
    /// it may block. A call to unpark makes the permit available, if it was not already
    /// available. (Unlike with Semaphores though, permits do not accumulate. There is
    /// at most one.)
    /// </summary>
    public static class LockSupport
    {
        private static object mutex = new object();

        /// <summary>
        /// Disables the current thread for thread scheduling purposes unless the
        /// permit is available.  If the permit is available then it is consumed and
        /// the call returns immediately; otherwise the current thread becomes disabled
        /// for thread scheduling purposes and lies dormant until one of three things
        /// happens; Some other thread invokes unpark with the current thread as the
        /// target; or Some other thread interrupts the current thread; or The call
        /// spuriously (that is, for no reason) returns.
        /// </summary>
        public static void Park()
        {
            EventWaitHandle parkToken = GetParkToken();
            try
            {
                parkToken.WaitOne();
            }
            catch (ThreadInterruptedException)
            {
                SetInterrupted();
            }
            parkToken.Reset();
        }

        /// <summary>
        /// Disables the current thread for thread scheduling purposes for the specified
        /// deadline unless the permit is available.  If the permit is available then it
        /// is consumed and the call returns immediately; otherwise the current thread
        /// becomes disabled for thread scheduling purposes and lies dormant until one
        /// of three things happens; Some other thread invokes unpark with the current
        /// thread as the target; or Some other thread interrupts the current thread;
        /// or The call spuriously (that is, for no reason) returns.
        /// </summary>
        public static void Park(int deadline)
        {
            if (deadline <= 0)
            {
                return;
            }

            EventWaitHandle parkToken = GetParkToken();
            try
            {
                parkToken.WaitOne(deadline, false);
            }
            catch (ThreadInterruptedException)
            {
                SetInterrupted();
            }
            parkToken.Reset();
        }

        /// <summary>
        /// Disables the current thread for thread scheduling purposes for the specified
        /// deadline unless the permit is available.  If the permit is available then it
        /// is consumed and the call returns immediately; otherwise the current thread
        /// becomes disabled for thread scheduling purposes and lies dormant until one
        /// of three things happens; Some other thread invokes unpark with the current
        /// thread as the target; or Some other thread interrupts the current thread;
        /// or The call spuriously (that is, for no reason) returns.
        /// </summary>
        public static void Park(TimeSpan deadline)
        {
            EventWaitHandle parkToken = GetParkToken();
            try
            {
                parkToken.WaitOne((int) deadline.TotalMilliseconds, false);
            }
            catch (ThreadInterruptedException)
            {
                SetInterrupted();
            }
            parkToken.Reset();
        }

        /// <summary>
        /// Disables the current thread for thread scheduling purposes for the specified
        /// deadline unless the permit is available.  If the permit is available then it
        /// is consumed and the call returns immediately; otherwise the current thread
        /// becomes disabled for thread scheduling purposes and lies dormant until one
        /// of three things happens; Some other thread invokes unpark with the current
        /// thread as the target; or Some other thread interrupts the current thread;
        /// or The call spuriously (that is, for no reason) returns.
        /// </summary>
        public static void ParkUntil(DateTime deadline)
        {
            if (deadline < DateTime.Now)
            {
                return;
            }

            TimeSpan interval = deadline - DateTime.Now;
            EventWaitHandle parkToken = GetParkToken();
            try
            {
                parkToken.WaitOne((int) interval.TotalMilliseconds, false);
            }
            catch (ThreadInterruptedException)
            {
                SetInterrupted();
            }
            parkToken.Reset();
        }

        /// <summary>
        /// Park this instance.
        /// </summary>
        public static void ParkNanos(long nanos)
        {
            TimeSpan interval = TimeSpan.FromTicks(nanos / 100);
            EventWaitHandle parkToken = GetParkToken();
            parkToken.WaitOne((int) interval.TotalMilliseconds, false);
            parkToken.Reset();
        }

        /// <summary>
        /// Makes available the permit for the given thread, if it was not already available.
        /// If the thread was blocked on park then it will unblock. Otherwise, its next call
        /// to park is guaranteed not to block. This operation is not guaranteed to have any
        /// effect at all if the given thread has not been started.
        /// </summary>
        public static void UnPark(Thread theThread)
        {
            EventWaitHandle parkToken = GetParkToken();
            parkToken.Set();
        }

        /// <summary>
        /// Tests whether the thread has been interrupted while parked. The interrupted
        /// status of the thread is cleared by this method. In other words, if this method
        /// were to be called twice in succession, the second call would return false
        /// (unless the current thread were interrupted again, after the first call had
        /// cleared its interrupted status and before the second call had examined it).
        /// </summary>
        public static bool Interrupted()
        {
            bool interrupted = false;

            lock(mutex)
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot("Interrupted");
                if (Thread.GetData(slot) != null)
                {
                    interrupted = true;
                }
                Thread.SetData(slot, null);
            }

            return interrupted;
        }

        private static void SetInterrupted()
        {
            lock(mutex)
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot("Interrupted");
                Thread.SetData(slot, true);
            }
        }

        private static EventWaitHandle GetParkToken()
        {
            EventWaitHandle parkToken;

            lock(mutex)
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot("ParkToken");
                parkToken = Thread.GetData(slot) as EventWaitHandle;
                if (parkToken == null)
                {
                    parkToken = new ManualResetEvent(false);
                    Thread.SetData(slot, parkToken);
                }
            }

            return parkToken;
        }
    }
}

