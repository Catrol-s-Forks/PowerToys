﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerToys.Run.Plugin.Calculator
{
    /// <summary>
    /// Tries to convert all numbers in a text from one culture format to another.
    /// </summary>
    public class NumberTranslator
    {
        private readonly CultureInfo sourceCulture;
        private readonly CultureInfo targetCulture;
        private readonly Regex splitRegexForSource;
        private readonly Regex splitRegexForTarget;

        private NumberTranslator(CultureInfo sourceCulture, CultureInfo targetCulture)
        {
            this.sourceCulture = sourceCulture;
            this.targetCulture = targetCulture;

            splitRegexForSource = GetSplitRegex(this.sourceCulture);
            splitRegexForTarget = GetSplitRegex(this.targetCulture);
        }

        /// <summary>
        /// Create a new <see cref="NumberTranslator"/>.
        /// </summary>
        /// <param name="sourceCulture">source culture</param>
        /// <param name="targetCulture">target culture</param>
        /// <returns>Number translator for target culture</returns>
        public static NumberTranslator Create(CultureInfo sourceCulture, CultureInfo targetCulture)
        {
            if (sourceCulture == null)
            {
                throw new ArgumentNullException(paramName: nameof(sourceCulture));
            }

            if (targetCulture == null)
            {
                throw new ArgumentNullException(paramName: nameof(targetCulture));
            }

            return new NumberTranslator(sourceCulture, targetCulture);
        }

        /// <summary>
        /// Translate from source to target culture.
        /// </summary>
        /// <param name="input">input string to translate</param>
        /// <returns>translated string</returns>
        public string Translate(string input)
        {
            return Translate(input, sourceCulture, targetCulture, splitRegexForSource);
        }

        /// <summary>
        /// Translate from target to source culture.
        /// </summary>
        /// <param name="input">input string to translate back to source culture</param>
        /// <returns>source culture string</returns>
        public string TranslateBack(string input)
        {
            return Translate(input, targetCulture, sourceCulture, splitRegexForTarget);
        }

        private static string Translate(string input, CultureInfo cultureFrom, CultureInfo cultureTo, Regex splitRegex)
        {
            var outputBuilder = new StringBuilder();

            string[] tokens = splitRegex.Split(input);
            foreach (string token in tokens)
            {
                decimal number;
                outputBuilder.Append(
                    decimal.TryParse(token, NumberStyles.Number, cultureFrom, out number)
                    ? number.ToString(cultureTo)
                    : token);
            }

            return outputBuilder.ToString();
        }

        private static Regex GetSplitRegex(CultureInfo culture)
        {
            // HACK: Specifically adding the decimal point here since some people expect that to work everywhere.
            // This allows avoiding some unexpected errors users are getting when . is not part of the number representation.
            // Users were getting errors where leading zeros were being removed from the decimal part of numbers like 56.0002.
            // 56.0002 would be transformed into 56.2 due to it being translated as two different numbers and this would be accepted into the engine.
            // Now, even if . is not part of the culture representation, users won't hit this error since the number will
            // be passed as is to the calculator engine.
            // This shouldn't add any regressions into accepted strings while it will have a behavior the users expect.
            var splitPattern = $"((?:\\d|[a-fA-F]|\\.|{Regex.Escape(culture.NumberFormat.NumberDecimalSeparator)}";
            if (!string.IsNullOrEmpty(culture.NumberFormat.NumberGroupSeparator))
            {
                splitPattern += $"|{Regex.Escape(culture.NumberFormat.NumberGroupSeparator)}";
            }

            splitPattern += ")+)";
            return new Regex(splitPattern);
        }
    }
}
