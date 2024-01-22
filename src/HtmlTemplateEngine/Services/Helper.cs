using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using HtmlTemplateEngine.Metadata;

namespace HtmlTemplateEngine.Services;

internal static class Helper
{
    public static ReadOnlySpan<char> Compact(this string value)
    {
        var span = value.AsSpan();
        Span<char> result = new char[span.Length];

        var index = 0;
        var flag = false;
        
        var prev = 0;
        for (var i = 0; i < span.Length; i++)
        {
            var symbol = span[i];

            if (!flag)
            {
                if (symbol is not SymbolChar.LineFeed)
                {
                    result[index] = symbol;
                    index++;
                    continue;
                }
                
                flag = true;
                prev = i;

                if (i != 0 && span[i - 1] is SymbolChar.CarriageReturn)
                {
                    prev--;
                    index--;
                    result[index] = char.MinValue;
                }
                continue;
            }

            switch (symbol)
            {
                case SymbolChar.Space or SymbolChar.CarriageReturn:
                    continue;
                case SymbolChar.LineFeed:
                {
                    flag = false;
                    continue;
                }
            }

            flag = false;

            if (span[prev - 1] is SymbolChar.CarriageReturn)
            {
                prev--;
                index--;
            }
            
            for (var j = prev; j <= i; j++)
            {
                result[index] = span[j];
                index++;
            }
        }

        return result[..index];
    }
    
    public static ReadOnlySpan<char> RemoveWhiteSpace(this string val)
    {
        var tempQualifier = val.AsSpan();
        int start;
        int end;
        
        for (start = 0; start < tempQualifier.Length; start++)
        {
            var symbol = tempQualifier[start];
            
            if (symbol is not (SymbolChar.Space or SymbolChar.CarriageReturn or SymbolChar.LineFeed or SymbolChar.Tab))
                break;
        }
        
        for (end = tempQualifier.Length - 1; end >= start; end--)
        {
            var symbol = tempQualifier[end];
            
            if (symbol is not (SymbolChar.Space or SymbolChar.CarriageReturn or SymbolChar.LineFeed or SymbolChar.Tab))
                break;
        }

        return tempQualifier.Slice(start, end - start + 1);
    }

    public static ReadOnlySpan<byte> RemoveWhiteSpace(this ReadOnlySpan<byte> span)
    {
        int start;
        int end;
        
        for (start = 0; start < span.Length; start++)
        {
            var symbol = span[start];
            if (symbol is not (Symbol.Space or Symbol.CarriageReturn or Symbol.LineFeed or Symbol.Tab))
                break;
        }
        
        for (end = span.Length - 1; end >= start; end--)
        {
            var symbol = span[end];
            if (symbol is not (Symbol.Space or Symbol.CarriageReturn or Symbol.LineFeed or Symbol.Tab))
                break;
        }

        return span.Slice(start, end - start + 1);
    }

    public static bool GetPropertyByCompositeName(
        this JsonObject json,
        string name,
        [MaybeNullWhen(false)]out JsonNode property)
    {
        var nameParts = name.Split('.');

        if (nameParts.Length == 1 &&
            json.TryGetPropertyValue(nameParts[0], out property!))
            return true;
            
        var currentJson = json;

        for (var i = 0; i < nameParts.Length; i++)
        {
            var propertyPart = nameParts[i];

            if (!currentJson.TryGetPropertyValue(propertyPart, out property!))
                return false;

            if (i == nameParts.Length - 1)
                return true;
            
            if (property is JsonObject currentJsonObject) currentJson = currentJsonObject;
            else return default;
        }

        property = default;
        return false;
    }
}