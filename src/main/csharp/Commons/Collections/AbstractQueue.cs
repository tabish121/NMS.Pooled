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
    public abstract class AbstractQueue<E> : AbstractCollection<E>, Queue<E> where E : class
    {
        public AbstractQueue() : base()
        {
        }

        public abstract bool Offer(E element);

        public abstract E Poll();

        public abstract E Peek();

        public override bool Add(E o)
        {
            if (null == o)
            {
                throw new NullReferenceException();
            }

            if (Offer(o))
            {
                return true;
            }

            throw new IllegalStateException();
        }

        public override bool AddAll(Collection<E> c)
        {
            if (null == c)
            {
                throw new NullReferenceException();
            }

            if (ReferenceEquals(this, c))
            {
                throw new ArgumentException();
            }

            return base.AddAll(c);
        }

        /// <summary>
        /// Remove and return the element at the head of the queue.
        /// </summary>
        /// <exception cref='NoSuchElementException'>
        /// Is thrown if the queue is empty.
        /// </exception>
        public virtual E Remove()
        {
            E o = Poll();

            if (null == o)
            {
                throw new NoSuchElementException();
            }

            return o;
        }

        /// <summary>
        /// Returns but does not remove the element at the head of the queue.
        /// </summary>
        /// <exception cref='NoSuchElementException'>
        /// Is thrown if the queue is empty.
        /// </exception>
        public virtual E Element()
        {
            E o = Peek();
            if (null == o)
            {
                throw new NoSuchElementException();
            }
            return o;
        }
    
        /// <summary>
        /// Removes all elements of the queue, leaving it empty.
        /// </summary>
        public override void Clear()
        {
            E o;
            do
            {
                o = Poll();
            }
            while (null != o);
        }
    }
}

