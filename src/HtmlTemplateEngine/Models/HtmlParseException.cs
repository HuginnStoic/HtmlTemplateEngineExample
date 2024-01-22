namespace HtmlTemplateEngine.Models;

public sealed class HtmlParseException(int index)
    : Exception($"Special symbol has been found on position {index} in wrong case. Please, use \"\\\" to escape it.");