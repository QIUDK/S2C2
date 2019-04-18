using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ATSGlobalIndicator;
using TradeLink.API;
namespace ATSGlobalIndicatorPersonal
{
    [Serializable]
    public class TD_sequence : GenericIndicatorTemplate
    {
        public TD_sequence()
        {
        }
        /// <summary>
        /// Initializatin
        /// </summary>
        public override void Initializatin()
        {
            Name = GetType().Name.ToLower();
            _lookBackPerid = 0;
            _buyActivateCounter = 0;            //买入启动阶段计数
            _buySignalCounter = 0;              //买入计数
            _sellActivateCounter = 0;           //卖出启动阶段计数
            _sellSignalCounter = 0;             //卖出计数
            _Counter2 = 0;                      //用于启动次数的判断
            _TD_buySignal = 0;
            _TD_sellSignal = 0;
            _isCalcStd = false;
            _isBuyActivated = false;            //是否买入启动
            _isSellActivated = false;           //是否卖出启动
            _isSellCross = false;               //是否卖出交叉
            _isBuyCross = false;                //是否买入交叉
            _buyflag = false;
            _sellflag = false;
            if (_inputParam != null)
                GetParam();
            _dataLength = 0;
            _varAdj = _lookBackPerid / (_lookBackPerid - 1.0m);
            _signalVecLength = 2;
        }
        /// <summary>
        /// Getting parameter from input
        /// </summary>
        public override void GetParam()
        {
            if (_inputParam.ContainsKey("name"))
            {
                Name = _inputParam["name"].ToString();
            }
            if (_inputParam.ContainsKey("period"))
            {
                _period = Convert.ToInt32(_inputParam["period"].ToString());
            }
            if (_inputParam.ContainsKey("lookbackperiod"))
            {
                _lookBackPerid = Convert.ToInt32(_inputParam["lookbackperiod"].ToString());
            }
            _alpha = 1.0m / (_lookBackPerid);
            if (_inputParam.ContainsKey("alpha"))
            {
                _alpha = Convert.ToDecimal(_inputParam["alpha"].ToString());
                if (_alpha == -10000)
                {
                    _alpha = 1.0m / (_lookBackPerid);
                }
            }
            //if (_inputparam.containskey("lastsma"))
            //{
            //    _lastsmavalue = convert.todecimal(_inputparam["lastsma"].tostring());
            //}
            _minmumSignalLength = _lookBackPerid;
            if (_inputParam.ContainsKey("minmumsignallength"))
            {
                _minmumSignalLength = Convert.ToInt32(_inputParam["minmumsignallength"].ToString());
            }
            if (_inputParam.ContainsKey("iscalcstd"))
            {
                _isCalcStd = Convert.ToBoolean(_inputParam["iscalcstd"].ToString());
            }
        }
        /// <summary>
        /// Reset indicator
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Initializatin();
        }
        /// <summary>
        /// Indicator values update, three values input
        /// </summary>
        public void UpdateValue(Bar[] _barVec)     //_barVec储存了当前（_barVec[1])到第九个之前的K线(_barVec[10])
        {
            _dataLength++;
            //Bar _eighthBar = null;
            //if (_buyflag == false)
            //{
            //    if (_barVec[1].Close > _barVec[5].Close)
            //    {
            //        _buyflag = true;
            //    }
            //}
            //else
            //{
                //判断买入启动
            if ((_barVec[1].Close < _barVec[5].Close && _buyActivateCounter > 0) || 
                (_barVec[1].Close < _barVec[5].Close && _barVec[2].Close > _barVec[6].Close && _buyActivateCounter == 0))
                {
                    _buyActivateCounter++;
                    if (_buyActivateCounter == 9)
                    {
                        _isBuyActivated = true;
                        _buyActBar = _barVec;
                        _buyActivateCounter = 0;
                        _Counter2++;

                        //处理之前已经卖出启动的情况
                        if (_isSellActivated == true)
                        {
                            _isSellActivated = false;
                            _sellActivateCounter = 0;
                            _sellSignalCounter = 0;
                            _isSellCross = false;
                            _sellflag = false;
                        }
                    }
                    if (_buyActivateCounter == 8)
                    {
                        _eighthBar = _barVec[1];
                    }
                }
                else
                {
                    _buyActivateCounter = 0;
                    _buyflag = false;
                }
            
            //if (_sellflag == false)
            //{
            //    if (_barVec[1].Close < _barVec[5].Close)
            //    {
            //        _sellflag = true;
            //    }
            //}
            //else
            //{
                //判断卖出启动
            if ((_barVec[1].Close > _barVec[5].Close && _sellActivateCounter > 0) ||
                (_barVec[1].Close > _barVec[5].Close && _barVec[2].Close < _barVec[6].Close && _sellActivateCounter == 0))
                {
                    _sellActivateCounter++;
                    if (_sellActivateCounter == 9)
                    {
                        _isSellActivated = true;
                        _sellActBar = _barVec;
                        _sellActivateCounter = 0;
                        _Counter2++;

                        //处理之前已经买入启动的情况
                        if (_isBuyActivated == true)
                        {
                            _isBuyActivated = false;
                            _buyActivateCounter = 0;
                            _buySignalCounter = 0;
                            _isBuyCross = false;
                            _buyflag = false;
                        }
                    }
                    if (_sellActivateCounter == 8)
                    {
                        _eighthBar = _barVec[1];
                    }
                }
                else
                {
                    _sellActivateCounter = 0;
                    _sellflag = false;
                }
            //}
            ////进入新的买入或卖出启动阶段，取消之前_buySignalCounter和_sellSignalCounter计数
            //if (_Counter2 > 1)
            //{
            //    _buySignalCounter = 0;
            //    _isBuyCross = false;
            //    _sellSignalCounter = 0;
            //    _isSellCross = false;
            //    _Counter2 = 1;
            //}
            //已买入启动
            if (_isBuyActivated)
            {
                //未买入交叉
                if (!_isBuyCross)
                {
                    _threeDayLow1 = Math.Min(_buyActBar[5].Low, Math.Min(_buyActBar[4].Low, _buyActBar[3].Low));
                    _threeDayLow2 = Math.Min(_barVec[4].Low, Math.Min(_barVec[3].Low, _barVec[2].Low));
                    if (_buyActBar[2] != null)
                    {
                        if (_buyActBar[2].Low <= _threeDayLow1 || _barVec[1].Low <= _threeDayLow2)
                        {
                            _isBuyCross = true;
                            if (_barVec[1].Close < _barVec[3].Close)
                            {
                                _buySignalCounter++;
                            }
                        }
                    }
                }
                //已买入交叉
                else
                {
                    if (_barVec[1].Close < _barVec[3].Close)
                    {
                        _buySignalCounter++;
                    }
                }
            }
            //已卖出启动
            if (_isSellActivated)
            {
                //未卖出交叉
                if (!_isSellCross)
                {
                    _threeDayHigh1 = Math.Max(_sellActBar[5].High, Math.Max(_sellActBar[4].High, _sellActBar[3].High));
                    _threeDayHigh2 = Math.Max(_barVec[4].High, Math.Max(_barVec[3].High, _barVec[2].High));
                    if (_sellActBar[2] != null)
                    {
                        if (_sellActBar[2].High >= _threeDayHigh1 || _barVec[1].High >= _threeDayHigh2)
                        {
                            _isSellCross = true;
                            if (_barVec[1].Close > _barVec[3].Close)
                            {
                                _sellSignalCounter++;
                            }
                        }
                    }
                }
                //已卖出交叉
                else
                {
                    if (_barVec[1].Close > _barVec[3].Close)
                    {
                        _sellSignalCounter++;
                    }
                }
            }
            //判断是否产生买入信号
            if (_buySignalCounter == 13)
            {
                _TD_buySignal = 1;
                _buySignalCounter = 0;
                _isBuyCross = false;
                _isBuyActivated = false;
                _sellSignalCounter = 0;
                _isSellCross = false;
                _isSellActivated = false;
                _eighthBar = null;
                _Counter2 = 0;
                _buyflag = false;
            }
            else
            {
                _TD_buySignal = 0;
            }
            //判断是否产生卖出信号
            if (_sellSignalCounter == 13)
            {
                _TD_sellSignal = 1;
                _sellSignalCounter = 0;
                _isSellCross = false;
                _isSellActivated = false;
                _buySignalCounter = 0;
                _isBuyCross = false;
                _isBuyActivated = false;
                _eighthBar = null;
                _Counter2 = 0;
                _sellflag = false;
            }
            else
            {
                _TD_sellSignal = 0;
            }
        }
        /// <summary>
        /// Indicator value update, three values input, period input
        /// </summary>
        //public void UpdateValue(Decimal high, Decimal low, Decimal close, Decimal _preHigh, Decimal _preLow, Decimal _preClose, int period)
        //{
        //    if (period == Period)
        //    {
        //        UpdateValue(high, low, close, _preHigh, _preLow, _preClose);
        //    }
        //}
        /// <summary>
        /// Signal value output
        /// </summary>
        public Decimal GetBuySignal()
        {
            if (_dataLength > _lookBackPerid)
            {
                return _TD_buySignal;
            }
            else
            {
                return 0;
            }
        }
        public Decimal GetSellSignal()
        {
            if (_dataLength > _lookBackPerid)
            {
                return _TD_sellSignal;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Signal vector value output
        /// </summary>
        //public override void GetSignalVec(ref Decimal[] signalVecValue)
        //{
        //    if (_dataLength <= (Int64)(_minmumSignalLength))
        //    {
        //        for (Int32 i = 0; i < GetSignalVecLength(); i++)
        //        {
        //            signalVecValue[i] = Decimal.MinValue;
        //        }
        //        return;
        //    }
        //    signalVecValue[0] = _currentVIUp;
        //    signalVecValue[1] = _currentVIDown;

        //}

        //[XmlIgnore]
        //public Queue<Decimal> _valueVec;
        public Int32 _lookBackPerid;
        public Decimal _alpha;
        
        public Boolean _isCalcStd;
        public Boolean _isBuyActivated;
        public Boolean _isSellCross;
        public Boolean _isSellActivated;
        public Boolean _isBuyCross;
        public Boolean _buyflag;
        public Boolean _sellflag;
        public Decimal _TD_buySignal;
        public Decimal _TD_sellSignal;
        public Decimal _varAdj;
        public Decimal _buyActivateCounter;
        public Decimal _buySignalCounter;
        public Decimal _sellActivateCounter;
        public Decimal _sellSignalCounter;
        public Decimal _Counter;
        public Decimal _Counter2;
        public Decimal _threeDayHigh1;
        public Decimal _threeDayHigh2;
        public Decimal _threeDayLow1;
        public Decimal _threeDayLow2;
        public Bar _eighthBar = null;
        public Bar[] _buyActBar;                 //用于储存买入启动阶段的K线
        public Bar[] _sellActBar;                //用于储存卖出启动阶段的K线
        public Queue<decimal> _TRvalueVec { get; set; }

        public Queue<decimal> _vmUpvalueVec { get; set; }

        public Queue<decimal> _vmDownvalueVec { get; set; }

    }
}
