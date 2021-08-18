// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HighlightSubstringConverter.cs" company="Advanced Health & Care">
//   Copyright © Advanced Health & Care 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Odyssey.Session.Client.UI.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;

    /// <summary>
    /// Convert two strings to a text block where instances of the second string within the first (ignoring case) are highlighted (bold)
    /// </summary>
    public class HighlightSubstringConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        /// <summary>
        /// Convert two strings to a text block where instances of the second string within the first (ignoring case) are highlighted (bold)
        /// </summary>
        /// <param name="values">Two strings, the second potentially a substring of the first</param>
        /// <param name="targetType">Ignored target type</param>
        /// <param name="parameter">If specified and of type Style, a style for the TextBlock</param>
        /// <param name="culture">Ignored culture</param>
        /// <returns>Textblock in which instances of the substring are in bold</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string main = values[0] as string;
            string substring = (values[1] as string ?? string.Empty).ToLower();
            if (main == null)
            {
                return null;
            }
            
            var textBlock = new TextBlock();
            var style = parameter as Style;
            if (style != null)
            {
                textBlock.Style = style;
            }

            string rest = main;
            if (substring != string.Empty)
            {
                int index;
                while ((index = rest.ToLower().IndexOf(substring)) >= 0)
                {                    
                    string plainPart = rest.Substring(0, index);
                    string boldPart = rest.Substring(index, substring.Length);
                    this.AddNonEmptyRun(textBlock, plainPart);
                    this.AddNonEmptyRun(textBlock, boldPart, true);
                    rest = rest.Substring(index + substring.Length);
                }
            }

            this.AddNonEmptyRun(textBlock, rest);
            return textBlock;
        }

        /// <summary>
        /// Convert back is not implemented - Converter not indended for two-way binding
        /// </summary>
        /// <param name="value">An object to convert</param>
        /// <param name="targetTypes">The types of the binding target properties</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>Throws an exception</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Add a non-empty string to a text block as a run
        /// </summary>
        /// <param name="text">Text to add</param>
        /// <param name="bold">Whether it should be bold</param>
        private void AddNonEmptyRun(TextBlock textBlock, string text, bool bold = false)
        {
            if (text != string.Empty)
            {
                textBlock.Inlines.Add(new Run { Text = text, FontWeight = bold ? FontWeights.ExtraBold : FontWeights.Normal });
            }
        }
    }
}
