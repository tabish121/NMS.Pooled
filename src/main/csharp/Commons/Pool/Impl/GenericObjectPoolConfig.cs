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
    /// <summary>
    /// Class to hold all the configuration data for a GenericObjectPool instance.
    /// </summary>
    public class GenericObjectPoolConfig : BaseObjectPoolConfig
    {
        /**
         * The default value for SoftMinEvictableIdleTimeMillis.
         */
        public static readonly long DEFAULT_SOFT_MIN_EVICTABLE_IDLE_TIME_MILLIS = -1;

        /**
         * The default maximum number of instances under management
         * (idle or checked out).
         */
        public static readonly int DEFAULT_MAX_TOTAL = 8;
    
        /**
         * The default cap on the number of "sleeping" instances in the pool.
         */
        public static readonly int DEFAULT_MAX_IDLE = 8;
    
        /**
         * The default minimum number of "sleeping" instances in the pool before
         * before the evictor thread (if active) spawns new objects.
         */
        public static readonly int DEFAULT_MIN_IDLE = 0;

        private long softMinEvictableIdleTimeMillis = DEFAULT_MIN_EVICTABLE_IDLE_TIME_MILLIS;
        private int maxTotal = DEFAULT_MAX_TOTAL;
        private int maxIdle = DEFAULT_MAX_IDLE;
        private int minIdle = DEFAULT_MIN_IDLE;
    
        public long SoftMinEvictableIdleTimeMillis
        {
            get { return softMinEvictableIdleTimeMillis; }
            set { this.softMinEvictableIdleTimeMillis = value; }
        }

        public int MaxTotal
        {
            get { return maxTotal; }
            set { this.maxTotal = value; }
        }

        public int MaxIdle
        {
            get { return maxIdle; }
            set { this.maxIdle = value; }
        }

        public int MinIdle
        {
            get { return minIdle; }
            set { this.minIdle = value; }
        }

        public override Object Clone()
        {
            return base.Clone();
        }
    }
}

