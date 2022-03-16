using System;
using System.ComponentModel;

using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.MainWindowVM
{
    class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Password
        {
            set
            {
                MainWindowM.Instance.Password = value;
            }
        }

        public Boolean IsToolPasswordEditable
        {
            get
            {
                return !MainWindowM.Instance.IsConnected;
            }
        }

        public string TradePassword
        {
            set
            {
                MainWindowM.Instance.TradePassword = value;
            }
        }

        public Boolean IsTradePasswordEditable
        {
            get
            {
                Boolean ret;

                ret = MainWindowM.Instance.IsConnected &&
                    !AutoRebalanceTopM.Instance.AutoRebalanceRunning;

                return ret;
            }
        }

        public OpenButton OpenButton { get; } = new OpenButton();

        public Boolean OpenButtonEnable
        {
            get
            {
                return !MainWindowM.Instance.IsConnected;
            }
        }

        public MainWindowVM()
        {
            AutoRebalanceTopM.Instance.PropertyChanged += AutoRebalanceTopModel_PropertyChanged;
            MainWindowM.Instance.PropertyChanged += MainWindowModel_PropertyChanged;
        }

        private void MainWindowModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowM.IsConnected):
                    OnPropertyChanged(nameof(this.IsToolPasswordEditable));
                    OnPropertyChanged(nameof(this.IsTradePasswordEditable));
                    OnPropertyChanged(nameof(this.OpenButtonEnable));
                    break;
                default:
                    break;
            }
        }

        private void AutoRebalanceTopModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AutoRebalanceTopM.AutoRebalanceRunning):
                    OnPropertyChanged(nameof(this.IsTradePasswordEditable));
                    break;
                default:
                    break;
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
