using System;
using System.ComponentModel;

namespace AutoTradeTool._20_Model.MainWindowM
{
    class MainWindowM : INotifyPropertyChanged
    {
        public static MainWindowM Instance = new MainWindowM();
        public event PropertyChangedEventHandler PropertyChanged;

        private Boolean _IsConnected = false;
        public Boolean IsConnected
        {
            get
            {
                return _IsConnected;
            }
            set
            {
                _IsConnected = value;
                OnPropertyChanged(nameof(this.IsConnected));
            }
        }

        public string Password
        {
            set
            {
                Communication.Password = value;
            }
        }

        public string TradePassword
        {
            set
            {
                Communication.TradePassword = value;
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
