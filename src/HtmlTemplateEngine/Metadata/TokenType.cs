namespace HtmlTemplateEngine.Metadata;

internal enum TokenType
{
    Text = 0,
    StartScope,
    EndScope,
    Operation,
    Property,
    Escape
}