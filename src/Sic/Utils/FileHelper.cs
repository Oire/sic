using Serilog;

namespace Oire.Sic.Utils;

public static class FileHelper {
    private const int FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x400000;
    private const int FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x200000;

    public static bool IsCloudPlaceholder(string path) {
        try {
            var attributes = (int)File.GetAttributes(path);
            return (attributes & FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0
                || (attributes & FILE_ATTRIBUTE_RECALL_ON_OPEN) != 0;
        } catch (FileNotFoundException) {
            return true;
        } catch (Exception ex) {
            Log.Warning("Failed to check cloud placeholder status for {Path}: {Error}", path, ex.Message);
            return false;
        }
    }

    public static IEnumerable<string> EnumerateImageFiles(
        string folder, string[] extensions, SearchOption searchOption) {
        var options = new EnumerationOptions {
            IgnoreInaccessible = true,
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
        };

        foreach (var extension in extensions) {
            foreach (var file in Directory.EnumerateFiles(folder, extension, options)) {
                yield return file;
            }
        }
    }
}
