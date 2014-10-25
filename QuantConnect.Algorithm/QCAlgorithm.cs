﻿/*
* QUANTCONNECT.COM - 
* QC.Algorithm - Base Class for Algorithm.
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using QuantConnect.Securities;
using QuantConnect.Models;

namespace QuantConnect 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// QC Algorithm Base Class - Handle the basic requirement of a trading algorithm, 
    /// allowing user to focus on event methods.
    /// </summary>
    public class QCAlgorithm : MarshalByRefObject, IAlgorithm 
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private DateTime _time = new DateTime();
        private DateTime _startDate;   //Default start and end dates.
        private DateTime _endDate;     //Default end to yesterday
        private RunMode _runMode = RunMode.Series;
        private bool _locked = false;
        private string _algorithmId = "";
        private bool _quit = false;
        private bool _processingOrder = false;
        private bool _liveMode = false;
        private List<string> _debugMessages = new List<string>();
        private List<string> _logMessages = new List<string>();
        private List<string> _errorMessages = new List<string>();
        private Dictionary<string, Chart> _charts = new Dictionary<string, Chart>();
        private Dictionary<string, string> _runtimeStatistics = new Dictionary<string, string>();
        public Console Console = new Console(null);

        //Error tracking to avoid message flooding:
        private string _previousDebugMessage = "";
        private bool _sentNoDataError = false;

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialise the Algorithm
        /// </summary>
        public QCAlgorithm()
        {
            //Initialise the Algorithm Helper Classes:
            //- Note - ideally these wouldn't be here, but because of the DLL we need to make the classes shared across 
            //  the Worker & Algorithm, limiting ability to do anything else.
            Securities = new SecurityManager();
            Transactions = new SecurityTransactionManager(Securities);
            Portfolio = new SecurityPortfolioManager(Securities, Transactions);

            //Initialise Data Manager 
            SubscriptionManager = new SubscriptionManager();

            //Initialise Algorithm RunMode to Series - Parallel Mode deprecated:
            _runMode = RunMode.Series;

            //Initialise to unlocked:
            _locked = false;

            //Initialise Start and End Dates:
            _startDate = new DateTime(1998, 01, 01);
            _endDate = DateTime.Now.AddDays(-1);
            _charts = new Dictionary<string, Chart>();

            //Init Console Override: Pass console messages through to IDE.
            Console = new Console(this);
        }


        /******************************************************** 
        * CLASS PUBLIC VARIABLES
        *********************************************************/
        /// <summary>
        /// Security Object Collection
        /// </summary>
        /// <remarks>AutoComplete: Securities</remarks>
        public SecurityManager Securities
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Portfolio Adaptor/Wrapper: Easy access to securities holding properties:
        /// </summary>
        /// <remarks>AutoComplete: Portfolio["symbol"]</remarks>
        public SecurityPortfolioManager Portfolio 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Transaction Manager - Process transaction fills and order management.
        /// </summary>
        /// <remarks>AutoComplete: Transactions</remarks>
        public SecurityTransactionManager Transactions 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Generic Data Manager - Required for compiling all data feeds in order,
        /// and passing them into algorithm event methods.
        /// </summary>
        /// <remarks>AutoComplete: SubscriptionManager</remarks>
        public SubscriptionManager SubscriptionManager 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Set a public name for the algorithm.
        /// </summary>
        /// <remarks>AutoComplete: Name</remarks>
        public string Name 
        {
            get;
            set;
        }

        /// <summary>
        /// Wait semaphore to signal the algoritm is currently processing a synchronous order.
        /// </summary>
        /// <remarks>AutoComplete: ProcessingOrder</remarks>
        public bool ProcessingOrder
        {
            get
            {
                return _processingOrder;
            }
            set 
            {
                _processingOrder = value;
            }
        }

        /// <summary>
        /// Get the current algorithm date/time.
        /// </summary>
        /// <remarks>AutoComplete: Time</remarks>
        public DateTime Time 
        {
            get 
            {
                return _time;
            }
        }

        /// <summary>
        /// Get requested algorithm start date set with SetStartDate()
        /// </summary>
        /// <remarks>AutoComplete: StartDate</remarks>
        public DateTime StartDate 
        {
            get 
            {
                return _startDate;
            }
        }

        /// <summary>
        /// Get requested algorithm end date set with SetEndDate()
        /// </summary>
        /// <remarks>AutoComplete: EndDate</remarks>
        public DateTime EndDate 
        {
            get 
            {
                return _endDate;
            }
        }

        /// <summary>
        /// Algorithm Id for this Backtest / Live Run
        /// </summary>
        /// <remarks>AutoComplete: AlgorithmId</remarks>
        public string AlgorithmId 
        {
            get 
            {
                return _algorithmId;
            }
        }

        /// <summary>
        /// Accessor for Filled Orders dictionary<int, Order>
        /// </summary>
        /// <remarks>AutoComplete: Orders</remarks>
        public ConcurrentDictionary<int, Order> Orders 
        {
            get 
            {
                return Transactions.Orders;
            }
        }

        /// <summary>
        /// [DEPRECATED] Server setup RunMode for the Algorithm: Automatic, Parallel or Series.
        /// </summary>
        /// <remarks>AutoComplete: RunMode</remarks>
        public RunMode RunMode 
        {
            get 
            {
                return _runMode;
            }
        }

        /// <summary>
        /// bool Check if the algorithm is locked from any further init changes.
        /// </summary>
        /// <remarks>AutoComplete: Locked</remarks>
        public bool Locked 
        {
            get 
            {
                return _locked;
            }
        }

        /// <summary>
        /// Bool Algorithm is Live.
        /// </summary>
        /// <remarks>AutoComplete: LiveMode</remarks>
        public bool LiveMode
        {
            get
            {
                return _liveMode;
            }
        }

        /// <summary>
        /// List<string> Get the debug messages from inner list
        /// </summary>
        /// <remarks>AutoComplete: DebugMessages</remarks>
        public List<string> DebugMessages
        {
            get 
            {
                return _debugMessages;
            }
            set 
            {
                _debugMessages = value;
            }
        }

        /// <summary>
        /// List<string> Downloadable large scale messaging systems
        /// </summary>
        /// <remarks>AutoComplete: LogMessages</remarks>
        public List<string> LogMessages 
        {
            get 
            {
                return _logMessages;
            }
            set 
            {
                _logMessages = value;
            }
        }

        /// <summary>
        /// Catchable Error List.
        /// </summary>
        /// <remarks>AutoComplete: ErrorMessages</remarks>
        public List<string> ErrorMessages
        {
            get
            {
                return _errorMessages;
            }
            set
            {
                _errorMessages = value;
            }
        }


        /// <summary>
        /// Access to the runtime statistics property. User provided statistics. Dictionary<string, string>
        /// </summary>
        /// <remarks>AutoComplete: RuntimeStatistics</remarks>
        public Dictionary<string, string> RuntimeStatistics
        {
            get
            {
                return _runtimeStatistics;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Initialise the data and resolution required
        /// </summary>
        /// <remarks>AutoComplete: Initialize</remarks>
        public virtual void Initialize() 
        {
            //Setup Required Data
            throw new NotImplementedException("Please override the Intitialize() method");
        }

        /// <summary>
        /// Event - DEPRECATED - v1.0 TRADEBAR EVENT HANDLER. Handle new data packets.
        /// </summary>
        /// <param name="data">Dictionary of MarketData Objects</param>
        /// <remarks>AutoComplete: OnTradeBar</remarks>
        public virtual void OnTradeBar(Dictionary<string, TradeBar> data)
        {
            //Algorithm Implementation
            //throw new NotImplementedException("OnTradeBar has been made obsolete. Please use OnData(TradeBars data) instead.");
        }

        /// <summary>
        /// Event - DEPRECATED - v1.0 TICK EVENT HANDLER. Handle a new incoming Tick Packet:
        /// </summary>
        /// <param name="data">Ticks arriving at the same moment come in a list. Because the "tick" data is actually list ordered within a second, you can get lots of ticks at once.</param>
        /// <remarks>AutoComplete: OnTick</remarks>
        public virtual void OnTick(Dictionary<string, List<Tick>> data)
        {
            //Algorithm Implementation
            //throw new NotImplementedException("OnTick has been made obsolete. Please use OnData(Ticks data) instead.");
        }

        /// <summary>
        /// Event - v2.0 TRADEBAR EVENT HANDLER: (Pattern) Basic template for user to override when requesting tradebar data.
        /// </summary>
        /// <param name="data"></param>
        /// <remarks>AutoComplete: OnData(TradeBars data)</remarks>
        //public void OnData(TradeBars data)
        //{
        //
        //}

        /// <summary>
        /// Event - v2.0 TICK EVENT HANDLER: (Pattern) Basic template for user to override when requesting tick data.
        /// </summary>
        /// <param name="data">List of Tick Data</param>
        /// <remarks>AutoComplete: OnData(Ticks data)</remarks>
        //public void OnData(Ticks data)
        //{
        //
        //}

        /// <summary>
        /// Event - Call this method at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>AutoComplete: OnEndOfDay()</remarks>
        public virtual void OnEndOfDay()
        {

        }

        /// <summary>
        /// Event - Call this method at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <param name="symbol">End of day for this symbol string</param>
        /// <remarks>AutoComplete: OnEndOfDay</remarks>
        public virtual void OnEndOfDay(string symbol) 
        {
            
        }

        /// <summary>
        /// Event - Call this at the end of the algorithm running.
        /// </summary>
        /// <remarks>AutoComplete: OnEndOfAlgorithm</remarks>
        public virtual void OnEndOfAlgorithm() 
        { 
            
        }

        /// <summary>
        /// Event - Order - Fill, update, cancel, etc. When an order is update the events is passed in here:
        /// </summary>
        /// <param name="orderEvent">Details of the order</param>
        /// <remarks>AutoComplete: OnOrderEvent</remarks>
        public virtual void OnOrderEvent(OrderEvent orderEvent)
        {
            
        }

        /// <summary>
        /// Add a Chart object to algorithm collection
        /// </summary>
        /// <param name="chart">Chart object to add to collection.</param>
        /// <remarks>AutoComplete: AddChart(Chart chart)</remarks>
        public void AddChart(Chart chart)
        {
            if (!_charts.ContainsKey(chart.Name))
            {
                _charts.Add(chart.Name, chart);
            }
        }

        /// <summary>
        /// Plot a chart using string series name, with value.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="value">Value to plot</param>
        /// <remarks>AutoComplete: Plot(string series, decimal value)</remarks>
        public void Plot(string series, decimal value)
        {
            //By default plot to the primary chart:
            this.Plot("Strategy Equity", series, value);
        }


        /// <summary>
        /// Plot a chart using string series name, with int value. Alias of Plot();
        /// </summary>
        /// <remarks>AutoComplete: Record(string series, int value)</remarks>
        public void Record(string series, int value)
        {
            this.Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value. Alias of Plot();
        /// </summary>
        /// <remarks>AutoComplete: Record(string series, double value)</remarks>
        public void Record(string series, double value)
        {
            this.Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with decimal value. Alias of Plot();
        /// </summary>
        /// <remarks>AutoComplete: Record(string series, decimal value)</remarks>
        public void Record(string series, decimal value)
        {
            //By default plot to the primary chart:
            this.Plot(series, value);
        }

        /// <summary>
        /// Plot a chart using string series name, with double value.
        /// </summary>
        /// <remarks>AutoComplete: Plot(string series, double value)</remarks>
        public void Plot(string series, double value) {
            this.Plot(series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart using string series name, with int value.
        /// </summary>
        /// <remarks>AutoComplete: Plot(string series, int value)</remarks>
        public void Plot(string series, int value)
        {
            this.Plot(series, (decimal)value);
        }

        /// <summary>
        ///Plot a chart using string series name, with float value.
        /// </summary>
        /// <remarks>AutoComplete: Plot(string series, float value)</remarks>
        public void Plot(string series, float value)
        {
            this.Plot(series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with double value.
        /// </summary>
        /// <remarks>AutoComplete: Plot(string series, double value)</remarks>
        public void Plot(string chart, string series, double value)
        {
            this.Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with int value
        /// </summary>
        /// <remarks>AutoComplete: Plot(string chart, string series, int value)</remarks>
        public void Plot(string chart, string series, int value)
        {
            this.Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a chart to string chart name, using string series name, with float value
        /// </summary>
        /// <remarks>AutoComplete: Plot(string chart, string series, float value)</remarks>
        public void Plot(string chart, string series, float value)
        {
            this.Plot(chart, series, (decimal)value);
        }

        /// <summary>
        /// Plot a value to a chart of string-chart name, with string series name, and decimal value. If chart does not exist, create it.
        /// </summary>
        /// <param name="chart">Chart name</param>
        /// <param name="series">Series name</param>
        /// <param name="value">Value of the point</param>
        /// <remarks>AutoComplete: Plot(string chart, string series, decimal value)</remarks>
        public void Plot(string chart, string series, decimal value) 
        {
            //Ignore the reserved chart names:
            if ((chart == "Strategy Equity" && series == "Equity") || (chart == "Daily Performance"))
            {
                throw new Exception("Algorithm.Plot(): 'Equity' and 'Performance' are reserved chart names create for all backtests.");
            }

            // If we don't have the chart, create it:
            if (!_charts.ContainsKey(chart))
            {
                _charts.Add(chart, new Chart(chart)); 
            }

            if (!_charts[chart].Series.ContainsKey(series)) 
            {
                //Number of series in total.
                int seriesCount = (from x in _charts.Values select x.Series.Count).Sum();

                if (seriesCount > 10)
                {
                    Error("Exceeded maximum series count: Each backtest can have up to 10 series in total.");
                    return;
                }

                //If we don't have the series, create it:
                _charts[chart].AddSeries(new Series(series));
            }

            if (_charts[chart].Series[series].Values.Count < 4000 || _liveMode)
            {
                _charts[chart].Series[series].AddPoint(Time, value, _liveMode);
            }
            else 
            {
                Debug("Exceeded maximum points per chart, data skipped.");
            }
        }

        /// <summary>
        /// QC.Engine Use Only: Set the current datetime frontier: the most forward looking tick so far. This is used by backend to advance time. Do not modify
        /// </summary>
        /// <param name="frontier">Current datetime.</param>
        /// <remarks>AutoComplete: SetDateTime(DateTime start)</remarks>
        public void SetDateTime(DateTime frontier) 
        {
            this._time = frontier;
        }

        /// <summary>
        /// Set the RunMode for the Servers. If you are running an overnight algorithm, you must select series.
        /// Automatic will analyse the selected data, and if you selected only minute data we'll select series for you.
        /// </summary>
        /// <param name="mode">Enum RunMode with options Series, Parallel or Automatic. Automatic scans your requested symbols and resolutions and makes a decision on the fastest analysis</param>
        /// <remarks>AutoComplete: SetRunMode(RunMode mode)</remarks>
        public void SetRunMode(RunMode mode) 
        {
            if (mode == QuantConnect.RunMode.Parallel)
            {
                Debug("Algorithm.SetRunMode(): RunMode-Parallel Type has been deprecated. Series analysis selected instead");
                mode = QuantConnect.RunMode.Series;
            }
            return;
        }


        /// <summary>
        /// Set Initial Cash for the Strategy. Alias of SetCash(decimal)
        /// </summary>
        /// <param name="startingCash">Double starting cash</param>
        /// <remarks>AutoComplete: SetCash(double startingCash)</remarks>
        public void SetCash(double startingCash) {
            this.SetCash((decimal)startingCash);
        }

        /// <summary>
        /// Alias of SetCash(decimal)
        /// </summary>
        /// <param name="startingCash">Int starting cash</param>
        /// <remarks>AutoComplete: SetCash(int startingCash)</remarks>
        public void SetCash(int startingCash)
        {
            this.SetCash((decimal)startingCash);
        }

        /// <summary>
        /// Set the requested balance to launch this algorithm
        /// </summary>
        /// <param name="startingCash">Minimum required cash</param>
        /// <remarks>AutoComplete: SetCash(decimal startingCash)</remarks>
        public void SetCash(decimal startingCash) 
        {
            if (!Locked) 
            {
                Portfolio.SetCash(startingCash);
            }
            else 
            {
                throw new Exception("Algorithm.SetCash(): Cannot change cash available after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set a runtime statistic for the algorithm,
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">String value of your runtime statistic</param>
        /// <remarks>AutoComplete: SetRuntimeStatistic(string name, string value)</remarks>
        public void SetRuntimeStatistic(string name, string value)
        {
            //If not set, add it to the dictionary:
            if (!_runtimeStatistics.ContainsKey(name))
            {
                _runtimeStatistics.Add(name, value);
            }

            //Set 
            _runtimeStatistics[name] = value;
        }

        /// <summary>
        /// Helper wrapper for SetRuntimeStatistic to convert decimals to strings.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Decimal value of your runtime statistic</param>
        /// <remarks>AutoComplete: SetRuntimeStatistic(string name, decimal value)</remarks>
        public void SetRuntimeStatistic(string name, decimal value)
        {
            SetRuntimeStatistic(name, value.ToString());
        }

        /// <summary>
        /// Helper wrapper for SetRuntimeStatistic to convert ints to strings.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Int value of your runtime statistic</param>
        /// <remarks>AutoComplete: SetRuntimeStatistic(string name, int value)</remarks>
        public void SetRuntimeStatistic(string name, int value)
        {
            SetRuntimeStatistic(name, value.ToString());
        }

        /// <summary>
        /// Helper wrapper for SetRuntimeStatistic to convert ints to strings.
        /// </summary>
        /// <param name="name">Name of your runtime statistic</param>
        /// <param name="value">Double value of your runtime statistic</param>
        /// <remarks>AutoComplete: SetRuntimeStatistic(string name, double value)</remarks>
        public void SetRuntimeStatistic(string name, double value)
        {
            SetRuntimeStatistic(name, value.ToString());
        }

        /// <summary>
        /// Wrapper for SetStartDate(DateTime). Set the start date for backtest.
        /// Must be less than end date.
        /// </summary>
        /// <param name="day">Int starting date 1-30</param>
        /// <param name="month">Int month starting date</param>
        /// <param name="year">Int year starting date</param>
        /// <remarks>AutoComplete: SetStartDate(int year, int month, int day)</remarks>
        public void SetStartDate(int year, int month, int day) 
        {
            try 
            {
                this.SetStartDate(new DateTime(year, month, day));
            } 
            catch (Exception err) 
            {
                throw new Exception("Date Invalid: " + err.Message);
            }
        }

        /// <summary>
        /// Wrapper for SetEndDate(datetime). Set the end backtest date. 
        /// </summary>
        /// <param name="day">Int end date 1-30</param>
        /// <param name="month">Int month end date</param>
        /// <param name="year">Int year end date</param>
        /// <remarks>AutoComplete: SetEndDate(int year, int month, int day)</remarks>
        public void SetEndDate(int year, int month, int day) 
        {
            try 
            {
                this.SetEndDate(new DateTime(year, month, day));
            } 
            catch (Exception err) 
            {
                throw new Exception("Date Invalid: " + err.Message);
            }
        }

        /// <summary>
        /// QC.Engine Use Only: Set the algorithm id (backtestId or deployId).
        /// </summary>
        /// <param name="algorithmId">String Algorithm Id</param>
        /// <remarks>AutoComplete: SetAlgorithmId(string algorithmId)</remarks>
        public void SetAlgorithmId(string algorithmId)
        {
            _algorithmId = algorithmId;
        }

        /// <summary>
        /// Set the start date for the backtest 
        /// Must be less than end date and within data available
        /// </summary>
        /// <param name="start">Datetime start date</param>
        /// <remarks>AutoComplete: SetStartDate(DateTime start)</remarks>
        public void SetStartDate(DateTime start) 
        { 
            //Validate the start date:
            //1. Check range;
            //if (start < (new DateTime(1998, 01, 01))) 
            //{
            //    throw new Exception("Please select data between January 1st, 1998 to July 31st, 2012.");
            //}

            //2. Check end date greater:
            if (_endDate != new DateTime()) 
            {
                if (start > _endDate) 
                {
                    throw new Exception("Please select start date less than end date.");
                }
            }

            //3. Check not locked already:
            if (!Locked) 
            {
                this._startDate = start;
            } 
            else
            {
                throw new Exception("Algorithm.SetStartDate(): Cannot change start date after algorithm initialized.");
            }
        }

        /// <summary>
        /// Set the end date for a backtest. Must be greater than the start date
        /// </summary>
        /// <param name="end">End datetime</param>
        /// <remarks>AutoComplete: SetEndDate(DateTime end)</remarks>
        public void SetEndDate(DateTime end) 
        { 
            //Validate:
            //1. Check Range:
            if (end > DateTime.Now.Date.AddDays(-1)) 
            {
                end = DateTime.Now.Date.AddDays(-1);
            }

            //2. Check start date less:
            if (_startDate != new DateTime()) 
            {
                if (end < _startDate) 
                {
                    throw new Exception("Please select end date greater than start date.");
                }
            }

            //3. Check not locked already:
            if (!Locked) 
            {
                this._endDate = end;
            }
            else 
            {
                throw new Exception("Algorithm.SetEndDate(): Cannot change end date after algorithm initialized.");
            }
        }

        /// <summary>
        /// QC.Engine Use Only: Lock the algorithm initialization to avoid messing with cash and data streams.
        /// </summary>
        /// <remarks>AutoComplete: SetLocked()</remarks>
        public void SetLocked() 
        {
            this._locked = true;
        }

        /// <summary>
        /// QC.Engine Use Only: Set live mode state, are we running on a live servers.
        /// </summary>
        /// <param name="live">Bool Live mode flag</param>
        /// <remarks>AutoComplete: SetLiveMode(bool live)</remarks>
        public void SetLiveMode(bool live) 
        {
            if (!_locked)
            {
                _liveMode = live;
            }
        }

        /// <summary>
        /// QC.Engine Use Only: Get the chart updates: fetch the recent points added and return for dynamic plotting.
        /// </summary>
        /// <returns>List of chart updates since the last request</returns>
        /// <remarks>AutoComplete: GetChartUpdates()</remarks>
        public List<Chart> GetChartUpdates() 
        {
            List<Chart> _updates = new List<Chart>();
            foreach (Chart _chart in _charts.Values) {
                _updates.Add(_chart.GetUpdates());
            }
            return _updates;
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine. This is a backwards compatibility wrapper function.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future or FOREX</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="extendedMarketHours">Show the after market data as well</param>
        /// <remarks>AutoComplete: AddSecurity(SecurityType securityType, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false)</remarks>
        public void AddSecurity(SecurityType securityType, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            AddSecurity(securityType, symbol, resolution, fillDataForward, 0, extendedMarketHours);
        }

        /// <summary>
        /// Add specified data to required list. QC will funnel this data to the handle data routine.
        /// </summary>
        /// <param name="securityType">MarketType Type: Equity, Commodity, Future or FOREX</param>
        /// <param name="symbol">Symbol Reference for the MarketType</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <param name="extendedMarketHours">Extended market hours</param>
        /// <remarks>AutoComplete: AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours)</remarks>
        public void AddSecurity(SecurityType securityType, string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours) 
        {
            try
            {
                if (!_locked) 
                {
                    symbol = symbol.ToUpper();
                    //If it hasn't been set, use some defaults based on the portfolio type:
                    if (leverage <= 0) 
                    {
                        switch (securityType) 
                        {
                            case SecurityType.Equity:
                                leverage = 2;   //Cash Ac. = 1, RegT Std = 2 or PDT = 4.
                                break;
                            case SecurityType.Forex:
                                leverage = 50;
                                break;
                        }
                    }

                    //Add the symbol to Data Manager -- generate unified data streams for algorithm events
                    SubscriptionManager.Add(securityType, symbol, resolution, fillDataForward, extendedMarketHours);
                    //Add the symbol to Securities Manager -- manage collection of portfolio entities for easy access.
                    Securities.Add(symbol, securityType, resolution, fillDataForward, leverage, extendedMarketHours, useQuantConnectData: true);
                }
                else 
                {
                    throw new Exception("Algorithm.AddSecurity(): Cannot add another security after algorithm running.");
                }
            }
            catch (Exception err) 
            {
                Error("Algorithm.AddSecurity(): " + err.Message);
            }
        }


        /// <summary>
        /// AddData<typeparam name="T"> a new user defined data source, requiring only the minimum config options:
        /// </summary>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <remarks>AutoComplete: AddData<T>(string symbol, Resolution resolution = Resolution.Second)</remarks>
        public void AddData<T>(string symbol, Resolution resolution = Resolution.Second) 
        {
            if (!_locked)
            {
                //Add this to the data-feed subscriptions
                SubscriptionManager.Add(typeof(T), SecurityType.Base, symbol, resolution, fillDataForward:false, extendedMarketHours:true);

                //Add this new generic data as a tradeable security: 
                // Defaults:extended market hours"      = true because we want events 24 hours, 
                //          fillforward                 = false because only want to trigger when there's new custom data.
                //          leverage                    = 1 because no leverage on nonmarket data?
                Securities.Add(symbol, SecurityType.Base, resolution, fillDataForward: false, leverage:1, extendedMarketHours:true, useQuantConnectData:false);
            }
        }


        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <remarks>AutoComplete: Buy(string symbol, int quantity)</remarks>
        public int Buy(string symbol, int quantity) {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <remarks>AutoComplete: Buy(string symbol, double quantity)</remarks>
        public int Buy(string symbol, double quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <remarks>AutoComplete: Buy(string symbol, decimal quantity)</remarks>
        public int Buy(string symbol, decimal quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <remarks>AutoComplete: Buy(string symbol, float quantity)</remarks>
        public int Buy(string symbol, float quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <remarks>AutoComplete: Sell(string symbol, int quantity)</remarks>
        public int Sell(string symbol, int quantity) 
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <remarks>AutoComplete: Sell(string symbol, double quantity)</remarks>
        public int Sell(string symbol, double quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <remarks>AutoComplete: Sell(string symbol, float quantity)</remarks>
        public int Sell(string symbol, float quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <remarks>AutoComplete: Sell(string symbol, decimal quantity)</remarks>
        public int Sell(string symbol, decimal quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks>AutoComplete: Order(string symbol, double quantity, OrderType type = OrderType.Market) </remarks>
        public int Order(string symbol, double quantity, OrderType type = OrderType.Market) 
        {
            return Order(symbol, (int)quantity, type);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks>AutoComplete: Order(string symbol, decimal quantity, OrderType type = OrderType.Market)</remarks>
        public int Order(string symbol, decimal quantity, OrderType type = OrderType.Market)
        {
            return Order(symbol, (int)quantity, type);
        }

        /// <summary>
        /// Submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="type">Buy/Sell Limit or Market Order Type.</param>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <remarks>AutoComplete: Order(string symbol, int quantity, OrderType type = OrderType.Market, bool asynchronous = false, string tag = "")</remarks>
        public int Order(string symbol, int quantity, OrderType type = OrderType.Market, bool asynchronous = false, string tag = "")
        {
            //Add an order to the transacion manager class:
            int orderId = -1;
            decimal price = 0;

            //Ordering 0 is useless.
            if (quantity == 0 || symbol == null || symbol == "") 
            {
                return -1;
            }

            //Internals use upper case symbols.
            symbol = symbol.ToUpper();

            //If we're not tracking this symbol: throw error:
            if (!Securities.ContainsKey(symbol) && !_sentNoDataError)
            {
                _sentNoDataError = true;
                Error("You haven't requested " + symbol + " data. Add this with AddSecurity() in the Initialize() Method.");
            }

            //Set a temporary price for validating order for market orders:
            price = Securities[symbol].Price;
            if (price == 0) 
            {
                Error("Asset price is $0. If using custom data make sure you've set the 'Value' property.");
                return -1;
            }

            //Check the exchange is open before sending a market order.
            if (type == OrderType.Market && !Securities[symbol].Exchange.ExchangeOpen)
            {
                return -3;
            }

            //We've already processed too many orders: max 100 per day or the memory usage explodes
            if (Orders.Count > (_endDate - _startDate).TotalDays * 100)
            {
                return -5;
            }

            //Add the order and create a new order Id.
            orderId = Transactions.AddOrder(new Order(symbol, quantity, type, Time, price, tag));

            //Wait for the order event to process:
            //Enqueue means send to order queue but don't wait for response:
            if (!asynchronous && type == OrderType.Market)
            {
                //Wait for the market order to fill.
                //This is processed in a parallel thread.
                while (!Transactions.Orders.ContainsKey(orderId) || 
                       (Transactions.Orders[orderId].Status != OrderStatus.Filled && 
                        Transactions.Orders[orderId].Status != OrderStatus.Invalid &&
                        Transactions.Orders[orderId].Status != OrderStatus.Canceled) || _processingOrder)
                {
                    Thread.Sleep(1);
                }
            }

            return orderId;
        }

        /// <summary>
        /// Liquidate all holdings. Called at the end of day for tick-strategies.
        /// </summary>
        /// <param name="symbolToLiquidate">Symbols we wish to liquidate</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <remarks>Liquidate(string symbolToLiquidate = "")</remarks>
        public List<int> Liquidate(string symbolToLiquidate = "")
        {
            int quantity = 0;
            List<int> orderIdList = new List<int>();

            symbolToLiquidate = symbolToLiquidate.ToUpper();

            foreach (string symbol in Securities.Keys) 
            {
                //Send market order to liquidate if 1, we have stock, 2, symbol matches.
                if (Portfolio[symbol].HoldStock && (symbol == symbolToLiquidate || symbolToLiquidate == "")) 
                {
                    
                    if (Portfolio[symbol].IsLong)
                    {
                        quantity = -Portfolio[symbol].Quantity;
                    }
                    else
                    {
                        quantity = Math.Abs(Portfolio[symbol].Quantity);
                    }
                    //Liquidate at market price.
                    orderIdList.Add(Order(symbol, quantity, OrderType.Market));
                    //orderIdList.Add(Transactions.AddOrder(new Order(symbol, quantity, OrderType.Market, Time, Securities[symbol].Price)));
                }
            }
            return orderIdList;
        }


        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">double percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">liquidate existing holdings if neccessary to hold this stock</param>
        /// <remarks>SetHoldings(string symbol, double percentage, bool liquidateExistingHoldings = false)</remarks>
        public void SetHoldings(string symbol, double percentage, bool liquidateExistingHoldings = false)
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings);
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if neccessary to hold this stock</param>
        /// <remarks>SetHoldings(string symbol, float percentage, bool liquidateExistingHoldings = false)</remarks>
        public void SetHoldings(string symbol, float percentage, bool liquidateExistingHoldings = false)
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings);
        }


        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if neccessary to hold this stock</param>
        /// <remarks>SetHoldings(string symbol, int percentage, bool liquidateExistingHoldings = false)</remarks>
        public void SetHoldings(string symbol, int percentage, bool liquidateExistingHoldings = false)
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings);
        }

        /// <summary>
        /// Automatically place an order which will set the holdings to between 100% or -100% of *Buying Power*.
        /// E.g. SetHoldings("AAPL", 0.1); SetHoldings("IBM", -0.2); -> Sets portfolio as long 10% APPL and short 20% IBM
        /// </summary>
        /// <param name="symbol">   string Symbol indexer</param>
        /// <param name="percentage">decimal fraction of portfolio to set stock</param>
        /// <param name="liquidateExistingHoldings">bool flag to clean all existing holdings before setting new faction.</param>
        /// <remarks>SetHoldings(string symbol, decimal percentage, bool liquidateExistingHoldings = false)</remarks>
        public void SetHoldings(string symbol, decimal percentage, bool liquidateExistingHoldings = false)
        {
            //Error checks:
            if (!Portfolio.ContainsKey(symbol)) 
            {
                Debug(symbol.ToUpper() + " not found in portfolio. Request this data when initializing the algorithm.");
                return;
            }
            
            //Range check values:
            if (percentage > 1) percentage = 1;
            if (percentage < -1) percentage = -1;

            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                foreach (string holdingSymbol in Portfolio.Keys)
                {
                    if (holdingSymbol != symbol && Portfolio[holdingSymbol].AbsoluteQuantity > 0)
                    {
                        //Go through all existing holdings [synchronously], market order the inverse quantity:
                        Order(holdingSymbol, -Portfolio[holdingSymbol].Quantity);
                    }
                }
            }

            //1. To set a fraction of whole, we need to know the whole: Cash * Leverage for remaining buying power:
            decimal total = Portfolio.TotalHoldingsValue + Portfolio.Cash * Securities[symbol].Leverage;

            //2. Difference between our target % and our current holdings: (relative +- number).
            decimal deltaValue = (total * percentage) - Portfolio[symbol].HoldingsValue;

            decimal deltaQuantity = 0;
            
            //Potential divide by zero error for zero prices assets.
            if (Math.Abs(Securities[symbol].Price) > 0)
            {
                //3. Now rebalance the symbol requested:
                deltaQuantity = Math.Round(deltaValue / Securities[symbol].Price);
            }

            //Determine if we need to place an order:
            if (Math.Abs(deltaQuantity) > 0)
            {
                Order(symbol, (int)deltaQuantity);
            }
            return;
        }

        /// <summary>
        /// Send a debug message to the console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <remarks>Debug(string message)</remarks>
        public void Debug(string message)
        {
            if (!_liveMode && (message == "" || _previousDebugMessage == message)) return;
            _debugMessages.Add(message);
            _previousDebugMessage = message;
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">String message to log.</param>
        /// <remarks>Log(string message)</remarks>
        public void Log(string message) 
        {
            if (message == "") return;
            _logMessages.Add(message);
        }

        /// <summary>
        /// Send Error Message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <remarks>Error(string message)</remarks>
        public void Error(string message)
        {
            if (message == "") return;
            _errorMessages.Add(message);
            Debug("BacktestId:(" + _algorithmId + ") Error: " + message);
        }

        /// <summary>
        /// Terminate the algorithm on exiting the current event processor.
        /// </summary>
        /// <param name="message">Exit message</param>
        /// <remarks>Quit(string message)</remarks>
        public void Quit(string message = "") 
        {
            Debug("Quit(): " + message);
            _quit = true;
        }

        /// <summary>
        /// QC.Engine Use Only: Set the Quit Flag
        /// </summary>
        /// <param name="quit">Boolean quit state</param>
        /// <remarks>SetQuit(bool quitState)</remarks>
        public void SetQuit(bool quit) 
        {
            _quit = quit;
        }

        /// <summary>
        /// QC.Engine Use Only: Get the quit flag state.
        /// </summary>
        /// <remarks>AutoComplete: GetQuit()</remarks>
        /// <returns>Boolean true if set to quit event loop.</returns>
        public bool GetQuit() 
        {
            return _quit;
        }

    } // End Algorithm Template


    /// <summary>
    /// Helper class to override default behaviour of Console.WriteLine(); This will force the write line messages to appear in the browser console.
    /// </summary>
    public class Console
    {
        QCAlgorithm algorithmNamespace;

        /// <summary>
        /// Initialiser for Console Override
        /// </summary>
        /// <param name="algorithmNamespace">Algorithm Debug Function Access</param>
        public Console(QCAlgorithm algorithmNamespace)
        {
            this.algorithmNamespace = algorithmNamespace;
        }

        /// <summary>
        /// Write a line to the console in the browser
        /// </summary>
        /// <param name="consoleMessage">String message to send.</param>
        public void WriteLine(string consoleMessage)
        {
            algorithmNamespace.Debug(consoleMessage);
        }

        /// <summary>
        /// Write a line to the console in the browser
        /// </summary>
        /// <param name="consoleMessage">String message to send.</param>
        public void Write(string consoleMessage)
        {
            algorithmNamespace.Debug(consoleMessage);
        }
    }

} // End QC Namespace
