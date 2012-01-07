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

namespace Apache.NMS.Pooled
{
    public class PooledQueueBrowser : IQueueBrowser
    {
        private readonly PooledSession session;
        private readonly IQueueBrowser browser;

        public PooledQueueBrowser(PooledSession session, IQueueBrowser browser)
        {
            this.session = session;
            this.browser = browser;
        }

        public PooledSession Session
        {
            get { return this.session; }
        }

        public IQueueBrowser Browser
        {
            get { return this.browser; }
        }

        public void Close()
        {
            this.session.OnQueueBrowserClosed(browser);
            this.browser.Close();
        }

        public void Dispose()
        {
            this.session.OnQueueBrowserClosed(browser);
            this.browser.Dispose();
        }

        public string MessageSelector
        {
            get { return this.browser.MessageSelector; }
        }

        public IQueue Queue
        {
            get { return this.browser.Queue; }
        }

        public override string ToString()
        {
            return "PooledQueueBrowser { " + browser + " }";
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return this.browser.GetEnumerator();
        }
    }
}

