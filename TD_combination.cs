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
    public class TD_combination : GenericIndicatorTemplate
    {
        public TD_combination()
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
            _buySignalCounter = 1;
            _sellActivateCounter = 0;
            _sellSignalCounter = 1;
            _Counter = 0;
            _Counter2 = 0;
            _TD_buySignal = 0;
            _TD_sellSignal = 0;
            _isCalcStd = false;
            _isBuyActivated = false;
            _isSellActivated = false;
            _isSellCross = false;
            _isBuyCross = false;
            _buyFlag = false;               //用于对买入启动阶段的K线的单独处理
            _sellFlag = false;              //用于对卖出启动阶段的K线的单独处理
            if (_inputParam != null)
                GetParam();
            _TRvalueVec = new Queue<decimal>();
            _vmUpvalueVec = new Queue<decimal>();
            _vmDownvalueVec = new Queue<decimal>();
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
            //Bar _eighthBar = null;

            //判断买入启动
            if (_barVec[1].Close < _barVec[5].Close)
            {
                _buyActivateCounter++;
                _buyFlag = false;
                if (_buyActivateCounter == 9)
                {
                    _isBuyActivated = true;
                    _buyActBar = _barVec;
                    _buyActivateCounter = 0;
                    _Counter2++;
                    if (_isSellActivated == true)
                    {
                        _isSellActivated = false;
                        _sellActivateCounter = 0;
                    }
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
                _sellFlag = false;
                if (_sellActivateCounter == 9)
                {
                    _isSellActivated = true;
                    _sellActBar = _barVec;
                    _sellActivateCounter = 0;
                    _Counter2++;
                    if (_isBuyActivated == true)
                    {
                        _isBuyActivated = false;
                        _buyActivateCounter = 0;
                    }
                }
            }
            else
            {
                _sellActivateCounter = 0;
            }
            //进入新的买入或卖出启动阶段，取消之前_buySignalCounter和_sellSignalCounter计数
            if (_Counter2 > 1)
            {
                _buyActivateCounter = 0;
                _sellActivateCounter = 0;
                _buySignalCounter = 1;
                _sellSignalCounter = 1;
                _Counter2 = 1;
            }
            //已买入启动
            if (_isBuyActivated)
            {

                if (_buyFlag == false)
                {
                    for (int i = 8; i >=1 ; i--)
                    {
                        if (_buyActBar[i].Close <= _buyActBar[i + 2].Low && _buyActBar[i].Low < _buyActBar[i + 1].Low && _buyActBar[i].Close < _buyActBar[9].Close)
                        {
                            _buySignalCounter++;
                        }
                    }
                    _buyFlag = true;
                }
                else
                {
                    if (_barVec[1].Close <= _barVec[3].Low && _barVec[1].Low < _barVec[2].Low && _barVec[1].Close < _buyActBar[9].Close)
                    {
                        _buySignalCounter++;
                    }
                }
            }
            //已卖出启动
            if (_isSellActivated)
            {

                if (_sellFlag == false)
                {
                    for (int i = 8; i >= 1; i--)
                    {
                        if (_sellActBar[i].Close >= _sellActBar[i + 2].High && _sellActBar[i].High > _sellActBar[i + 1].High && _sellActBar[i].Close > _sellActBar[9].Close)
                        {
                            _sellSignalCounter++;
                        }
                    }
                    _sellFlag = true;
                }
                else
                {
                    if (_barVec[1].Close >= _barVec[3].High && _barVec[1].High > _barVec[2].High && _barVec[1].Close > _sellActBar[9].Close)
                    {
                        _sellSignalCounter++;
                    }
                }

            }

            if (_buySignalCounter == 13)
            {
                _TD_buySignal = 1;
                _buySignalCounter = 1;
                _isBuyActivated = false;
                _sellSignalCounter = 1;
                _isSellActivated = false;
                _Counter2 = 0;
                _buyFlag = false;
            }
            else
            {
                _TD_buySignal = 0;
            }
            if (_sellSignalCounter == 13)
            {
                _TD_sellSignal = 1;
                _sellSignalCounter = 1;
                _isSellActivated = false;
                _buySignalCounter = 1;
                _isBuyActivated = false;
                _Counter2 = 0;
                _sellFlag = false;
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
        public Boolean _buyFlag;
        public Boolean _sellFlag;
        public Decimal _TD_buySignal;
        public Decimal _TD_sellSignal;
        public Decimal _varAdj;
        public Decimal _buyActivateCounter;
        public Decimal _buySignalCounter;
        public Decimal _sellActivateCounter;
        public Decimal _sellSignalCounter;
        public Decimal _Counter;
        public Decimal _Counter2;
        public Decimal _threeDayLow;
        public Bar[] _buyActBar;                 //用于储存买入启动阶段的K线
        public Bar[] _sellActBar;                //用于储存卖出启动阶段的K线
        public Queue<decimal> _TRvalueVec { get; set; }

        public Queue<decimal> _vmUpvalueVec { get; set; }

        public Queue<decimal> _vmDownvalueVec { get; set; }

    }
}
