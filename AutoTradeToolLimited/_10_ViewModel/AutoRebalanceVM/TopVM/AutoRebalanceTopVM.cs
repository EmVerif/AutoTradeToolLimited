using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

using AutoTradeTool._00_View.AutoRebalance;
using AutoTradeTool._10_ViewModel.AutoRebalanceVM.SymbolVM;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.TopVM
{
    class AutoRebalanceTopVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public StartStopButton StartStopButton { get; } = new StartStopButton();
        public AddButton AddButton { get; } = new AddButton();
        public UpdateButton UpdateButton { get; } = new UpdateButton();


        public ObservableCollection<OneSymbol> Symbols { get; } = new ObservableCollection<OneSymbol>();

        public Boolean IsReadOnly
        {
            get
            {
                return AutoRebalanceTopM.Instance.AutoRebalanceRunning;
            }
        }
        public Boolean IsEditable
        {
            get
            {
                return !AutoRebalanceTopM.Instance.AutoRebalanceRunning;
            }
        }
        public Boolean IsEnable
        {
            get
            {
                return MainWindowM.Instance.IsConnected;
            }
        }

        public string CurrentCash
        {
            get
            {
                return AutoRebalanceTopM.Instance.CurrentCash.ToString();
            }
            set
            {
                AutoRebalanceTopM.Instance.PutOrPullCash(Convert.ToDecimal(value));
            }
        }

        public string CurrentTotalMarketCapitalization
        {
            get
            {
                return @"時価総額：\" + ((UInt64)AutoRebalanceTopM.Instance.CurrentTotalMarketCapitalization).ToString();
            }
        }

        public AutoRebalanceTopVM()
        {
            SyncDatabase();
            AutoRebalanceTopM.Instance.PropertyChanged += Model_PropertyChanged;
            MainWindowM.Instance.PropertyChanged += MainWindowModel_PropertyChanged;
        }

        private void MainWindowModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowM.IsConnected):
                    OnPropertyChanged(nameof(this.IsEnable));
                    break;
                default:
                    break;
            }
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AutoRebalanceTopM.Database):
                    SyncDatabase();
                    OnPropertyChanged(nameof(this.Symbols));
                    break;
                case nameof(AutoRebalanceTopM.AutoRebalanceRunning):
                    OnPropertyChanged(nameof(this.IsReadOnly));
                    OnPropertyChanged(nameof(this.IsEditable));
                    break;
                case nameof(AutoRebalanceTopM.CurrentCash):
                    OnPropertyChanged(nameof(this.CurrentCash));
                    break;
                case nameof(AutoRebalanceTopM.CurrentTotalMarketCapitalization):
                    OnPropertyChanged(nameof(this.CurrentTotalMarketCapitalization));
                    break;
                default:
                    break;
            }
        }

        private void SyncDatabase()
        {
            var modelCount = AutoRebalanceTopM.Instance.Database.Count;
            var viewCount = Symbols.Count;

            if (modelCount > viewCount)
            {
                for (int idx = 0; idx < (modelCount - viewCount); idx++)
                {
                    Symbols.Add(new OneSymbol());
                }
            }
            else
            {
                for (int idx = 0; idx < (viewCount - modelCount); idx++)
                {
                    Symbols.RemoveAt(0);
                }
            }
            for (int idx = 0; idx < modelCount; idx++)
            {
                ((AutoRebalanceSymbolVM)Symbols[idx].DataContext).RefModel = AutoRebalanceTopM.Instance.Database[idx];
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
