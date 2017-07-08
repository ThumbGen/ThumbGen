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
using System.Windows.Shapes;
using System.Security;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for ChangePasswordDialog.xaml
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public static string Password { get; private set; }

        public ChangePasswordDialog()
        {
            InitializeComponent();
            Password = string.Empty;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (tbNewPass.Password != tbConfirmPass.Password)
            {
                MessageBox.Show("Confirmed password does not match the new password!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Password = tbNewPass.Password;
            this.DialogResult = true;
        }

        public static bool Show(Window owner)
        {
            Password = string.Empty;
            ChangePasswordDialog _box = new ChangePasswordDialog();
            _box.Owner = owner;
            _box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var res = _box.ShowDialog();
            return res.HasValue && res.Value;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbNewPass.Focus();
        }
    }
}
