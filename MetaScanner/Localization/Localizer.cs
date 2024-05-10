////////////////////////////////////////////////////////////////

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
using System.Drawing;
using System.Reflection;
using System.Resources;

namespace WirelessFireless.Localization
{
    /// <summary>
    /// Class that provides localized string resources
    /// </summary>
    public static class Localizer
    {
        #region Fields

        // underlying resource manager
        private static ResourceManager _manager;

        #endregion Fields

        #region Public Methods

        /// <summary>
        /// Returns a resource bitmap
        /// </summary>
        /// <param name="name">resource key</param>
        /// <returns>bitmap corresponding to the key.</returns>
        public static Bitmap GetBitmap(string name)
        {
            // ensure the resource manager is initialized
            if (null == _manager)
            {
                lock (typeof(ResourceManager))
                {
                    // double check
                    if (null == _manager)
                    {
                        _manager = new ResourceManager("WirelessFireless.LocalizerResources",
                            Assembly.GetExecutingAssembly());
                    }
                }
            }

            Bitmap bitmap = _manager.GetObject(name) as Bitmap;
            return bitmap;
        }

        /// <summary>
        /// Gets the localized string corresponding to the
        /// key value.
        /// </summary>
        /// <param name="key">string key</param>
        /// <param name="args">resource string format arguments</param>
        /// <returns>the localized string corresponding to the key</returns>
        public static string GetString(string key, params object[] args)
        {
            // ensure the resource manager is initialized
            if (null == _manager)
            {
                lock (typeof(ResourceManager))
                {
                    // double check
                    if (null == _manager)
                    {
                        _manager = new ResourceManager("WirelessFireless.Localization.LocalizerResources",
                                Assembly.GetExecutingAssembly());
                    }
                }
            }

            // Attempt to find the resource string
            string stringValue;
            try
            {
                lock (_manager)
                {
                    stringValue = _manager.GetString(key);
                }
                // format the string if arguments are provided
                if (null != stringValue)
                {
                    stringValue = String.Format(stringValue, args);
                }
            }
            catch
            {
                // if the string is missing, return the key
                stringValue = key;
            }

            return stringValue;
        }

        #endregion Public Methods
    }
}