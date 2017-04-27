using System;
using System.Collections.Generic;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker.Messaging
{
    public class MessageEnvelope<T>
    {
        public T Message { get; set; }
    }
}
