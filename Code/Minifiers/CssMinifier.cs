using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NTTData.SitecoreCDN.Minifiers
{
    /// <summary>
    /// Css Minifier
    /// </summary>
    public class CssMinifier
    {
        /// <summary>
        /// Minify css content
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Minify(string input)
        {
            string output = input;
            output = Regex.Replace(output, @"[a-zA-Z]+#", "#");
            output = Regex.Replace(output, @"[\n\r]+\s*", string.Empty);
            output = Regex.Replace(output, @"\s+", " ");
            output = Regex.Replace(output, @"\s?([:,;{}])\s?", "$1");
            output = output.Replace(";}", "}");
            output = Regex.Replace(output, @"([\s:]0)(px|pt|%|em)", "$1");

            // Remove comments from CSS
            output = Regex.Replace(output, @"/\*[\d\D]*?\*/", string.Empty);

            return output;
        }
    }

}
