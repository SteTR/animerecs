﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using AnimeRecs.UpdateStreams;
using AnimeRecs.DAL;

namespace AnimeRecs.UpdateStreams.Tests
{
    [TestFixture]
    public partial class FunimationStreamInfoSourceTests
    {
        [Test]
        public void TestRegex()
        {
            FunimationStreamInfoSource funi = new FunimationStreamInfoSource();
            ICollection<AnimeStreamInfo> streams = funi.GetAnimeStreamInfo(FunimationStreamInfoSourceTests.TestHtml);
            Assert.That(streams, Contains.Item(new AnimeStreamInfo("Haibane Renmei", "http://www.funimation.com/haibane-renmei/episodes", StreamingService.Funimation)));
        }
    }
}

// Copyright (C) 2012 Greg Najda
//
// This file is part of AnimeRecs.UpdateStreams.Tests
//
// AnimeRecs.UpdateStreams.Tests is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AnimeRecs.UpdateStreams.Tests is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with AnimeRecs.UpdateStreams.Tests.  If not, see <http://www.gnu.org/licenses/>.
//
//  If you modify AnimeRecs.UpdateStreams.Tests, or any covered work, by linking 
//  or combining it with HTML Agility Pack (or a modified version of that 
//  library), containing parts covered by the terms of the Microsoft Public 
//  License, the licensors of AnimeRecs.UpdateStreams.Tests grant you additional 
//  permission to convey the resulting work. Corresponding Source for a non-
//  source form of such a combination shall include the source code for the parts 
//  of HTML Agility Pack used as well as that of the covered work.