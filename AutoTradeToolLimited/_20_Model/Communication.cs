using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoTradeTool._20_Model
{
    public class BoardInfo
    {
        public bool TradeEnableFlag = false;
        public string SymbolName;
        public Decimal? CurrentPrice;

        public Decimal? SellQty;
        public Decimal? SellPrice;
        public Decimal? Sell2Qty;
        public Decimal? Sell2Price;

        public Decimal? BuyQty;
        public Decimal? BuyPrice;
        public Decimal? Buy2Qty;
        public Decimal? Buy2Price;
    }

    public class SymbolInfo
    {
        public string DisplayName;
        public string Exchange;
        public Decimal TradingUnit = 1;
        public Decimal UpperLimit;
        public Decimal LowerLimit;
        public string PriceUnitType;
    }

    public class PositionInfo
    {
        public string Symbol;
        public Decimal LeavesQty;
        public Decimal HoldQty;
    }

    class SymbolData
    {
        public string Symbol;
        public int Exchange;
    }

    public static class Communication
    {
        public static string Password { private get; set; }

        public static string TradePassword { private get; set; }

        private static string _TokenBody = null;
        private static string _Token
        {
            get
            {
                lock (_CommLock)
                {
                    if (string.IsNullOrEmpty(_TokenBody))
                    {
                        _TokenBody = GetToken();
                        if (string.IsNullOrEmpty(_TokenBody))
                        {
                            throw new Exception("Token取得失敗\nパスワード間違いか、Kabuステーションを立ち上げなおす必要有。");
                        }
                    }
                }

                return _TokenBody;
            }
        }

        private static HttpClient _Client = new HttpClient();

        private static object _CommLock = new object();

        private static ClientWebSocket _WebSocket;
        private static CancellationTokenSource _WebSocketCancelTaken;
        private static Task _WebSocketTask = null;
        private static object _BoardInfoFromWebSocketLock = new object();
        private static Dictionary<string, BoardInfo> _BoardInfoFromWebSocket = new Dictionary<string, BoardInfo>();

        private static Boolean _IsFirstLogAccess = true;

        private static void OutputLog(string inLog)
        {
            lock (_CommLock)
            {
                using (StreamWriter sw = new StreamWriter("ComLog.txt", !_IsFirstLogAccess, new UTF8Encoding(false)))
                {
                    sw.Write(DateTime.Now + "\n");
                    sw.Write(inLog);
                    sw.Flush();
                }
                _IsFirstLogAccess = false;
            }
        }

        // Get Token API
        private static string GetToken()
        {
            var obj = new
            {
                APIPassword = Password
            };
            var url = Parameter.BaseHttpUrl + "token";
            string ret;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                JObject results = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetToken) + "\n" + results.ToString() + "\n");
                ret = (string)results["Token"];
            }
            catch
            {
                ret = null;
            }

            return ret;
        }

        public static BoardInfo GetBoardInfo(string inSymbol, string inExchange = "1"/* とりあえず、東証*/)
        {
            var url = Parameter.BaseHttpUrl + @"board/" + inSymbol + @"@" + inExchange;
            BoardInfo ret = null;
            string symbol;

            lock (_BoardInfoFromWebSocketLock)
            {
                if (_BoardInfoFromWebSocket.ContainsKey(inSymbol))
                {
                    ret = _BoardInfoFromWebSocket[inSymbol];
                }
            }
            if (!GetWebSocketRunning() || (ret == null))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("X-API-KEY", _Token);
                lock (_CommLock)
                {
                    HttpResponseMessage response = _Client.SendAsync(request).Result;
                    Task.Delay(100).Wait();
                    AnalyzeBoardInfoJson(
                        response.Content.ReadAsStringAsync().Result,
                        out symbol, out ret, true
                    );
                }
                if (ret != null)
                {
                    lock (_BoardInfoFromWebSocketLock)
                    {
                        _BoardInfoFromWebSocket[inSymbol] = ret;
                    }
                }
            }

            return ret;
        }

        public static void GetSoftLimit()
        {
            // TODO: 戻り値未設定
            var url = Parameter.BaseHttpUrl + "apisoftlimit";
            JObject infos;
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetSoftLimit) + "\n" + infos.ToString() + "\n");
            }
        }

        public static void Buy(string inSymbol, Decimal inPrice, Decimal inQty, out Decimal outBuyNum, out Decimal outPrevCash, out Decimal outPostCash)
        {
            lock (_CommLock)
            {
                // 購入前のキャッシュ取得
                outPrevCash = GetCash();

                // 購入要求
                var obj = new
                {
                    Password = TradePassword,
                    Symbol = inSymbol,
                    Exchange = 1,// とりあえず、東証
                    SecurityType = 1,
                    Side = "2",
                    CashMargin = 1,// とりあえず、現物
                    DelivType = 2,// とりあえず、お預り金
                    FundType = "AA",// とりあえず、信用代用
                    AccountType = 4,// とりあえず、特定
                    Qty = (Int32)inQty,
                    FrontOrderType = 20,// とりあえず、指値
                    Price = (double)inPrice,
                    ExpireDay = 0,// とりあえず、本日
                                  //ReverseLimitOrder = new
                                  //{
                                  //    TriggerSec = 3,
                                  //    TriggerPrice = 1600,
                                  //    UnderOver = 2,
                                  //    AfterHitOrderType = 1,
                                  //    AfterHitPrice = 0
                                  //}
                };
                var url = Parameter.BaseHttpUrl + "sendorder";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Add("ContentType", "application/json");
                request.Headers.Add("X-API-KEY", _Token);
                request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(200).Wait();
                JObject infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(Buy) + "\n" + infos.ToString() + "\n");
                if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
                {
                    throw new Exception((string)infos["Message"] + "⇒" + inSymbol + "\nトレードパスワード間違いの可能性有。");
                }
                var orderId = (string)infos["OrderId"];

                // 0.5秒後にキャンセル（0.2秒はその前に待っているため、引く。）
                Task.Delay(300).Wait();
                var cancelId = CancelTrade(orderId);
                if (!string.IsNullOrEmpty(cancelId))
                {
                    Int32 loopCnt = 0;

                    do
                    {
                        loopCnt++;
                        if (loopCnt >= 10)
                        {
                            // １秒キャンセル出来なかったら、一旦アクセス頻度を落として様子見
                            Task.Delay(900).Wait();
                        }
                        if (loopCnt >= (10 + 29))
                        {
                            // TODO: ３０秒経ってキャンセル出来なくても、中断せずに進めることとする。
                            //throw new Exception("想定外エラー：注文のキャンセル出来ない。⇒" + cancelId);
                            break;
                        }
                    }
                    while (!GetOrderIsFinished(cancelId));
                }

                // キャッシュの変化チェック
                outPostCash = GetCash();
                Boolean finFlag;
                do
                {
                    outBuyNum = GetContractNum(orderId, out finFlag);
                }
                while (!finFlag);
                if (outBuyNum != 0)
                {
                    // トレード成立した場合は、現金値が変化するまでポーリングする。
                    Decimal diffCash = outPrevCash - outPostCash;
                    Decimal diffCashMin = outBuyNum * inPrice;
                    Int32 loopCnt = 0;

                    while (diffCash < diffCashMin)
                    {
                        loopCnt++;
                        if (loopCnt >= 100)
                        {
                            throw new Exception("想定外エラー：購入後の現金取得失敗。");
                        }
                        outPostCash = GetCash();
                        diffCash = outPrevCash - outPostCash;
                    }
                }
            }
        }

        public static void Sell(string inSymbol, Decimal inPrice, Decimal inQty, out Decimal outSellNum, out Decimal outPrevCash, out Decimal outPostCash)
        {
            lock (_CommLock)
            {
                // 売却前のキャッシュ取得
                outPrevCash = GetCash();

                // 売却要求
                var obj = new
                {
                    Password = TradePassword,
                    Symbol = inSymbol,
                    Exchange = 1,// とりあえず、東証
                    SecurityType = 1,
                    Side = "1",
                    CashMargin = 1,// とりあえず、現物
                    DelivType = 0,
                    FundType = "  ",
                    AccountType = 4,// とりあえず、特定
                    Qty = (Int32)inQty,
                    FrontOrderType = 20,// とりあえず、指値
                    Price = (double)inPrice,
                    ExpireDay = 0,// とりあえず、本日
                                  //ReverseLimitOrder = new
                                  //{
                                  //    TriggerSec = 3,
                                  //    TriggerPrice = 1600,
                                  //    UnderOver = 2,
                                  //    AfterHitOrderType = 1,
                                  //    AfterHitPrice = 0
                                  //}
                };
                var url = Parameter.BaseHttpUrl + "sendorder";
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Add("ContentType", "application/json");
                request.Headers.Add("X-API-KEY", _Token);
                request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(200).Wait();
                JObject infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(Sell) + "\n" + infos.ToString() + "\n");
                if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
                {
                    throw new Exception((string)infos["Message"] + "⇒" + inSymbol + "\nトレードパスワード間違いの可能性有。");
                }
                var orderId = (string)infos["OrderId"];

                // 0.5秒後にキャンセル（0.2秒はその前に待っているため、引く。）
                Task.Delay(300).Wait();
                var cancelId = CancelTrade(orderId);
                if (!string.IsNullOrEmpty(cancelId))
                {
                    Int32 loopCnt = 0;

                    do
                    {
                        loopCnt++;
                        if (loopCnt >= 10)
                        {
                            // １秒キャンセル出来なかったら、一旦アクセス頻度を落として様子見
                            Task.Delay(900).Wait();
                        }
                        if (loopCnt >= (10 + 29))
                        {
                            // TODO: ３０秒経ってキャンセル出来なくても、中断せずに進めることとする。
                            //throw new Exception("想定外エラー：注文のキャンセル出来ない。⇒" + cancelId);
                            break;
                        }
                    }
                    while (!GetOrderIsFinished(cancelId));
                }

                // ホールド解除待ち
                Decimal holdQty;
                do
                {
                    var list = GetPositions();
                    var info = list.Find(x => x.Symbol == inSymbol);
                    if (info != null)
                    {
                        holdQty = info.HoldQty;
                    }
                    else
                    {
                        holdQty = 0;
                    }
                } while (holdQty != 0);

                // キャッシュの変化チェック
                outPostCash = GetCash();
                Boolean finFlag;
                do
                {
                    outSellNum = GetContractNum(orderId, out finFlag);
                }
                while (!finFlag);
                if (outSellNum != 0)
                {
                    // トレード成立した場合は、現金値が変化するまでポーリングする。
                    Int32 loopCnt = 0;

                    while (outPostCash == outPrevCash)
                    {
                        loopCnt++;
                        if (loopCnt >= 100)
                        {
                            throw new Exception("想定外エラー：売却後の現金取得失敗。");
                        }
                        outPostCash = GetCash();
                    }
                }
            }
        }

        public static string CancelTrade(string inOrderId)
        {
            string ret;
            var obj = new
            {
                OrderId = inOrderId,
                Password = TradePassword
            };
            var url = Parameter.BaseHttpUrl + "cancelorder";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            JObject infos;

            request.Headers.Add("ContentType", "application/json");
            request.Headers.Add("X-API-KEY", _Token);
            request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(CancelTrade) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                ret = null;
            }
            else
            {
                ret = (string)infos["OrderId"];
            }

            return ret;
        }

        public static SymbolInfo GetSymbolInfo(string inSymbol, string inExchange = "1"/* とりあえず、東証*/)
        {
            SymbolInfo ret = new SymbolInfo();
            var url = Parameter.BaseHttpUrl + @"symbol/" + inSymbol + @"@" + inExchange + @"?info=true";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JObject infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetSymbolInfo) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }
            ret.DisplayName = (string)infos["DisplayName"];
            ret.Exchange = (string)infos["Exchange"];
            ret.LowerLimit = (Decimal)infos["LowerLimit"];
            ret.UpperLimit = (Decimal)infos["UpperLimit"];
            ret.TradingUnit = (Decimal)infos["TradingUnit"];
            ret.PriceUnitType = (string)infos["PriceRangeGroup"];

            return ret;
        }

        public static List<PositionInfo> GetPositions()
        {
            List<PositionInfo> ret = new List<PositionInfo>();
            string product = "0";
            string symbol = "";

            var builder = new UriBuilder(Parameter.BaseHttpUrl + "positions");
            var param = HttpUtility.ParseQueryString(builder.Query);

            param["Product"] = product;
            if (!string.IsNullOrEmpty(symbol))
            {
                param["symbol"] = symbol;
            }
            builder.Query = param.ToString();

            string url = builder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JArray infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JArray>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetPositions) + "\n" + infos.ToString() + "\n");
            }
            foreach (var info in infos)
            {
                PositionInfo posInfo = new PositionInfo();
                posInfo.Symbol = (string)info["Symbol"];
                posInfo.LeavesQty = Convert.ToDecimal(info["LeavesQty"]);
                posInfo.HoldQty = Convert.ToDecimal(info["HoldQty"]);
                ret.Add(posInfo);
            }

            return ret;
        }

        public static Decimal GetCash()
        {
            Decimal ret;

            var url = Parameter.BaseHttpUrl + @"wallet/cash";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JObject infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetCash) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }
            ret = Convert.ToDecimal((string)infos["StockAccountWallet"]);

            return ret;
        }

        public static Boolean GetOrderIsFinished(string inOrderId)
        {
            var builder = new UriBuilder(Parameter.BaseHttpUrl + "orders");
            var param = HttpUtility.ParseQueryString(builder.Query);
            Boolean ret;

            param["id"] = inOrderId;
            builder.Query = param.ToString();

            string url = builder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JArray infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JArray>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetOrderIsFinished) + "\n" + infos.ToString() + "\n");
            }
            ret = (Convert.ToInt32((string)infos[0]["State"]) == 5);

            return ret;
        }

        public static Decimal GetContractNum(string inOrderId, out Boolean outFinFlag)
        {
            var builder = new UriBuilder(Parameter.BaseHttpUrl + "orders");
            var param = HttpUtility.ParseQueryString(builder.Query);
            Decimal ret;

            param["id"] = inOrderId;
            builder.Query = param.ToString();

            string url = builder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JArray infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JArray>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetContractNum) + "\n" + infos.ToString() + "\n");
            }
            ret = Convert.ToDecimal((string)infos[0]["CumQty"]);
            outFinFlag = ((string)infos[0]["OrderState"] == "5");

            return ret;
        }

        public static void UnregisterAll()
        {
            lock (_BoardInfoFromWebSocketLock)
            {
                _BoardInfoFromWebSocket = new Dictionary<string, BoardInfo>();
            }
            var url = Parameter.BaseHttpUrl + "unregister/all";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            JObject infos;

            request.Headers.Add("ContentType", "application/json");
            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(UnregisterAll) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }
        }

        public static void RegisterTosho(IReadOnlyList<string> symbolList)
        {
            var symbols = new List<SymbolData>();
            foreach (var symbol in symbolList)
            {
                symbols.Add(new SymbolData() { Symbol = symbol, Exchange = 1 });
            }
            var obj = new { Symbols = symbols.ToArray() };
            var url = Parameter.BaseHttpUrl + "register";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            JObject infos;

            request.Headers.Add("ContentType", "application/json");
            request.Headers.Add("X-API-KEY", _Token);
            request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(RegisterTosho) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }
        }

        public static void RegisterFutureOption(IReadOnlyList<string> symbolList)
        {
            var symbols = new List<SymbolData>();
            foreach (var symbol in symbolList)
            {
                symbols.Add(new SymbolData() { Symbol = symbol, Exchange = 2 });
            }
            var obj = new { Symbols = symbols.ToArray() };
            var url = Parameter.BaseHttpUrl + "register";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            JObject infos;

            request.Headers.Add("ContentType", "application/json");
            request.Headers.Add("X-API-KEY", _Token);
            request.Content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(RegisterFutureOption) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }
        }

        public static string GetFutureSymbol(string inFutureCode, Int32 inDerivMonth = 0)
        {
            var builder = new UriBuilder(Parameter.BaseHttpUrl + @"symbolname/future");
            var param = HttpUtility.ParseQueryString(builder.Query);

            param["FutureCode"] = inFutureCode;
            param["DerivMonth"] = inDerivMonth.ToString();

            builder.Query = param.ToString();
            string url = builder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JObject infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetFutureSymbol) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }

            return (string)infos["Symbol"];
        }

        public static string GetOptionSymbol(string inPutOrCall, Int32 inStrikePrice, Int32 inDerivMonth = 0)
        {
            var builder = new UriBuilder(Parameter.BaseHttpUrl + @"symbolname/option");
            var param = HttpUtility.ParseQueryString(builder.Query);

            param["DerivMonth"] = inDerivMonth.ToString();
            param["PutOrCall"] = inPutOrCall;
            param["StrikePrice"] = inStrikePrice.ToString();

            builder.Query = param.ToString();
            string url = builder.ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            JObject infos;

            request.Headers.Add("X-API-KEY", _Token);
            lock (_CommLock)
            {
                HttpResponseMessage response = _Client.SendAsync(request).Result;
                Task.Delay(100).Wait();
                infos = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);
                OutputLog(nameof(GetOptionSymbol) + "\n" + infos.ToString() + "\n");
            }
            if (infos.ContainsKey("Code") && infos.ContainsKey("Message"))
            {
                throw new Exception((string)infos["Message"]);
            }

            return (string)infos["Symbol"];
        }

        public static void StartWebSocket()
        {
            lock (_CommLock)
            {
                if (_WebSocketTask != null)
                {
                    return;
                }

                var url = Parameter.BaseWsUrl;
                _WebSocket = new ClientWebSocket();

                Task con = _WebSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                con.Wait();
                if (con.Status != TaskStatus.RanToCompletion)
                {
                    throw new Exception("WebSocketが開けません。");
                }

                _WebSocketCancelTaken = new CancellationTokenSource();
                var token = _WebSocketCancelTaken.Token;
                _WebSocketTask = Task.Run(() =>
                {
                    var buffer = new byte[65536 * 4];
                    var segment = new ArraySegment<byte>(buffer);

                    while (true)
                    {
                        var result = _WebSocket.ReceiveAsync(segment, token);
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }
                        if (result.Status == TaskStatus.Faulted)
                        {
                            Task.Delay(100).Wait();
                            continue;
                        }
                        try
                        {
                            string symbol;
                            BoardInfo boardInfo;

                            AnalyzeBoardInfoJson(
                                Encoding.UTF8.GetString(buffer, 0, result.Result.Count),
                                out symbol, out boardInfo
                            );
                            if (boardInfo != null)
                            {
                                lock (_BoardInfoFromWebSocketLock)
                                {
                                    _BoardInfoFromWebSocket[symbol] = boardInfo;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                );
            }
        }

        public static void StopWebSocket()
        {
            lock (_CommLock)
            {
                if (_WebSocketTask != null)
                {
                    _WebSocketCancelTaken.Cancel();
                    while (!_WebSocketTask.IsCompleted)
                    {
                        Task.Delay(100).Wait();
                    }
                    _WebSocketTask = null;
                }
            }
        }

        private static Boolean GetWebSocketRunning()
        {
            Boolean ret;

            lock (_CommLock)
            {
                if (_WebSocketTask != null)
                {
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }

            return ret;
        }

        public static Decimal CalcCurrentPriceUnit(string inPriceRangeGroup, Decimal inBuyPrice)
        {
            Decimal ret;

            switch (inPriceRangeGroup)
            {
                case "10000":
                    if (inBuyPrice < 3000)
                    {
                        ret = 1;
                    }
                    else if (inBuyPrice < 5000)
                    {
                        ret = 5;
                    }
                    else if (inBuyPrice < 30000)
                    {
                        ret = 10;
                    }
                    else if (inBuyPrice < 50000)
                    {
                        ret = 50;
                    }
                    else if (inBuyPrice < 300000)
                    {
                        ret = 100;
                    }
                    else if (inBuyPrice < 500000)
                    {
                        ret = 500;
                    }
                    else if (inBuyPrice < 3000000)
                    {
                        ret = 1000;
                    }
                    else if (inBuyPrice < 5000000)
                    {
                        ret = 5000;
                    }
                    else if (inBuyPrice < 30000000)
                    {
                        ret = 10000;
                    }
                    else if (inBuyPrice < 50000000)
                    {
                        ret = 50000;
                    }
                    else
                    {
                        ret = 100000;
                    }
                    break;
                case "10003":
                    if (inBuyPrice < 1000)
                    {
                        ret = 0.1M;
                    }
                    else if (inBuyPrice < 3000)
                    {
                        ret = 0.5M;
                    }
                    else if (inBuyPrice < 10000)
                    {
                        ret = 1;
                    }
                    else if (inBuyPrice < 30000)
                    {
                        ret = 5;
                    }
                    else if (inBuyPrice < 100000)
                    {
                        ret = 10;
                    }
                    else if (inBuyPrice < 300000)
                    {
                        ret = 50;
                    }
                    else if (inBuyPrice < 1000000)
                    {
                        ret = 100;
                    }
                    else if (inBuyPrice < 3000000)
                    {
                        ret = 500;
                    }
                    else if (inBuyPrice < 10000000)
                    {
                        ret = 1000;
                    }
                    else if (inBuyPrice < 30000000)
                    {
                        ret = 5000;
                    }
                    else
                    {
                        ret = 10000;
                    }
                    break;
                default:
                    throw new Exception("価格単位不明");
            }

            return ret;
        }

        public static void ClearToken()
        {
            _TokenBody = null;
        }

        private static void AnalyzeBoardInfoJson(string inJsonText, out string outSymbol, out BoardInfo outBoardInfo, Boolean isLogOut = false)
        {
            Boolean tradeEnableFlag = true;
            outBoardInfo = new BoardInfo();
            JObject infos = JsonConvert.DeserializeObject<JObject>(inJsonText);
            if (isLogOut)
            {
                OutputLog(nameof(AnalyzeBoardInfoJson) + "\n" + infos.ToString() + "\n");
            }
            outSymbol = (string)infos["Symbol"];
            try
            {
                outBoardInfo.CurrentPrice = (Decimal?)infos["CurrentPrice"];
                outBoardInfo.SellQty = (Decimal?)infos["BidQty"];
                outBoardInfo.SellPrice = (Decimal?)infos["BidPrice"];
                outBoardInfo.Sell2Qty = (Decimal?)infos["Sell2"]["Qty"];
                outBoardInfo.Sell2Price = (Decimal?)infos["Sell2"]["Price"];
                outBoardInfo.BuyQty = (Decimal?)infos["AskQty"];
                outBoardInfo.BuyPrice = (Decimal?)infos["AskPrice"];
                outBoardInfo.Buy2Qty = (Decimal?)infos["Buy2"]["Qty"];
                outBoardInfo.Buy2Price = (Decimal?)infos["Buy2"]["Price"];
                outBoardInfo.SymbolName = (string)infos["SymbolName"];
                string bidSign = (string)infos["BidSign"];
                string askSign = (string)infos["AskSign"];
                string currentPriceChangeStatus = (string)infos["CurrentPriceChangeStatus"];
                if (
                    (bidSign != "0000") &&
                    (bidSign != "0101")
                )
                {
                    tradeEnableFlag = false;
                }
                if (
                    (askSign != "0000") &&
                    (askSign != "0101")
                )
                {
                    tradeEnableFlag = false;
                }
                if (
                    (currentPriceChangeStatus != "0000") &&
                    (currentPriceChangeStatus != "0056") &&
                    (currentPriceChangeStatus != "0057") &&
                    (currentPriceChangeStatus != "0058") &&
                    (currentPriceChangeStatus != "0059")
                )
                {
                    tradeEnableFlag = false;
                }
                if (outBoardInfo.SellPrice == outBoardInfo.BuyPrice)
                {
                    tradeEnableFlag = false;
                }
                outBoardInfo.TradeEnableFlag = tradeEnableFlag;
            }
            catch
            {
                outBoardInfo = null;
            }
        }
    }
}
