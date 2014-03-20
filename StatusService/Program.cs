using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main( string[] args )
        {
            string path = Assembly.GetExecutingAssembly().Location;
            path = Path.GetDirectoryName( path );
            Directory.SetCurrentDirectory( path );

            AppDomain.CurrentDomain.UnhandledException += ( sender, e ) =>
            {
                Log.WriteError( "Program", "Unhandled exception (IsTerm = {1}): {0}", e.ExceptionObject, e.IsTerminating );
            };

            TaskScheduler.UnobservedTaskException += ( sender, e ) =>
            {
                Log.WriteWarn( "Program", "Task exception (Observed = {1}): {0}", e.Exception, e.Observed );
                e.SetObserved();
            };

            var service = new Service();

#if SERVICE_BUILD
            ServiceBase.Run( service );
#else
            service.Start( args );
#endif
        }
    }
}
