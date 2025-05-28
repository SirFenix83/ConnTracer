using System;
using System.Collections.Generic;

namespace ConnTracer.Network.AnomalyDetection
{
    public class RollingStats
    {
        private readonly int windowSize;
        private readonly Queue<double> values = new();
        private double sum = 0;
        private double sumSq = 0;

        public RollingStats(int windowSize)
        {
            if (windowSize <= 0) throw new ArgumentException("windowSize must be > 0");
            this.windowSize = windowSize;
        }

        public void Add(double value)
        {
            values.Enqueue(value);
            sum += value;
            sumSq += value * value;

            if (values.Count > windowSize)
            {
                var old = values.Dequeue();
                sum -= old;
                sumSq -= old * old;
            }
        }

        public double Mean => values.Count == 0 ? 0 : sum / values.Count;

        public double StdDev
        {
            get
            {
                if (values.Count == 0) return 0;
                double mean = Mean;
                double variance = (sumSq / values.Count) - (mean * mean);
                return variance > 0 ? Math.Sqrt(variance) : 0;
            }
        }

        public double ZScore(double value)
        {
            double stdDev = StdDev;
            if (stdDev == 0) return 0;
            return (value - Mean) / stdDev;
        }
    }
}
