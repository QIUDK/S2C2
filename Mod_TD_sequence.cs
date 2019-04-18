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
    public class Mod_TD_sequence : GenericIndicatorTemplate
    {
        public Mod_TD_sequence()
        {
        }
        /// <summary>
        /// Initializatin
        /// </summary>
        public override void Initializatin()
        {
            Name = GetType().Name.ToLower();
            _lookBackPerid = 0;
            _buyActivateCounter = 0;
            _buySignalCounter1 = 0;
            _buySignalCounter2 = 0;
            _sellActivateCounter = 0;
            _sellSignalCounter1 = 0;
            _sellSignalCounter2 = 0;
            _Counter2 = 0;             
            _TD_buySignal = 0;
            _TD_sellSignal = 0;
            _isCalcStd = false;
            _isBuyActivated = false;
            _isSellActivated = false;
            _isSellCross = false;
            _isBuyCross = false;

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
        public void UpdateValue(Bar[] _barVec)
        {
            _dataLength++;
            //Bar _sixthBar = null;

            //判断买入启动
            if (_barVec[1].Close < _barVec[5].Close)
            {
                _buyActivateCounter++;
                if (_buyActivateCounter == _barPeriod)
                {
                    _isBuyActivated = true;
                    _buyActivateCounter = 0;
                    _Counter2++;
                    if (_isSellActivated == true)
                    {
                        _isSellActivated = false;
                    }
                }
                if (_buyActivateCounter == _barPeriod-1)
                {
                    _sixthBar = _barVec[1];
                }
            }
            else
            {
                _buyActivateCounter = 0;
            }
            //判断卖出启动
            if (_barVec[1].Close > _barVec[5].Close)
            {
                _sellActivateCounter++;
                if (_sellActivateCounter == _barPeriod)
                {
                    _isSellActivated = true;
                    _sellActivateCounter = 0;
                    _Counter2++;
                    if (_isBuyActivated == true)
                    {
                        _isBuyActivated = false;
                    }
                }
                if (_sellActivateCounter == _barPeriod-1)
                {
                    _sixthBar = _barVec[1];
                }
            }
            else
            {
                _sellActivateCounter = 0;
            }
            //进入新的买入或卖出启动阶段，取消之前_buySignalCounter1和_sellSignalCounter1计数
            if (_Counter2 > 1)
            {
                _buySignalCounter1 = 0;
                _buySignalCounter2 = 0;
                _isBuyCross = false;
                _sellSignalCounter1 = 0;
                _sellSignalCounter2 = 0;
                _isSellCross = false;
                _Counter2 = 1;
            }
            //已买入启动
            if (_isBuyActivated)
            {
                //未买入交叉
                if (!_isBuyCross)
                {
                    _threeDayLow = Math.Max(_barVec[4].High, Math.Max(_barVec[3].High, _barVec[2].High));
                    if (_sixthBar != null)
                    {
                        if (_sixthBar.High >= _threeDayLow || _barVec[1].High >= _threeDayLow)
                        {
                            _isBuyCross = true;
                            if (_barVec[1].Close < _barVec[3].Close)
                            {
                                _buySignalCounter1++;
                            }
                            if (_barVec[1].Close > _barVec[3].Close)
                            {
                                _buySignalCounter2++;
                            }
                        }
                    }
                }
                //已买入交叉
                else
                {
                    if (_barVec[1].Close < _barVec[3].Close)
                    {
                        _buySignalCounter1++;
                    }
                    if (_barVec[1].Close > _barVec[3].Close)
                    {
                        _buySignalCounter2++;
                    }
                }
            }
            //已卖出启动
            if (_isSellActivated)
            {
                //未卖出交叉
                if (!_isSellCross)
                {
                    _threeDayLow = Math.Min(_barVec[4].Low, Math.Min(_barVec[3].Low, _barVec[2].Low));
                    if (_sixthBar != null)
                    {
                        if (_sixthBar.Low <= _threeDayLow || _barVec[1].Low <= _threeDayLow)
                        {
                            _isSellCross = true;
                            if (_barVec[1].Close > _barVec[3].Close)
                            {
                                _sellSignalCounter1++;
                            }
                            if (_barVec[1].Close < _barVec[3].Close)
                            {
                                _sellSignalCounter2++;
                            }
                        }
                    }
                }
                //已卖出交叉
                else
                {
                    if (_barVec[1].Close > _barVec[3].Close)
                    {
                        _sellSignalCounter1++;
                    }
                    if (_barVec[1].Close < _barVec[3].Close)
                    {
                        _sellSignalCounter2++;
                    }
                }
            }

            if (_buySignalCounter1 == 12 || _buySignalCounter2 == 6)
            {
                _TD_buySignal = 1;
                _buySignalCounter1 = 0;
                _buySignalCounter2 = 0;
                _isBuyCross = false;
                _isBuyActivated = false;
                _sellSignalCounter1 = 0;
                _sellSignalCounter2 = 0;
                _isSellCross = false;
                _isSellActivated = false;
                _sixthBar = null;
                _Counter2 = 0;
            }
            else
            {
                _TD_buySignal = 0;
            }
            if (_sellSignalCounter1 == 12 || _sellSignalCounter2 == 6)
            {
                _TD_sellSignal = 1;
                _sellSignalCounter1 = 0;
                _sellSignalCounter2 = 0;
                _isSellCross = false;
                _isSellActivated = false;
                _buySignalCounter1 = 0;
                _buySignalCounter2 = 0;
                _isBuyCross = false;
                _isBuyActivated = false;
                _sixthBar = null;
                _Counter2 = 0;
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
        public Decimal _TD_buySignal;
        public Decimal _TD_sellSignal;
        public Decimal _varAdj;
        public Decimal _buyActivateCounter;
        public Decimal _buySignalCounter1;
        public Decimal _buySignalCounter2;
        public Decimal _sellActivateCounter;
        public Decimal _sellSignalCounter1;
        public Decimal _sellSignalCounter2;
        public Decimal _Counter2;
        public int _barPeriod = 6;
        public Decimal _threeDayLow;
        public Bar _sixthBar = null;
        public Queue<decimal> _TRvalueVec { get; set; }

        public Queue<decimal> _vmUpvalueVec { get; set; }

        public Queue<decimal> _vmDownvalueVec { get; set; }

    }
}
