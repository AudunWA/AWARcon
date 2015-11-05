using System;
using System.Text;

namespace AWARCon
{
    public class NumberUtil
    {
        private static readonly String[] ORDINAL_SUFFIXES =
        {
            "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th"
        };

        public static String OrdinalSuffix(int value)
        {
            int n = Math.Abs(value);
            int lastTwoDigits = n%100;
            int lastDigit = n%10;
            int index = (lastTwoDigits >= 11 && lastTwoDigits <= 13) ? 0 : lastDigit;
            return ORDINAL_SUFFIXES[index];
        }

        public static String ToOrdinal(int n)
        {
            return new StringBuilder().Append(n).Append(OrdinalSuffix(n)).ToString();
        }
    }
}