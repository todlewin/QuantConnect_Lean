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

using Python.Runtime;
using QuantConnect.Python;
using QuantConnect.Securities.Option;
using System.Collections.Generic;

namespace QuantConnect.Orders.OptionExercise
{
    /// <summary>
    /// Python wrapper for custom option exercise models
    /// </summary>
    public class OptionExerciseModelPythonWrapper: IOptionExerciseModel
    {
        private readonly dynamic _model;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="model">The python model to wrapp</param>
        public OptionExerciseModelPythonWrapper(PyObject model)
        {
            _model = model.ValidateImplementationOf<IOptionExerciseModel>();
        }

        /// <summary>
        /// Performs option exercise for the option security class.
        /// </summary>
        /// <param name="option">Option we're trading this order</param>
        /// <param name="order">Order to update</param>
        public IEnumerable<OrderEvent> OptionExercise(Option option, OptionExerciseOrder order)
        {
            using (Py.GIL())
            {
                using var orderEventGenerator = _model.OptionExercise(option, order) as PyObject;
                using var iterator = orderEventGenerator.GetIterator();
                foreach (PyObject item in iterator)
                {
                    yield return item.GetAndDispose<OrderEvent>();
                }
            }
        }
    }
}
