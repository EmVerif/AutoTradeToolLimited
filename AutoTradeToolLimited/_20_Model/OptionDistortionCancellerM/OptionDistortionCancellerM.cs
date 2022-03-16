using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTradeTool._20_Model.OptionDistortionCancellerM
{
    enum TradePattern
    {
        None,
        HighPutBuy_LowPutSell_LowCallBuy_HighCallSell_0,
        HighPutBuy_LowPutSell_LowCallBuy_HighCallSell_1,
        HighPutSell_LowPutBuy_LowCallSell_HighCallBuy_0,
        HighPutSell_LowPutBuy_LowCallSell_HighCallBuy_1
    }

    class OptionDistortionCancellerM : INotifyPropertyChanged
    {
        public static OptionDistortionCancellerM Instance = new OptionDistortionCancellerM();

        public event PropertyChangedEventHandler PropertyChanged;

        private Boolean _ObserveRunning = false;
        public Boolean ObserveRunning
        {
            get
            {
                return _ObserveRunning;
            }
            private set
            {
                _ObserveRunning = value;
                OnPropertyChanged(nameof(ObserveRunning));
            }
        }

        private Decimal _NK225MiniPrice = 0;
        public Decimal NK225MiniPrice
        {
            get
            {
                return _NK225MiniPrice;
            }
            private set
            {
                _NK225MiniPrice = value;
                OnPropertyChanged(nameof(NK225MiniPrice));
            }
        }

        private string _ResultDisplay = "";
        public string ResultDisplay
        {
            get
            {
                return _ResultDisplay;
            }
            private set
            {
                _ResultDisplay = value;
                OnPropertyChanged(nameof(ResultDisplay));
            }
        }

        public List<BoardInfo> CallBoardInfo { get; private set; } = new List<BoardInfo>();
        public List<BoardInfo> PutBoardInfo { get; private set; } = new List<BoardInfo>();

        private string _NK225MiniSymbol;
        private Dictionary<Decimal, List<string>> _OptionPriceSymbolDict = new Dictionary<decimal, List<string>>();
        private CancellationTokenSource _ObserveCancelTaken;

        private TradePattern _TradePattern = TradePattern.None;
        private Decimal _Profit = 0;
        private Decimal _SafeLevel = 0;
        private Decimal _MaxProfit = Decimal.MinValue;
        private Decimal _SafeLevelAtMaxProfit = 0;

        public async Task StartObserveAsync()
        {
            ObserveRunning = true;
            _ObserveCancelTaken = new CancellationTokenSource();
            var token = _ObserveCancelTaken.Token;

            _OptionPriceSymbolDict = new Dictionary<decimal, List<string>>();
            _NK225MiniSymbol = Communication.GetFutureSymbol("NK225mini");
            Communication.RegisterFutureOption(new List<string>() { _NK225MiniSymbol });

            await Task.Run(() =>
            {
                while (true)
                {
                    GetNk225MiniPrice();
                    var upperPrice = CalcOptionUpperPrice();
                    GetBoardInfo(upperPrice);
                    CalcResult();
                    Task.Delay(100).Wait();
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });
        }

        private void GetNk225MiniPrice()
        {
            var info = Communication.GetBoardInfo(_NK225MiniSymbol, "2");

            if (info.CurrentPrice != null)
            {
                NK225MiniPrice = info.CurrentPrice.Value;
            }
        }

        private Decimal CalcOptionUpperPrice()
        {
            Decimal upper = Math.Ceiling((NK225MiniPrice + 250) / 125) * 125;

            if (!_OptionPriceSymbolDict.ContainsKey(upper))
            {
                RegisterOption(upper);
            }
            if (!_OptionPriceSymbolDict.ContainsKey(upper - 125))
            {
                RegisterOption(upper - 125);
            }
            if (!_OptionPriceSymbolDict.ContainsKey(upper - 500))
            {
                RegisterOption(upper - 500);
            }
            if (!_OptionPriceSymbolDict.ContainsKey(upper - 625))
            {
                RegisterOption(upper - 625);
            }

            return upper;
        }

        private void RegisterOption(Decimal inPrice)
        {
            var putSymbol = Communication.GetOptionSymbol("P", (int)inPrice);
            var callSymbol = Communication.GetOptionSymbol("C", (int)inPrice);
            var symbolList = new List<string> { putSymbol, callSymbol };

            Communication.RegisterFutureOption(symbolList);
            _OptionPriceSymbolDict.Add(inPrice, symbolList);
        }

        private void GetBoardInfo(decimal inUpperPrice)
        {
            PutBoardInfo = new List<BoardInfo>();
            PutBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice][0], "2"));
            PutBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 125][0], "2"));
            PutBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 500][0], "2"));
            PutBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 625][0], "2"));
            CallBoardInfo = new List<BoardInfo>();
            CallBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice][1], "2"));
            CallBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 125][1], "2"));
            CallBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 500][1], "2"));
            CallBoardInfo.Add(Communication.GetBoardInfo(_OptionPriceSymbolDict[inUpperPrice - 625][1], "2"));
            OnPropertyChanged(nameof(PutBoardInfo));
            OnPropertyChanged(nameof(CallBoardInfo));
        }

        private void CalcResult()
        {
            Decimal maxProfit;
            Decimal safeLevel;
            TradePattern pattern = TradePattern.None;
            string resultDisplay = "";

            if ((PutBoardInfo.Count < 4) || (CallBoardInfo.Count < 4))
            {
                ResultDisplay = resultDisplay;
                _TradePattern = pattern;

                return;
            }
            foreach (var info in PutBoardInfo)
            {
                if (
                    (info.BuyPrice == null) || (info.SellPrice == null) || (info.BuyPrice == 0) || (info.SellPrice == 0) ||
                    (info.Buy2Price == null) || (info.Sell2Price == null) || (info.Buy2Price == 0) || (info.Sell2Price == 0)
                )
                {
                    ResultDisplay = resultDisplay;
                    _TradePattern = pattern;

                    return;
                }
            }
            foreach (var info in CallBoardInfo)
            {
                if (
                    (info.BuyPrice == null) || (info.SellPrice == null) || (info.BuyPrice == 0) || (info.SellPrice == 0) ||
                    (info.Buy2Price == null) || (info.Sell2Price == null) || (info.Buy2Price == 0) || (info.Sell2Price == 0)
                )
                {
                    ResultDisplay = resultDisplay;
                    _TradePattern = pattern;

                    return;
                }
            }
            var a = 500M
                - PutBoardInfo[0].SellPrice.Value
                + CallBoardInfo[0].BuyPrice.Value
                + PutBoardInfo[2].BuyPrice.Value
                - CallBoardInfo[2].SellPrice.Value;
            var aSafe = PutBoardInfo[0].SellQty.Value + PutBoardInfo[0].Sell2Qty.Value;
            aSafe = Math.Min(aSafe, CallBoardInfo[0].BuyQty.Value + CallBoardInfo[0].Buy2Qty.Value);
            aSafe = Math.Min(aSafe, PutBoardInfo[2].BuyQty.Value + PutBoardInfo[2].Buy2Qty.Value);
            aSafe = Math.Min(aSafe, CallBoardInfo[2].SellQty.Value + CallBoardInfo[2].Sell2Qty.Value);
            var b = 500M
                - PutBoardInfo[1].SellPrice.Value
                + CallBoardInfo[1].BuyPrice.Value
                + PutBoardInfo[3].BuyPrice.Value
                - CallBoardInfo[3].SellPrice.Value;
            var bSafe = PutBoardInfo[1].SellQty.Value + PutBoardInfo[1].Sell2Qty.Value;
            bSafe = Math.Min(bSafe, CallBoardInfo[1].BuyQty.Value + CallBoardInfo[1].Buy2Qty.Value);
            bSafe = Math.Min(bSafe, PutBoardInfo[3].BuyQty.Value + PutBoardInfo[3].Buy2Qty.Value);
            bSafe = Math.Min(bSafe, CallBoardInfo[3].SellQty.Value + CallBoardInfo[3].Sell2Qty.Value);
            var c = -500M
                + PutBoardInfo[0].BuyPrice.Value
                - CallBoardInfo[0].SellPrice.Value
                - PutBoardInfo[2].SellPrice.Value
                + CallBoardInfo[2].BuyPrice.Value;
            var cSafe = PutBoardInfo[0].BuyQty.Value + PutBoardInfo[0].Buy2Qty.Value;
            cSafe = Math.Min(cSafe, CallBoardInfo[0].SellQty.Value + CallBoardInfo[0].Sell2Qty.Value);
            cSafe = Math.Min(cSafe, PutBoardInfo[2].SellQty.Value + PutBoardInfo[2].Sell2Qty.Value);
            cSafe = Math.Min(cSafe, CallBoardInfo[2].BuyQty.Value + CallBoardInfo[2].Buy2Qty.Value);
            var d = -500M
                + PutBoardInfo[1].BuyPrice.Value
                - CallBoardInfo[1].SellPrice.Value
                - PutBoardInfo[3].SellPrice.Value
                + CallBoardInfo[3].BuyPrice.Value;
            var dSafe = PutBoardInfo[1].BuyQty.Value + PutBoardInfo[1].Buy2Qty.Value;
            dSafe = Math.Min(dSafe, CallBoardInfo[1].SellQty.Value + CallBoardInfo[1].Sell2Qty.Value);
            dSafe = Math.Min(dSafe, PutBoardInfo[3].SellQty.Value + PutBoardInfo[3].Sell2Qty.Value);
            dSafe = Math.Min(dSafe, CallBoardInfo[3].BuyQty.Value + CallBoardInfo[3].Buy2Qty.Value);

            maxProfit = a;
            safeLevel = aSafe;
            pattern = TradePattern.HighPutBuy_LowPutSell_LowCallBuy_HighCallSell_0;
            resultDisplay = "利益" + (a * 1000) + "円、安全度" + aSafe + "\n"
                + PutBoardInfo[0].SymbolName + "買、"
                + PutBoardInfo[2].SymbolName + "売、"
                + CallBoardInfo[0].SymbolName + "売、"
                + CallBoardInfo[2].SymbolName + "買";
            if (maxProfit < b)
            {
                maxProfit = b;
                safeLevel = bSafe;
                pattern = TradePattern.HighPutBuy_LowPutSell_LowCallBuy_HighCallSell_1;
                resultDisplay = "利益" + (b * 1000) + "円、安全度" + bSafe + "\n"
                    + PutBoardInfo[1].SymbolName + "買、"
                    + PutBoardInfo[3].SymbolName + "売、"
                    + CallBoardInfo[1].SymbolName + "売、"
                    + CallBoardInfo[3].SymbolName + "買";
            }
            if (maxProfit < c)
            {
                maxProfit = c;
                safeLevel = cSafe;
                pattern = TradePattern.HighPutSell_LowPutBuy_LowCallSell_HighCallBuy_0;
                resultDisplay = "利益" + (c * 1000) + "円、安全度" + cSafe + "\n"
                    + PutBoardInfo[0].SymbolName + "売、"
                    + PutBoardInfo[2].SymbolName + "買、"
                    + CallBoardInfo[0].SymbolName + "買、"
                    + CallBoardInfo[2].SymbolName + "売";
            }
            if (maxProfit < d)
            {
                maxProfit = d;
                safeLevel = dSafe;
                pattern = TradePattern.HighPutSell_LowPutBuy_LowCallSell_HighCallBuy_1;
                resultDisplay = "利益" + (d * 1000) + "円、安全度" + dSafe + "\n"
                    + PutBoardInfo[1].SymbolName + "売、"
                    + PutBoardInfo[3].SymbolName + "買、"
                    + CallBoardInfo[1].SymbolName + "買、"
                    + CallBoardInfo[3].SymbolName + "売";
            }
            _TradePattern = pattern;
            _Profit = maxProfit;
            _SafeLevel = safeLevel;
            if (maxProfit > _MaxProfit)
            {
                _MaxProfit = maxProfit;
                _SafeLevelAtMaxProfit = safeLevel;
            }
            ResultDisplay = resultDisplay + "\n最大利益" + (_MaxProfit * 1000) + "円、安全度" + _SafeLevelAtMaxProfit;
        }

        public void StopObserve()
        {
            _ObserveCancelTaken.Cancel();
            ObserveRunning = false;
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
