using System.Collections.ObjectModel;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.RowItems
{
    /// <summary>
    /// Tests for <see cref="TimeSeriesRowItem"/>.
    /// </summary>
    public class TimeSeriesRowItemTests
    {
        /// <summary>
        /// Creates row items backed by a time series with the supplied interval.
        /// </summary>
        /// <param name="interval">The time interval for the series.</param>
        /// <param name="dates">The date-times for the row items.</param>
        /// <returns>The row item collection.</returns>
        private static ObservableCollection<object> CreateRows(TimeInterval interval, params DateTime[] dates)
        {
            var series = new TimeSeries(interval);
            for (int i = 0; i < dates.Length; i++)
            {
                series.Add(new SeriesOrdinate<DateTime, double>(dates[i], i + 1d));
            }

            var rows = new ObservableCollection<object>();
            for (int i = 0; i < series.Count; i++)
            {
                rows.Add(new TimeSeriesRowItem(rows, series[i], series, i));
            }

            return rows;
        }

        /// <summary>
        /// Gets a typed row from the row collection.
        /// </summary>
        /// <param name="rows">The row collection.</param>
        /// <param name="index">The row index.</param>
        /// <returns>The typed row item.</returns>
        private static TimeSeriesRowItem Row(ObservableCollection<object> rows, int index)
        {
            return (TimeSeriesRowItem)rows[index];
        }

        /// <summary>
        /// Verifies that ascending irregular date-times are valid.
        /// </summary>
        [Fact]
        public void DateTimeRule_IrregularAscendingDates_NoValidationError()
        {
            var start = new DateTime(2020, 1, 1);
            var rows = CreateRows(TimeInterval.Irregular, start, start.AddDays(1), start.AddDays(2));

            Row(rows, 0).ForceValidation();
            Row(rows, 1).ForceValidation();
            Row(rows, 2).ForceValidation();

            Assert.False(Row(rows, 0).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
            Assert.False(Row(rows, 1).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
            Assert.False(Row(rows, 2).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
        }

        /// <summary>
        /// Verifies that an earlier appended irregular date-time is invalid in backing data order.
        /// </summary>
        [Fact]
        public void DateTimeRule_IrregularEarlierDateAppended_HasValidationError()
        {
            var start = new DateTime(2020, 1, 1);
            var rows = CreateRows(TimeInterval.Irregular, start.AddDays(1), start.AddDays(2), start);

            Row(rows, 2).ForceValidation();

            Assert.True(Row(rows, 2).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
        }

        /// <summary>
        /// Verifies that duplicate irregular date-times are invalid.
        /// </summary>
        [Fact]
        public void DateTimeRule_IrregularDuplicateAdjacentDate_HasValidationError()
        {
            var start = new DateTime(2020, 1, 1);
            var rows = CreateRows(TimeInterval.Irregular, start, start);

            Row(rows, 1).ForceValidation();

            Assert.True(Row(rows, 1).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
        }

        /// <summary>
        /// Verifies that the date-time order rule is not applied to regular interval series.
        /// </summary>
        [Fact]
        public void DateTimeRule_RegularSeriesOutOfOrder_NoValidationError()
        {
            var start = new DateTime(2020, 1, 1);
            var rows = CreateRows(TimeInterval.OneDay, start.AddDays(1), start);

            Row(rows, 1).ForceValidation();

            Assert.False(Row(rows, 1).RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
        }

        /// <summary>
        /// Verifies ordering validation uses the stored series index without searching a parent row collection.
        /// </summary>
        [Fact]
        public void DateTimeRule_WithoutParentRowCollection_UsesSeriesIndex()
        {
            var start = new DateTime(2020, 1, 1);
            var series = new TimeSeries(TimeInterval.Irregular);
            series.Add(new SeriesOrdinate<DateTime, double>(start.AddDays(1), 1d));
            series.Add(new SeriesOrdinate<DateTime, double>(start, 2d));
            var row = new TimeSeriesRowItem(null, series[1], series, 1);

            row.ForceValidation();

            Assert.True(row.RuleMap[nameof(TimeSeriesRowItem.DateTime)].HasError);
        }
    }
}
