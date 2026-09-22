using System;

namespace Sandbox.Utils
{
    /// <summary>
    /// Utility class for formatting numbers with abbreviated notation (K, M, B, T, etc.)
    /// </summary>
    public static class NumberFormatter
    {
        // Extended suffix list supporting numbers up to ~10^303
        private static readonly string[] Suffixes =
        {
            "",      // 10^0
            "K",     // 10^3 Kilo
            "M",     // 10^6 Mega
            "B",     // 10^9 Billion
            "T",     // 10^12 Trillion
            "Q",     // 10^15 Quadrillion
            "Qu",    // 10^18 Quintillion
            "Sx",    // 10^21 Sextillion
            "Sp",    // 10^24 Septillion
            "Oc",    // 10^27 Octillion
            "No",    // 10^30 Nonillion
            "Dc",    // 10^33 Decillion
            "Ud",    // 10^36 Undecillion
            "Do",    // 10^39 Duodecillion
            "Td",    // 10^42 Tredecillion
            "Qt",    // 10^45 Quattuordecillion
            "Qd",    // 10^48 Quindecillion
            "Sx2",   // 10^51 Sexdecillion
            "Sp2",   // 10^54 Septendecillion
            "Oc2",   // 10^57 Octodecillion
            "No2",   // 10^60 Novemdecillion
            "Vi",    // 10^63 Vigintillion
            "Rv",    // 10^66 Unvigintillion
            "Dv",    // 10^69 Duovigintillion
            "Trv",   // 10^72 Trevigintillion
            "Qv",    // 10^75 Quattuorvigintillion
            "Quv",   // 10^78 Quinvigintillion
            "Sxv",   // 10^81 Sexvigintillion
            "Spv",   // 10^84 Septenvigintillion
            "Ocv",   // 10^87 Octovigintillion
            "Nov",   // 10^90 Novemvigintillion
            "Tr",    // 10^93 Trigintillion
            "Utr",   // 10^96 Untrigintillion
            "Dtr",   // 10^99 Duotrigintillion
            "Ttr",   // 10^102 Tretrigintillion
            "Qtr",   // 10^105 Quattuortrigintillion
            "Qutr",  // 10^108 Quintrigintillion
            "Sxtr",  // 10^111 Sextrigintillion
            "Sptr",  // 10^114 Septentrigintillion
            "Octr",  // 10^117 Octotrigintillion
            "Notr",  // 10^120 Novemtrigintillion
            "Qg",    // 10^123 Quadragintillion
            "Uqg",   // 10^126 Unquadragintillion
            "Dqg",   // 10^129 Duoquadragintillion
            "Tqg",   // 10^132 Tretquadragintillion
            "Qqg",   // 10^135 Quattuorquadragintillion
            "Ququg", // 10^138 Quinquadragintillion
            "Sxqg",  // 10^141 Sexquadragintillion
            "Spqg",  // 10^144 Septenquadragintillion
            "Ocqg",  // 10^147 Octoquadragintillion
            "Noqug", // 10^150 Novemquadragintillion
            "Qn",    // 10^153 Quinquagintillion
            "Uqn",   // 10^156 Unquinquagintillion
            "Dqn",   // 10^159 Duoquinquagintillion
            "Tqn",   // 10^162 Tretquinquagintillion
            "Qqn",   // 10^165 Quattuorquinquagintillion
            "Quqn",  // 10^168 Quinquinquagintillion
            "Sxqn",  // 10^171 Sexquinquagintillion
            "Spqn",  // 10^174 Septenquinquagintillion
            "Ocqn",  // 10^177 Octoquinquagintillion
            "Noqun", // 10^180 Novemquinquagintillion
            "Sx3",   // 10^183 Sexagintillion
            "Usx",   // 10^186 Unsexagintillion
            "Dsx",   // 10^189 Duosexagintillion
            "Tsx",   // 10^192 Tretosexagintillion
            "Qsx",   // 10^195 Quattuorsexagintillion
            "Quxs",  // 10^198 Quinsexagintillion
            "Sxxs",  // 10^201 Sexsexagintillion
            "Spxs",  // 10^204 Septensexagintillion
            "Ocxs",  // 10^207 Octosexagintillion
            "Noxs",  // 10^210 Novemsexagintillion
            "Sp3",   // 10^213 Septuagintillion
            "Usp",   // 10^216 Unseptuagintillion
            "Dsp",   // 10^219 Duoseptuagintillion
            "Tsp",   // 10^222 Tretoseptuagintillion
            "Qsp",   // 10^225 Quattuorseptuagintillion
            "Qusp",  // 10^228 Quinseptuagintillion
            "Sxsp",  // 10^231 Sexseptuagintillion
            "Spsp",  // 10^234 Septenseptuagintillion
            "Ocsp",  // 10^237 Octoseptuagintillion
            "Nosp",  // 10^240 Novemseptuagintillion
            "Oc3",   // 10^243 Octogintillion
            "Uoc",   // 10^246 Unoctogintillion
            "Doc",   // 10^249 Duooctogintillion
            "Toc",   // 10^252 Tretooctogintillion
            "Qoc",   // 10^255 Quattuoroctogintillion
            "Quoc",  // 10^258 Quinoctogintillion
            "Sxoc",  // 10^261 Sexoctogintillion
            "Spoc",  // 10^264 Septenoctogintillion
            "Ococ",  // 10^267 Octooctogintillion
            "Nooc",  // 10^270 Novemoctogintillion
            "No3",   // 10^273 Nonagintillion
            "Uno",   // 10^276 Unnonagintillion
            "Dno",   // 10^279 Duononagintillion
            "Tno",   // 10^282 Tretonagintillion
            "Qno",   // 10^285 Quattuornonagintillion
            "Quno",  // 10^288 Quinnonagintillion
            "Sxno",  // 10^291 Sexnonagintillion
            "Spno",  // 10^294 Septennonagintillion
            "Ocno",  // 10^297 Octononagintillion
            "Nono",  // 10^300 Novemnonagintillion
            "Cent"   // 10^303 Centillion
        };
        private const double ThresholdPerSuffix = 1000.0;

        /// <summary>
        /// Formats a double with abbreviated notation (1200000 becomes "1.2M")
        /// Maintains full precision for large numbers
        /// </summary>
        /// <param name="value">The number to format</param>
        /// <param name="decimals">Number of decimal places (default: 1)</param>
        /// <returns>Formatted string with suffix</returns>
        public static string FormatWithSuffix( double value, int decimals = 2 )
        {
            if ( value == 0 ) return "0";

            // Handle negative numbers
            bool isNegative = value < 0;
            value = Math.Abs( value );

            int suffixIndex = 0;

            // Determine which suffix to use
            while ( value >= ThresholdPerSuffix && suffixIndex < Suffixes.Length - 1 )
            {
                value /= ThresholdPerSuffix;
                suffixIndex++;
            }

            // Format with specified decimals
            string formatted = value.ToString( $"F{decimals}" );

            // Remove trailing zeros after decimal point if decimals > 0
            if ( decimals > 0 )
            {
                formatted = formatted.TrimEnd( '0' ).TrimEnd( '.' );
            }

            return (isNegative ? "-" : "") + formatted + Suffixes[suffixIndex];
        }

        /// <summary>
        /// Formats a float with abbreviated notation
        /// </summary>
        public static string FormatWithSuffix( float value, int decimals = 2 )
        {
            return FormatWithSuffix( (double)value, decimals );
        }

        /// <summary>
        /// Formats an integer with abbreviated notation
        /// </summary>
        public static string FormatWithSuffix( int value, int decimals = 2 )
        {
            return FormatWithSuffix( (double)value, decimals );
        }

        /// <summary>
        /// Formats a long with abbreviated notation
        /// </summary>
        public static string FormatWithSuffix( long value, int decimals = 2 )
        {
            return FormatWithSuffix( (double)value, decimals );
        }
    }
}
