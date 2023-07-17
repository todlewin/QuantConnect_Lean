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

using System;
using QuantConnect.Data;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Daily data
    /// </summary>
    public class BasicTemplateIndexDailyAlgorithm : BasicTemplateIndexAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;
        protected override int StartDay => 1;

        // two complete weeks starting from the 5th. The 18th bar is not included since it is a holiday
        protected virtual int ExpectedBarCount => 2 * 5;
        protected int BarCounter = 0;

        /// <summary>
        /// Purchase a contract when we are not invested, liquidate otherwise
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // SPX Index is not tradable, but we can trade an option
                MarketOrder(SpxOption, 1);
            }
            else
            {
                Liquidate();
            }

            // Count how many slices we receive with SPX data in it to assert later
            if (slice.ContainsKey(Spx))
            {
                BarCounter++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (BarCounter != ExpectedBarCount)
            {
                throw new ArgumentException($"Bar Count {BarCounter} is not expected count of {ExpectedBarCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 0;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
