using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HtmlTemplateEngine;

namespace UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
internal sealed class BasicTests
{
    [TestCaseSource(typeof(TestCases))]
    public void TemplateParse_DoesNotThrow(TestCase testCase)
    {
        // Assert.
        
        // Act;
        void Test()
        {
            using var template = HtmlTemplate.Parse(testCase.Template);
        }

        // Assert.
        Assert.DoesNotThrow(Test);
    }

    [TestCaseSource(typeof(TestCases))]
    public void TemplateBuild_EqualToExpected(TestCase testCase)
    {
        // Assert.
        using var template = HtmlTemplate.Parse(testCase.Template);
        
        // Act;
        var html = template.Build(testCase.Json);

        // Assert.
        Assert.That(html, Is.EqualTo(Encoding.UTF8.GetString(testCase.ExpectedResult)));
    }
}

file sealed class TestCases : IEnumerable<TestCase>
{
    public IEnumerator<TestCase> GetEnumerator()
    {
        yield return new(
            """
            {% for product in products %}
                {% for item in product.items %}
                    {{item}}
                {% endfor %}
            {% endfor %}
            """,
            """
            {
              "products": [
                {"items": ["test1", "test2"]},
                {"items": ["test3", "test4"]}
              ]
            }
            """,
            [32,32,32,32,32,32,32,32,32,32,32,32,116,101,115,116,49,32,32,32,32,32,32,32,32,116,101,115,116,50,32,32,32,32,32,32,32,32,32,32,32,32,116,101,115,116,51,32,32,32,32,32,32,32,32,116,101,115,116,52]);
        yield return new(
            "{{product.name}}",
            """
            {
              "product": { "name": "test" }
            }
            """,
            [116,101,115,116]);
        yield return new(
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
            """,
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
            """,
            [60,117,108,32,105,100,61,34,112,114,111,100,117,99,116,115,34,62,13,10,32,32,32,32,32,60,108,105,62,13,10,32,32,32,32,32,32,32,32,32,60,104,50,62,65,112,112,108,101,60,47,104,50,62,13,10,32,32,32,32,32,32,32,32,32,79,110,108,121,32,51,50,57,13,10,32,32,32,32,32,32,32,32,32,102,108,97,116,45,111,117,116,32,102,117,110,13,10,32,32,32,32,32,60,47,108,105,62,32,32,32,32,32,60,108,105,62,13,10,32,32,32,32,32,32,32,32,32,60,104,50,62,79,114,97,110,103,101,60,47,104,50,62,13,10,32,32,32,32,32,32,32,32,32,79,110,108,121,32,50,53,13,10,32,32,32,32,32,32,32,32,32,99,111,108,111,114,102,117,108,13,10,32,32,32,32,32,60,47,108,105,62,32,32,32,32,32,60,108,105,62,13,10,32,32,32,32,32,32,32,32,32,60,104,50,62,66,97,110,97,110,97,60,47,104,50,62,13,10,32,32,32,32,32,32,32,32,32,79,110,108,121,32,57,57,13,10,32,32,32,32,32,32,32,32,32,112,101,101,108,32,105,116,13,10,32,32,32,32,32,60,47,108,105,62,13,10,60,47,117,108,62]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal record struct TestCase(
    [StringSyntax("HTML")] string Template,
    [StringSyntax(StringSyntaxAttribute.Json)] string Json, 
    byte[] ExpectedResult);