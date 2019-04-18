using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ATSGlobalIndicator;

namespace ATSGlobalIndicatorPersonal
{
    [Serializable]
    public class Vortex : GenericIndicatorTemplate
    {
        public Vortex()
        {
        }
        /// <summary>
        /// Initializatin
        /// </summary>
        public override void Initializatin()
        {
            Name = GetType().Name.ToLower();
            _lookBackPerid = 0;
            _previousHigh = 0;
            _previousLow = 0;
            _previousClose = 0;
            _isCalcStd = false;
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
        public void UpdateValue(Decimal high, Decimal low, Decimal close, Decimal _preHigh, Decimal _preLow, Decimal _preClose)
        {
            _currentHigh = high;
            _currentLow = low;
            _currentClose = close;
            _previousHigh = _preHigh;
            _previousLow = _preLow;
            _previousClose = _preClose;
            _dataLength++;
          
            if (_dataLength <= _minmumSignalLength)
            {
                _TR = 0;
                _vmUp = 0;
                _vmDown = 0;
            }
            else
            {
                //计算TR
                _TR = Math.Max(Math.Max(Math.Abs(_currentHigh - _currentLow), Math.Abs(_currentLow - _previousClose)),
                               Math.Abs(_currentHigh - _previousClose));

                //计算VM+和VM-
                _vmUp = Math.Abs(_currentHigh - _previousLow);
                _vmDown = Math.Abs(_currentLow - _previousHigh);
            }
            //计算SUM_TR21
            Int32 _TRelementCount = _TRvalueVec.Count;
            _TRvalueVec.Enqueue(_TR);
            _TRelementCount++;
            if (_TRelementCount > _lookBackPerid)
            {
                _TRvalueVec.Dequeue();
            }
            //计算SUM_VM+
            Int32 _vmUpelementCount = _vmUpvalueVec.Count;
            _vmUpvalueVec.Enqueue(_vmUp);
            _vmUpelementCount++;
            if (_vmUpelementCount > _lookBackPerid )
            {
                _vmUpvalueVec.Dequeue();
            }
            //计算SUM_VM-
            Int32 _vmDownelementCount = _vmDownvalueVec.Count;
            _vmDownvalueVec.Enqueue(_vmDown);
            _vmDownelementCount++;
            if (_vmDownelementCount > _lookBackPerid)
            {
                _vmDownvalueVec.Dequeue();
            }
            _SumTR = 0;
            foreach (Decimal vTmp1 in _TRvalueVec)
            {
                _SumTR += vTmp1;
            }
            _SumVmUp = 0;
            foreach (Decimal vTmp2 in _vmUpvalueVec)
            {
                _SumVmUp += vTmp2;
            }
            _SumVmDown = 0;
            foreach (Decimal vTmp3 in _vmDownvalueVec)
            {
                _SumVmDown += vTmp3;
            }

            //计算VI21+和VI21-
            if (_SumTR != 0)
            {
                _currentVIUp = _SumVmUp / _SumTR;
                _currentVIDown = _SumVmDown / _SumTR;
            }
            
        }
        /// <summary>
        /// Indicator value update, three values input, period input
        /// </summary>
        public void UpdateValue(Decimal high, Decimal low, Decimal close, Decimal _preHigh, Decimal _preLow, Decimal _preClose, int period)
        {
            if (period == Period)
            {
                UpdateValue(high, low, close, _preHigh, _preLow, _preClose);
            }
        }
        /// <summary>
        /// Signal value output
        /// </summary>
        public Decimal GetSignalUp()
        {
            if (_dataLength > _lookBackPerid)
            {
                return _currentVIUp;
            }
            else
            {
                return 0;
            }
        }
        public Decimal GetSignalDown()
        {
            if (_dataLength > _lookBackPerid)
            {
                return _currentVIDown;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Signal vector value output
        /// </summary>
        public override void GetSignalVec(ref Decimal[] signalVecValue)
        {
            if (_dataLength <= (Int64)(_minmumSignalLength))
            {
                for (Int32 i = 0; i < GetSignalVecLength(); i++)
                {
                    signalVecValue[i] = Decimal.MinValue;
                }
                return;
            }
            signalVecValue[0] = _currentVIUp;
            signalVecValue[1] = _currentVIDown;

        }

        //[XmlIgnore]
        //public Queue<Decimal> _valueVec;
        public Int32 _lookBackPerid;
        public Decimal _alpha;
        public Decimal _currentHigh;
        public Decimal _currentLow;
        public Decimal _currentClose;
        public Decimal _previousHigh;
        public Decimal _previousLow;
        public Decimal _previousClose;
        public Decimal _TR;
        public Decimal _vmUp;
        public Decimal _vmDown;
        public Decimal _SumTR;
        public Decimal _SumVmUp;
        public Decimal _SumVmDown;
        public Decimal _currentVIUp;
        public Decimal _currentVIDown;
        public Boolean _isCalcStd;
        public Decimal _varAdj;

        public Queue<decimal> _TRvalueVec { get; set; }

        public Queue<decimal> _vmUpvalueVec { get; set; }

        public Queue<decimal> _vmDownvalueVec { get; set; }

    }
}
