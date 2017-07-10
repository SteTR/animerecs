﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeRecs.UpdateStreams
{
    static class WebClientExtensions
    {
        public static async Task<string> GetStringAsync(this IWebClient webClient, string url, CancellationToken cancellationToken)
        {
            using (IWebClientResult result = await webClient.GetAsync(url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await result.ReadResponseAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public static async Task<string> GetStringAsync(this IWebClient webClient, WebClientRequest request, CancellationToken cancellationToken)
        {
            using (IWebClientResult result = await webClient.GetAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                return await result.ReadResponseAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public static Task<IWebClientResult> GetAsync(this IWebClient webClient, string url, CancellationToken cancellationToken)
        {
            return webClient.GetAsync(new WebClientRequest(url), cancellationToken);
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

