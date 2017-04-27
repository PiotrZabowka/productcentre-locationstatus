using System;
using System.Collections.Generic;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class UpdateLocationStatusRequest
    {
        public string SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public Guid LocationId { get; set; }
        public int ProcessedProducts { get; set; }
        public int ValidProcessedProducts { get; set; }
    }
}
