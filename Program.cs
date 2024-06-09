using System.ServiceProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Logging;
using JamfMaintainer.Entities;
using Newtonsoft.Json;
using NLog;
using Topshelf;
using Microsoft.Extensions.Configuration;
using NLog.LayoutRenderers;
using SQLitePCL;


namespace JamfMaintainer
{
    class Program
    {
        private static readonly Microsoft.Extensions.Logging.ILogger _consoleLogger = new ColorConsoleLogger("_consoleLogger", () => new ColorConsoleLoggerConfiguration());
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool consoleMode;
        
        static async Task Main(string[] args)
        {
            //Startup configurations
            System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls11 |
            System.Net.SecurityProtocolType.Tls12 |
            System.Net.SecurityProtocolType.Tls13;

            string fullrunMode = "";

            var api = new APIConfig();

            var archive = new ArchiveContext();

            bool ensured = archive.Database.EnsureCreated();
            archive.Database.Migrate();

            if (ensured)
            {
                _logger.Info("Ensured and migrated JamfArchive.db");
            }

            consoleMode = false;
            if (args.Length > 0)
            {
                _consoleLogger.LogInformation("!!Console mode!! Will execute full run.");
                if (args[0].ToLower() == "fullrun" || args[0].ToLower() == "students" || args[0].ToLower() == "teachers" || args[0].ToLower() == "vo" || args[0].ToLower() == "single" || args[0].ToLower() == "groups")
                {
                    fullrunMode = args[0];

                    consoleMode = true;

                    _logger.Info("Stopping service!!");
                    _consoleLogger.LogInformation("Stopping service!!");

                    bool stopped = StopService("JamfMaintainer");
                    if (!stopped)
                    {
                        _logger.Info("Could not stop service");
                        _consoleLogger.LogInformation("Could not stop service. Shutting down.");
                        return;
                    }
                }
            }

            if (!consoleMode)
            {
                //Run as service
                try
                {
                    _logger.Info("Starting JamfMaintainer!");

                    var processor = new Processor(api, _logger, _consoleLogger, archive);
                    var service = new Service(processor);
                    
                    HostFactory.Run(configurator =>
                    {
                        configurator.Service<Service>(s =>
                        {
                            s.ConstructUsing(name => service);
                            s.WhenStarted((svc, hostControl) => svc.Start());
                            s.WhenStopped((svc, hostControl) =>
                            {
                                svc.Stop();
                                archive.Dispose();
                                LogManager.Shutdown();
                                return true;
                            });
                        });

                        configurator.SetServiceName("JamfMaintainer");
                        configurator.SetDisplayName("JamfMaintainer");
                        configurator.SetDescription("Periodically pulls data from Master stores and updates users in Jamf if changes as occurred");
                    });
                }
                catch(Exception ex)
                {
                    _logger.Error(ex);
                    _logger.Info("Something went wrong here!");
                }
            }
            else
            {
                //Run as console application
                _consoleLogger.LogInformation("Starting JamfMaintainer! CONSOLE MODE");

                var processor = new Processor(api, _logger, _consoleLogger, archive);

                if (fullrunMode == "fullrun")
                {
                    await processor.FullRunAll();
                }

                if (fullrunMode == "students")
                {
                    await processor.FullRunStudents();
                }

                if (fullrunMode == "vo")
                {
                    await processor.FullRunVO();
                }

                if (fullrunMode == "teachers")
                {
                    await processor.FullRunTeachers();
                }

                if (fullrunMode == "single")
                {
                    if (!string.IsNullOrWhiteSpace(args[1]))
                    {
                        await processor.RunSpecificUser(args[1]);
                    }
                    else
                    {
                        _consoleLogger.LogInformation("Can not run Single user, because no valid ADObjectID was provided.");
                    }
                }

                if (fullrunMode == "groups")
                {
                    await processor.MaintainJamfGroups();
                }

                archive.Dispose();

                _consoleLogger.LogInformation("Detailed logs can be found here: C:\\Program Files (x86)\\xxx\\JamfMaintainer\\Logs");

                _consoleLogger.LogInformation("Restarting JamfMaintainer service");

                StartService("JamfMaintainer");

                LogManager.Shutdown();
            }
        }

        public static bool StopService(string servicename)
        {
            bool stopped = false;
            try
            {
                using (var serviceController = new ServiceController(servicename))
                {
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                        stopped = true;
                    }
                    else
                    {
                        stopped = true;
                    }
                }
            }
            catch(Exception ex)
            {
                _consoleLogger.LogError("Error stopping service!");
                _consoleLogger.LogInformation(ex.Message);
            }
            return stopped;
        }

        private static void StartService(string servicename)
        {
            bool started = false;
            try
            {
                using (var serviceController = new ServiceController(servicename))
                {
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        serviceController.Start();
                    }
                    else
                    {
                        _consoleLogger.LogInformation("JamfMaintainer service already running!");
                    }
                }
            }
            catch(Exception ex)
            {
                _consoleLogger.LogError("Error restarting service!");
                _consoleLogger.LogError(ex.Message);
            }
        }
    }
}
