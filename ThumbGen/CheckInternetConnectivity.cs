using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ThumbGen
{
    public static class CheckInternetConnectivity
    {
        [DllImport("wininet", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref ConnectionStatusEnum flags, int dw);

        /// <summary>
        /// enum to hold the possible connection states
        /// </summary>
        [Flags]
        enum ConnectionStatusEnum : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        /// <summary>
        /// method to check the status of the pinging machines internet connection
        /// </summary>
        /// <returns></returns>
        private static bool HasConnection()
        {
            //instance of out ConnectionStatusEnum
            ConnectionStatusEnum state = 0;

            //call the API
            InternetGetConnectedState(ref state, 0);

            //check the status, if not offline and the returned state
            //isnt 0 then we have a connection
            if (((int)ConnectionStatusEnum.INTERNET_CONNECTION_OFFLINE & (int)state) != 0)
            {
                //return true, we have a connection
                return false;
            }
            //return false, no connection available
            return true;
        }

        /// <summary>
        /// method for retrieving the IP address from the host provided
        /// </summary>
        /// <param name="host">the host we need the address for</param>
        /// <returns></returns>
        private static IPAddress GetIpFromHost(ref string host)
        {
            string returnMessage = string.Empty;
            //IPAddress instance for holding the returned host
            IPAddress address = null;

            //wrap the attempt in a try..catch to capture
            //any exceptions that may occur
            try
            {
                //get the host IP from the name provided
                address = Dns.GetHostEntry(host).AddressList[0];
            }
            catch (SocketException ex)
            {
                //some DNS error happened, return the message
                returnMessage = string.Format("DNS Error: {0}", ex.Message);
            }
            return address;
        }

        /// <summary>
        /// method to check the ping status of a provided host
        /// </summary>
        /// <param name="addr">the host we need to ping</param>
        /// <returns></returns>
        public static CheckResponse CheckSiteStatus(string host)
        {
            CheckResponse _result = new CheckResponse();
            _result.Online = false;
            try
            {
                //string to hold our return messge
                string returnMessage = string.Empty;

                //IPAddress instance for holding the returned host
                IPAddress address = GetIpFromHost(ref host);

                //set the ping options, TTL 128
                PingOptions options = new PingOptions(128, true);

                //create a new ping instance
                Ping ping = new Ping();

                //32 byte buffer
                byte[] data = new byte[32];

                //first make sure we actually have an internet connection
                if (HasConnection())
                {
                    //here we will ping the host 4 times (standard)
                    for (int i = 0; i < 4; i++)
                    {
                        try
                        {
                            //send the ping 4 times to the host and record the returned data
                            PingReply reply = ping.Send(address, 1000, data, options);

                            //make sure we dont have a null reply
                            if (!(reply == null))
                            {
                                switch (reply.Status)
                                {
                                    case IPStatus.Success:
                                        returnMessage = string.Format("Reply from {0}: bytes={1} time={2}ms TTL={3}", reply.Address, reply.Buffer.Length, reply.RoundtripTime, reply.Options.Ttl);
                                        _result.Online = true;
                                        break;
                                    case IPStatus.TimedOut:
                                        returnMessage = "Connection has timed out...";
                                        break;
                                    default:
                                        returnMessage = string.Format("Ping failed: {0}", reply.Status.ToString());
                                        break;
                                }
                            }
                            else
                                returnMessage = "Connection failed for an unknown reason...";
                        }
                        catch (PingException ex)
                        {
                            returnMessage = string.Format("Connection Error: {0}", ex.Message);
                        }
                        catch (SocketException ex)
                        {
                            returnMessage = string.Format("Connection Error: {0}", ex.Message);
                        }
                    }
                }
                else
                    returnMessage = "No Internet connection found...";

                _result.Message = returnMessage;
            }
            catch { }
            //return the message
            return _result;
        }


        public class CheckResponse
        {
            public string Message { get; set; }
            public bool Online { get; set; }
        }
    }
}