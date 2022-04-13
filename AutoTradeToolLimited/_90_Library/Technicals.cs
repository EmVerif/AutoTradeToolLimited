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
            Int32 count = inDataList.Count;

            if (count > 0)
            {
                foreach (var data in inDataList)
                {
                    ret += data;
                }
                ret /= count;
            }

            return ret;
        }

        public static double CalcOneSigma(IReadOnlyList<double> inDataList)
        {
            double sigma = 0;
            double sum = 0;
            double squareSum = 0;
            Int32 count = inDataList.Count;

            if (count > 1)
            {
                foreach (var data in inDataList)
                {
                    sum += data;
                    squareSum += data * data;
                }
                sigma = Math.Sqrt(((count * squareSum) - (sum * sum)) / (count * (count - 1)));
            }

            return sigma;
        }

        public static double CalcRSI(IReadOnlyList<double> inDataList)
        {
            double rsi = 0;
            double up = 0;
            double down = 0;
            Int32 count = inDataList.Count;

            if (count > 1)
            {
                double prev = inDataList[0];

                for (var idx = 1; idx < count; idx++)
                {
                    var cur = inDataList[idx];

                    if (cur >= prev)
                    {
                        up += (cur - prev);
                    }
                    else
                    {
                        down += (prev - cur);
                    }
                    prev = cur;
                }

                rsi = up / (up + down) * 100;
            }

            return rsi;
        }
    }
}
