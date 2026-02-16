using System.CommandLine;
using Oire.Sic.Utils;
using ImageConverter = Oire.Sic.Services.ImageConverter;
using Serilog;
using Serilog.Formatting.Compact;
using App = Oire.Sic.Utils.Constants.App;
using LogConstants = Oire.Sic.Utils.Constants.Logging;
using LogLevel = Serilog.Events.LogEventLevel;

namespace Oire.Sic;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ConfigureLogging();

        try
        {
            if (args.Length > 0)
            {
                return RunCli(args);
            }

            ApplicationConfiguration.Initialize();
            Config.Load();
#if DEBUG
            Log.Debug("App Startup: Config loaded");
#endif

            Application.Run(new MainWindow());
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal("App Startup: Unable to initialize application: {0}", ex.Message);

            if (args.Length > 0)
            {
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                return 1;
            }

            DialogResult msg = MessageBox.Show("Unable to start the program up. Please contact the developer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (msg == DialogResult.OK)
            {
                System.Windows.Forms.Application.Exit();
            }

            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static int RunCli(string[] args)
    {
        var inputOption = new Option<string>("--input", "-i") { Required = true, Description = "Path to the source image file" };
        var outputOption = new Option<string>("--output", "-o") { Required = true, Description = "Path for the converted image (format inferred from extension)" };
        var resizeOption = new Option<string?>("--resize", "-r") { Description = "Resize dimensions as WxH (e.g. 128x128)" };

        var rootCommand = new RootCommand("SIC! — Simple Image Convertor") {
            inputOption,
            outputOption,
            resizeOption,
        };

        rootCommand.SetAction((Func<ParseResult, int>)(parseResult =>
        {
            var input = parseResult.GetValue(inputOption)!;
            var output = parseResult.GetValue(outputOption)!;
            var resize = parseResult.GetValue(resizeOption);

            if (!File.Exists(input))
            {
                Console.Error.WriteLine($"File not found: {input}");
                return 1;
            }

            var extension = Path.GetExtension(output).TrimStart('.').ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                Console.Error.WriteLine("Output path must have a file extension (e.g. .jpg, .png)");
                return 1;
            }

            var targetFormat = extension == "JPEG" ? "JPG" : extension == "TIF" ? "TIFF" : extension;
            var supportedFormats = ImageConverter.GetSupportedFormats();
            if (!supportedFormats.Any(f => f.Equals(targetFormat, StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"Unsupported format: {extension}");
                Console.Error.WriteLine($"Supported formats: {string.Join(", ", supportedFormats)}");
                return 1;
            }

            int? width = null, height = null;
            if (!string.IsNullOrWhiteSpace(resize))
            {
                var parts = resize.Split('x', 'X');
                if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                {
                    width = w;
                    height = h;
                }
                else
                {
                    Console.Error.WriteLine($"Invalid resize format: {resize}. Use WxH (e.g. 128x128)");
                    return 1;
                }
            }

            try
            {
                var item = ImageConverter.LoadFromFile(input);

                var dir = Path.GetDirectoryName(output);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                ImageConverter.Convert(item, targetFormat, output, width, height);
                Console.WriteLine($"Converted: {output}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Conversion failed: {ex.Message}");
                return 1;
            }
        }));

        return rootCommand.Parse(args).Invoke();
    }

    private static void ConfigureLogging()
    {
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
