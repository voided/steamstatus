using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class SteamMonitor
    {
        static SteamMonitor _instance = new SteamMonitor();
        public static SteamMonitor Instance { get { return _instance; } }


        Monitor mainMonitor;
        Dictionary<IPEndPoint, Monitor> monitors;


        SteamMonitor()
        {
            // the main monitor will bootstrap the CM list
            mainMonitor = new Monitor( null );

            monitors = new Dictionary<IPEndPoint, Monitor>();
        }


        public void Start()
        {
            mainMonitor.Connect();
        }

        public void Tick()
        {
            mainMonitor.Tick();

            foreach ( var monitor in monitors.Values )
            {
                monitor.Tick();
            }
        }


        public void UpdateCMList( IEnumerable<IPEndPoint> cmList )
        {
            // handle any new CMs we've learned about
            var newCms = cmList.Except( monitors.Keys );
            HandleNewCMs( newCms.ToArray() );

            // handle any CMs that have gone away
            var goneCms = monitors.Keys.Except( cmList );
            HandleGoneCMs( goneCms.ToArray() );
        }


        void HandleNewCMs( IPEndPoint[] newCms )
        {
            for ( int x = 0 ; x < newCms.Length ; ++x )
            {
                IPEndPoint newServer = newCms[ x ];

                var newMonitor = new Monitor( newServer );
                monitors[ newServer ] = newMonitor;

                newMonitor.Connect( DateTime.Now + TimeSpan.FromSeconds( x ) );
            }
        }

        void HandleGoneCMs( IPEndPoint[] goneCms )
        {
            foreach ( IPEndPoint goneServer in goneCms )
            {
                Monitor goneMonitor = monitors[ goneServer ];
                goneMonitor.Disconnect();

                monitors.Remove( goneServer );
            }
        }
    }
}
