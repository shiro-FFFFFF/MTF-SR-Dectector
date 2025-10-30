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
    public int IndexFirstTouchH4S { get; set; } = -1;
    public int IndexFirstTouchH4R { get; set; } = -1;
    public int IndexFirstTouchH1S { get; set; } = -1;
    public int IndexFirstTouchH1R { get; set; } = -1;

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

    public bool CheckBreakout(int currBarIndex, DateTime currTime, double currOpen, double currClose, int dir = 0, bool debug = false)
    {
        bool b = cAlgo.MTFSRDectector.IsBodyTouched(currOpen, currClose, this.Price);
        if (this.Index + 2 <= currBarIndex)
        {
            if (b && (dir == 0 || (dir > 0 && cAlgo.MTFSRDectector.IsBullish(currOpen, currClose)) || (dir < 0 && cAlgo.MTFSRDectector.IsBearish(currOpen, currClose))))
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
            else return false;
        }
        return b;
    }

    public SR RevertTo(int index, int currBarIndex)
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
            IndexFirstTouchH4S = this.IndexFirstTouchH4S,
            IndexFirstTouchH4R = this.IndexFirstTouchH4R,
            IndexFirstTouchH1S = this.IndexFirstTouchH1S,
            IndexFirstTouchH1R = this.IndexFirstTouchH1R
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
                x.IsFresh = x.IndexBo.Contains(indexLastAction) || (x.IndexRejUp.Count == 0 && x.IndexRejLo.Count == 0);
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

        [Parameter("Daily Support Color", DefaultValue = "Green")]
        public Color DailySupportColor { get; set; }

        [Parameter("Daily Resistance Color", DefaultValue = "Red")]
        public Color DailyResistanceColor { get; set; }

        [Parameter("H4 Support Color", DefaultValue = "Blue")]
        public Color H4SupportColor { get; set; }

        [Parameter("H4 Resistance Color", DefaultValue = "Orange")]
        public Color H4ResistanceColor { get; set; }

        [Parameter("H4 Support BO Color", DefaultValue = "BlueViolet")]
        public Color H4SupportBoColor { get; set; }

        [Parameter("H4 Resistance BO Color", DefaultValue = "BlueViolet")]
        public Color H4ResistanceBoColor { get; set; }

        [Parameter("H1 Support BO Color", DefaultValue = "Cyan")]
        public Color H1SupportBoColor { get; set; }

        [Parameter("H1 Resistance BO Color", DefaultValue = "Cyan")]
        public Color H1ResistanceBoColor { get; set; }

        [Parameter("Lookback Bars", DefaultValue = 60)]
        public int LookbackBars { get; set; }

        [Parameter("Line Thickness", DefaultValue = 1)]
        public int LineThickness { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private Dictionary<int, SR> dailySRs = new Dictionary<int, SR>();
        private Dictionary<int, SR> h4SRs = new Dictionary<int, SR>();
        private Dictionary<int, SR> h1SRs = new Dictionary<int, SR>();
        private Bars dailyBars;
        private Bars h4Bars;
        private Bars h1Bars;
        private int lastDailyIndex = -1;
        private int lastH4Index = -1;
        private int lastH1Index = -1;
        private int dailyBarsCount = -1;
        private int h4BarsCount = -1;
        private int h1BarsCount = -1;
        private short dSignal = 0;
        private short h4Signal = 0;

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
            
            bool isNew = false;
            // Process Daily timeframe
            if (dailyBars != null && dailyBars.Count > 0)
            {
                int dailyIndex = dailyBars.Count - 1;
                if (dailyIndex > lastDailyIndex)
                {
                    for (int i = dailyIndex - LookbackBars * 2 + 1; i < dailyIndex; i++)
                    {
                        ProcessSR(dailySRs, dailyBars, i, TimeFrame.Daily);
                        // Print("D Index = " + i);
                    }
                    lastDailyIndex = dailyIndex;
                    isNew = true;
                }
            }

            // Process H4 timeframe
            if (h4Bars != null && h4Bars.Count > 0)
            {
                int h4Index = h4Bars.Count - 1;
                if (h4Index > lastH4Index)
                {
                    for (int i = h4Index - LookbackBars * 2 + 1; i < h4Index; i++)
                    {
                        ProcessSR(h4SRs, h4Bars, i, TimeFrame.Hour4);
                        // Print("H4 Index = " + i);
                    }
                    lastH4Index = h4Index;
                    isNew = true;
                }
            }

            // Process H1 timeframe
            if (h1Bars != null && h1Bars.Count > 0)
            {
                int h1Index = h1Bars.Count - 1;
                if (h1Index > lastH1Index)
                {
                    for (int i = h1Index - LookbackBars * 2 + 1; i < h1Index; i++)
                    {
                        ProcessSR(h1SRs, h1Bars, i, TimeFrame.Hour);
                        // Print("H1 Index = " + i);
                    }
                    lastH1Index = h1Index;
                    isNew = true;
                }
            }

            if (!isNew) return;

            // foreach (var sr in dailySRs.Values)
            // {
            //     Print("Daily SR at bar[" + sr.Index + "]: " + sr.ToString());
            // }
            // Run layer logic for Daily and H4
            var (dailyHR, dailyLS, dailyMainIndex) = RunLayer1(dailySRs, dailyBars.Count - 1, lastDailyIndex - LookbackBars * 2 - 1);
            var (h4HR, h4LS, h4MainIndex) = RunLayer1(h4SRs, h4Bars.Count - 1, lastH4Index - LookbackBars * 2 - 1);
            Print("\ndailyMainIndex: " + dailyMainIndex.ToString());
            Print("\nh4MainIndex: " + h4MainIndex.ToString());
            // Print("\nD_S reverted: " + (dailyLS != null ? dailyLS.RevertTo(dailyMainIndex-1, dailyBars.Count - 1).ToString() : "null"));

            // Log selected SR levels
            bool temp = true;
            if (temp && dailyHR != null) Print("Daily HR: " + dailyHR.ToString());
            if (temp && dailyLS != null) Print("Daily LS: " + dailyLS.ToString());
            if (temp && h4HR != null) Print("H4 HR: " + h4HR.ToString());
            if (temp && h4LS != null) Print("H4 LS: " + h4LS.ToString());

            // Draw levels for Daily and H4
            DrawSRLevels(dailyHR, dailyLS, "D", DailySupportColor, DailyResistanceColor, index, dailyMainIndex, new DateTime(), new DateTime(), TimeFrame.Daily);
            DrawSRLevels(h4HR, h4LS, "H4", H4SupportColor, H4ResistanceColor, index, h4MainIndex, new DateTime(), new DateTime(), TimeFrame.Hour4);

            // Run D-H4 Layer 2
            SR h4BoS;
            SR h4BoR;
            DateTime timeH4BoS;
            DateTime timeH4BoR;
            (h4BoS, h4BoR, timeH4BoS, timeH4BoR, dSignal) = RunLayer2("D", "H4", dailyHR, dailyLS, dailyMainIndex, dailyBars, h4Bars, h4SRs, dSignal);
            DrawSRLevels(h4BoR, h4BoS, "H4_BO", H4SupportBoColor, H4ResistanceBoColor, index, 0, timeH4BoS, timeH4BoR, TimeFrame.Hour4);

            // Run H4-H1 Layer 2
            SR h1BoS;
            SR h1BoR;
            DateTime timeH1BoS;
            DateTime timeH1BoR;
            (h1BoS, h1BoR, timeH1BoS, timeH1BoR, h4Signal) = RunLayer2("H4", "H1", h4HR, h4LS, h4MainIndex, h4Bars, h1Bars, h1SRs, h4Signal);
            DrawSRLevels(h1BoR, h1BoS, "H1_BO", H1SupportBoColor, H1ResistanceBoColor, index, 0, timeH1BoS, timeH1BoR, TimeFrame.Hour);
            
            // Log BO levels
            if (temp && h4BoS != null) Print("H4 BO_S at " + timeH4BoS + ": " + h4BoS.ToString());
            if (temp && h4BoR != null) Print("H4 BO_R at " + timeH4BoR + ": " + h4BoR.ToString());
            if (temp && h1BoS != null) Print("H1 BO_S at " + timeH1BoS + ": " + h1BoS.ToString());
            if (temp && h1BoR != null) Print("H1 BO_R at " + timeH1BoR + ": " + h1BoR.ToString());
            Chart.DrawStaticText("D_signal", "D Signal: " + dSignal.ToString(), VerticalAlignment.Top, HorizontalAlignment.Right, dSignal == 0 ? Color.White : (dSignal > 0 ? Color.Lime : Color.Red));
            Chart.DrawStaticText("H4_signal", "\nH4 Signal: " + h4Signal.ToString(), VerticalAlignment.Top, HorizontalAlignment.Right, h4Signal == 0 ? Color.White : (h4Signal > 0 ? Color.Lime : Color.Red));
            Print("D Signal: " + dSignal.ToString() + ", H4 Signal: " + h4Signal.ToString());
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

        private void DrawSRLevels(SR r, SR s, string prefix, Color supportColor, Color resistanceColor, int chartIndex, int mainIndex, DateTime timeBoS, DateTime timeBoR, TimeFrame tf)
        {
            if (Chart.TimeFrame > tf) return;

            // Clear previous lines for this timeframe
            var linesToRemove = Chart.Objects.Where(obj => obj.Name.StartsWith(prefix + "_")).ToList();
            foreach (var line in linesToRemove)
            {
                Chart.RemoveObject(line.Name);
            }

            // Draw R and S with labels
            if (!prefix.Contains("BO"))
            {
                if (r != null)
                {
                    string hrName = prefix + "_R";
                    DateTime startTime = r.Timestamp;
                    int pos = r.IndexRejUp.IndexOf(mainIndex);
                    if (pos != -1)
                    {
                        DateTime endTime = r.TimeRejUp[pos];
                        Chart.DrawTrendLine(hrName, startTime, r.Price, endTime, r.Price, resistanceColor, LineThickness, LineStyle.Solid);
                        Chart.DrawText(hrName + "_Label", prefix + "_R", chartIndex + 2, r.Price, resistanceColor);
                    }
                }
                if (s != null)
                {
                    string lsName = prefix + "_S";
                    DateTime startTime = s.Timestamp;
                    int pos = s.IndexRejLo.IndexOf(mainIndex);
                    if (pos != -1)
                    {
                        DateTime endTime = s.TimeRejLo[pos];
                        Chart.DrawTrendLine(lsName, startTime, s.Price, endTime, s.Price, supportColor, LineThickness, LineStyle.Solid);
                        Chart.DrawText(lsName + "_Label", prefix + "_S", chartIndex + 2, s.Price, supportColor);
                    }
                }
            }
            else
            {
                if (r != null)
                {
                    string rName = prefix + "_R";
                    Chart.DrawTrendLine(rName, r.Timestamp, r.Price, timeBoR, r.Price, resistanceColor, LineThickness, LineStyle.Dots);
                    Chart.DrawText(rName + "_Label", prefix + "_R", chartIndex + 2, r.Price, resistanceColor);
                }
                if (s != null)
                {
                    string sName = prefix + "_S";
                    Chart.DrawTrendLine(sName, s.Timestamp, s.Price, timeBoS, s.Price, supportColor, LineThickness, LineStyle.Dots);
                    Chart.DrawText(sName + "_Label", prefix + "_S", chartIndex + 2, s.Price, supportColor);
                }
            }
        }
        public static (SR hr, SR ls, int mainIndex) RunLayer1(Dictionary<int, SR> srs, int barIndex, int lookbackIndex)
        {
            SR hr = null;
            SR ls = null;
            int mainIndex = 0;

            foreach (var l in srs.Values)
            {
                var sr = l;
                if (sr.Index < lookbackIndex) continue;
                int lastIndex = 0;
                SR s = null;
                SR r = null;
                if (sr.IndexRejLo.Count > 0) s = sr.RevertTo(sr.IndexRejLo[^1], barIndex);
                if (sr.IndexRejUp.Count > 0) r = sr.RevertTo(sr.IndexRejUp[^1], barIndex);
                if (s != null && s.IsSupport)
                {
                    lastIndex = sr.IndexRejLo[^1];
                }
                if (r != null && r.IsResistance)
                {
                    lastIndex = Math.Max(lastIndex, sr.IndexRejUp[^1]);
                }
                if (lastIndex > mainIndex && lastIndex < barIndex)
                {
                    mainIndex = lastIndex;
                }
            }
            // Now revert and select
            foreach (var sr in srs.Values)
            {
                if (sr.Index < lookbackIndex) continue;
                var reverted1 = sr.RevertTo(mainIndex, barIndex);
                var reverted2 = sr.RevertTo(mainIndex - 1, barIndex);
                if (reverted1.IsResistance && reverted2.IsFresh && reverted1.IndexRejUp.Count > 0 && reverted1.IndexRejUp[^1] == mainIndex)
                {
                    if (hr == null || reverted1.Price > hr.Price)
                    {
                        hr = reverted1;
                        continue;
                    }
                }
                if (reverted1.IsSupport && reverted2.IsFresh && reverted1.IndexRejLo.Count > 0 && reverted1.IndexRejLo[^1] == mainIndex)
                {
                    if (ls == null || reverted1.Price < ls.Price)
                    {
                        ls = reverted1;
                    }
                }
            }
            return (hr, ls, mainIndex);
        }

        private (SR hs, SR lr, DateTime timeBoS, DateTime timeBoR, short signal) RunLayer2(string htf, string ltf, SR hr, SR ls, int mainIndex, Bars htfBars, Bars ltfBars, Dictionary<int, SR> srs, short prevSignal)
        {
            // step 1: get ltf main bar
            if (mainIndex < 0 || mainIndex >= htfBars.Count) return (null, null, new DateTime(), new DateTime(), 0);

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
                    if (firstLTFForS == -1) Print("Touch S: " + touchS);
                    if (touchS && firstLTFForS == -1)
                    {
                        firstLTFForS = i;
                        if(htf == "D") ls.IndexFirstTouchH4S = i;
                        if(htf == "H4") ls.IndexFirstTouchH1S = i;
                        Print(">>>>>>>>>> First touch S found at " + ltf + " " + i);
                    }
                    // Check for hr (resistance) - upward wick touch
                    bool touchR = hr != null && (IsWickTouched(ltfBar.Open, ltfBar.High, ltfBar.Low, ltfBar.Close, hr.Price) || IsBodyTouched(ltfBar.Open, ltfBar.Close, hr.Price));
                    if (firstLTFForR == -1) Print("Touch R: " + touchR);
                    if (touchR && firstLTFForR == -1)
                    {
                        firstLTFForR = i;
                        if(htf == "D") hr.IndexFirstTouchH4R = i;
                        if(htf == "H4") hr.IndexFirstTouchH1R = i;
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

            // Step 2: find unbroken SR levels (LR & HS) before first touching LTF bar
            int indexBoR = -1;
            SR lr = null;
            if (firstLTFForS != -1)
            {
                int ltfMainIndex = -1;
                for (int i = firstLTFForS - 1; i >= firstLTFForS - LookbackBars; i--)
                {
                    SR cand = srs[i].RevertTo(firstLTFForS, ltfBars.Count - 1);
                    if (cand.IsResistance && cand.IsClassic && cand.TimeBo.Count == 0 && (lr == null || cand.Price <= lr.Price))
                    {
                        lr = srs[i];
                        Print(">>>>>>>>>> New " + ltf + " LR: " + cand.ToString());
                        ltfMainIndex = i;
                    }
                }

                // Step 3: Check for BO of LR within this and next HTF bars
                if (lr != null)
                {
                    DateTime startTime2 = (ltfMainIndex + 1 < ltfBars.Count) ? ltfBars[ltfMainIndex + 1].OpenTime : DateTime.MaxValue;
                    DateTime endTime2 = (mainIndex + 2 < htfBars.Count) ? htfBars[mainIndex + 2].OpenTime : DateTime.MaxValue;
                    if (lr.TimeBo.Count > 0)
                    {
                        for (int i = ltfBars.Count - 1; i > ltfMainIndex; i--)
                        {
                            int idx = lr.IndexBo.IndexOf(i);
                            if (idx != -1 && lr.TimeBo[idx] >= startTime2 && lr.TimeBo[idx] < endTime2)
                            {
                                indexBoR = i;
                                Print(">>>>>>>>>> LR BO found at " + ltf + " bar " + indexBoR + "(" + lr.TimeBo[idx] + ")");
                            }
                        }
                    }
                    if (indexBoR == -1 && IsWickTouched(ltfBars[^1].Open, ltfBars[^1].High, ltfBars[^1].Low, ltfBars[^1].Close, hr.Price) || IsBodyTouched(ltfBars[^1].Open, ltfBars[^1].Close, lr.Price))
                    {
                        indexBoR = ltfBars.Count - 1;
                        Print(">>>>>>>>>> LR BO found at last " + ltf + " bar (UNCONFIRMED)");
                    }
                }
            }

            int indexBoS = -1;
            SR hs = null;
            if (firstLTFForR != -1)
            {
                int ltfMainIndex = -1;
                for (int i = firstLTFForR - 1; i >= firstLTFForR - LookbackBars; i--)
                {
                    SR cand = srs[i].RevertTo(firstLTFForS, ltfBars.Count - 1);
                    if (cand.IsSupport && cand.IsClassic && cand.TimeBo.Count == 0 && (hs == null || cand.Price >= hs.Price))
                    {
                        hs = srs[i];
                        Print(">>>>>>>>>> New " + ltf + " HS: " + cand.ToString());
                        ltfMainIndex = i;
                    }
                }

                // Step 3: Check for BO of HS within this and next HTF bars
                if (hs != null)
                {
                    DateTime startTime2 = (ltfMainIndex + 1 < ltfBars.Count) ? ltfBars[ltfMainIndex + 1].OpenTime : DateTime.MaxValue;
                    DateTime endTime2 = (mainIndex + 2 < htfBars.Count) ? htfBars[mainIndex + 2].OpenTime : DateTime.MaxValue;
                    if (hs.TimeBo.Count > 0)
                    {
                        for (int i = ltfBars.Count - 1; i > ltfMainIndex; i--)
                        {
                            int idx = hs.IndexBo.IndexOf(i);
                            if (idx != -1 && hs.TimeBo[idx] >= startTime2 && hs.TimeBo[idx] < endTime2)
                            {
                                indexBoS = i;
                                Print(">>>>>>>>>> HS BO found at " + ltf + " bar " + indexBoS + "(" + hs.TimeBo[idx] + ")");
                            }
                        }
                    }
                    if (indexBoS == -1 && IsWickTouched(ltfBars[^1].Open, ltfBars[^1].High, ltfBars[^1].Low, ltfBars[^1].Close, hs.Price) || IsBodyTouched(ltfBars[^1].Open, ltfBars[^1].Close, hs.Price))
                    {
                        indexBoS = ltfBars.Count - 1;
                        Print(">>>>>>>>>> HS BO found at last " + ltf + " bar (UNCONFIRMED)");
                    }
                }
            }

            DateTime timeBoR = new DateTime();
            DateTime timeBoS = new DateTime();
            if (indexBoR != -1) timeBoR = ltfBars[indexBoR].OpenTime;
            if (indexBoS != -1) timeBoS = ltfBars[indexBoS].OpenTime;
            Print("\nHS: " + (hs != null ? hs.ToString() : "null") + "\nLR: " + (lr != null ? lr.ToString() : "null") + "\ntimeBoS: " + timeBoS.ToString("yyyy-MM-dd HH:mm:ss") + "\ntimeBoR: " + timeBoR.ToString("yyyy-MM-dd HH:mm:ss") + "\nprevSignal: " + prevSignal.ToString());
            // Step 4: Determine signal
            if (indexBoS == -1 && indexBoR == -1) return (hs, lr, timeBoS, timeBoR, 0); // no signal case 1
            if (indexBoS == ltfBars.Count - 1 || indexBoR == ltfBars.Count - 1) return (hs, lr, timeBoS, timeBoR, 0); // no signal case 2
            if (Math.Max(indexBoS, indexBoR) > 0 && indexBoS == indexBoR) return (null, null, new DateTime(), new DateTime(), 0); // no signal case 3
            if (Math.Max(indexBoS, indexBoR) > 0 && indexBoS < indexBoR) return (null, lr, new DateTime(), timeBoR, 1); // bullish signal
            if (Math.Max(indexBoS, indexBoR) > 0 && indexBoS > indexBoR) return (hs, null, timeBoS, new DateTime(), -1); // bearish signal

            return (hs, lr, timeBoS, timeBoR, prevSignal);
        }
    }
}