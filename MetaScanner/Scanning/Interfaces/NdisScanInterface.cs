﻿////////////////////////////////////////////////////////////////

#region Header

//
// Copyright (c) 2007-2010 MetaGeek, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion Header


////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Timers;
using WirelessFireless.Misc;
using ManagedWifi;
using MetaGeek.IoctlNdis;
using MetaGeek.WiFi;

namespace WirelessFireless.Scanning.Interfaces
{
    public class NdisScanInterface : IScanningInterface
    {
        #region Fields

        private IoctlNdis Ndis;
        private Timer ScanCompleteTimer = new Timer(1000) {AutoReset = false};
        private NetworkInterface _interface;

        #endregion Fields

        #region Events

        public event EventHandler InterfaceError;

        public event EventHandler ScanComplete;

        #endregion Events

        #region Constructors

        public NdisScanInterface()
        {
            Ndis = new IoctlNdis();
            ScanCompleteTimer.Elapsed += ScanCompleteTimer_Elapsed;
        }

        #endregion Constructors

        #region Public Methods

        public IEnumerable<NetworkData> GetNetworkData()
        {
            List<NetworkData> list = new List<NetworkData>();
            IEnumerable<NdisWlanBssidEx> exArray = Ndis.QueryBssidList(_interface);

            //Gets the connected AP BSSID
            byte[] connectedBssid = Ndis.QueryConnected(_interface);
            if (exArray != null)
            {
                foreach (NdisWlanBssidEx ex in exArray)
                {
                    NetworkData data2 = new NetworkData(ex.MacAddress);
                    data2.Channel = Utilities.ConvertToChannel(ex.Configuration.DSConfig);
                    NetworkData item = data2;

                    if (item.MyMacAddress.MyValue == 0)
                        continue;

                    if ((ex.IELength <= ex.IEs.Length) && (ex.IELength > 28))
                    {
                        byte[] ies = new byte[ex.IELength];
                        Array.Copy(ex.IEs, 0, ies, 0, ex.IELength);

                            item.NSettings = IeParser.Parse(ies);
                            item.IsTypeN = item.NSettings != null;
                            if (item.NSettings != null)
                            {
                                //Add the extended 802.11N rates
                                item.Rates.AddRange(item.NSettings.Rates.Where(f => !item.Rates.Contains(f)));
                                item.Rates.Sort();
                            }
                        //}
                    }
                    Utilities.ConvertToMbs(ex.SupportedRates, item.Rates, item.IsTypeN);
                    item.Rssi = ex.Rssi;
                    item.SignalQuality = 0;
                    string str = Encoding.ASCII.GetString(ex.Ssid, 0, (int)ex.SsidLength);
                    if (str != null)
                    {
                        str = str.Trim();
                    }
                    item.Ssid = str;
                    item.Security = Ndis.GetSecurityString(ex);
                    item.NetworkType = Utilities.FindValueString(Utilities.InfrastructureText, (int)ex.InfrastructureMode);

                    //Check to see if this AP is the connected one
                    item.Connected = item.MyMacAddress.Bytes.SequenceEqual(connectedBssid);

                    list.Add(item);
                }
            }
            return list;
        }

        public void Init(NetworkInterface wlanInterface, out Exception error)
        {
            error = null;

            _interface = wlanInterface;

            if (_interface == null)
            {
                error = new ArgumentException("Invalid wireless interface", "wlanInterface");
                return;
            }
        }

        public void ScanNetworks()
        {
            if(!Ndis.Scan(_interface))
            {
                //There was a problem
                OnInterfaceError();
            }

            //Signal complete after a delay
            ScanCompleteTimer.Start();
        }

        #endregion Public Methods

        #region Private Methods

        private void OnInterfaceError()
        {
            if (InterfaceError == null) return;
            InterfaceError(this, EventArgs.Empty);
        }

        private void OnScanComplete()
        {
            if (ScanComplete == null) return;
            ScanComplete(this, EventArgs.Empty);
        }

        private void ScanCompleteTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnScanComplete();
        }

        #endregion Private Methods
    }
}