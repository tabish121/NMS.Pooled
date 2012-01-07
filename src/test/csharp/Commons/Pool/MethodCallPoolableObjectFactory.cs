/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
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

using Apache.NMS.Pooled.Commons.Collections;

namespace Apache.NMS.Pooled.Commons.Pool
{
    public class MethodCallPoolableObjectFactory : PoolableObjectFactory<Object>
    {
        private readonly List<MethodCall> methodCalls = new ArrayList<MethodCall>();
        private int count = 0;
        private bool valid = true;
        private bool makeObjectFail;
        private bool activateObjectFail;
        private bool validateObjectFail;
        private bool suspendObjectFail;
        private bool destroyObjectFail;
    
        public void Reset()
        {
            count = 0;
            MethodCalls.Clear();
            MakeObjectFail = false;
            ActivateObjectFail = false;
            Valid = true;
            ValidateObjectFail = false;
            SuspendObjectFail = false;
            DestroyObjectFail = false;
        }
    
        public List<MethodCall> MethodCalls
        {
            get { return methodCalls; }
        }
    
        public int CurrentCount
        {
            get { return count; }
            set { this.count = value; }
        }

        public bool MakeObjectFail
        {
            get { return makeObjectFail; }
            set { this.makeObjectFail = value; }
        }
    
        public bool DestroyObjectFail
        {
            get { return destroyObjectFail; }
            set { this.destroyObjectFail = value; }
        }
    
        public bool Valid
        {
            get { return valid; }
            set { this.valid = value; }
        }

        public bool ValidateObjectFail
        {
            get { return validateObjectFail; }
            set { this.validateObjectFail = value; }
        }

        public bool ActivateObjectFail
        {
            get { return activateObjectFail; }
            set { this.activateObjectFail = value; }
        }

        public bool SuspendObjectFail
        {
            get { return suspendObjectFail; }
            set { this.suspendObjectFail = value; }
        }

        public virtual Object CreateObject()
        {
            MethodCall call = new MethodCall("CreateObject");
            methodCalls.Add(call);
            int count = this.count++;
            if (makeObjectFail)
            {
                throw new MethodAccessException("CreateObject");
            }
            Int32 obj = count;
            call.Returned = obj;
            return obj;
        }
    
        public virtual void ActivateObject(Object obj)
        {
            methodCalls.Add(new MethodCall("ActivateObject", obj));
            if (activateObjectFail)
            {
                throw new MethodAccessException("ActivateObject");
            }
        }
    
        public bool ValidateObject(Object obj)
        {
            MethodCall call = new MethodCall("ValidateObject", obj);
            methodCalls.Add(call);
            if (validateObjectFail)
            {
                throw new MethodAccessException("ValidateObject");
            }
            call.Returned = valid;
            return valid;
        }
    
        public void SuspendObject(Object obj)
        {
            methodCalls.Add(new MethodCall("SuspendObject", obj));
            if (SuspendObjectFail)
            {
                throw new MethodAccessException("SuspendObject");
            }
        }
    
        public void DestroyObject(Object obj)
        {
            methodCalls.Add(new MethodCall("DestroyObject", obj));
            if (destroyObjectFail)
            {
                throw new MethodAccessException("DestroyObject");
            }
        }
    }
}

