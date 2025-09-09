using Analiser.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

namespace Analiser
{
    public class CodeTreeBuilder
    {
        private readonly Workspace _workspace;
        private readonly Compilation _compilation;

        public List<CodeProject> Projects { get; } = new List<CodeProject>();

        public CodeTreeBuilder(Workspace workspace, List<MetadataReference> references)
        {
            _workspace = workspace;
            _compilation = CSharpCompilation.Create("Assembly", references: references);

            foreach (Project projectToBuild in _workspace.CurrentSolution.Projects)
            {
                // currently .NET 6.0 and .NET core aren't supported, only .NET 4.x.
                // This is a choice on our part, since we need to pick one and can't mix them.
                if (projectToBuild.ParseOptions != null &&
                    (projectToBuild.ParseOptions.PreprocessorSymbolNames.Contains("NET6_0") || projectToBuild.ParseOptions.PreprocessorSymbolNames.Contains("NETCOREAPP"))
                )
                {
                    continue;
                }

                foreach (Document document in projectToBuild.Documents)
                {
                    SyntaxTree? syntaxTree = document.GetSyntaxTreeAsync().Result;
                    if (syntaxTree == null)
                    {
                        continue;
                    }

                    _compilation = _compilation.AddSyntaxTrees(syntaxTree);
                }
            }
        }

        #region Processing Methods

        public void ProcessProject(string name)
        {
            Project? project = _workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Name == name);
            if (project == null)
            {
                throw new Exception($"No project found with name '{name}'");
            }

            ProcessProject(project);
        }

        public void ProcessAllProjects()
        {
            foreach (Project project in _workspace.CurrentSolution.Projects)
            {
                ProcessProject(project);
            }
        }

        private void ProcessProject(Project project)
        {
            CodeProject codeProject = new CodeProject(project.Name);

            foreach (Document document in project.Documents)
            {
                if (document.TryGetSyntaxTree(out SyntaxTree? syntaxTree))
                {
                    SemanticModel model = _compilation.GetSemanticModel(syntaxTree);
                    codeProject.Classes.AddRange(ProcessClassesFromDocument(model, syntaxTree));
                }
            }

            Projects.Add(codeProject);
        }

        private List<CodeClass> ProcessClassesFromDocument(SemanticModel model, SyntaxTree tree)
        {
            List<CodeClass> classes = new List<CodeClass>();

            foreach (ClassDeclarationSyntax treeClass in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                INamedTypeSymbol? treeClassModel = model.GetDeclaredSymbol(treeClass);
                if (treeClassModel == null)
                {
                    continue;
                }

                string codeClassNamespace = GetNamespace(treeClassModel);
                CodeType[] interfaces = treeClassModel.Interfaces.Select(GetCodeType).ToArray();
                CodeType? baseType = GetNullableCodeType(treeClassModel.BaseType);
                CodeClass codeClass = new CodeClass(codeClassNamespace, treeClassModel.Name, baseType, interfaces);

                codeClass.Properties.AddRange(ProcessPropertiesFromClass(model, treeClass));
                codeClass.Methods.AddRange(ProcessMethodsFromClass(model, treeClass));

                classes.Add(codeClass);
            }

            return classes;
        }

        private List<CodeProperty> ProcessPropertiesFromClass(SemanticModel model, ClassDeclarationSyntax treeClass)
        {
            List<CodeProperty> properties = new List<CodeProperty>();

            foreach (PropertyDeclarationSyntax treeClassProperty in treeClass.Members.OfType<PropertyDeclarationSyntax>())
            {
                IPropertySymbol? propertyModel = model.GetDeclaredSymbol(treeClassProperty);
                if (propertyModel == null)
                {
                    continue;
                }

                CodeType type = GetCodeType(propertyModel.Type);
                string propertyName = propertyModel.Name;
                properties.Add(new CodeProperty(type, propertyName));
            }

            return properties;
        }

        private List<CodeMethod> ProcessMethodsFromClass(SemanticModel model, ClassDeclarationSyntax treeClass)
        {
            List<CodeMethod> methods = new List<CodeMethod>();

            foreach (MethodDeclarationSyntax treeClassMethod in treeClass.Members.OfType<MethodDeclarationSyntax>())
            {
                IMethodSymbol? methodModel = model.GetDeclaredSymbol(treeClassMethod);
                if (methodModel == null)
                {
                    continue;
                }

                string[] modifiers = treeClassMethod.Modifiers.Select(m => m.Text).ToArray();
                CodeType returnType = methodModel.ReturnsVoid ? CodeType.Void : GetCodeType(methodModel.ReturnType);
                string name = methodModel.Name;
                string hash = CreateClassMethodHash(treeClassMethod);

                CodeMethod method = new CodeMethod(modifiers, returnType, name, hash);
                method.Parameters.AddRange(PopulateMethodParametersFromMethod(model, treeClassMethod));
                ProcessMethodBodyFromMethod(model, treeClassMethod, method.Body, method.Parameters);

                methods.Add(method);
            }

            return methods;
        }

        private string CreateClassMethodHash(MethodDeclarationSyntax treeClassMethod)
        {
            string modifiers = treeClassMethod.Modifiers.ToString();
            string returnTypeName = treeClassMethod.ReturnType.GetText().ToString().Trim();
            string methodName = treeClassMethod.Identifier.Text.Trim();
            string parameterList = string.Join(", ", treeClassMethod.ParameterList.Parameters.Select(p => p.GetText().ToString().Trim()));

            string fullText = $"{modifiers} {returnTypeName} {methodName} ({parameterList})";
            string hash = CreateSha256Hash(fullText);
            return hash;
        }

        private List<CodeMethodParameter> PopulateMethodParametersFromMethod(SemanticModel model, MethodDeclarationSyntax treeClassMethod)
        {
            List<CodeMethodParameter> parameters = new List<CodeMethodParameter>();

            foreach (ParameterSyntax parameter in treeClassMethod.ParameterList.Parameters)
            {
                IParameterSymbol? parameterModel = model.GetDeclaredSymbol(parameter);
                if (parameterModel == null)
                {
                    continue;
                }

                CodeType type = GetCodeType(parameterModel.Type);
                string name = parameterModel.Name;

                CodeMethodParameter codeParameter = new CodeMethodParameter(type, name);
                parameters.Add(codeParameter);
            }

            return parameters;
        }

        private void ProcessMethodBodyFromMethod(SemanticModel model, MethodDeclarationSyntax treeClassMethod, CodeMethodBody methodBody, List<CodeMethodParameter> parameters)
        {
            BlockSyntax? body = treeClassMethod.Body;
            if (body == null)
            {
                return;
            }

            methodBody.BodyText = body.Statements.ToFullString();

            Dictionary<string, CodeType> declaredVariables = new Dictionary<string, CodeType>();
            foreach (CodeMethodParameter parameter in parameters)
            {
                declaredVariables.Add(parameter.Name, parameter.Type);
            }

            SyntaxNode[] descendents = body.DescendantNodes().ToArray(); // make it an array so we can debug it easier
            foreach (SyntaxNode node in descendents)
            {
                // get referenced types, so we can link changes to these types to the method
                CodeType? codeType = GetNullableCodeType(model.GetTypeInfo(node).ConvertedType);
                if (codeType != null && codeType.IsDataTorque)
                {
                    // add any DataTorque.* references to our list of types
                    int codeTypeHashCode = codeType.GetHashCode();
                    if (methodBody.ReferencedTypes.Any(t => t.GetHashCode() == codeTypeHashCode) == false)
                    {
                        methodBody.ReferencedTypes.Add(codeType);
                    }
                }

                // get any references to stored procedures, so we can link SP changes to the method
                string nodeString = node.ToFullString();
                if (nodeString.Contains("\"spf_"))
                {
                    string storedProcedure = ExtractStoredProcedureFromText(nodeString);
                    if (storedProcedure != string.Empty && !methodBody.ReferencedStoredProcedures.Contains(storedProcedure))
                    {
                        methodBody.ReferencedStoredProcedures.Add(storedProcedure);
                    }
                }

                // if this statement is a method or property expression, then add it to the body
                if (node is MemberAccessExpressionSyntax memberAccessExpression)
                {
                    ProcessMemberAccessExpression(memberAccessExpression, model, methodBody, declaredVariables);
                }

                // Consider using the following to gather additional data:
                // VariableDeclarationSyntax
                // IdentifierNameSyntax
            }
        }

        private void ProcessMemberAccessExpression(MemberAccessExpressionSyntax memberAccessExpression, SemanticModel model, CodeMethodBody methodBody, Dictionary<string, CodeType> declaredVariables)
        {
            IdentifierNameSyntax? expression = memberAccessExpression.Expression as IdentifierNameSyntax;
            if (expression == null)
            {
                return;
            }

            string name = expression.Identifier.Text;
            string methodOrPropertyName = memberAccessExpression.Name.Identifier.Text;

            CodeType? expressionCodeType;
            if (!declaredVariables.TryGetValue(name, out expressionCodeType))
            {
                expressionCodeType = GetNullableCodeType(model.GetTypeInfo(expression).ConvertedType);
                if (expressionCodeType == null)
                {
                    return;
                }

                declaredVariables.Add(name, expressionCodeType);
            }

            if (!expressionCodeType!.IsDataTorque)
            {
                return;
            }

            CodeExpression codeExpression = new CodeExpression(expressionCodeType, methodOrPropertyName);
            int hashCode = codeExpression.GetHashCode();
            if (!methodBody.ReferencedExpressions.Any(e => e.GetHashCode() == hashCode))
            {
                methodBody.ReferencedExpressions.Add(codeExpression);
            }
        }

        private string ExtractStoredProcedureFromText(string input)
        {
            int start = input.IndexOf("\"spf_");
            if (start == -1)
            {
                return string.Empty;
            }

            start += 1; // we don't want the opening quote.

            int end = input.IndexOf("\"", start);
            if (end == -1)
            {
                return string.Empty;
            }

            return input.Substring(start, end - start);
        }

        #endregion

        #region Helpers

        private static string CreateSha256Hash(string inputString)
        {
            // it's a bit overkill to use SHA256 just to get a hash, but it's easier to write this in C# than using Crc32 to create a CRC hash.
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array
                byte[] bytes = Encoding.UTF8.GetBytes(inputString);

                // Compute the hash of the byte array
                byte[] hashBytes = sha256Hash.ComputeHash(bytes);

                // Convert the hash byte array to a hexadecimal string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("X2")); // "X2" formats as two hexadecimal digits
                }
                return builder.ToString();
            }
        }

        private string GetNamespace(ITypeSymbol? type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            return type.ContainingNamespace?.ToString() ?? string.Empty;
        }

        private CodeType GetCodeType(ITypeSymbol? type)
        {
            if (type == null)
            {
                throw new Exception("Unable to create type from null value");
            }

            string ns = GetNamespace(type);
            if (ns == string.Empty)
            {
                return new CodeType(string.Empty, type.Name);
            }

            return new CodeType(ns, type.Name);
        }

        private CodeType? GetNullableCodeType(ITypeSymbol? type)
        {
            if (type == null)
            {
                return null;
            }

            string ns = GetNamespace(type);
            if (ns == string.Empty)
            {
                return null;
            }

            return new CodeType(ns, type.Name);
        }

        #endregion
    }
}
