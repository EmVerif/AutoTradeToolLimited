using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradeTool._90_Library
{
    public static class Technicals
    {
        public static double CalcMovingAverage(IReadOnlyList<double> inDataList)
        {
            double ret = 0;

            foreach (var data in inDataList)
            {
                ret += data;
            }
            ret /= inDataList.Count;

            return ret;
        }

        public static double CalcOneSigma(IReadOnlyList<double> inDataList)
        {
            double sigma;
            double sum = 0;
            double squareSum = 0;
            Int32 count = inDataList.Count;

            foreach (var data in inDataList)
            {
                sum += data;
                squareSum += data * data;
            }
            sigma = Math.Sqrt(((count * squareSum) - (sum * sum)) / (count * (count - 1)));

            return sigma;
        }
    }
}
