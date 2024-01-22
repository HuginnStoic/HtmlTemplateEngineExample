using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using HtmlTemplateEngine;

BenchmarkRunner.Run<Benchmark>();

[MemoryDiagnoser]
public class Benchmark
{
    private const string Template =
        """
        <ul id="products">
            {% for product in products %}
             <li>
                 <h2>{{product.name}}</h2>
                 Only {{product.price | price }}
                 {{product.description | paragraph }}
             </li>
            {% endfor %}
        </ul>
        """;
    
    private const string Json =
        """
        {
          "products": [
            {
              "name": "Apple",
              "price": 329,
              "description": "flat-out fun"
            },
            {
              "name": "Orange",
              "price": 25,
              "description": "colorful"
            },
            {
              "name": "Banana",
              "price": 99,
              "description": "peel it"
            }
          ]
        }
        """;
    
    [Benchmark]
    [SuppressMessage(
        "Performance",
        "CA1822",
        Justification = "Benchmark doesn't accept static method")]
    public string Test()
    {
        using var template = HtmlTemplate.Parse(Template);
        return template.Build(Json);
    }
}