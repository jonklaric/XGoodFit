// --------------------------------------------------------------------------------------------------------------------
// XGoodFit addition (see the class remarks).
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// A live, read-only list view of a collection that notifies changes but isn't a list, which a
    /// <see cref="Series"/> takes as its ItemsSource.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF's ItemsControl takes any collection as its ItemsSource. Avalonia's needs a list
    /// when the collection notifies changes, as RMC-BestFit's data series do (an <c>IList&lt;Data&gt;</c>, not
    /// an <see cref="IList"/>), and throws otherwise. A series' items are only its data, which the OxyPlot
    /// series reads from the ItemsSource each time it updates, so the series shows such a collection through
    /// this view: it reads the collection afresh each time, and passes its notifications on.
    /// </remarks>
    internal sealed class NotifyingCollectionList : IList, INotifyCollectionChanged
    {
        /// <summary>The collection.</summary>
        private readonly IEnumerable source;

        /// <summary>The handlers of <see cref="CollectionChanged"/>.</summary>
        private NotifyCollectionChangedEventHandler handlers;

        /// <summary>Initializes a new instance of the <see cref="NotifyingCollectionList"/> class.</summary>
        /// <param name="source">The collection, which notifies changes.</param>
        internal NotifyingCollectionList(IEnumerable source)
        {
            this.source = source;
        }

        /// <summary>Occurs when the collection changes: its own notifications, passed on.</summary>
        /// <remarks>It follows the collection only while it has handlers, so it doesn't outlive its series.</remarks>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (this.handlers == null)
                {
                    ((INotifyCollectionChanged)this.source).CollectionChanged += this.OnSourceChanged;
                }

                this.handlers += value;
            }

            remove
            {
                this.handlers -= value;
                if (this.handlers == null)
                {
                    ((INotifyCollectionChanged)this.source).CollectionChanged -= this.OnSourceChanged;
                }
            }
        }

        /// <summary>Gets the collection.</summary>
        internal IEnumerable Source => this.source;

        /// <inheritdoc/>
        public int Count => this.source.Cast<object>().Count();

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <inheritdoc/>
        public bool IsFixedSize => false;

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public object SyncRoot => this;

        /// <inheritdoc/>
        public object this[int index]
        {
            get => this.source.Cast<object>().ElementAt(index);
            set => throw new NotSupportedException("The view is read-only.");
        }

        /// <inheritdoc/>
        public IEnumerator GetEnumerator() => this.source.GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(object value) => this.source.Cast<object>().Contains(value);

        /// <inheritdoc/>
        public int IndexOf(object value)
        {
            int index = 0;
            foreach (object item in this.source)
            {
                if (Equals(item, value))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            foreach (object item in this.source)
            {
                array.SetValue(item, index++);
            }
        }

        /// <inheritdoc/>
        public int Add(object value) => throw new NotSupportedException("The view is read-only.");

        /// <inheritdoc/>
        public void Clear() => throw new NotSupportedException("The view is read-only.");

        /// <inheritdoc/>
        public void Insert(int index, object value) => throw new NotSupportedException("The view is read-only.");

        /// <inheritdoc/>
        public void Remove(object value) => throw new NotSupportedException("The view is read-only.");

        /// <inheritdoc/>
        public void RemoveAt(int index) => throw new NotSupportedException("The view is read-only.");

        /// <summary>Passes the collection's notification on.</summary>
        /// <param name="sender">The collection.</param>
        /// <param name="e">The notification.</param>
        private void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e) => this.handlers?.Invoke(this, e);
    }
}
