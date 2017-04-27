using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class RunAndWait : IDisposable
    {
        public static RunAndWait WithMessage(string message = "Press [Ctrl+C] to exit.") => new RunAndWait(message);
        ManualResetEvent exitEvent;
        private string message;

        public RunAndWait(string message)
        {
            this.message = message;
            exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
        }
        public void Dispose()
        {
            Console.WriteLine(this.message);
            exitEvent.WaitOne();
            Log.Logger.Debug("RunAndWait disposed.");
        }
    }
}
