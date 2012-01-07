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

namespace Apache.NMS.Pooled.Util
{
    public class ConnectionKey
    {
        private readonly String username;
        private readonly String password;
        private readonly int hash;

        public ConnectionKey(String username, String password)
        {
            this.username = username;
            this.password = password;

            this.hash = 31;
            if (!String.IsNullOrEmpty(username))
            {
                hash += username.GetHashCode();
            }

            hash *= 31;
            if (!String.IsNullOrEmpty(password))
            {
                hash += password.GetHashCode();
            }
        }

        public String Username
        {
            get { return this.username; }
        }

        public String Password
        {
            get { return this.password; }
        }

        public override bool Equals(Object that)
        {
            if (this == that)
            {
                return true;
            }

            if (that is ConnectionKey)
            {
                return Equals((ConnectionKey)that);
            }

            return false;
        }

        public bool Equals(ConnectionKey that)
        {
            return AreEqual(this.username, that.username) && AreEqual(this.password, that.password);
        }

        public override int GetHashCode ()
        {
            return this.hash;
        }

        public static bool AreEqual(Object o1, Object o2)
        {
            if (o1 == o2)
            {
                return true;
            }

            return o1 != null && o2 != null && o1.Equals(o2);
        }
    }
}

