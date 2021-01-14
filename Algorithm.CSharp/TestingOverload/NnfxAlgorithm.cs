using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm.CSharp.TestingOverload
{
    /// <summary>
    /// Algorithm demonstrating FOREX asset types and requesting history on them in bulk. As FOREX uses
    /// QuoteBars you should request slices or
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="forex" />
    public class NnfxAlgorithm : QCAlgorithm
    {
        private const Resolution EntryResolution = Resolution.Daily;
        private const Resolution ExitResolution = Resolution.Minute;

        private AverageTrueRange _atr;

        private ExponentialMovingAverage _fastMa;
        private ExponentialMovingAverage _slowMa;

        private RollingWindow<IndicatorDataPoint> _fastMaWindow;
        private RollingWindow<IndicatorDataPoint> _slowMaWindow;

        private Forex _eurusd;
        private Func<decimal, bool> _isStopLossTriggered;
        private Func<decimal, bool> _isTakeProfitTriggered;

        /// <summary>
        /// All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetTimeZone(DateTimeZone.Utc);

            SetStartDate(2015, 8, 8);
            SetEndDate(2020, 8, 8);
            SetCash(100000);

            // Our data source is setup with the higher resolution. It is used to check on our active trades, see 'OnData()'.
            // For the low res data we just consolidate the high res data and pipe that through 'OnDailyData'.
            _eurusd = AddForex("EURUSD", ExitResolution);
            Consolidate(_eurusd.Symbol, EntryResolution, OnDailyData);

            // Indicator setup with rolling windows where we are interested in past data
            _fastMa = new ExponentialMovingAverage(5);
            _fastMaWindow = new RollingWindow<IndicatorDataPoint>(2);
            _fastMa.Updated += (sender, updated) => _fastMaWindow.Add(updated);

            _slowMa = new ExponentialMovingAverage(30);
            _slowMaWindow = new RollingWindow<IndicatorDataPoint>(2);
            _slowMa.Updated += (sender, updated) => _slowMaWindow.Add(updated);

            _atr = new AverageTrueRange("EURUSD_daily", 14, MovingAverageType.Simple);

            // Our indicators rely on past data and therefore need some warmup before they can be used
            var warmUpPeriod = new List<IIndicator>() {_atr, _fastMa, _slowMa}
                .Where(i => i is IIndicatorWarmUpPeriodProvider)
                .Select(i => ((IIndicatorWarmUpPeriodProvider) i).WarmUpPeriod)
                .DefaultIfEmpty(0)
                .Max();

            SetWarmup(warmUpPeriod, EntryResolution);
        }

        private void LogInfo(object msg)
        {
            Log($"{Time.ToUniversalTime().ToString("O", DateTimeFormatInfo.CurrentInfo)}: {msg}");
        }

        /// <summary>
        /// The OnDailyData event handler contains the part of our algorithm that decides whether to enter a new trade. Each new data point on the larger timeframe will be pumped in here.
        /// </summary>
        private void OnDailyData(QuoteBar bar)
        {
            _atr.Update(bar);
            _fastMa.Update(Time, bar.Close);
            _slowMa.Update(Time, bar.Close);

            if (IsWarmingUp)
            {
                return;
            }

            if (Portfolio.CashBook.TotalValueInAccountCurrency < 5)
            {
                LogInfo("Cannot enter new trades. No money left.");
                return;
            }

            if (Portfolio.Invested)
            {
                // we want to avoid opening multiple trades for the same pair in parallel
                return;
            }

            // here is our first C1: an exponential moving average crossover
            if (_fastMaWindow[0] > _slowMaWindow[0] && _fastMaWindow[1] <= _slowMaWindow[1])
            {
                const int direction = 1;
                EnterNewTrade(direction);
            }
            else if (_fastMaWindow[0] < _slowMaWindow[0] && _fastMaWindow[1] >= _slowMaWindow[1])
            {
                const int direction = -1;
                EnterNewTrade(direction);
            }
        }

        /// <summary>
        /// With entering a new trade we also figure out where the stop loss and take profit go. 
        /// </summary>
        /// <param name="direction"> 1 for long and -1 for short trades</param>
        private void EnterNewTrade(int direction)
        {
            var stopLossDelta = 1.5m * _atr.Current.Value;
            var takeProfitDelta = 1m * _atr.Current.Value;

            var stopLoss = _eurusd.Price - direction * stopLossDelta;
            var takeProfit = _eurusd.Price + direction * takeProfitDelta;

            _isStopLossTriggered = price => direction * price < direction * stopLoss;
            _isTakeProfitTriggered = price => direction * price > direction * takeProfit;

            LogInfo($"entering {direction}@{_eurusd.Price}. set sl@{stopLoss}, tp@{takeProfit}");
            SetHoldings(_eurusd.Symbol,
                direction * 0.0005); // for now we are only interested in win rate, so the amount we trade does not matter.
        }

        /// <summary>
        /// OnData event is the primary entry point for our algorithm. Each new data point on the smaller timeframe will be pumped in here.
        /// For now we will just use this as a trigger to check on our active trades.
        /// </summary>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                return;
            }

            if (_isStopLossTriggered(_eurusd.Price))
            {
                LogInfo($"sl hit@{_eurusd.Price}");
                SetHoldings(_eurusd.Symbol, 0);
            }
            else if (_isTakeProfitTriggered(_eurusd.Price))
            {
                LogInfo($"tp hit@{_eurusd.Price}");
                SetHoldings(_eurusd.Symbol, 0);
            }
        }
    }
}
