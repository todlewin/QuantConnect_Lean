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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that market orders are supported on extended market hours for futures.
    /// </summary>
    public class MarketOrdersAreSupportedOnExtendedHoursForFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Future _futureContract;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 10);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0,
                extendedMarketHours: true
            );
            _futureContract = AddFutureContract(FutureChainProvider.GetFutureContractList(_continuousContract.Symbol, Time).First(),
                extendedMarketHours: true);
        }
        public override void OnData(Slice slice)
        {
            if (Time.TimeOfDay.Hours > 18 && !Portfolio.Invested)
            {
                var futureContractMarketOrder = MarketOrder(_futureContract.Symbol, 1);
                var continuousContractMarketOrder = MarketOrder(_continuousContract.Mapped, 1);
                if (futureContractMarketOrder.Status == OrderStatus.Invalid || continuousContractMarketOrder.Status == OrderStatus.Invalid)
                {
                    throw new Exception($"Market orders should be allowed for futures outside of regular market hours");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Transactions.GetOrders().Any(order => order.Status != OrderStatus.Filled ))
            {
                throw new Exception("Not all orders were filled");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // 13:30 and 21:00 UTC are 9:30 and 17 New york, which are the regular market hours litimits for this security
            if (orderEvent.Status == OrderStatus.Filled && !Securities[orderEvent.Symbol].Exchange.DateTimeIsOpen(orderEvent.UtcTime) &&
                (orderEvent.UtcTime.TimeOfDay >= new TimeSpan(13, 30, 0) && orderEvent.UtcTime.TimeOfDay < new TimeSpan(21, 0, 0)))
            {
                throw new Exception($"Order should have been filled during extended market hours");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 82366;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "113.036%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.041%"},
            {"Sharpe Ratio", "4.286"},
            {"Probabilistic Sharpe Ratio", "58.720%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.133"},
            {"Beta", "1.285"},
            {"Annual Standard Deviation", "0.314"},
            {"Annual Variance", "0.098"},
            {"Information Ratio", "15.206"},
            {"Tracking Error", "0.078"},
            {"Treynor Ratio", "1.046"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$12000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "33.61%"},
            {"OrderListHash", "e5f9bd8f1fe5684eab176db3e67ec772"}
        };
    }
}
