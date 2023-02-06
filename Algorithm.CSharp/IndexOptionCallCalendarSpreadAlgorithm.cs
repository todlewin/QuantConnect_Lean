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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionCallCalendarSpreadAlgorithm : QCAlgorithm
    {
        private Symbol _vixw, _vxz, _spy;
        private decimal _multiplier;
        private List<Leg> _legs = new();
        private DateTime _firstExpiry = DateTime.MaxValue;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2021, 1, 1);
            SetCash(50000);

            _vxz = AddEquity("VXZ", Resolution.Minute).Symbol;
            _spy = AddEquity("SPY", Resolution.Minute).Symbol;

            var index = AddIndex("VIX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "VIXW", Resolution.Minute);
            option.SetFilter((x) => x.Strikes(-2, 2).Expiration(15, 45));

            _vixw = option.Symbol;
            _multiplier = option.SymbolProperties.ContractMultiplier;
        }

        public override void OnData(Slice slice)
        {
            // Liquidate if the shorter term option is about to expire
            if (_firstExpiry < Time.AddDays(2) && _legs.All(x => slice.ContainsKey(x.Symbol)))
            {
                Liquidate();
            }
            // Return if there is any opening position
            else if (_legs.Any(x => Portfolio[x.Symbol].Invested))
            {
                return;
            }

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_vixw, out var chain)) return;

            // Get ATM strike price
            var strike = chain.MinBy(x => Math.Abs(x.Strike - chain.Underlying.Value)).Strike;
            
            // Select the ATM call Option contracts and sort by expiration date
            var calls = chain.Where(x => x.Strike == strike && x.Right == OptionRight.Call)
                            .OrderBy(x => x.Expiry).ToArray();
            if (calls.Length < 2) return;
            _firstExpiry = calls[0].Expiry;

            // Create combo order legs
            _legs = new List<Leg>
            {
                Leg.Create(calls[0].Symbol, -1),
                Leg.Create(calls[^1].Symbol, 1),
                Leg.Create(_vxz, -100),
                Leg.Create(_spy, -10)
            };
            var quantity = Portfolio.TotalPortfolioValue / _legs.Sum(x =>
            {
                var value = Math.Abs(Securities[x.Symbol].Price * x.Quantity);
                return x.Symbol.ID.SecurityType == SecurityType.IndexOption
                    ? value * _multiplier
                    : value;
            });
            ComboMarketOrder(_legs, -(int)Math.Floor(quantity), asynchronous: true);
        }
    }
}
