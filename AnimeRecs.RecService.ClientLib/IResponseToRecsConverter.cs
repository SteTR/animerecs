﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimeRecs.RecService.DTO;
using AnimeRecs.RecEngine;

namespace AnimeRecs.RecService.ClientLib
{
    internal interface IResponseToRecsConverter<TResponse>
        where TResponse : GetMalRecsResponse
    {
        IEnumerable<IRecommendation> ConvertResponseToRecommendations(TResponse response);
    }
}

namespace AnimeRecs.RecService.ClientLib.Registrations
{
    internal partial class ResponseToRecsConverter
    {
        internal static readonly string ConvertMethodName = "ConvertResponseToRecommendations";
    }
}

// Copyright (C) 2012 Greg Najda
//
// This file is part of AnimeRecs.RecService.ClientLib.
//
// AnimeRecs.RecService.ClientLib is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AnimeRecs.RecService.ClientLib is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with AnimeRecs.RecService.ClientLib.  If not, see <http://www.gnu.org/licenses/>.