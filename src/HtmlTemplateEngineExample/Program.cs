using HtmlTemplateEngine;

const string templateKey = "templateFilePath";
const string dataKey = "dataFilePath";
const string outputKey = "outputFilePath";

string? templatePath = default;
string? dataPath = default;
string? outputPath = default;

foreach (var arg in args)
{
    var meta = arg.Split('=');
    if (meta.Length != 2) continue;

    var propName = meta[0];
    var propValue = meta[1];
    
    if (propName.Contains(templateKey))
        templatePath = propValue;
    else if (propName.Contains(dataKey))
        dataPath = propValue;
    else if (propName.Contains(outputKey))
        outputPath = propValue;
}

if (string.IsNullOrWhiteSpace(templatePath)) throw new ArgumentNullException(nameof(args), "Template file path hasn't been set");
if (string.IsNullOrWhiteSpace(dataPath)) throw new ArgumentNullException(nameof(args), "Data file path hasn't been set");
if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(args), "Output path hasn't been set");;

if (templatePath == dataPath) throw new ArgumentException("Template file path cannot be equal data file path", nameof(args));

if (!File.Exists(templatePath)) throw new ArgumentException("Template file hasn't been found", nameof(args));
if (!File.Exists(dataPath)) throw new ArgumentException("Data file hasn't been found", nameof(args));
if (Directory.GetParent(outputPath)?.Exists != true) throw new ArgumentException("Directory for output hasn't been found", nameof(args));

var getTemplate = File.ReadAllTextAsync(templatePath);
var getJson = File.ReadAllTextAsync(dataPath);

using var htmlTemplate = HtmlTemplate.Parse(await getTemplate);
await File
    .WriteAllTextAsync(
        outputPath, 
        htmlTemplate.Build(await getJson));