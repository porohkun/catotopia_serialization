using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGenEngine
{
    internal class TypeMember
    {
        internal string Name { get; private set; }
        internal string JsonName { get; private set; }
        internal TypeSyntax Type { get; private set; }
        internal TypeKind Kind { get; private set; }

        internal enum TypeKind
        {
            Simple,
            Def,
            Array,
            Array2,
            Array3
        }

        internal TypeMember(string name, string jsonName, TypeSyntax typeSyntax)
        {
            Name = name;
            JsonName = jsonName;
            Type = typeSyntax;

            if (typeSyntax is QualifiedNameSyntax)
                Kind = (typeSyntax as QualifiedNameSyntax).Right.ToString().EndsWith("Def") ? TypeKind.Def : TypeKind.Simple;
            else if (typeSyntax is PredefinedTypeSyntax)
                Kind = (typeSyntax as PredefinedTypeSyntax).ToString().EndsWith("Def") ? TypeKind.Def : TypeKind.Simple;
            else if (typeSyntax is ArrayTypeSyntax)
            {
                var type = (typeSyntax as ArrayTypeSyntax);
                switch (type.RankSpecifiers[0].Rank)
                {
                    case 1: Kind = TypeKind.Array; break;
                    case 2: Kind = TypeKind.Array2; break;
                    case 3: Kind = TypeKind.Array3; break;
                }

            }
        }
    }
}
