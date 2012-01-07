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

namespace Apache.NMS.Pooled.Commons.Pool.Impl
{
    public abstract class BaseObjectPoolConfig : ICloneable
    {
        /**
         * The default LIFO status. True means that BorrowObject returns the most
         * recently used ("last in") idle object in the pool (if there are idle
         * instances available). False means that the pool behaves as a FIFO queue -
         * objects are taken from the idle object pool in the order that they are
         * returned to the pool.
         */
        public static readonly bool DEFAULT_LIFO = true;
    
        /**
         * The default maximum amount of time (in milliseconds) the BorrowObject
         * method should block before throwing an exception when the pool is
         * exhausted and is true.
         */
        public static readonly long DEFAULT_MAX_WAIT = -1L;

        /**
         * The default value for MinEvictableIdleTimeMillis.
         */
        public static readonly long DEFAULT_MIN_EVICTABLE_IDLE_TIME_MILLIS = 1000L * 60L * 30L;

        /**
         * The default value for SoftMinEvictableIdleTimeMillis.
         */
        public static readonly long DEFAULT_SOFT_MIN_EVICTABLE_IDLE_TIME_MILLIS = -1;

        /**
         * The default number of objects to examine per run in the idle object evictor.
         */
        public static readonly int DEFAULT_NUM_TESTS_PER_EVICTION_RUN = 3;

        /**
         * The default "test on borrow" value.
         */
        public static readonly bool DEFAULT_TEST_ON_BORROW = false;
    
        /**
         * The default "test on return" value.
         */
        public static readonly bool DEFAULT_TEST_ON_RETURN = false;
    
        /**
         * The default "test while idle" value.
         */
        public static readonly bool DEFAULT_TEST_WHILE_IDLE = false;
    
        /**
         * The default "time between eviction runs" value.
         */
        public static readonly long DEFAULT_TIME_BETWEEN_EVICTION_RUNS_MILLIS = -1L;
    
        /**
         * The default "block when exhausted" value for the pool.
         */
        public static readonly bool DEFAULT_BLOCK_WHEN_EXHAUSTED = true;

        private bool lifo = DEFAULT_LIFO;
        private long maxWait = DEFAULT_MAX_WAIT;
        private long minEvictableIdleTimeMillis = DEFAULT_MIN_EVICTABLE_IDLE_TIME_MILLIS;
        private long softMinEvictableIdleTimeMillis = DEFAULT_MIN_EVICTABLE_IDLE_TIME_MILLIS;
        private int numTestsPerEvictionRun = DEFAULT_NUM_TESTS_PER_EVICTION_RUN;

        private bool testOnBorrow = DEFAULT_TEST_ON_BORROW;
        private bool testOnReturn = DEFAULT_TEST_ON_RETURN;
        private bool testWhileIdle = DEFAULT_TEST_WHILE_IDLE;
        private long timeBetweenEvictionRunsMillis = DEFAULT_TIME_BETWEEN_EVICTION_RUNS_MILLIS;

        private bool blockWhenExhausted = DEFAULT_BLOCK_WHEN_EXHAUSTED;

        public bool Lifo
        {
            get { return lifo; }
            set { this.lifo = value; }
        }

        public long MaxWait
        {
            get { return maxWait; }
            set { this.maxWait = value; }
        }

        public long MinEvictableIdleTimeMillis
        {
            get { return minEvictableIdleTimeMillis; }
            set { this.minEvictableIdleTimeMillis = value; }
        }

        public long SoftMinEvictableIdleTimeMillis
        {
            get { return softMinEvictableIdleTimeMillis; }
            set { this.softMinEvictableIdleTimeMillis = value; }
        }

        public int NumTestsPerEvictionRun
        {
            get { return numTestsPerEvictionRun; }
            set { this.numTestsPerEvictionRun = value; }
        }

        public bool TestOnBorrow
        {
            get { return testOnBorrow; }
            set { this.testOnBorrow = value; }
        }

        public bool TestOnReturn
        {
            get { return testOnReturn; }
            set { this.testOnReturn = value; }
        }
    
        public bool TestWhileIdle
        {
            get { return testWhileIdle; }
            set { this.testWhileIdle = value; }
        }

        public long TimeBetweenEvictionRunsMillis
        {
            get { return timeBetweenEvictionRunsMillis; }
            set { this.timeBetweenEvictionRunsMillis = value; }
        }

        public bool BlockWhenExhausted
        {
            get { return blockWhenExhausted; }
            set { this.blockWhenExhausted = value; }
        }

        public virtual Object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

