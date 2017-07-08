using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using Fluent;

namespace ThumbGen
{
    public class WindowSettings
    {
        #region Constructor
        private Window window = null;

        public WindowSettings(Window window)
        {
            this.window = window;
        }

        #endregion

        #region Attached "Save" Property Implementation
        /// <summary>
        /// Register the "Save" attached property and the "OnSaveInvalidated" callback 
        /// </summary>
        public static readonly DependencyProperty SaveProperty
           = DependencyProperty.RegisterAttached("Save", typeof(bool), typeof(WindowSettings),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSaveInvalidated)));

        public static void SetSave(DependencyObject dependencyObject, bool enabled)
        {
            dependencyObject.SetValue(SaveProperty, enabled);
        }

        /// <summary>
        /// Called when Save is changed on an object.
        /// </summary>
        private static void OnSaveInvalidated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            Window window = dependencyObject as Window;
            if (window != null)
            {
                if ((bool)e.NewValue)
                {
                    WindowSettings settings = new WindowSettings(window);
                    settings.Attach();
                }
            }
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Load the Window Size Location and State from the settings object
        /// </summary>
        protected virtual void LoadWindowState()
        {
            if (FileManager.Configuration.Options.WindowsOptions.Windows.ContainsKey(this.window.GetType().FullName))
            {
                UserOptions.WindowProperties _props = FileManager.Configuration.Options.WindowsOptions.Windows[this.window.GetType().FullName];
                if (_props.Position != Rect.Empty)
                {
                    this.window.WindowState = _props.State;
                    
                    this.window.Left = Math.Max(-16, _props.Position.X);
                    this.window.Top = Math.Max(-16, _props.Position.Y);
                    m_Left_Correction = Math.Max(-16, _props.Position.X - this.window.Left);
                    m_Top_Correction = Math.Max(-16, _props.Position.Y - this.window.Top);
                    this.window.Width = _props.Position.Width;
                    this.window.Height = _props.Position.Height;

                    this.window.WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
        }

        private double m_Left_Correction = 0d;
        private double m_Top_Correction = 0d;

        /// <summary>
        /// Save the Window Size, Location and State to the settings object
        /// </summary>
        protected virtual void SaveWindowState()
        {
            UserOptions.WindowProperties _props = null;
            if (FileManager.Configuration.Options.WindowsOptions.Windows.ContainsKey(this.window.GetType().FullName))
            {
                _props = FileManager.Configuration.Options.WindowsOptions.Windows[this.window.GetType().FullName];
            }
            else
            {
                _props = new UserOptions.WindowProperties();
                FileManager.Configuration.Options.WindowsOptions.Windows.Add(this.window.GetType().FullName, _props);
            }
            //_props.Position = this.window.RestoreBounds;
            _props.Position = new Rect(this.window.Left + m_Left_Correction, this.window.Top + m_Top_Correction, this.window.ActualWidth, this.window.ActualHeight);
            _props.State = this.window.WindowState;
        }
        #endregion

        #region Private Methods

        private void Attach()
        {
            if (this.window != null)
            {
                this.window.Closing += new CancelEventHandler(window_Closing);
                this.window.Initialized += new EventHandler(window_Initialized);
                this.window.Loaded += new RoutedEventHandler(window_Loaded);
            }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWindowState();
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            LoadWindowState();
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowState();
        }
        #endregion

    }
}
