using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace CodeGenEngine
{
    internal class DefStructure
    {
        internal string RootPath { get; private set; }
        internal string FilePath { get; private set; }
        internal string RelativePath { get; private set; }
        internal string Filename { get; private set; }

        internal string Namespace { get; private set; }
        internal string ClassName { get; private set; }
        internal string FullName { get; private set; }
        internal string[] Usings { get; private set; }
        internal SyntaxKind[] Modifiers { get; private set; }
        internal TypeMember[] Members { get; private set; }

        private ClassDeclarationSyntax _classdecl;

        public DefStructure(string rootPath, string filepath, string @namespace, string[] usings, ClassDeclarationSyntax classdecl)
        {
            RootPath = rootPath;
            Filename = Path.GetFileName(filepath);
            FilePath = Path.GetDirectoryName(filepath);
            RelativePath = FilePath.Substring(RootPath.Length + 1);

            Namespace = @namespace;
            Usings = usings;

            _classdecl = classdecl;

            ClassName = classdecl.Identifier.ToString();
            FullName = Namespace.Attach(".", ClassName);
            Modifiers = GetModifiersKind(classdecl.Modifiers);

            var members = new List<TypeMember>();
            foreach (var memberDecl in classdecl.Members)
            {
                if (memberDecl is PropertyDeclarationSyntax)
                {
                    var propDecl = memberDecl as PropertyDeclarationSyntax;
                    AttributeSyntax jsonIgnore;
                    AttributeSyntax jsonProperty;
                    GetAttributes(propDecl.AttributeLists, out jsonIgnore, out jsonProperty);
                    if (jsonIgnore != null)
                        continue;
                    var modifiers = GetModifiersKind(propDecl.Modifiers);
                    string name = propDecl.Identifier.ToString();
                    string jsonName;
                    if (!ResolvePropertyName(jsonProperty, modifiers, name, out jsonName))
                        continue;
                    members.Add(new TypeMember(name, jsonName, propDecl.Type));
                }
                else if (memberDecl is FieldDeclarationSyntax)
                {
                    var fieldDecl = memberDecl as FieldDeclarationSyntax;
                    AttributeSyntax jsonIgnore;
                    AttributeSyntax jsonProperty;
                    GetAttributes(fieldDecl.AttributeLists, out jsonIgnore, out jsonProperty);
                    if (jsonIgnore != null)
                        continue;
                    var modifiers = GetModifiersKind(fieldDecl.Modifiers);
                    foreach (var variable in fieldDecl.Declaration.Variables)
                    {
                        string name = variable.Identifier.ToString();
                        string jsonName;
                        if (!ResolvePropertyName(jsonProperty, modifiers, name, out jsonName))
                            continue;
                        members.Add(new TypeMember(name, jsonName, fieldDecl.Declaration.Type));
                    }
                }
            }
            Members = members.ToArray();
        }

        internal static bool ResolvePropertyName(AttributeSyntax jsonProperty, SyntaxKind[] modifiers, string name, out string jsonName)
        {
            if (jsonProperty == null)
            {
                if (modifiers.Any(m => m == SyntaxKind.PrivateKeyword || m == SyntaxKind.ProtectedKeyword))
                {
                    jsonName = null;
                    return false;
                }
                jsonName = ResolvePropertyName(name);
                return true;
            }
            else
            {
                if ((jsonProperty.ArgumentList?.Arguments.Count ?? 0) == 1)
                {
                    var argument = jsonProperty.ArgumentList.Arguments[0];
                    var expression = argument.Expression;
                    if (expression.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        var expr = expression as LiteralExpressionSyntax;
                        jsonName = expr.Token.ValueText;
                        return true;
                    }
                }
                jsonName = ResolvePropertyName(name);
                return true;
            }
        }

        internal static string ResolvePropertyName(string name)
        {
            var sb = new StringBuilder();
            var checkUnderline = true;
            var checkFirstCharacter = true;
            foreach (var c in name)
                if (!checkUnderline || c != '_')
                {
                    checkUnderline = false;
                    if (checkFirstCharacter)
                    {
                        checkFirstCharacter = false;
                        sb.Append(Char.ToLowerInvariant(c));
                    }
                    else
                        sb.Append(c);
                }

            return sb.ToString();
        }

        internal static SyntaxKind[] GetModifiersKind(SyntaxTokenList list)
        {
            return list.Select(m => m.Kind()).ToArray();
        }

        private void GetAttributes(SyntaxList<AttributeListSyntax> attributeLists, out AttributeSyntax jsonIgnore, out AttributeSyntax jsonProperty)
        {
            bool ignore = false;
            bool proper = false;
            jsonIgnore = null;
            jsonProperty = null;

            foreach (var list in attributeLists)
                foreach (var attribute in list.Attributes)
                {
                    if (ignore && proper)
                        return;
                    else if (!ignore && attribute.Name.ToString() == "JsonIgnore")
                    {
                        jsonIgnore = attribute;
                        ignore = true;
                    }
                    else if (!proper && attribute.Name.ToString() == "JsonProperty")
                    {
                        jsonProperty = attribute;
                        proper = true;
                    }
                }
        }
    }
}
