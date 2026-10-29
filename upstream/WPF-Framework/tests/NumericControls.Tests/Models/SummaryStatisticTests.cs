using NumericControls.Distributions.Univariate;
using Xunit;

namespace NumericControls.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="SummaryStatistic"/>.
    /// </summary>
    public class SummaryStatisticTests
    {
        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly sets the StatName property.
        /// </summary>
        [Fact]
        public void Constructor_SetsStatName()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Assert
            Assert.Equal("Mean", stat.StatName);
        }

        /// <summary>
        /// Tests that the constructor correctly sets the DistStat property.
        /// </summary>
        [Fact]
        public void Constructor_SetsDistStat()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Assert
            Assert.Equal("100.0000", stat.DistStat);
        }

        /// <summary>
        /// Tests that the constructor correctly sets the DataStat property.
        /// </summary>
        [Fact]
        public void Constructor_SetsDataStat()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Assert
            Assert.Equal("98.5432", stat.DataStat);
        }

        /// <summary>
        /// Tests that the constructor correctly handles null values for all parameters.
        /// </summary>
        [Fact]
        public void Constructor_WithNullValues_SetsCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic(null, null, null);

            // Assert
            Assert.Null(stat.StatName);
            Assert.Null(stat.DistStat);
            Assert.Null(stat.DataStat);
        }

        /// <summary>
        /// Tests that the constructor correctly handles empty strings for all parameters.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyStrings_SetsCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("", "", "");

            // Assert
            Assert.Equal("", stat.StatName);
            Assert.Equal("", stat.DistStat);
            Assert.Equal("", stat.DataStat);
        }

        #endregion

        #region DistStat Property Tests

        /// <summary>
        /// Tests that setting the DistStat property updates its value correctly.
        /// </summary>
        [Fact]
        public void DistStat_SetValue_UpdatesCorrectly()
        {
            // Arrange
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Act
            stat.DistStat = "105.0000";

            // Assert
            Assert.Equal("105.0000", stat.DistStat);
        }

        /// <summary>
        /// Tests that the DistStat property can be set to null.
        /// </summary>
        [Fact]
        public void DistStat_SetToNull_UpdatesCorrectly()
        {
            // Arrange
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Act
            stat.DistStat = null;

            // Assert
            Assert.Null(stat.DistStat);
        }

        #endregion

        #region DataStat Property Tests

        /// <summary>
        /// Tests that setting the DataStat property updates its value correctly.
        /// </summary>
        [Fact]
        public void DataStat_SetValue_UpdatesCorrectly()
        {
            // Arrange
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Act
            stat.DataStat = "99.1234";

            // Assert
            Assert.Equal("99.1234", stat.DataStat);
        }

        /// <summary>
        /// Tests that the DataStat property can be set to null.
        /// </summary>
        [Fact]
        public void DataStat_SetToNull_UpdatesCorrectly()
        {
            // Arrange
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Act
            stat.DataStat = null;

            // Assert
            Assert.Null(stat.DataStat);
        }

        /// <summary>
        /// Tests that the DataStat property can be set to a dash to represent no data.
        /// </summary>
        [Fact]
        public void DataStat_SetToDash_RepresentsNoData()
        {
            // Arrange
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Act
            stat.DataStat = " - ";

            // Assert
            Assert.Equal(" - ", stat.DataStat);
        }

        #endregion

        #region Common Statistics Tests

        /// <summary>
        /// Tests that a minimum statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void MinimumStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Minimum", "45.2345", "42.1234");

            // Assert
            Assert.Equal("Minimum", stat.StatName);
        }

        /// <summary>
        /// Tests that a maximum statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void MaximumStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Maximum", "154.7655", "157.8766");

            // Assert
            Assert.Equal("Maximum", stat.StatName);
        }

        /// <summary>
        /// Tests that a mean statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void MeanStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mean", "100.0000", "98.5432");

            // Assert
            Assert.Equal("Mean", stat.StatName);
        }

        /// <summary>
        /// Tests that a mode statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void ModeStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mode", "100.0000", "99.8765");

            // Assert
            Assert.Equal("Mode", stat.StatName);
        }

        /// <summary>
        /// Tests that a standard deviation statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void StdDevStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Std Dev", "15.0000", "14.8765");

            // Assert
            Assert.Equal("Std Dev", stat.StatName);
        }

        /// <summary>
        /// Tests that a skewness statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void SkewnessStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Skewness", "0.0000", "0.1234");

            // Assert
            Assert.Equal("Skewness", stat.StatName);
        }

        /// <summary>
        /// Tests that a kurtosis statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void KurtosisStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Kurtosis", "3.0000", "2.9876");

            // Assert
            Assert.Equal("Kurtosis", stat.StatName);
        }

        #endregion

        #region Percentile Statistics Tests

        /// <summary>
        /// Tests that percentile statistics are created correctly with proper values.
        /// </summary>
        /// <param name="statName">The name of the percentile statistic.</param>
        /// <param name="distValue">The distribution statistic value.</param>
        /// <param name="dataValue">The data statistic value.</param>
        [Theory]
        [InlineData("5%", "75.3288", "76.1234")]
        [InlineData("25%", "89.8765", "88.5432")]
        [InlineData("50%", "100.0000", "99.5678")]
        [InlineData("75%", "110.1235", "111.4568")]
        [InlineData("95%", "124.6712", "122.8765")]
        public void PercentileStatistics_CreatedCorrectly(string statName, string distValue, string dataValue)
        {
            // Arrange & Act
            var stat = new SummaryStatistic(statName, distValue, dataValue);

            // Assert
            Assert.Equal(statName, stat.StatName);
            Assert.Equal(distValue, stat.DistStat);
            Assert.Equal(dataValue, stat.DataStat);
        }

        #endregion

        #region Goodness of Fit Statistics Tests

        /// <summary>
        /// Tests that an RMSE statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void RMSEStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("RMSE", "2.3456", "2.3456");

            // Assert
            Assert.Equal("RMSE", stat.StatName);
        }

        /// <summary>
        /// Tests that a Chi-Squared statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void ChiSquaredStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Chi-Squared", "12.3456", " - ");

            // Assert
            Assert.Equal("Chi-Squared", stat.StatName);
        }

        /// <summary>
        /// Tests that a Kolmogorov-Smirnov statistic is created correctly with proper stat name.
        /// </summary>
        [Fact]
        public void KSStatistic_CreatedCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("K-S", "0.0543", " - ");

            // Assert
            Assert.Equal("K-S", stat.StatName);
        }

        #endregion

        #region No Data Scenarios Tests

        /// <summary>
        /// Tests that a dash in DataStat indicates no data is available.
        /// </summary>
        [Fact]
        public void DataStatWithDash_IndicatesNoData()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Mean", "100.0000", " - ");

            // Assert
            Assert.Equal(" - ", stat.DataStat);
        }

        /// <summary>
        /// Tests that all statistics can have a no-data indicator (dash).
        /// </summary>
        [Fact]
        public void AllStatisticsCanHaveNoDataIndicator()
        {
            // Arrange & Act
            var stats = new[]
            {
                new SummaryStatistic("Minimum", "45.2345", " - "),
                new SummaryStatistic("Maximum", "154.7655", " - "),
                new SummaryStatistic("Mean", "100.0000", " - "),
                new SummaryStatistic("Std Dev", "15.0000", " - ")
            };

            // Assert
            foreach (var stat in stats)
            {
                Assert.Equal(" - ", stat.DataStat);
            }
        }

        #endregion

        #region Multiple Statistics Collection Tests

        /// <summary>
        /// Tests creating a collection of statistics for a normal distribution comparison.
        /// </summary>
        [Fact]
        public void CreateStatisticsCollection_ForNormalDistribution()
        {
            // Arrange & Act
            var stats = new List<SummaryStatistic>
            {
                new SummaryStatistic("Minimum", "45.2345", "42.1234"),
                new SummaryStatistic("Maximum", "154.7655", "157.8766"),
                new SummaryStatistic("Mean", "100.0000", "98.5432"),
                new SummaryStatistic("Mode", "100.0000", "99.8765"),
                new SummaryStatistic("Std Dev", "15.0000", "14.8765"),
                new SummaryStatistic("Skewness", "0.0000", "0.1234"),
                new SummaryStatistic("Kurtosis", "3.0000", "2.9876"),
                new SummaryStatistic("5%", "75.3288", "76.1234"),
                new SummaryStatistic("25%", "89.8765", "88.5432"),
                new SummaryStatistic("50%", "100.0000", "99.5678"),
                new SummaryStatistic("75%", "110.1235", "111.4568"),
                new SummaryStatistic("95%", "124.6712", "122.8765")
            };

            // Assert
            Assert.Equal(12, stats.Count);
            Assert.All(stats, s => Assert.NotNull(s.StatName));
            Assert.All(stats, s => Assert.NotNull(s.DistStat));
            Assert.All(stats, s => Assert.NotNull(s.DataStat));
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// Tests that a statistic with very long numeric values is handled correctly.
        /// </summary>
        [Fact]
        public void StatWithVeryLongValue_HandledCorrectly()
        {
            // Arrange
            var longValue = "123456789.123456789012345678901234567890";

            // Act
            var stat = new SummaryStatistic("Test", longValue, longValue);

            // Assert
            Assert.Equal(longValue, stat.DistStat);
            Assert.Equal(longValue, stat.DataStat);
        }

        /// <summary>
        /// Tests that a statistic with scientific notation values is handled correctly.
        /// </summary>
        [Fact]
        public void StatWithScientificNotation_HandledCorrectly()
        {
            // Arrange
            var sciNotation = "1.234E+10";

            // Act
            var stat = new SummaryStatistic("Large Value", sciNotation, sciNotation);

            // Assert
            Assert.Equal(sciNotation, stat.DistStat);
            Assert.Equal(sciNotation, stat.DataStat);
        }

        /// <summary>
        /// Tests that a statistic with negative values is handled correctly.
        /// </summary>
        [Fact]
        public void StatWithNegativeValues_HandledCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Skewness", "-0.5432", "-0.4321");

            // Assert
            Assert.Equal("-0.5432", stat.DistStat);
            Assert.Equal("-0.4321", stat.DataStat);
        }

        /// <summary>
        /// Tests that a statistic with zero values is handled correctly.
        /// </summary>
        [Fact]
        public void StatWithZeroValues_HandledCorrectly()
        {
            // Arrange & Act
            var stat = new SummaryStatistic("Skewness", "0.0000", "0.0000");

            // Assert
            Assert.Equal("0.0000", stat.DistStat);
            Assert.Equal("0.0000", stat.DataStat);
        }

        #endregion
    }
}
