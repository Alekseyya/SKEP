using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Extensions
{
    public static class DoubleExtention
    {
        /// <summary>
        /// В случае целых чисел необходимо отображать значения часов без запятой.
        /// В случае наличия дробной части разряда десятых и сотых необходимо отображать значения соответствующие количеству знаков после запятой.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string RoundingIntegersAndFractionToTwoDigits(this Double value)
        {
            double fract = value - (int)value;
            return (fract == 0.0) ? value.ToString("0") : value.ToString("0.00");
        }

    }
}
