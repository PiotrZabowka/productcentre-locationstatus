using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public static class WorkerConfiguration
    {
        public static IConfiguration Config { get; set; }
    }
}
