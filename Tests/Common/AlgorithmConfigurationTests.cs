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

using System.Collections.Generic;

using NUnit.Framework;

using QuantConnect.Brokerages;
using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class AlgorithmConfigurationTests
    {
        [TestCaseSource(nameof(AlgorithmConfigurationTestCases))]
        public void CreatesConfiguration(string currency, BrokerageName brokerageName, AccountType accountType,
            Dictionary<string, string> parameters)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetAccountCurrency(currency);
            algorithm.SetBrokerageModel(brokerageName, accountType);
            algorithm.SetParameters(parameters);


            var algorithmConfiguration = AlgorithmConfiguration.Create(algorithm);

            Assert.AreEqual(currency, algorithmConfiguration.AccountCurrency);
            Assert.AreEqual(brokerageName, algorithmConfiguration.BrokerageName);
            Assert.AreEqual(accountType, algorithmConfiguration.AccountType);
            CollectionAssert.AreEquivalent(parameters, algorithmConfiguration.Parameters);
        }

        private static TestCaseData[] AlgorithmConfigurationTestCases => new[]
        {
            new TestCaseData("BTC", BrokerageName.Binance, AccountType.Cash,
                new Dictionary<string, string> { { "param1", "param1 value" }, { "param2", "param2 value" } }),
            new TestCaseData("USDT", BrokerageName.GDAX, AccountType.Cash,
                new Dictionary<string, string> { { "a", "A" }, { "b", "B" } }),
            new TestCaseData("EUR", BrokerageName.Bitfinex, AccountType.Margin,
                new Dictionary<string, string> { { "first", "1" }, { "second", "2" }, { "third", "3" } }),
            new TestCaseData("AUD", BrokerageName.Atreyu, AccountType.Margin,
                new Dictionary<string, string> { { "ema-slow", "20" }, { "ema-fast", "10" } })
        };
    }
}
