﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;

using HolisticWare.Xamarin.Tools.Bindings.XamarinAndroid.AndroidX.Migraineator;
using AST=HolisticWare.Xamarin.Tools.Bindings.XamarinAndroid.AndroidX.Migraineator.AST;

namespace Xamarin.AndroidX.Cecilfier.MigrationImplementations
{
    public class MigrationTraversingWithLogging : MigrationImplementation
    {
        public MigrationTraversingWithLogging(AndroidXMigrator migrator) : base(migrator)
        {
            androidx_migrator = migrator;

            return;
        }

        public override void Migrate(ref long duration)
        {
            string msg = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}-androidx-migrated";

            int idx = this.PathAssemblyOutput.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            string asm = this.PathAssemblyOutput.Substring(idx, this.PathAssemblyOutput.Length - idx );

            if
                (
                    asm.StartsWith("System", StringComparison.InvariantCultureIgnoreCase)
                    ||
                    asm.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)
                    ||
                    asm.StartsWith("Java.Interop", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                duration = -1;

                return;
            }

            log = new System.Text.StringBuilder();
            timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            if (File.Exists(this.PathAssemblyOutput))
            {
                File.Delete(this.PathAssemblyOutput);
            }
            File.Copy(this.PathAssemblyInput, this.PathAssemblyOutput);

            if(File.Exists(Path.ChangeExtension(this.PathAssemblyOutput, "pdb")))
            {
                File.Delete(Path.ChangeExtension(this.PathAssemblyOutput, "pdb"));
            }
            if (File.Exists(Path.ChangeExtension(this.PathAssemblyInput, "pdb")))
            {
                File.Copy(Path.ChangeExtension(this.PathAssemblyInput, "pdb"), Path.ChangeExtension(this.PathAssemblyOutput, "pdb"));
            }

            bool hasPdb = File.Exists(Path.ChangeExtension(this.PathAssemblyInput, "pdb"));

			var readerParams = new ReaderParameters
			{
				ReadSymbols = hasPdb,
			};

            asm_def = Mono.Cecil.AssemblyDefinition.ReadAssembly
                                                        (
                                                            this.PathAssemblyOutput,
                                                            new Mono.Cecil.ReaderParameters
                                                                {
                                                                    AssemblyResolver = CreateAssemblyResolver(),
                                                                    ReadWrite = true,
                                                                    //InMemory = true,
                                                                    ReadSymbols = hasPdb,
                                                                }
                                                        );

            System.Diagnostics.Trace.WriteLine($"===================================================================================");
            System.Diagnostics.Trace.WriteLine($"migrating assembly               = {this.PathAssemblyInput}");

            AST.Assembly ast_assembly = new AST.Assembly()
            {
                Name = asm
            };

            foreach(ModuleDefinition module in asm_def.Modules)
            {
                System.Diagnostics.Trace.WriteLine($"--------------------------------------------------------------------------");
                System.Diagnostics.Trace.WriteLine($"    migrating Module           = {module.Name}");
                //module.AssemblyReferences;

                AST.Module ast_module = ProcessModuleRedth(module);

                if(ast_module != null)
                {
                    if (ast_assembly == null)
                    {
                        ast_assembly = new AST.Assembly()
                        {

                        };
                    }
                    ast_assembly.Modules.Add(ast_module);
                }
            }

            AndroidXMigrator.AbstractSyntaxTree.Assemblies.Add(ast_assembly);
            timer.Stop();

            log.AppendLine($"{timer.ElapsedMilliseconds}ms");
            System.Diagnostics.Trace.WriteLine($"{timer.ElapsedMilliseconds}ms");
            //System.Diagnostics.Trace.WriteLine(log.ToString());


            File.WriteAllText
                (
                    Path.ChangeExtension(this.PathAssemblyInput, $"AbstractSyntaxTree.{msg}.json"),
                    Newtonsoft.Json.JsonConvert.SerializeObject
                    (
                        ast_assembly,
                        Newtonsoft.Json.Formatting.Indented,
                        new Newtonsoft.Json.JsonSerializerSettings()
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                        }
                    )
                );


            System.Diagnostics.Debug.WriteLine(log.ToString());
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"), log.ToString());

            asm_def.Write();

            duration = timer.ElapsedMilliseconds;

            return;
        }

        private AST.Module ProcessModuleRedth(ModuleDefinition module)
        {
            AST.Module ast_module = null;

            foreach (TypeReference type in module.GetTypeReferences())
            {
                if
                    (
                        //type.FullName == "<Module>"
                        //||
                        //type.FullName == "<PrivateImplementationDetails>"
                        //||
                        type.FullName.StartsWith("System.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("Microsoft.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("AndroidX.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("Java.Interop.", StringComparison.Ordinal)
                    )
                {
                    continue;
                }
                System.Diagnostics.Trace.WriteLine($"    processing ReferenceType");
                System.Diagnostics.Trace.WriteLine($"        Name        = {type.Name}");
                System.Diagnostics.Trace.WriteLine($"        FullName    = {type.FullName}");

                AST.Type ast_type = ProcessTypeReferenceRedth(type);

                if (ast_type == null)
                {
                    continue;
                }
                else
                {
                    if (ast_module == null)
                    {
                        ast_module = new AST.Module()
                        {
                            Name = module.Name
                        };
                    }
                    ast_module.TypesReference.Add(ast_type);
                }
            }

            foreach (TypeDefinition type in module.Types)
            {
                if
                    (
                        //type.FullName == "<Module>"
                        //||
                        //type.FullName == "<PrivateImplementationDetails>"
                        //||
                        type.FullName.StartsWith("System.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("Microsoft.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("AndroidX.", StringComparison.Ordinal)
                        ||
                        type.FullName.StartsWith("Java.Interop.", StringComparison.Ordinal)
                    )
                {
                    continue;
                }

                System.Diagnostics.Trace.WriteLine($"    processing Type");
                System.Diagnostics.Trace.WriteLine($"        Name        = {type.Name}");
                System.Diagnostics.Trace.WriteLine($"        FullName    = {type.FullName}");
                System.Diagnostics.Trace.WriteLine($"        IsClass     = {type.IsClass}");
                System.Diagnostics.Trace.WriteLine($"        IsInterface = {type.IsInterface}");

                AST.Type ast_type = ProcessTypeRedth(type);

                if(ast_type == null)
                {
                    continue;
                }
                else
                {
                    if (ast_module == null)
                    {
                        ast_module = new AST.Module()
                        { 
                            Name = module.Name
                        };
                    }
                    ast_module.Types.Add(ast_type);
                }

            }

            return ast_module;
        }

        private AST.Type ProcessTypeRedth(TypeDefinition type)
        {
            AST.Type ast_type = null;

            AST.Type ast_type_base = ProcessBaseTypeRedth(type.BaseType);
            if(ast_type_base != null)
            {
                TypeDefinition type_found = type.Module.Types
                                                            .Where(t => t.FullName == ast_type_base.NameFullyQualifiedOldMigratred)
                                                            .FirstOrDefault();
            }

            List<AST.Type> ast_types_nested = null;
            foreach (TypeDefinition type_nested in type.NestedTypes)
            {
                AST.Type ast_type_nested = ProcessNestedTypeRedth(type_nested);

                if (ast_type_nested != null)
                {
                    ast_types_nested = new List<AST.Type>();
                }
                else
                {
                    continue;
                }

                ast_types_nested.Add(ast_type_nested);
            }

            List<AST.Method> ast_methods = null;
            foreach(var method in type.Methods)
            {
                AST.Method ast_method = ProcessMethodRedth(method);

                if (ast_method != null)
                {
                    ast_methods = new List<AST.Method>();
                }
                else
                {
                    continue;
                }

                ast_methods.Add(ast_method);
            }

            if (ast_type_base == null && ast_methods == null)
            {
                return ast_type;
            }

            ast_type = new AST.Type()
            {
                Name = type.Name,
                NameFullyQualified = type.FullName,
            };

            if (ast_type != null)
            {
                ast_type.BaseType = ast_type_base;
            }
            if (ast_methods != null)
            {
                ast_type.Methods = ast_methods;
            }

            return ast_type;
        }

        private AST.Type ProcessTypeReferenceRedth(TypeReference type)
        {
            AST.Type ast_type_base = null;

            if
                (
                    type == null
                    ||
                    ! (type?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                )
            {
                return ast_type_base;
            }

            System.Diagnostics.Trace.WriteLine($"        processing References - TypeReference");
            System.Diagnostics.Trace.WriteLine($"            Name        = {type.Name}");
            System.Diagnostics.Trace.WriteLine($"            FullName    = {type.FullName}");

            string type_fqn_old = type.FullName;

            string r = FindReplacingTypeFromMappings(type.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_base;
            }

            int idx = r.LastIndexOf('.');
            type.Namespace = r.Substring(0, idx);
			type.Scope.Name = r.Substring(idx + 1, r.Length - idx - 1);

            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"    BaseType: {type.FullName}");
            Console.ResetColor();

           ast_type_base = new AST.Type()
           {
               Name = type.Name,
               NameFullyQualified = type.FullName,
               NameFullyQualifiedOldMigratred = type_fqn_old
           };

            return ast_type_base;
        }

        private AST.Type ProcessBaseTypeRedth(TypeReference type_base)
        {
            AST.Type ast_type_base = null;

            if
                (
                    type_base == null
                    ||
                    ! (type_base?.FullName).StartsWith("Android.Support.", StringComparison.Ordinal)
                )
            {
                return ast_type_base;
            }

            System.Diagnostics.Trace.WriteLine($"        processing BaseType - TypeReference");
            System.Diagnostics.Trace.WriteLine($"            Name        = {type_base.Name}");
            System.Diagnostics.Trace.WriteLine($"            FullName    = {type_base.FullName}");

            string type_fqn_old = type_base.FullName;

            string r = FindReplacingTypeFromMappings(type_base.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_base;
            }
            int idx = r.LastIndexOf('.');
            if (idx < 0)
            {
                return ast_type_base;
            }
            type_base.Namespace = r.Substring(0, idx);
			type_base.Scope.Name = r.Substring(idx + 1, r.Length - idx - 1);

            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"    BaseType: {type_base.FullName}");
            Console.ResetColor();

           ast_type_base = new AST.Type()
           {
               Name = type_base.Name,
               NameFullyQualified = type_base.FullName,
               NameFullyQualifiedOldMigratred = type_fqn_old
           };

            return ast_type_base;
        }

        private AST.Type ProcessNestedTypeRedth(TypeDefinition type_nested)
        {
            AST.Type ast_type_nested = null;

            if
                (
                    type_nested == null
                    ||
                    ! type_nested.FullName.StartsWith("Android.Support.")
                    ||
                    type_nested.Name.Contains("<>c")  // anonymous methods, lambdas 
                    ||
                    type_nested.Name.Contains("<>c__DisplayClass")  // anonymous methods, lambdas 
                )
            {
                return ast_type_nested;
            }
            
            string type_nested_fqn_old = type_nested.FullName;
            string r = FindReplacingTypeFromMappings(type_nested.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_nested;
            }
            int idx1 = r.LastIndexOf('.');
            int idx2 = r.LastIndexOf('/');
            type_nested.Namespace = r.Substring(0, idx1);
			//type_nested.Scope.Name = r.Substring(idx1 + 1, r.Length - idx2 - 1);
            Console.ResetColor();

            ast_type_nested = new AST.Type()
            {
               Name = type_nested.Name,
               NameFullyQualified = type_nested.FullName,
               NameFullyQualifiedOldMigratred = type_nested_fqn_old
            };

            return ast_type_nested;
        }


        private AST.Method ProcessMethodRedth(MethodDefinition method)
        {
            AST.Method ast_method = null;

            System.Diagnostics.Trace.WriteLine($"        processing method");
            System.Diagnostics.Trace.WriteLine($"           Name        = {method.Name}");
            System.Diagnostics.Trace.WriteLine($"           FullName    = {method.ReturnType.FullName}");

            AST.Type ast_method_type_return = ProcessMethodReturnTypeRedth(method.ReturnType);

            string jni_signature = ProcessMethodJNISignatureRedth(method);

            List<AST.Parameter> ast_method_parameters = null;
            foreach (ParameterDefinition method_parameter in method.Parameters)
            {
                AST.Parameter ast_method_parameter = ProcessMethodParameterRedth(method_parameter);

                if (ast_method_parameter != null)
                {
                    if(ast_method_parameters == null)
                    {
                        ast_method_parameters = new List<AST.Parameter>();
                    }
                }
                else
                {
                    continue;
                }
                ast_method_parameters.Add(ast_method_parameter);
            }

            AST.MethodBody ast_method_body = ProcessMethodBodyRedth(method.Body);

            if (ast_method_type_return == null && jni_signature == null && ast_method_body == null && ast_method_parameters == null)
            {
                return ast_method;
            }

            ast_method = new AST.Method();

            if (ast_method_type_return != null)
            {
                ast_method.ReturnType = ast_method_type_return;
            }

            if (ast_method_body != null)
            {
                ast_method.Body = ast_method_body;
            }

            if (ast_method_parameters != null)
            {
                ast_method.Parameters = ast_method_parameters;
            }

            return ast_method;
        }

        private AST.Type ProcessMethodReturnTypeRedth(TypeReference type_return)
        {
            AST.Type ast_type_return = null;

            if (! type_return.FullName.StartsWith("Android.Support."))
            {
                return ast_type_return;
            }

            System.Diagnostics.Trace.WriteLine($"        changing return type");
            System.Diagnostics.Trace.WriteLine($"           Name    = {type_return.Name}");
            System.Diagnostics.Trace.WriteLine($"           FullName    = {type_return.FullName}");

            string r = FindReplacingTypeFromMappings(type_return.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_type_return;
            }
            type_return.Namespace = replacement;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            log.AppendLine($"{type_return.Name} returns {type_return.FullName}");
            Console.ResetColor();

            return ast_type_return;
        }

        private AST.Parameter ProcessMethodParameterRedth(ParameterDefinition method_parameter)
        {
            AST.Parameter ast_method_parameter = null;

            if
                (
                    method_parameter == null
                    ||
                    ! method_parameter.ParameterType.FullName.StartsWith("Android.Support.", StringComparison.Ordinal)
                )
            {
                return ast_method_parameter;
            }

            string r = FindReplacingTypeFromMappings(method_parameter.ParameterType.FullName);
            if (string.IsNullOrEmpty(r))
            {
                return ast_method_parameter;
            }
            method_parameter.ParameterType.Namespace = r;

            ast_method_parameter = new AST.Parameter()
            {

            };

            return ast_method_parameter;
        }

        private string ProcessMethodJNISignatureRedth(MethodDefinition method)
        {
            string jni_signature = null;

            foreach (CustomAttribute attr in method.CustomAttributes)
            {
                if (attr.AttributeType.FullName.Equals("Android.Runtime.RegisterAttribute"))
                {
                    CustomAttributeArgument jniSigArg = attr.ConstructorArguments[1];

                    string registerAttrMethodName = attr.ConstructorArguments[0].Value.ToString();
                    string registerAttributeJniSig = jniSigArg.Value?.ToString();
                    object registerAttributeNewJniSig = ReplaceJniSignatureRedth(registerAttributeJniSig);

                    attr.ConstructorArguments[1] = new CustomAttributeArgument(jniSigArg.Type, registerAttributeNewJniSig);

                    bool isBindingMethod = true;

                    log.AppendLine($"[Register(\"{attr.ConstructorArguments[0].Value}\", \"{registerAttributeNewJniSig}\")]");
                }
            }

            return jni_signature;
        }

        private AST.MethodBody ProcessMethodBodyRedth(Mono.Cecil.Cil.MethodBody method_body)
        {
            AST.MethodBody ast_method_body = null;

            if (method_body == null)
            {
                return ast_method_body;
            }

            // Replace all the JNI Signatures inside the method body
            foreach (Mono.Cecil.Cil.Instruction instr in method_body.Instructions)
            {
                if (instr.OpCode.Name == "ldstr")
                {
                    string jniSig = instr.Operand.ToString();

                    int indexOfDot = jniSig.IndexOf('.');

                    if (indexOfDot < 0)
                    {
                        continue;
                    }

                    // New binding glue style is `methodName.(Lparamater/Type;)Lreturn/Type;`
                    if (indexOfDot >= 0)
                    {
                        string methodName = jniSig.Substring(0, indexOfDot);
                        string newJniSig = ReplaceJniSignatureRedth(jniSig.Substring(indexOfDot + 1));
                        instr.Operand = $"{methodName}.{newJniSig}";

                        log.AppendLine($"{methodName} -> {newJniSig}");
                    }
                    // Old style is two strings, one with method name and then `(Lparameter/Type;)Lreturn/Type;`
                    else if (jniSig.Contains('(') && jniSig.Contains(')'))
                    {
                        string methodName = instr.Previous.Operand.ToString();
                        string newJniSig = ReplaceJniSignatureRedth(jniSig);
                        instr.Operand = newJniSig;

                        log.AppendLine($"{methodName} -> {newJniSig}");
                    }
                    else
                    {
                        string msg = "Method Body Code Smell";
                        //throw new 
                    }

                    if (ast_method_body == null)
                    {
                        ast_method_body = new AST.MethodBody();
                    }
                }
            }

            return ast_method_body;
        }



        static string ReplaceJniSignatureRedth(string jniSignature)
        {
            if
                (
                    //-------------------------------
                    // WTF ??
                    jniSignature.Contains("Forms.Init(); prior to using it.") // WTF
                    ||
                    jniSignature.Contains("Init() before this")
                    ||
                    jniSignature.Contains("Init(); prior to using it.")
                    //-------------------------------
                    // iOS - picked during batch brute force Ceciling
                    ||
                    jniSignature.Contains("FinishedLaunching ()") // Xamarin.Forms.Platform.iOS.migrated.dll
                    //-------------------------------
                    ||
                    string.IsNullOrEmpty(jniSignature)
                    ||
                    ! jniSignature.Contains('(')
                    ||
                    ! jniSignature.Contains(')')
                )
            {
                return jniSignature;
            }

            var sig = new global::Xamarin.Android.Tools.Bytecode.MethodTypeSignature(jniSignature);

            var sb_newSig = new System.Text.StringBuilder();

            sb_newSig.Append("(");

            foreach (var p in sig.Parameters)
            {
                string mapped = "mapped"; //mappings[p];
                sb_newSig.Append($"L{mapped};" ?? p);               
            }

            sb_newSig.Append(")");

            sb_newSig.Append(sig.ReturnTypeSignature);

            string newSig = null;  // sb_newSig.ToString();

            return newSig;
        }

    }
}