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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities.CurrencyConversion
{
    /// <summary>
    /// Provides an implementation of <see cref="ICurrencyConversion"/> with a fixed conversion rate
    /// </summary>
    public class ConstantCurrencyConversion : ICurrencyConversion
    {
        private decimal _conversionRate;

        /// <summary>
        /// Event fired when the conversion rate is updated
        /// </summary>
        public event EventHandler<decimal> ConversionRateUpdated;

        /// <summary>
        /// The currency this conversion converts from
        /// </summary>
        public string SourceCurrency { get; }

        /// <summary>
        /// The currency this conversion converts to
        /// </summary>
        public string DestinationCurrency { get; }

        /// <summary>
        /// The current conversion rate
        /// </summary>
        public decimal ConversionRate
        {
            get
            {
                return _conversionRate;
            }
            set
            {
                if (_conversionRate != value)
                {
                    // only update if there was actually one
                    _conversionRate = value;
                    ConversionRateUpdated?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// The securities which the conversion rate is based on
        /// </summary>
        public IEnumerable<Security> ConversionRateSecurities => Enumerable.Empty<Security>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantCurrencyConversion"/> class.
        /// </summary>
        /// <param name="sourceCurrency">The currency this conversion converts from</param>
        /// <param name="destinationCurrency">The currency this conversion converts to</param>
        /// <param name="conversionRate">The conversion rate between the currencies</param>
        public ConstantCurrencyConversion(string sourceCurrency, string destinationCurrency, decimal conversionRate = 1m)
        {
            SourceCurrency = sourceCurrency;
            DestinationCurrency = destinationCurrency;
            ConversionRate = conversionRate;
        }

        /// <summary>
        /// Marks the conversion rate as potentially outdated, needing an update based on the latest data
        /// </summary>
        /// <remarks>This conversion is not based on securities, so we don't really need an update</remarks>
        public void Update()
        {
        }

        /// <summary>
        /// Creates a new identity conversion, where the conversion rate is set to 1 and the source and destination currencies might the same
        /// </summary>
        /// <param name="sourceCurrency">The currency this conversion converts from</param>
        /// <param name="destinationCurrency">The currency this conversion converts to. If null, the destination and source currencies are the same</param>
        /// <returns>The identity currency conversion</returns>
        public static ConstantCurrencyConversion Identity(string sourceCurrency, string destinationCurrency = null)
        {
            return new ConstantCurrencyConversion(sourceCurrency, destinationCurrency ?? sourceCurrency);
        }

        /// <summary>
        /// Returns an instance of <see cref="ConstantCurrencyConversion"/> that represents a null conversion
        /// </summary>
        public static ConstantCurrencyConversion Null(string sourceCurrency, string destinationCurrency)
        {
            return new ConstantCurrencyConversion(sourceCurrency, destinationCurrency, 0m);
        }
    }
}
