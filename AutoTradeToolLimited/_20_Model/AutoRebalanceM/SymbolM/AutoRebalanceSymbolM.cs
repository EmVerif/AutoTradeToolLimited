using System;
using System.ComponentModel;
using System.Xml.Serialization;

using AutoTradeTool._20_Model.AutoRebalanceM.TopM;

namespace AutoTradeTool._20_Model.AutoRebalanceM.SymbolM
{
    public enum TradeType
    {
        Buy,
        Buying,
        InitBuying,
        Sell,
        Selling,
        InitSelling,
        Neutral,
        Init
    }

    public class AutoRebalanceSymbolM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private const double _MaxTotalExpectedRatio = 1.00;

        // リバランス閾値
        private double _AutoRebalanceThreshold = 0.02;
        public double AutoRebalanceThreshold
        {
            get
            {
                return _AutoRebalanceThreshold;
            }
            set
            {
                _AutoRebalanceThreshold = value;
                OnPropertyChanged(nameof(AutoRebalanceThreshold));
            }
        }

        private string _Symbol = "1000";
        public string Symbol
        {
            get
            {
                return _Symbol;
            }
            set
            {
                _Symbol = value;
                OnPropertyChanged(nameof(Symbol));
            }
        }

        private string _SymbolName = "";
        public string SymbolName
        {
            get
            {
                return _SymbolName;
            }
            set
            {
                _SymbolName = value;
                OnPropertyChanged(nameof(SymbolName));
            }
        }

        public double ExpectedMaxRatio
        {
            get
            {
                var ret = _MaxTotalExpectedRatio - AutoRebalanceTopM.Instance.TotalExpectedRatio + ExpectedRatio;
                return ret;
            }
        }

        private double _ExpectedRatio = 0.0;
        public double ExpectedRatioForXml
        {
            get
            {
                return _ExpectedRatio;
            }
            set
            {
                _ExpectedRatio = value;
            }
        }
        [XmlIgnore]
        public double ExpectedRatio
        {
            get
            {
                return _ExpectedRatio;
            }
            set
            {
                var prev = _ExpectedRatio;

                if ((Position != 0) && (value == 0.0))
                {
                    _ExpectedRatio = 0.001;
                }
                else
                {
                    _ExpectedRatio = value;
                }
                if (prev != 0)
                {
                    VirtualCurrentRatio *= _ExpectedRatio / prev;
                }
                else
                {
                    VirtualCurrentRatio = 0;
                }
                AdjustFlag = true;
                OnPropertyChanged(nameof(ExpectedRatio));
            }
        }

        public double CurrentRatio
        {
            get
            {
                double ret;
                var total = AutoRebalanceTopM.Instance.CurrentTotalMarketCapitalization;

                if (total == 0)
                {
                    ret = 0;
                }
                else
                {
                    ret = (double)((Position * CurrentPrice) / total);
                }

                return ret;
            }
        }

        private double _VirtualCurrentRatio = 0;
        public double VirtualCurrentRatio
        {
            get
            {
                return _VirtualCurrentRatio;
            }
            set
            {
                _VirtualCurrentRatio = value;
                OnPropertyChanged(nameof(VirtualCurrentRatio));
            }
        }

        private Decimal _Position = 0;
        public Decimal Position
        {
            get
            {
                return _Position;
            }
            set
            {
                _Position = value;
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(CurrentRatio));
            }
        }

        private Boolean? _PriceIsDown = null;
        [XmlIgnore]
        public Boolean? PriceIsDown
        {
            get
            {
                return _PriceIsDown;
            }
            private set
            {
                _PriceIsDown = value;
                OnPropertyChanged(nameof(PriceIsDown));
            }
        }

        private Decimal _CurrentPrice = 0.1M;
        public Decimal CurrentPrice
        {
            get
            {
                return _CurrentPrice;
            }
            set
            {
                var prev = _CurrentPrice;

                _CurrentPrice = value;
                if (prev != 0)
                {
                    if (prev < CurrentPrice)
                    {
                        PriceIsDown = false;
                    }
                    else if (prev > CurrentPrice)
                    {
                        PriceIsDown = true;
                    }
                }
                OnPropertyChanged(nameof(CurrentPrice));
                OnPropertyChanged(nameof(CurrentRatio));
            }
        }

        private double _ExpectedPositionBorder = 0.0;
        public double ExpectedPositionBorder
        {
            get
            {
                return _ExpectedPositionBorder;
            }
            set
            {
                _ExpectedPositionBorder = value;
                OnPropertyChanged(nameof(ExpectedPositionBorder));
            }
        }

        private TradeType _PrevTrade = TradeType.Init;
        public TradeType PrevTrade
        {
            get
            {
                return _PrevTrade;
            }
            set
            {
                _PrevTrade = value;
                OnPropertyChanged(nameof(PrevTrade));
            }
        }

        private Boolean _AdjustFlag = false;
        public Boolean AdjustFlagForXml
        {
            get
            {
                return _AdjustFlag;
            }
            set
            {
                _AdjustFlag = value;
                OnPropertyChanged(nameof(AdjustFlag));
            }
        }
        [XmlIgnore]
        public Boolean AdjustFlag
        {
            get
            {
                return _AdjustFlag;
            }
            set
            {
                if (PrevTrade == TradeType.Init)
                {
                    _AdjustFlag = false;
                }
                else if (GetAdjustNum() != 0)
                {
                    _AdjustFlag = value;
                }
                else
                {
                    _AdjustFlag = false;
                }
                OnPropertyChanged(nameof(AdjustFlag));
            }
        }

        private DateTime _PrevTradeDay = DateTime.Now;
        public DateTime PrevTradeDay
        {
            get
            {
                return _PrevTradeDay;
            }
            set
            {
                _PrevTradeDay = value;
                OnPropertyChanged(nameof(PrevTradeDay));
            }
        }

        public Boolean BuyTradeToday { get; set; } = false;
        public Boolean SellTradeToday { get; set; } = false;

        private Decimal _TradingUnit = 1;
        public Decimal TradingUnit
        {
            get
            {
                return _TradingUnit;
            }
            set
            {
                _TradingUnit = value;
            }
        }

        private string _PriceUnitType = "";
        public string PriceUnitType
        {
            get
            {
                return _PriceUnitType;
            }
            set
            {
                _PriceUnitType = value;
            }
        }

        public Boolean TradeEnableFlag
        {
            get
            {
                return _BoardInfo.TradeEnableFlag;
            }
        }

        private BoardInfo _BoardInfo = new BoardInfo();
        private Decimal _ServerPosition
        {
            get
            {
                var positionList = Communication.GetPositions();

                return positionList.Find(x => x.Symbol == Symbol).LeavesQty;
            }
        }
        private UInt32 _AdjustRetryNum = 0;

        public AutoRebalanceSymbolM()
        {
            AutoRebalanceTopM.Instance.PropertyChanged += AutoRebalanceTopModel_PropertyChanged;
        }

        public void GetSymbolInfo()
        {
            var symbolInfo = Communication.GetSymbolInfo(Symbol);
            SymbolName = symbolInfo.DisplayName;
            PriceUnitType = symbolInfo.PriceUnitType;
            TradingUnit = symbolInfo.TradingUnit;
        }

        public void GetBoardInfo()
        {
            var boardInfo = Communication.GetBoardInfo(Symbol);

            if (boardInfo != null)
            {
                _BoardInfo = boardInfo;

                // トレードが少なく、現在値と板情報の乖離が発生した場合は、板情報を現在値とする。
                if ((_BoardInfo.CurrentPrice != null) && (_BoardInfo.SellPrice != null) && (_BoardInfo.BuyPrice != null))
                {
                    if (_BoardInfo.BuyPrice > _BoardInfo.CurrentPrice)
                    {
                        CurrentPrice = _BoardInfo.BuyPrice.Value;
                    }
                    else if (_BoardInfo.SellPrice < _BoardInfo.CurrentPrice)
                    {
                        CurrentPrice = _BoardInfo.SellPrice.Value;
                    }
                    else
                    {
                        CurrentPrice = _BoardInfo.CurrentPrice.Value;
                    }
                }
                else if (_BoardInfo.BuyPrice != null)
                {
                    CurrentPrice = _BoardInfo.BuyPrice.Value;
                }
            }
        }

        public void Rebalance()
        {
            RebalancePreprocess();
            // TODO: トレード対象が全てトレード可能状態になってからトレードするか、個別判断するか、別の手法があるか。。。
            //if (!AutoRebalanceTopM.Instance.AllTradeEnable)
            if (!_BoardInfo.TradeEnableFlag)
            {
                return;
            }
            if (AdjustFlag)
            {
                // キャッシュの増減や、割合変更による調整売買
                // ここで発生した売買は、PrevTradeに反映させない。
                AdjustTrade();
            }
            else
            {
                // 通常リバランス動作
                RebalanceNormal();
                VirtualCurrentRatio = CurrentRatio;
            }
        }

        private void RebalancePreprocess()
        {
            GetBoardInfo();
            if (
                (PrevTradeDay.Year != DateTime.Now.Year) ||
                (PrevTradeDay.Month != DateTime.Now.Month) ||
                (PrevTradeDay.Day != DateTime.Now.Day)
            )
            {
                // 土日祝に起動してしまった時のガード条件
                if (TradeEnableFlag)
                {
                    BuyTradeToday = false;
                    SellTradeToday = false;
                    PrevTradeDay = DateTime.Now;
                }
            }
        }

        private void AdjustTrade()
        {
            var prevPosition = Position;
            var tradeReqNum = GetAdjustNum();
            Decimal contractNum;
            Decimal prevCash;
            Decimal postCash;

            if (prevPosition == 0)
            {
                throw new Exception("調整機能でここに来ることが無いはず。来た場合は、アルゴミスの可能性大。");
            }
            if (tradeReqNum == 0)
            {
                // 調整終了
                AdjustFlag = false;
                _AdjustRetryNum = 0;
                return;
            }
            else if (CheckIsOverDiffPrice() || (BuyTradeToday && SellTradeToday))
            {
                // スプレッド広すぎのため、一旦トレードを中断。
                // 売⇒買⇒売、あるいは、買⇒売⇒買禁止。
                return;
            }
            else if (tradeReqNum > 0)
            {
                if (_BoardInfo.SellPrice == null)
                {
                    // 売り板が無いため、終了。
                    return;
                }
                // 一気にトレードすると、誤差が大きくなるため、半分ずつトレードする。
                Decimal tradeNum = Math.Truncate((tradeReqNum + 1) / 2);

                if (!CheckBuyEnable(tradeNum))
                {
                    if (!CheckBuyEnable(1))
                    {
                        // 現金不足の場合は、一旦スルー。
                        _AdjustRetryNum++;
                        if (_AdjustRetryNum > 100)
                        {
                            AdjustFlag = false;
                            _AdjustRetryNum = 0;
                        }
                        return;
                    }
                    Communication.Buy(Symbol, _BoardInfo.SellPrice.Value, TradingUnit, out contractNum, out prevCash, out postCash);
                }
                else
                {
                    Communication.Buy(Symbol, _BoardInfo.SellPrice.Value, tradeNum * TradingUnit, out contractNum, out prevCash, out postCash);
                }
            }
            else
            {
                if (_BoardInfo.BuyPrice == null)
                {
                    // 買い板が無いため、終了。
                    return;
                }
                // 一気にトレードすると、誤差が大きくなるため、半分ずつトレードする。
                Decimal tradeNum = Math.Truncate((-tradeReqNum + 1) / 2);

                CheckSell(tradeNum);
                Communication.Sell(Symbol, _BoardInfo.BuyPrice.Value, tradeNum * TradingUnit, out contractNum, out prevCash, out postCash);
            }

            Position = _ServerPosition;
            AutoRebalanceTopM.Instance.AddCashAfterTrade(postCash - prevCash);
            AutoRebalanceTopM.Instance.SetTradeLog(
                Symbol + "（調整トレード）：\n" +
                "\t" + @"　　　　　　日時：" + DateTime.Now + "\n" +
                "\t" + @"　ポジション変化：" + prevPosition + @"->" + Position + "\n" +
                "\t" + @"ポジション変化量：" + (Position - prevPosition) + "\n" +
                "\t" + @"　　　　現金変化：" + prevCash + @"->" + postCash + "\n" +
                "\t" + @"　　　現金変化量：" + (postCash - prevCash) + "\n" +
                "\t" + @"　　　現在買価格：" + _BoardInfo.BuyPrice + "\n" +
                "\t" + @"　　　現在売価格：" + _BoardInfo.SellPrice + "\n"
            );
            ExpectedPositionBorder *= (double)Position / (double)prevPosition;
            if (contractNum == Math.Abs(tradeReqNum))
            {
                AdjustFlag = false;
                _AdjustRetryNum = 0;
                if (tradeReqNum > 0)
                {
                    BuyTradeToday = true;
                }
                else
                {
                    SellTradeToday = true;
                }
            }
            AutoRebalanceTopM.Instance.Save();
            CheckContract(contractNum, prevPosition);
        }

        private Decimal GetAdjustNum()
        {
            var diffRatio = VirtualCurrentRatio - CurrentRatio;
            var diffCap = (Decimal)diffRatio * AutoRebalanceTopM.Instance.CurrentTotalMarketCapitalization;
            var ret = Math.Truncate(diffCap / CurrentPrice / TradingUnit + 0.000001M);

            return ret;
        }

        private void RebalanceNormal()
        {
            var prevPosition = Position;
            var tradeReqNum = GetTradeNum();
            Decimal contractNum;
            Decimal prevCash;
            Decimal postCash;

            if ((tradeReqNum == 0) || CheckIsOverDiffPrice() || (BuyTradeToday && SellTradeToday))
            {
                // リバランス不要。
                // スプレッド広すぎのため、一旦トレードを中断。
                // 売⇒買⇒売、あるいは、買⇒売⇒買禁止。
                return;
            }
            else if (tradeReqNum > 0)
            {
                // 一気にトレードすると、誤差が大きくなるため、半分ずつトレードする。
                Decimal tradeNum = Math.Truncate((tradeReqNum + 1) / 2);

                if (!CheckBuyEnable(tradeNum) || (_BoardInfo.SellPrice == null))
                {
                    // 現金不足や、売り板が無い場合は、一旦スルー。
                    return;
                }
                Communication.Buy(Symbol, _BoardInfo.SellPrice.Value, tradeNum * TradingUnit, out contractNum, out prevCash, out postCash);
                if ((PrevTrade == TradeType.Init) || (PrevTrade == TradeType.InitBuying))
                {
                    PrevTrade = TradeType.InitBuying;
                }
                else
                {
                    PrevTrade = TradeType.Buying;
                }
            }
            else
            {
                // 一気にトレードすると、誤差が大きくなるため、半分ずつトレードする。
                Decimal tradeNum = Math.Truncate((-tradeReqNum + 1) / 2);

                if (_BoardInfo.BuyPrice == null)
                {
                    // 買い板が無い場合は、一旦スルー。
                    return;
                }
                CheckSell(tradeNum);
                Communication.Sell(Symbol, _BoardInfo.BuyPrice.Value, tradeNum * TradingUnit, out contractNum, out prevCash, out postCash);
                if ((PrevTrade == TradeType.Init) || (PrevTrade == TradeType.InitSelling))
                {
                    PrevTrade = TradeType.InitSelling;
                }
                else
                {
                    PrevTrade = TradeType.Selling;
                }
            }

            Position = _ServerPosition;
            AutoRebalanceTopM.Instance.AddCashAfterTrade(postCash - prevCash);
            AutoRebalanceTopM.Instance.SetTradeLog(
                Symbol + "（リバランストレード）：\n" +
                "\t" + @"　　　　　　日時：" + DateTime.Now + "\n" +
                "\t" + @"　ポジション変化：" + prevPosition + @"->" + Position + "\n" +
                "\t" + @"ポジション変化量：" + (Position - prevPosition) + "\n" +
                "\t" + @"　　　　現金変化：" + prevCash + @"->" + postCash + "\n" +
                "\t" + @"　　　現金変化量：" + (postCash - prevCash) + "\n" +
                "\t" + @"　　　現在買価格：" + _BoardInfo.BuyPrice + "\n" +
                "\t" + @"　　　現在売価格：" + _BoardInfo.SellPrice + "\n"
            );
            AutoRebalanceTopM.Instance.Save();
            CheckContract(contractNum, prevPosition);
        }

        private Boolean CheckIsOverDiffPrice()
        {
            Boolean overDiffPrice;

            if ((_BoardInfo.SellPrice != null) && (_BoardInfo.BuyPrice != null))
            {
                Decimal priceUnit = Communication.CalcCurrentPriceUnit(PriceUnitType, _BoardInfo.BuyPrice.Value);

                if ((_BoardInfo.SellPrice - _BoardInfo.BuyPrice) > (priceUnit * 2))
                {
                    Decimal spreadRatio = (_BoardInfo.SellPrice.Value - _BoardInfo.BuyPrice.Value) / _BoardInfo.SellPrice.Value;

                    if (spreadRatio > 0.005M)
                    {
                        overDiffPrice = true;
                    }
                    else
                    {
                        overDiffPrice = false;
                    }
                }
                else
                {
                    overDiffPrice = false;
                }
            }
            else
            {
                overDiffPrice = true;
            }

            return overDiffPrice;
        }

        private Boolean CheckBuyEnable(decimal tradeNum)
        {
            Decimal mergin = 1.01M;// TODO: 手数料１％と仮定。
            Boolean ret;

            if (AutoRebalanceTopM.Instance.CurrentCash < (tradeNum * TradingUnit * _BoardInfo.SellPrice * mergin))
            {
                ret = false;
            }
            else
            {
                ret = true;
            }

            return ret;
        }

        private void CheckSell(decimal tradeNum)
        {
            if (Position < (tradeNum * TradingUnit))
            {
                // ポジション不足の場合は、アルゴミスのため、例外。
                throw new Exception(
                    "ポジション不足で売れない。アルゴミスの可能性大。\n" +
                    "\tポジション⇒" + Position + "\n" +
                    "\t　売要求数⇒" + (tradeNum * TradingUnit)
                );
            }
        }

        private void CheckContract(decimal contractNum, decimal prevPosition)
        {
            if (contractNum != Math.Abs(Position - prevPosition))
            {
                throw new Exception("トレード後処理が想定外動作。\n約定数：" + contractNum + "\nポジション変化：" + Math.Abs(Position - prevPosition));
            }
        }

        private Decimal GetTradeNum()
        {
            var total = (double)AutoRebalanceTopM.Instance.CurrentTotalMarketCapitalization;
            var expectedPosition = total * ExpectedRatio / (double)CurrentPrice;
            if (expectedPosition == 0)
            {
                // ０％は全売り
                return (Position / TradingUnit);
            }
            var diffRatio = Math.Abs((ExpectedRatio - CurrentRatio) / ExpectedRatio);
            double reboundRatio;
            Decimal ret = Math.Floor(((Decimal)expectedPosition - Position) / TradingUnit);

            switch (PrevTrade)
            {
                case TradeType.Buy:
                    ExpectedPositionBorder = Math.Max(ExpectedPositionBorder, expectedPosition);
                    reboundRatio = (ExpectedPositionBorder - expectedPosition) / ExpectedPositionBorder;
                    if (diffRatio < AutoRebalanceThreshold)
                    {
                        ret = 0;
                    }
                    else if ((ret > 0) && (reboundRatio < (AutoRebalanceThreshold / 2.0)))
                    {
                        ret = 0;
                    }
                    break;
                case TradeType.Sell:
                    ExpectedPositionBorder = Math.Min(ExpectedPositionBorder, expectedPosition);
                    reboundRatio = (expectedPosition - ExpectedPositionBorder) / ExpectedPositionBorder;
                    if (diffRatio < AutoRebalanceThreshold)
                    {
                        ret = 0;
                    }
                    else if ((ret < 0) && (reboundRatio < (AutoRebalanceThreshold / 2.0)))
                    {
                        ret = 0;
                    }
                    break;
                case TradeType.Buying:
                    if (ret <= 0)
                    {
                        ExpectedPositionBorder = expectedPosition;
                        PrevTrade = TradeType.Buy;
                        BuyTradeToday = true;
                        ret = 0;
                    }
                    break;
                case TradeType.Selling:
                    if (ret >= 0)
                    {
                        ExpectedPositionBorder = expectedPosition;
                        PrevTrade = TradeType.Sell;
                        SellTradeToday = true;
                        ret = 0;
                    }
                    break;
                case TradeType.InitBuying:
                    if (ret <= 0)
                    {
                        PrevTrade = TradeType.Neutral;
                        BuyTradeToday = true;
                        ret = 0;
                    }
                    break;
                case TradeType.InitSelling:
                    if (ret >= 0)
                    {
                        PrevTrade = TradeType.Neutral;
                        SellTradeToday = true;
                        ret = 0;
                    }
                    break;
                case TradeType.Init:
                    if (ret == 0)
                    {
                        PrevTrade = TradeType.Neutral;
                    }
                    break;
                case TradeType.Neutral:
                    if (diffRatio < AutoRebalanceThreshold)
                    {
                        ret = 0;
                    }
                    break;
            }

            return ret;
        }

        private void AutoRebalanceTopModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AutoRebalanceTopM.TotalExpectedRatio):
                    OnPropertyChanged(nameof(ExpectedMaxRatio));
                    break;
                case nameof(AutoRebalanceTopM.CurrentTotalMarketCapitalization):
                    OnPropertyChanged(nameof(CurrentRatio));
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
