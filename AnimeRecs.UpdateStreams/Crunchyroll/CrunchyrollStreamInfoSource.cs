﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeRecs.UpdateStreams.Crunchyroll
{
    class CrunchyrollStreamInfoSource : IAnimeStreamInfoSource
    {
        private const string LoginUrl = "https://www.crunchyroll.com/?a=formhandler";

        private IWebClient _webClient;
        private string _crunchyrollUsername;
        private string _crunchyrollPassword;
        
        public CrunchyrollStreamInfoSource(string crunchyrollUsername, string crunchyrollPassword, IWebClient webClient)
        {
            _webClient = webClient;
            _crunchyrollUsername = crunchyrollUsername;
            _crunchyrollPassword = crunchyrollPassword;
        }

        public async Task<ICollection<AnimeStreamInfo>> GetAnimeStreamInfoAsync(CancellationToken cancellationToken)
        {
            // Need to log in to Crunchyroll because anime classified as "mature content" is not listed
            // to unregistered users.

            // POST to login, get cookies
            await LoginAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // Use cookies in GET
            var source = new CrunchyrollLoggedInStreamInfoSource(loggedInWebClient: _webClient);
            return await source.GetAnimeStreamInfoAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        private async Task LoginAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Logging into Crunchyroll.");
            WebClientRequest request = new WebClientRequest(LoginUrl);
            request.UserAgent = "animerecs.com stream update tool";
            request.PostParameters.Add(new KeyValuePair<string, string>("formname", "RpcApiUser_Login"));
            request.PostParameters.Add(new KeyValuePair<string, string>("fail_url", "http://www.crunchyroll.com/login"));
            request.PostParameters.Add(new KeyValuePair<string, string>("name", _crunchyrollUsername));
            request.PostParameters.Add(new KeyValuePair<string, string>("password", _crunchyrollPassword));

            using (var response = await _webClient.PostAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                // don't care about response text, just the cookie
            }

            CookieCollection cookies = _webClient.Cookies.GetCookies(new Uri("http://www.crunchyroll.com"));
            if (cookies["sess_id"] == null)
            {
                throw new Exception("Crunchyroll sess_id cookie was not set after logging in, maybe username/password was wrong.");
            }
        }
    }
}

// Copyright (C) 2017 Greg Najda
//
// This file is part of AnimeRecs.UpdateStreams
//
// AnimeRecs.UpdateStreams is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AnimeRecs.UpdateStreams is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with AnimeRecs.UpdateStreams.  If not, see <http://www.gnu.org/licenses/>.
