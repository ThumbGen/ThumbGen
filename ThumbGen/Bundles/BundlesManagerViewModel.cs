using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using FileExplorer.ViewModel;
using Microsoft.Win32;

namespace ThumbGen.Bundles
{
    public class BundlesManagerViewModel : BaseNotifyPropertyChanged
    {
        public ObservableCollection<Bundle> Bundles { get; private set; }

        private readonly BundlesDatabase database;

        public RelayCommand AddBundleCommand { get; private set; }

        public RelayCommand RemoveBundleCommand { get; private set; }

        public BundlesManagerViewModel()
        {
            Bundles = new ObservableCollection<Bundle>();
            database = new BundlesDatabase(FileManager.GetThumbGenFolder());

            AddBundleCommand = new RelayCommand(AddBundleCommandExecute);
            RemoveBundleCommand = new RelayCommand(RemoveBundleCommandExecute);

            Refresh();
        }

        private void Refresh()
        {
            Bundles.Clear();
            database.Refresh();
            database.Bundles.ForEach(x => Bundles.Add(x));
        }

        private void AddBundleCommandExecute(object param)
        {
            var ofd = new OpenFileDialog { Filter = "ThumbGen Bundles (.bundle files) |*.bundle", Multiselect = false, Title = "Choose a bundle to install" };
            if ((bool)ofd.ShowDialog())
            {
                var manager = new BundleManager(FileManager.GetThumbGenFolder(), ofd.FileName);
                var bundle = manager.GetBundle();
                manager.BundleInstalled += (o, e) =>
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        // switch the default profile
                        if (!string.IsNullOrEmpty(bundle.DefaultProfile))
                        {
                            FileManager.ProfilesMan.RefreshProfiles(Path.GetFileNameWithoutExtension(bundle.DefaultProfile));
                        }
                        OverlayAdornerHelper.RemoveAllAdorners(param as UIElement);

                        Refresh();

                        MessageBox.Show(string.Format("'{0} v{1}' was installed successfully!", bundle.Name, bundle.Version), "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                    }));

                if (database.HasBundle(bundle.Id, bundle.Version))
                {
                    // ask user if proceed or not
                    if (MessageBox.Show(string.Format("The '{0} v{1}' bundle is already installed!\n\rBy reinstalling it all its files and settings will be overwritten!\r\nDo you want to proceed installing?", bundle.Name, bundle.Version),
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                else if (database.HasBundle(bundle.Id)) // maybe some other version
                {
                    MessageBox.Show(string.Format("A different version of the '{0}' bundle is already installed!\n\rPlease delete the other bundle version before installing the new one.", bundle.Name),
                        "Confirmation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                new OverlayAdornerHelper(param as UIElement, new LoadingScreen("Please wait... Installing bundle...", false));

                ThreadPool.QueueUserWorkItem(o => manager.InstallBundle());
            }
        }

        private void RemoveBundleCommandExecute(object param)
        {
            var bundle = param as Bundle;
            if (bundle == null) return;

            var manager = new BundleManager(FileManager.GetThumbGenFolder(), null);

            manager.BundleRemoved += (o, e) =>
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    Refresh();

                    //OverlayAdornerHelper.RemoveAllAdorners(param as UIElement);
                    MessageBox.Show(string.Format("'{0} v{1}' was removed successfully!", bundle.Name, bundle.Version), "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                }));

            if (database.HasBundle(bundle.Id, bundle.Version))
            {
                // ask user if proceed or not
                if (MessageBox.Show(string.Format("Uninstalling a bundle will remove the templates and the profiles belonging to this bundle and will uninstall all "+
                    "fonts provided by the bundle!\r\n\r\nAre you sure you want to permanently delete the '{0} v{1}' bundle?", bundle.Name, bundle.Version),
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }
            }

            //new OverlayAdornerHelper(param as UIElement, new LoadingScreen("Please wait... Removing bundle...", false));

            // switch to the default profile
            FileManager.ProfilesMan.RefreshProfiles(FileManager.ProfilesMan.DefaultProfileName);

            ThreadPool.QueueUserWorkItem(o => manager.RemoveBundle(bundle.Id, bundle.Version));

        }
    }
}
