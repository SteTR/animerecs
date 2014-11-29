﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Hosting.Self;
#if MONO
using Mono.Unix;
using Mono.Unix.Native;
#endif

namespace AnimeRecs.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            Logging.Log.Info("Starting AnimeRecs web app");

            HostConfiguration config = new HostConfiguration()
            {
                RewriteLocalhost = false
            };

            string portString = ConfigurationManager.AppSettings["Hosting.Port"];
            uint port;
            if (!uint.TryParse(portString, out port))
            {
                throw new Exception("Hosting.Port is not a valid port number.");
            }

            using (var host = new NancyHost(config, new Uri(string.Format("http://localhost:{0}", port))))
            {
                host.Start();
                Logging.Log.InfoFormat("Started listening on port {0}", port);
#if MONO
                    WaitForUnixStopSignal();
#else
                Console.ReadLine();
#endif
                Logging.Log.Info("Got stop signal, stopping web app");
            }
        }

#if MONO
        static void WaitForUnixStopSignal()
        {
            UnixSignal[] signals = new UnixSignal[]
                        {
                            new UnixSignal(Signum.SIGINT),
                            new UnixSignal(Signum.SIGTERM)
                        };
            UnixSignal.WaitAny(signals);
        }
#endif
    }
}

// Copyright (C) 2014 Greg Najda
//
// This file is part of AnimeRecs.Web.
//
// AnimeRecs.Web is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AnimeRecs.Web is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with AnimeRecs.Web.  If not, see <http://www.gnu.org/licenses/>.