using System.Reflection;

namespace FrameworkUI
{
    /// <summary>
    /// A utility class for application attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Woody Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class ApplicationAttributes
    {

        /// <summary>
        /// Construct empty application attributes.
        /// </summary>
        private ApplicationAttributes() { }

        /// <summary>
        /// Initializes the application attributes.
        /// </summary>
        static ApplicationAttributes()
        {
            Assembly = Assembly.GetEntryAssembly();
            if (Assembly != null)
            {
                var attributes = Assembly.GetCustomAttributes(false);
                foreach (object attribute in attributes)
                {
                    var type = attribute.GetType();
                    if (type == typeof(AssemblyTitleAttribute))
                    {
                        _Title = (AssemblyTitleAttribute)attribute;
                    }
                    if (type == typeof(AssemblyCompanyAttribute))
                    {
                        _Company = (AssemblyCompanyAttribute)attribute;
                    }
                    if (type == typeof(AssemblyCopyrightAttribute))
                    {
                        _Copyright = (AssemblyCopyrightAttribute)attribute;
                    }
                    if (type == typeof(AssemblyProductAttribute))
                    {
                        _Product = (AssemblyProductAttribute)attribute;
                    }
                    if (type == typeof(AssemblyDescriptionAttribute))
                    {
                        _Description = (AssemblyDescriptionAttribute)attribute;
                    }
                }
                _Version = Assembly.GetName().Version;
            }
        }

        /// <summary>
        /// The assembly title attribute.
        /// </summary>
        private static readonly AssemblyTitleAttribute? _Title = null;
        /// <summary>
        /// The assembly company attribute.
        /// </summary>
        private static readonly AssemblyCompanyAttribute? _Company = null;
        /// <summary>
        /// The assembly copyright attribute.
        /// </summary>
        private static readonly AssemblyCopyrightAttribute? _Copyright = null;
        /// <summary>
        /// The assembly product attribute.
        /// </summary>
        private static readonly AssemblyProductAttribute? _Product = null;
        /// <summary>
        /// The assembly description attribute.
        /// </summary>
        private static readonly AssemblyDescriptionAttribute? _Description = null;
        /// <summary>
        /// The assembly version.
        /// </summary>
        private static readonly Version? _Version = null;

        /// <summary>
        /// Gets the application assembly.
        /// </summary>
        public static readonly Assembly? Assembly;

        /// <summary>
        /// Gets the assembly title.
        /// </summary>
        public static string Title
        {
            get { return _Title?.Title ?? string.Empty; }
        }

        /// <summary>
        /// Gets the application company name.
        /// </summary>
        public static string CompanyName
        {
            get { return _Company?.Company ?? string.Empty; }
        }

        /// <summary>
        /// Gets the application copyright.
        /// </summary>
        public static string Copyright
        {
            get { return _Copyright?.Copyright ?? string.Empty; }
        }

        /// <summary>
        /// Gets the application product name.
        /// </summary>
        public static string ProductName
        {
            get { return _Product?.Product ?? string.Empty; }
        }

        /// <summary>
        /// Gets the assembly description.
        /// </summary>
        public static string Description
        {
            get { return _Description?.Description ?? string.Empty; }
        }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        public static string Version
        {
            get { return _Version?.ToString() ?? string.Empty; }
        }

    }
}
