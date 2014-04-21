﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Coding4Fun.Toolkit.Controls;
using CoolEditor.Class;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using CoolEditor.Resources;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace CoolEditor
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<File> _source;
        private ObservableCollection<AlphaKeyGroup<File>> _dataSource;
        private MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //handle first login in
            IsolatedStorageSettings setting = IsolatedStorageSettings.ApplicationSettings;
            if (setting.Contains("firstuse") && (string)setting["firstuse"] == "false")
            {
                //do nothing
            }
            else
            {
                //move sample code to folder
                LoadSampleFiles();
                //
                setting.Add("firstuse", "false");
                setting.Save();
            }

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
            ListFiles();
            // for WP8 users
            if (Environment.OSVersion.Version.Minor == 0) // WP8.0
            {
                Wp81.Visibility = Visibility.Collapsed;
            }
            else
            {
                Wp80.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //handle back navigation
            FileListSelector.SelectedItem = null;
            ListFiles();

            if (e.NavigationMode != NavigationMode.Back)
            {
                //trail version
                if ((App.Current as App).IsTrial)
                {
                    var trialMessageBox = new CustomMessageBox()
                    {
                        Caption = "Buy Cool Editor",
                        Message = "We really hope you can buy this app to support us to make it better.",
                        LeftButtonContent = "Buy",
                        RightButtonContent = "Continue trial"
                    };

                    trialMessageBox.Dismissed += (s, e1) =>
                    {
                        if (e1.Result == CustomMessageBoxResult.LeftButton)
                        {
                            _marketPlaceDetailTask.Show();
                        }
                    };

                    trialMessageBox.Show();
                }
            }
#if DEBUG
            PanoramaItemAbout.Header = "about";//specify when is debugging
#endif
        }

        private async void LoadSampleFiles()
        {
            var samples = new string[]
                {
                    "sample.php",
                    "sample.css",
                    "sample.java",
                    "sample.js",
                    "sample.cpp"
                };
            foreach (var sample in samples)
            {
                var uri = "CoolEditor;component/Assets/Sample_Code/" + sample;
                System.Windows.Resources.StreamResourceInfo strm = Application.GetResourceStream(new Uri(uri, UriKind.Relative));
                var reader = new System.IO.StreamReader(strm.Stream);
                string data = reader.ReadToEnd();
                await FileIOUtility.WriteDataToFileAsync(sample, data);
                await ListFiles();
            }
        }

        public async Task ListFiles()
        {
            var storageFiles = IsolatedStorageFile.GetUserStoreForApplication();
            var storageFolder = ApplicationData.Current.LocalFolder;
            var files = await storageFolder.GetFilesAsync();
            _source = new ObservableCollection<File>();
            foreach (var file in files)
            {
                var storageFile = file as StorageFile;
                if (storageFile != null)
                {
                    if (storageFile.Name == "__ApplicationSettings") continue;
                    DateTime dt = storageFiles.GetLastWriteTime(storageFile.Path).LocalDateTime;
                    _source.Add(new File(storageFile.Name, storageFile.Path, dt));
                }
            }
            _dataSource = new ObservableCollection<AlphaKeyGroup<File>>(
                AlphaKeyGroup<File>.CreateGroups(_source,
                System.Threading.Thread.CurrentThread.CurrentUICulture,
                (File s) => s.FileName, true));
            FileListSelector.ItemsSource = _dataSource;
            no_file.Visibility = !_source.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        public void OpenFile(string fileName)
        {
            NavigationService.Navigate(new Uri(string.Format("/Editor.xaml?name={0}", fileName), UriKind.Relative));
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // share
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (File)menuItem.DataContext;
                var shareBox = new ShareBox(file.FileName);
                shareBox.Show();
            }
        }

        private async void MenuItem2_OnClick(object sender, RoutedEventArgs e)
        {
            // delete
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (File)menuItem.DataContext;
                MessageBoxResult result =
                MessageBox.Show("Do you want to delete " + file.FileName + "?", "Warning",
                    MessageBoxButton.OKCancel);

                if (result != MessageBoxResult.OK)
                {
                    return;
                }
                if (await FileIOUtility.DeleteFileAsync(file.FileName))
                {
                    //_source.Remove(_source.FirstOrDefault(x => x.FileName == file.FileName));
                    //var group =
                    //    _dataSource.FirstOrDefault(x => x.FirstOrDefault(y => y.FileName == file.FileName) != null);
                    //if (@group != null)
                    //{
                    //    var target = @group.FirstOrDefault(y => y.FileName == file.FileName);
                    //    group.Remove(target);
                    //    FileListSelector.ItemsSource = _dataSource;
                    //}
                    await ListFiles();
                    ToastNotification.ShowSimple(file.FileName + " deleted.");
                    //ListFiles();
                }
                else
                {
                    ToastNotification.ShowSimple("Fail.");
                }
            }
            //var file = (File)FileListSelector.SelectedItem;
        }

        private void MenuItem3_OnClick(object sender, RoutedEventArgs e)
        {
            //rename
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var file = (File)menuItem.DataContext;
                
                var prompt = new InputPrompt {Title = "Rename file", Message = "Specify a new file name:", Value = file.FileName};
                prompt.Show();

                prompt.Completed += async (s1, e1) =>
                {
                    if (await FileIOUtility.RenameFileAsync(file.FileName, e1.Result))
                    {
                        ToastNotification.ShowSimple(file.FileName + " renamed to " + e1.Result);
                        await ListFiles();
                    }
                    else
                    {
                        ToastNotification.ShowSimple("Fail, please check if the name is used.");
                    }
                };
            }
        }

        private void FileListSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListSelector.SelectedItem == null)
            {
                return;
            }
            var file = (File) FileListSelector.SelectedItem;
            OpenFile(file.FileName);
        }

        private async void ApplicationBarIconButton_OnClick(object sender, EventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                "Do you want to delete all files?!", "Warning", MessageBoxButton.OKCancel);

            if (result != MessageBoxResult.OK)
            {
                return;
            }
            var storageFiles = IsolatedStorageFile.GetUserStoreForApplication();
            try
            {
                foreach (var fileName in storageFiles.GetFileNames())
                {
                    await FileIOUtility.DeleteFileAsync(fileName);
                }
                ToastNotification.ShowSimple("All files deleted.");
                await ListFiles();
            }
            catch (Exception ex)
            {
                ToastNotification.ShowSimple("Fail to delete.");
            }
            
        }

        private async void ApplicationBarIconButton1_OnClick(object sender, EventArgs e)
        {
            //create file
            var textbox = new TextBox();
            var newFileBox = new CustomMessageBox()
            {
                Caption = "Create a New File",
                Message = "Enter file name",
                LeftButtonContent = "create",
                RightButtonContent = "cancel",
                Content = textbox
            };

            newFileBox.Dismissed += async (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        string fileName = textbox.Text;
                        await FileIOUtility.WriteDataToFileAsync(fileName, "");
                        NavigationService.Navigate(new Uri(string.Format("/Editor.xaml?name={0}", fileName), UriKind.Relative));
                        break;
                    default:
                        break;
                }
            };
            newFileBox.Show();
        }

        private void ApplicationBarMenuItem_OnClick(object sender, EventArgs e)
        {
            //connect the feedback
            var email = new EmailComposeTask
            {
                To = "landxh@gmail.com", 
                Subject = "Cool Editor Feedback"
            };
            email.Show();
        }

        private void RichTextBox_ContentChanged(object sender, ContentChangedEventArgs e)
        {

        }

        private async void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            //download file by url
            if (e.Key != Key.Enter) return;
            //download file
            var phoneTextBox = sender as PhoneTextBox;
            if (phoneTextBox == null) return;
            var url = phoneTextBox.Text;
            var targetUri = new UriBuilder(url).Uri;
            try
            {
                var client = new WebClient();
                client.DownloadStringCompleted += async (s1, e1) =>
                {
                    var headers = ((WebClient) s1).ResponseHeaders;
                    string fileName = headers.AllKeys.Contains("Content-Disposition") ?  // if has the header, use the header's file name
                        headers["Content-Disposition"] : url.Split('/')[url.Split('/').Count() - 1];
                    var content = e1.Result;
                    fileName = await FileIOUtility.CreateFileAndWriteDataAsync(fileName, content); // write to local
                    phoneTextBox.Text = "";
                    SimpleProgressIndicator.Set(false);
                };
                SimpleProgressIndicator.Set(true);
                FileListSelector.Focus();
                client.DownloadStringAsync(targetUri);
            } 
            catch(Exception ex)
            {
                MessageBox.Show("Something wrong.");
            }

        }

    }
    public class File
    {
        public string FileName
        {
            get;
            set;
        }
        public string FilePath
        {
            get;
            set;
        }

        public DateTime LastWriteTime
        {
            get;
            set;
        }

        public File(string filename, string filepath, DateTime lastwritetime)
        {
            this.FileName = filename;
            this.FilePath = filepath;
            this.LastWriteTime = lastwritetime;
        }
    }
}