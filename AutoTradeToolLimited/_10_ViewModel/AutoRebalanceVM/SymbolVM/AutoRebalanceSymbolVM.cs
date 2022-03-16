using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using AutoTradeTool._20_Model.AutoRebalanceM.SymbolM;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.SymbolVM
{
    class AutoRebalanceSymbolVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Brush BackgroundColor
        {
            get
            {
                Brush ret;
                var idx = AutoRebalanceTopM.Instance.Database.FindIndex(x => x == RefModel);

                if ((idx % 2) == 0)
                {
                    ret = new SolidColorBrush(Colors.LightPink);
                }
                else
                {
                    ret = new SolidColorBrush(Colors.LightGreen);
                }

                return ret;
            }
        }
        public string Symbol
        {
            get
            {
                return RefModel.Symbol;
            }
            set
            {
                RefModel.Symbol = value;
            }
        }
        public string SymbolName
        {
            get
            {
                return "名称：" + _RefModel.SymbolName;
            }
        }
        public Decimal ExpectedMaxPercent
        {
            get
            {
                return (Decimal)(_RefModel.ExpectedMaxRatio * 100);
            }
        }
        public string ExpectedPercent
        {
            get
            {
                return (_RefModel.ExpectedRatio * 100).ToString("F1");
            }
            set
            {
                _RefModel.ExpectedRatio = Convert.ToDouble(value) / 100;
            }
        }
        public string AutoRebalanceThresholdMinPercent
        {
            get
            {
                return (_RefModel.AutoRebalanceThreshold * 100).ToString("F1");
            }
            set
            {
                _RefModel.AutoRebalanceThreshold = Convert.ToDouble(value) / 100;
            }
        }
        public string CurrentPercent
        {
            get
            {
                return (_RefModel.CurrentRatio * 100).ToString("F3") + "／";
            }
        }
        public string Position
        {
            get
            {
                return @"株数：" + _RefModel.Position;
            }
        }
        public string CurrentPrice
        {
            get
            {
                return @"値段：\" + _RefModel.CurrentPrice;
            }
        }
        public Brush PriceColor
        {
            get
            {
                Brush ret;

                if (_RefModel.PriceIsDown == null)
                {
                    ret = new SolidColorBrush(Colors.Transparent);
                }
                else if (_RefModel.PriceIsDown == true)
                {
                    ret = new SolidColorBrush(Colors.LightBlue);
                }
                else
                {
                    ret = new SolidColorBrush(Colors.Red);
                }

                return ret;
            }
        }
        public string CurrentMarketCapitalization
        {
            get
            {
                return @"合計：\" + (UInt64)(_RefModel.CurrentPrice * _RefModel.Position);
            }
        }
        public string State
        {
            get
            {
                string ret;

                if (_RefModel.AdjustFlag)
                {
                    ret = @"状態：調整中";
                }
                else
                {
                    switch (_RefModel.PrevTrade)
                    {
                        case TradeType.Buy:
                            ret = @"状態：前回買";
                            break;
                        case TradeType.Sell:
                            ret = @"状態：前回売";
                            break;
                        case TradeType.InitBuying:
                        case TradeType.InitSelling:
                        case TradeType.Buying:
                        case TradeType.Selling:
                            ret = @"状態：売買中";
                            break;
                        case TradeType.Neutral:
                            ret = @"状態：開始直後";
                            break;
                        case TradeType.Init:
                        default:
                            ret = @"状態：未トレード";
                            break;
                    }
                }

                return ret;
            }
        }
        public Boolean IsEditable
        {
            get
            {
                return MainWindowM.Instance.IsConnected &&
                    !AutoRebalanceTopM.Instance.AutoRebalanceRunning;
            }
        }

        public Boolean IsSymbolReadOnly
        {
            get
            {
                Boolean ret;

                if (_RefModel.Position != 0)
                {
                    ret = true;
                }
                else
                {
                    ret = false;
                }

                return ret;
            }
        }
        public Visibility UpButtonVisibility
        {
            get
            {
                Visibility ret;

                if (AutoRebalanceTopM.Instance.Database.IndexOf(RefModel) == 0)
                {
                    ret = Visibility.Hidden;
                }
                else
                {
                    ret = Visibility.Visible;
                }

                return ret;
            }
        }
        public Visibility DownButtonVisibility
        {
            get
            {
                Visibility ret;

                if (AutoRebalanceTopM.Instance.Database.IndexOf(RefModel) == (AutoRebalanceTopM.Instance.Database.Count - 1))
                {
                    ret = Visibility.Hidden;
                }
                else
                {
                    ret = Visibility.Visible;
                }

                return ret;
            }
        }

        private AutoRebalanceSymbolM _RefModel = new AutoRebalanceSymbolM();
        public AutoRebalanceSymbolM RefModel
        {
            get
            {
                return _RefModel;
            }
            set
            {
                _RefModel.PropertyChanged -= OneSymbol_PropertyChanged;
                _RefModel = value;
                UpButton = new UpButton(value);
                DownButton = new DownButton(value);
                DelButton = new DelButton(value);
                value.PropertyChanged += OneSymbol_PropertyChanged;
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(Symbol));
                OnPropertyChanged(nameof(SymbolName));
                OnPropertyChanged(nameof(AutoRebalanceThresholdMinPercent));
                OnPropertyChanged(nameof(ExpectedMaxPercent));
                OnPropertyChanged(nameof(ExpectedPercent));
                OnPropertyChanged(nameof(CurrentPercent));
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(CurrentPrice));
                OnPropertyChanged(nameof(CurrentMarketCapitalization));
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(UpButton));
                OnPropertyChanged(nameof(DownButton));
                OnPropertyChanged(nameof(DelButton));
                OnPropertyChanged(nameof(IsSymbolReadOnly));
                OnPropertyChanged(nameof(UpButtonVisibility));
                OnPropertyChanged(nameof(DownButtonVisibility));
            }
        }

        public UpButton UpButton { get; private set; }
        public DownButton DownButton { get; private set; }
        public DelButton DelButton { get; private set; }

        public AutoRebalanceSymbolVM()
        {
            AutoRebalanceTopM.Instance.PropertyChanged += AutoRebalanceTopModel_PropertyChanged;
            MainWindowM.Instance.PropertyChanged += MainWindowModel_PropertyChanged;
        }

        private void MainWindowModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowM.IsConnected):
                    OnPropertyChanged(nameof(this.IsEditable));
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
                    OnPropertyChanged(nameof(this.IsEditable));
                    break;
                case nameof(AutoRebalanceTopM.Database):
                    OnPropertyChanged(nameof(this.BackgroundColor));
                    OnPropertyChanged(nameof(this.UpButtonVisibility));
                    OnPropertyChanged(nameof(this.DownButtonVisibility));
                    break;
                default:
                    break;
            }
        }

        private void OneSymbol_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_RefModel.Symbol):
                    OnPropertyChanged(nameof(this.Symbol));
                    break;
                case nameof(_RefModel.SymbolName):
                    OnPropertyChanged(nameof(this.SymbolName));
                    break;
                case nameof(_RefModel.AutoRebalanceThreshold):
                    OnPropertyChanged(nameof(AutoRebalanceThresholdMinPercent));
                    break;
                case nameof(_RefModel.ExpectedMaxRatio):
                    OnPropertyChanged(nameof(this.ExpectedMaxPercent));
                    break;
                case nameof(_RefModel.ExpectedRatio):
                    OnPropertyChanged(nameof(this.ExpectedPercent));
                    break;
                case nameof(_RefModel.CurrentRatio):
                    OnPropertyChanged(nameof(this.CurrentPercent));
                    break;
                case nameof(_RefModel.CurrentPrice):
                    OnPropertyChanged(nameof(this.CurrentPrice));
                    OnPropertyChanged(nameof(this.CurrentMarketCapitalization));
                    break;
                case nameof(_RefModel.Position):
                    OnPropertyChanged(nameof(this.Position));
                    OnPropertyChanged(nameof(this.CurrentMarketCapitalization));
                    OnPropertyChanged(nameof(this.IsSymbolReadOnly));
                    break;
                case nameof(_RefModel.PriceIsDown):
                    OnPropertyChanged(nameof(this.PriceColor));
                    break;
                case nameof(_RefModel.PrevTrade):
                case nameof(_RefModel.AdjustFlag):
                    OnPropertyChanged(nameof(this.State));
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
