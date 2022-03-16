namespace AutoTradeTool._20_Model
{
    static class Parameter
    {
        public const string BaseWsUrl = @"ws://localhost:18080/kabusapi/websocket";
        public const string BaseHttpUrl = @"http://localhost:18080/kabusapi/";
        public const string Password = @"";

        public const string AutoRebalanceDirectory = @"AutoRebalance/";
        public const string AutoRebalanceData = AutoRebalanceDirectory + @"Data.xml";
        public const string AutoRebalanceTradeLog = AutoRebalanceDirectory + @"Trade.log";
    }
}
