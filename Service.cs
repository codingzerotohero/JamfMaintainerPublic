using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamfMaintainer
{
    public class Service
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Processor _processor;
        private bool _isRunning;

        //temp test

        //prod 
        public Service(Processor processor)
        {
            _processor = processor;
        }

        public bool Start()
        {
            _isRunning = true;
            Task.Run(() => _processor.CheckForChangesAndUpdateUsers());
            Logger.Info("Service started");

            return true;
        }

        public bool Stop()
        {
            _isRunning = false;
            Logger.Info("Service stopped");
            return true;
        }
    }
}
