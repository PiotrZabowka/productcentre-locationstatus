using System;
using System.Collections.Generic;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class UpdateLocationStatusSessionRequest
    {
        public string SessionId { get; set; }
        public DateTime SessionDate { get; set; }
    }
}
