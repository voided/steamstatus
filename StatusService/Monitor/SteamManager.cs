using BookSleeve;
using SteamKit2;
using SteamShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

            Log.WriteInfo( "SteamManager", "Clearing stale servers" );
            redis.Keys.Remove( 10, "steamstatus:servers" );
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

        public async void NotifyCMOnline( Monitor monitor )
        {
            IPHostEntry dnsEntry = null;

            try
            {
                dnsEntry = await Task<IPHostEntry>.Factory.FromAsync( Dns.BeginGetHostEntry, Dns.EndGetHostEntry, monitor.Server.Address, null );
            }
            catch ( SocketException )
            {
                // not the end of the world if we can't reverse resolve the hostname
            }

            string keyName = monitor.Server.ToString();

            var serverInfo = new ServerInfo
            {
                Server = keyName,
                IsOnline = true,
                Result = null,
            };

            if ( dnsEntry != null )
            {
                serverInfo.Host = dnsEntry.HostName;
            }

            var task = redis.Hashes.Set( 10, "steamstatus:servers", keyName, serverInfo.SerializeToBytes() );
        }

        public async void NotifyCMOffline( Monitor monitor, EResult result = EResult.Invalid )
        {
            string keyName = monitor.Server.ToString();

            ServerInfo serverInfo = ServerInfo.DeserializeFromBytes( await redis.Hashes.Get( 10, "steamstatus:servers", keyName ) );

            serverInfo.IsOnline = false;

            if ( result != EResult.Invalid )
            {
                serverInfo.Result = result.ToString();
            }

            var task = redis.Hashes.Set( 10, "steamstatus:servers", keyName, serverInfo.SerializeToBytes() );
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
