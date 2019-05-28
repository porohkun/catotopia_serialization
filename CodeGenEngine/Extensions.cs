using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CodeGenEngine
{
    internal static class Extensions
    {
        internal static IEnumerable<UsingDirectiveSyntax> With(this IEnumerable<UsingDirectiveSyntax> items, params UsingDirectiveSyntax[] additionalItems)
        {
            return items.With((IEnumerable<UsingDirectiveSyntax>)additionalItems);
        }

        internal static IEnumerable<UsingDirectiveSyntax> With(this IEnumerable<UsingDirectiveSyntax> items, IEnumerable<UsingDirectiveSyntax> additionalItems)
        {
            return items.Union(additionalItems).Distinct(new PartComparer<UsingDirectiveSyntax>(u => u.Name));
        }


        internal static IEnumerable<SyntaxToken> With(this IEnumerable<SyntaxToken> items, params SyntaxToken[] additionalItems)
        {
            return items.With((IEnumerable<SyntaxToken>)additionalItems);
        }

        internal static IEnumerable<SyntaxToken> With(this IEnumerable<SyntaxToken> items, IEnumerable<SyntaxToken> additionalItems)
        {
            return items.Union(additionalItems).Distinct(new PartComparer<SyntaxToken>(u => u.Kind()));
        }


        internal static string Attach(this string a, string separator, string b)
        {
            if (string.IsNullOrWhiteSpace(a))
                if (string.IsNullOrWhiteSpace(b))
                    return string.Empty;
                else
                    return b;
            else if (string.IsNullOrWhiteSpace(b))
                return a;
            else
                return $"{a}{separator}{b}";
        }


        internal static IEnumerable<T> Union<T>(this IEnumerable<T> items, params T[] additionalItems)
        {
            foreach (var item in items)
                yield return item;
            foreach (var item in additionalItems)
                yield return item;
        }

    }
}
