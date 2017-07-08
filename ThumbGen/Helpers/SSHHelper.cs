using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tamir.SharpSsh;
using System.Windows;
using System.Windows.Input;

namespace ThumbGen
{
    internal class SSHHelper
    {
        private SshShell m_Shell;

        public string Prompt { get; set; }

        public SSHHelper()
        {
            Prompt = string.Empty;
        }

        public bool Connect()
        {
            bool _result = false;

            try
            {

                if (m_Shell != null)
                {
                    try
                    {
                        m_Shell.Close();
                    }
                    catch { }
                }
                m_Shell = new SshShell(FileManager.Configuration.Options.SSHOptions.SSHHost,
                                       FileManager.Configuration.Options.SSHOptions.SSHUser,
                                       FileManager.Configuration.Options.SSHOptions.SSHPass);
                int _port = 22;
                try
                {
                    _port = Int32.Parse(FileManager.Configuration.Options.SSHOptions.SSHPort);
                }
                catch { }

                m_Shell.Connect(_port);

                _result = m_Shell.Connected;
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("SSH Connect: ", ex);
            }
            return _result;
        }

        public void Disconnect()
        {
            if (m_Shell != null && m_Shell.Connected)
            {
                m_Shell.Close();
            }
        }

        public void SendReboot()
        {
            SendShellCommand("sync && sync && reboot -f");
        }

        public void SendShellCommand(string command)
        {
            ExecuteCommands(new Dictionary<string, string>() { {command, this.Prompt} });
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
                    Loggy.Logger.Debug("SSH Connecting...");
                    if (this.Connect())
                    {
                        m_Shell.Expect(commands.Values.ElementAt(0));
                        if (m_Shell.ShellOpened)
                        {
                            m_Shell.RemoveTerminalEmulationCharacters = true;

                            Loggy.Logger.Debug("SSH Connected to " + m_Shell.ServerVersion);

                            foreach (KeyValuePair<string, string> _command in commands)
                            {
                                m_Shell.ExpectPattern = _command.Value;

                                if (string.IsNullOrEmpty(_command.Key))
                                {
                                    Loggy.Logger.Debug("SSH Empty command");
                                    continue;
                                }
                                Loggy.Logger.Debug(string.Format("SSH Sending: {0} Expecting: {1}", _command.Key, _command.Value));
                                m_Shell.WriteLine(_command.Key);
                                //System.Threading.Thread.Sleep(1000);
                                string _res = null;
                                if (!_command.Key.ToLowerInvariant().Contains("reboot"))
                                {
                                    //System.Threading.Thread.Sleep(1000);
                                    _res = m_Shell.Expect(m_Shell.ExpectPattern);
                                }
                                Loggy.Logger.Debug("SSH Response: " + _res);
                                System.Threading.Thread.Sleep(200);
                            }
                        }
                        System.Threading.Thread.Sleep(1000);
                        Loggy.Logger.Debug("SSH Disconnecting...");
                        m_Shell.Close();
                        Loggy.Logger.Debug("SSH Disconnected");
                    }
                    else
                    {
                        Loggy.Logger.Debug("SSH Cannot connect");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Loggy.Logger.DebugException("SSH Exception: ", ex);
                }
            }
            finally
            {
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }
        }

    }
}
