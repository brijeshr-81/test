// <copyright file="WatermarkedSearchTextBox.cs" company="Advanced Health & Care">
//   Copyright © Advanced Health & Care 2017
// </copyright>
namespace Odyssey.Demographics.Client.UI
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Custom Search box for Presenting complaint with search button (and clear search)
    /// </summary>
    public class WatermarkedSearchTextBox : TextBox
    {
        /// <summary>
        /// Dependency property for the AutoSearch enabling
        /// </summary>
        private static readonly DependencyProperty AutoSearchProperty =
            DependencyProperty.Register("AutoSearch", typeof(bool), typeof(WatermarkedSearchTextBox), new PropertyMetadata());

        /// <summary>
        /// Dependency property for the ClearText enabling
        /// </summary>
        private static readonly DependencyProperty IsClearTextProperty =
            DependencyProperty.Register("IsClearText", typeof(bool), typeof(WatermarkedSearchTextBox), new PropertyMetadata());

        /// <summary>
        /// Dependency property for the watermark
        /// </summary>
        private static readonly DependencyProperty WatermarkedSearchText =
            DependencyProperty.Register("Watermark", typeof(string), typeof(WatermarkedSearchTextBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Dependency Property for Search command
        /// </summary>
        private static DependencyProperty searchCommandProperty =
            DependencyProperty.Register("SearchCommand", typeof(ICommand), typeof(WatermarkedSearchTextBox));

        /// <summary>
        /// Dependency Property for Search count
        /// </summary>
        private static DependencyProperty searchCountProperty =
            DependencyProperty.Register("SearchCount", typeof(int), typeof(WatermarkedSearchTextBox), new PropertyMetadata(0));

        /// <summary>
        /// Initializes static members of the WatermarkedSearchTextBox class
        /// </summary>
        static WatermarkedSearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkedSearchTextBox), new FrameworkPropertyMetadata(typeof(WatermarkedSearchTextBox)));
        }

        /// <summary>
        /// Gets or sets a value indicating whether AutoSearch 
        /// </summary>
        public bool AutoSearch
        {
            get { return (bool)GetValue(AutoSearchProperty); }
            set { this.SetValue(AutoSearchProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether watermark
        /// </summary>
        public string Watermark
        {
            get { return (string)GetValue(WatermarkedSearchText); }
            set { this.SetValue(WatermarkedSearchText, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether SearchCommand
        /// </summary>
        public ICommand SearchCommand
        {
            get { return (ICommand)GetValue(searchCommandProperty); }
            set { this.SetValue(searchCommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether SearchCount
        /// </summary>
        public int SearchCount
        {
            get { return (int)GetValue(searchCountProperty); }
            set { this.SetValue(searchCountProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether text cleared
        /// </summary>
        public bool IsClearText
        {
            get { return (bool)GetValue(IsClearTextProperty); }
            set { this.SetValue(IsClearTextProperty, value); }
        }

        /// <summary>
        /// Overrides the OnApplyTemplate 
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Image iconSearch = GetTemplateChild("PART_SearchIcon") as Image;
            if (iconSearch != null)
            {
                iconSearch.MouseDown += this.IconSearch_MouseDown;
            }
        }

        /// <summary>
        /// Overrides the OnTextChanged event
        /// </summary>
        /// <param name="e">Text changed event argument</param>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            this.AutoSearch = this.Text.Length > 2;
            if (this.AutoSearch)
            {
                this.SearchCommand.Execute(null);
            }
            else
            {
                if (this.SearchCount > 0 || this.IsClearText)
                {
                    this.SearchCommand.Execute(null);
                }
                else if (!this.IsClearText && string.IsNullOrEmpty(this.Text))
                {
                    this.SearchCommand.Execute(null);
                }
           }
        }

        /// <summary>
        /// Mouse Down event for the image IconSearch
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">Mouse Button event argument</param>
        private void IconSearch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.AutoSearch)
            {
                this.IsClearText = true;
                this.Text = string.Empty;
                this.IsClearText = false;
            }
        }
    }
}
