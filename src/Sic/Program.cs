using System.CommandLine;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;
using ImageConverter = Oire.Sic.Services.ImageConverter;
using Serilog;
using Serilog.Formatting.Compact;
using App = Oire.Sic.Utils.Constants.App;
using LogConstants = Oire.Sic.Utils.Constants.Logging;
using LogLevel = Serilog.Events.LogEventLevel;

namespace Oire.Sic;

internal static class Program {
    [STAThread]
    static int Main(string[] args) {
        ConfigureLogging();

        try {
            if (args.Length > 0) {
                return RunCli(args);
            }

            ApplicationConfiguration.Initialize();
            Config.Load();
            Localization.SetLanguage(Config.General.Language);
#if DEBUG
            Log.Debug("App Startup: Config loaded");
#endif

            Application.Run(new MainWindow());
            return 0;
        } catch (Exception ex) {
            Log.Fatal("App Startup: Unable to initialize application: {0}", ex.Message);

            if (args.Length > 0) {
                Console.Error.WriteLine(_("Fatal error: {0}", ex.Message));
                return 1;
            }

            DialogResult msg = MessageBox.Show(_("Unable to start the program up. Please contact the developer."), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (msg == DialogResult.OK) {
                System.Windows.Forms.Application.Exit();
            }

            return 1;
        } finally {
            Log.CloseAndFlush();
        }
    }

    private static int RunCli(string[] args) {
        var inputOption = new Option<string>("--input", "-i") { Required = true, Description = _("Path to the source image file") };
        var outputOption = new Option<string>("--output", "-o") { Required = true, Description = _("Path for the converted image (format inferred from extension)") };
        var resizeOption = new Option<string?>("--resize", "-r") { Description = _("Resize dimensions as WxH, Wx, or xH (e.g. 128x128, 128x, x128)") };
        var cropOption = new Option<bool>("--crop", "-c") { Description = _("Use crop mode (scale to cover, then center-crop to exact dimensions)") };

        var rootCommand = new RootCommand(_("SIC! \u2014 Simple Image Converter")) {
            inputOption,
            outputOption,
            resizeOption,
            cropOption,
        };

        rootCommand.SetAction((Func<ParseResult, int>)(parseResult => {
            var input = parseResult.GetValue(inputOption)!;
            var output = parseResult.GetValue(outputOption)!;
            var resize = parseResult.GetValue(resizeOption);
            var crop = parseResult.GetValue(cropOption);

            if (!File.Exists(input)) {
                Console.Error.WriteLine(_("File not found: {0}", input));
                return 1;
            }

            var extension = Path.GetExtension(output).TrimStart('.').ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(extension)) {
                Console.Error.WriteLine(_("Output path must have a file extension (e.g. .jpg, .png)"));
                return 1;
            }

            var targetFormat = extension == "JPEG" ? "JPG" : extension == "TIF" ? "TIFF" : extension;
            var supportedFormats = ImageConverter.GetSupportedFormats();
            if (!supportedFormats.Any(f => f.Equals(targetFormat, StringComparison.OrdinalIgnoreCase))) {
                Console.Error.WriteLine(_("Unsupported format: {0}", extension));
                Console.Error.WriteLine(_("Supported formats: {0}", string.Join(", ", supportedFormats)));
                return 1;
            }

            int? width = null, height = null;
            var resizeMode = crop ? ResizeMode.Crop : ResizeMode.KeepProportions;

            if (!string.IsNullOrWhiteSpace(resize)) {
                var normalized = resize.ToLowerInvariant();
                string[] parts;

                if (normalized.Contains('x')) {
                    parts = normalized.Split('x');
                    if (parts.Length != 2) {
                        Console.Error.WriteLine(_("Invalid resize format: {0}. Use WxH, Wx, xH, or W (e.g. 128x128, 128x, x128, 128)", resize));
                        return 1;
                    }
                } else {
                    parts = [normalized, ""];
                }

                var hasWidth = int.TryParse(parts[0], out var w) && w >= 1;
                var hasHeight = int.TryParse(parts[1], out var h) && h >= 1;

                if (!hasWidth && !string.IsNullOrEmpty(parts[0])) {
                    Console.Error.WriteLine(_("Invalid width value: {0}", parts[0]));
                    return 1;
                }

                if (!hasHeight && !string.IsNullOrEmpty(parts[1])) {
                    Console.Error.WriteLine(_("Invalid height value: {0}", parts[1]));
                    return 1;
                }

                if (!hasWidth && !hasHeight) {
                    Console.Error.WriteLine(_("Invalid resize format: {0}. At least one dimension is required.", resize));
                    return 1;
                }

                if (hasWidth)
                    width = w;
                if (hasHeight)
                    height = h;
            }

            if (crop && (!width.HasValue || !height.HasValue)) {
                Console.Error.WriteLine(_("Crop mode requires both width and height (e.g. --resize 128x128 --crop)"));
                return 1;
            }

            try {
                var item = ImageConverter.LoadFromFile(input);

                var dir = Path.GetDirectoryName(output);
                if (dir != null && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }

                ImageConverter.Convert(item, targetFormat, output, width, height, resizeMode);
                Console.WriteLine(_("Converted: {0}", output));
                return 0;
            } catch (Exception ex) {
                Console.Error.WriteLine(_("Conversion failed: {0}", ex.Message));
                return 1;
            }
        }));

        return rootCommand.Parse(args).Invoke();
    }

    private static void ConfigureLogging() {
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                LogConstants.GenericFile,
                rollOnFileSizeLimit: true
            )
            .WriteTo.File(
                LogConstants.GenericFileShort,
                rollOnFileSizeLimit: true,
                outputTemplate: LogConstants.OutputTemplateShort
            )
            .WriteTo.File(
                LogConstants.ErrorsFile,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogLevel.Warning
            )
            .WriteTo.File(
                LogConstants.ErrorsFileShort,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogLevel.Warning,
                outputTemplate: LogConstants.OutputTemplateShort
            )
            .WriteTo.File(
                new CompactJsonFormatter(),
                LogConstants.TelemetryFile,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogLevel.Information
            )
            .CreateLogger();
#else
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                LogConstants.GenericFile,
                rollOnFileSizeLimit: true
            )
            .WriteTo.File(
                LogConstants.GenericFileShort,
                rollOnFileSizeLimit: true,
                outputTemplate: LogConstants.OutputTemplateShort
            )
            .WriteTo.File(
                LogConstants.ErrorsFile,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogLevel.Warning
            )
            .WriteTo.File(
                LogConstants.ErrorsFileShort,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: LogLevel.Warning,
                outputTemplate: LogConstants.OutputTemplateShort
            )
            .CreateLogger();
#endif
    }
}
