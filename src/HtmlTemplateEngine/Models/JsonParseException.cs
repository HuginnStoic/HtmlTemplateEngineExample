namespace HtmlTemplateEngine.Models;

public sealed class JsonParseException(string property)
    : Exception($"Property by name \"{property}\" hasn't been found in JSON-data");