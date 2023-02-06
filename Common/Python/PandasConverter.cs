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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        private static dynamic _pandas;
        private static PyObject _concat;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        public PandasConverter()
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    var pandas = Py.Import("pandas");
                    _pandas = pandas;
                    // keep it so we don't need to ask for it each time
                    _concat = pandas.GetAttr("concat");
                }
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <param name="dataType">Optional type of bars to add to the data frame</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data, Type dataType = null)
        {
            var maxLevels = 0;
            var sliceDataDict = new Dictionary<Symbol, PandasData>();

            foreach (var slice in data)
            {
                if (dataType == null)
                {
                    AddSliceDataToDict(slice, sliceDataDict, ref maxLevels);
                }
                else
                {
                    AddSliceDataTypeDataToDict(slice, dataType, sliceDataDict, ref maxLevels);
                }
            }

            using (Py.GIL())
            {
                if (sliceDataDict.Count == 0)
                {
                    return _pandas.DataFrame();
                }
                using var dataFrames = sliceDataDict.Select(x => x.Value.ToPandasDataFrame(maxLevels)).ToPyList();
                using var sortDic = Py.kw("sort", true);
                var result = _concat.Invoke(new[] { dataFrames }, sortDic);

                foreach (var df in dataFrames)
                {
                    df.Dispose();
                }
                return result;
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseData"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame<T>(IEnumerable<T> data)
            where T : IBaseData
        {
            PandasData sliceData = null;
            foreach (var datum in data)
            {
                if (sliceData == null)
                {
                    sliceData = new PandasData(datum);
                }

                sliceData.Add(datum);
            }

            using (Py.GIL())
            {
                // If sliceData is still null, data is an empty enumerable
                // returns an empty pandas.DataFrame
                if (sliceData == null)
                {
                    return _pandas.DataFrame();
                }
                return sliceData.ToPandasDataFrame();
            }
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Dictionary with a list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(IDictionary<string, List<IndicatorDataPoint>> data)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();

                foreach (var kvp in data)
                {
                    AddSeriesToPyDict(kvp.Key, kvp.Value, pyDict);
                }

                return MakeIndicatorDataFrame(pyDict);
            }
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data"><see cref="PyObject"/> that should be a dictionary (convertible to PyDict) of string to list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(PyObject data)
        {
            using (Py.GIL())
            {
                using var inputPythonType = data.GetPythonType();
                var inputTypeStr = inputPythonType.ToString();
                var targetTypeStr = nameof(PyDict);
                PyObject currentKvp = null;

                try
                {
                    using var pyDictData = new PyDict(data);
                    using var seriesPyDict = new PyDict();

                    targetTypeStr = $"{nameof(String)}: {nameof(List<IndicatorDataPoint>)}";

                    foreach (var kvp in pyDictData.Items())
                    {
                        currentKvp = kvp;
                        AddSeriesToPyDict(kvp[0].As<string>(), kvp[1].As<List<IndicatorDataPoint>>(), seriesPyDict);
                    }

                    return MakeIndicatorDataFrame(seriesPyDict);
                }
                catch (Exception e)
                {
                    if (currentKvp != null)
                    {
                        inputTypeStr = $"{currentKvp[0].GetPythonType()}: {currentKvp[1].GetPythonType()}";
                    }

                    throw new ArgumentException(
                        $"ConvertToDictionary cannot be used to convert a {inputTypeStr} into {targetTypeStr}. Reason: {e.Message}",
                        e
                    );
                }
            }
        }

        /// <summary>
        /// Returns a string that represent the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _pandas == null
                ? "pandas module was not imported."
                : _pandas.Repr();
        }

        /// <summary>
        /// Creates a series from a list of <see cref="IndicatorDataPoint"/> and adds it to the
        /// <see cref="PyDict"/> as the value of the given <paramref name="key"/>
        /// </summary>
        /// <param name="key">Key to insert in the <see cref="PyDict"/></param>
        /// <param name="points">List of <see cref="IndicatorDataPoint"/> that will make up the resulting series</param>
        /// <param name="pyDict"><see cref="PyDict"/> where the resulting key-value pair will be inserted into</param>
        private void AddSeriesToPyDict(string key, List<IndicatorDataPoint> points, PyDict pyDict)
        {
            var index = new List<DateTime>();
            var values = new List<double>();

            foreach (var point in points)
            {
                index.Add(point.EndTime);
                values.Add((double) point.Value);
            }
            pyDict.SetItem(key.ToLowerInvariant(), _pandas.Series(values, index));
        }

        /// <summary>
        /// Converts a <see cref="PyDict"/> of string to pandas.Series in a pandas.DataFrame
        /// </summary>
        /// <param name="pyDict"><see cref="PyDict"/> of string to pandas.Series</param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        private PyObject MakeIndicatorDataFrame(PyDict pyDict)
        {
            return _pandas.DataFrame(pyDict, columns: pyDict.Keys().Select(x => x.As<string>().ToLowerInvariant()).OrderBy(x => x));
        }

        /// <summary>
        /// Gets the <see cref="PandasData"/> for the given symbol if it exists in the dictionary, otherwise it creates a new instance with the
        /// given base data and adds it to the dictionary
        /// </summary>
        private PandasData GetPandasDataValue(IDictionary<Symbol, PandasData> sliceDataDict, Symbol symbol, object data, ref int maxLevels)
        {
            PandasData value;
            if (!sliceDataDict.TryGetValue(symbol, out value))
            {
                sliceDataDict.Add(symbol, value = new PandasData(data));
                maxLevels = Math.Max(maxLevels, value.Levels);
            }

            return value;
        }

        /// <summary>
        /// Adds each slice data to the pandas data dictionary
        /// </summary>
        private void AddSliceDataToDict(Slice slice, IDictionary<Symbol, PandasData> sliceDataDict, ref int maxLevels)
        {
            foreach (var key in slice.Keys)
            {
                var baseData = slice[key];
                var value = GetPandasDataValue(sliceDataDict, key, baseData, ref maxLevels);

                if (value.IsCustomData)
                {
                    value.Add(baseData);
                }
                else
                {
                    var ticks = slice.Ticks.ContainsKey(key) ? slice.Ticks[key] : null;
                    var tradeBars = slice.Bars.ContainsKey(key) ? slice.Bars[key] : null;
                    var quoteBars = slice.QuoteBars.ContainsKey(key) ? slice.QuoteBars[key] : null;
                    value.Add(ticks, tradeBars, quoteBars);
                }
            }
        }

        /// <summary>
        /// Adds each slice data corresponding to the requested data type to the pandas data dictionary
        /// </summary>
        private void AddSliceDataTypeDataToDict(Slice slice, Type dataType, IDictionary<Symbol, PandasData> sliceDataDict, ref int maxLevels)
        {
            var isTick = dataType == typeof(Tick) || dataType == typeof(OpenInterest);
            // Access ticks directly since slice.Get(typeof(Tick)) and slice.Get(typeof(OpenInterest)) will return only the last tick
            var sliceData = isTick ? slice.Ticks : slice.Get(dataType);

            foreach (var key in sliceData.Keys)
            {
                var baseData = sliceData[key];
                PandasData value = GetPandasDataValue(sliceDataDict, key, baseData, ref maxLevels);

                if (value.IsCustomData)
                {
                    value.Add(baseData);
                }
                else
                {
                    var ticks = isTick ? baseData : null;
                    var tradeBars = dataType == typeof(TradeBar) ? baseData : null;
                    var quoteBars = dataType == typeof(QuoteBar) ? baseData : null;
                    value.Add(ticks, tradeBars, quoteBars);
                }
            }
        }
    }
}
