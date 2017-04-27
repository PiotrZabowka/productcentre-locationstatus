using System;
using System.Collections.Generic;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker.Messaging
{
    public class ProductCreationRequestProcessed
    {
        public string SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public Guid LocationId { get; set; }
        public bool WithSuccess { get; set; }
        /// <summary>
        /// True means that location feed has not changed since last update and we will update only SessionDate value for given SessionId
        /// </summary>
        public bool LocationStatusNotChanged { get; set; }
    }
}
 