// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class NoPiaInstantiationOfGenericClassAndStruct : CSharpTestBase
    {
        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForClassThatInheritsGeneric()
        {
            //Test class that inherits Generic<NoPIAType>

            var localTypeSource = @"public class NoPIAGenerics 
{
   Class1 field =  null;   
}";

            var localConsumer1 = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer1.SourceModule.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType1.GetMembersAsImmutable("field").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localField.Type.BaseType().Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localField.Type.BaseType());
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForGenericType()
        {
            //Test field with Generic(Of NoPIAType())

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = null;
}";

            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.GetMembersAsImmutable("field2").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForFieldWithNestedGenericType()
        {
            //Test field with Generic(Of IGeneric(Of NoPIAType))

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = null;
}";

            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.GetMembersAsImmutable("field3").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForFieldWithTwoNestedGenericType()
        {
            //Test field with IGeneric(Of IGeneric(Of Generic(Of NoPIAType)))

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = New NestedConstructs();
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.GetMembersAsImmutable("field5").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, importedField.Type.Kind);

            var outer = ((NamedTypeSymbol)importedField.Type).TypeArguments().Single();
            Assert.Equal(SymbolKind.NamedType, outer.Kind);

            var inner = ((NamedTypeSymbol)outer).TypeArguments().Single();
            Assert.Equal(SymbolKind.ErrorType, inner.Kind);
        }

        [Fact]
        public void NoPIAInterfaceInheritsGenericInterface()
        {
            //Test interface that inherits IGeneric(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   Interface1 i1 = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var var1 = classLocalType1.GetMembersAsImmutable("i1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(var1.Type);
        }

        [Fact]
        public void NoPIALocalClassInheritsGenericTypeWithPIATypeParameters()
        {
            //Test class that inherits Generic(Of NoPIAType) used as method return or arguments

            var localTypeSource1 = @"public class NoPIAGenerics 
{
     InheritsMethods inheritsMethods = null;
}";
            var localConsumer = CreateCompilation(localTypeSource1);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("inheritsMethods").OfType<FieldSymbol>().Single();

            foreach (MethodSymbol m in localField.Type.GetMembersAsImmutable("Method1").OfType<MethodSymbol>())
            {
                if (m.Parameters.Length > 0)
                {
                    Assert.Equal(SymbolKind.ErrorType, m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.BaseType().Kind);
                    Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.BaseType());
                }
                if (m.ReturnType.TypeKind != TypeKind.Struct)
                {
                    Assert.Equal(SymbolKind.ErrorType, m.ReturnType.BaseType().Kind);
                    Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.ReturnType.BaseType());
                }
            }
        }

        [Fact]
        public void NoPIALocalStructImplementInterfaceThatInheritsGenericTypeWithPIATypeParameters()
        {
            //Test implementing an interface that inherits IGeneric(Of NoPIAType) 

            var localTypeSource1 = @" public class NoPIAGenerics 
{
    Interface1 i1 = new Interface1Impl();
}";
            var localConsumer = CreateCompilation(localTypeSource1);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembersAsImmutable("i1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(var1.Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType()
        {
            //Test a static property that takes Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembersAsImmutable("typeRef").OfType<FieldSymbol>().Single();
            var importedProperty = local.Type.GetMembersAsImmutable("Property1").OfType<PropertySymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedProperty.Parameters.Single(arg => arg.Name == "x").Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedProperty.Parameters.Single(arg => arg.Name == "x").Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType2()
        {
            //Test a static property that takes Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembersAsImmutable("typeRef").OfType<FieldSymbol>().Single();
            var importedProperty = local.Type.GetMembersAsImmutable("Property2").OfType<PropertySymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedProperty.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedProperty.Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfPiaType()
        {
            //Test a static method that takes Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembersAsImmutable("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable("Method1").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesOptionalGenericOfPiaType()
        {
            //Test a static method that takes an Optional Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembersAsImmutable("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable("Method2").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForMethodThatTakesGenericOfEnumPiaType()
        {
            // Test an interface method that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   TypeRefs1.Interface2 i2 = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType.GetMembersAsImmutable("i2").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable("Method3").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfInterfacePiaType()
        {
            // Test a static method that returns Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType.GetMembersAsImmutable("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable("Method4").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.ReturnType);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForConstructorThatTakesGenericOfStructPiaType()
        {
            // Test a constructor that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
     TypeRefs2 tr2a = new TypeRefs2(null);
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType.GetMembersAsImmutable("tr2a").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable(".ctor").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForOperatorThatTakesGenericOfPiaType()
        {
            // Test an operator that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
     TypeRefs2 tr2a = new TypeRefs2(null);
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType.GetMembersAsImmutable("tr2a").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.GetMembersAsImmutable("op_Implicit").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Single(arg => arg.Name == "x").Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Single(arg => arg.Name == "x").Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForEventThatTakesGenericOfPiaType()
        {
            // Test hooking an event that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   TypeRefs2 tr2b = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var local = classLocalType.GetMembersAsImmutable("tr2b").OfType<FieldSymbol>().Single();
            var importedField = local.Type.GetMembersAsImmutable("Event1").OfType<EventSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForEventWithDelegateTypeThatTakesGenericOfPiaType()
        {
            //Test declaring event with delegate type that takes generic argument

            var localTypeSource = @" public class NoPIAGenerics 
{
        private event TypeRefs2.Delegate1 Event2;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembersAsImmutable("Event2").OfType<EventSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForFieldWithDelegateTypeThatReturnGenericOfPiaType()
        {
            //Test declaring field with delegate type that takes generic argument

            var localTypeSource = @" public class NoPIAGenerics 
{
        private static TypeRefs2.Delegate2 Event3;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembersAsImmutable("Event3").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForStaticMethodAccessedThroughImports()
        {
            //Test static method accessed through Imports

            var localTypeSource = @" 
using MyClass1 = Class1;
public class NoPIAGenerics
{
    MyClass1 myclass = null;
    
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("myclass").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localField.Type.BaseType().Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localField.Type.BaseType());
        }

        [Fact]
        public void NoPiaStaticMethodAccessedThroughImportsOfGenericClass()
        {
            //Test static method accessed through Imports of generic class

            var localTypeSource = @"
using MyGenericClass2 = Class2<ISubFuncProp>;

public class NoPIAGenerics  
{
    MyGenericClass2 mygeneric = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembersAsImmutable("mygeneric").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, localField.Type.Kind);
            Assert.IsType<ConstructedNamedTypeSymbol>(localField.Type);
        }

        [Fact]
        public void NoPIAClassThatInheritsGenericOfNoPIAType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localTypeSource1 = @"public class BaseClass : System.Collections.Generic.List<FooStruct>
{
}

public class DrivedClass
{

    public static void Method1(BaseClass c1)
    {
    }

    public static BaseClass Method1()
    {
        return null;
    }

}";
            var localConsumer = CreateCompilation(localTypeSource1);
            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("DrivedClass").Single();

            foreach (MethodSymbol m in classLocalType.GetMembersAsImmutable("Method1").OfType<MethodSymbol>())
            {
                if (m.Parameters.Length > 0)
                {
                    Assert.Equal(SymbolKind.Parameter, m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Kind);
                    Assert.True(m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.IsFromCompilation(localConsumer));
                }
                if (m.ReturnType.TypeKind != TypeKind.Struct)
                {
                    Assert.Equal(SymbolKind.NamedType, m.ReturnType.Kind);
                    Assert.True(m.ReturnType.IsFromCompilation(localConsumer));
                }
            }
        }

        [Fact]
        public void NoPIATypeImplementingAnInterfaceThatInheritsIGenericOfNoPiaType()
        {
            //Test implementing an interface that inherits IGeneric(Of NoPIAType)

            var localTypeSource = @"public struct Interface1Impl2 : Interface4
{
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembersAsImmutable("Interface1Impl2").Single();

            Assert.Equal(SymbolKind.NamedType, classLocalType.Kind);
            Assert.True(classLocalType.IsFromCompilation(localConsumer));
            Assert.True(classLocalType.IsValueType);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localConsumer = CreateCompilationWithMscorlib40(assemblyName: "Dummy", source: (string[])null,
                references: new[]
                {
                    TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1,
                                                               });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm.Where(a => a.Name == "NoPIAGenerics1-Asm1").Single().GlobalNamespace.GetTypeMembersAsImmutable("NestedConstructs").Single();
            var localField = nestedType.GetMembersAsImmutable("field1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ArrayType, localField.Type.Kind);
            Assert.Equal(SymbolKind.ErrorType, ((ArrayTypeSymbol)localField.Type).ElementType.Kind);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NoPIAGenericsAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localConsumer = CreateCompilation(null);

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm[1].GlobalNamespace.GetTypeMembersAsImmutable("NestedConstructs").Single();
            var localField = nestedType.GetMembersAsImmutable("field1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ArrayType, localField.Type.Kind);
            Assert.True(localField.Type is ArrayTypeSymbol);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NoPIAGenericsAssemblyRefs3()
        {
            //Test a static method that returns Generic(Of NoPIAType)

            var localConsumer = CreateCompilation(null);

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm[1].GlobalNamespace.GetTypeMembersAsImmutable("TypeRefs1").Single();
            var localMethod = nestedType.GetMembersAsImmutable("Method4").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localMethod.ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localMethod.ReturnType);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolforStaticMethodThatReturnsGenericOfNoPiaType()
        {
            //Test a static method that returns Generic(Of NoPIAType)

            var localTypeSource = @"
using System.Collections.Generic;
public class TypeRefs1
{
    public void Method1(List<FooStruct> x)
    {
    }
    public void Method2(List<ISubFuncProp> x = null)
    {
    }
    public interface Interface2
    {
        void Method3(List<FooEnum> x);
    }
    public List<ISubFuncProp> Method4()
    {
        return new List<ISubFuncProp>();
    }
    public List<FooStruct> Method4()
    {
        return null;
    }
}";

            var localType = CreateCompilation(assemblyName: "Dummy", source: localTypeSource,
                references: new[] { TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(true) });

            var localConsumer = CreateCompilation(assemblyName: "Dummy", source: (string[])null,
                references: new MetadataReference[]
                {
                    TestReferences.SymbolsTests.NoPia.GeneralPiaCopy,
                    new CSharpCompilationReference(localType)
                                                        });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm.First(arg => arg.Name == "Dummy").GlobalNamespace.GetTypeMembersAsImmutable("TypeRefs1").Single();
            var methodSymbol = nestedType.GetMembersAsImmutable("Method4").OfType<MethodSymbol>();

            foreach (MethodSymbol m in methodSymbol)
            {
                Assert.Equal(SymbolKind.ErrorType, m.ReturnType.Kind);
                Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.ReturnType);
            }
        }

        public CSharpCompilation CreateCompilation(string source)
        {
            return CreateCompilationWithMscorlib46(
                assemblyName: "Dummy",
                source: (null == source) ? null : new string[] { source },
                references: new[]
                {
                    TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1,
                    TestReferences.SymbolsTests.NoPia.GeneralPiaCopy
                });
        }
    }
}

