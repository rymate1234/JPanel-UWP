using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    // todo: implement code formatting (low priority)
    // regexes so far:
    // - match number or boolean with (true|false|\b\d+\b|[+-]?\d*\.\d+)(?![-+0-9\\.])
    public sealed partial class FileView : Page
    {
        private HttpCookie sessionCookie;
        private HttpClient client;
        private string currentFile = "";

        Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;


        public FileView()
        {
            this.InitializeComponent();

            Loaded += FileView_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            currentFile = (string) e.Parameter;

        }

        private async void FileView_Loaded(object sender, RoutedEventArgs e)
        {
            if (await AttemptLogin())
            {
                LoadFile();
            }
        }

        private async void LoadFile()
        {
            HttpResponseMessage result = await client.GetAsync(new Uri(roamingSettings.Values["panelurl"] + "/file/" + currentFile));
            if (result.IsSuccessStatusCode)
            {
                var fileText = await result.Content.ReadAsStringAsync();

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    FileEditor.Text = fileText;
                });
            }
        }

        private async void SaveFile(object sender, RoutedEventArgs e)
        {
            HttpStringContent stringContent = new HttpStringContent(FileEditor.Text);
            HttpResponseMessage result = await client.PostAsync(new Uri(roamingSettings.Values["panelurl"] + "/file/" + currentFile), stringContent);
            if (result.IsSuccessStatusCode)
            {
                var res = await result.Content.ReadAsStringAsync();

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (res == "0")
                    {
                        var dialog = new MessageDialog("You cannot edit files!");
                        await dialog.ShowAsync();
                    }
                    else
                    {
                        var dialog = new MessageDialog("File saved!");
                        await dialog.ShowAsync();
                    }
                });
            }
        }


        private async Task<bool> AttemptLogin()
        {
            var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior =
                Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
            var cookieManager = filter.CookieManager;
            client = new HttpClient(filter);

            Uri loginUri = new Uri(roamingSettings.Values["panelurl"] + "/auth");
            Dictionary<string, string> pairs = new Dictionary<string, string>();

            pairs.Add("username", (string)roamingSettings.Values["username"]);

            pairs.Add("password", (string)roamingSettings.Values["password"]);

            HttpFormUrlEncodedContent formContent = new HttpFormUrlEncodedContent(pairs);
            HttpResponseMessage response = await client.PostAsync(loginUri, formContent);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();

                var responseCookies = cookieManager.GetCookies(loginUri);

                foreach (HttpCookie cookie in responseCookies)
                {
                    if (cookie.Name == "loggedin")
                    {
                        this.sessionCookie = cookie;
                        return true;
                    }
                }
            }
            return false;
        }

        
    }
}
