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
using System.Text;

using Apache.NMS.Pooled.Commons.Collections;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public class MethodCall
    {
        private readonly String name;
        private readonly List<Object> parameters;
        private Object returned;
    
        public MethodCall(String name) : this(name, null)
        {
        }
    
        public MethodCall(String name, Object param) : this(name, CollectionUtils.SingletonList<Object>(param))
        {
        }

        public MethodCall(String name, Object param1, Object param2) :
            this(name, Arrays.AsList(new Object[] {param1, param2}))
        {
        }

        public MethodCall(String name, List<Object> parameters)
        {
            if (name == null)
            {
                throw new ArgumentException("name must not be null.");
            }

            this.name = name;

            if (parameters != null)
            {
                this.parameters = parameters;
            }
            else
            {
                this.parameters = CollectionUtils.EmptyList<Object>();
            }
        }
    
        public String Name
        {
            get { return name; }
        }
    
        public List<Object> Params
        {
            get { return parameters; }
        }

        public Object Returned
        {
            get { return this.returned; }
            set { this.returned = value; }
        }

        public MethodCall SetReturned(Object val)
        {
            this.returned = val;
            return this;
        }

        public override bool Equals(Object o)
        {
            if (ReferenceEquals(this, o)) return true;
            if (o == null || GetType() != o.GetType()) return false;

            MethodCall that = o as MethodCall;

            if (name != null ? !name.Equals(that.name) : that.name != null) return false;
            if (parameters != null ? !parameters.Equals(that.parameters) : that.parameters != null) return false;
            if (returned != null ? !returned.Equals(that.returned) : that.returned != null) return false;
    
            return true;
        }
    
        public override int GetHashCode()
        {
            int result;
            result = (name != null ? name.GetHashCode() : 0);
            result = 29 * result + (parameters != null ? parameters.GetHashCode() : 0);
            result = 29 * result + (returned != null ? returned.GetHashCode() : 0);
            return result;
        }
    
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MethodCall");
            sb.Append("{name='").Append(name).Append('\'');

            if (!parameters.IsEmpty())
            {
                sb.Append(", params=").Append(parameters);
            }

            if (returned != null)
            {
                sb.Append(", returned=").Append(returned);
            }

            sb.Append('}');
            return sb.ToString();
        }
    }
}

