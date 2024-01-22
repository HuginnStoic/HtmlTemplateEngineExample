using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HtmlTemplateEngine.Metadata;
using HtmlTemplateEngine.Models;
using HtmlTemplateEngine.Services;

namespace HtmlTemplateEngine;

public partial class HtmlTemplate
{
    /// <summary>
    /// Parses text representing HTML.
    /// </summary>
    /// <param name="data"> HTML template text. </param>
    /// <returns> Container with deserialized HTML template.  </returns>
    public static HtmlTemplate Parse([StringSyntax("HTML")] string data)
    {
        var chars = data.AsSpan();
        var expectedByteCount = Encoding.UTF8.GetByteCount(chars);
        var utf8Bytes = ArrayPool<byte>.Shared.Rent(expectedByteCount);

        HtmlTemplate template = new();
        
        try
        {
            var actualByteCount = Encoding.UTF8.GetBytes(chars, utf8Bytes);
            var source = utf8Bytes.AsMemory(0, actualByteCount);
        
            Feed(template, source);
            template._rentedBytes = utf8Bytes;
            template._source = source;
            
            return template;
        }
        catch
        {
            utf8Bytes.AsSpan(0, expectedByteCount).Clear();
            ArrayPool<byte>.Shared.Return(utf8Bytes);
            throw;
        }
    }
    
    private static void Feed(IAddable baseContainer, [StringSyntax("HTML")] ReadOnlyMemory<byte> data)
    {
        ITemplateNode? node = default;
        Stack<OperationNode> operations = new();
        var container = baseContainer;
        var dataSpan = data.Span;
        var reader = new HtmlTemplateReader(dataSpan);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case TokenType.Text:
                    if (node is null) node = new HtmlNode(reader.Index);
                    else node.Extend();

                    break;
                case TokenType.StartScope:
                    if (node is not null)
                    {
                        container.Add(node);
                        node = null;
                    }

                    break;
                case TokenType.EndScope:
                    switch (node)
                    {
                        case null: throw new UnreachableException();
                        case OperationNode operationNode:
                            var checkResult = operationNode.Check(dataSpan);

                            switch (checkResult)
                            {
                                case OperationState.Started:
                                    operations.Push(operationNode);
                                    container = operationNode;
                                    break;
                                case OperationState.Finished:
                                    if (!operations.TryPop(out operationNode!))
                                        throw new Exception();

                                    container = operations.TryPeek(out var prevOperation)
                                        ? prevOperation
                                        : baseContainer;
                            
                                    container.Add(operationNode);
                                    break;
                                case OperationState.Wrong: throw new Exception();
                                case OperationState.Unknown: throw new UnreachableException();
                                default: throw new ArgumentOutOfRangeException();
                            }
                            
                            break;
                        default:
                            container.Add(node);
                            break;
                    }
                    
                    node = null;

                    break;
                case TokenType.Operation:
                    if (node is not null)
                    {
                        node.Extend();
                    }
                    else node = new OperationNode(reader.Index);

                    break;
                case TokenType.Property:
                    if (node is null) node = new PropertyNode(reader.Index);
                    else node.Extend();

                    break;
                case TokenType.Escape:
                    if (node is null) throw new UnreachableException();
                    node.Extend();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (node is null) return;
        container.Add(node);
    }
}