using SteamKit2;
using SteamShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace SteamStatus.Models
{
    public class HomeIndexViewModel
    {
        public int NumServers { get; set; }

        public List<ServerInfo> Servers { get; set; }


        public HomeIndexViewModel()
        {
            Servers = new List<ServerInfo>();
        }
    }
}