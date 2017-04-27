using Miinto.Bus.Core;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WorkerConfiguration.Config = new Startup().Configure();
                new Worker().Start();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unhandled exception occured.");
                throw;
            }

        }
    }
}