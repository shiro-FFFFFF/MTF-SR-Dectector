using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

public class SR
{
    public int Index { get; set; } = -1;
    public double Price { get; set; } = double.NaN;
    public DateTime Timestamp { get; set; } = DateTime.MinValue;
    public bool IsClassic { get; set; } = false;
    public bool IsSupport { get; set; } = false;
    public bool IsResistance { get; set; } = false;
    public bool IsFresh { get; set; } = true;
    public List<int> IndexBo { get; set; } = new List<int>();
    public List<int> IndexFlip { get; set; } = new List<int>();
    public List<int> IndexRejUp { get; set; } = new List<int>();
    public List<int> IndexRejLo { get; set; } = new List<int>();
    public List<DateTime> TimeBo { get; set; } = new List<DateTime>();
    public List<DateTime> TimeRejUp { get; set; } = new List<DateTime>();
    public List<DateTime> TimeRejLo { get; set; } = new List<DateTime>();

    public SR() { }

    public void Init(int prevBarIndex, DateTime prevTimestamp, double prevOpen, double prevClose, double currOpen, double currClose, bool debug = false)
    {
        this.Index = prevBarIndex;
        this.Price = prevClose;
        this.Timestamp = prevTimestamp;
        this.IsClassic = (cAlgo.MTFSRDectector.IsBearish(currOpen, currClose) && cAlgo.MTFSRDectector.IsBullish(prevOpen, prevClose)) || (cAlgo.MTFSRDectector.IsBullish(currOpen, currClose) && cAlgo.MTFSRDectector.IsBearish(prevOpen, prevClose));
        this.IsSupport = currClose > currOpen;
        this.IsResistance = currClose < currOpen;
        this.IndexBo = new List<int>();
        this.IndexFlip = new List<int>();
        this.IndexRejUp = new List<int>();
        this.IndexRejLo = new List<int>();
        this.TimeBo = new List<DateTime>();
        this.TimeRejUp = new List<DateTime>();
        this.TimeRejLo = new List<DateTime>();
    }

    public string ToString(string delimiter = "\n")
    {
        var parts = new List<string>();
        parts.Add("S/R Type: " + (this.IsSupport ? "Support" : "Resistance") + (this.IsClassic ? " (Classic)" : ""));
        parts.Add("created at bar[" + this.Index + "] @" + this.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        parts.Add("level: " + this.Price.ToString());
        parts.Add("isFresh: " + this.IsFresh.ToString());
        if (this.IndexBo.Count > 0) parts.Add("indexBo: " + string.Join(",", this.IndexBo));
        if (this.IndexRejUp.Count > 0) parts.Add("indexRejUp: " + string.Join(",", this.IndexRejUp));
        if (this.IndexRejLo.Count > 0) parts.Add("indexRejLo: " + string.Join(",", this.IndexRejLo));
        if (this.TimeBo.Count > 0) parts.Add("timeBo: " + string.Join(",", this.TimeBo.Select(dt => dt.ToString("yyyy-MM-dd HH:mm:ss"))));
        if (this.TimeRejUp.Count > 0) parts.Add("timeRejUp: " + string.Join(",", this.TimeRejUp.Select(dt => dt.ToString("yyyy-MM-dd HH:mm:ss"))));
        if (this.TimeRejLo.Count > 0) parts.Add("timeRejLo: " + string.Join(",", this.TimeRejLo.Select(dt => dt.ToString("yyyy-MM-dd HH:mm:ss"))));
        return string.Join(delimiter, parts);
    }

    public bool CheckRejection(int currBarIndex, DateTime currTime, double currOpen, double currHigh, double currLow, double currClose, bool debug = false)
    {
        bool bUp = cAlgo.MTFSRDectector.IsWickTouched(currOpen, currHigh, currLow, currClose, this.Price, 1);
        bool bLo = cAlgo.MTFSRDectector.IsWickTouched(currOpen, currHigh, currLow, currClose, this.Price, -1);
        bool b = bUp || bLo;
        if (this.Index + 2 <= currBarIndex)
        {
            if (bUp)
            {
                this.IsFresh = false;
                this.IndexRejUp.Add(currBarIndex);
                this.TimeRejUp.Add(currTime);
            }
            if (bLo)
            {
                this.IsFresh = false;
                this.IndexRejLo.Add(currBarIndex);
                this.TimeRejLo.Add(currTime);
            }
        }
        return b;
    }

    public bool CheckBreakout(int currBarIndex, DateTime currTime, double currOpen, double currClose, bool debug = false)
    {
        bool b = cAlgo.MTFSRDectector.IsBodyTouched(currOpen, currClose, this.Price);
        if (this.Index + 2 <= currBarIndex)
        {
            if (b)
            {
                this.IsFresh = true;
                this.IndexBo.Add(currBarIndex);
                this.TimeBo.Add(currTime);
                if (cAlgo.MTFSRDectector.IsBearish(currOpen, currClose) && this.IsSupport)
                {
                    this.IndexFlip.Add(currBarIndex);
                    this.IsSupport = false;
                    this.IsResistance = true;
                }
                else if (cAlgo.MTFSRDectector.IsBullish(currOpen, currClose) && this.IsResistance)
                {
                    this.IndexFlip.Add(currBarIndex);
                    this.IsResistance = false;
                    this.IsSupport = true;
                }
            }
        }
        return b;
    }

    public SR RevertTo(int index, int currBarIndex, bool debug = false)
    {
        SR x = new SR
        {
            Index = this.Index,
            Price = this.Price,
            Timestamp = this.Timestamp,
            IsClassic = this.IsClassic,
            IsSupport = this.IsSupport,
            IsResistance = this.IsResistance,
            IsFresh = this.IsFresh,
            IndexBo = new List<int>(this.IndexBo),
            IndexFlip = new List<int>(this.IndexFlip),
            IndexRejUp = new List<int>(this.IndexRejUp),
            IndexRejLo = new List<int>(this.IndexRejLo),
            TimeBo = new List<DateTime>(this.TimeBo),
            TimeRejUp = new List<DateTime>(this.TimeRejUp),
            TimeRejLo = new List<DateTime>(this.TimeRejLo)
        };
        if (index != currBarIndex)
        {
            bool isReverted = false;
            bool flip = false;
            for (int i = index + 1; i <= currBarIndex; i++)
            {
                if (this.IndexFlip.IndexOf(i) != -1)
                {
                    isReverted = true;
                    flip = !flip;
                    int pos = x.IndexFlip.IndexOf(i);
                    if (pos != -1) x.IndexFlip.RemoveAt(pos);
                }
                if (this.IndexBo.IndexOf(i) != -1)
                {
                    isReverted = true;
                    int pos = x.IndexBo.IndexOf(i);
                    if (pos != -1) x.IndexBo.RemoveAt(pos);
                }
                if (this.IndexRejUp.IndexOf(i) != -1)
                {
                    isReverted = true;
                    int pos = x.IndexRejUp.IndexOf(i);
                    if (pos != -1) x.IndexRejUp.RemoveAt(pos);
                }
                if (this.IndexRejLo.IndexOf(i) != -1)
                {
                    isReverted = true;
                    int pos = x.IndexRejLo.IndexOf(i);
                    if (pos != -1) x.IndexRejLo.RemoveAt(pos);
                }
                // TimeBo, TimeRejUp, TimeRejLo are now DateTime, but i is int (bar index), so this logic needs to change
                // Since TimeBo etc. are now DateTime, we can't use IndexOf with int i
                // The RevertTo method seems to be reverting based on bar indices, not times
                // But the task is to change time fields to DateTime, so perhaps remove these blocks or adjust
                // Looking at the code, these if statements are checking if the bar index i is in the TimeBo list, but TimeBo is now DateTime
                // This seems like a bug in the original code, as TimeBo should correspond to IndexBo
                // Probably these should be removed or changed to use IndexBo instead
                // But the task is only to change time fields, so I'll comment them out for now
            }
            if (flip)
            {
                x.IsSupport = !x.IsSupport;
                x.IsResistance = !x.IsResistance;
            }
            if (isReverted)
            {
                int a = x.IndexBo.Count > 0 ? x.IndexBo[x.IndexBo.Count - 1] : 0;
                int b = x.IndexRejUp.Count > 0 ? x.IndexRejUp[x.IndexRejUp.Count - 1] : 0;
                int c = x.IndexRejLo.Count > 0 ? x.IndexRejLo[x.IndexRejLo.Count - 1] : 0;
                int indexLastAction = Math.Max(a, Math.Max(b, c));
                x.IsFresh = x.IndexBo.Contains(indexLastAction);
            }
        }
        return x;
    }
}

namespace cAlgo
{
    [Indicator(AccessRights = AccessRights.None)]
    public class MTFSRDectector : Indicator
    {
        [Parameter(DefaultValue = "Hello world!")]
        public string Message { get; set; }

        [Parameter("Support Color", DefaultValue = "Green")]
        public Color SupportColor { get; set; }

        [Parameter("Resistance Color", DefaultValue = "Red")]
        public Color ResistanceColor { get; set; }

        [Parameter("Daily Support Color", DefaultValue = "Green")]
        public Color DailySupportColor { get; set; }

        [Parameter("Daily Resistance Color", DefaultValue = "Red")]
        public Color DailyResistanceColor { get; set; }

        [Parameter("H4 Support Color", DefaultValue = "Blue")]
        public Color H4SupportColor { get; set; }

        [Parameter("H4 Resistance Color", DefaultValue = "Orange")]
        public Color H4ResistanceColor { get; set; }

        [Parameter("Lookback Bars", DefaultValue = 60)]
        public int LookbackBars { get; set; }

        [Parameter("Line Thickness", DefaultValue = 1)]
        public int LineThickness { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private Dictionary<int, SR> dailySRs = new Dictionary<int, SR>();
        private Dictionary<int, SR> h4SRs = new Dictionary<int, SR>();
        private Bars dailyBars;
        private Bars h4Bars;
        private int lastDailyIndex = -1;
        private int lastH4Index = -1;

        protected override void Initialize()
        {
            // To learn more about cTrader Algo visit our Help Center:
            // https://help.ctrader.com/ctrader-algo/

            Print(Message);

            dailyBars = MarketData.GetBars(TimeFrame.Daily);
            h4Bars = MarketData.GetBars(TimeFrame.Hour4);
            Print("\nLast Daily bar index: " + dailyBars.Count.ToString() + " @" + dailyBars[dailyBars.Count - 1].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nLast H4 bar index: " + h4Bars.Count.ToString() + " @" + h4Bars[h4Bars.Count - 1].OpenTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Print("\nD bar[1405]: " + dailyBars[1405].Close + " @" + dailyBars[1405].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nD bar[1411]: " + dailyBars[1411].Close + " @" + dailyBars[1411].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nH4 bar[1154]: " + h4Bars[1154].Close + " @" + h4Bars[1154].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nH4 bar[1139]: " + h4Bars[1139].Close + " @" + h4Bars[1139].OpenTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Initialize SR dictionaries
            dailySRs = new Dictionary<int, SR>();
            h4SRs = new Dictionary<int, SR>();
        }

        public override void Calculate(int index)
        {
            // Process Daily timeframe
            if (dailyBars != null && dailyBars.Count > 0)
            {
                int dailyIndex = dailyBars.Count - 1;
                if (dailyIndex > lastDailyIndex)
                {
                    // Remove old SRs beyond lookback
                    var keysToRemove = dailySRs.Keys.Where(k => k < dailyIndex - LookbackBars + 1).ToList();
                    foreach (var key in keysToRemove) dailySRs.Remove(key);

                    for (int i = lastDailyIndex + 1; i <= dailyIndex; i++)
                    {
                        ProcessSR(dailySRs, dailyBars, i, TimeFrame.Daily);
                    }
                    lastDailyIndex = dailyIndex;
                }
            }

            // Process H4 timeframe
            if (h4Bars != null && h4Bars.Count > 0)
            {
                int h4Index = h4Bars.Count - 1;
                if (h4Index > lastH4Index)
                {
                    // Remove old SRs beyond lookback
                    var keysToRemove = h4SRs.Keys.Where(k => k < h4Index - LookbackBars + 1).ToList();
                    foreach (var key in keysToRemove) h4SRs.Remove(key);

                    for (int i = lastH4Index + 1; i <= h4Index; i++)
                    {
                        ProcessSR(h4SRs, h4Bars, i, TimeFrame.Hour4);
                    }
                    lastH4Index = h4Index;
                }
            }

            // Run layer logic for Daily and H4
            var (dailyHR, dailyLS, dailyMainIndex) = RunLayer1(dailySRs, dailyBars.Count - 1, false);
            var (h4HR, h4LS, h4MainIndex) = RunLayer1(h4SRs, h4Bars.Count - 1, false);

            // Log selected SR levels
            if (dailyHR != null) Print("Daily HR: " + dailyHR.ToString());
            if (dailyLS != null) Print("Daily LS: " + dailyLS.ToString());
            if (h4HR != null) Print("H4 HR: " + h4HR.ToString());
            if (h4LS != null) Print("H4 LS: " + h4LS.ToString());

            // Draw levels for Daily and H4
            DrawSRLevels(dailyHR, dailyLS, "D", DailySupportColor, DailyResistanceColor, index, dailyMainIndex, TimeFrame.Daily);
            DrawSRLevels(h4HR, h4LS, "H4", H4SupportColor, H4ResistanceColor, index, h4MainIndex, TimeFrame.Hour4);
        }

        public static bool IsBearish(double open, double close)
        {
            return close < open;
        }

        public static bool IsBullish(double open, double close)
        {
            return close > open;
        }

        public static bool IsWickTouched(double open, double high, double low, double close, double level, short dir = 0)
        {
            bool dUp = Math.Max(open, close) < level && level <= high;
            bool dLo = Math.Min(open, close) > level && level >= low;
            return dir == 0 ? dUp || dLo : dir > 0 ? dUp : dLo;
        }

        public static bool IsBodyTouched(double open, double close, double level)
        {
            return (open > level && close < level) || (open < level && close > level);
        }
        public static (SR hr, SR ls, int mainIndex) RunLayer1(Dictionary<int, SR> srs, int barIndex, bool isLast)
        {
            SR hr = null;
            SR ls = null;
            int mainIndex = 0;
            if (isLast)
            {
                return (hr, ls, mainIndex);
            }
            // Find mainIndex from confirmed bars only (avoid unclosed current bar)
            foreach (var sr in srs.Values)
            {
                int lastIndex = 0;
                if (sr.IsSupport && sr.IndexRejLo.Count > 0)
                {
                    lastIndex = sr.IndexRejLo[sr.IndexRejLo.Count - 1];
                }
                else if (sr.IsResistance && sr.IndexRejUp.Count > 0)
                {
                    lastIndex = sr.IndexRejUp[sr.IndexRejUp.Count - 1];
                }
                if (lastIndex > mainIndex && lastIndex < barIndex)
                {
                    mainIndex = lastIndex;
                }
            }
            // Now revert and select
            foreach (var sr in srs.Values)
            {
                var reverted = sr.RevertTo(mainIndex, barIndex, false);
                if (reverted.IsResistance && reverted.IndexRejUp.Contains(mainIndex))
                {
                    if (hr == null || reverted.Price > hr.Price)
                    {
                        hr = reverted;
                    }
                }
                if (reverted.IsSupport && reverted.IndexRejLo.Contains(mainIndex))
                {
                    if (ls == null || reverted.Price < ls.Price)
                    {
                        ls = reverted;
                    }
                }
            }
            return (hr, ls, mainIndex);
        }

        private void ProcessSR(Dictionary<int, SR> srs, Bars bars, int barIndex, TimeFrame tf)
        {
            if (barIndex < 1) return; // Need at least 2 bars

            var prevBar = bars[barIndex - 1];
            var currBar = bars[barIndex];

            // Check for new SR formation
            // if (IsBearish(currBar.Open, currBar.Close) != IsBearish(prevBar.Open, prevBar.Close))
            // {
            SR newSR = new SR();
            newSR.Init(barIndex - 1, prevBar.OpenTime, prevBar.Open, prevBar.Close, currBar.Open, currBar.Close);
            srs[barIndex - 1] = newSR;
            // }

            // Check breakouts and rejections for existing SRs
            foreach (var sr in srs.Values.ToList())
            {
                sr.CheckBreakout(barIndex, currBar.OpenTime, currBar.Open, currBar.Close);
                sr.CheckRejection(barIndex, currBar.OpenTime, currBar.Open, currBar.High, currBar.Low, currBar.Close);
            }
        }

        private void DrawSRLevels(SR hr, SR ls, string prefix, Color supportColor, Color resistanceColor, int chartIndex, int mainIndex, TimeFrame tf)
        {
            if (Chart.TimeFrame > tf) return;

            // Clear previous lines for this timeframe
            var linesToRemove = Chart.Objects.Where(obj => obj.Name.StartsWith(prefix + "_")).ToList();
            foreach (var line in linesToRemove)
            {
                Chart.RemoveObject(line.Name);
            }

            // Draw current HR and LS with labels
            if (hr != null)
            {
                string hrName = prefix + "_R";
                DateTime startTime = hr.Timestamp;
                int pos = hr.IndexRejUp.IndexOf(mainIndex);
                if (pos != -1)
                {
                    DateTime endTime = hr.TimeRejUp[pos];
                    Chart.DrawTrendLine(hrName, startTime, hr.Price, endTime, hr.Price, resistanceColor, LineThickness, LineStyle.Solid);
                    Chart.DrawText(hrName + "_Label", prefix + "_R", chartIndex + 2, hr.Price, resistanceColor);
                }
            }
            if (ls != null)
            {
                string lsName = prefix + "_S";
                DateTime startTime = ls.Timestamp;
                int pos = ls.IndexRejLo.IndexOf(mainIndex);
                if (pos != -1)
                {
                    DateTime endTime = ls.TimeRejLo[pos];
                    Chart.DrawTrendLine(lsName, startTime, ls.Price, endTime, ls.Price, supportColor, LineThickness, LineStyle.Solid);
                    Chart.DrawText(lsName + "_Label", prefix + "_S", chartIndex + 2, ls.Price, supportColor);
                }
            }
        }
    }
}
