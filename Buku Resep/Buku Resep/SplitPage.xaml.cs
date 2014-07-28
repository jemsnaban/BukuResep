using Buku_Resep.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Printing;
using Windows.UI.Xaml.Printing;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;
//using Windows.UI.Xaml.Navigation;
//using Windows.UI.Xaml.Printing;
using System.Text.RegularExpressions;
//using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace Buku_Resep
{
    class InvalidPageException : Exception
    {
        public InvalidPageException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for the
    /// currently selected item.
    /// </summary>
    public sealed partial class SplitPage : Buku_Resep.Common.LayoutAwarePage
    {

        private bool pageRangeEditVisible = false;

        /// <summary>
        /// The pages in the range
        /// </summary>
        private List<int> pageList;

        /// <summary>
        /// Flag used to determine if content selection mode is on
        /// </summary>
        private bool selectionMode;

        /// <summary>
        /// This is the original number of pages before processing(filtering) in ScenarioInput5_pagesCreated
        /// </summary>
        private int totalPages;

        public SplitPage()
        {
            this.InitializeComponent();

            pageList = new List<int>();

            pagesCreated += ScenarioInput_pagesCreated;
        }

        /// <summary>
        /// Filter pages that are not in the given range
        /// </summary>
        /// <param name="sender">The list of preview pages</param>
        /// <param name="e"></param>
        /// <note> Handling preview for page range
        /// Developers have the control over how the preview should look when the user specifies a valid page range.
        /// There are three common ways to handle this:
        /// 1) Preview remains unaffected by the page range and all the pages are shown independent of the specified page range.
        /// 2) Preview is changed and only the pages specified in the range are shown to the user.
        /// 3) Preview is changed, showing all the pages and graying out the pages not in page range.
        /// We chose option (2) for this sample, developers can choose their preview option.
        /// </note>
        void ScenarioInput_pagesCreated(object sender, EventArgs e)
        {
            totalPages = printPreviewPages.Count;

            if (pageRangeEditVisible)
            {
                // ignore page range if there are any invalid pages regarding current context
                if (!pageList.Exists(page => page > printPreviewPages.Count))
                {
                    for (int i = printPreviewPages.Count; i > 0 && pageList.Count > 0; --i)
                    {
                        if (this.pageList.Contains(i) == false)
                            printPreviewPages.RemoveAt(i - 1);
                    }
                }
            }
        }  

        #region Page state management

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var group = SampleDataSource.GetGroup((String)navigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;

            if (pageState == null)
            {
                this.itemListView.SelectedItem = null;
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (pageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    var selectedItem = SampleDataSource.GetItem((String)pageState["SelectedItem"]);
                    this.itemsViewSource.View.MoveCurrentTo(selectedItem);
                }
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = (SampleDataItem)this.itemsViewSource.View.CurrentItem;
                if (selectedItem != null) pageState["SelectedItem"] = selectedItem.UniqueId;
            }
        }

        #endregion

        #region Logical page navigation

        // Visual state management typically reflects the four application view states directly
        // (full screen landscape and portrait plus snapped and filled views.)  The split page is
        // designed so that the snapped and portrait view states each have two distinct sub-states:
        // either the item list or the details are displayed, but not both at the same time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed, or null
        /// for the current view state.  This parameter is optional with null as the default
        /// value.</param>
        /// <returns>True when the view state in question is portrait or snapped, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is Snapped)
        /// displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoked when the page's back button is pressed.
        /// </summary>
        /// <param name="sender">The back button instance.</param>
        /// <param name="e">Event data that describes how the back button was clicked.</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return
                // to the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // When logical page navigation is not in effect, or when there is no selected
                // item, use the default back button behavior.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed.</param>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Determine visual states for landscape layouts based not on the view state, but
            // on the width of the window.  This page has one layout that is appropriate for
            // 1366 virtual pixels or wider, and another for narrower displays or when a snapped
            // application reduces the horizontal space available to less than 1366.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscapeOrWide";
                return "FilledOrNarrow";
            }

            // When in portrait or snapped start with the default visual state name, then add a
            // suffix when viewing details instead of the list
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        #endregion

        private void btnTopApp_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                this.Frame.Navigate(typeof(ItemsPage), "AllGroups");
            }
            catch (Exception) { }
            
        }

        private async void btnTopApp2_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            printPreviewPages = new List<UIElement>();
            try
            {
                await Windows.Graphics.Printing.PrintManager.ShowPrintUIAsync();
            }
            catch(Exception)
            {
            }
        }

        private void btnTopApp3_Tapped_1(object sender, TappedRoutedEventArgs e)
        {

        }

        //-------------------------------------------------------------------------//


        protected override void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;
            printTask = e.Request.CreatePrintTask("Buku Resep",
                                                  sourceRequestedArgs =>
                                                  {
                                                      IList<string> displayedOptions = printTask.Options.DisplayedOptions;

                                                      // Choose the printer options to be shown.
                                                      // The order in which the options are appended determines the order in which they appear in the UI
                                                      displayedOptions.Clear();
                                                      //displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Copies);
                                                      //displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Orientation);
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.MediaSize);
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Collation);
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Duplex);

                                                      // Preset the default value of the printer option
                                                      printTask.Options.MediaSize = PrintMediaSize.NorthAmericaLegal;

                                                      PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
                                                      //IList<string> displayedOptions = printDetailedOptions.DisplayedOptions;

                                                      // Choose the printer options to be shown.
                                                      // The order in which the options are appended determines the order in which they appear in the UI
                                                      //displayedOptions.Clear();

                                                      //-------------scene5--
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Copies);
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.Orientation);
                                                      displayedOptions.Add(Windows.Graphics.Printing.StandardPrintTaskOptions.ColorMode);

                                                      // Create a new list option

                                                      PrintCustomItemListOptionDetails pageFormat = printDetailedOptions.CreateItemListOption("PageRange", "Page Range");
                                                      pageFormat.AddItem("PrintAll", "Print all");
                                                      pageFormat.AddItem("PrintSelection", "Print Selection");
                                                      pageFormat.AddItem("PrintRange", "Print Range");

                                                      // Add the custom option to the option list
                                                      displayedOptions.Add("PageRange");

                                                      // Create new edit option
                                                      PrintCustomTextOptionDetails pageRangeEdit = printDetailedOptions.CreateTextOption("PageRangeEdit", "Range");

                                                      // Register the handler for the option change event
                                                      printDetailedOptions.OptionChanged += printDetailedOptions_OptionChanged;


                                                      //-----------scene3---
                                                      // Print Task event handler is invoked when the print job is completed.

                                                      printTask.Completed += async (s, args) =>
                                                      {
                                                          pageRangeEditVisible = false;
                                                          selectionMode = false;
                                                          pageList.Clear();

                                                          // Notify the user when the print operation fails.
                                                          if (args.Completion == PrintTaskCompletion.Failed)
                                                          {
                                                              await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                                              {
                                                                  rootPage.NotifyUser("Failed to print.", NotifyType.ErrorMessage);
                                                              });
                                                          }

                                                          ///scene5
                                                          await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                                             () =>
                                                             {
                                                                 // Restore first page to its default layout
                                                                 // Undo any changes made by a text selection
                                                                 ShowContent(null);
                                                             });
                                                      };

                                                      sourceRequestedArgs.SetSource(printDocumentSource);
                                                  });
        }


        async void printDetailedOptions_OptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        {
            if (args.OptionId == null)
                return;

            string optionId = args.OptionId.ToString();

            // Handle change in Page Range Option

            if (optionId == "PageRange")
            {
                IPrintOptionDetails pageRange = sender.Options[optionId];
                string pageRangeValue = pageRange.Value.ToString();

                selectionMode = false;

                switch (pageRangeValue)
                {
                    case "PrintRange":
                        // Add PageRangeEdit custom option to the option list
                        sender.DisplayedOptions.Add("PageRangeEdit");
                        pageRangeEditVisible = true;
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                                  () =>
                                                  {
                                                      ShowContent(null);
                                                  });
                        break;
                    case "PrintSelection":
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                              () =>
                                              {
                                                  try
                                                  {
                                                      ScenarioOutput2 outputContent = (ScenarioOutput2)rootPage.FindName("itemDetailGrid");
                                                      ShowContent(outputContent.SelectedText);
                                                  }
                                                  catch (Exception) { }
                                              });
                            RemovePageRangeEdit(sender);
                        }
                        break;
                    default:
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                                  () =>
                                                  {
                                                      ShowContent(null);
                                                  });
                        RemovePageRangeEdit(sender);
                        break;
                }

                Refresh();

            }
            else if (optionId == "PageRangeEdit")
            {
                IPrintOptionDetails pageRange = sender.Options[optionId];
                // Expected range format (p1,p2...)*, (p3-p9)* ...
                if (!Regex.IsMatch(pageRange.Value.ToString(), @"^\s*\d+\s*(\-\s*\d+\s*)?(\,\s*\d+\s*(\-\s*\d+\s*)?)*$"))
                {
                    pageRange.ErrorText = "Invalid Page Range (eg: 1-3, 5)";
                }
                else
                {
                    pageRange.ErrorText = string.Empty;
                    try
                    {
                        GetPagesInRange(pageRange.Value.ToString());
                        Refresh();
                    }
                    catch (InvalidPageException ipex)
                    {
                        pageRange.ErrorText = ipex.Message;
                    }
                }
            }
        }

        private void ShowContent(string selectionText)
        {
            bool hasSelection = !string.IsNullOrEmpty(selectionText);
            selectionMode = hasSelection;

            // Hide/show images depending by the selected text
            StackPanel header = (StackPanel)firstPage.FindName("header");
            try
            {
                header.Visibility = hasSelection ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            }
            catch (Exception) { }
            Grid pageContent = (Grid)firstPage.FindName("printableArea");
            try
            {
                pageContent.RowDefinitions[0].Height = GridLength.Auto;
            }
            catch (Exception) { }

            Image scenarioImage = (Image)firstPage.FindName("scenarioImage");
            scenarioImage.Visibility = hasSelection ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;

            // Expand the middle paragraph on the full page if printing only selected text
            RichTextBlockOverflow firstLink = (RichTextBlockOverflow)firstPage.FindName("firstLinkedContainer");
            firstLink.SetValue(Grid.ColumnSpanProperty, hasSelection ? 2 : 1);

            // Clear(hide) current text and add only the selection if a selection exists
            RichTextBlock mainText = (RichTextBlock)firstPage.FindName("textContent");

            RichTextBlock textSelection = (RichTextBlock)firstPage.FindName("textSelection");

            // Main (default) scenario text
            mainText.Visibility = hasSelection ? Windows.UI.Xaml.Visibility.Collapsed : Windows.UI.Xaml.Visibility.Visible;
            mainText.OverflowContentTarget = hasSelection ? null : firstLink;

            // Scenario text-blocks used for displaying selection
            try
            {
                textSelection.OverflowContentTarget = hasSelection ? firstLink : null;
                textSelection.Visibility = hasSelection ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
                textSelection.Blocks.Clear();
            }
            catch (Exception) { }

            // Force the visual root to go through layout so that the linked containers correctly distribute the content inside them.
            PrintingRoot.InvalidateArrange();
            PrintingRoot.InvalidateMeasure();
            PrintingRoot.UpdateLayout();

            // Add the text selection if any
            if (hasSelection)
            {
                Run inlineText = new Run();
                inlineText.Text = selectionText;

                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(inlineText);

                textSelection.Blocks.Add(paragraph);
            }
        }

        protected override RichTextBlockOverflow AddOnePrintPreviewPage(RichTextBlockOverflow lastRTBOAdded, PrintPageDescription printPageDescription)
        {
            RichTextBlockOverflow textLink = base.AddOnePrintPreviewPage(lastRTBOAdded, printPageDescription);

            // Don't show footer in selection mode
            if (selectionMode)
            {
                FrameworkElement page = (FrameworkElement)printPreviewPages[printPreviewPages.Count - 1];
                StackPanel footer = (StackPanel)page.FindName("footer");
                footer.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            return textLink;
        }

        /// <summary>
        /// Removes the PageRange edit from the charm window
        /// </summary>
        /// <param name="printTaskOptionDetails">Details regarding PrintTaskOptions</param>
        private void RemovePageRangeEdit(PrintTaskOptionDetails printTaskOptionDetails)
        {
            if (pageRangeEditVisible)
            {
                string lastDisplayedOption = printTaskOptionDetails.DisplayedOptions.FirstOrDefault(p => p.Contains("PageRangeEdit"));
                if (!string.IsNullOrEmpty(lastDisplayedOption))
                {
                    printTaskOptionDetails.DisplayedOptions.Remove(lastDisplayedOption);
                }
                pageRangeEditVisible = false;
            }
        }

        private async void Refresh()
        {
            // Refresh
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                    () =>
                                    {
                                        // Refresh preview
                                        printDocument.InvalidatePreview();
                                    });
        }

        private static readonly char[] enumerationSeparator = new char[] { ',' };
        private static readonly char[] rangeSeparator = new char[] { '-' };

        /// <summary>
        /// This is where we parse the range field
        /// </summary>
        /// <param name="pageRange">the page range value</param>
        private void GetPagesInRange(string pageRange)
        {
            string[] rangeSplit = pageRange.Split(enumerationSeparator);

            // Clear the previous values
            pageList.Clear();

            foreach (string range in rangeSplit)
            {
                // Interval
                if (range.Contains("-"))
                {
                    string[] limits = range.Split(rangeSeparator);
                    int start = int.Parse(limits[0]);
                    int end = int.Parse(limits[1]);

                    if ((start < 1) || (end > totalPages) || (start >= end))
                    {
                        throw new InvalidPageException(string.Format("Invalid page(s) in range {0} - {1}", start, end));
                    }

                    for (int i = start; i <= end; ++i)
                    {
                        pageList.Add(i);
                    }
                    continue;
                }

                // Single page

                var pageNr = int.Parse(range);

                if (pageNr < 1)
                {
                    throw new InvalidPageException(string.Format("Invalid page {0}", pageNr));
                }

                // compare to the maximum number of available pages
                if (pageNr > totalPages)
                {
                    throw new InvalidPageException(string.Format("Invalid page {0}", pageNr));
                }

                pageList.Add(pageNr);
            }
        }


        public void NotifyUser(string strMessage, NotifyType type)
        {
            /* switch (type)
             {
                 case NotifyType.StatusMessage:
                     StatusBlock.Style = Resources["StatusStyle"] as Style;
                     break;
                 case NotifyType.ErrorMessage:
                     StatusBlock.Style = Resources["ErrorStyle"] as Style;
                     break;
             }
             //StatusBlock.Text = strMessage;*/
        }

        protected override void PreparePrintContent()
        {
            if (firstPage == null)
            {
                firstPage = new ScenarioOutput2();
                StackPanel header = (StackPanel)firstPage.FindName("itemDetailTitlePanel");
                try
                {
                    header.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                catch (Exception) { }
            }

            // Add the (newley created) page to the printing root which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            try
            {
                PrintingRoot.Children.Add(firstPage);
                PrintingRoot.InvalidateMeasure();
                PrintingRoot.UpdateLayout();
            }
            catch (Exception) { }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Tell the user how to print
            try
            {
                rootPage.NotifyUser("Print contract registered with customization, use the Charms Bar to print.", NotifyType.StatusMessage);
            }
            catch (Exception) { }
        }
    }

    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };
}
