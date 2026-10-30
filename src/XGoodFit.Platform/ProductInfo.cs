using System.Reflection;

namespace XGoodFit.Platform;

/// <summary>
/// XGoodFit addition: the product's identity, from the branding properties in
/// <c>Directory.Build.props</c> (migration plan §1.1), which the packages are built with too
/// (<c>scripts/package.sh</c>).
/// </summary>
public static class ProductInfo
{
    /// <summary>The product name: the installer's and shortcuts' title, and the Windows launcher's name.</summary>
    public static string Name { get; } = Read("XGoodFitProductName");

    /// <summary>The bundle ID and Velopack pack ID, such as <c>io.github.jonklaric.xgoodfit</c>.</summary>
    public static string AppId { get; } = Read("XGoodFitAppId");

    /// <summary>The GitHub repository whose releases are the update feed.</summary>
    public static string UpdateRepository { get; } = Read("XGoodFitUpdateRepository");

    private static string Read(string key)
    {
        string? value = typeof(ProductInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?.Value;
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"XGoodFit.Platform was built without {key} (Directory.Build.props).");
        return value;
    }
}
