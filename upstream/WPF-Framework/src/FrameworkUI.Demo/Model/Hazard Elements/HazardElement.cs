using DatabaseManager;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using FrameworkInterfaces.Undo;
using FrameworkInterfaces.Undo.Actions;
using Numerics.Distributions;
using Numerics.Data;
using OxyPlot.Wpf;
using OxyPlot.Wpf.Serialization;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace FrameworkUI.Demo
{
    /// <summary>
    /// Represents a hazard element in the demo project. This is an example project element for testing purposes.
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
    public class HazardElement : ElementBase, IUndoableElement
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="HazardElement"/> class.
        /// </summary>
        /// <param name="name">The name of the hazard element.</param>
        /// <param name="parentCollection">The parent collection that contains this element.</param>
        /// <param name="openFromFile">Optional parameter to open the function from disk upon construction.</param>
        public HazardElement(string name, IElementCollection parentCollection, bool openFromFile = false) : base(name, parentCollection)
        {
            IsUndoEnabled = false;
            try
            {
                Name = name;
                _nameOnDisk = name;
                _creationDate = DateTime.Now;
                _lastModified = DateTime.Now;

                // The default constructor seeds the standard 25 probability ordinates.
                _probabilityOrdinates.CollectionChanged += ProbabilityOrdinates_CollectionChanged;

                // Create the element-owned frequency plot. The plot object lives on the
                // element so its visual state can be tracked for undo and persisted.
                _frequencyPlot = CreateDefaultFrequencyPlot();

                InitializeMessages();

                if (openFromFile)
                {
                    Open();
                    _nameValid = ValidateName(DemoProject.InvalidNameCharacters, 50, "PHF");
                }
                else
                {
                    _nameValid = ValidateName(DemoProject.InvalidNameCharacters, 50, "PHF");
                    _messenger.Add(_descriptionMsg);
                    _messenger.Add(_estimatedMsg);
                }

                SetElementValidation();
                SetIsDirty(false);
            }
            finally
            {
                // Open() creates the bridges itself; only create them here when
                // constructing a fresh element.
                if (!openFromFile) SetupBridges();
                IsUndoEnabled = true;
                ClearUndoHistory();
            }
        }

        /// <summary>
        /// Initializes the message items for validation and warnings.
        /// </summary>
        private void InitializeMessages()
        {
            _descriptionMsg = new BasicMessageItem(MessageType.Message, "The parametric hazard function does not have a description.",
                this, ParentCollection.Name, Name, nameof(Description), "PHF-MSG-001");

            _lowRealizationsWarning = new BasicMessageItem(MessageType.Warning, "The number of realizations is less than 1,000. The accuracy of the confidence intervals and mean curve will be diminished.",
                this, ParentCollection.Name, Name, nameof(Realizations), "PHF-WNG-001");

            _parentDistMsg = new BasicMessageItem(MessageType.Error, "The parent distribution parameters are invalid.",
                this, ParentCollection.Name, Name, nameof(ParentDistribution), "PHF-ERR-007");

            _recordLengthRangeMsg = new BasicMessageItem(MessageType.Error, "The effective record length must be between 10 and 10,000.",
                this, ParentCollection.Name, Name, nameof(EffectiveRecordLength), "PHF-ERR-008");

            _noConfidenceIntervalMsg = new BasicMessageItem(MessageType.Error, "The confidence interval width cannot be nothing.",
                this, ParentCollection.Name, Name, nameof(ConfidenceIntervalWidth), "PHF-ERR-009");

            _badConfidenceIntervalMsg = new BasicMessageItem(MessageType.Error, "The confidence interval width must be between 0 and 1.",
                this, ParentCollection.Name, Name, nameof(ConfidenceIntervalWidth), "PHF-ERR-010");

            _badSeedMsg = new BasicMessageItem(MessageType.Error, "The PRNG seed must be greater than 0.",
                this, ParentCollection.Name, Name, nameof(PRNGSeed), "PHF-ERR-011");

            _badRealizationsMsg = new BasicMessageItem(MessageType.Error, "The number of realizations must be between 100 and 100,000.",
                this, ParentCollection.Name, Name, nameof(Realizations), "PHF-ERR-012");

            _noOrdinatesMsg = new BasicMessageItem(MessageType.Error, "There must be at least one probability ordinate.",
                this, ParentCollection.Name, Name, nameof(ProbabilityOrdinates), "PHF-ERR-013");

            _badOrdinatesMsg = new BasicMessageItem(MessageType.Error, "All probability values must be between 0 and 1. Please resolve all of the errors in the probability ordinate table.",
                this, ParentCollection.Name, Name, nameof(ProbabilityOrdinates), "PHF-ERR-014");

            _unorderedOrdinatesMsg = new BasicMessageItem(MessageType.Error, "The probability values must be in ascending order. Please resolve all of the errors in the probability ordinate table.",
                this, ParentCollection.Name, Name, nameof(ProbabilityOrdinates), "PHF-ERR-015");

            _badSimulationMsg = new BasicMessageItem(MessageType.Error, "There were errors during the bootstrap simulation. Try adjusting the input parameters or changing the parameter estimation method.",
                this, ParentCollection.Name, Name, nameof(Estimate), "PHF-ERR-016");

            _estimatedMsg = new BasicMessageItem(MessageType.Error, "The parametric hazard function has not been estimated.",
                this, ParentCollection.Name, Name, nameof(IsEstimated), "PHF-ERR-017");

            _pmomMsg = new BasicMessageItem(MessageType.Error, "The selected distribution cannot be estimated with product moments.",
                this, ParentCollection.Name, Name, nameof(EstimationMethod), "PHF-ERR-018");

            _lmomMsg = new BasicMessageItem(MessageType.Error, "The selected distribution cannot be estimated with linear moments.",
                this, ParentCollection.Name, Name, nameof(EstimationMethod), "PHF-ERR-019");

            // Add all messages to the local list for easy access.
            _messages.AddRange(new[] {
                _descriptionMsg,
                _estimatedMsg,
                _lowRealizationsWarning,
                _parentDistMsg,
                _recordLengthRangeMsg,
                _noConfidenceIntervalMsg,
                _badConfidenceIntervalMsg,
                _badSeedMsg,
                _badRealizationsMsg,
                _noOrdinatesMsg,
                _badOrdinatesMsg,
                _unorderedOrdinatesMsg,
                _badSimulationMsg,
                _pmomMsg,
                _lmomMsg
            });
        }

        #endregion

        #region Members

        #region IMetaData Properties

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Name"), Description("The name of this parametric hazard function."), Browsable(true)]
        public override string Name
        {
            get => NameField;
            set
            {
                if (NameField != value)
                {
                    var oldValue = NameField;
                    NameField = value;

                    // Reset messages with new name
                    foreach (var item in _messages)
                    {
                        item.SourceName = value;
                    }

                    _nameValid = ValidateName(DemoProject.InvalidNameCharacters, 50, "PHF");
                    SetElementValidation();
                    RecordPropertyChange(nameof(Name), oldValue, value);
                }
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Description"), Description("The description of this hazard function."), Browsable(true)]
        public override string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    var oldValue = _description;
                    _description = value;
                    if (string.IsNullOrEmpty(_description))
                    {
                        _messenger.Add(_descriptionMsg);
                    }
                    else
                    {
                        _messenger.Remove(_descriptionMsg);
                    }
                    RecordPropertyChange(nameof(Description), oldValue, value);
                }
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Creation Date"), Description("The date and time this hazard function was first created."), Browsable(true)]
        public override DateTime CreationDate => _creationDate;

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Last Edited"), Description("The date and time this hazard function was last modified."), Browsable(true)]
        public override DateTime LastModified => _lastModified;

        #endregion

        #region IElement Properties

        /// <inheritdoc/>
        public override ImageSource ElementImage =>
            System.Windows.Application.Current?.TryFindResource(ElementImageResourceKey) as ImageSource;

        /// <summary>
        /// Gets the resource key for the theme-aware element icon. The framework's project
        /// explorer and window menus bind to this key with SetResourceReference so the
        /// icon re-resolves when the theme changes.
        /// </summary>
        public string ElementImageResourceKey => "HazardElementIcon";

        /// <inheritdoc/>
        public override bool CanCopyFromExternal => true;

        /// <inheritdoc/>
        public override string NameOnDisk => _nameOnDisk;

        /// <inheritdoc/>
        public override bool IsValid => _isValid;

        #endregion

        #region Properties and Events

        private UnivariateDistributionBase _parentDistribution = new LogPearsonTypeIII();
        private int _effectiveRecordLength = 100;
        private double _confidenceIntervalWidth = 0.9;
        private int _realizations = 10000;
        private int _prngSeed = 12345;
        private ParameterEstimationMethod _estimationMethod = ParameterEstimationMethod.MethodOfMoments;
        private ProbabilityOrdinates _probabilityOrdinates = new ProbabilityOrdinates();
        private UndoableCollectionBridge<double> _ordinatesBridge;
        private Plot _frequencyPlot;
        private PlotUndoManager _frequencyPlotUndo;
        private XElement _distributionSnapshot;
        private bool _isEstimated = false;
        private bool _minmaxComputed = false;
        private double[] _minmax = new double[2];
        private bool _isUncertain = true;

        private bool _nameValid = false;
        private bool _distributionValid = true;
        private bool _recordLengthValid = true;
        private bool _confidenceIntervalValid = true;
        private bool _realizationsValid = true;
        private bool _ordinatesValid = true;
        private bool _estimationMethodValid = true;

        private List<BasicMessageItem> _messages = new List<BasicMessageItem>();
        private Messenger _messenger = Messenger.GetInstance();

        private BasicMessageItem _descriptionMsg;
        private BasicMessageItem _lowRealizationsWarning;
        private BasicMessageItem _parentDistMsg;
        private BasicMessageItem _recordLengthRangeMsg;
        private BasicMessageItem _noConfidenceIntervalMsg;
        private BasicMessageItem _badConfidenceIntervalMsg;
        private BasicMessageItem _badSeedMsg;
        private BasicMessageItem _badRealizationsMsg;
        private BasicMessageItem _noOrdinatesMsg;
        private BasicMessageItem _badOrdinatesMsg;
        private BasicMessageItem _unorderedOrdinatesMsg;
        private BasicMessageItem _badSimulationMsg;
        private BasicMessageItem _estimatedMsg;
        private BasicMessageItem _pmomMsg;
        private BasicMessageItem _lmomMsg;

        /// <summary>
        /// Univariate continuous probability distribution used to model the population.
        /// </summary>
        [Category("Inputs"), DisplayName("Parent Distribution"), Description("The continuous distribution describing the population."), Browsable(true)]
        public UnivariateDistributionBase ParentDistribution
        {
            get => _parentDistribution;
            set
            {
                if (value == null) return;

                var oldSnapshot = _parentDistribution?.ToXElement();
                var incomingSnapshot = value.ToXElement();
                if (oldSnapshot != null && XNode.DeepEquals(oldSnapshot, incomingSnapshot)) return;

                _parentDistribution = value.Clone();
                IsEstimated = false;
                ClearResults();
                ValidateParentDistribution();
                ValidateEstimationMethod();
                RecordDistributionUndo(nameof(ParentDistribution), oldSnapshot, _parentDistribution.ToXElement());
                _distributionSnapshot = _parentDistribution.ToXElement();
                RaisePropertyChange(nameof(ParentDistribution), setDirty: false);
            }
        }

        /// <summary>
        /// Determines if the distribution has uncertainty.
        /// </summary>
        [Category("Parameter"), DisplayName("Uncertainty On"), Description("Determines if the parametric hazard function includes uncertainty defined by the effective record length (ERL)."), Browsable(true)]
        public bool IsUncertain
        {
            get => _isUncertain;
            set
            {
                if (_isUncertain != value)
                {
                    var oldValue = _isUncertain;
                    _isUncertain = value;
                    IsEstimated = false;
                    ClearResults();
                    ValidateEstimationMethod();
                    RecordPropertyChange(nameof(IsUncertain), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the effective record length.
        /// </summary>
        [Category("Inputs"), DisplayName("Effective Record Length"), Description("The Effective Record Length (ERL) is used to estimate the uncertainty in the parent distribution. A higher ERL indicates more data coverage, resulting in reduced uncertainty and narrower confidence intervals."), Browsable(true)]
        public int EffectiveRecordLength
        {
            get => _effectiveRecordLength;
            set
            {
                if (_effectiveRecordLength != value)
                {
                    var oldValue = _effectiveRecordLength;
                    _effectiveRecordLength = value;

                    if (_effectiveRecordLength < 10 || _effectiveRecordLength > 10000)
                    {
                        _recordLengthValid = false;
                        _messenger.Add(_recordLengthRangeMsg);
                    }
                    else
                    {
                        _recordLengthValid = true;
                        _messenger.Remove(_recordLengthRangeMsg);
                    }
                    IsEstimated = false;
                    ClearResults();
                    SetElementValidation();
                    RecordPropertyChange(nameof(EffectiveRecordLength), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the width of the confidence intervals.
        /// </summary>
        [Category("Output Options"), DisplayName("Confidence Interval Width"), Description("Sets the width of the confidence interval. For example, a 90% confidence interval means the value of interest falls within this range with a 90% probability. To ensure accuracy, it is recommended to compute at least 10,000 realizations for a 90% confidence interval."), Browsable(true)]
        public double ConfidenceIntervalWidth
        {
            get => _confidenceIntervalWidth;
            set
            {
                if (_confidenceIntervalWidth != value)
                {   
                    var oldValue = _confidenceIntervalWidth;
                    _confidenceIntervalWidth = value;

                    _confidenceIntervalValid = true;
                    _messenger.Remove(_noConfidenceIntervalMsg);
                    _messenger.Remove(_badConfidenceIntervalMsg);
                    if (double.IsNaN(_confidenceIntervalWidth))
                    {
                        _confidenceIntervalValid = false;
                        _messenger.Add(_noConfidenceIntervalMsg);
                    }
                    if (_confidenceIntervalWidth <= 0 || _confidenceIntervalWidth >= 1)
                    {
                        _confidenceIntervalValid = false;
                        _messenger.Add(_badConfidenceIntervalMsg);
                    }

                    IsEstimated = false;
                    ClearResults();
                    SetElementValidation();
                    RecordPropertyChange(nameof(ConfidenceIntervalWidth), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the number of bootstrap realizations.
        /// </summary>
        [Category("Simulation Options"), DisplayName("Realizations"), Description("The number of bootstrap realizations used in the analysis. For accurate results, it is recommended to compute at least 10,000 realizations to ensure a reliable 90% confidence interval."), Browsable(true)]
        public int Realizations
        {
            get => _realizations;
            set
            {
                if (_realizations != value)
                {   
                    var oldValue = _realizations;
                    _realizations = value;

                    if (_realizations < 100 || _realizations > 100000)
                    {
                        _realizationsValid = false;
                        _messenger.Add(_badRealizationsMsg);
                    }
                    else
                    {
                        _realizationsValid = true;
                        _messenger.Remove(_badRealizationsMsg);
                    }
                    if (_realizations < 1000)
                    {
                        _messenger.Add(_lowRealizationsWarning);
                    }
                    else
                    {
                        _messenger.Remove(_lowRealizationsWarning);
                    }

                    IsEstimated = false;
                    ClearResults();
                    SetElementValidation();
                    RecordPropertyChange(nameof(Realizations), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the pseudo random number generator (PRNG) seed.
        /// </summary>
        [Category("Simulation Options"), DisplayName("PRNG Seed"), Description("The seed for the pseudo-random number generator (PRNG) used in the bootstrap simulation. Setting a seed ensures repeatability of results."), Browsable(true), DefaultValue(12345)]
        public int PRNGSeed
        {
            get => _prngSeed;
            set
            {
                if (_prngSeed != value)
                {
                    var oldValue = _prngSeed;
                    _prngSeed = value;

                    if (_prngSeed <= 0)
                    {
                        _messenger.Add(_badSeedMsg);
                    }
                    else
                    {
                        _messenger.Remove(_badSeedMsg);
                    }

                    IsEstimated = false;
                    ClearResults();
                    SetElementValidation();
                    RecordPropertyChange(nameof(PRNGSeed), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets and sets the parameter estimation method.
        /// </summary>
        [Category("Simulation Options"), DisplayName("Estimation Method"), Description("The parameter estimation technique used in the bootstrap uncertainty analysis."), Browsable(true)]
        public ParameterEstimationMethod EstimationMethod
        {
            get => _estimationMethod;
            set
            {
                if (_estimationMethod != value)
                {
                    var oldValue = _estimationMethod;
                    _estimationMethod = value;
                    IsEstimated = false;
                    ClearResults();
                    ValidateEstimationMethod();
                    RecordPropertyChange(nameof(EstimationMethod), oldValue, value);
                }
            }
        }

        /// <summary>
        /// Gets the exceedance probability values used for plotting the distribution.
        /// The collection is mutated in place (e.g., via the ordinates table or
        /// <see cref="Numerics.Data.ProbabilityOrdinates.FromDelimitedString(string, string)"/>);
        /// edits are recorded for undo through the collection bridge.
        /// </summary>
        public ProbabilityOrdinates ProbabilityOrdinates => _probabilityOrdinates;

        /// <summary>
        /// Gets the element-owned frequency plot. The view hosts this plot directly, so
        /// user styling changes are recorded for undo through the plot undo manager and
        /// persisted with the element.
        /// </summary>
        public Plot FrequencyPlot => _frequencyPlot;

        /// <summary>
        /// Determines whether the distribution has been bootstrapped.
        /// </summary>
        public bool IsEstimated
        {
            get => _isEstimated;
            private set
            {
                if (_isEstimated != value)
                {
                    _isEstimated = value;

                    _messenger.Remove(_estimatedMsg);
                    if (!_isEstimated)
                    {
                        _messenger.Add(_estimatedMsg);
                    }

                    SetElementValidation();
                    RaisePropertyChange(nameof(IsEstimated));
                }
            }
        }

        /// <summary>
        /// The parametric bootstrap results. 
        /// </summary>
        public UncertaintyAnalysisResults Results { get; private set; }

        /// <summary>
        /// Determines if the function is deterministic or if it has uncertainty.
        /// </summary>
        public bool IsDeterministic => !IsUncertain;

        #endregion

        #endregion

        #region Methods

        #region IElement Methods

        /// <inheritdoc/>
        public override IElement Copy(string newName = null)
        {
            var element = new HazardElement(newName ?? Name, ParentCollection);

            // Disable undo recording while copying data
            element.IsUndoEnabled = false;
            try
            {
                element.Description = Description;
                element.ParentDistribution = ParentDistribution.Clone();
                element.IsUncertain = IsUncertain;
                element.EffectiveRecordLength = EffectiveRecordLength;
                element.ConfidenceIntervalWidth = ConfidenceIntervalWidth;
                element.Realizations = Realizations;
                element.PRNGSeed = PRNGSeed;
                element.EstimationMethod = EstimationMethod;

                element._probabilityOrdinates.Clear();
                element._probabilityOrdinates.AddRange(ProbabilityOrdinates);
                element._isEstimated = false;

                // Copy the plot visual state via an XElement round-trip.
                PlotSerializer.FromXElement(element._frequencyPlot, PlotSerializer.ToXElement(_frequencyPlot));
            }
            finally
            {
                element.SetupBridges();
                element.IsUndoEnabled = true;
                element.ClearUndoHistory();
            }

            return element;
        }

        /// <inheritdoc/>
        public override IElement CopyFromExternal(string itemName, string fullFileName)
        {
            // Create SQLite connection to the external project file
            var sqlite = new SQLiteManager(fullFileName);
            var element = new HazardElement(itemName, ParentCollection);
            element.Open(sqlite);
            return element;
        }

        /// <inheritdoc/>
        public override void Delete()
        {
            if (Name == null) return;
            DisposeBridges();
            SetIsDirty(false);

            var sqlite = new SQLiteManager(ParentCollection.ParentProject.FullFileName);
            sqlite.Open();
            try
            {
                if (sqlite.TableNames.Contains(ParentCollection.Name))
                {
                    var dtView = sqlite.GetTableManager(ParentCollection.Name);
                    int rowIndex = dtView.SearchColumn(0, dtView.NumberOfRows - 1, "Name", NameOnDisk, true, true);
                    if (rowIndex >= 0 && rowIndex < dtView.NumberOfRows) dtView.DeleteRow(rowIndex);
                    dtView.ApplyEdits();
                }
            }
            finally
            {
                if (sqlite.DataBaseOpen) sqlite.Close();
            }

            _messenger.Clear(this);
            _undoManager.Clear();
            RaiseDeleted(this);
        }

        /// <inheritdoc/>
        public override void Open()
        {
            Open(new SQLiteManager(ParentCollection.ParentProject.FullFileName));
        }

        /// <summary>
        /// Opens the element from disk using the specified SQLite manager.
        /// </summary>
        /// <param name="sqlite">The SQLite manager to use for opening the element.</param>
        public void Open(SQLiteManager sqlite)
        {
            var wasUndoEnabled = IsUndoEnabled;
            IsUndoEnabled = false;
            try
            {
                _messenger.Clear(this);

                var wasOpen = sqlite.DataBaseOpen;
                if (wasOpen == false) sqlite.Open();

                var dtView = sqlite.GetTableManager(ParentCollection.Name);
                int rowIndex = dtView.SearchColumn(0, dtView.NumberOfRows - 1, "Name", NameOnDisk, true, true);
                if (rowIndex != -1)
                {
                    // Read scalar meta data using backing fields to avoid undo recording.
                    if (dtView.ColumnNames.Contains(nameof(Name)))
                    {
                        _name = dtView.GetCell(nameof(Name), rowIndex).ToString();
                        foreach (var item in _messages) item.SourceName = _name;
                        _nameValid = ValidateName(DemoProject.InvalidNameCharacters, 50, "PHF");
                    }
                    if (dtView.ColumnNames.Contains(nameof(Description)))
                    {
                        _description = dtView.GetCell(nameof(Description), rowIndex).ToString();
                        if (string.IsNullOrEmpty(_description))
                            _messenger.Add(_descriptionMsg);
                        else
                            _messenger.Remove(_descriptionMsg);
                    }
                    if (dtView.ColumnNames.Contains(nameof(CreationDate))) _creationDate = FrameworkInterfaces.Utilities.Tools.DateFromString(dtView.GetCell(nameof(CreationDate), rowIndex).ToString()) ?? DateTime.MinValue;
                    if (dtView.ColumnNames.Contains(nameof(LastModified))) _lastModified = FrameworkInterfaces.Utilities.Tools.DateFromString(dtView.GetCell(nameof(LastModified), rowIndex).ToString()) ?? DateTime.MinValue;

                    // Load the probability ordinates before the results so the collection
                    // changed handler (which clears results) cannot wipe restored output.
                    if (dtView.ColumnNames.Contains(nameof(ProbabilityOrdinates)))
                    {
                        ProbabilityOrdinates.FromDelimitedString(
                            dtView.GetCell(nameof(ProbabilityOrdinates), rowIndex).ToString(), "|");
                    }

                    // Parent distribution from XML, with a try/catch guard for legacy or
                    // corrupted files.
                    if (dtView.ColumnNames.Contains(nameof(ParentDistribution)))
                    {
                        try
                        {
                            var distXml = dtView.GetCell(nameof(ParentDistribution), rowIndex).ToString();
                            if (!string.IsNullOrEmpty(distXml))
                                _parentDistribution = UnivariateDistributionFactory.CreateDistribution(XElement.Parse(distXml));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not deserialize the parent distribution for '{Name}': {ex.Message}");
                        }
                    }
                    _distributionValid = _parentDistribution != null && _parentDistribution.ParametersValid;
                    if (!_distributionValid) _messenger.Add(_parentDistMsg);

                    // Scalar simulation inputs. Parse into temporaries so a value that
                    // fails to parse leaves the element's default in place.
                    if (dtView.ColumnNames.Contains(nameof(IsUncertain)) && bool.TryParse(dtView.GetCell(nameof(IsUncertain), rowIndex).ToString(), out bool isUncertain)) _isUncertain = isUncertain;
                    if (dtView.ColumnNames.Contains(nameof(EffectiveRecordLength)) && int.TryParse(dtView.GetCell(nameof(EffectiveRecordLength), rowIndex).ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int erl)) _effectiveRecordLength = erl;
                    if (dtView.ColumnNames.Contains(nameof(ConfidenceIntervalWidth)) && double.TryParse(dtView.GetCell(nameof(ConfidenceIntervalWidth), rowIndex).ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ciWidth)) _confidenceIntervalWidth = ciWidth;
                    if (dtView.ColumnNames.Contains(nameof(Realizations)) && int.TryParse(dtView.GetCell(nameof(Realizations), rowIndex).ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int realizations)) _realizations = realizations;
                    if (dtView.ColumnNames.Contains(nameof(PRNGSeed)) && int.TryParse(dtView.GetCell(nameof(PRNGSeed), rowIndex).ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int seed)) _prngSeed = seed;
                    if (dtView.ColumnNames.Contains(nameof(EstimationMethod)) && Enum.TryParse(dtView.GetCell(nameof(EstimationMethod), rowIndex).ToString(), out ParameterEstimationMethod method)) _estimationMethod = method;
                    if (dtView.ColumnNames.Contains(nameof(IsEstimated)) && bool.TryParse(dtView.GetCell(nameof(IsEstimated), rowIndex).ToString(), out bool isEstimated)) _isEstimated = isEstimated;

                    // Bootstrap results from a compressed byte array BLOB.
                    if (dtView.ColumnNames.Contains(nameof(Results)))
                    {
                        try
                        {
                            if (dtView.GetCell(nameof(Results), rowIndex) is byte[] bytes && bytes.Length > 0)
                                Results = ResultsFromByteArray(Numerics.Tools.Decompress(bytes));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not deserialize the results for '{Name}': {ex.Message}");
                            Results = null;
                        }
                    }

                    // Check for corrupted results.
                    if (_isEstimated && _isUncertain && (Results == null || Results.ParameterSets == null || Results.ParameterSets.Length == 0))
                    {
                        _isEstimated = false;
                        Results = new UncertaintyAnalysisResults();
                    }
                    if (Results == null) Results = new UncertaintyAnalysisResults();
                    if (!_isEstimated) _messenger.Add(_estimatedMsg);

                    // Deserialize the element-owned frequency plot.
                    if (dtView.ColumnNames.Contains("FrequencyPlotSettings"))
                    {
                        var plotXml = dtView.GetCell("FrequencyPlotSettings", rowIndex).ToString();
                        if (!string.IsNullOrEmpty(plotXml))
                        {
                            try
                            {
                                PlotSerializer.FromXElement(_frequencyPlot, XElement.Parse(plotXml));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Could not deserialize the frequency plot for '{Name}': {ex.Message}");
                            }
                        }
                    }
                }

                if (wasOpen == false) sqlite.Close();
                SetupBridges();

                ValidateEstimationMethod();
                SetElementValidation();
                SetIsDirty(false);

                // Notify bindings for properties loaded via backing fields.
                RaisePropertyChange(nameof(Name), setDirty: false);
                RaisePropertyChange(nameof(ProbabilityOrdinates), setDirty: false);
                RaisePropertyChange(nameof(IsEstimated), setDirty: false);
            }
            finally
            {
                IsUndoEnabled = wasUndoEnabled;
                if (wasUndoEnabled) ClearUndoHistory();
            }
        }

        /// <summary>
        /// Raises the PreviewObjectSaved event before saving the element.
        /// </summary>
        /// <param name="cancel">Output parameter that determines if the save operation should be canceled.</param>
        public void RaisePreviewSaved(ref bool cancel)
        {
            RaisePreviewObjectSaved(this, ref cancel);
        }

        /// <inheritdoc/>
        public override void Save()
        {
            // Preview save
            bool cancel = false;
            RaisePreviewObjectSaved(this, ref cancel);
            if (cancel) return;

            // Create SQLite connection
            var sqlite = new SQLiteManager(ParentCollection.ParentProject.FullFileName);
            sqlite.Open();
            DateTime previousLastModified = _lastModified;
            bool committed = false;
            try
            {
                // Only update last edited if user data actually changed
                if (IsDirty)
                {
                    _lastModified = DateTime.Now;
                    RaisePropertyChange(nameof(LastModified));
                }

                // Create the element collection table if it doesn't exist.
                CreateTable(sqlite);

                var dtView = sqlite.GetTableManager(ParentCollection.Name);
                int rowIndex = dtView.SearchColumn(0, dtView.NumberOfRows - 1, "Name", NameOnDisk, true, true);
                if (rowIndex < 0 || rowIndex >= dtView.NumberOfRows)
                {
                    dtView.AddRow();
                    rowIndex = dtView.NumberOfRows - 1;
                }

                dtView.EditCell(rowIndex, nameof(Name), Name);
                dtView.EditCell(rowIndex, nameof(Description), Description);
                dtView.EditCell(rowIndex, nameof(CreationDate), FrameworkInterfaces.Utilities.Tools.DateToUniversalString(CreationDate));
                dtView.EditCell(rowIndex, nameof(LastModified), FrameworkInterfaces.Utilities.Tools.DateToUniversalString(LastModified));
                dtView.EditCell(rowIndex, nameof(ParentDistribution), ParentDistribution.ToXElement().ToString());
                dtView.EditCell(rowIndex, nameof(IsUncertain), IsUncertain);
                dtView.EditCell(rowIndex, nameof(EffectiveRecordLength), EffectiveRecordLength);
                dtView.EditCell(rowIndex, nameof(ConfidenceIntervalWidth), ConfidenceIntervalWidth.ToString("G17", CultureInfo.InvariantCulture));
                dtView.EditCell(rowIndex, nameof(Realizations), Realizations);
                dtView.EditCell(rowIndex, nameof(PRNGSeed), PRNGSeed);
                dtView.EditCell(rowIndex, nameof(EstimationMethod), EstimationMethod.ToString());
                dtView.EditCell(rowIndex, nameof(ProbabilityOrdinates), ProbabilityOrdinates?.ToDelimitedString("|") ?? "");
                dtView.EditCell(rowIndex, nameof(IsEstimated), IsEstimated);
                dtView.EditCell(rowIndex, nameof(Results), Numerics.Tools.Compress(ResultsToByteArray()));
                dtView.EditCell(rowIndex, "FrequencyPlotSettings", _frequencyPlot != null ? PlotSerializer.ToXElement(_frequencyPlot).ToString() : "");

                dtView.ApplyEdits();
                sqlite.Close();
                committed = true;
            }
            finally
            {
                if (sqlite.DataBaseOpen) sqlite.Close();
                if (!committed && _lastModified != previousLastModified)
                {
                    _lastModified = previousLastModified;
                    RaisePropertyChange(nameof(LastModified));
                }
            }

            // Only mark clean / fire ObjectSaved when the commit actually succeeded.
            if (committed)
            {
                SetIsDirty(false);
                MarkUndoSavePoint();
                _nameOnDisk = Name;
                RaiseObjectSaved(this);
            }
        }

        #endregion

        #region SQLite Persistence

        /// <summary>
        /// The required columns for the SQLite table.
        /// If you want to add a new column, add it to the end of the dictionary.
        /// </summary>
        private static Dictionary<string, Type> RequiredColumns { get; } = new Dictionary<string, Type>() {
            { nameof(Name), typeof(string) },
            { nameof(Description), typeof(string) },
            { nameof(CreationDate), typeof(string) },
            { nameof(LastModified), typeof(string) },
            { nameof(ParentDistribution), typeof(string) },
            { nameof(IsUncertain), typeof(bool) },
            { nameof(EffectiveRecordLength), typeof(int) },
            { nameof(ConfidenceIntervalWidth), typeof(string) },
            { nameof(Realizations), typeof(int) },
            { nameof(PRNGSeed), typeof(int) },
            { nameof(EstimationMethod), typeof(string) },
            { nameof(ProbabilityOrdinates), typeof(string) },
            { nameof(IsEstimated), typeof(bool) },
            { nameof(Results), typeof(byte[]) },
            { "FrequencyPlotSettings", typeof(string) } };

        /// <summary>
        /// Creates or updates the SQLite database table for storing hazard elements.
        /// </summary>
        /// <param name="sqlite">The SQLite database manager instance.</param>
        internal void CreateTable(SQLiteManager sqlite)
        {
            if (sqlite.TableNames.Contains(ParentCollection.Name) == false)
            {
                // If the table does not exist, then create the table
                var dataTable = new DataTable(ParentCollection.Name);
                foreach (KeyValuePair<string, Type> column in RequiredColumns)
                    dataTable.Columns.Add(column.Key, column.Value);
                sqlite.SaveDataTable(dataTable);
            }
            else
            {
                // Add any required columns that don't exist
                var dt = sqlite.GetTableManager(ParentCollection.Name);
                int columnIndex;
                foreach (KeyValuePair<string, Type> column in RequiredColumns)
                {
                    columnIndex = Array.IndexOf(dt.ColumnNames, column.Key);
                    // If the column doesn't exist in the database then create it.
                    if (columnIndex < 0)
                    {
                        dt.AddColumn(column.Key, column.Value);
                    }
                    else
                    {
                        if (dt.ColumnTypes[columnIndex] != column.Value)
                        {
                            dt.DeleteColumn(columnIndex);
                            dt.AddColumn(column.Key, column.Value);
                        }
                    }
                }
                dt.ApplyEdits();
            }
        }

        /// <summary>
        /// Converts the bootstrap results to a byte array for BLOB storage. The byte array
        /// format retains the bootstrap parameter sets, which the XML format does not.
        /// </summary>
        /// <returns>The serialized results, or an empty array when there are no results.</returns>
        private byte[] ResultsToByteArray()
        {
            if (Results == null) return Array.Empty<byte>();
            return UncertaintyAnalysisResults.ToByteArray(Results);
        }

        /// <summary>
        /// Reconstructs the bootstrap results from a byte array.
        /// </summary>
        /// <param name="bytes">The serialized results.</param>
        /// <returns>The reconstructed results, or null when the array is empty.</returns>
        private static UncertaintyAnalysisResults ResultsFromByteArray(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            return UncertaintyAnalysisResults.FromByteArray(bytes);
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Determines if the critical inputs are valid.
        /// </summary>
        private bool AreInputsValid()
        {
            if (!_nameValid) return false;
            if (_parentDistribution == null || !_distributionValid) return false;
            if (!_recordLengthValid) return false;
            if (!_confidenceIntervalValid) return false;
            if (_prngSeed <= 0) return false;
            if (!_realizationsValid) return false;
            if (!_estimationMethodValid) return false;

            _ordinatesValid = true;
            _messenger.Remove(_noOrdinatesMsg);
            _messenger.Remove(_badOrdinatesMsg);
            _messenger.Remove(_unorderedOrdinatesMsg);

            if (ProbabilityOrdinates.Count == 0)
            {
                _ordinatesValid = false;
                _messenger.Add(_noOrdinatesMsg);
            }

            for (int i = 0; i < ProbabilityOrdinates.Count; i++)
            {
                if (ProbabilityOrdinates[i] < 0 || ProbabilityOrdinates[i] > 1)
                {
                    _ordinatesValid = false;
                    _messenger.Add(_badOrdinatesMsg);
                    break;
                }
                if (i > 0 && ProbabilityOrdinates[i] <= ProbabilityOrdinates[i - 1])
                {
                    _ordinatesValid = false;
                    _messenger.Add(_unorderedOrdinatesMsg);
                    break;
                }
            }

            if (!_ordinatesValid) return false;

            return true;
        }

        /// <summary>
        /// Validates the selected estimation method.
        /// </summary>
        private void ValidateEstimationMethod()
        {
            _estimationMethodValid = true;
            _messenger.Remove(_pmomMsg);
            _messenger.Remove(_lmomMsg);

            if (ParentDistribution == null)
            {
                _estimationMethodValid = false;
                SetElementValidation();
                return;
            }

            if (IsUncertain && EstimationMethod == ParameterEstimationMethod.MethodOfMoments)
            {
                if (ParentDistribution.Type == UnivariateDistributionType.GeneralizedNormal ||
                    ParentDistribution.Type == UnivariateDistributionType.KappaFour ||
                    ParentDistribution.Type == UnivariateDistributionType.Weibull)
                {
                    _messenger.Add(_pmomMsg);
                    _estimationMethodValid = false;
                }
            }
            else if (IsUncertain && EstimationMethod == ParameterEstimationMethod.MethodOfLinearMoments)
            {
                if (ParentDistribution.Type == UnivariateDistributionType.Weibull)
                {
                    _messenger.Add(_lmomMsg);
                    _estimationMethodValid = false;
                }
            }

            SetElementValidation();
        }

        /// <summary>Refreshes validation state and messages for the current parent distribution.</summary>
        private void ValidateParentDistribution()
        {
            _distributionValid = true;
            _messenger.Remove(_parentDistMsg);
            if (_parentDistribution == null || !_parentDistribution.ParametersValid)
            {
                _distributionValid = false;
                _messenger.Add(_parentDistMsg);
            }
        }

        /// <summary>
        /// Sets the IsValid property for the element, this needs to be called whenever a property is changed.
        /// </summary>
        private void SetElementValidation()
        {
            bool valid = AreInputsValid();
            if (!_isEstimated) valid = false;

            if (valid != _isValid)
            {
                _isValid = valid;
                RaisePropertyChange(nameof(IsValid));
            }
        }

        #endregion

        #region Element Specific Methods

        /// <summary>
        /// Raise property changed when the collection changed. 
        /// </summary>
        private void ProbabilityOrdinates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsEstimated = false;
            ClearResults();
            SetElementValidation();
            RaisePropertyChange(nameof(ProbabilityOrdinates));
        }

        /// <summary>
        /// Clear the results.
        /// </summary>
        private void ClearResults()
        {
            Results = new UncertaintyAnalysisResults();
            _minmaxComputed = false;
        }

        /// <summary>
        /// Set the parent distribution parameters.
        /// </summary>
        /// <param name="parameters">The list of parameters.</param>
        public void SetDistributionParameters(IList<double> parameters)
        {
            if (parameters == null || ParentDistribution == null) return;
            if (parameters.Count != ParentDistribution.NumberOfParameters) return;

            var oldSnapshot = ParentDistribution.ToXElement();
            var parms = ParentDistribution.GetParameters;
            bool changed = false;

            for (int i = 0; i < ParentDistribution.NumberOfParameters; i++)
            {
                if (parms[i] != parameters[i])
                {
                    changed = true;
                    break;
                }
            }

            ParentDistribution.SetParameters(parameters);
            ValidateParentDistribution();
            ValidateEstimationMethod();

            if (changed)
            {
                IsEstimated = false;
                ClearResults();
                RecordDistributionUndo("Distribution Parameters", oldSnapshot, ParentDistribution.ToXElement());
                _distributionSnapshot = ParentDistribution.ToXElement();
            }

            RaisePropertyChange("Distribution Parameters", setDirty: false);
            RaisePropertyChange(nameof(ParentDistribution), setDirty: false);
        }

        /// <summary>
        /// Perform the parametric bootstrap.
        /// </summary>
        public void Estimate()
        {
            if (!AreInputsValid()) return;

            _messenger.Remove(_badSimulationMsg);
            _messenger.Add(new BasicMessageItem(MessageType.Event, $"The parametric bootstrap analysis for '{Name}' has started.",
                this, ParentCollection.Name, Name, nameof(HazardElement)));

            IsEstimated = false;
            ClearResults();

            // Get the computed curve
            Results.ParentDistribution = ParentDistribution;
            var probs = new double[ProbabilityOrdinates.Count];
            Results.ModeCurve = new double[ProbabilityOrdinates.Count];

            for (int i = 0; i < ProbabilityOrdinates.Count; i++)
            {
                probs[i] = 1 - ProbabilityOrdinates[i];
                Results.ModeCurve[i] = ParentDistribution.InverseCDF(probs[i]);
            }

            if (IsUncertain)
            {
                try
                {
                    // Create seed from hash code
                    int seed = PRNGSeed;
                    // Perform the bootstrap
                    var bootstrap = new BootstrapAnalysis(ParentDistribution, EstimationMethod, EffectiveRecordLength, Realizations, seed);
                    Results = bootstrap.Estimate(probs, 1 - ConfidenceIntervalWidth);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Bootstrap estimation failed: {ex.Message}");
                    _messenger.Add(_badSimulationMsg);
                    IsEstimated = false;
                    return;
                }
            }

            _messenger.Add(new BasicMessageItem(MessageType.Event, $"The parametric bootstrap analysis for '{Name}' is complete.",
                this, ParentCollection.Name, Name, nameof(HazardElement)));

            IsEstimated = true;
        }

        /// <summary>
        /// Returns the mean hazard function
        /// </summary>
        public IUnivariateDistribution SampleFunction()
        {
            if (!IsEstimated) return null;

            if (IsUncertain)
            {
                if (!HasCompatibleCurveResults()) return null;

                var xValues = Results.MeanCurve.ToArray();
                var pValues = new double[ProbabilityOrdinates.Count];

                for (int i = 0; i < ProbabilityOrdinates.Count; i++)
                {
                    pValues[i] = 1 - ProbabilityOrdinates[i];
                }

                Array.Reverse(xValues);
                Array.Reverse(pValues);

                return new EmpiricalDistribution(xValues, pValues) { ProbabilityTransform = Numerics.Data.Transform.NormalZ };
            }
            else
            {
                return ParentDistribution.Clone();
            }
        }

        /// <summary>
        /// Returns the hazard function associated with the specified percentile. 
        /// </summary>
        /// <param name="percentile">The percentile of the uncertainty distribution.</param>
        public IUnivariateDistribution SampleFunction(double percentile)
        {
            if (!IsEstimated) return null;
            if (double.IsNaN(percentile) || percentile < 0 || percentile > 1) return null;

            if (IsUncertain)
            {
                var parameterSets = Results?.ParameterSets;
                if (parameterSets == null || parameterSets.Length == 0) return null;

                int index = (int)Math.Floor(percentile * parameterSets.Length);
                if (index >= parameterSets.Length) index = parameterSets.Length - 1;
                return SampleFunction(index);
            }
            else
            {
                return ParentDistribution.Clone();
            }
        }

        /// <summary>
        /// Returns the hazard function associated with the specified array index. 
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        public IUnivariateDistribution SampleFunction(int index)
        {
            if (!IsEstimated) return null;

            if (IsUncertain)
            {
                var parameterSets = Results?.ParameterSets;
                if (parameterSets == null || index < 0 || index >= parameterSets.Length) return null;
                if (parameterSets[index].Values == null) return null;

                var distribution = ParentDistribution.Clone();
                distribution.SetParameters(parameterSets[index].Values);
                return distribution;
            }
            else
            {
                return ParentDistribution.Clone();
            }
        }

        /// <summary>
        /// Returns the minimum hazard level of the function.
        /// </summary>
        /// <param name="meanOnly">Determines whether to return the mean-only value, or value from full uncertainty.</param>
        public double MinHazard(bool meanOnly)
        {
            if (_minmaxComputed) return _minmax[0];
            ComputeMinMax(meanOnly);
            return _minmax[0];
        }

        /// <summary>
        /// Returns the maximum hazard level of the function. 
        /// </summary>
        /// <param name="meanOnly">Determines whether to return the mean-only value, or value from full uncertainty.</param>
        public double MaxHazard(bool meanOnly)
        {
            if (_minmaxComputed) return _minmax[1];
            ComputeMinMax(meanOnly);
            return _minmax[1];
        }

        /// <summary>
        /// Compute min max of function. 
        /// </summary>
        private void ComputeMinMax(bool meanOnly)
        {
            if (ProbabilityOrdinates == null || ProbabilityOrdinates.Count == 0)
            {
                _minmax[0] = double.NaN;
                _minmax[1] = double.NaN;
                _minmaxComputed = true;
                return;
            }

            double minP = Numerics.Tools.Min(ProbabilityOrdinates);
            double maxP = Numerics.Tools.Max(ProbabilityOrdinates);

            if (meanOnly && IsUncertain)
            {
                var dist = SampleFunction();
                _minmax[0] = dist == null ? double.NaN : dist.InverseCDF(1 - maxP);
                _minmax[1] = dist == null ? double.NaN : dist.InverseCDF(1 - minP);
            }
            else if (!meanOnly && IsUncertain && Results?.ParameterSets != null && Results.ParameterSets.Length > 0)
            {
                double localMin = double.MaxValue;
                double localMax = double.MinValue;
                object lockObj = new object();

                Parallel.For(0, Results.ParameterSets.Length, idx =>
                {
                    var dist = ParentDistribution.Clone();
                    if (Results?.ParameterSets?[idx].Values != null)
                    {
                        dist.SetParameters(Results.ParameterSets[idx].Values);
                        double minX = dist.InverseCDF(1 - maxP);
                        double maxX = dist.InverseCDF(1 - minP);
                        lock (lockObj)
                        {
                            if (minX < localMin) localMin = minX;
                            if (maxX > localMax) localMax = maxX;
                        }
                    }
                });

                _minmax[0] = localMin == double.MaxValue ? double.NaN : localMin;
                _minmax[1] = localMax == double.MinValue ? double.NaN : localMax;
            }
            else
            {
                _minmax[0] = ParentDistribution.InverseCDF(1 - maxP);
                _minmax[1] = ParentDistribution.InverseCDF(1 - minP);
            }

            _minmaxComputed = true;
        }

        /// <summary>Checks whether stored curve results match the current probability ordinates and uncertainty mode.</summary>
        /// <returns>True when every required result array has the expected dimensions.</returns>
        private bool HasCompatibleCurveResults()
        {
            int count = ProbabilityOrdinates?.Count ?? 0;
            if (count == 0 || Results?.ModeCurve == null || Results.ModeCurve.Length != count) return false;

            if (IsUncertain)
            {
                if (Results.MeanCurve == null || Results.MeanCurve.Length != count) return false;
                if (Results.ConfidenceIntervals == null || Results.ConfidenceIntervals.GetLength(0) != count || Results.ConfidenceIntervals.GetLength(1) < 2) return false;
            }

            return true;
        }

        #endregion

        #region Plot Factory Methods

        /// <summary>
        /// Applies the default plot style used for the element-owned plot.
        /// </summary>
        /// <param name="plot">The plot to style.</param>
        private static void ApplyDefaultPlotStyle(Plot plot)
        {
            plot.BorderThickness = new System.Windows.Thickness(0);
            plot.Background = System.Windows.Media.Brushes.Transparent;
            plot.LegendBackground = (Color)ColorConverter.ConvertFromString("#8CFFFFFF");
            plot.LegendBorder = Colors.DarkGray;
            plot.LegendPosition = OxyPlot.Legends.LegendPosition.TopRight;
            plot.Padding = new System.Windows.Thickness(10, 10, 14, 10);
            plot.PlotAreaBackground = new SolidColorBrush(Colors.White);
        }

        /// <summary>
        /// Creates the default frequency plot with a logarithmic hazard axis and a normal
        /// probability exceedance axis.
        /// </summary>
        /// <returns>A new <see cref="Plot"/> configured for hazard frequency display.</returns>
        private static Plot CreateDefaultFrequencyPlot()
        {
            var plot = new Plot();
            ApplyDefaultPlotStyle(plot);
            plot.Title = "Hazard Distribution";
            plot.LegendPosition = OxyPlot.Legends.LegendPosition.TopLeft;
            plot.Axes.Add(new LogarithmicAxis
            {
                Key = "Yaxis",
                Position = OxyPlot.Axes.AxisPosition.Left,
                PowerPadding = true,
                Title = "Discharge",
                Unit = "cfs",
                AxisTitleDistance = 20,
                TitleFontSize = 16,
                FontSize = 12,
                MajorGridlineStyle = OxyPlot.LineStyle.Solid,
                MinorGridlineStyle = OxyPlot.LineStyle.Dash,
                StringFormat = "N0"
            });
            plot.Axes.Add(new NormalProbabilityAxis
            {
                Key = "Xaxis",
                Title = "Exceedance Probability ",
                Unit = "P(X > x)",
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                AxisTitleDistance = 20,
                TitleFontSize = 16,
                FontSize = 12,
            });
            return plot;
        }

        #endregion

        #region Distribution Undo

        /// <summary>Records a changed distribution snapshot when undo tracking is enabled.</summary>
        /// <param name="propertyName">The property named in the undo action.</param>
        /// <param name="oldSnapshot">The distribution state before the change.</param>
        /// <param name="newSnapshot">The distribution state after the change.</param>
        private void RecordDistributionUndo(string propertyName, XElement oldSnapshot, XElement newSnapshot)
        {
            var undoManager = IsUndoEnabled ? UndoManager : null;
            if (undoManager == null || undoManager.IsExecutingAction) return;
            if (oldSnapshot == null || newSnapshot == null) return;
            if (XNode.DeepEquals(oldSnapshot, newSnapshot)) return;

            var undoSnapshot = new XElement(oldSnapshot);
            var redoSnapshot = new XElement(newSnapshot);
            var action = new DelegateAction(
                $"Change {propertyName}",
                () => RestoreDistributionFromSnapshot(redoSnapshot),
                () => RestoreDistributionFromSnapshot(undoSnapshot),
                this);
            undoManager.RecordAction(action);
            SetIsDirty(true);
        }

        /// <summary>Restores a distribution snapshot and refreshes validation and dependent result state.</summary>
        /// <param name="snapshot">The serialized distribution state to restore.</param>
        private void RestoreDistributionFromSnapshot(XElement snapshot)
        {
            if (snapshot == null) return;

            _parentDistribution = UnivariateDistributionFactory.CreateDistribution(new XElement(snapshot));
            IsEstimated = false;
            ClearResults();
            ValidateParentDistribution();
            ValidateEstimationMethod();
            _distributionSnapshot = _parentDistribution.ToXElement();
            RaisePropertyChange(nameof(ParentDistribution), setDirty: false);
            RaisePropertyChange("Distribution Parameters", setDirty: false);
        }

        #endregion

        #region Undo Bridge Management

        /// <summary>
        /// Creates undo bridges for the ordinates collection and the element-owned plot.
        /// Disposes any existing bridges before creating new ones.
        /// </summary>
        private void SetupBridges()
        {
            DisposeBridges();

            // Subscribe to UndoManager.StateChanged to revalidate after undo/redo completes.
            UndoManager.StateChanged += UndoManager_StateChanged;

            // Collection bridge for probability ordinates.
            // Note: Use UndoManager property (not _undoManager field) to ensure lazy initialization.
            _ordinatesBridge = new UndoableCollectionBridge<double>(
                _probabilityOrdinates,
                () => IsUndoEnabled ? UndoManager : null,
                "probability ordinates",
                this);

            _distributionSnapshot = _parentDistribution?.ToXElement();

            // Plot undo manager for the element-owned frequency plot.
            if (_frequencyPlot != null)
            {
                _frequencyPlotUndo = new PlotUndoManager(
                    _frequencyPlot,
                    () => IsUndoEnabled ? UndoManager : null,
                    "frequency plot",
                    this,
                    () => SetIsDirty(true));
            }
        }

        /// <summary>
        /// Handles UndoManager.StateChanged to revalidate the element after undo/redo.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void UndoManager_StateChanged(object sender, EventArgs e)
        {
            SetElementValidation();
        }

        /// <summary>
        /// Disposes all undo bridges and sets their references to null.
        /// </summary>
        private void DisposeBridges()
        {
            UndoManager.StateChanged -= UndoManager_StateChanged;

            _ordinatesBridge?.Dispose();
            _ordinatesBridge = null;

            _frequencyPlotUndo?.Dispose();
            _frequencyPlotUndo = null;
            _distributionSnapshot = null;
        }

        /// <summary>
        /// Suspends the plot undo bridges so that programmatic plot changes (series population,
        /// plot settings restoration) do not create undo entries.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that resumes recording when disposed.</returns>
        public IDisposable SuspendPlotBridges()
        {
            var suspensions = new List<IDisposable>();
            if (_frequencyPlotUndo != null) suspensions.Add(_frequencyPlotUndo.SuspendRecording());
            return new AggregateDisposable(suspensions);
        }

        /// <summary>
        /// Rebuilds series and annotation bridges for the specified plot after a bulk series update.
        /// Call this after populating series inside a <see cref="SuspendPlotBridges"/> block.
        /// </summary>
        /// <param name="plot">The plot whose bridges should be rebuilt.</param>
        public void RebuildSeriesAndAnnotationBridges(Plot plot)
        {
            if (plot == _frequencyPlot) _frequencyPlotUndo?.RebuildSeriesAndAnnotationBridges();
        }

        #endregion

        #endregion

    }
}
