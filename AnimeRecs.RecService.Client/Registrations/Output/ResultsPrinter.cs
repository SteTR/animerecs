﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimeRecs.RecService.ClientLib;
using AnimeRecs.RecEngine;
using AnimeRecs.RecEngine.MAL;

namespace AnimeRecs.RecService.Client.Registrations.Output
{
    internal partial class ResultsPrinter
    {
        private Dictionary<string, Action<MalRecResults<IEnumerable<IRecommendation>>, IDictionary<int, MalListEntry>, decimal>> RegisteredPrinters;

        public ResultsPrinter()
        {
            RegisteredPrinters = new Dictionary<string, Action<MalRecResults<IEnumerable<IRecommendation>>, IDictionary<int, MalListEntry>, decimal>>(StringComparer.OrdinalIgnoreCase)
            {
                { DTO.RecommendationTypes.AverageScore, PrintAverageScoreResults },
                { DTO.RecommendationTypes.MostPopular, PrintMostPopularResults },
                { DTO.RecommendationTypes.RatingPrediction, PrintRatingPredictionResults },
                { DTO.RecommendationTypes.AnimeRecs, PrintAnimeRecsResults }
            };
        }

        public void PrintResults(MalRecResults<IEnumerable<IRecommendation>> results, IDictionary<int, MalListEntry> animeList, decimal targetScore)
        {
            if (RegisteredPrinters.ContainsKey(results.RecommendationType))
            {
                RegisteredPrinters[results.RecommendationType](results, animeList, targetScore);
            }
            else
            {
                PrintFallbackResults(results, animeList, targetScore);
            }
        }
    }
}

// Copyright (C) 2012 Greg Najda
//
// This file is part of AnimeRecs.RecService.Client.
//
// AnimeRecs.RecService.Client is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AnimeRecs.RecService.Client is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with AnimeRecs.RecService.Client.  If not, see <http://www.gnu.org/licenses/>.