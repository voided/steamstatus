using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace SteamStatus.Models
{
    public class HomeIndexViewModel
    {
        public class Server
        {
            public IPEndPoint Address { get; set; }
            public string Host { get; set; }

            public string Status { get; set; }

            public string Result { get; set; }
        }


        public List<Server> Servers { get; set; }

        public HomeIndexViewModel()
        {
            Servers = new List<Server>();
        }
    }
}