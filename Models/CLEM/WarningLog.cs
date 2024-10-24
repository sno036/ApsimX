﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM
{
    /// <summary>
    /// A class to hold and question the existence of warnings generated by resources or activities
    /// Allows to track whether a particular warning has previously occurred for avoiding multiple error display etc.
    /// </summary>
    [Serializable]
    public class WarningLog
    {
        private static WarningLog instance;

        /// <summary>
        /// Obtain a static single instance of thei object
        /// </summary>
        /// <param name="maxEntries">Maximum number of entries permitted</param>
        /// <returns>A shared WarningLog</returns>
        public static WarningLog GetInstance(int maxEntries)
        {
            if(instance == null)
            {
                instance = new WarningLog(maxEntries);
            }
            else
            {
                if(maxEntries > instance.maxCount)
                {
                    instance.maxCount = maxEntries;
                }
            }
            return instance;
        }

        private int maxCount { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public WarningLog(int maxEntries)
        {
            maxCount = maxEntries;
            warningList = new List<string>();
        }

        private List<string> warningList { get; set; }

        /// <summary>
        /// Add new warning to the ists
        /// </summary>
        /// <param name="name">Name of warning</param>
        public void Add(string name)
        {
            if(!warningList.Contains(name))
            {
                warningList.Add(name.ToUpper());
            }
        }

        /// <summary>
        /// Determine if warning exists
        /// </summary>
        /// <param name="name">name of warning</param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            if (warningList.Count <= maxCount)
            {
                return warningList.Contains(name.ToUpper());
            }
            else
            {
                return true;
            }
        }
    }
}
