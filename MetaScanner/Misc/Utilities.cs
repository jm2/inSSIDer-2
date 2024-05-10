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
using System.Drawing;
using System.Globalization;
using WirelessFireless.Localization;
using ManagedWifi;
using MetaGeek.IoctlNdis;
using MetaGeek.WiFi;

namespace WirelessFireless.Misc
{
    public static class Utilities
    {
        #region Fields

        // access point color map
        public static Color[] ApColors = 
        {
            Color.FromArgb(255, 47,255,5),
            Color.FromArgb(255, 36,170,231),
            Color.FromArgb(255, 252,253,50),
            Color.FromArgb(255, 253,153,51),
            Color.FromArgb(255, 101,205,252),
            Color.FromArgb(255, 150,50,206),
            Color.FromArgb(255, 99,206,49),
            Color.FromArgb(255, 31,101,199),
            Color.FromArgb(255, 253,102,55),
            Color.FromArgb(255, 204,255,105),
            Color.FromArgb(255, 252,52,255),
            Color.FromArgb(255, 148,255,255),
        };
        private static readonly CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();

        /// <summary>
        /// Infrastructure mode strings.
        /// </summary>
        public static readonly ValueString[] InfrastructureText = new[]{
            new ValueString( (int)NetworkInfrastructure.Ndis80211Infrastructure, "Infrastructure" ),
            new ValueString( (int)NetworkInfrastructure.Ndis80211Ibss, "Adhoc" ),
            new ValueString( (int)NetworkInfrastructure.Ndis80211AutoUnknown, "Auto or unknown" )
         };

        /// <summary>
        /// Network type strings.
        /// </summary>
        public static readonly ValueString[] NetworkTypeText = new[]{ 
            new ValueString( (int)NdisNetworkType.Ndis80211Ofdm24, "2.4-GHz OFDM"),
            new ValueString( (int)NdisNetworkType.Ndis80211Ofdm5, "5-GHz OFDM" ),
            new ValueString( (int)NdisNetworkType.Ndis80211Ds, "DS (direct-sequence spread-spectrum)")
        };
        public static List<Color> UsedColors = new List<Color>();
        private static int _currentIndex = -1;

        #endregion Fields

        #region Enumerations

        public enum SwitchMode
        {
            None,
            ToMain,
            ToMini
        }

        #endregion Enumerations

        #region Public Methods

        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        /// <summary>
        /// Computes the RSSI dBM value based on the signal quality.
        /// The signal quality is a percentage value between 0 and 100,
        /// where 0 equals -100 dBm and 100 equals -50 dBm
        /// </summary>
        /// <param name="linkQuality"></param>
        /// <returns></returns>
        public static int ComputeRssi(uint linkQuality)
        {
            //
            // Make sure the link quality is within the range of [0..100]
            //
            if (linkQuality > 100)
                linkQuality = 100;
            //
            // Compute the RSSI based on the link quality. Link quality
            // is a percentage value between [0..100], where 0 equals -100dBm
            // and 100 equals -50dBm.
            //
            return (int)(.5 * linkQuality - 100);
        }

        /// <summary>
        /// Converts a frequency value to a designated WIFI channel
        /// </summary>
        /// <param name="frequency">frequency to convert to a channel</param>
        /// <returns>the WIFI channel at the given frequency</returns>
        public static uint ConvertToChannel(uint frequency)
        {
            uint retVal = 0;
            // 2.4 GHz
            if ((frequency > 2400000) && (frequency < 2484000))
                retVal = (frequency - 2407000) / 5000;

            if ((frequency >= 2484000) && (frequency <= 2495000))
                retVal = 14;

            // 5 GHz
            if ((frequency > 5000000) && (frequency < 5900000))
                retVal = (frequency - 5000000) / 5000;

            return retVal;
        }

        /// <summary>
        /// Converts a list of raw rate values into a list of rates 
        /// in units of Mbits/second.
        /// </summary>
        /// <param name="rates">array of raw rate values</param>
        /// <param name="rateList">output list of rates in Mbs</param>
        public static void ConvertToMbs(ushort[] rates, List<double> rateList)
        {
            ConvertToMbs(rates, rateList, false);
        }

        /// <summary>
        /// Converts a list of raw rate values into a list of rates 
        /// in units of Mbits/second.
        /// </summary>
        /// <param name="rates">array of raw rate values</param>
        /// <param name="rateList">utput list of rates in Mbs</param>
        /// <param name="isTypeN">indicates whether this is a type N network</param>
        public static void ConvertToMbs(ushort[] rates, List<double> rateList, bool isTypeN)
        {
            for (int i = 0; i < rates.Length; ++i)
            {
                if (rates[i] > 0)
                    rateList.Add((rates[i] & 0x7FFF) * 0.5);
            }
            if (isTypeN)
                rateList.Add(65.0f);
            rateList.Sort(new Comparison<double>(Compare));
        }

        /// <summary>
        /// Converts a list of raw rate values into a list of rates 
        /// in units of Mbits/second.
        /// </summary>
        /// <param name="rates">array of raw rate values</param>
        /// <param name="rateList">output list of rates in Mbs</param>
        public static void ConvertToMbs(byte[] rates, List<double> rateList)
        {
            ConvertToMbs(rates, rateList, false);
        }

        /// <summary>
        /// Converts a list of raw rate values into a list of rates 
        /// in units of Mbits/second.
        /// </summary>
        /// <param name="rates">array of raw rate values</param>
        /// <param name="rateList">utput list of rates in Mbs</param>
        /// <param name="isTypeN">
        ///   indicates whether this is a type N network
        ///   indicates whether this is a type N network
        /// </param>
        public static void ConvertToMbs(byte[] rates, List<double> rateList, bool isTypeN)
        {
            for (int i = 0; i < rates.Length; ++i)
            {
                if (rates[i] > 0)
                    rateList.Add((rates[i] & 0x7F) * 0.5);
            }
            if (isTypeN)
                rateList.Add(65.0f);
            rateList.Sort(new Comparison<double>(Compare));
        }

        /// <summary>
        /// Converts the bssType to a readable string
        /// </summary>
        /// <param name="bssType">type to convert to a string</param>
        /// <returns>string representing the bss type</returns>
        public static string ConvertToString(Wlan.Dot11BssType bssType)
        {
            string bssText;
            switch (bssType)
            {
                case Wlan.Dot11BssType.Infrastructure:
                    bssText = "Infrastructure";
                    break;
                case Wlan.Dot11BssType.Independent:
                    bssText = "Adhoc";
                    break;
                case Wlan.Dot11BssType.Any:
                    bssText = "AutoUnknown";
                    break;
                default:
                    bssText = "Unknown";
                    break;
            }

            return (bssText);
        }

        /// <summary>
        /// Creates a readable string based on the encryption algorithm and
        /// the authentication method
        /// </summary>
        /// <param name="authentication">authentication used</param>
        /// <param name="encryption">encryption used</param>
        /// <returns>a string representing the security mode</returns>
        public static string CreateSecurityString(Wlan.Dot11AuthAlgorithm authentication,
            Wlan.Dot11CipherAlgorithm encryption)
        {
            String text = authentication + "-" + encryption;
            text = text.Replace("RSNA", "WPA2");
            text = text.Replace("_PSK-CCMP", "-Personal");
            text = text.Replace("_PSK-TKIP", "-Personal");
            text = text.Replace("CCMP", "Enterprise");
            text = text.Replace("IEEE80211_", "");
            text = text.Replace("None", "");
            text = text.Replace("Open", "");
            text = text.Replace("WEP40", "WEP");
            text = text.Replace("WEP104", "WEP");
            text = text.Trim(new[] { '-' });
            if (null == text || text.Equals(String.Empty))
            {
                text = "Open";
            }
            return text;
        }

        public static AdapterVendors CreateVendorLookup()
        {
            AdapterVendors vendors = new AdapterVendors();
            try
            {
                vendors.LoadFromOui();
            }
            catch
            {
                throw new Exception(Localizer.GetString("ErrorLoadingVendorNames"));
            }

            return vendors;
        }

        public static string FindValueString(ValueString[] strings, int value)
        {
            for (int i = 0; i < strings.Length; ++i)
            {
                if (strings[i].Value.Equals(value))
                    return (strings[i].Text);
            }
            return (null);
        }

        public static float FloatParse(string s)
        {
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            return float.Parse(s, NumberStyles.Any, ci);
        }

        public static string GetServicePack()
        {
            return Environment.OSVersion.ServicePack;
        }

        public static int IntParse(string s)
        {
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            return int.Parse(s, NumberStyles.Any, ci);
        }

        public static bool IsXp()
        {
            return (Environment.OSVersion.Version.Major == 5) &&
                   (Environment.OSVersion.Version.Minor >= 1);
        }

        public static byte[] StringToByteArray(String hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public struct ValueString
        {
            internal readonly int Value;
            internal readonly string Text;

            internal ValueString(int val, string txt)
            {
                Value = val;
                Text = txt;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Compares two doubles for equality.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        private static int Compare(double d1, double d2)
        {
            if (d1 > d2)
                return 1;

            if (d1 < d2)
                return -1;
            return 0;
        }

        internal static float ConvertToFrequency(uint channel)
        {
            // 2.4 GHz
            if ((channel >= 1) && (channel <= 13))
            {
                return (channel * 5) + 2407;
            }
            if (channel == 14)
            {
                return 2484;
            }
            // 5 GHz
            if (channel >= 36)
            {
                return (channel * 5) + 5000;
            }
            // Unknown
            return 0;
        }

        private static string CropField(string data, int length)
        {
            if (data.Length > length)
                return data.Substring(0, length) + "...";
            return data;
        }

        /// <summary>
        /// Returns the next AP color 
        /// </summary>
        /// <returns></returns>
        internal static Color GetColor()
        {
            _currentIndex = (_currentIndex + 1) % ApColors.Length;
            return ApColors[_currentIndex];
        }

        /// <summary>
        /// Resets the color counter
        /// </summary>
        internal static void ResetColor()
        {
            _currentIndex = -1;
        }

        #endregion Private Methods
    }
}