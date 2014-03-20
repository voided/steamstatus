using BookSleeve;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class SteamManager
    {
        static SteamManager _instance = new SteamManager();
        public static SteamManager Instance { get { return _instance; } }


        MasterMonitor mainMonitor;
        Dictionary<IPEndPoint, Monitor> monitors;

        RedisConnection redis;


        SteamManager()
        {
            redis = new RedisConnection( "localhost" );

            // the main monitor will bootstrap the CM list
            mainMonitor = new MasterMonitor();

            monitors = new Dictionary<IPEndPoint, Monitor>();
        }


        public void Start()
        {
            Log.WriteInfo( "SteamManager", "Connecting to Redis instance..." );
            redis.Wait( redis.Open() );

            Log.WriteInfo( "SteamManager", "Connecting master monitor to Steam..." );
            mainMonitor.Connect();
        }

        public void Stop()
        {
            Log.WriteInfo( "SteamManager", "Closing connection to Redis, {0} messages to drain...", redis.OutstandingCount );

            // we shouldn't have too many queued up messages, so it should
            // be okay to wait to drain the queue
            redis.Wait( redis.CloseAsync( false ) );

            Log.WriteInfo( "SteamManager", "Disconnecting master monitor from Steam..." );
            mainMonitor.Disconnect();

            foreach ( var monitor in monitors.Values )
            {
                monitor.Disconnect();
            }
        }

        public void Tick()
        {
            mainMonitor.DoTick();

            foreach ( var monitor in monitors.Values )
            {
                monitor.DoTick();
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

            redis.Strings.Set( 10, "steamstatus:num_servers", cmList.Count() );
        }

        public void NotifyCMOnline( Monitor monitor )
        {
            string keyName = string.Format( "steamstatus:{0}", monitor.Server );

            var monitorParams = new Dictionary<string, object>
            {
                { "server", monitor.Server },
                { "status", "Online" },
                { "result", null },
            };

            redis.Hashes.Set( 10, keyName, monitorParams.ToRedisHash() );
            redis.Keys.Expire( 10, keyName, (int)TimeSpan.FromMinutes( 30 ).TotalSeconds );

            redis.Sets.Add( 10, "steamstatus:servers", monitor.Server.ToString() );

            var resolveTask = Task<IPHostEntry>.Factory.FromAsync( Dns.BeginGetHostEntry, Dns.EndGetHostEntry, monitor.Server.Address, null );

            resolveTask.ContinueWith( task =>
            {
                redis.Hashes.Set( 10, keyName, "host", task.Result.HostName );
            }, TaskContinuationOptions.OnlyOnRanToCompletion );
        }

        public void NotifyCMOffline( Monitor monitor, EResult result = EResult.Invalid )
        {
            string keyName = string.Format( "steamstatus:{0}", monitor.Server );

            var monitorParams = new Dictionary<string, object>
            {
                { "server", monitor.Server },
                { "status", "Offline" },
                { "result", null },
            };

            if ( result != EResult.Invalid )
            {
                monitorParams[ "result" ] = result;
            }

            redis.Hashes.Set( 10, keyName, monitorParams.ToRedisHash() );
            redis.Keys.Expire( 10, keyName, (int)TimeSpan.FromMinutes( 30 ).TotalSeconds );
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

                Log.WriteWarn( "SteamManager", "CM {0} has been removed from CM list", goneServer );
            }
        }
    }
}
