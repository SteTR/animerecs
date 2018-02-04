﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimeRecs.RecEngine
{
    public interface ITrainable<in TTrainingData>
    {
        void Train(TTrainingData trainingData);
    }
}
