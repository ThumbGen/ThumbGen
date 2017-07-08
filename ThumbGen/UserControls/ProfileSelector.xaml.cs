using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for ProfileSelector.xaml
    /// </summary>
    public partial class ProfileSelector : UserControl
    {
        public ProfileSelector()
        {
            InitializeComponent();
        }

        public ProfilesManagerBase Manager
        {
            get { return (ProfilesManagerBase)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Manager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManagerProperty =
            DependencyProperty.Register("Manager", typeof(ProfilesManagerBase), typeof(ProfileSelector), new UIPropertyMetadata(OnManagerChanged));

        private static void OnManagerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var sel = sender as ProfileSelector;
            if(sel != null)
            {
                var manager = args.NewValue as ProfilesManager;
                if (manager != null)
                {
                    manager.SelectedProfileChanged += (o, e) => sel.SwitchProfiles(e.OldProfilePath, e.NewProfilePath);
                }
            }
        }

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(ProfileSelector), new UIPropertyMetadata("Profiles"));
       
        public event EventHandler<ProfileChangedEventArgs> OnProfileChanged;

        public event EventHandler<ProfileSavingEventArgs> OnProfileSaving;

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (OnProfileSaving != null)
            {
                OnProfileSaving(this, new ProfileSavingEventArgs(Manager.SelectedProfile.ProfilePath));
            }

            Manager.SaveCurrentProfile();
        }

        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            if (OnProfileSaving != null)
            {
                OnProfileSaving(this, new ProfileSavingEventArgs(Manager.SelectedProfile.ProfilePath));
            }

            Manager.AddNewProfile();
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            Manager.Delete(Manager.SelectedProfile.ProfilePath);
        }

        private void RefreshProfiles_Click(object sender, RoutedEventArgs e)
        {
            Manager.RefreshProfiles(Manager.SelectedProfile.ProfileName);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // first item in the combo is always the "default" (mandatory) profile; that cannot be deleted
            DeleteProfileButton.IsEnabled = (sender as ComboBox).SelectedIndex != 0;

            string oldProfile = e.RemovedItems.Count != 0 ? (e.RemovedItems[0] as ProfileItem).ProfilePath : null;
            string newProfile = e.AddedItems.Count != 0 ? (e.AddedItems[0] as ProfileItem).ProfilePath : null;
            if (!string.IsNullOrEmpty(oldProfile) && !string.IsNullOrEmpty(newProfile) && oldProfile != newProfile)
            {
                SwitchProfiles(oldProfile, newProfile);
            }
        }

        private void SwitchProfiles(string oldProfile, string newProfile)
        {
            // trigger saving event
            if (OnProfileSaving != null)
            {
                OnProfileSaving(this, new ProfileSavingEventArgs(oldProfile));
            }

            // switch profiles
            Manager.SwitchProfiles(oldProfile, newProfile);
            if (OnProfileChanged != null)
            {
                // trigger profile changed event
                OnProfileChanged(this, new ProfileChangedEventArgs(oldProfile, newProfile));
            }
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement _elem = sender as FrameworkElement;
            ContentPresenter _pres = _elem.TemplatedParent as ContentPresenter;
            //_pres.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
        }

        
    }

    public class ProfileSavingEventArgs : EventArgs
    {
        public string ProfilePath { get; set; }

        public ProfileSavingEventArgs(string profile)
        {
            ProfilePath = profile;
        }
    }

    public class ProfileChangedEventArgs : EventArgs
    {
        public string OldProfilePath { get; set; }
        public string NewProfilePath { get; set; }

        public ProfileChangedEventArgs(string old, string newP)
        {
            OldProfilePath = old;
            NewProfilePath = newP;
        }
    }
}
