using System.CommandLine;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;
using ImageConverter = Oire.Sic.Services.ImageConverter;
using Serilog;
using Serilog.Formatting.Compact;
using App = Oire.Sic.Utils.Constants.App;
using ExitCode = Oire.Sic.Utils.Constants.ExitCode;
using LogConstants = Oire.Sic.Utils.Constants.Logging;
using LogLevel = Serilog.Events.LogEventLevel;

namespace Oire.Sic;

internal static class Program {
    [STAThread]
    static int Main(string[] args) {
        ConfigureLogging();

        AppDomain.CurrentDomain.UnhandledException += (_, e) => {
            if (e.ExceptionObject is Exception ex) {
                Log.Fatal(ex, "Unhandled exception on background thread");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) => {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        try {
            if (args.Length > 0) {
                return RunCli(args);
            }

            ApplicationConfiguration.Initialize();
            Config.Load();
            Localization.SetLanguage(Config.General.Language);
            EnsureOutputFolderExists(showGui: true);
#if DEBUG
            Log.Debug("App Startup: Config loaded");
#endif

            Application.Run(new MainWindow());
            return ExitCode.Success;
        } catch (Exception ex) {
            Log.Fatal("App Startup: Unable to initialize application: {0}", ex.Message);

            if (args.Length > 0) {
                Console.Error.WriteLine(_("Fatal error: {0}", ex.Message));
                return ExitCode.Error;
            }

            DialogResult msg = MessageBox.Show(_("Unable to start the program up. Please contact the developer."), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (msg == DialogResult.OK) {
                Environment.Exit(ExitCode.Error);
            }

            return ExitCode.Error;
        } finally {
            Log.CloseAndFlush();
        }
    }

    private static void EnsureOutputFolderExists(bool showGui) {
        var outputFolder = Config.General.OutputFolder;

        if (string.IsNullOrWhiteSpace(outputFolder)) {
            return;
        }

        // Default folder: always create silently
        if (outputFolder == App.DefaultOutputFolder) {
            try {
                Directory.CreateDirectory(outputFolder);
            } catch (Exception ex) {
                Log.Warning("Failed to create default output folder {Folder}: {Error}", outputFolder, ex.Message);
            }

            return;
        }

        // Custom folder exists — nothing to do
        if (Directory.Exists(outputFolder)) {
            return;
        }

        // Custom folder is gone — warn and reset
        Log.Warning("Custom output folder no longer exists: {Folder}", outputFolder);

        if (showGui) {
            MessageBox.Show(
                _("The output folder \"{0}\" no longer exists. The default folder will be used.", outputFolder),
                _("Output Folder Not Found"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        } else {
            Console.Error.WriteLine(
                _("Warning! The output folder \"{0}\" no longer exists. Using default folder.", outputFolder));
        }

        Config.General.OutputFolder = App.DefaultOutputFolder;
        Config.Save();

        try {
            Directory.CreateDirectory(App.DefaultOutputFolder);
        } catch (Exception ex) {
            Log.Warning("Failed to create default output folder {Folder}: {Error}", App.DefaultOutputFolder, ex.Message);
        }
    }

    private static int RunCli(string[] args) {
        Config.Load(isGui: false);
        Localization.SetLanguage(Config.General.Language);
        EnsureOutputFolderExists(showGui: false);

        var inputOption = new Option<string>("--input", "-i") { Required = true, Description = _("Path to the source image file or a URL") };
        var outputOption = new Option<string?>("--output", "-o") { Description = _("Path for the converted image (format inferred from extension)") };
        var formatOption = new Option<string?>("--format", "-f") { Description = _("Target format (e.g. jpg, png, webp). Used when --output is omitted.") };
        var resizeOption = new Option<string?>("--resize", "-r") { Description = _("Resize dimensions as WxH, Wx, or xH (e.g. 128x128, 128x, x128)") };
        var cropOption = new Option<bool>("--crop", "-c") { Description = _("Use crop mode (scale to cover, then center-crop to exact dimensions)") };

        var rootCommand = new RootCommand(_("SIC! — Simple Image Converter")) {
            inputOption,
            outputOption,
            formatOption,
            resizeOption,
            cropOption,
        };

        rootCommand.SetAction((Func<ParseResult, int>)(parseResult => {
            var input = parseResult.GetValue(inputOption)!;
            var output = parseResult.GetValue(outputOption);
            var format = parseResult.GetValue(formatOption);
            var resize = parseResult.GetValue(resizeOption);
            var crop = parseResult.GetValue(cropOption);

            var isUrl = Uri.TryCreate(input, UriKind.Absolute, out var inputUri)
                && (inputUri.Scheme == Uri.UriSchemeHttp || inputUri.Scheme == Uri.UriSchemeHttps);

            if (!isUrl && !File.Exists(input)) {
                Log.Error("CLI: Input file not found: {Input}", input);
                Console.Error.WriteLine(_("File not found: {0}", input));
                return ExitCode.Error;
            }

            string targetFormat;
            if (!string.IsNullOrWhiteSpace(output)) {
                var extension = Path.GetExtension(output).TrimStart('.').ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(extension)) {
                    Log.Error("CLI: Output path has no file extension: {Output}", output);
                    Console.Error.WriteLine(_("Output path must have a file extension (e.g. .jpg, .png)"));
                    return ExitCode.Error;
                }
                targetFormat = extension == "JPEG" ? "JPG" : extension == "TIF" ? "TIFF" : extension;
            } else if (!string.IsNullOrWhiteSpace(format)) {
                targetFormat = format.TrimStart('.').ToUpperInvariant();
                targetFormat = targetFormat == "JPEG" ? "JPG" : targetFormat == "TIF" ? "TIFF" : targetFormat;
                var baseName = isUrl
                    ? Path.GetFileNameWithoutExtension(inputUri!.AbsolutePath)
                    : Path.GetFileNameWithoutExtension(input);
                var ext = ImageConverter.GetFileExtension(targetFormat);
                var outputFolder = Config.General.OutputFolder;
                if (string.IsNullOrWhiteSpace(outputFolder)) {
                    outputFolder = App.DefaultOutputFolder;
                }
                Directory.CreateDirectory(outputFolder);
                output = Path.Combine(outputFolder, baseName + ext);
            } else {
                Log.Error("CLI: Neither --output nor --format specified");
                Console.Error.WriteLine(_("Either --output or --format must be specified."));
                return ExitCode.Error;
            }

            var supportedFormats = ImageConverter.GetSupportedFormats();
            if (!supportedFormats.Any(f => f.Equals(targetFormat, StringComparison.OrdinalIgnoreCase))) {
                Log.Error("CLI: Unsupported format: {Format}", targetFormat);
                Console.Error.WriteLine(_("Unsupported format: {0}", targetFormat));
                Console.Error.WriteLine(_("Supported formats: {0}", string.Join(", ", supportedFormats)));
                return ExitCode.Error;
            }

            int? width = null, height = null;
            var resizeMode = crop ? ResizeMode.Crop : ResizeMode.KeepProportions;

            if (!string.IsNullOrWhiteSpace(resize)) {
                var normalized = resize.ToLowerInvariant();
                string[] parts;

                if (normalized.Contains('x')) {
                    parts = normalized.Split('x');
                    if (parts.Length != 2) {
                        Log.Error("CLI: Invalid resize format: {Resize}", resize);
                        Console.Error.WriteLine(_("Invalid resize format: {0}. Use WxH, Wx, xH, or W (e.g. 128x128, 128x, x128, 128)", resize));
                        return ExitCode.Error;
                    }
                } else {
                    parts = [normalized, ""];
                }

                var hasWidth = int.TryParse(parts[0], out var w) && w >= 1;
                var hasHeight = int.TryParse(parts[1], out var h) && h >= 1;

                if (!hasWidth && !string.IsNullOrEmpty(parts[0])) {
                    Log.Error("CLI: Invalid width value: {Width}", parts[0]);
                    Console.Error.WriteLine(_("Invalid width value: {0}", parts[0]));
                    return ExitCode.Error;
                }

                if (!hasHeight && !string.IsNullOrEmpty(parts[1])) {
                    Log.Error("CLI: Invalid height value: {Height}", parts[1]);
                    Console.Error.WriteLine(_("Invalid height value: {0}", parts[1]));
                    return ExitCode.Error;
                }

                if (!hasWidth && !hasHeight) {
                    Log.Error("CLI: Invalid resize format (no dimensions): {Resize}", resize);
                    Console.Error.WriteLine(_("Invalid resize format: {0}. At least one dimension is required.", resize));
                    return ExitCode.Error;
                }

                if (hasWidth)
                    width = w;
                if (hasHeight)
                    height = h;
            }

            if (crop && (!width.HasValue || !height.HasValue)) {
                Log.Error("CLI: Crop mode requires both width and height, got {Resize}", resize);
                Console.Error.WriteLine(_("Crop mode requires both width and height (e.g. --resize 128x128 --crop)"));
                return ExitCode.Error;
            }

            try {
                var item = isUrl
                    ? ImageConverter.LoadFromUrl(input).GetAwaiter().GetResult()
                    : ImageConverter.LoadFromFile(input);

                var dir = Path.GetDirectoryName(output!);
                if (dir != null && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }

                ImageConverter.Convert(item, targetFormat, output!, width, height, resizeMode);
                Console.WriteLine(_("Converted: {0}", output));
                return ExitCode.Success;
            } catch (Exception ex) {
                Log.Error("CLI: Conversion failed for {Input}: {Error}", input, ex.Message);
                Console.Error.WriteLine(_("Conversion failed: {0}", ex.Message));
                return ExitCode.Error;
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
