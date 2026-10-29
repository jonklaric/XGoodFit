using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NumericControls.Distributions.Multivariate
{
    /// <summary>
    /// Represents a bivariate empirical distribution for modeling paired data with cumulative distribution functions.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class BivariateEmpirical : INotifyPropertyChanged
    {

        private BivariateEmpirical _bivariateEmpiricalCDF;

        /// <summary>
        /// Gets or sets the bivariate empirical cumulative distribution function.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public BivariateEmpirical BivariateEmpiricalCDF
        {
            get { return _bivariateEmpiricalCDF; }
            set 
            {
                _bivariateEmpiricalCDF = value;
                RaisePropertyChanged(nameof(BivariateEmpiricalCDF));
            }
        }

        /// <summary>
        /// Gets or sets the array of X1 values (distribution 1). Points on the cumulative curve are specified
        /// with increasing value and increasing probability.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public ObservableCollection<double> X1Values { get; set; }

        /// <summary>
        /// Gets or sets the array of X2 values (distribution 2). Points on the cumulative curve are specified
        /// with increasing value and increasing probability.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public ObservableCollection<double> X2Values { get; set; }


        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
