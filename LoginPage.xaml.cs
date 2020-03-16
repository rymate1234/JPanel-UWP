using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace JPanel_W10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private HttpClient httpClient;
        private Uri panelUrl;
        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        public LoginPage()
        {
            this.InitializeComponent();

            httpClient = new HttpClient();

            Loaded += (s, e) =>
            {
                if (roamingSettings.Values.ContainsKey("darktheme"))
                {
                    Root.RequestedTheme =
                        (bool) roamingSettings.Values["darktheme"] ? ElementTheme.Dark : ElementTheme.Light;
                }

                if (roamingSettings.Values.ContainsKey("hasloggedin"))
                {
                    loginUrl.Text = (string)roamingSettings.Values["panelurl"];
                    loginUser.Text = (string)roamingSettings.Values["username"];
                    loginPass.Password = (string)roamingSettings.Values["password"];
                    sslSwitch.IsOn = (bool)roamingSettings.Values["ssl"];


                    checkUrl();

                }


                //Show UI back button - do it on each page navigation
                if (Frame.CanGoBack)
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                else
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            };
        }

        private void loginUrl_CheckUrl(object sender, RoutedEventArgs e)
        {
            checkUrl();
        }

        private void sslSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sslSwitch.IsOn)
            {
                if (loginUrl.Text.Contains("http"))
                {
                    loginUrl.Text = loginUrl.Text.Replace("http", "https");
                    checkUrl();
                }
            }
            else
            {
                if (loginUrl.Text.Contains("https"))
                {
                    loginUrl.Text = loginUrl.Text.Replace("https", "http");
                    checkUrl();
                }
            }
        }

        private async void checkUrl()
        {
            Uri uri;

            string url;

            if (!loginUrl.Text.Contains("http"))
            {
                loginUrl.Text = "http://" + loginUrl.Text;
            }

            if (!TryGetUri(loginUrl.Text, out uri))
            {
                ShowToast(ContentRoot, "Incorrect URL");
                loginButton.IsEnabled = false;
                return;
            }

            try
            {
                var result = await httpClient.GetAsync(uri);
                if (result.IsSuccessStatusCode)
                {
                    ShowToast(ContentRoot, "Connection Success!");
                    loginButton.IsEnabled = true;
                    panelUrl = uri;
                }
            }
            catch (Exception ex)
            {
                ShowToast(ContentRoot, "Connection Failed!");
                loginButton.IsEnabled = false;
            }
        }

        internal static bool TryGetUri(string uriString, out Uri uri)
        {
            if (!Uri.TryCreate(uriString.Trim(), UriKind.Absolute, out uri))
            {
                return false;
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return false;
            }

            return true;
        }

        public void ShowToast(Grid layoutRoot, string message)
        {
            Grid grid = new Grid();
            grid.Width = 300;
            grid.Height = 60;
            if (roamingSettings.Values.ContainsKey("darktheme"))
                grid.Background = new SolidColorBrush((bool)roamingSettings.Values["darktheme"] ? makeColor("#333333") : makeColor("#D5555A"));
            else
                grid.Background = new SolidColorBrush(makeColor("#D5555A"));

            grid.HorizontalAlignment = HorizontalAlignment.Center;
            grid.VerticalAlignment = VerticalAlignment.Bottom;
            grid.Margin = new Thickness(0, 0, 0, 30);


            TextBlock text = new TextBlock();
            text.Text = message;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.FontSize = 22;

            grid.Children.Add(text);

            layoutRoot.Children.Add(grid);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Tick += (sender, args) =>
            {
                layoutRoot.Children.Remove(grid);
                timer.Stop();
            };
            timer.Start();
        }

        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;
            HttpClient loginClient = new HttpClient(filter);

            Uri loginUri = new Uri(panelUrl + "/auth");
            Dictionary<string, string> pairs = new Dictionary<string, string>();

            pairs.Add("username", loginUser.Text);
            pairs.Add("password", loginPass.Password);

            HttpFormUrlEncodedContent formContent = new HttpFormUrlEncodedContent(pairs);
            try
            {
                HttpResponseMessage response = await loginClient.PostAsync(loginUri, formContent);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    if (content.Contains("SUCCESS"))
                    {
                        roamingSettings.Values["username"] = loginUser.Text;
                        roamingSettings.Values["password"] = loginPass.Password;
                        roamingSettings.Values["panelurl"] = panelUrl.ToString();
                        roamingSettings.Values["domain"] = panelUrl.Host;
                        roamingSettings.Values["ssl"] = sslSwitch.IsOn;
                        roamingSettings.Values["hasloggedin"] = true;

                        var dialog = new MessageDialog("Logged into the panel!");
                        await dialog.ShowAsync();

                        if (Frame.CanGoBack)
                            Frame.GoBack();
                    }
                    else if (content.Contains("FAIL"))
                    {
                        var dialog = new MessageDialog(content);
                        await dialog.ShowAsync();

                    }
                    else
                    {
                        var dialog = new MessageDialog("We couldn't find a panel to log into");
                        await dialog.ShowAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                ShowToast(ContentRoot, "Unknown error when logging in.");
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        public Color makeColor(String text)
        {
            var color = new Color();
            color.R = byte.Parse(text.Substring(1, 2), NumberStyles.AllowHexSpecifier);
            color.G = byte.Parse(text.Substring(3, 2), NumberStyles.AllowHexSpecifier);
            color.B = byte.Parse(text.Substring(5, 2), NumberStyles.AllowHexSpecifier);
            return color;
        }
    }
}
