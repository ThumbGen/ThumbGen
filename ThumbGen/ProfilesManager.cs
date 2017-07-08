using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using Microsoft.Win32;

namespace ThumbGen
{

    public abstract class ProfilesManagerBase : DependencyObject
    {
        public abstract string DefaultProfileName { get; }

        public ObservableCollection<ProfileItemBase> Profiles { get; protected set; }

        public ProfileItemBase SelectedProfile
        {
            get { return (ProfileItemBase)GetValue(SelectedProfileProperty); }
            set { SetValue(SelectedProfileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedProfile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedProfileProperty =
            DependencyProperty.Register("SelectedProfile", typeof(ProfileItemBase), typeof(ProfilesManager), new UIPropertyMetadata(null));

        public string SelectedProfileName
        {
            get
            {
                return this.SelectedProfile != null ? this.SelectedProfile.ProfileName : null;
            }
        }

        public event EventHandler<ProfileChangedEventArgs> SelectedProfileChanged;

        public abstract void RefreshProfiles(string selectedProfileName);

        public abstract void Delete(string profilePath);

        public abstract void SaveCurrentProfile();

        public abstract void AddNewProfile();

        public abstract void SwitchProfiles(string old, string newP);

        public abstract string GetProfileName(string profilePath);

        public ProfilesManagerBase()
        {
            Profiles = new ObservableCollection<ProfileItemBase>();
        }

        protected void OnSelectedProfileChanged(string oldProfile, string newProfile)
        {
            var handler = SelectedProfileChanged;
            if(handler != null)
            {
                SelectedProfileChanged(this, new ProfileChangedEventArgs(oldProfile, newProfile));
            }
        }
    }


    public class ProfilesManager : ProfilesManagerBase
    {
        //public ProfilesManager()
        //{
        //    Profiles = new ObservableCollection<ProfileItemBase>();
        //}

        public override string DefaultProfileName
        {
            get { return "<default>"; }
        }

        public override void RefreshProfiles(string selectedProfileName)
        {
            var oldProfile = SelectedProfile != null ? SelectedProfile.ProfilePath : null;
            ProfileItem _selectedProfile = null;
            try
            {
                Profiles.Clear();
                // ALWAYS on the first position insert the default profile (ThumbGen.tgp, ex config.xml)
                Profiles.Add(new ProfileItem(Configuration.ConfigFilePath, GetProfileName(Configuration.ConfigFilePath)));
                // now add the rest
                string _ProfilesFolder = FileManager.GetProfilesFolder();
                if (Directory.Exists(_ProfilesFolder))
                {
                    string[] _Profiles = Directory.GetFiles(_ProfilesFolder, "*.tgp", SearchOption.TopDirectoryOnly);
                    if (_Profiles != null && _Profiles.Count() != 0)
                    {
                        foreach (string _ProfilePath in _Profiles)
                        {
                            ProfileItem _Profile = new ProfileItem(_ProfilePath, GetProfileName(_ProfilePath));
                            Profiles.Add(_Profile);
                            if (!string.IsNullOrEmpty(selectedProfileName) &&
                                 string.Compare(_Profile.ProfileName, selectedProfileName, true) == 0)
                            {
                                _selectedProfile = _Profile;
                            }
                        }
                    }
                }
                if (_selectedProfile == null && selectedProfileName == DefaultProfileName)
                {
                    _selectedProfile = Profiles[0] as ProfileItem;
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Refresh Profiles", ex);
            }
            if (_selectedProfile != null)
            {
                _selectedProfile.IsSelected = false;
                _selectedProfile.IsSelected = true;
                SelectedProfile = _selectedProfile;
                OnSelectedProfileChanged(oldProfile, SelectedProfile.ProfilePath);
                FileManager.Configuration.LoadConfiguration(_selectedProfile.ProfilePath);
            }
        }

        public override void Delete(string profilePath)
        {
            if (MessageBox.Show("Are you sure you want to delete the current profile?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                if (!string.IsNullOrEmpty(profilePath))
                {
                    // switch to the default profile
                    FileManager.Configuration.LoadConfiguration(Configuration.ConfigFilePath);
                    // remove profile
                    if (File.Exists(profilePath))
                    {
                        File.Delete(profilePath);
                    }
                    // refresh profiles list and select the default one
                    RefreshProfiles(DefaultProfileName);
                }
            }
        }

        public override void SaveCurrentProfile()
        {
            FileManager.Configuration.SaveConfiguration(this.SelectedProfile.ProfilePath);
        }

        public override void AddNewProfile()
        {
            SaveFileDialog _sfd = new SaveFileDialog();
            _sfd.InitialDirectory = FileManager.GetProfilesFolder();
            _sfd.Title = "Save profile as";
            _sfd.DefaultExt = ".tgp";
            _sfd.Filter = "ThumbGen Profile (*.tgp)|*.tgp";
            if ((bool)_sfd.ShowDialog())
            {
                // save current profile with the new name
                FileManager.Configuration.SaveConfiguration(_sfd.FileName);
                // select the new profile as current
                RefreshProfiles(GetProfileName(_sfd.FileName));
            }
        }

        public override void SwitchProfiles(string old, string newP)
        {
            // save old profile
            FileManager.Configuration.SaveConfiguration(old);
            // load the new profile
            FileManager.Configuration.LoadConfiguration(newP);
        }

        public override string GetProfileName(string profilePath)
        {
            if (!string.IsNullOrEmpty(profilePath) && string.Compare(profilePath, Configuration.ConfigFilePath, true) == 0)
            {
                return DefaultProfileName;
            }
            else
            {
                return (!string.IsNullOrEmpty(profilePath) ? Path.GetFileNameWithoutExtension(profilePath) : string.Empty);
            }
        }

        public ProfileItem CreateProfileItem(string profilePath)
        {
            return new ProfileItem(profilePath, GetProfileName(profilePath));
        }
    }

    public class LayoutsManager : ProfilesManagerBase, IDisposable
    {
        public AvalonDock.DockingManager DockManager { get; set; }

        public string DefaultLayoutPath
        {
            get 
            {
                return Path.Combine(FileManager.GetMovieLayoutsFolder(), DefaultProfileName + ".tgl");
            }

        }

        public override string DefaultProfileName
        {
            get { return "default"; }
        }

        public void RefreshProfiles()
        {
            RefreshProfiles(null);
        }

        public override void RefreshProfiles(string selectedProfileName)
        {
            // if selectedProfileName is null, select the first one (that should always be there as it is generated by default when loading ResultsPage
            ProfileItem _selectedProfile = null;
            try
            {
                Profiles.Clear();
                // ALWAYS on the first position insert the default profile 
                Profiles.Add(new ProfileItem(DefaultLayoutPath, GetProfileName(DefaultLayoutPath)));

                // now add the rest
                string _ProfilesFolder = FileManager.GetMovieLayoutsFolder();
                if (Directory.Exists(_ProfilesFolder))
                {
                    string[] _Profiles = Directory.GetFiles(_ProfilesFolder, "*.tgl", SearchOption.TopDirectoryOnly);
                    if (_Profiles != null && _Profiles.Count() != 0)
                    {
                        foreach (string _ProfilePath in _Profiles)
                        {
                            string _profileName = GetProfileName(_ProfilePath);
                            if (_profileName.ToLowerInvariant() != DefaultProfileName.ToLowerInvariant())
                            {
                                ProfileItem _Profile = new ProfileItem(_ProfilePath, GetProfileName(_ProfilePath));
                                Profiles.Add(_Profile);
                                if (!string.IsNullOrEmpty(selectedProfileName) &&
                                     string.Compare(_Profile.ProfileName, selectedProfileName, true) == 0)
                                {
                                    _selectedProfile = _Profile;
                                }
                            }
                        }
                    }
                }
                if (_selectedProfile == null || selectedProfileName == DefaultProfileName)
                {
                    _selectedProfile = Profiles[0] as ProfileItem;
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Refresh Layouts", ex);
            }
            if (_selectedProfile != null)
            {
                this.SelectedProfile = null;
                this.SelectedProfile = _selectedProfile;
                this.SelectedProfile.IsSelected = false;
                this.SelectedProfile.IsSelected = true;

                Helpers.DoEvents();

                if (!File.Exists(_selectedProfile.ProfilePath))
                {
                    SaveLayout(_selectedProfile.ProfilePath);
                }
                else
                {
                    try
                    {
                        DockManager.RestoreLayout(_selectedProfile.ProfilePath);
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("Cannot restore results layout", ex);
                    }
                    
                }
            }
        }

        public override void Delete(string profilePath)
        {
            if (MessageBox.Show("Are you sure you want to delete the current layout?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                if (!string.IsNullOrEmpty(profilePath))
                {
                    // switch to the default profile
                    try
                    {
                        DockManager.RestoreLayout(DefaultLayoutPath);
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("Cannot switch results layout", ex);
                    }
                    // remove profile
                    if (File.Exists(profilePath))
                    {
                        File.Delete(profilePath);
                    }
                    // refresh profiles list and select the default one (first one)
                    RefreshProfiles();
                }
            }
        }

        public override void SaveCurrentProfile()
        {
            SaveLayout(SelectedProfile.ProfilePath);
        }

        public override void AddNewProfile()
        {
            SaveFileDialog _sfd = new SaveFileDialog();
            _sfd.InitialDirectory = FileManager.GetMovieLayoutsFolder();
            _sfd.Title = "Save layout as";
            _sfd.DefaultExt = ".tgl";
            _sfd.Filter = "ThumbGen Layout (*.tgl)|*.tgl";
            if ((bool)_sfd.ShowDialog())
            {
                // save current profile with the new name
                SaveLayout(_sfd.FileName);
                // select the new profile as current
                RefreshProfiles(GetProfileName(_sfd.FileName));
            }
        }

        private void SaveLayout(string filename)
        {
            if(string.IsNullOrEmpty(filename)) return;
            try
            {
                DockManager.SaveLayout(filename);
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error("Cannot save layout:" + ex.Message);
            }
        }

        public override void SwitchProfiles(string old, string newP)
        {
            // save old layout
            SaveLayout(old);
            // load the new layout
            try
            {
                DockManager.RestoreLayout(newP);
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Cannot switch results layout 2 ", ex);
            }
        }

        public override string GetProfileName(string profilePath)
        {
            return (!string.IsNullOrEmpty(profilePath) ? Path.GetFileNameWithoutExtension(profilePath) : string.Empty);
        }

        public void Dispose()
        {
            DockManager = null;
        }
    }
}
