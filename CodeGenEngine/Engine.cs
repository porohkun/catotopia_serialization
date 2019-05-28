using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CodeGenEngine
{
    public class Engine
    {
        public string RootPath { get; private set; }
        public string CsprojPath { get; private set; }

        #region messages
        public event Action<MessageType, string> MessageSent;
        private void SendInfoMessage(string message) { MessageSent?.Invoke(MessageType.Info, message); }
        private void SendWarningMessage(string message) { MessageSent?.Invoke(MessageType.Warning, message); }
        private void SendErrorMessage(string message) { MessageSent?.Invoke(MessageType.Error, message); }
        #endregion

        const string _csprojNmsp = "http://schemas.microsoft.com/developer/msbuild/2003";
        const string _generatedDirName = "Generated";
        string[] _additionalDefUsings = new[]
        {
            "Newtonsoft.Json.Linq",
            "Catotopia.Shared"
        };
        SyntaxKind[] _publicOverride = new[]
        {
             SyntaxKind.PublicKeyword,
             SyntaxKind.OverrideKeyword
        };

        public Engine(string path)
        {
            CsprojPath = path;
            RootPath = Path.GetDirectoryName(path);
            if (!File.Exists(CsprojPath))
                throw new FileNotFoundException(CsprojPath);
            if (!Directory.Exists(RootPath))
                throw new DirectoryNotFoundException(RootPath);
        }

        public void Run()
        {
            SendInfoMessage($"Begin at '{RootPath}'");

            var csproj = XDocument.Parse(File.ReadAllText(CsprojPath));

            RemovePreviouslyGeneratedFilesFromProject(csproj);
            RemovePreviouslyGeneratedFilesFromDrive();

            var generatedFiles = new List<string>();

            var defStructs = new List<DefStructure>(GetDefStructures(Path.Combine(RootPath, "Defs")));

            foreach (var def in defStructs)
                CreateDefDeserializer(def, generatedFiles);

            AddGeneratedFilesToProject(csproj, generatedFiles);
            csproj.Save(CsprojPath);
        }

        private IEnumerable<DefStructure> GetDefStructures(string defsPath)
        {
            var defs = new List<DefStructure>();
            foreach (var filename in Directory.GetFiles(defsPath, "*.cs", SearchOption.AllDirectories))
            {
                SendInfoMessage($"    ======== {Path.GetFileNameWithoutExtension(filename)} ========");
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filename));
                var root = tree.GetCompilationUnitRoot();

                foreach (var def in GetDefStructures(filename, root.Members, root.Usings.Select(u => u.Name.ToString()).ToArray()))
                    yield return def;
            }
        }

        private IEnumerable<DefStructure> GetDefStructures(string filename, IEnumerable<MemberDeclarationSyntax> members, string[] usings, string @namespace = "")
        {
            foreach (var member in members)
            {
                if (member is NamespaceDeclarationSyntax)
                {
                    var nmspace = member as NamespaceDeclarationSyntax;
                    foreach (var def in GetDefStructures(filename, nmspace.Members,
                        usings.Union(nmspace.Usings.Select(u => u.Name.ToString())).ToArray(),
                        @namespace.Attach(".", nmspace.Name.ToString())))
                        yield return def;
                }
                else if (member is ClassDeclarationSyntax)
                {
                    var classdecl = member as ClassDeclarationSyntax;
                    yield return new DefStructure(RootPath, filename, @namespace, usings, classdecl);
                }
            }
        }

        private void CreateDefDeserializer(DefStructure def, List<string> generatedFiles)
        {
            if (def.Modifiers.Contains(SyntaxKind.AbstractKeyword))
                return;

            var statements = new List<StatementSyntax>();
            statements.Add(SyntaxFactory.ParseStatement("base.Fill(source,resources);"));
            foreach (var member in def.Members)
            {
                statements.Add(SyntaxFactory.ParseStatement($"{member.Name}=resources.GetFrom<{member.Type.ToString()}>(source,\"{member.JsonName}\");"));
            }

            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Fill")
                .AddModifiers(_publicOverride.Select(m => SyntaxFactory.Token(m)).ToArray())
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("source"))
                        .WithType(SyntaxFactory.ParseTypeName("JToken")),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("resources"))
                        .WithType(SyntaxFactory.ParseTypeName("IResourcesContainer")))
              .WithBody(SyntaxFactory.Block(statements));

            var classDeclaration = SyntaxFactory.ClassDeclaration(def.ClassName)
                .AddModifiers(def.Modifiers.Union(SyntaxKind.PartialKeyword).Distinct().Select(m => SyntaxFactory.Token(m)).ToArray())
                .AddMembers(methodDeclaration);

            var unit = SyntaxFactory.CompilationUnit(
                new SyntaxList<ExternAliasDirectiveSyntax>(),
                new SyntaxList<UsingDirectiveSyntax>(def.Usings.Union(_additionalDefUsings).Distinct().Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u)))),
                new SyntaxList<AttributeListSyntax>(),
                new SyntaxList<MemberDeclarationSyntax>(string.IsNullOrWhiteSpace(def.Namespace) ? (MemberDeclarationSyntax)classDeclaration :
                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(def.Namespace))/*.NormalizeWhitespace()*/.AddMembers(classDeclaration)));

            var code = unit.NormalizeWhitespace().ToFullString();

            var filename = def.ClassName + ".cs";
            var relativePath = Path.Combine(_generatedDirName, def.RelativePath, filename);
            var targetPath = Path.Combine(def.RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllText(targetPath, code);

            generatedFiles.Add(relativePath);
        }

        //private string CreateClassDeserializer(ClassDeclarationSyntax classmemb, NameSyntax nmspace, IEnumerable<UsingDirectiveSyntax> usings, string rootPath, string filepath)
        //{
        //    if (classmemb.Modifiers.Any(SyntaxKind.AbstractKeyword))
        //        return null;
        //    var classDeclaration = SyntaxFactory.ClassDeclaration(classmemb.Identifier)
        //        .AddModifiers(classmemb.Modifiers.With(SyntaxFactory.Token(SyntaxKind.PartialKeyword)).ToArray());

        //    //var @namespace = SyntaxFactory.NamespaceDeclaration(nmspace)/*.NormalizeWhitespace()*/.AddMembers(classDeclaration);

        //    var unit = SyntaxFactory.CompilationUnit(
        //        new SyntaxList<ExternAliasDirectiveSyntax>(),
        //        new SyntaxList<UsingDirectiveSyntax>(usings),
        //        new SyntaxList<AttributeListSyntax>(),
        //        new SyntaxList<MemberDeclarationSyntax>(nmspace == null ? (MemberDeclarationSyntax)classDeclaration :
        //        SyntaxFactory.NamespaceDeclaration(nmspace)/*.NormalizeWhitespace()*/.AddMembers(classDeclaration)));






        //    var code = unit.NormalizeWhitespace().ToFullString();

        //    var sourceDirectoryPath = Path.GetDirectoryName(filepath);
        //    var sourceRelativeDirectoryPath = sourceDirectoryPath.Substring(rootPath.Length + 1);
        //    var filename = classmemb.Identifier + ".cs";
        //    var relativePath = Path.Combine(_generatedDirName, sourceRelativeDirectoryPath, filename);
        //    var targetPath = Path.Combine(rootPath, relativePath);
        //    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
        //    File.WriteAllText(targetPath, code);

        //    return relativePath;
        //}

        #region misc around

        private void RemovePreviouslyGeneratedFilesFromProject(XDocument csproj)
        {
            var forRemove = new List<XElement>();

            foreach (var itemgroup in csproj.Element(XmlName("Project")).Elements(XmlName("ItemGroup")))
                forRemove.AddRange(itemgroup.Elements(XmlName("Compile")).Where(c => c.Attribute("Include").Value.StartsWith($"{_generatedDirName}\\")));
            foreach (var element in forRemove)
                element.Remove();
            forRemove.Clear();

            forRemove.AddRange(csproj.Element(XmlName("Project")).Elements(XmlName("ItemGroup")).Where(g => !g.HasElements));
            foreach (var element in forRemove)
                element.Remove();
        }

        private void RemovePreviouslyGeneratedFilesFromDrive()
        {
            var generatedPath = Path.Combine(RootPath, _generatedDirName);
            if (Directory.Exists(generatedPath))
                Directory.Delete(generatedPath, true);
        }

        private void AddGeneratedFilesToProject(XDocument csproj, List<string> generatedFiles)
        {
            var itemGroup = new XElement(XmlName("ItemGroup"));
            foreach (var generatedFile in generatedFiles.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                var compile = new XElement(XmlName("Compile"), new XAttribute("Include", generatedFile));
                itemGroup.Add(compile);
            }
            if (itemGroup.HasElements)
                csproj.Root.Add(itemGroup);
        }

        private XName XmlName(string name)
        {
            return XName.Get(name, _csprojNmsp);
        }

        #endregion
    }
}
