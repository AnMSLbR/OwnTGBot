using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;

public static class TelegramMarkdownConverter
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly HashSet<char> _reserved =
        new HashSet<char> { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };

    public static string Convert(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, _pipeline);
        var sb = new StringBuilder();

        foreach (var block in document)
        {
            sb.Append(ProcessBlock(block));
            sb.Append("\n\n");
        }

        return sb.ToString().Trim();
    }

    private static string ProcessBlock(Block block)
    {
        return block switch
        {
            ParagraphBlock paragraph => ProcessParagraph(paragraph),
            FencedCodeBlock codeBlock => ProcessFencedCodeBlock(codeBlock),
            CodeBlock codeBlock => ProcessCodeBlock(codeBlock),
            ListBlock list => ProcessList(list),
            HeadingBlock heading => ProcessHeading(heading),
            _ => string.Empty
        };
    }

    private static string ProcessParagraph(ParagraphBlock paragraph)
    {
        var sb = new StringBuilder();
        foreach (var inline in paragraph.Inline)
        {
            sb.Append(ProcessInline(inline));
        }
        return sb.ToString();
    }

    private static string ProcessFencedCodeBlock(FencedCodeBlock codeBlock)
    {
        var code = codeBlock.Lines.ToString().TrimEnd();
        return $"```\n{Escape(code)}\n```";
    }

    private static string ProcessCodeBlock(CodeBlock codeBlock)
    {
        var code = codeBlock.Lines.ToString().TrimEnd();
        return $"```\n{Escape(code)}\n```";
    }

    private static string ProcessList(ListBlock list)
    {
        var sb = new StringBuilder();
        int index = 1;
        foreach (var item in list)
        {
            var li = (ListItemBlock)item;
            sb.Append(list.IsOrdered ? $"{index}. " : "• ");
            foreach (var block in li)
            {
                sb.Append(ProcessBlock(block));
            }
            sb.Append("\n");
            index++;
        }
        return sb.ToString();
    }

    private static string ProcessHeading(HeadingBlock heading)
    {
        var sb = new StringBuilder();
        foreach (var inline in heading.Inline)
        {
            sb.Append(ProcessInline(inline));
        }
        return $"*{sb}*";
    }

    private static string ProcessInline(Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => Escape(literal.Content.ToString()),
            EmphasisInline emph when emph.DelimiterChar == '*' =>
                $"*{string.Concat(emph.Select(ProcessInline))}*",
            EmphasisInline emph when emph.DelimiterChar == '_' =>
                $"_{string.Concat(emph.Select(ProcessInline))}_",
            CodeInline code => $"`{Escape(code.Content)}`",
            LinkInline link when link.IsImage => string.Empty,
            LinkInline link => $"[{string.Concat(link.Select(ProcessInline))}]({Escape(link.Url)})",
            _ => string.Empty
        };
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (_reserved.Contains(c))
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
