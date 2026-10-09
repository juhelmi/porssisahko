using System;
using System.IO;

namespace LaskeEdullisinLataus;

public static class AppConstants
{
    public const string PriceExportDirectoryName = "LaskeEdullisinLataus";
    public const string LatestPricesFileBaseName = "latest-prices";

    public static string PriceExportDirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        PriceExportDirectoryName);
}