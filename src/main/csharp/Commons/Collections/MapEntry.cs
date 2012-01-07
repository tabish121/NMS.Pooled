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

namespace Apache.NMS.Pooled.Commons.Collections
{
    /// <summary>
    /// Internal implementation of an Entry used in Map objects.
    /// </summary>
    public class MapEntry<K, V> : Entry<K, V>, ICloneable where K : class where V : class
    {
        private K key;
        private V val;

        public MapEntry(K key) : base()
        {
            this.key = key;
        }

        public MapEntry(K key, V val) : base()
        {
            this.key = key;
            this.val = val;
        }

        public virtual K Key
        {
            get { return key; }
        }

        public virtual V Value
        {
            get { return val; }
            set { this.val = value; }
        }

        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }

        public override bool Equals(Object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is MapEntry<K, V>)
            {
                MapEntry<K, V> entry = obj as MapEntry<K, V>;

                return (key == null ? entry.Key == null : key.Equals(entry.Key)) &&
                       (val == null ? entry.Value == null : val.Equals(entry.Value));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (key == null ? 0 : key.GetHashCode()) ^
                   (val == null ? 0 : val.GetHashCode());
        }

        public override String ToString()
        {
            return key + "=" + val;
        }
    }
}

