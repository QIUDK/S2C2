using System;
using System.Collections;
using System.Collections.Generic;
using TradeLink.Common;
using TradeLink.API;
using System.ComponentModel;
using System.Reflection;
using CommonUtilityAlbert;
using CommonUtilityAlbertA;
using ATSGlobalIndicator;
using ATSGlobalIndicatorPersonal;

namespace ResponsesATSPersonal
{
    // make sure your response is based on ResponseTemplateBasePub
    public class Mod_TD_combinationResponsePub : ResponseTemplateBasePub
    {
        // parameters of this system       
        [Description("BarType")]
        public BarInterval BarType { get { return _barType; } set { _barType = value; } }
        [Description("NumItemPerBar")]
        public int NumItemPerBar { get { return _numTickPerBar; } set { _numTickPerBar = value; } } 
        [Description("Bars back when calculating Mod_Mod_TD_combination")]
        public int BarsBack { get { return _barsback; } set { _barsback = value; } }
        [Description("Shutdown time")]
        public int Shutdown { get { return _shutdowntime; } set { _shutdowntime = value; } }        
        [Description("GenericParameter")]
        public String GenericParameter{get { return _genericParameter; } set { _genericParameter = (value); }}
        [Description("UseMod_TD_combinationFromATSGlobalIndicator")]
        public Boolean UseMod_TD_combinationFromATSGlobalIndicator { get { return _useMod_TD_combinationFromATSGlobalIndicator; } set { _useMod_TD_combinationFromATSGlobalIndicator = value; } }
        [Description("Entry size when signal is found")]
        public int EntrySize { get { return _entrysize; } set { _entrysize = value; } }
        [Description("Total Profit")]
        public decimal TotalProfit { get { return _totalprofit; } set { _totalprofit = value; } }
 
        public Mod_TD_combinationResponsePub() : this(true) { }
        public Mod_TD_combinationResponsePub(bool prompt)
        {
            _black = !prompt;
            // handle when new symbols are added to the active tracker
            _active.NewTxt += new TextIdxDelegate(_active_NewTxt);

            // set our indicator names, in case we import indicators into R
            // or excel, or we want to view them in gauntlet or kadina
            Indicators = new string[] { "Time", "BuySignalStr", "SellSignalStr", "LastHigh", "LastLow", "LastClose"};
        }
        public override void Reset()
        {
            // enable prompting of system parameters to user,
            // so they do not have to recompile to change things
            ParamPrompt.Popup(this, true, _black);

            Int32 numOfInterval = 1;
            BarInterval[] intervaltypes = new BarInterval[numOfInterval];
            Int32[] intervalValues = new Int32[numOfInterval];
            for (Int32 i = 0; i < numOfInterval; i++)
            {
                intervaltypes[i] = BarType;
                intervalValues[i] = NumItemPerBar;
            }

            _blt = new BarListTracker(intervalValues, intervaltypes);

            _blt.GotNewBar += new SymBarIntervalDelegate(blt_GotNewBar);

            GenericParamUpdateHelper.updateParam(this, GenericParameter, false);

            Hashtable Mod_TD_combinationInputParamTmp = new Hashtable();
            Mod_TD_combinationInputParamTmp["lookbackperiod"] = _barsback;
            Mod_TD_combinationInputParamTmp["minmumsignallength"] = _barsback;
            _Mod_TD_combinationFromATSGlobalIndicator = new ATSGlobalIndicatorPersonal.Mod_TD_combination();
            _Mod_TD_combinationFromATSGlobalIndicator.Param = Mod_TD_combinationInputParamTmp;
            _Mod_TD_combinationFromATSGlobalIndicator.Initializatin();
            _isShutDown = false;
            TotalProfit = 0m;
            _pt.Clear();
        }
        void _active_NewTxt(string txt, int idx)
        {
            // go ahead and notify any other trackers about this symbol
            _wait.addindex(txt, false);
        }
        void blt_GotNewBar(string symbol, int interval)
        {

            int idx = _active.getindex(symbol);
            Tick tOther = _kt[symbol];

            // calculate the Mod_Mod_TD_combination using high/low/close prices for so many bars back
            BarList _myBars = _blt[symbol, interval];
            // make sure the lastBar is full
            //Bar _lastBar = _myBars[_myBars.Count - 2];
            //Bar _secondBar = _myBars[_myBars.Count - 3];
            //Bar _thirdBar = _myBars[_myBars.Count - 4];
            //Bar _forthBar = _myBars[_myBars.Count - 5];
            //Bar _sixthBar = _myBars[_myBars.Count - 7];
            //Bar _seventhBar = _myBars[_myBars.Count - 8];
            //Bar _fifthBar = _myBars[_myBars.Count - 6];
            //Bar _ninthBar = _myBars[_myBars.Count - 10];
            //Bar _eighthBar = _myBars[_myBars.Count - 9];
            for (int i = 1; i <= 10; i++ )
            {
                _barVec[i] = _myBars[_myBars.Count - 1 - i];
            }
            if (UseMod_TD_combinationFromATSGlobalIndicator )
            {
                _Mod_TD_combinationFromATSGlobalIndicator.UpdateValue(_barVec);
                BuySignal = _Mod_TD_combinationFromATSGlobalIndicator.GetBuySignal();
                SellSignal = _Mod_TD_combinationFromATSGlobalIndicator.GetSellSignal();
            }
            //else
            //{
            //    SMA = Calc.Avg(Calc.EndSlice(_blt[symbol].Open(), _barsback));
            //}
            // wait until we have the Mod_Mod_TD_combination+ and the Mod_Mod_TD_combination-
            //if (BuySignal == 0 && SellSignal == 0)
            //    return;


            //ensure we aren't waiting for previous order to fill
            if (!_wait[symbol])
            {

                // if we're flat and not waiting
                // if Mod_Mod_TD_combination+ is above Mod_Mod_TD_combination-, buy
                if (Money_state == 0 && BuySignal == 1)
                {
                    Money_state = 1;
                    D("TD_BuySignal appears, buy");
                    //sendorder(new BuyMarket(symbol, EntrySize));
                    _side = true;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                    // wait for fill
                    _wait[symbol] = true;
                }
                // otherwise if Mod_Mod_TD_combination+ is above Mod_Mod_TD_combination-, sell
                if (Money_state == 1 && SellSignal == 1)
                {
                    Money_state = 0;
                    D("TD_SellSignal appears, sell");
                    //sendorder(new SellMarket(symbol, EntrySize));
                    _side = false;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                    // wait for fill
                    _wait[symbol] = true;
                }

            }

            // this way we can debug our indicators during development
            // indicators are sent in the same order as they are named above
            sendindicators(new string[] { _time.ToString(),
                                          BuySignal.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          SellSignal.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          _barVec[1].High.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          _barVec[1].Low.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          _barVec[1].Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          //_previousBar.High.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          //_previousBar.Low.ToString("F5", System.Globalization.CultureInfo.InvariantCulture),
                                          //_previousBar.Close.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)
                                        });
        }
        // got tick is called whenever this strategy receives a tick
        public override void GotTick(Tick tick)
        {
            // keep track of time from tick
            _time = tick.time;
            //if (_date != tick.date)
            //{
            //    // this is important for consistency between runs for ATSTestBench and ATSTestBenchBatch
            //    Reset();
            //}
            _date = tick.date;
            // ensure response is active
            if (!isValid) return;
            // ensure we are tracking active status for this symbol
            int idx = _active.addindex(tick.symbol, true);
            // if we're not active, quit
            if (!_active[idx]) return;
            // check for shutdown time
            if (tick.time > Shutdown)
            {
                // if so shutdown
                if (!_isShutDown)
                {
                    shutdown();
                }
                // and quit               
                return;
            }
            else
            {
                _isShutDown = false;
            }
            // apply bar tracking to all ticks that enter
            _kt.newTick(tick);
            _blt.newTick(tick);

            // ignore anything that is not a trade
            if (!tick.isTrade) return;
        }   
        public override void GotFill(Trade fill)
        {
            // make sure every fill is tracked against a position
            int sizebefore = _pt[fill.symbol].Size;
            _pt.Adjust(fill);
            bool isclosing = (sizebefore) * fill.xsize < 0;
            if (isclosing)
            {
                decimal pl = Calc.Sum(Calc.AbsoluteReturn(_pt));
                TotalProfit = pl;
            }
            // get index for this symbol
            int idx = _wait.getindex(fill.symbol);
            // ignore unknown symbols
            if (idx < 0) return;
            // stop waiting
            _wait[fill.symbol] = false;            
        }
        void shutdown()
        {
            D("shutting down everything");
            foreach (Position p in _pt)
            {
                if (!_wait[p.symbol] && !_pt[p.symbol].isFlat)
                {
                    Tick tOther = _kt[p.symbol];
                    _side = !p.isLong;
                    _adj = (_side ? -1 : +1) * _quasiMarketOrderAdjSize;
                    Int64 _orderidLocal = _idtLocal.AssignId;
                    LimitOrder lOrder = new LimitOrder(p.symbol, _side, EntrySize, tOther.trade - _adj, _orderidLocal);
                    sendorder(lOrder);
                }
            }
            _isShutDown = true;
        }

        BarInterval _barType = BarInterval.CustomTime;//"Time","Volume"
        int _numTickPerBar = 86400;
        int _barsback = 9;        
        int _shutdowntime = 151200;
        String _genericParameter = String.Empty;
        Boolean _useMod_TD_combinationFromATSGlobalIndicator = true;
        int _entrysize = 1;
        decimal _totalprofit = 0;
        int _time = 0;
        int _date = 0;
        decimal _quasiMarketOrderAdjSize = 10m;
        bool _isShutDown = false;
        ATSGlobalIndicatorPersonal.Mod_TD_combination _Mod_TD_combinationFromATSGlobalIndicator = null;
        bool _black = false;
        Boolean _side = true;
        Decimal _adj = 1;
        Decimal BuySignal = 0;
        Decimal SellSignal = 0;
        Decimal Money_state = 0;
        Bar[] _barVec = new Bar[11];
        // wait for fill
        GenericTracker<bool> _wait = new GenericTracker<bool>();
        // track whether shutdown 
        GenericTracker<bool> _active = new GenericTracker<bool>();
        // turn on bar tracking
        BarListTracker _blt = new BarListTracker();
        // turn on position tracking
        PositionTracker _pt = new PositionTracker();
        TickTracker _kt = new TickTracker();
        IdTracker _idtLocal = new IdTracker(0);
    }

    /// <summary>
    /// this is the same as Mod_TD_combinationResponse, except it runs without prompting user
    /// </summary>
    public class Mod_TD_combinationResponseAutoPub : Mod_TD_combinationResponsePub
    {
        public Mod_TD_combinationResponseAutoPub() : base(false) { }
    }
}
