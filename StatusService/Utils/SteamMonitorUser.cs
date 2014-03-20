using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace StatusService
{
    class SteamMonitorUser : ClientMsgHandler
    {
        public void LogOn()
        {
            var logonMsg = new ClientMsgProtobuf<CMsgClientLogon>( EMsg.ClientLogon );

            SteamID steamId = new SteamID( 0, SteamID.AllInstances, Client.ConnectedUniverse, EAccountType.AnonUser );

            logonMsg.ProtoHeader.steamid = steamId;
            logonMsg.Body.protocol_version = MsgClientLogon.CurrentProtocol;

            Client.Send( logonMsg );
        }


        public override void HandleMsg( IPacketMsg packetMsg )
        {
        }
    }
}
