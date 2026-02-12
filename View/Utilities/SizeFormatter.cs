using System;

namespace easySave_BMT.View_
{
    public static class SizeFormatter
    {
        public static string Format(long octet)
        {
            if (octet > 1000000000000)
            {
                return Math.Round((decimal)octet / 1000000000000, 2) + "To";
            }
            else if (octet > 1000000000)
            {
                return Math.Round((decimal)octet / 1000000000, 2) + "Go";
            }
            else if (octet > 1000000)
            {
                return Math.Round((decimal)octet / 1000000, 2) + "Mo";
            }
            else if (octet > 1000)
            {
                return Math.Round((decimal)octet / 1000, 2) + "ko";
            }
            else
            {
                return octet + "o";
            }
        }
    }
}
