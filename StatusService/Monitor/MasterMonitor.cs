using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class MasterMonitor : BaseMonitor
    {
        DateTime nextRelog = DateTime.MaxValue;


        public MasterMonitor()
            : base( null )
        {
        }


        protected override void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            if ( callback.Result == EResult.OK )
            {
                nextRelog = DateTime.Now + TimeSpan.FromMinutes( 30 );
            }

            base.OnLoggedOn( callback );
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
