using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        const string _generatedPath = "Generated";
        const string _assetsPath = "Assets";
        const string _schemaPath = "json_schema.json";
        const string _vsCodeSettingsPath = ".vscode/settings.json";
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

            //RemovePreviouslyGeneratedFilesFromProject(csproj);
            RemovePreviouslyGeneratedFilesFromDrive();

            var generatedFiles = new List<ProjectFile>();

            var defStructs = new List<DefStructure>(GetDefStructures(Path.Combine(RootPath, "Defs")));

            foreach (var def in defStructs)
                generatedFiles.Add(CreateDefDeserializer(def));

            generatedFiles.Add(CreateVSCodeSettings(defStructs));
            generatedFiles.Add(CreateJsonSchema(defStructs));

            if (AddGeneratedFilesToProject(csproj, generatedFiles))
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

        private ProjectFile CreateDefDeserializer(DefStructure def)
        {
            if (def.Modifiers.Contains(SyntaxKind.AbstractKeyword))
                return default(ProjectFile);

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

            var notification = string.Format(Properties.Resources.GeneratedNotification, GetType().Assembly.GetName().Version.ToString());
            var code = $"{notification}\r\n{unit.NormalizeWhitespace().ToFullString()}";

            var filename = def.ClassName + ".cs";
            var relativePath = Path.Combine(_generatedPath, def.RelativePath, filename);
            var targetPath = Path.Combine(def.RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllText(targetPath, code);

            return new ProjectFile(relativePath, ItemGroupLabel.Generated, BuildActionType.Compile);
        }

        private ProjectFile CreateVSCodeSettings(IEnumerable<DefStructure> defs)
        {
            var relativePath = Path.Combine(_assetsPath, _vsCodeSettingsPath);
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllBytes(path, Properties.Resources.VSCodeSettings);

            return new ProjectFile(relativePath, ItemGroupLabel.VSCode, BuildActionType.None, true);
        }

        private ProjectFile CreateJsonSchema(IEnumerable<DefStructure> defs)
        {
            var relativePath = Path.Combine(_assetsPath, _schemaPath);
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllBytes(path, Properties.Resources.VSCodeSettings);

            return new ProjectFile(relativePath, ItemGroupLabel.VSCode, BuildActionType.None, true);
        }

        #region misc around

        private void RemovePreviouslyGeneratedFilesFromProject(XDocument csproj)
        {
            var forRemove = new List<XElement>();

            foreach (var itemgroup in csproj.Element(XmlName("Project")).Elements(XmlName("ItemGroup")))
                forRemove.AddRange(itemgroup.Elements(XmlName("Compile")).Where(c => c.Attribute("Include").Value.StartsWith($"{_generatedPath}\\")));
            foreach (var element in forRemove)
                element.Remove();
            forRemove.Clear();

            forRemove.AddRange(csproj.Element(XmlName("Project")).Elements(XmlName("ItemGroup")).Where(g => !g.HasElements));
            foreach (var element in forRemove)
                element.Remove();
        }

        private void RemovePreviouslyGeneratedFilesFromDrive()
        {
            var generatedPath = Path.Combine(RootPath, _generatedPath);
            if (Directory.Exists(generatedPath))
                Directory.Delete(generatedPath, true);
            var assetsPath = Path.Combine(RootPath, _assetsPath);
            if (Directory.Exists(assetsPath))
                Directory.Delete(assetsPath, true);
        }

        private bool AddGeneratedFilesToProject(XDocument csproj, List<ProjectFile> generatedFiles)
        {
            bool projectChanged = false;
            generatedFiles.RemoveAll(f => f.Equals(default(ProjectFile)));
            var itemGroups = new Dictionary<ItemGroupLabel, XElement>();
            var forRemove = new List<XElement>();
            foreach (var itemgroup in csproj.Element(XmlName("Project")).Elements(XmlName("ItemGroup")))
            {
                var labelAtribute = itemgroup.Attribute("Label");
                if (labelAtribute != null)
                {
                    ItemGroupLabel label;
                    if (Enum.TryParse(labelAtribute.Value, out label))
                    {
                        itemGroups.Add(label, itemgroup);
                        foreach (var entry in itemgroup.Elements())
                        {
                            BuildActionType action;
                            if (Enum.TryParse(entry.Name.LocalName, out action))
                            {
                                var include = entry.Attribute("Include");
                                if (include != null)
                                {
                                    var path = include.Value;
                                    var copyToOutputDirectory = false;
                                    var copyEntry = entry.Element(XmlName("CopyToOutputDirectory"));
                                    if (copyEntry != null)
                                        copyToOutputDirectory = copyEntry.Value == "Always" || copyEntry.Value == "PreserveNewest";
                                    var projectFile = new ProjectFile(path, label, action, copyToOutputDirectory);
                                    if (generatedFiles.RemoveAll(f => f.Equals(projectFile)) > 0)
                                        continue;
                                }
                            }
                            forRemove.Add(entry);
                        }
                    }
                }
            }

            foreach (var node in forRemove)
            {
                node.Remove();
                projectChanged = true;
            }

            foreach (var file in generatedFiles)
            {
                XElement itemGroup;
                if (!itemGroups.TryGetValue(file.ItemGroup, out itemGroup))
                {
                    itemGroup = new XElement(XmlName("ItemGroup"), new XAttribute("Label", file.ItemGroup));
                    csproj.Root.Add(itemGroup);
                    itemGroups.Add(file.ItemGroup, itemGroup);
                }
                var action = new XElement(XmlName(file.BuildAction.ToString()), new XAttribute("Include", file.File));
                if (file.CopyToOutputDirectory)
                    action.Add(new XElement(XmlName("CopyToOutputDirectory"), "Always"));
                itemGroup.Add(action);
                projectChanged = true;
            }

            return projectChanged;
        }

        private XName XmlName(string name)
        {
            return XName.Get(name, _csprojNmsp);
        }

        #endregion
    }
}
