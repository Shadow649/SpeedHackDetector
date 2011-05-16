using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedHackDetector.Filter
{
    public interface Filter<T>
    {
        bool DoFilter(T param);

        String Username { get;}

        void Reset();

        int Sequence { get; set; }
    }
}
