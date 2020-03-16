using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace JPanel_W10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;


        public MainPage()
        {
            this.InitializeComponent();

            Loaded += (sender, args) =>
            {
                if (roamingSettings.Values.ContainsKey("darktheme"))
                {
                    Root.RequestedTheme =
                        (bool) roamingSettings.Values["darktheme"] ? ElementTheme.Dark : ElementTheme.Light;
                    themeToggle.IsOn = (bool) roamingSettings.Values["darktheme"];

                    Color titlebarHex = themeToggle.IsOn ? makeColor("#333333") : makeColor("#D5555A");

                    var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                    var titleBarInst = v.TitleBar;
                    titleBarInst.BackgroundColor = titlebarHex;
                    titleBarInst.ForegroundColor = Colors.White;
                    titleBarInst.ButtonBackgroundColor = titlebarHex;
                    titleBarInst.ButtonForegroundColor = Colors.White;
                }
                else
                {
                    Root.RequestedTheme = ElementTheme.Light;
                    themeToggle.IsOn = false;

                    Color titlebarHex = makeColor("#D5555A");

                    var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                    var titleBarInst = v.TitleBar;
                    titleBarInst.BackgroundColor = titlebarHex;
                    titleBarInst.ForegroundColor = Colors.White;
                    titleBarInst.ButtonBackgroundColor = titlebarHex;
                    titleBarInst.ButtonForegroundColor = Colors.White;
                }

                if (roamingSettings.Values.ContainsKey("hasloggedin"))
                {
                    ConsoleFrame.Navigate(typeof(ConsolePage));
                    StatsFrame.Navigate(typeof(StatsPage));
                    FilesFrame.Navigate(typeof(FilesPage));

                }
                else
                {
                    Frame.Navigate(typeof(LoginPage));

                }


                //Show UI back button - do it on each page navigation
                if (Frame.CanGoBack)
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                else
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            };
            


        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
        }

        private void ThemeToggled(object sender, RoutedEventArgs e)
        {
            Color titlebarHex = themeToggle.IsOn ? makeColor("#333333") : makeColor("#D5555A");

            var v = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            var titleBarInst = v.TitleBar;
            titleBarInst.BackgroundColor = titlebarHex;
            titleBarInst.ForegroundColor = Colors.White;
            titleBarInst.ButtonBackgroundColor = titlebarHex;
            titleBarInst.ButtonForegroundColor = Colors.White;
        
            roamingSettings.Values["darktheme"] = themeToggle.IsOn;
                Root.RequestedTheme =
                        (bool)roamingSettings.Values["darktheme"] ? ElementTheme.Dark : ElementTheme.Light;

        }

        public Color makeColor(String text)
        {
            var color = new Color();
            color.R = byte.Parse(text.Substring(1, 2), NumberStyles.AllowHexSpecifier);
            color.G = byte.Parse(text.Substring(3, 2), NumberStyles.AllowHexSpecifier);
            color.B = byte.Parse(text.Substring(5, 2), NumberStyles.AllowHexSpecifier);
            return color;
        }

        private void JPanelPivot_TabChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (PivotItem)(sender as Pivot).SelectedItem;
            var tc = new TelemetryClient(); // Call once per thread
            switch (pivot.Header.ToString())
            {
                case "Console":
                    tc.TrackEvent("Console Loaded");
                    break;
                case "Files":
                    tc.TrackEvent("Files Loaded");
                    break;
            }
        }

        private void StatsButton_Clicked(object sender, RoutedEventArgs e)
        {
            var tc = new TelemetryClient(); // Call once per thread
            tc.TrackEvent("Stats Loaded");
        }
    }
}
