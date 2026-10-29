using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace GenericControls
{
    /// <summary>
    /// Provides general utility methods for WPF applications, including visual tree searching, screen capture, and UI threading helpers.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class GeneralMethods
    {

        /// <summary>
        /// Find the first child element in a framework element with the given name for the specified type T.
        /// </summary>
        /// <typeparam name="T">Framework element type to be returned.</typeparam>
        /// <param name="element">Element to search.</param>
        /// <param name="childName">Name of the child element to be returned.</param>
        /// <returns>The first child element of type T with the specified name, or null if not found.</returns>
        public static T FindElementByName<T>(FrameworkElement element, string childName) where T : FrameworkElement
        {
            T childElement = null;
            int childCount = VisualTreeHelper.GetChildrenCount(element);

            if (childCount == 0 && element.GetType() == typeof(System.Windows.Controls.Primitives.Popup))
            {
                System.Windows.Controls.Primitives.Popup popupElement = (System.Windows.Controls.Primitives.Popup)element;
                if (!(popupElement.Child == null))
                    return FindElementByName<T>((FrameworkElement)popupElement.Child, childName);
            }

            for (int i = 0, loopTo = childCount - 1; i <= loopTo; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child is null)
                    continue;

                if (child is T && child.Name.Equals(childName))
                {
                    childElement = (T)child;
                    break;
                }

                childElement = FindElementByName<T>(child, childName);
                if (childElement is not null)
                    break;
            }

            return childElement;
        }

        /// <summary>
        /// Get all child elements of a specific type.
        /// </summary>
        /// <param name="element">Element to search.</param>
        /// <param name="t">Type of child elements to search for and return.</param>
        /// <returns>A list of all visual elements of the specified type found in the element tree.</returns>
        public static List<Visual> GetAllElementsOfType(Visual element, Type t)
        {
            var r = new List<Visual>();
            for (int i = 0, loopTo = VisualTreeHelper.GetChildrenCount(element) - 1; i <= loopTo; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(element, i);
                if (v == null)
                    continue;
                r.AddRange(GetAllElementsOfType(v, t));
                if (v.GetType() == t)
                    r.Add(v);
            }
            return r;
        }

        /// <summary>
        /// Test if a web connection is available. Returns true if connection is available.
        /// </summary>
        /// <returns>Returns true if connection is available.</returns>
        public static bool HasWebConnection()
        {
            try
            {
                var dummy = System.Net.Dns.GetHostEntry("www.google.com");
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return false;
            }
        }

        /// <summary>
        /// File open dialog that returns the full path of the file to be opened. Returns blank string if nothing selected.
        /// </summary>
        /// <param name="filters">Filters for the file open dialog selector. (e.g. "Text File (*.txt) |*.txt|Word File (*.docx) |*.docx|All files (*.*) |*.*"</param>
        /// <returns>String of the file to be opened.</returns>
        public static string FileOpenDialog(string filters)
        {
            string FileOpenDialogRet = default;
            var OpenfileDialog = new OpenFileDialog() { Filter = filters};
            if (OpenfileDialog.ShowDialog() is { } arg1 && arg1 == true)
            {
                FileOpenDialogRet = OpenfileDialog.FileName;
            }
            else
            {
                FileOpenDialogRet = default;
            }

            return FileOpenDialogRet;
        }

        /// <summary>
        /// File save dialog that returns the full path of the file to be saved. Returns blank string if nothing selected.
        /// </summary>
        /// <param name="filters">Filters for the file open dialog selector. (e.g. "Text File (*.txt) |*.txt|Word File (*.docx) |*.docx|All files (*.*) |*.*"</param>
        /// <param name="overwritePrompt">Whether to prompt the user before overwriting an existing file.</param>
        /// <returns>The full path of the selected file, or an empty string if the dialog was cancelled.</returns>
        public static string FileSaveDialog(string filters, bool overwritePrompt = true)
        {
            string FileSaveDialogRet = default;
            var SaveFileBrowser = new SaveFileDialog() { OverwritePrompt = overwritePrompt, Filter = filters };
            if (SaveFileBrowser.ShowDialog() is { } arg2 && arg2 == true)
            {
                FileSaveDialogRet = SaveFileBrowser.FileName.ToString();
            }
            else
            {
                FileSaveDialogRet = "";
            }

            return FileSaveDialogRet;
        }

        /// <summary>
        /// Folder browser dialog that returns the full path of the selected folder. Returns blank if nothing is selected.
        /// </summary>
        /// <param name="owner">The owner of the dialog box.</param>
        /// <param name="title">The text that appears in the title bar of the dialog.</param>
        /// <param name="initialDirectory">The initial directory in which the dialog will be opened to.</param>
        /// <param name="defaultDirectory">The directory in which the dialog will be opened to if there is no recent directory available.</param>
        public static string FolderBrowserDialog(Window owner = null, string title = null, string initialDirectory = null, string defaultDirectory = null)
        {
            var FolderBrowser = new FolderBrowser() { Title = title, InitialDirectory = initialDirectory, DefaultDirectory = defaultDirectory };
            if (FolderBrowser.ShowDialog(owner) == true)
            {
                return FolderBrowser.SelectedFolder;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Copies a file with optional progress reporting.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="destinationFilePath">Destination file path.</param>
        /// <param name="progress">Optional progress reporter (0-100%).</param>
        public static void CopyFile(string sourceFilePath, string destinationFilePath, IProgress<int> progress = null)
        {
            byte[] buffer = new byte[1048576]; // 1MB buffer
            using (var source = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                long fileLength = source.Length;
                using (var dest = new FileStream(destinationFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    long totalBytes = 0L;
                    int currentBlockSize = source.Read(buffer, 0, buffer.Length);
                    while (currentBlockSize > 0)
                    {
                        totalBytes += currentBlockSize;
                        dest.Write(buffer, 0, currentBlockSize);
                        if (progress != null)
                            progress.Report((int)Math.Round(totalBytes * 100.0d / fileLength));
                        currentBlockSize = source.Read(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Copies all files in one directory to another directory.
        /// </summary>
        /// <param name="sourcePath">Directory path to be copied.</param>
        /// <param name="destinationPath">Destination directory where files will be copied to.</param>
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (string file__1 in Directory.GetFiles(sourcePath))
            {
                string dest = System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(file__1));
                File.Copy(file__1, dest);
            }

            foreach (string folder in Directory.GetDirectories(sourcePath))
            {
                string dest = System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(folder));
                CopyDirectory(folder, dest);
            }
        }

        /// <summary>
        /// Recursively copies the files with a UI progress bar and text updates.
        /// </summary>
        /// <param name="sourcePath">Source directory path.</param>
        /// <param name="destinationPath">Destination directory path.</param>
        /// <param name="theProgressBar">ProgressBar to visually update progress.</param>
        /// <param name="progressText">TextBlock to show progress messages.</param>
        public static void CopyDirectory(string sourcePath, string destinationPath, ProgressBar theProgressBar, TextBlock progressText)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            if (!Directory.Exists(sourcePath))
                return;
            // 
            var dir = new DirectoryInfo(sourcePath);
            string DirName = dir.Name;
            string[] DirectoryFiles = Directory.GetFiles(sourcePath);
            var updatePbDelegate = new UpdateProgressBarDelegate(theProgressBar.SetValue);
            var UpdatePbTDelegate = new UpdateProgressBarDelegate(progressText.SetValue);
            for (int i = 0, loopTo = DirectoryFiles.Count() - 1; i <= loopTo; i++)
            {
                string dest = System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(DirectoryFiles[i]));
                File.Copy(DirectoryFiles[i], dest);
                Application.Current.Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, new object[] { System.Windows.Controls.Primitives.RangeBase.ValueProperty, 100 * i / (double)DirectoryFiles.Count() });
                Application.Current.Dispatcher.Invoke(UpdatePbTDelegate, DispatcherPriority.Background, new object[] { TextBlock.TextProperty, (int)Math.Round(100d * (i / (double)DirectoryFiles.Count())) + "% Copying From " + DirName });
            }
            // 
            foreach (string folder in Directory.GetDirectories(sourcePath))
            {
                string dest = System.IO.Path.Combine(destinationPath, System.IO.Path.GetFileName(folder));
                CopyDirectory(folder, dest, theProgressBar, progressText);
            }

            Application.Current.Dispatcher.Invoke(updatePbDelegate, DispatcherPriority.Background, new object[] { System.Windows.Controls.Primitives.RangeBase.ValueProperty, 0d });
            Application.Current.Dispatcher.Invoke(UpdatePbTDelegate, DispatcherPriority.Background, new object[] { TextBlock.TextProperty, "" });
        }
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

        /// <summary>
        /// Attempts to delete a directory and all contents.
        /// </summary>
        /// <param name="dir">The path of the directory to delete.</param>
        /// <returns><c>true</c> if the directory was successfully deleted or didn't exist; <c>false</c> if an error occurred.</returns>
        public static bool DeleteDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    var directory = new DirectoryInfo(dir);
                    directory.Delete(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Determines if a type is numeric.  Nullable numeric types are considered numeric.
        /// </summary>
        /// <remarks>
        /// Boolean is not considered numeric.
        /// http://stackoverflow.com/questions/124411/using-net-how-can-i-determine-if-a-type-is-a-numeric-valuetype
        /// </remarks>
        public static bool IsNumericType(Type typeToTest)
        {
            if (typeToTest is null)
            {
                return false;
            }

            switch (Type.GetTypeCode(typeToTest))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        return true;
                    }
                case TypeCode.Object:
                    {
                        if (typeToTest.IsGenericType && typeToTest.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            return IsNumericType(Nullable.GetUnderlyingType(typeToTest));
                        }
                        return false;
                    }
            }
            return false;
        }


        /// <summary>
        /// Random color short list, 64.
        /// </summary>
        public static string[] RandomColorsShortList = new string[] { "#000000", "#00FF00", "#0000FF", "#FF0000", "#01FFFE", "#FFA6FE", "#FFDB66", "#006401", "#010067", "#007DB5", "#FF00F6", "#FFEEE8", "#774D00", "#90FB92", "#0076FF", "#D5FF00", "#FF937E", "#6A826C", "#FF029D", "#FE8900", "#7A4782", "#7E2DD2", "#85A900", "#FF0056", "#A42400", "#00AE7E", "#683D3B", "#BDC6FF", "#263400", "#BDD393", "#00B917", "#9E008E", "#001544", "#C28C9F", "#FF74A3", "#01D0FF", "#004754", "#E56FFE", "#788231", "#0E4CA1", "#91D0CB", "#BE9970", "#968AE8", "#BB8800", "#43002C", "#DEFF74", "#00FFC6", "#FFE502", "#620E00", "#008F9C", "#98FF52", "#7544B1", "#B500FF", "#00FF78", "#FF6E41", "#005F39", "#6B6882", "#5FAD4E", "#A75740", "#A5FFD2", "#FFB167", "#009BFF", "#E85EBE" };

        /// <summary>
        /// Random color long list, 1024.
        /// </summary>
        public static string[] RandomColorsLongList = new string[] { "#000000", "#FFFF00", "#1CE6FF", "#FF34FF", "#FF4A46", "#008941", "#006FA6", "#A30059", "#FFDBE5", "#7A4900", "#0000A6", "#63FFAC", "#B79762", "#004D43", "#8FB0FF", "#997D87", "#5A0007", "#809693", "#FEFFE6", "#1B4400", "#4FC601", "#3B5DFF", "#4A3B53", "#FF2F80", "#61615A", "#BA0900", "#6B7900", "#00C2A0", "#FFAA92", "#FF90C9", "#B903AA", "#D16100", "#DDEFFF", "#000035", "#7B4F4B", "#A1C299", "#300018", "#0AA6D8", "#013349", "#00846F", "#372101", "#FFB500", "#C2FFED", "#A079BF", "#CC0744", "#C0B9B2", "#C2FF99", "#001E09", "#00489C", "#6F0062", "#0CBD66", "#EEC3FF", "#456D75", "#B77B68", "#7A87A1", "#788D66", "#885578", "#FAD09F", "#FF8A9A", "#D157A0", "#BEC459", "#456648", "#0086ED", "#886F4C", "#34362D", "#B4A8BD", "#00A6AA", "#452C2C", "#636375", "#A3C8C9", "#FF913F", "#938A81", "#575329", "#00FECF", "#B05B6F", "#8CD0FF", "#3B9700", "#04F757", "#C8A1A1", "#1E6E00", "#7900D7", "#A77500", "#6367A9", "#A05837", "#6B002C", "#772600", "#D790FF", "#9B9700", "#549E79", "#FFF69F", "#201625", "#72418F", "#BC23FF", "#99ADC0", "#3A2465", "#922329", "#5B4534", "#FDE8DC", "#404E55", "#0089A3", "#CB7E98", "#A4E804", "#324E72", "#6A3A4C", "#83AB58", "#001C1E", "#D1F7CE", "#004B28", "#C8D0F6", "#A3A489", "#806C66", "#222800", "#BF5650", "#E83000", "#66796D", "#DA007C", "#FF1A59", "#8ADBB4", "#1E0200", "#5B4E51", "#C895C5", "#320033", "#FF6832", "#66E1D3", "#CFCDAC", "#D0AC94", "#7ED379", "#012C58", "#7A7BFF", "#D68E01", "#353339", "#78AFA1", "#FEB2C6", "#75797C", "#837393", "#943A4D", "#B5F4FF", "#D2DCD5", "#9556BD", "#6A714A", "#001325", "#02525F", "#0AA3F7", "#E98176", "#DBD5DD", "#5EBCD1", "#3D4F44", "#7E6405", "#02684E", "#962B75", "#8D8546", "#9695C5", "#E773CE", "#D86A78", "#3E89BE", "#CA834E", "#518A87", "#5B113C", "#55813B", "#E704C4", "#00005F", "#A97399", "#4B8160", "#59738A", "#FF5DA7", "#F7C9BF", "#643127", "#513A01", "#6B94AA", "#51A058", "#A45B02", "#1D1702", "#E20027", "#E7AB63", "#4C6001", "#9C6966", "#64547B", "#97979E", "#006A66", "#391406", "#F4D749", "#0045D2", "#006C31", "#DDB6D0", "#7C6571", "#9FB2A4", "#00D891", "#15A08A", "#BC65E9", "#FFFFFE", "#C6DC99", "#203B3C", "#671190", "#6B3A64", "#F5E1FF", "#FFA0F2", "#CCAA35", "#374527", "#8BB400", "#797868", "#C6005A", "#3B000A", "#C86240", "#29607C", "#402334", "#7D5A44", "#CCB87C", "#B88183", "#AA5199", "#B5D6C3", "#A38469", "#9F94F0", "#A74571", "#B894A6", "#71BB8C", "#00B433", "#789EC9", "#6D80BA", "#953F00", "#5EFF03", "#E4FFFC", "#1BE177", "#BCB1E5", "#76912F", "#003109", "#0060CD", "#D20096", "#895563", "#29201D", "#5B3213", "#A76F42", "#89412E", "#1A3A2A", "#494B5A", "#A88C85", "#F4ABAA", "#A3F3AB", "#00C6C8", "#EA8B66", "#958A9F", "#BDC9D2", "#9FA064", "#BE4700", "#658188", "#83A485", "#453C23", "#47675D", "#3A3F00", "#061203", "#DFFB71", "#868E7E", "#98D058", "#6C8F7D", "#D7BFC2", "#3C3E6E", "#D83D66", "#2F5D9B", "#6C5E46", "#D25B88", "#5B656C", "#00B57F", "#545C46", "#866097", "#365D25", "#252F99", "#00CCFF", "#674E60", "#FC009C", "#92896B", "#1E2324", "#DEC9B2", "#9D4948", "#85ABB4", "#342142", "#D09685", "#A4ACAC", "#00FFFF", "#AE9C86", "#742A33", "#0E72C5", "#AFD8EC", "#C064B9", "#91028C", "#FEEDBF", "#FFB789", "#9CB8E4", "#AFFFD1", "#2A364C", "#4F4A43", "#647095", "#34BBFF", "#807781", "#920003", "#B3A5A7", "#018615", "#F1FFC8", "#976F5C", "#FF3BC1", "#FF5F6B", "#077D84", "#F56D93", "#5771DA", "#4E1E2A", "#830055", "#02D346", "#BE452D", "#00905E", "#BE0028", "#6E96E3", "#007699", "#FEC96D", "#9C6A7D", "#3FA1B8", "#893DE3", "#79B4D6", "#7FD4D9", "#6751BB", "#B28D2D", "#E27A05", "#DD9CB8", "#AABC7A", "#980034", "#561A02", "#8F7F00", "#635000", "#CD7DAE", "#8A5E2D", "#FFB3E1", "#6B6466", "#C6D300", "#0100E2", "#88EC69", "#8FCCBE", "#21001C", "#511F4D", "#E3F6E3", "#FF8EB1", "#6B4F29", "#A37F46", "#6A5950", "#1F2A1A", "#04784D", "#101835", "#E6E0D0", "#FF74FE", "#00A45F", "#8F5DF8", "#4B0059", "#412F23", "#D8939E", "#DB9D72", "#604143", "#B5BACE", "#989EB7", "#D2C4DB", "#A587AF", "#77D796", "#7F8C94", "#FF9B03", "#555196", "#31DDAE", "#74B671", "#802647", "#2A373F", "#014A68", "#696628", "#4C7B6D", "#002C27", "#7A4522", "#3B5859", "#E5D381", "#FFF3FF", "#679FA0", "#261300", "#2C5742", "#9131AF", "#AF5D88", "#C7706A", "#61AB1F", "#8CF2D4", "#C5D9B8", "#9FFFFB", "#BF45CC", "#493941", "#863B60", "#B90076", "#003177", "#C582D2", "#C1B394", "#602B70", "#887868", "#BABFB0", "#030012", "#D1ACFE", "#7FDEFE", "#4B5C71", "#A3A097", "#E66D53", "#637B5D", "#92BEA5", "#00F8B3", "#BEDDFF", "#3DB5A7", "#DD3248", "#B6E4DE", "#427745", "#598C5A", "#B94C59", "#8181D5", "#94888B", "#FED6BD", "#536D31", "#6EFF92", "#E4E8FF", "#20E200", "#FFD0F2", "#4C83A1", "#BD7322", "#915C4E", "#8C4787", "#025117", "#A2AA45", "#2D1B21", "#A9DDB0", "#FF4F78", "#528500", "#009A2E", "#17FCE4", "#71555A", "#525D82", "#00195A", "#967874", "#555558", "#0B212C", "#1E202B", "#EFBFC4", "#6F9755", "#6F7586", "#501D1D", "#372D00", "#741D16", "#5EB393", "#B5B400", "#DD4A38", "#363DFF", "#AD6552", "#6635AF", "#836BBA", "#98AA7F", "#464836", "#322C3E", "#7CB9BA", "#5B6965", "#707D3D", "#7A001D", "#6E4636", "#443A38", "#AE81FF", "#489079", "#897334", "#009087", "#DA713C", "#361618", "#FF6F01", "#006679", "#370E77", "#4B3A83", "#C9E2E6", "#C44170", "#FF4526", "#73BE54", "#C4DF72", "#ADFF60", "#00447D", "#DCCEC9", "#BD9479", "#656E5B", "#EC5200", "#FF6EC2", "#7A617E", "#DDAEA2", "#77837F", "#A53327", "#608EFF", "#B599D7", "#A50149", "#4E0025", "#C9B1A9", "#03919A", "#1B2A25", "#E500F1", "#982E0B", "#B67180", "#E05859", "#006039", "#578F9B", "#305230", "#CE934C", "#B3C2BE", "#C0BAC0", "#B506D3", "#170C10", "#4C534F", "#224451", "#3E4141", "#78726D", "#B6602B", "#200441", "#DDB588", "#497200", "#C5AAB6", "#033C61", "#71B2F5", "#A9E088", "#4979B0", "#A2C3DF", "#784149", "#2D2B17", "#3E0E2F", "#57344C", "#0091BE", "#E451D1", "#4B4B6A", "#5C011A", "#7C8060", "#FF9491", "#4C325D", "#005C8B", "#E5FDA4", "#68D1B6", "#032641", "#140023", "#8683A9", "#CFFF00", "#A72C3E", "#34475A", "#B1BB9A", "#B4A04F", "#8D918E", "#A168A6", "#813D3A", "#425218", "#DA8386", "#776133", "#563930", "#8498AE", "#90C1D3", "#B5666B", "#9B585E", "#856465", "#AD7C90", "#E2BC00", "#E3AAE0", "#B2C2FE", "#FD0039", "#009B75", "#FFF46D", "#E87EAC", "#DFE3E6", "#848590", "#AA9297", "#83A193", "#577977", "#3E7158", "#C64289", "#EA0072", "#C4A8CB", "#55C899", "#E78FCF", "#004547", "#F6E2E3", "#966716", "#378FDB", "#435E6A", "#DA0004", "#1B000F", "#5B9C8F", "#6E2B52", "#011115", "#E3E8C4", "#AE3B85", "#EA1CA9", "#FF9E6B", "#457D8B", "#92678B", "#00CDBB", "#9CCC04", "#002E38", "#96C57F", "#CFF6B4", "#492818", "#766E52", "#20370E", "#E3D19F", "#2E3C30", "#B2EACE", "#F3BDA4", "#A24E3D", "#976FD9", "#8C9FA8", "#7C2B73", "#4E5F37", "#5D5462", "#90956F", "#6AA776", "#DBCBF6", "#DA71FF", "#987C95", "#52323C", "#BB3C42", "#584D39", "#4FC15F", "#A2B9C1", "#79DB21", "#1D5958", "#BD744E", "#160B00", "#20221A", "#6B8295", "#00E0E4", "#102401", "#1B782A", "#DAA9B5", "#B0415D", "#859253", "#97A094", "#06E3C4", "#47688C", "#7C6755", "#075C00", "#7560D5", "#7D9F00", "#C36D96", "#4D913E", "#5F4276", "#FCE4C8", "#303052", "#4F381B", "#E5A532", "#706690", "#AA9A92", "#237363", "#73013E", "#FF9079", "#A79A74", "#029BDB", "#FF0169", "#C7D2E7", "#CA8869", "#80FFCD", "#BB1F69", "#90B0AB", "#7D74A9", "#FCC7DB", "#99375B", "#00AB4D", "#ABAED1", "#BE9D91", "#E6E5A7", "#332C22", "#DD587B", "#F5FFF7", "#5D3033", "#6D3800", "#FF0020", "#B57BB3", "#D7FFE6", "#C535A9", "#260009", "#6A8781", "#A8ABB4", "#D45262", "#794B61", "#4621B2", "#8DA4DB", "#C7C890", "#6FE9AD", "#A243A7", "#B2B081", "#181B00", "#286154", "#4CA43B", "#6A9573", "#A8441D", "#5C727B", "#738671", "#D0CFCB", "#897B77", "#1F3F22", "#4145A7", "#DA9894", "#A1757A", "#63243C", "#ADAAFF", "#00CDE2", "#DDBC62", "#698EB1", "#208462", "#00B7E0", "#614A44", "#9BBB57", "#7A5C54", "#857A50", "#766B7E", "#014833", "#FF8347", "#7A8EBA", "#274740", "#946444", "#EBD8E6", "#646241", "#373917", "#6AD450", "#81817B", "#D499E3", "#979440", "#011A12", "#526554", "#B5885C", "#A499A5", "#03AD89", "#B3008B", "#E3C4B5", "#96531F", "#867175", "#74569E", "#617D9F", "#E70452", "#067EAF", "#A697B6", "#B787A8", "#9CFF93", "#311D19", "#3A9459", "#6E746E", "#B0C5AE", "#84EDF7", "#ED3488", "#754C78", "#384644", "#C7847B", "#00B6C5", "#7FA670", "#C1AF9E", "#2A7FFF", "#72A58C", "#FFC07F", "#9DEBDD", "#D97C8E", "#7E7C93", "#62E674", "#B5639E", "#FFA861", "#C2A580", "#8D9C83", "#B70546", "#372B2E", "#0098FF", "#985975", "#20204C", "#FF6C60", "#445083", "#8502AA", "#72361F", "#9676A3", "#484449", "#CED6C2", "#3B164A", "#CCA763", "#2C7F77", "#02227B", "#A37E6F", "#CDE6DC", "#CDFFFB", "#BE811A", "#F77183", "#EDE6E2", "#CDC6B4", "#FFE09E", "#3A7271", "#FF7B59", "#4E4E01", "#4AC684", "#8BC891", "#BC8A96", "#CF6353", "#DCDE5C", "#5EAADD", "#F6A0AD", "#E269AA", "#A3DAE4", "#436E83", "#002E17", "#ECFBFF", "#A1C2B6", "#50003F", "#71695B", "#67C4BB", "#536EFF", "#5D5A48", "#890039", "#969381", "#371521", "#5E4665", "#AA62C3", "#8D6F81", "#2C6135", "#410601", "#564620", "#E69034", "#6DA6BD", "#E58E56", "#E3A68B", "#48B176", "#D27D67", "#B5B268", "#7F8427", "#FF84E6", "#435740", "#EAE408", "#F4F5FF", "#325800", "#4B6BA5", "#ADCEFF", "#9B8ACC", "#885138", "#5875C1", "#7E7311", "#FEA5CA", "#9F8B5B", "#A55B54", "#89006A", "#AF756F", "#2A2000", "#576E4A", "#7F9EFF", "#7499A1", "#FFB550", "#00011E", "#D1511C", "#688151", "#BC908A", "#78C8EB", "#8502FF", "#483D30", "#C42221", "#5EA7FF", "#785715", "#0CEA91", "#FFFAED", "#B3AF9D", "#3E3D52", "#5A9BC2", "#9C2F90", "#8D5700", "#ADD79C", "#00768B", "#337D00", "#C59700", "#3156DC", "#944575", "#ECFFDC", "#D24CB2", "#97703C", "#4C257F", "#9E0366", "#88FFEC", "#B56481", "#396D2B", "#56735F", "#988376", "#9BB195", "#A9795C", "#E4C5D3", "#9F4F67", "#1E2B39", "#664327", "#AFCE78", "#322EDF", "#86B487", "#C23000", "#ABE86B", "#96656D", "#250E35", "#A60019", "#0080CF", "#CAEFFF", "#323F61", "#A449DC", "#6A9D3B", "#FF5AE4", "#636A01", "#D16CDA", "#736060", "#FFBAAD", "#D369B4", "#FFDED6", "#6C6D74", "#927D5E", "#845D70", "#5B62C1", "#2F4A36", "#E45F35", "#FF3B53", "#AC84DD", "#762988", "#70EC98", "#408543", "#2C3533", "#2E182D", "#323925", "#19181B", "#2F2E2C", "#023C32", "#9B9EE2", "#58AFAD", "#5C424D", "#7AC5A6", "#685D75", "#B9BCBD", "#834357", "#1A7B42", "#2E57AA", "#E55199", "#316E47", "#CD00C5", "#6A004D", "#7FBBEC", "#F35691", "#D7C54A", "#62ACB7", "#CBA1BC", "#A28A9A", "#6C3F3B", "#FFE47D", "#DCBAE3", "#5F816D", "#3A404A", "#7DBF32", "#E6ECDC", "#852C19", "#285366", "#B8CB9C", "#0E0D00", "#4B5D56", "#6B543F", "#E27172", "#0568EC", "#2EB500", "#D21656", "#EFAFFF", "#682021", "#2D2011", "#DA4CFF", "#70968E", "#FF7B7D", "#4A1930", "#E8C282", "#E7DBBC", "#A68486", "#1F263C", "#36574E", "#52CE79", "#ADAAA9", "#8A9F45", "#6542D2", "#00FB8C", "#5D697B", "#CCD27F", "#94A5A1", "#790229", "#E383E6", "#7EA4C1", "#4E4452", "#4B2C00", "#620B70", "#314C1E", "#874AA6", "#E30091", "#66460A", "#EB9A8B", "#EAC3A3", "#98EAB3", "#AB9180", "#B8552F", "#1A2B2F", "#94DDC5", "#9D8C76", "#9C8333", "#94A9C9", "#392935", "#8C675E", "#CCE93A", "#917100", "#01400B", "#449896", "#1CA370", "#E08DA7", "#8B4A4E", "#667776", "#4692AD", "#67BDA8", "#69255C", "#D3BFFF", "#4A5132", "#7E9285", "#77733C", "#E7A0CC", "#51A288", "#2C656A", "#4D5C5E", "#C9403A", "#DDD7F3", "#005844", "#B4A200", "#488F69", "#858182", "#D4E9B9", "#3D7397", "#CAE8CE", "#D60034", "#AA6746", "#9E5585", "#BA6200" };


    }
}