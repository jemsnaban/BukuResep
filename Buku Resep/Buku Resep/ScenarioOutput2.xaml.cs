﻿using System;
using System.Linq;
using System.Collections.Generic;
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

namespace Buku_Resep
{
    public sealed partial class ScenarioOutput2 : Page
    {
        // A pointer back to the main page which is used to gain access to the input and output frames and their content.
        SplitPage rootPage = null;

        public string SelectedText { get; set; }

        public ScenarioOutput2()
        {
            this.InitializeComponent();

            Loaded += new RoutedEventHandler(ScenarioOutput_Loaded);

            // Hook the Width and Resolution changed events.  This is only necessary if you need to modify your
            // content to fit well in the various view states and/or orientations.
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(ScenarioOutput_SizeChanged);

            textContent.SelectionChanged += textContent_SelectionChanged;
        }

        void textContent_SelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectedText = textContent.SelectedText;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get a pointer to our main page.
            rootPage = e.Parameter as SplitPage;
        }

        void ScenarioOutput_Loaded(object sender, RoutedEventArgs e)
        {
            CheckLayout();
        }

        // You may or may not need to handle resolution and view state changes in your specific scenario page content.
        // It will simply depend on your content.  In the case of this specific example, we need to adjust the content 
        // to fit well when the application is in portrait or when snapped.

        void CheckLayout()
        {
            String visualState = this.ActualWidth < 768 ? "Below768Layout" : "DefaultLayout";
            VisualStateManager.GoToState(this, visualState, false);
        }

        void ScenarioOutput_SizeChanged(Object sender, Windows.UI.Core.WindowSizeChangedEventArgs args)
        {
            CheckLayout();
        }
    }
}
