/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly Dictionary<Type, List<object>> _listeners;

        public EventPublisher()
        {
            _listeners = new Dictionary<Type, List<object>>();
        }

        public void Publish<TEvent>(TEvent @event)
        {
            List<object> list;
            if (_listeners.TryGetValue(typeof(TEvent), out list))
            {
                foreach (var listener in list.OfType<IEventListener<TEvent>>())
                {
                    listener.Apply(@event);
                }
            }
        }

        public void Subscribe(object listener)
        {
            Ensure.IsNotNull("listener", listener);

            var eventTypes = listener
                .GetType()
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventListener<>))
                .Select(x => x.GetGenericArguments()[0]);

            bool foundAny = false;
            foreach (var eventType in eventTypes)
            {
                foundAny = true;
                List<object> list;
                if (!_listeners.TryGetValue(eventType, out list))
                {
                    _listeners[eventType] = list = new List<object>();
                }

                list.Add(listener);
            }

            if (!foundAny)
            {
                throw new ArgumentException("The provided listener did not implement IEventPublisher<>. As such, no events were subscribed.", "listener");
            }
        }
    }
}