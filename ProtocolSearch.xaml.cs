//-----------------------------------------------------------------------
// <copyright file="ProtocolSearch.xaml.cs" company="Advanced Health & Care">
//     Copyright © Advanced Health & Care 2017
// </copyright>
//-----------------------------------------------------------------------

namespace Odyssey.Session.Client.UI
{
    using System.Windows.Controls;
    using System.Windows.Input;
    using ViewModels.Common;

    /// <summary>
    /// Interaction logic for ProtocolSearch
    /// </summary>
    public partial class ProtocolSearch : UserControl
    {
        /// <summary>
        /// Initialises a new instance of the ProtocolSearch class
        /// </summary>
        /// 
        public ProtocolSearch()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Double click on the item text block
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Mouse button event arguments</param>
        private void ItemTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                var context = this.DataContext as PresentingComplaintViewModelBase;
                if (context != null)
                {
                    context.ProcessDoubleClick();
                    e.Handled = true;
                }
            }
        }
    }
}
