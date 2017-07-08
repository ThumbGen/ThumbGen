using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Windows;
using System.Windows.Input;

namespace ThumbGen
{
    internal class TelnetHelper
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string User { get; private set; }
        public string Pass { get; private set; }
        public string Prompt { get; private set; }

        public string LastError { get; private set; }

        private TelnetConnection m_TC;

        public TelnetHelper()
        {
            Host = FileManager.Configuration.Options.TelnetOptions.TelnetHost;
            Port = 23;
            try
            {
                Port = Int32.Parse(FileManager.Configuration.Options.TelnetOptions.TelnetPort);
            }
            catch { }
            User = FileManager.Configuration.Options.TelnetOptions.TelnetUser;
            Pass = FileManager.Configuration.Options.TelnetOptions.TelnetPass;
        }

        public bool IsConnected
        {
            get
            {
                return m_TC != null && m_TC.IsConnected;
            }
        }

        public bool Connect()
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(Host) && !string.IsNullOrEmpty(User))
            {
                this.Disconnect();
                try
                {
                    //create a new telnet connection 
                    m_TC = new TelnetConnection(this.Host, this.Port);

                    //login with user and pass
                    string s = m_TC.Login(this.User, this.Pass, 400);
                    Loggy.Logger.Debug("Telnet Connected: " + s);
                    // server output should end with "$" or ">" or "#" otherwise the connection failed
                    string prompt = s.TrimEnd();
                    prompt = s.Substring(prompt.Length - 1, 1);
                    _result = prompt == "$" || prompt == ">" || prompt == "#";
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Telnet Login :", ex);
                }
            }

            return _result;
        }

        public void Disconnect()
        {
            if (m_TC != null && m_TC.IsConnected)
            {
                m_TC.WriteLine("exit");
                m_TC.Close();
            }
        }

        public void SendReboot()
        {
            SendCommand("sync && sync && reboot -f");
        }

        public void SendCommand(string command)
        {
            ExecuteCommands(new Dictionary<string, string>() { { command, this.Prompt } });
        }

        public bool DoChangePassword(Window owner)
        {
            bool _result = false;
            
            try
            {
                this.LastError = string.Empty;

                if (ChangePasswordDialog.Show(owner))
                {
                    Mouse.SetCursor(System.Windows.Input.Cursors.Wait);
                    this.Connect();
                    try
                    {
                        // if connected
                        if (this.IsConnected)
                        {
                            m_TC.Read();

                            string _response;
                            // execute passwd
                            Loggy.Logger.Debug("Telnet Sent passwd");
                            m_TC.WriteLine("passwd");
                            _response = m_TC.Read().Replace("passwd", "");
                            if (_response.Contains("Changing password"))
                            {
                                m_TC.WriteLine(ChangePasswordDialog.Password);
                                _response = m_TC.Read();
                                if (_response.Contains("Retype password"))
                                {
                                    m_TC.WriteLine(ChangePasswordDialog.Password);
                                    _response = m_TC.Read();
                                    _result = _response.Contains("changed");
                                    Loggy.Logger.Debug("Telnet Password changed");
                                    //if (!_result)
                                    //{
                                    //    this.LastError = _response.Replace("\r", " ").Replace("\n", " ");
                                    //}
                                }
                            }
                        }
                    }
                    finally
                    {
                        this.Disconnect();
                    }
                }

                if (_result)
                {
                    FileManager.Configuration.Options.TelnetOptions.TelnetPass = ChangePasswordDialog.Password;
                    FileManager.Configuration.Options.SSHOptions.SSHPass = ChangePasswordDialog.Password;
                }
            }
            finally
            {
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }

            return _result;
        }

        public static void ChangePassword(Window owner)
        {
            TelnetHelper _th = new TelnetHelper();
            if (_th.DoChangePassword(owner))
            {
                MessageBox.Show("Password changed!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Password was NOT changed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
            }
            _th.Disconnect();
            _th = null;
        }

        public void ExecuteCommands(Dictionary<string, string> commands)
        {
            if (commands == null || commands.Count() == 0)
            {
                return;
            }

            Mouse.SetCursor(System.Windows.Input.Cursors.Wait);
            try
            {
                try
                {
                    Loggy.Logger.Debug("Telnet Connecting...");
                    if (this.Connect())
                    {
                        if (m_TC.IsConnected)
                        {
                            m_TC.Read();

                            Loggy.Logger.Debug("Telnet Connected to server");

                            foreach (KeyValuePair<string, string> _command in commands)
                            {
                                if (string.IsNullOrEmpty(_command.Key))
                                {
                                    Loggy.Logger.Debug("Telnet Empty command");
                                    continue;
                                }
                                Loggy.Logger.Debug(string.Format("Telnet Sending: {0}", _command.Key));
                                m_TC.WriteLine(_command.Key);
                                //System.Threading.Thread.Sleep(1000);
                                string _res = null;
                                if (!_command.Key.ToLowerInvariant().Contains("reboot"))
                                {
                                    //System.Threading.Thread.Sleep(1000);
                                    _res = m_TC.Read();
                                }
                                Loggy.Logger.Debug("Telnet Response: " + _res);
                                System.Threading.Thread.Sleep(200);
                            }
                        }
                        System.Threading.Thread.Sleep(1000);
                        Loggy.Logger.Debug("Telnet Disconnecting...");
                        m_TC.Close();
                        Loggy.Logger.Debug("Telnet Disconnected");
                    }
                    else
                    {
                        Loggy.Logger.Debug("Telnet Cannot connect");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Loggy.Logger.DebugException("Telnet Exception: ", ex);
                }
            }
            finally
            {
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }
        }
    }
}
