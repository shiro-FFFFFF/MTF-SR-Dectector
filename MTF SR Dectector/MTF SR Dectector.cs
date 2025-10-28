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
    public int FirstTouchingH4IndexForS { get; set; } = -1;
    public int FirstTouchingH4IndexForR { get; set; } = -1;
    public int FirstTouchingH1IndexForS { get; set; } = -1;
    public int FirstTouchingH1IndexForR { get; set; } = -1;

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
            TimeRejLo = new List<DateTime>(this.TimeRejLo),
            FirstTouchingH4IndexForS = this.FirstTouchingH4IndexForS,
            FirstTouchingH4IndexForR = this.FirstTouchingH4IndexForR,
            FirstTouchingH1IndexForS = this.FirstTouchingH1IndexForS,
            FirstTouchingH1IndexForR = this.FirstTouchingH1IndexForR
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
                    if (pos != -1)
                    {
                        x.IndexBo.RemoveAt(pos);
                        x.TimeBo.RemoveAt(pos);
                    }
                }
                if (this.IndexRejUp.IndexOf(i) != -1)
                {
                    isReverted = true;
                    int pos = x.IndexRejUp.IndexOf(i);
                    if (pos != -1)
                    {
                        x.IndexRejUp.RemoveAt(pos);
                        x.TimeRejUp.RemoveAt(pos);
                    }
                }
                if (this.IndexRejLo.IndexOf(i) != -1)
                {
                    isReverted = true;
                    int pos = x.IndexRejLo.IndexOf(i);
                    if (pos != -1)
                    {
                        x.IndexRejLo.RemoveAt(pos);
                        x.TimeRejLo.RemoveAt(pos);
                    }
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
                int a = x.IndexBo.Count > 0 ? x.IndexBo[^1] : 0;
                int b = x.IndexRejUp.Count > 0 ? x.IndexRejUp[^1] : 0;
                int c = x.IndexRejLo.Count > 0 ? x.IndexRejLo[^1] : 0;
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
        private Bars h1Bars;
        private int lastDailyIndex = -1;
        private int lastH4Index = -1;
        private int dailyBarsCount = -1;
        private int h4BarsCount = -1;
        private int h1BarsCount = -1;


        protected override void Initialize()
        {
            // To learn more about cTrader Algo visit our Help Center:
            // https://help.ctrader.com/ctrader-algo/

            Print(Message);
            Print(Chart.TimeFrame.ToString());
            dailyBars = MarketData.GetBars(TimeFrame.Daily);
            dailyBarsCount = dailyBars.Count;
            h4Bars = MarketData.GetBars(TimeFrame.Hour4);
            h4BarsCount = h4Bars.Count;
            h1Bars = MarketData.GetBars(TimeFrame.Hour);
            h1BarsCount = h1Bars.Count;
            while(h4Bars.LastBar != h4Bars[^1])
            {
                Print(h4Bars.LastBar);
            }
            Print("\nLast Daily bar index: " + dailyBars.Count.ToString() + " @" + dailyBars[^1].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nLast H4 bar index: " + h4Bars.Count.ToString() + " @" + h4Bars[^1].OpenTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Print("\nD bar[1405]: " + dailyBars[1405].Close + " @" + dailyBars[1405].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nD bar[1411]: " + dailyBars[1411].Close + " @" + dailyBars[1411].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nH4 bar[1154]: " + h4Bars[1154].Close + " @" + h4Bars[1154].OpenTime.ToString("yyyy-MM-dd HH:mm:ss") + "\nH4 bar[1139]: " + h4Bars[1139].Close + " @" + h4Bars[1139].OpenTime.ToString("yyyy-MM-dd HH:mm:ss"));

            // Initialize SR dictionaries
            dailySRs = new Dictionary<int, SR>();
            h4SRs = new Dictionary<int, SR>();
        }

        public override void Calculate(int index)
        {
            // Print(dailyBarsCount + "\t" + h4BarsCount + "\t" + h1BarsCount);
            int checkIndex = -1;
            switch (Chart.TimeFrame.ToString())
            {
                case "Daily":
                    checkIndex = dailyBarsCount - 1;
                    break;
                case "Hour4":
                    checkIndex = h4BarsCount - 1;
                    break;
                case "Hour":
                    checkIndex = h1BarsCount - 1;
                    break;
            }
            // Print(checkIndex);
            if (checkIndex > index)
            {
                // Print("checkIndex=" + checkIndex.ToString() + " == " + "index=" + index.ToString() + ": " + (checkIndex > index).ToString());
                return;
            }
            
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
                        Print("D Index = " + i);
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
                        Print("H4 Index = " + i);
                    }
                    lastH4Index = h4Index;
                }
            }
            Print("\nh4Bars: \n" + h4Bars[1156].ToString());
            // Run layer logic for Daily and H4
            var (dailyHR, dailyLS, dailyMainIndex) = RunLayer1(dailySRs, dailyBars.Count - 1);
            var (h4HR, h4LS, h4MainIndex) = RunLayer1(h4SRs, h4Bars.Count - 1);
            Print("\ndailyMainIndex: " + dailyMainIndex.ToString());
            Print("\nh4MainIndex: " + h4MainIndex.ToString());

            // Run D-H4 Layer 2
            RunLayer2("D", "H4", dailyHR, dailyLS, dailyMainIndex, dailyBars, h4Bars);

            // Run H4-H1 Layer 2
            RunLayer2("H4", "H1", h4HR, h4LS, h4MainIndex, h4Bars, h1Bars);

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

        private void RunLayer2(string htf, string ltf, SR hr, SR ls, int mainIndex, Bars htfBars, Bars ltfBars)
        {
            if (mainIndex < 0 || mainIndex >= htfBars.Count) return;

            var htfBar = htfBars[mainIndex];
            DateTime startTime = htfBar.OpenTime;
            DateTime endTime = (mainIndex + 1 < htfBars.Count) ? htfBars[mainIndex + 1].OpenTime : DateTime.MaxValue;

            Print("Layer2 " + htf + "-" + ltf + ": " + htf + "MainIndex=" + mainIndex + ", startTime=" + startTime + ", endTime=" + endTime);
            Print(htf + " bar: O=" + htfBar.Open + ", H=" + htfBar.High + ", L=" + htfBar.Low + ", C=" + htfBar.Close);
            if (ls != null) Print(htf + "_ls price: " + ls.Price);
            if (hr != null) Print(htf + "_hr price: " + hr.Price);

            int firstLTFForS = -1;
            int firstLTFForR = -1;

            for (int i = 0; i < ltfBars.Count; i++)
            {
                var ltfBar = ltfBars[i];
                if (ltfBar.OpenTime >= startTime && ltfBar.OpenTime < endTime)
                {
                    Print("Checking " + ltf + " bar " + i + " at " + ltfBar.OpenTime + ": O=" + ltfBar.Open + ", H=" + ltfBar.High + ", L=" + ltfBar.Low + ", C=" + ltfBar.Close);
                    // Check for ls (support) - downward wick touch
                    bool touchS = ls != null && (IsWickTouched(ltfBar.Open, ltfBar.High, ltfBar.Low, ltfBar.Close, ls.Price) || IsBodyTouched(ltfBar.Open, ltfBar.Close, ls.Price));
                    Print("Touch S: " + touchS);
                    if (touchS && firstLTFForS == -1)
                    {
                        firstLTFForS = i;
                        if(htf == "D") ls.FirstTouchingH4IndexForS = i;
                        if(htf == "H4") ls.FirstTouchingH1IndexForS = i;
                        Print(">>>>>>>>>> First touch S found at " + ltf + " " + i);
                    }
                    // Check for hr (resistance) - upward wick touch
                    bool touchR = hr != null && (IsWickTouched(ltfBar.Open, ltfBar.High, ltfBar.Low, ltfBar.Close, hr.Price) || IsBodyTouched(ltfBar.Open, ltfBar.Close, hr.Price));
                    Print("Touch R: " + touchR);
                    if (touchR && firstLTFForR == -1)
                    {
                        firstLTFForR = i;
                        if(htf == "D") hr.FirstTouchingH4IndexForR = i;
                        if(htf == "H4") hr.FirstTouchingH1IndexForR = i;
                        Print(">>>>>>>>>> First touch R found at " + ltf + " " + i);
                    }
                    // Since we expect up to 6 H4 or 4 H1 candles, we can break early if both are found
                    if (firstLTFForS != -1 && firstLTFForR != -1) break;
                }
            }

            // Print the first touching indices
            if (firstLTFForS != -1)
            {
                Print("first " + ltf + " bar for " + htf + "_s: " + firstLTFForS);
            }
            if (firstLTFForR != -1)
            {
                Print("first " + ltf + " bar for " + htf + "_r: " + firstLTFForR);
            }
        }

        private void RunLayer2_D_H4(SR d_hr, SR d_ls, int dailyMainIndex, Bars dailyBars, Bars h4Bars)
        {
            if (dailyMainIndex < 0 || dailyMainIndex >= dailyBars.Count) return;

            var dailyBar = dailyBars[dailyMainIndex];
            DateTime startTime = dailyBar.OpenTime;
            DateTime endTime = (dailyMainIndex + 1 < dailyBars.Count) ? dailyBars[dailyMainIndex + 1].OpenTime : DateTime.MaxValue;

            Print("Layer2 D-H4: dailyMainIndex=" + dailyMainIndex + ", startTime=" + startTime + ", endTime=" + endTime);
            Print("Daily bar: O=" + dailyBar.Open + ", H=" + dailyBar.High + ", L=" + dailyBar.Low + ", C=" + dailyBar.Close);
            if (d_ls != null) Print("d_ls price: " + d_ls.Price);
            if (d_hr != null) Print("d_hr price: " + d_hr.Price);

            int firstH4ForS = -1;
            int firstH4ForR = -1;

            for (int i = 0; i < h4Bars.Count; i++)
            {
                var h4Bar = h4Bars[i];
                if (h4Bar.OpenTime >= startTime && h4Bar.OpenTime < endTime)
                {
                    Print("Checking H4 bar " + i + " at " + h4Bar.OpenTime + ": O=" + h4Bar.Open + ", H=" + h4Bar.High + ", L=" + h4Bar.Low + ", C=" + h4Bar.Close);
                    // Check for d_ls (support) - downward wick touch
                    bool touchS = d_ls != null && (IsWickTouched(h4Bar.Open, h4Bar.High, h4Bar.Low, h4Bar.Close, d_ls.Price) || IsBodyTouched(h4Bar.Open, h4Bar.Close, d_hr.Price));
                    Print("Touch S: " + touchS);
                    if (touchS && firstH4ForS == -1)
                    {
                        firstH4ForS = i;
                        d_ls.FirstTouchingH4IndexForS = i;
                        Print(">>>>>>>>>> First touch S found at H4 " + i);
                    }
                    // Check for d_hr (resistance) - upward wick touch
                    bool touchR = d_hr != null && (IsWickTouched(h4Bar.Open, h4Bar.High, h4Bar.Low, h4Bar.Close, d_hr.Price) || IsBodyTouched(h4Bar.Open, h4Bar.Close, d_hr.Price));
                    Print("Touch R: " + touchR);
                    if (touchR && firstH4ForR == -1)
                    {
                        firstH4ForR = i;
                        d_hr.FirstTouchingH4IndexForR = i;
                        Print(">>>>>>>>>> First touch R found at H4 " + i);
                    }
                    // Since we expect up to 6 H4 candles, we can break early if both are found
                    if (firstH4ForS != -1 && firstH4ForR != -1) break;
                }
            }

            // Print the first touching indices
            if (firstH4ForS != -1)
            {
                Print("first h4 bar for d_s: " + firstH4ForS);
            }
            if (firstH4ForR != -1)
            {
                Print("first h4 bar for d_r: " + firstH4ForR);
            }
        }

        private void RunLayer2_H4_H1(SR h4_hr, SR h4_ls, int h4MainIndex, Bars h4Bars, Bars h1Bars)
        {
            if (h4MainIndex < 0 || h4MainIndex >= h4Bars.Count) return;

            var h4Bar = h4Bars[h4MainIndex];
            DateTime startTime = h4Bar.OpenTime;
            DateTime endTime = (h4MainIndex + 1 < h4Bars.Count) ? h4Bars[h4MainIndex + 1].OpenTime : DateTime.MaxValue;

            Print("Layer2 H4-H1: h4MainIndex=" + h4MainIndex + ", startTime=" + startTime + ", endTime=" + endTime);
            Print("H4 bar: O=" + h4Bar.Open + ", H=" + h4Bar.High + ", L=" + h4Bar.Low + ", C=" + h4Bar.Close);
            if (h4_ls != null) Print("h4_ls price: " + h4_ls.Price);
            if (h4_hr != null) Print("h4_hr price: " + h4_hr.Price);

            int firstH1ForS = -1;
            int firstH1ForR = -1;

            for (int i = 0; i < h1Bars.Count; i++)
            {
                var h1Bar = h1Bars[i];
                if (h1Bar.OpenTime >= startTime && h1Bar.OpenTime < endTime)
                {
                    Print("Checking H1 bar " + i + " at " + h1Bar.OpenTime + ": O=" + h1Bar.Open + ", H=" + h1Bar.High + ", L=" + h1Bar.Low + ", C=" + h1Bar.Close);
                    // Check for h4_ls (support) - downward wick touch
                    bool touchS = h4_ls != null && (IsWickTouched(h1Bar.Open, h1Bar.High, h1Bar.Low, h1Bar.Close, h4_ls.Price) || IsBodyTouched(h1Bar.Open, h1Bar.Close, h4_ls.Price));
                    Print("Touch S: " + touchS);
                    if (touchS && firstH1ForS == -1)
                    {
                        firstH1ForS = i;
                        h4_ls.FirstTouchingH1IndexForS = i;
                        Print(">>>>>>>>>> First touch S found at H1 " + i);
                    }
                    // Check for h4_hr (resistance) - upward wick touch
                    bool touchR = h4_hr != null && (IsWickTouched(h1Bar.Open, h1Bar.High, h1Bar.Low, h1Bar.Close, h4_hr.Price) || IsBodyTouched(h1Bar.Open, h1Bar.Close, h4_hr.Price));
                    Print("Touch R: " + touchR);
                    if (touchR && firstH1ForR == -1)
                    {
                        firstH1ForR = i;
                        h4_hr.FirstTouchingH1IndexForR = i;
                        Print(">>>>>>>>>> First touch R found at H1 " + i);
                    }
                    // Since we expect up to 4 H1 candles, we can break early if both are found
                    if (firstH1ForS != -1 && firstH1ForR != -1) break;
                }
            }

            // Print the first touching indices
            if (firstH1ForS != -1)
            {
                Print("first h1 bar for h4_s: " + firstH1ForS);
            }
            if (firstH1ForR != -1)
            {
                Print("first h1 bar for h4_r: " + firstH1ForR);
            }
        }

        public static (SR hr, SR ls, int mainIndex) RunLayer1(Dictionary<int, SR> srs, int barIndex)
        {
            SR hr = null;
            SR ls = null;
            int mainIndex = 0;
            // if (isLast)
            // {
            //     return (hr, ls, mainIndex);
            // }
            // Find mainIndex from confirmed bars only (avoid unclosed current bar)
            foreach (var sr in srs.Values)
            {
                int lastIndex = 0;
                if (sr.IsSupport && sr.IndexRejLo.Count > 0)
                {
                    lastIndex = sr.IndexRejLo[^1];
                }
                else if (sr.IsResistance && sr.IndexRejUp.Count > 0)
                {
                    lastIndex = sr.IndexRejUp[^1];
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
