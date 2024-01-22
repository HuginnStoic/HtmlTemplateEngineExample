using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlTemplateEngine.Metadata;
using HtmlTemplateEngine.Services;

namespace HtmlTemplateEngine.Models;

internal interface IAddable
{
    void Add(ITemplateNode container);
}

internal interface ITemplateNode
{
    void Extend();
    ReadOnlySpan<char> ToString(ReadOnlyMemory<byte> data, JsonObject json);
}

internal sealed class HtmlNode(int start) : ITemplateNode
{
    private int _length = 1;
    public void Extend() => _length++;

    public ReadOnlySpan<char> ToString(ReadOnlyMemory<byte> data, JsonObject _) =>
        Encoding
            .UTF8
            .GetString(data.Slice(start, _length).Span);
}

internal sealed partial class OperationNode(int openStart) : IAddable, ITemplateNode
{
    private int _openLength = 1;

    [GeneratedRegex(@"(?: *)for(?: *)(?:\w*)(?: *)in(?: *)(?:[\w.\d]*)(?: *)")] private static partial Regex Start();
    [GeneratedRegex("(?: *)endfor(?: *)")] private static partial Regex End();
    [GeneratedRegex(@"(?: *)for(?: *)(\w*)(?: *)in(?: *)([\w.\d]*)(?: *)")] private static partial Regex Command();
    
    private readonly List<ITemplateNode> _children = [];

    public OperationState Check(ReadOnlySpan<byte> data)
    {
        var start = data.Slice(openStart, _openLength);
        Span<char> charsStart = stackalloc char[start.Length];
        Encoding.UTF8.GetChars(start, charsStart);

        if (Start().EnumerateMatches(charsStart).MoveNext()) return OperationState.Started;
        if (End().EnumerateMatches(charsStart).MoveNext()) return OperationState.Finished;
        return OperationState.Wrong;
    }

    public void Extend() => _openLength++;

    public ReadOnlySpan<char> ToString(ReadOnlyMemory<byte> data, JsonObject json)
    {
        var start = Encoding
            .UTF8
            .GetString(data.Slice(openStart, _openLength).Span);

        var match = Command().Matches(start)[0];
        var itemName = match.Groups[1].Value;
        var loopName = match.Groups[2].Value;

        if (!json.GetPropertyByCompositeName(loopName, out var loopNode))
            throw new JsonException($"There isn't element with name \"{loopName}\"");

        if (loopNode is not JsonArray jsonArray)
            throw new JsonException($"Property \"{loopName}\" needs to be array");
        
        StringBuilder result = new(jsonArray.Count);
        result.Append(SymbolChar.LineFeed);
        foreach (var item in jsonArray)
        {
            if (item is null) throw new UnreachableException();
            
            if (!json.TryAdd(itemName, item.DeepClone()))
                throw new JsonException($"Ambiguous property name {itemName}");
            
            foreach (var node in _children) result.Append(node.ToString(data, json));
            json.Remove(itemName);
        }

        result.Append(SymbolChar.LineFeed);

        return result.ToString();
    }

    public void Add(ITemplateNode node) => _children.Add(node);
}

internal sealed class PropertyNode(int start) : ITemplateNode
{
    private int _length = 1;
    public void Extend() => _length++;

    public ReadOnlySpan<char> ToString(ReadOnlyMemory<byte> data, JsonObject json)
    {
        var propertyName = Encoding
            .UTF8
            .GetString(data.Slice(start, _length).Span.RemoveWhiteSpace());
        var variants = propertyName
            .Split('|')
            .Select(item => item.RemoveWhiteSpace().ToString());

        foreach (var variant in variants)
        {
            if (json.GetPropertyByCompositeName(variant, out var property))
                return property.ToString();
        }

        throw new JsonParseException(propertyName);
    }
}

internal enum OperationState
{
    Unknown,
    Started,
    Finished,
    Wrong
}