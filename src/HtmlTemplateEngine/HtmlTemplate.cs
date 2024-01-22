using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlTemplateEngine.Models;
using HtmlTemplateEngine.Services;

namespace HtmlTemplateEngine;

/// <summary>
/// Represents container for HTML template.
/// </summary>
public sealed partial class HtmlTemplate : IDisposable, IAddable
{
    private readonly List<ITemplateNode> _nodes = [];
    private byte[]? _rentedBytes;
    private ReadOnlyMemory<byte> _source;

    /// <summary>
    /// Builds HTML by template and input JSON.
    /// </summary>
    /// <param name="jsonData"> JSON text as source of template. </param>
    /// <returns></returns>
    /// <exception cref="JsonException"> JSON does not represent a valid single JSON value. </exception>
    /// <exception cref="ArgumentNullException"> JSON is null. </exception>
    public string Build([StringSyntax(StringSyntaxAttribute.Json)] string jsonData)
    {
        var json = JsonNode.Parse(jsonData);
        
        if (json is null) throw new ArgumentNullException("Invalid JSON");
        if (json is not JsonObject jsonObject) throw new JsonException("JSON has to be object");

        StringBuilder result = new(_source.Length);
        foreach (var node in _nodes) result.Append(node.ToString(_source, jsonObject));
        
        return result.ToString().Compact().ToString();
    }
    
    public void Dispose()
    {
        var length = _source.Length;

        if (length == 0) return;
        
        _source = ReadOnlyMemory<byte>.Empty;

        if (_rentedBytes is null) 
            return;
        
        var extraRentedBytes = Interlocked.Exchange(ref _rentedBytes, null);
        extraRentedBytes.AsSpan(0, length).Clear();
        ArrayPool<byte>.Shared.Return(extraRentedBytes);
    }

    void IAddable.Add(ITemplateNode node) => _nodes.Add(node);
}