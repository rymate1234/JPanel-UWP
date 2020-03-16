using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
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
    public sealed partial class FilesPage : Page
    {
        private HttpCookie sessionCookie;
        private HttpClient client;
        private string currentDir = "";
        private string currentFile = "";

        public ObservableCollection<File> FilesObservableCollection { get; set; }


        private Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        public FilesPage()
        {
            this.InitializeComponent();

            FilesObservableCollection = new ObservableCollection<File>();
            Loaded += FilesPage_Loaded;

        }

        private async void FilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            filesGrid.ItemsSource = FilesObservableCollection;
            if (await AttemptLogin())
            {
                LoadFiles();
            }
        }

        private async void LoadFiles()
        {                                                                                                            
            HttpResponseMessage result = await client.GetAsync(new Uri(roamingSettings.Values["panelurl"] + "/file/" + currentDir));
            if (result.IsSuccessStatusCode)
            {
                var filesJson = JsonObject.Parse(await result.Content.ReadAsStringAsync());
                var folders = filesJson.GetNamedArray("folders");
                var files = filesJson.GetNamedArray("files");

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.FilesObservableCollection.Clear();
                    if (currentDir != "")
                    {
                        File file = new File();
                        file.filename = "..";
                        file.isFolder = true;
                        file.icon = new Uri("ms-appx:///Assets/folder-icon.png");
                        FilesObservableCollection.Add(file);
                    }
                    for (int index = 0; index < folders.Count; index++)
                    {
                        string folder = folders[index].ToString();
                        File file = new File();
                        file.filename = folder.Replace("\"", "");
                        file.isFolder = true;
                        file.icon = new Uri("ms-appx:///Assets/folder-icon.png");
                        FilesObservableCollection.Add(file);
                    }

                    for (int index = 0; index < files.Count; index++)
                    {
                        string fileStr = files[index].ToString();
                        File file = new File();
                        file.filename = fileStr.Replace("\"", "");
                        file.isFolder = false;
                        file.icon = new Uri("ms-appx:///Assets/file-icon.png");
                        FilesObservableCollection.Add(file);
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

        public class File
        {
            public Uri icon { get; set; }
            public String filename { get; set; }
            public bool isFolder { get; set; }
        }

        private void FilesGrid_ItemClicked(object sender, ItemClickEventArgs e)
        {
            var fileClicked = e.ClickedItem as File;
            if (fileClicked.isFolder)
            {
                if (fileClicked.filename == "..")
                {
                    var folders = currentDir.Split(Convert.ToChar("/"));
                    folders = folders.Take(folders.Count() - 1).ToArray();
                    currentDir = String.Join("/", folders);
                }
                else
                {
                    currentDir += "/" + fileClicked.filename;
                }
                LoadFiles();
            }
            else
            {
                currentFile = currentDir + "/" + fileClicked.filename;
                var appFrame = new Frame();
                var emptyNavState = appFrame.GetNavigationState();
                FileViewFrame.SetNavigationState(emptyNavState);
                FileViewFrame.Navigate(typeof (FileView), currentFile);
            }
        }

        private void AdaptiveStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {

        }
    }
}
