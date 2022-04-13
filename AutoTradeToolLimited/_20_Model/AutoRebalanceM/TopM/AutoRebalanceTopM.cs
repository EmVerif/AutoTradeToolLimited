using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

using AutoTradeTool._20_Model.AutoRebalanceM.SymbolM;

namespace AutoTradeTool._20_Model.AutoRebalanceM.TopM
{
    public class ModelBackup
    {
        public List<AutoRebalanceSymbolM> Database;
        public Decimal CurrentCash;
    }

    class AutoRebalanceTopM : INotifyPropertyChanged
    {
        public const double MaxTotalExpectedRatio = 1.00;

        public static AutoRebalanceTopM Instance = new AutoRebalanceTopM();
        public event PropertyChangedEventHandler PropertyChanged;

        private List<AutoRebalanceSymbolM> _Database = new List<AutoRebalanceSymbolM>();
        public List<AutoRebalanceSymbolM> Database
        {
            get
            {
                return _Database;
            }
            private set
            {
                _Database = value;
                OnPropertyChanged(nameof(Database));
            }
        }

        public double TotalExpectedRatio
        {
            get
            {
                double ret = 0;

                foreach (var data in Database)
                {
                    ret += data.ExpectedRatio;
                }

                return ret;
            }
        }

        private Boolean _AutoRebalanceRunning = false;
        public Boolean AutoRebalanceRunning
        {
            get
            {
                return _AutoRebalanceRunning;
            }
            private set
            {
                _AutoRebalanceRunning = value;
                OnPropertyChanged(nameof(AutoRebalanceRunning));
            }
        }

        private Decimal _CurrentCash = 0;
        public Decimal CurrentCash
        {
            get
            {
                return _CurrentCash;
            }
        }

        public Decimal CurrentTotalMarketCapitalization
        {
            get
            {
                Decimal ret = CurrentCash;

                foreach (var data in Database)
                {
                    ret += data.Position * data.CurrentPrice;
                }

                return ret;
            }
        }

        public Boolean AllTradeEnable
        {
            get
            {
                Boolean ret = true;

                if (Database.Count == 0)
                {
                    ret = false;
                }
                else
                {
                    foreach (var data in Database)
                    {
                        if (!data.TradeEnableFlag)
                        {
                            ret = false;
                        }
                    }
                }

                return ret;
            }
        }

        private CancellationTokenSource _AutoRebalanceCancelTaken;
        private string _TradeLog = "";

        public void Restore()
        {
            if (!File.Exists(Parameter.AutoRebalanceData))
            {
                return;
            }

            var xs = new XmlSerializer(typeof(ModelBackup));
            using (StreamReader sr = new StreamReader(Parameter.AutoRebalanceData, new UTF8Encoding(false)))
            {
                var modelBackup = (ModelBackup)xs.Deserialize(sr);
                foreach (var data in modelBackup.Database)
                {
                    data.PropertyChanged += OneSymbolModel_PropertyChanged;
                }
                _CurrentCash = modelBackup.CurrentCash;
                Database = modelBackup.Database;
            }
            OnPropertyChanged(nameof(CurrentCash));
            OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
        }

        public void Save()
        {
            var xs = new XmlSerializer(typeof(ModelBackup));

            Directory.CreateDirectory(Parameter.AutoRebalanceDirectory);
            using (StreamWriter sw = new StreamWriter(Parameter.AutoRebalanceData, false, new UTF8Encoding(false)))
            {
                ModelBackup modelBackup = new ModelBackup()
                {
                    CurrentCash = _CurrentCash,
                    Database = Database
                };
                xs.Serialize(sw, modelBackup);
                sw.Flush();
            }
            using (StreamWriter sw = new StreamWriter(
                Parameter.AutoRebalanceDirectory +
                @"Data_" +
                DateTime.Today.Year +
                DateTime.Today.Month.ToString("D2") +
                DateTime.Today.Day.ToString("D2") +
                @".xml",
                false, new UTF8Encoding(false)))
            {
                ModelBackup modelBackup = new ModelBackup()
                {
                    CurrentCash = _CurrentCash,
                    Database = Database
                };
                xs.Serialize(sw, modelBackup);
                sw.Flush();
            }
            using (StreamWriter sw = new StreamWriter(Parameter.AutoRebalanceTradeLog, true, new UTF8Encoding(false)))
            {
                sw.Write(_TradeLog);
                _TradeLog = "";
                sw.Flush();
            }
        }

        public void GetSymbolInfo()
        {
            foreach (AutoRebalanceSymbolM symbol in Database)
            {
                symbol.GetSymbolInfo();
            }
        }

        public void GetBoardInfo()
        {
            foreach (var symbol in Database)
            {
                symbol.GetBoardInfo();
            }
        }

        public void SyncPositionInfo()
        {
            var positionInfoList = Communication.GetPositions();
            foreach (var data in Database)
            {
                var realData = positionInfoList.Find(x => x.Symbol == data.Symbol);
                var prevPosition = data.Position;
                if (realData == null)
                {
                    data.Position = 0;
                }
                else
                {
                    data.Position = realData.LeavesQty;
                }
                if (data.Position == 0)
                {
                    // ポジション０は、一からやり直し。
                    data.VirtualCurrentRatio = 0;
                    data.ExpectedPositionBorder = 0;
                    data.PrevTrade = TradeType.Init;
                }
                if (prevPosition == 0)
                {
                    // データ管理されていない状態のため、一からやり直し。
                    data.VirtualCurrentRatio = 0;
                    data.ExpectedPositionBorder = 0;
                    data.PrevTrade = TradeType.Init;
                }
                else
                {
                    var ratio = (double)data.Position / (double)prevPosition;

                    // ツール管理のポジションと、サーバー管理のポジションが一致すること前提で、
                    // 差が出る場合は、株分割、統合した場合なので、以下プロパティを比例させる。
                    data.ExpectedPositionBorder *= ratio;
                    data.CurrentPrice /= (Decimal)ratio;
                }
            }
        }

        public void AddSymbol()
        {
            var addModel = new AutoRebalanceSymbolM();
            addModel.PropertyChanged += OneSymbolModel_PropertyChanged;
            Database.Add(addModel);
            OnPropertyChanged(nameof(Database));
        }

        public void DelSymbol(AutoRebalanceSymbolM refModel)
        {
            refModel.PropertyChanged -= OneSymbolModel_PropertyChanged;
            Database.Remove(refModel);
            foreach (var data in Database)
            {
                data.AdjustFlag = true;
            }
            OnPropertyChanged(nameof(Database));
            OnPropertyChanged(nameof(TotalExpectedRatio));
            OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
        }

        public void UpSymbol(AutoRebalanceSymbolM refModel)
        {
            var idx = Database.FindIndex(x => x == refModel);
            if (idx > 0)
            {
                var tmp = Database[idx - 1];
                Database[idx - 1] = Database[idx];
                Database[idx] = tmp;
            }
            OnPropertyChanged(nameof(Database));
        }

        public void DownSymbol(AutoRebalanceSymbolM refModel)
        {
            var idx = Database.FindIndex(x => x == refModel);
            if (idx < (Database.Count - 1))
            {
                var tmp = Database[idx + 1];
                Database[idx + 1] = Database[idx];
                Database[idx] = tmp;
            }
            OnPropertyChanged(nameof(Database));
        }

        public void CheckAutoRebalance()
        {
            GetSymbolInfo();
            SyncPositionInfo();
            GetBoardInfo();

            // 現金チェック
            var curRealCash = Communication.GetCash();
            if (curRealCash < CurrentCash)
            {
                throw new Exception("現金不足です。\n現在の現金は" + curRealCash + "円です。");
            }

            // シンボル登録チェック
            if (Database.Count == 0)
            {
                throw new Exception("シンボルが登録されていません。");
            }

            // シンボル毎のチェック
            var total = (double)CurrentTotalMarketCapitalization;
            foreach (var data in Database)
            {
                //if (data.ExpectedRatio == 0)
                //{
                //    throw new Exception(data.SymbolName + "の比率が０％です。");
                //}
                if (Database.Find(x => x.Symbol == data.Symbol) != data)
                {
                    throw new Exception(data.SymbolName + "が二重登録されています。");
                }
                //var num = total * data.ExpectedRatio / (double)data.CurrentPrice / (double)data.TradingUnit;
                //if (num < 5.0)
                //{
                //    throw new Exception(data.SymbolName + "に掛ける資金が少なすぎます。\n最低５単元が、開始条件の閾値。");
                //}
            }
        }

        public void PutOrPullCash(Decimal inCash)
        {
            _CurrentCash = inCash;
            foreach (var data in Database)
            {
                data.AdjustFlag = true;
            }
            OnPropertyChanged(nameof(CurrentCash));
            OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
        }

        public void AddCashAfterTrade(Decimal inDiffCash)
        {
            _CurrentCash += inDiffCash;
            OnPropertyChanged(nameof(CurrentCash));
            OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
        }

        public async Task StartAutoRebalanceAsync()
        {
            AutoRebalanceRunning = true;
            _AutoRebalanceCancelTaken = new CancellationTokenSource();
            var token = _AutoRebalanceCancelTaken.Token;

            await Task.Run(() =>
            {
                UInt32 cnt = 0;

                while (true)
                {
                    // Kabuステーションとの接続チェックで、定期的にKabuステーションと通信する。
                    if ((cnt % 1024) == 0)
                    {
                        Communication.GetCash();
                    }
                    cnt++;
                    foreach (var data in Database)
                    {
                        data.Rebalance();
                    }
                    Task.Delay(100).Wait();
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });
        }

        public void StopAutoRebalance()
        {
            _AutoRebalanceCancelTaken.Cancel();
            AutoRebalanceRunning = false;
        }

        public void SetTradeLog(string inLog)
        {
            _TradeLog += inLog;
        }

        private void OneSymbolModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AutoRebalanceSymbolM.CurrentPrice):
                    OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
                    break;
                case nameof(AutoRebalanceSymbolM.Position):
                    OnPropertyChanged(nameof(CurrentTotalMarketCapitalization));
                    break;
                case nameof(AutoRebalanceSymbolM.ExpectedRatio):
                    OnPropertyChanged(nameof(TotalExpectedRatio));
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
