namespace MBM.Classes
{
    /// <summary>
    /// Daily Prices Object Class
    /// </summary>
    public class DailyPrices
    {
        public string Date { get; set; }
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string PriceOpen { get; set; }
        public string PriceClose { get; set; }
        public string PriceLow { get; set; }
        public string PriceHigh { get; set; }
        public string StockPriceAdj { get; set; }
        public string Volume { get; set; }
    }
}
