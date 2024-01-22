using HtmlTemplateEngine.Metadata;
using HtmlTemplateEngine.Models;

namespace HtmlTemplateEngine.Services;

internal ref struct HtmlTemplateReader(ReadOnlySpan<byte> data)
{
    public int Index { get; private set; } = -1;
    public TokenType TokenType { get; private set; } = TokenType.Text;
    private readonly ReadOnlySpan<byte> _data = data;

    public bool Read()
    {
        Again:
        Index++;
        
        if (Index == _data.Length) return false;

        var symbol = _data[Index];

        switch (TokenType)
        {
            case TokenType.Escape:
                TokenType = TokenType.Text;
                return true;
            case TokenType.StartScope:
                TokenType = symbol switch
                {
                    Symbol.Percent => TokenType.Operation,
                    Symbol.OpenBrace => TokenType.Property,
                    _ => throw new HtmlParseException(Index)
                };
                Index++;
                return true;
            case TokenType.Text:
                TokenType = symbol switch
                {
                    Symbol.Backslash => TokenType.Escape,
                    Symbol.OpenBrace => TokenType.StartScope,
                    Symbol.Percent or Symbol.CloseBrace => throw new HtmlParseException(Index),
                    _ => TokenType
                };

                return true;
            case TokenType.Operation:
                if (symbol != Symbol.Percent) return true;
                
                TokenType = TokenType.EndScope;
                return true;
            case TokenType.Property:
                if (symbol == Symbol.CloseBrace) TokenType = TokenType.EndScope;
                return true;
            case TokenType.EndScope:
                if (symbol != Symbol.CloseBrace) throw new HtmlParseException(Index);
                TokenType = TokenType.Text;
                goto Again;
            default:
                return true;
        }
    }
}