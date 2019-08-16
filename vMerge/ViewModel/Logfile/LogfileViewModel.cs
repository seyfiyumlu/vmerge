using alexbegh.Utility.Helpers.Logging;
using alexbegh.vMerge.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace alexbegh.vMerge.ViewModel.Logfile
{
    class LogfileViewModel : INotifyPropertyChanged
    {
        private string _exceptionSL;
        public string ExceptionSL
        {
            get
            {
                return _exceptionSL;
            }
            set
            {
                if(_exceptionSL != value)
                {
                    _exceptionSL = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string LogFilePath
        {
            get
            {
                return SimpleLogger.LogFilePath;
            }
        }

        private string _logSL;
        public string LogSL
        {
            get
            {
                return _logSL;
            }
            set
            {
                if (_logSL != value)
                {
                    _logSL = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public LogfileViewModel()
        {
            try
            {
                string logFile = SimpleLogger.GetLogFileContent(SimpleLogger.LogFilePath);
                LogSL = logFile;

                string exceptions = SimpleLogger.GetExceptions(SimpleLogger.ExceptionMessage);
                ExceptionSL = exceptions;                
            } catch (Exception ex)
            {
                ExceptionSL = String.Format("An exception occurred in vMerge.\nDetail:\nException '{0}' occurred, Stacktrace:\n{1}", ex.Message, ex.StackTrace);
                SimpleLogger.Log(ex, false, false);
            }
        }
    }
}
