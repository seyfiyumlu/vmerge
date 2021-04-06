﻿using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Helpers.WeakReference;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace alexbegh.vMerge.Model.Implementation
{
    /// <summary>
    /// This class provides the settings for the package.
    /// See SetSettings, FetchSettings, Load/SaveConfiguration
    /// </summary>
    [Serializable]
    public class Settings : ISettings
    {
        private IProfilesProvider _profileProvider;

        #region Constructor
        /// <summary>
        /// Constructs an instance
        /// </summary>
        public Settings()
        {
            SerializedSettings = new SerializableDictionary<string, object>();
            Lock = new object();
            ChangeListeners = new Dictionary<string, WeakReferenceList<ISettingsChangeListener>>();
            Serializer.RegisterAssemblyTypes();
        }
        #endregion

        #region Private Properties
        /// <summary>
        /// The lock object
        /// </summary>
        private object Lock
        {
            get;
            set;
        }

        /// <summary>
        /// Remembers the dirty state
        /// </summary>
        private bool IsDirty
        {
            get;
            set;
        }

        /// <summary>
        /// The serialized settings
        /// </summary>
        internal SerializableDictionary<string, object> SerializedSettings
        {
            get;
            set;
        }

        public IProfilesProvider ProfilesSettings
        {
            get
            {
                if (_profileProvider == null)
                {
                    _profileProvider = new ProfilesProvider();                    
                }
                return _profileProvider;
            }            
        }

        /// <summary>
        /// The timer for auto-saving
        /// </summary>
        private Timer AutoSaveTimer
        {
            get;
            set;
        }

        /// <summary>
        /// True while the timer method is executing
        /// </summary>
        private static int TimerIsExecuting;

        /// <summary>
        /// List of change listeners
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        private Dictionary<string, WeakReferenceList<ISettingsChangeListener>> ChangeListeners
        {
            get;
            set;
        }
        #endregion

        #region Public Operations
        /// <summary>
        /// Sets the settings for a given key
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="data">The data</param>
        public void SetSettings(string key, object data)
        {
            lock (Lock)
            {
                if (SerializedSettings == null) SerializedSettings = new SerializableDictionary<string, object>();
                IsDirty = true;
                SerializedSettings[key] = data;
            }
            WeakReferenceList<ISettingsChangeListener> changeListeners = null;
            if (ChangeListeners.TryGetValue(key, out changeListeners))
            {
                var notify = changeListeners.CompactAndReturn();
                foreach (var item in notify)
                    item.SettingsChanged(key, data);
            }
            if (ChangeListeners.TryGetValue("", out changeListeners))
            {
                var notify = changeListeners.CompactAndReturn();
                foreach (var item in notify)
                    item.SettingsChanged(key, data);
            }
        }

        /// <summary>
        /// Fetches settings for a given key, returning null if no setting was found
        /// </summary>
        /// <typeparam name="T_Item">The type to cast the result to</typeparam>
        /// <param name="key">The key to fetch the settings for</param>
        /// <returns>null if not found, the object otherwise</returns>
        public T_Item FetchSettings<T_Item>(string key)
        {
            lock (Lock)
            {
                try
                {
                    if (SerializedSettings == null || !SerializedSettings.ContainsKey(key))
                    {
                        SimpleLogger.Log(SimpleLogLevel.Info, "Settings use default of: " + key);
                        var res = default(T_Item);
                        SerializedSettings.Add(key, res);
                        return res;
                    }
                    return (T_Item)SerializedSettings[key];
                }
                catch (Exception ex)
                {
                    SimpleLogger.Log(SimpleLogLevel.Warn, "Fail to fetch settings for: '" + key + "'  " + ex.Message);
                    return default(T_Item);
                }
            }
        }

        /// <summary>
        /// Checks if a specific key exists in the settings
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>true if existing</returns>
        public bool CheckSettingsExist(string key)
        {
            lock (Lock)
            {
                return SerializedSettings.ContainsKey(key);
            }
        }

        /// <summary>
        /// Loads the settings from a given source path
        /// </summary>
        /// <param name="source">Source file name</param>
        public void LoadSettings(string name)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "Start load settings");
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge", name + ".qvmset");
            SimpleLogger.Log(SimpleLogLevel.Info, "Load settings from: " + path);

            if (!File.Exists(path))
            {
                SerializedSettings = new SerializableDictionary<string, object>();
                return;
            }

            if (new FileInfo(path).Length == 0)
            {
                SimpleLogger.Log(SimpleLogLevel.Info, "Settings file is empty.");
                SerializedSettings = new SerializableDictionary<string, object>();
                return;
            }

            try
            {
                lock (Lock)
                {
                    SimpleLogger.Log(SimpleLogLevel.Info, "Settings file found.");
                    this.ProfilesSettings.LoadAsJson(path);
                }
            }
            catch (FileNotFoundException)
            {
                var message = "Failed to load Settings from '" + path + "': File not found";
                SimpleLogger.Log(SimpleLogLevel.Warn, message);
            }
            catch (Exception ex)
            {
                var message = "Failed to load Settings from '" + path + "': " + ex.Message;
                SimpleLogger.Log(SimpleLogLevel.Error, message);
                throw new Exception(message, ex);
            }
        }

        /// <summary>
        /// Saves the settings to a given destination path
        /// </summary>
        /// <param name="destination">Destination path</param>
        public void SaveSettings(string name)
        {
            SimpleLogger.Log(SimpleLogLevel.Info, "Start save settings");

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge" , name + ".qvmset");
            string pathBak = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge" ,  name + ".bak.qvmset");

            lock (Lock)
            {
                if (SerializedSettings == null)
                    return;

                if (File.Exists(path))
                    File.Copy(path, pathBak, true);

                SimpleLogger.Log(SimpleLogLevel.Info, "Serialize to: " + path);
                this.ProfilesSettings.SaveAsJson(path);
            }
        }

        /// <summary>
        /// Returns all available setting files
        /// </summary>
        /// <returns>List of setting files</returns>
        public IEnumerable<string> GetAvailableSettings()
        {
            foreach (var file in Directory.EnumerateFiles(
                                    Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "vMerge"),
                                    "*.qvmset", SearchOption.TopDirectoryOnly))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }

        public void AddChangeListener(string key, ISettingsChangeListener listener)
        {
            if (key == null)
                key = "";
            WeakReferenceList<ISettingsChangeListener> existing = null;
            if (!ChangeListeners.TryGetValue(key, out existing))
            {
                ChangeListeners[key] = existing = new WeakReferenceList<ISettingsChangeListener>();
            }
            existing.Add(listener);
        }

        /// <summary>
        /// Sets the state to dirty
        /// </summary>
        public void SetDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Activates auto-saving to a certain location with a specified delay
        /// </summary>
        /// <param name="path">The target path</param>
        /// <param name="milliseconds">The delay in milliseconds</param>
        public void SetAutoSave(string name, int milliseconds)
        {

            if (AutoSaveTimer != null)
            {
                AutoSaveTimer.Dispose();
            }
            AutoSaveTimer = new Timer(
                (o) =>
                {
                    SimpleLogger.Log(SimpleLogLevel.Info, "AutoSave timer event triggered");
                    if (Interlocked.CompareExchange(ref TimerIsExecuting, 1, 0) == 0)
                    {
                        try
                        {
                            if (IsDirty)
                            {
                                try
                                {
                                    SaveSettings(name);
                                    IsDirty = false;
                                }
                                catch (Exception ex)
                                {
                                    SimpleLogger.Log(ex, false, false);
                                    Debug.WriteLine(ex.ToString());
                                }
                            }
                        }
                        finally
                        {
                            TimerIsExecuting = 0;
                        }
                    }
                }, null,
                    TimeSpan.FromMilliseconds(0),
                    TimeSpan.FromMilliseconds(milliseconds));
                    
        }
        #endregion
    }
}
