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
using NUnit.Framework;
using Python.Runtime;

namespace QuantConnect.Tests.Algorithm.Framework.Selection
{
    [TestFixture]
    public class ETFConstituentsUniverseSelectionModelTests
    {
        [TestCase("from Selection.ETFConstituentsUniverseSelectionModel import *", "Selection.ETFConstituentsUniverseSelectionModel.ETFConstituentsUniverseSelectionModel")]
        [TestCase("from QuantConnect.Algorithm.Framework.Selection import *", "QuantConnect.Algorithm.Framework.Selection.ETFConstituentsUniverseSelectionModel")]
        public void TestPythonAndCSharpImports(string importStatement, string expected)
        {
            using (Py.GIL())
            {
                dynamic module = PyModule.FromString("testModule",
                    @$"from AlgorithmImports import *
{importStatement}
class ETFConstituentsFrameworkAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Daily
        symbol = Symbol.Create('SPY', SecurityType.Equity, Market.USA)
        selection_model = ETFConstituentsUniverseSelectionModel(symbol, self.UniverseSettings, self.ETFConstituentsFilter)
        self.universe_type = str(type(selection_model))

    def ETFConstituentsFilter(self, constituents):
        return [c.Symbol for c in constituents]");
                
                dynamic algorithm = module.GetAttr("ETFConstituentsFrameworkAlgorithm").Invoke();
                algorithm.Initialize();
                string universeTypeStr = algorithm.universe_type.ToString();
                Assert.IsTrue(universeTypeStr.Contains(expected, StringComparison.InvariantCulture));
            }
        }
    }
}
