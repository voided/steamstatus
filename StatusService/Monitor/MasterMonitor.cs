using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class MasterMonitor : BaseMonitor
    {
        List<IPEndPoint> lastCmList;

        DateTime nextRelog = DateTime.MaxValue;


        public MasterMonitor()
            : base( null )
        {
            lastCmList = new List<IPEndPoint>();
        }


        protected override void OnConnected( SteamClient.ConnectedCallback callback )
        {
            Log.WriteInfo( "MasterMonitor", "Connected to Steam!" );

            base.OnConnected( callback );
        }

        protected override void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            Log.WriteWarn( "MasterMonitor", "Disconnected from Steam!" );

            base.OnDisconnected( callback );
        }

        protected override void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            base.OnLoggedOn( callback );

            if ( callback.Result != EResult.OK )
            {
                Log.WriteWarn( "MasterMonitor", "Unable to logon to Steam: {0}", callback.Result );
                return;
            }

            Log.WriteInfo( "MasterMonitor", "Logged onto Steam!" );

            nextRelog = DateTime.Now + TimeSpan.FromMinutes( 30 );
        }

        protected override void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            Log.WriteWarn( "MasterMonitor", "Logged off Steam: {0}", callback.Result );

            base.OnLoggedOff( callback );
        }

        protected override void OnCMList( SteamClient.CMListCallback callback )
        {
            List<IPEndPoint> sortedServers = callback.Servers
                .OrderBy( s => s.ToString() )
                .ToList();

            if ( !lastCmList.SequenceEqual( sortedServers ) )
            {
                Log.WriteInfo( "MasterMonitor", "Got new CM list containing {0} servers", sortedServers.Count );

                lastCmList = sortedServers;
            }

            base.OnCMList( callback );
        }


        protected override void Tick()
        {
            base.Tick();

            if ( DateTime.Now >= nextRelog )
            {
                if ( Client.IsConnected )
                {
                    Client.Disconnect();
                }

                nextRelog = DateTime.Now + TimeSpan.FromMinutes( 30 );
            }
        }
    }
}
