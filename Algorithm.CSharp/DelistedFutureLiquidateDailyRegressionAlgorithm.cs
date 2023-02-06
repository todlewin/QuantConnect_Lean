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
 *
*/

using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduces GH issue 4446, in the case of daily resolution.
    /// </summary>
    public class DelistedFutureLiquidateDailyRegressionAlgorithm : DelistedFutureLiquidateRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 1850;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "7.78%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "38.033%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "7.779%"},
            {"Sharpe Ratio", "3.2"},
            {"Probabilistic Sharpe Ratio", "99.459%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.149"},
            {"Beta", "0.271"},
            {"Annual Standard Deviation", "0.08"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-1.446"},
            {"Tracking Error", "0.098"},
            {"Treynor Ratio", "0.946"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$60000000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.019"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "28.065"},
            {"Return Over Maximum Drawdown", "246.484"},
            {"Portfolio Turnover", "0.019"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "bbd8ee011b21ef33f4b15b0509c96bbb"}
        };
    }
}
