/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NodaTime;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Gets or sets the history provider for the algorithm
        /// </summary>
        public IHistoryProvider HistoryProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether or not this algorithm is still warming up
        /// </summary>
        public bool IsWarmingUp
        {
            get;
            private set;
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        public void SetWarmup(TimeSpan timeSpan)
        {
            SetWarmUp(timeSpan, null);
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        public void SetWarmUp(TimeSpan timeSpan)
        {
            SetWarmup(timeSpan);
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        /// <param name="resolution">The resolution to request</param>
        public void SetWarmup(TimeSpan timeSpan, Resolution? resolution)
        {
            if (_locked)
            {
                throw new InvalidOperationException("QCAlgorithm.SetWarmup(): This method cannot be used after algorithm initialized");
            }

            _warmupBarCount = null;
            _warmupTimeSpan = timeSpan;
            _warmupResolution = resolution;
        }

        /// <summary>
        /// Sets the warm up period to the specified value
        /// </summary>
        /// <param name="timeSpan">The amount of time to warm up, this does not take into account market hours/weekends</param>
        /// <param name="resolution">The resolution to request</param>
        public void SetWarmUp(TimeSpan timeSpan, Resolution? resolution)
        {
            SetWarmup(timeSpan, resolution);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        public void SetWarmup(int barCount)
        {
            SetWarmUp(barCount, null);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm. The highest (smallest) resolution in the securities collection will be used.
        /// For example, if an algorithm has minute and daily data and 200 bars are requested, that would
        /// use 200 minute bars.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        public void SetWarmUp(int barCount)
        {
            SetWarmup(barCount);
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        /// <param name="resolution">The resolution to request</param>
        public void SetWarmup(int barCount, Resolution? resolution)
        {
            if (_locked)
            {
                throw new InvalidOperationException("QCAlgorithm.SetWarmup(): This method cannot be used after algorithm initialized");
            }

            _warmupTimeSpan = null;
            _warmupBarCount = barCount;
            _warmupResolution = resolution;
        }

        /// <summary>
        /// Sets the warm up period by resolving a start date that would send that amount of data into
        /// the algorithm.
        /// </summary>
        /// <param name="barCount">The number of data points requested for warm up</param>
        /// <param name="resolution">The resolution to request</param>
        public void SetWarmUp(int barCount, Resolution? resolution)
        {
            SetWarmup(barCount, resolution);
        }

        /// <summary>
        /// Sets <see cref="IAlgorithm.IsWarmingUp"/> to false to indicate this algorithm has finished its warm up
        /// </summary>
        public void SetFinishedWarmingUp()
        {
            IsWarmingUp = false;

            // notify the algorithm
            OnWarmupFinished();
        }

        /// <summary>
        /// Message for exception that is thrown when the implicit conversion between symbol and string fails
        /// </summary>
        private readonly string _symbolEmptyErrorMessage = "Cannot create history for the given ticker. " +
                                                           "Either explicitly use a symbol object to make the history request " +
                                                           "or ensure the symbol has been added using the AddSecurity() method before making the history request.";

        /// <summary>
        /// Gets the history requests required for provide warm up data for the algorithm
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HistoryRequest> GetWarmupHistoryRequests()
        {
            if (_warmupBarCount.HasValue)
            {
                return CreateBarCountHistoryRequests(Securities.Keys, _warmupBarCount.Value, _warmupResolution);
            }
            if (_warmupTimeSpan.HasValue)
            {
                var end = UtcTime.ConvertFromUtc(TimeZone);
                return CreateDateRangeHistoryRequests(Securities.Keys, end - _warmupTimeSpan.Value, end, _warmupResolution);
            }

            // if not warmup requested return nothing
            return Enumerable.Empty<HistoryRequest>();
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to request data. This is a calendar span, so take into consideration weekends and such</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        public IEnumerable<Slice> History(TimeSpan span, Resolution? resolution = null)
        {
            return History(Securities.Keys, Time - span, Time, resolution).Memoize();
        }

        /// <summary>
        /// Get the history for all configured securities over the requested span.
        /// This will use the resolution and other subscription settings for each security.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing data over the most recent span for all configured securities</returns>
        public IEnumerable<Slice> History(int periods, Resolution? resolution = null)
        {
            return History(Securities.Keys, periods, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for all symbols of the requested type over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(TimeSpan span, Resolution? resolution = null)
            where T : IBaseData
        {
            return History<T>(Securities.Keys, span, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null)
            where T : IBaseData
        {
            return History<T>(symbols, Time - span, Time, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null)
            where T : IBaseData
        {
            var requests = symbols.Select(x =>
            {
                var config = GetMatchingSubscription(x, typeof(T));
                if (config == null) return null;

                var exchange = GetExchangeHours(x);
                var res = GetResolution(x, resolution);
                var start = _historyRequestFactory.GetStartTimeAlgoTz(x, periods, res, exchange, config.DataTimeZone);
                return _historyRequestFactory.CreateHistoryRequest(config, start, Time, exchange, res);
            });

            return History(requests.Where(x => x != null)).Get<T>().Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbols</typeparam>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<DataDictionary<T>> History<T>(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null)
            where T : IBaseData
        {
            var requests = symbols.Select(x =>
            {
                var config = GetMatchingSubscription(x, typeof(T));
                if (config == null) return null;

                return _historyRequestFactory.CreateHistoryRequest(config, start, end, GetExchangeHours(x), resolution);
            });

            return History(requests.Where(x => x != null)).Get<T>().Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, TimeSpan span, Resolution? resolution = null)
            where T : IBaseData
        {
            return History<T>(symbol, Time - span, Time, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, int periods, Resolution? resolution = null)
        {
            if (symbol == null) throw new ArgumentException(_symbolEmptyErrorMessage);

            resolution = GetResolution(symbol, resolution);
            var marketHours = GetMarketHours(symbol);
            var start = _historyRequestFactory.GetStartTimeAlgoTz(symbol, periods, resolution.Value, marketHours.ExchangeHours, marketHours.DataTimeZone);

            return History(symbol, start, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <typeparam name="T">The data type of the symbol</typeparam>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, int periods, Resolution? resolution = null)
            where T : IBaseData
        {
            if (resolution == Resolution.Tick) throw new ArgumentException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");

            var config = GetHistoryRequestConfig(symbol, typeof(T), resolution);
            resolution = GetResolution(symbol, resolution);
            var start = _historyRequestFactory.GetStartTimeAlgoTz(symbol, periods, resolution.Value, GetExchangeHours(symbol), config.DataTimeZone);
            return History<T>(symbol, start, Time, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol between the specified dates. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<T> History<T>(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null)
            where T : IBaseData
        {
            var config = GetHistoryRequestConfig(symbol, typeof(T), resolution);

            var request = _historyRequestFactory.CreateHistoryRequest(config, start, end, GetExchangeHours(symbol), resolution);
            return History(request).Get<T>(symbol).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, TimeSpan span, Resolution? resolution = null)
        {
            return History(symbol, Time - span, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbol over the request span. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<TradeBar> History(Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null)
        {
            var securityType = symbol.ID.SecurityType;
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
            {
                Error("Calling History<TradeBar> method on a Forex or CFD security will return an empty result. Please use the generic version with QuoteBar type parameter.");
            }

            var resolutionToUse = resolution ?? GetResolution(symbol, resolution);
            if (resolutionToUse == Resolution.Tick)
            {
                throw new InvalidOperationException("Calling History<TradeBar> method with Resolution.Tick will return an empty result." +
                                                    " Please use the generic version with Tick type parameter or provide a list of Symbols to use the Slice history request API.");
            }

            return History(new[] { symbol }, start, end, resolutionToUse).Get(symbol).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbol's configured values for resolution and fill forward behavior will be used
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, TimeSpan span, Resolution? resolution = null)
        {
            return History(symbols, Time - span, Time, resolution).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null)
        {
            if (resolution == Resolution.Tick) throw new ArgumentException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");
            return History(CreateBarCountHistoryRequests(symbols, periods, resolution)).Memoize();
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarket">True to include extended market hours data, false otherwise</param>
        /// <returns>An enumerable of slice containing the requested historical data</returns>
        public IEnumerable<Slice> History(IEnumerable<Symbol> symbols, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarket = null)
        {
            return History(CreateDateRangeHistoryRequests(symbols, start, end, resolution, fillForward, extendedMarket)).Memoize();
        }

        /// <summary>
        /// Executes the specified history request
        /// </summary>
        /// <param name="request">the history request to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        public IEnumerable<Slice> History(HistoryRequest request)
        {
            return History(new[] { request }).Memoize();
        }

        /// <summary>
        /// Executes the specified history requests
        /// </summary>
        /// <param name="requests">the history requests to execute</param>
        /// <returns>An enumerable of slice satisfying the specified history request</returns>
        public IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests)
        {
            return History(requests, TimeZone).Memoize();
        }

        /// <summary>
        /// Yields data to warmup a security for all it's subscribed data types
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>Securities historical data</returns>
        public IEnumerable<BaseData> GetLastKnownPrices(Security security)
        {
            return GetLastKnownPrices(security.Symbol);
        }

        /// <summary>
        /// Yields data to warmup a security for all it's subscribed data types
        /// </summary>
        /// <param name="symbol">The symbol we want to get seed data for</param>
        /// <returns>Securities historical data</returns>
        public IEnumerable<BaseData> GetLastKnownPrices(Symbol symbol)
        {
            if (symbol.IsCanonical() || HistoryProvider == null)
            {
                return Enumerable.Empty<BaseData>();
            }

            var result = new Dictionary<TickType, BaseData>();
            // For speed and memory usage, use Resolution.Minute as the minimum resolution
            var resolution = (Resolution)Math.Max((int)Resolution.Minute,
                (int)SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).GetHighestResolution());
            Func<int, bool> requestData = period =>
            {
                var historyRequests = CreateBarCountHistoryRequests(new[] { symbol }, period, resolution)
                    .Select(request =>
                    {
                        // force no fill forward behavior
                        request.FillForwardResolution = null;
                        return request;
                    })
                    // request only those tick types we didn't get the data we wanted
                    .Where(request => !result.ContainsKey(request.TickType))
                    .ToList();
                foreach (var slice in History(historyRequests))
                {
                    for (var i = 0; i < historyRequests.Count; i++)
                    {
                        var historyRequest = historyRequests[i];
                        var data = slice.Get(historyRequest.DataType);
                        if (data.ContainsKey(symbol))
                        {
                            // keep the last data point per tick type
                            result[historyRequest.TickType] = (BaseData)data[symbol];
                        }
                    }
                }
                // true when all history requests tick types have a data point
                return historyRequests.All(request => result.ContainsKey(request.TickType));
            };

            if (!requestData(5))
            {
                // If the first attempt to get the last know price returns null, it maybe the case of an illiquid security.
                // We increase the look-back period for this case accordingly to the resolution to cover 3 trading days
                var periods =
                    resolution == Resolution.Daily ? 3 :
                    resolution == Resolution.Hour ? 24 : 1440;
                requestData(periods);
            }
            // return the data ordered by time ascending
            return result.Values.OrderBy(data => data.Time);
        }

        /// <summary>
        /// Get the last known price using the history provider.
        /// Useful for seeding securities with the correct price
        /// </summary>
        /// <param name="security"><see cref="Security"/> object for which to retrieve historical data</param>
        /// <returns>A single <see cref="BaseData"/> object with the last known price</returns>
        [Obsolete("This method is obsolete please use 'GetLastKnownPrices' which will return the last data point" +
            " for each type associated with the requested security")]
        public BaseData GetLastKnownPrice(Security security)
        {
            return GetLastKnownPrices(security.Symbol)
                // since we are returning a single data point let's respect order
                .OrderByDescending(data => GetTickTypeOrder(data.Symbol.SecurityType, LeanData.GetCommonTickTypeForCommonDataTypes(data.GetType(), data.Symbol.SecurityType)))
                .LastOrDefault();
        }

        /// <summary>
        /// Centralized logic to get a configuration for a symbol, a data type and a resolution
        /// </summary>
        private SubscriptionDataConfig GetHistoryRequestConfig(Symbol symbol, Type requestedType, Resolution? resolution = null)
        {
            if (symbol == null) throw new ArgumentException(_symbolEmptyErrorMessage);

            // verify the types match
            var config = GetMatchingSubscription(symbol, requestedType, resolution);
            if (config == null)
            {
                var actualType = GetMatchingSubscription(symbol, typeof(BaseData)).Type;
                var message = $"The specified security is not of the requested type. Symbol: {symbol} Requested Type: {requestedType.Name} Actual Type: {actualType}";
                if (resolution.HasValue)
                {
                    message += $" Requested Resolution.{resolution.Value}";
                }
                throw new ArgumentException(message);
            }
            return config;
        }

        private IEnumerable<Slice> History(IEnumerable<HistoryRequest> requests, DateTimeZone timeZone)
        {
            var sentMessage = false;
            // filter out any universe securities that may have made it this far
            var filteredRequests = requests.Where(hr => !UniverseManager.ContainsKey(hr.Symbol)).ToList();
            for (var i = 0; i < filteredRequests.Count; i++)
            {
                var request  = filteredRequests[i];
                // prevent future requests
                if (request.EndTimeUtc > UtcTime)
                {
                    var endTimeUtc = UtcTime;
                    var startTimeUtc = request.StartTimeUtc;
                    if (request.StartTimeUtc > request.EndTimeUtc)
                    {
                        startTimeUtc = request.EndTimeUtc;
                    }

                    filteredRequests[i] = new HistoryRequest(startTimeUtc, endTimeUtc,
                        request.DataType, request.Symbol, request.Resolution, request.ExchangeHours,
                        request.DataTimeZone, request.FillForwardResolution, request.IncludeExtendedMarketHours,
                        request.IsCustomData, request.DataNormalizationMode, request.TickType);

                    if (!sentMessage)
                    {
                        sentMessage = true;
                        Debug("Request for future history modified to end now.");
                    }
                }
            }

            // filter out future data to prevent look ahead bias
            return ((IAlgorithm)this).HistoryProvider.GetHistory(filteredRequests, timeZone);
        }

        /// <summary>
        /// Helper method to create history requests from a date range
        /// </summary>
        private IEnumerable<HistoryRequest> CreateDateRangeHistoryRequests(IEnumerable<Symbol> symbols, DateTime startAlgoTz, DateTime endAlgoTz, Resolution? resolution = null, bool? fillForward = null, bool? extendedMarket = null)
        {
            return symbols.Where(x => !x.IsCanonical()).SelectMany(x =>
            {
                var requests = new List<HistoryRequest>();

                foreach (var config in GetMatchingSubscriptions(x, typeof(BaseData), resolution))
                {
                    var request = _historyRequestFactory.CreateHistoryRequest(config, startAlgoTz, endAlgoTz, GetExchangeHours(x), resolution);

                    // apply overrides
                    var res = GetResolution(x, resolution);
                    if (fillForward.HasValue) request.FillForwardResolution = fillForward.Value ? res : (Resolution?)null;
                    if (extendedMarket.HasValue) request.IncludeExtendedMarketHours = extendedMarket.Value;

                    requests.Add(request);
                }

                return requests;
            });
        }

        /// <summary>
        /// Helper methods to create a history request for the specified symbols and bar count
        /// </summary>
        private IEnumerable<HistoryRequest> CreateBarCountHistoryRequests(IEnumerable<Symbol> symbols, int periods, Resolution? resolution = null)
        {
            return symbols.Where(x => !x.IsCanonical()).SelectMany(x =>
            {
                var res = GetResolution(x, resolution);
                var exchange = GetExchangeHours(x);
                var configs = GetMatchingSubscriptions(x, typeof(BaseData), resolution).ToList();
                if (!configs.Any())
                {
                    return Enumerable.Empty<HistoryRequest>();
                }

                var start = _historyRequestFactory.GetStartTimeAlgoTz(x, periods, res, exchange, configs.First().DataTimeZone);
                var end = Time;

                return configs.Select(config => _historyRequestFactory.CreateHistoryRequest(config, start, end, exchange, res));
            });
        }

        private SubscriptionDataConfig GetMatchingSubscription(Symbol symbol, Type type, Resolution? resolution = null)
        {
            // find the first subscription matching the requested type with a higher resolution than requested
            return GetMatchingSubscriptions(symbol, type, resolution).FirstOrDefault();
        }

        private int GetTickTypeOrder(SecurityType securityType, TickType tickType)
        {
            return SubscriptionManager.AvailableDataTypes[securityType].IndexOf(tickType);
        }

        private IEnumerable<SubscriptionDataConfig> GetMatchingSubscriptions(Symbol symbol, Type type, Resolution? resolution = null)
        {
            var matchingSubscriptions = SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(symbol, includeInternalConfigs:true)
                // find all subscriptions matching the requested type with a higher resolution than requested
                .OrderByDescending(s => s.Resolution)
                // lets make sure to respect the order of the data types
                .ThenByDescending(config => GetTickTypeOrder(config.SecurityType, config.TickType))
                .ThenBy(config => config.IsInternalFeed ? 1 : 0)
                .Where(s => SubscriptionDataConfigTypeFilter(type, s.Type))
                .ToList();
            // we use the subscription manager registered configurations here, we can not rely on the Securities collection
            // since this might be called when creating a security and warming it up
            if (matchingSubscriptions.Count != 0)
            {
                if (resolution.HasValue
                    && (resolution == Resolution.Daily || resolution == Resolution.Hour)
                    && symbol.SecurityType == SecurityType.Equity)
                {
                    // for Daily and Hour resolution, for equities, we have to
                    // filter out any existing subscriptions that could be of Quote type
                    // This could happen if they were Resolution.Minute/Second/Tick
                    return matchingSubscriptions.Where(s => s.TickType != TickType.Quote);
                }

                return matchingSubscriptions;
            }
            else
            {
                var entry = MarketHoursDatabase.GetEntry(symbol, new []{ type });
                resolution = GetResolution(symbol, resolution);

                return SubscriptionManager
                    .LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution.Value, symbol.IsCanonical())
                    .Where(tuple => SubscriptionDataConfigTypeFilter(type, tuple.Item1))
                    .Select(x => new SubscriptionDataConfig(
                        x.Item1,
                        symbol,
                        resolution.Value,
                        entry.DataTimeZone,
                        entry.ExchangeHours.TimeZone,
                        UniverseSettings.FillForward,
                        UniverseSettings.ExtendedMarketHours,
                        true,
                        false,
                        x.Item2,
                        true,
                        UniverseSettings.DataNormalizationMode));
            }
        }

        /// <summary>
        /// Helper method to determine if the provided config type passes the filter of the target type
        /// </summary>
        /// <remarks>If the target type is <see cref="BaseData"/>, <see cref="OpenInterest"/> config types will return false.
        /// This is useful to filter OpenInterest by default from history requests unless it's explicitly requested</remarks>
        private bool SubscriptionDataConfigTypeFilter(Type targetType, Type configType)
        {
            var targetIsGenericType = targetType == typeof(BaseData);

            return targetType.IsAssignableFrom(configType) && (!targetIsGenericType || configType != typeof(OpenInterest));
        }

        private SecurityExchangeHours GetExchangeHours(Symbol symbol)
        {
            return GetMarketHours(symbol).ExchangeHours;
        }

        private MarketHoursDatabase.Entry GetMarketHours(Symbol symbol)
        {
            var hoursEntry = MarketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);

            // user can override the exchange hours in algorithm, i.e. HistoryAlgorithm
            Security security;
            if (Securities.TryGetValue(symbol, out security))
            {
                return new MarketHoursDatabase.Entry(hoursEntry.DataTimeZone, security.Exchange.Hours);
            }

            return hoursEntry;
        }

        private Resolution GetResolution(Symbol symbol, Resolution? resolution)
        {
            Security security;
            if (Securities.TryGetValue(symbol, out security))
            {
                return resolution ?? SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(symbol)
                    .GetHighestResolution();
            }
            else
            {
                return resolution ?? UniverseSettings.Resolution;
            }
        }
    }
}
