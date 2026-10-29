using System;
using System.Collections.Generic;
using System.Linq;
using Numerics;

namespace DatabaseControls
{
    /// <summary>
    /// Provides static methods for classifying numerical data into ranges using various classification algorithms.
    /// </summary>
    /// <remarks>
    /// This class supports multiple classification methods including equal interval, defined interval,
    /// quantiles, head-tail breaks, standard deviation intervals, and Jenks natural breaks optimization.
    /// All methods return arrays of upper bound break values that define class boundaries.
    /// </remarks>
    public static class Classification
    {
        /// <summary>
        /// Determines classification range break values into equally sized intervals.
        /// </summary>
        /// <param name="data">The data array to be classified.</param>
        /// <param name="nClasses">The number of classes to determine break values.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="nClasses"/>
        /// is less than or equal to zero, or if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// This method divides the data range into equal-width intervals. Invalid values
        /// (infinity, NaN) at the beginning of sorted data are automatically skipped.
        /// </remarks>
        public static double[] EqualInterval(double[] data, int nClasses, bool dataIsSorted)
        {
            if (nClasses <= 0) return Array.Empty<double>();
            if (data == null || data.Length == 0) return Array.Empty<double>();
            if (data.Length == 1) return new double[] { data[0] };

            // Sort the data if it needs to be sorted.
            double[] sortedData = data;
            if (!dataIsSorted)
            {
                sortedData = data.ToArray();
                Array.Sort(sortedData);
            }

            if (double.IsInfinity(sortedData[0]) || double.IsNaN(sortedData[0]))
            {
                for (int i = 1; i < sortedData.Length; i++)
                {
                    if (!double.IsInfinity(sortedData[i]) && !double.IsNaN(sortedData[i]))
                    {
                        return EqualInterval(sortedData[i], sortedData.Last(), nClasses);
                    }
                }
                return Array.Empty<double>();
            }
            else
            {
                return EqualInterval(sortedData[0], sortedData.Last(), nClasses);
            }
        }

        /// <summary>
        /// Determines classification range break values into equally sized intervals using minimum and maximum values.
        /// </summary>
        /// <param name="minValue">The minimum value for determining classification.</param>
        /// <param name="maxValue">The maximum value for determining classification.</param>
        /// <param name="nClasses">The number of classes to determine break values for.</param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="nClasses"/>
        /// is less than or equal to zero. Returns a single-element array containing <paramref name="maxValue"/>
        /// if <paramref name="nClasses"/> equals 1.
        /// </returns>
        /// <remarks>
        /// The interval width is calculated as (maxValue - minValue) / nClasses.
        /// The last break value is always set to exactly maxValue to avoid floating-point precision issues.
        /// </remarks>
        public static double[] EqualInterval(double minValue, double maxValue, int nClasses)
        {
            if (nClasses <= 0) return Array.Empty<double>();
            if (nClasses == 1) return new double[] { maxValue };

            double[] result = new double[nClasses];
            double multiplier = (maxValue - minValue) / nClasses;

            for (int i = 1; i <= nClasses; i++)
            {
                result[i - 1] = minValue + i * multiplier;
            }

            result[nClasses - 1] = maxValue;
            return result;
        }

        /// <summary>
        /// Determines classification range break values for a predefined interval size.
        /// </summary>
        /// <param name="data">The data to be classified.</param>
        /// <param name="intervalSize">The size of each interval for classification.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// This method creates breaks at regular intervals of the specified size, starting from the minimum value.
        /// The final break is always set to the maximum value in the data.
        /// </remarks>
        public static double[] DefinedInterval(IList<double> data, double intervalSize, bool dataIsSorted)
        {
            if (data == null || data.Count == 0) return Array.Empty<double>();
            if (data.Count == 1) return new double[] { data[0] };
            if (dataIsSorted) return DefinedInterval(data[0], data.Last(), intervalSize);

            double[] sortedData = data.ToArray();
            Array.Sort(sortedData);
            return DefinedInterval(sortedData[0], sortedData.Last(), intervalSize);
        }

        /// <summary>
        /// Determines classification range break values for a predefined interval size using minimum and maximum values.
        /// </summary>
        /// <param name="minValue">The minimum value for determining classification.</param>
        /// <param name="maxValue">The maximum value for determining classification.</param>
        /// <param name="intervalSize">The size of each interval for classification.</param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="intervalSize"/>
        /// is less than or equal to zero or is NaN.
        /// </returns>
        /// <remarks>
        /// The number of breaks is calculated based on the range divided by interval size.
        /// Machine epsilon is used to handle floating-point precision edge cases.
        /// </remarks>
        public static double[] DefinedInterval(double minValue, double maxValue, double intervalSize)
        {
            if (intervalSize <= 0 || double.IsNaN(intervalSize)) return Array.Empty<double>();

            int nBreaks = (int)Math.Floor((maxValue - minValue) / intervalSize);
            if (Math.Abs((maxValue - minValue) / intervalSize - nBreaks) <= Tools.DoubleMachineEpsilon)
            {
                nBreaks -= 1;
            }

            double[] results = new double[nBreaks + 1];

            for (int i = 0; i < nBreaks; i++)
            {
                results[i] = minValue + intervalSize * (i + 1);
            }

            results[nBreaks] = maxValue;
            return results;
        }

        /// <summary>
        /// Determines classification range break values using quantile-based classification.
        /// </summary>
        /// <param name="data">The data to be classified.</param>
        /// <param name="nClasses">The number of classes (quantiles) to create.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <returns>
        /// An array of upper bound break values where each class contains approximately the same number of observations.
        /// Returns an empty array if <paramref name="nClasses"/> is less than or equal to zero,
        /// or if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// Quantile classification divides data so that each class contains an equal number of values.
        /// If <paramref name="nClasses"/> exceeds the data count, all unique values are returned as breaks.
        /// </remarks>
        public static double[] Quantiles(IList<double> data, int nClasses, bool dataIsSorted)
        {
            if (nClasses <= 0) return Array.Empty<double>();
            if (data == null || data.Count == 0) return Array.Empty<double>();
            if (data.Count == 1) return new double[] { data[0] };

            if (nClasses > data.Count)
            {
                if (dataIsSorted) return data.ToArray();
                double[] sortedData = data.ToArray();
                Array.Sort(sortedData);
                return sortedData;
            }

            if (nClasses == 1)
            {
                return dataIsSorted ? new double[] { data.Last() } : new double[] { data.Max() };
            }

            double countPerBin = data.Count / (double)nClasses;
            double[] results = new double[nClasses];

            if (dataIsSorted)
            {
                for (int i = 1; i < nClasses; i++)
                {
                    results[i - 1] = data[Convert.ToInt32(countPerBin * i - 1)];
                }
                results[nClasses - 1] = data[data.Count - 1];
            }
            else
            {
                double[] sortedData = data.ToArray();
                Array.Sort(sortedData);

                for (int i = 1; i < nClasses; i++)
                {
                    results[i - 1] = sortedData[Convert.ToInt32(countPerBin * i - 1)];
                }
                results[nClasses - 1] = sortedData[sortedData.Length - 1];
            }

            return results;
        }

        /// <summary>
        /// Determines classification range break values using the head/tail breaks algorithm.
        /// </summary>
        /// <param name="data">The data array to be classified.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <param name="threshold">
        /// The proportion threshold (0 to 1) of the last bin to stop the iterative classification process.
        /// Default is 0.4.
        /// </param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// Head/tail breaks is designed for data with a heavy-tailed distribution. It iteratively partitions
        /// data around the mean, creating natural breaks for data that follows a power-law or similar distribution.
        /// </remarks>
        public static double[] HeadTailInterval(double[] data, bool dataIsSorted, double threshold = 0.4)
        {
            if (data == null) return Array.Empty<double>();
            if (data.Length == 0) return Array.Empty<double>();
            if (data.Length == 1) return new double[] { data[0] };

            double[] sortedData = data;
            if (!dataIsSorted)
            {
                sortedData = data.ToArray();
                Array.Sort(sortedData);
            }

            double avg = sortedData.Average();
            List<double> results = new List<double> { avg };

            int initialSplit = Array.BinarySearch(sortedData, avg);
            if (initialSplit < 0) initialSplit = ~initialSplit;

            int splitIndex = initialSplit;
            int previousCount = sortedData.Length;

            while ((sortedData.Length - splitIndex) / (double)previousCount <= threshold && (sortedData.Length - splitIndex > 1))
            {
                avg = 0.0;

                for (int i = splitIndex; i < sortedData.Length; i++)
                {
                    avg += sortedData[i];
                }

                avg = avg / (sortedData.Length - splitIndex);
                results.Add(avg);
                previousCount = sortedData.Length - splitIndex;
                splitIndex = Array.BinarySearch(sortedData, splitIndex, sortedData.Length - splitIndex, avg);
                if (splitIndex < 0) splitIndex = ~splitIndex;
                if (sortedData.Length - splitIndex == previousCount) break;
            }

            if (results.Last() != sortedData.Last())
            {
                results.Add(sortedData[sortedData.Length - 1]);
            }

            results.Sort();
            return results.ToArray();
        }

        /// <summary>
        /// Determines classification range break values based on standard deviation intervals from the mean.
        /// </summary>
        /// <param name="data">The data to be classified.</param>
        /// <param name="nDeviations">The number of standard deviations to use as the interval multiplier.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <returns>
        /// An array of upper bound break values. Returns an empty array if <paramref name="nDeviations"/>
        /// is less than or equal to zero, or if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// This method creates breaks at multiples of the standard deviation above and below the mean.
        /// It is useful for normally distributed data where breaks should reflect statistical properties.
        /// </remarks>
        public static double[] StandardDeviationInterval(IList<double> data, double nDeviations, bool dataIsSorted)
        {
            if (nDeviations <= 0.0) return Array.Empty<double>();
            if (data == null) return Array.Empty<double>();
            if (data.Count == 0) return Array.Empty<double>();
            if (data.Count == 1) return new double[] { data[0] };

            IList<double> sortedData = data;
            if (!dataIsSorted)
            {
                double[] sortedArray = data.ToArray();
                Array.Sort(sortedArray);
                sortedData = sortedArray;
            }

            double summaryData = Numerics.Data.Statistics.Statistics.PopulationStandardDeviation(sortedData);
            return StandardDeviationInterval(sortedData.Average(), summaryData, sortedData[0], sortedData[sortedData.Count - 1], nDeviations);
        }

        /// <summary>
        /// Determines classification range break values based on standard deviation intervals using pre-computed statistics.
        /// </summary>
        /// <param name="mean">The mean (average) of the data.</param>
        /// <param name="standardDeviation">The standard deviation of the data.</param>
        /// <param name="minValue">The minimum value in the data.</param>
        /// <param name="maxValue">The maximum value in the data.</param>
        /// <param name="nDeviations">The number of standard deviations to use as the interval multiplier.</param>
        /// <returns>
        /// An array of upper bound break values sorted in ascending order.
        /// Returns an empty array if <paramref name="nDeviations"/> is less than or equal to zero.
        /// Returns a single-element array if <paramref name="minValue"/> equals <paramref name="maxValue"/>
        /// or if statistical values are NaN.
        /// </returns>
        /// <remarks>
        /// Breaks are created at intervals of nDeviations * standardDeviation, starting from
        /// mean - (standardDeviation * nDeviations / 2) and extending in both directions.
        /// </remarks>
        public static double[] StandardDeviationInterval(double mean, double standardDeviation, double minValue, double maxValue, double nDeviations)
        {
            if (nDeviations <= 0.0) return Array.Empty<double>();
            if (minValue == maxValue) return new double[] { maxValue };
            if (double.IsNaN(mean) || double.IsNaN(standardDeviation)) return new double[] { maxValue };

            List<double> results = new List<double>();
            double breakValue = mean - standardDeviation * nDeviations / 2.0;
            if (breakValue > minValue) results.Add(breakValue);

            while (breakValue >= minValue)
            {
                breakValue = breakValue - nDeviations * standardDeviation;
                if (breakValue > minValue) results.Add(breakValue);
            }

            breakValue = mean + standardDeviation * nDeviations / 2.0;
            if (breakValue < maxValue) results.Add(breakValue);

            while (breakValue <= maxValue)
            {
                breakValue = breakValue + nDeviations * standardDeviation;
                if (breakValue < maxValue) results.Add(breakValue);
            }

            results.Add(maxValue);
            results.Sort();
            return results.ToArray();
        }

        /// <summary>
        /// Determines classification range break values using the Jenks natural breaks optimization algorithm.
        /// </summary>
        /// <param name="data">The data array to be classified.</param>
        /// <param name="nClasses">The number of classes to create.</param>
        /// <param name="dataIsSorted">A boolean value indicating if the data is sorted in ascending order.</param>
        /// <param name="breakCounts">
        /// An output parameter that receives the count of values in each class.
        /// </param>
        /// <returns>
        /// An array of upper bound break values that minimize within-class variance.
        /// Returns an empty array if <paramref name="nClasses"/> is less than or equal to zero,
        /// or if <paramref name="data"/> is null or empty.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The Jenks natural breaks algorithm seeks to minimize variance within classes while maximizing
        /// variance between classes. It is also known as the Goodness of Variance Fit (GVF) method.
        /// </para>
        /// <para>
        /// This implementation uses an iterative optimization approach rather than the original
        /// Fisher-Jenks algorithm, which provides good results with better performance for large datasets.
        /// </para>
        /// </remarks>
        public static double[] JenksNaturalBreaks(double[] data, int nClasses, bool dataIsSorted, ref int[]? breakCounts)
        {
            if (nClasses <= 0) return Array.Empty<double>();
            if (data == null) return Array.Empty<double>();
            if (data.Length == 0) return Array.Empty<double>();
            if (data.Length == 1) return new double[] { data[0] };

            // Strip NaN values: NaN <= anything is false, which confuses the class-assignment
            // loop below (it can increment binIdx past distinctValues.Length into an
            // IndexOutOfRangeException). NaN in a column of real-valued measurements is
            // conventionally treated as "missing" for classification purposes.
            if (!dataIsSorted)
            {
                data = data.Where(v => !double.IsNaN(v)).ToArray();
            }
            else if (data.Any(double.IsNaN))
            {
                data = data.Where(v => !double.IsNaN(v)).ToArray();
                dataIsSorted = false;
            }
            if (data.Length == 0) return Array.Empty<double>();
            if (data.Length == 1) return new double[] { data[0] };

            double[] sortedData = data;
            if (!dataIsSorted)
            {
                sortedData = data.ToArray();
                Array.Sort(sortedData);
            }

            if (sortedData.Length <= nClasses)
            {
                if (breakCounts == null || breakCounts.Length != sortedData.Length)
                {
                    breakCounts = new int[sortedData.Length];
                }
                for (int i = 0; i < breakCounts.Length; i++)
                {
                    breakCounts[i] = 1;
                }
                return sortedData;
            }

            double[] squaredValues = new double[sortedData.Length];
            for (int i = 0; i < sortedData.Length; i++)
            {
                squaredValues[i] = sortedData[i] * sortedData[i];
            }

            double avg = sortedData.Average();
            double SDAM = 0;
            for (int i = 0; i < sortedData.Length; i++)
            {
                SDAM += Math.Pow(sortedData[i] - avg, 2.0);
            }

            // Initiate Breaks
            if (nClasses > sortedData.Length) nClasses = sortedData.Length;
            if (nClasses == 1)
            {
                breakCounts = new int[] { sortedData.Length };
                return new double[] { sortedData[sortedData.Length - 1] };
            }

            // Calculate initial classes.
            JenksBreak[] classes = new JenksBreak[nClasses];
            for (int i = 0; i < nClasses; i++)
            {
                classes[i] = new JenksBreak();
            }

            double[] distinctValues = sortedData.Distinct().ToArray();
            if (distinctValues.Length <= 2)
            {
                breakCounts = new int[] { sortedData.Length };
                return new double[] { sortedData[sortedData.Length - 1] };
            }

            if (distinctValues.Length <= nClasses)
            {
                if (breakCounts == null || breakCounts.Length != classes.Length)
                {
                    breakCounts = new int[distinctValues.Length];
                }
                int binIdx = 0;
                for (int i = 0; i < sortedData.Length; i++)
                {
                    if (sortedData[i] <= distinctValues[binIdx])
                    {
                        breakCounts[binIdx] += 1;
                    }
                    else
                    {
                        i -= 1;
                        binIdx += 1;
                    }
                }
                return distinctValues;
            }

            int classIdx = 0;
            int lastClassIdx = -1;

            if (distinctValues.Length != sortedData.Length)
            {
                int[]? argbreakCounts = null;
                double[] initialBreaks = JenksNaturalBreaks(distinctValues, nClasses, true, ref argbreakCounts);

                for (int i = 0; i < sortedData.Length; i++)
                {
                    if (sortedData[i] > initialBreaks[classIdx])
                    {
                        classIdx += 1;
                        if (classIdx >= classes.Length) classIdx = classes.Length - 1;
                    }

                    classes[classIdx].Sum += sortedData[i];
                    classes[classIdx].SumofSquares += squaredValues[i];

                    if (classIdx != lastClassIdx)
                    {
                        classes[classIdx].StartIdx = i;
                        lastClassIdx = classIdx;
                        if (classIdx > 0) classes[classIdx - 1].EndIdx = i - 1;
                    }
                }
            }
            else
            {
                double classCount = sortedData.Length / (double)nClasses;

                for (int i = 0; i < sortedData.Length; i++)
                {
                    classIdx = Convert.ToInt32(i / classCount);
                    if (classIdx > classes.Length - 1) classIdx = classes.Length - 1;
                    classes[classIdx].Sum += sortedData[i];
                    classes[classIdx].SumofSquares += squaredValues[i];

                    if (classIdx != lastClassIdx)
                    {
                        classes[classIdx].StartIdx = i;
                        lastClassIdx = classIdx;
                        if (classIdx > 0) classes[classIdx - 1].EndIdx = i - 1;
                    }
                }
            }

            classes[nClasses - 1].EndIdx = sortedData.Length - 1;
            for (int i = 0; i < nClasses; i++)
            {
                classes[i].RefreshSquareDeviation();
            }

            // Optimize
            JenksOptimize(classes, sortedData, squaredValues, 0, classes.Length - 1, SDAM);

            // Break range counts
            if (breakCounts == null || breakCounts.Length != classes.Length)
            {
                breakCounts = new int[classes.Length];
            }

            for (int i = 0; i < classes.Length; i++)
            {
                breakCounts[i] = classes[i].Count;
            }

            // Break upper bounds
            double[] result = new double[classes.Length];
            for (int i = 0; i < classes.Length; i++)
            {
                result[i] = sortedData[classes[i].EndIdx];
            }
            return result;
        }

        /// <summary>
        /// Performs iterative optimization of Jenks break positions to minimize within-class variance.
        /// </summary>
        /// <param name="classes">The array of JenksBreak objects representing current class boundaries.</param>
        /// <param name="sortedData">The sorted data array.</param>
        /// <param name="squaredValues">Pre-computed squared values of the sorted data.</param>
        /// <param name="leftIndex">The left boundary index for optimization.</param>
        /// <param name="rightIndex">The right boundary index for optimization.</param>
        /// <param name="SDAM">The Sum of Squared Deviations About the array Mean (total variance).</param>
        private static void JenksOptimize(JenksBreak[] classes, double[] sortedData, double[] squaredValues, int leftIndex, int rightIndex, double SDAM)
        {
            double minValue = 0.0;
            bool proceed = true;
            int previousFromIndex = -1;
            int previousToIndex = -1;

            while (proceed)
            {
                int fromIndex = 1;
                int toIndex = -1;
                double minSDCM = double.MaxValue;
                double SDCM;

                for (int i = leftIndex; i < rightIndex; i++)
                {
                    if (classes[i].Count == 1) continue;
                    MakeShift(classes, sortedData, squaredValues, i, i + 1, 1);
                    SDCM = GetSumSquaredDeviations(classes, leftIndex, rightIndex);

                    if (SDCM < minSDCM)
                    {
                        fromIndex = i;
                        toIndex = i + 1;
                        minSDCM = SDCM;
                    }

                    MakeShift(classes, sortedData, squaredValues, i + 1, i, 1);
                }

                for (int i = rightIndex; i > leftIndex; i--)
                {
                    if (classes[i].Count == 1) continue;
                    MakeShift(classes, sortedData, squaredValues, i, i - 1, 1);
                    SDCM = GetSumSquaredDeviations(classes, leftIndex, rightIndex);

                    if (SDCM < minSDCM)
                    {
                        fromIndex = i;
                        toIndex = i - 1;
                        minSDCM = SDCM;
                    }

                    MakeShift(classes, sortedData, squaredValues, i - 1, i, 1);
                }

                if (minSDCM == double.MaxValue) break;
                MakeShift(classes, sortedData, squaredValues, fromIndex, toIndex, 1);
                double GVF = (SDAM - GetSumSquaredDeviations(classes)) / SDAM;

                if (GVF > minValue && previousFromIndex != toIndex && previousToIndex != fromIndex)
                {
                    minValue = GVF;
                    proceed = true;
                }
                else
                {
                    MakeShift(classes, sortedData, squaredValues, toIndex, fromIndex, 1);
                    int lowerIndex, upperIndex;

                    if (toIndex > fromIndex)
                    {
                        lowerIndex = fromIndex;
                        upperIndex = toIndex;
                    }
                    else if (toIndex < fromIndex)
                    {
                        lowerIndex = toIndex;
                        upperIndex = fromIndex;
                    }
                    else
                    {
                        lowerIndex = fromIndex;
                        upperIndex = toIndex;
                    }

                    if (lowerIndex > leftIndex) JenksOptimize(classes, sortedData, squaredValues, leftIndex, lowerIndex, SDAM);
                    if (upperIndex < rightIndex) JenksOptimize(classes, sortedData, squaredValues, upperIndex, rightIndex, SDAM);
                    proceed = false;
                }

                previousFromIndex = fromIndex;
                previousToIndex = toIndex;
            }
        }

        /// <summary>
        /// Shifts data points between adjacent classes during Jenks optimization.
        /// </summary>
        /// <param name="classes">The array of JenksBreak objects.</param>
        /// <param name="sortedData">The sorted data array.</param>
        /// <param name="squaredValues">Pre-computed squared values of the sorted data.</param>
        /// <param name="currentClassIndex">The index of the class to shift data from.</param>
        /// <param name="targetId">The index of the target class to shift data to.</param>
        /// <param name="nShifts">The number of data points to shift. Default is 1.</param>
        private static void MakeShift(JenksBreak[] classes, double[] sortedData, double[] squaredValues, int currentClassIndex, int targetId, int nShifts = 1)
        {
            int dataIndex;

            for (int i = 1; i <= nShifts; i++)
            {
                if (targetId < currentClassIndex)
                {
                    if (classes[currentClassIndex].StartIdx < sortedData.Length - 1) classes[currentClassIndex].StartIdx += 1;
                    if (classes[targetId].EndIdx < sortedData.Length - 1) classes[targetId].EndIdx += 1;
                    dataIndex = classes[targetId].EndIdx;
                    if (dataIndex == -1) return;
                    classes[currentClassIndex].Sum -= sortedData[dataIndex];
                    classes[currentClassIndex].SumofSquares -= squaredValues[dataIndex];
                    classes[currentClassIndex].RefreshSquareDeviation();
                    classes[targetId].Sum += sortedData[dataIndex];
                    classes[targetId].SumofSquares += squaredValues[dataIndex];
                    classes[targetId].RefreshSquareDeviation();
                }
                else
                {
                    if (classes[currentClassIndex].EndIdx > 0) classes[currentClassIndex].EndIdx -= 1;
                    if (classes[targetId].StartIdx > 0) classes[targetId].StartIdx -= 1;
                    dataIndex = classes[targetId].StartIdx;
                    if (dataIndex == -1) return;
                    classes[currentClassIndex].Sum -= sortedData[dataIndex];
                    classes[currentClassIndex].SumofSquares -= squaredValues[dataIndex];
                    classes[currentClassIndex].RefreshSquareDeviation();
                    classes[targetId].Sum += sortedData[dataIndex];
                    classes[targetId].SumofSquares += squaredValues[dataIndex];
                    classes[targetId].RefreshSquareDeviation();
                }
            }
        }

        /// <summary>
        /// Calculates the sum of squared deviations for a range of classes.
        /// </summary>
        /// <param name="classes">The array of JenksBreak objects.</param>
        /// <param name="startIndex">The starting index of classes to include. Default is 0.</param>
        /// <param name="endIndex">The ending index of classes to include. Default is -1 (last class).</param>
        /// <returns>The sum of squared deviations across the specified classes.</returns>
        private static double GetSumSquaredDeviations(JenksBreak[] classes, int startIndex = -1, int endIndex = -1)
        {
            if (startIndex < 0) startIndex = 0;
            if (endIndex <= 0) endIndex = classes.Length - 1;
            double sum = 0.0;

            for (int i = startIndex; i <= endIndex; i++)
            {
                sum += classes[i].SquareDeviation;
            }

            return sum;
        }

        /// <summary>
        /// Represents a single class (break) in the Jenks natural breaks algorithm,
        /// tracking statistical properties for optimization.
        /// </summary>
        private class JenksBreak
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="JenksBreak"/> class with default values.
            /// </summary>
            public JenksBreak()
            {
                Sum = 0.0;
                SumofSquares = 0.0;
                SquareDeviation = 0.0;
                StartIdx = -1;
                EndIdx = -1;
            }

            /// <summary>
            /// Gets or sets the starting index of this class in the sorted data array.
            /// </summary>
            public int StartIdx { get; set; }

            /// <summary>
            /// Gets or sets the ending index of this class in the sorted data array.
            /// </summary>
            public int EndIdx { get; set; }

            /// <summary>
            /// Gets or sets the average (mean) value of data points in this class.
            /// </summary>
            public double Average { get; set; }

            /// <summary>
            /// Gets or sets the variance of data points in this class.
            /// </summary>
            public double Variance { get; set; }

            /// <summary>
            /// Gets or sets the sum of squared deviations from the mean for this class.
            /// </summary>
            public double SquareDeviation { get; set; }

            /// <summary>
            /// Gets or sets the sum of squared values in this class.
            /// </summary>
            public double SumofSquares { get; set; }

            /// <summary>
            /// Gets or sets the sum of values in this class.
            /// </summary>
            public double Sum { get; set; }

            /// <summary>
            /// Gets or sets the count of data points in this class.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Recalculates the statistical properties (Count, Average, Variance, SquareDeviation)
            /// based on the current index range and accumulated sums.
            /// </summary>
            public void RefreshSquareDeviation()
            {
                Count = EndIdx - StartIdx + 1;

                if (Count <= 0)
                {
                    Average = 0.0;
                    Variance = 0.0;
                }
                else if (Count == 1)
                {
                    Average = Sum;
                    Variance = 0.0;
                }
                else
                {
                    Average = Sum / Count;
                    Variance = SumofSquares / Count - Math.Pow(Average, 2.0);
                    if (Variance < 0.0) Variance = 0.0;
                }

                SquareDeviation = Variance * Count;
            }
        }
    }
}
