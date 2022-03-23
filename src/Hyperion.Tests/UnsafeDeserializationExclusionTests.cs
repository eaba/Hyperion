﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Hyperion.Internal;
using Xunit;
using FluentAssertions;
using Hyperion.Extensions;
using Xunit.Abstractions;

namespace Hyperion.Tests
{
    public class UnsafeDeserializationExclusionTests
    {
        private readonly ITestOutputHelper _output;
        
        public UnsafeDeserializationExclusionTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void CantDeserializeANaughtyType()
        {
            var serializer = new Hyperion.Serializer();
            var di =new System.IO.DirectoryInfo(@"c:\");

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(di, stream);
                stream.Position = 0;
                Assert.Throws<EvilDeserializationException>(() =>
                    serializer.Deserialize<DirectoryInfo>(stream));
            }
        }

        [Theory]
        [MemberData(nameof(DangerousObjectFactory))]
        public void DetectNaughtyTypesByDefault(Type dangerousType)
        {
            _output.WriteLine($"Testing for dangerous type [{dangerousType.AssemblyQualifiedName}]");
            TypeEx.IsDisallowedType(dangerousType).Should().BeTrue();
        }
/*
        X "System.Security.Principal.WindowsIdentity",
        X "System.Security.Principal.WindowsPrincipal",
        X "System.Security.Claims.ClaimsIdentity",
        X "System.Web.Security.RolePrincipal",
        X "System.Windows.Forms.AxHost.State",
        X "System.Windows.Data.ObjectDataProvider",
        X "System.Management.Automation.PSObject",
        X "System.IO.FileSystemInfo",
        X "System.IO.FileInfo",
        X "System.IdentityModel.Tokens.SessionSecurityToken",
        X "SessionViewStateHistoryItem",
        X "TextFormattingRunProperties",
        X "ToolboxItemContainer",
        X "System.CodeDom.Compiler.TempFileCollection",
        X "System.Activities.Presentation.WorkflowDesigner",
        X "System.Windows.ResourceDictionary",
        X "System.Windows.Forms.BindingSource",
        X "System.Diagnostics.Process",
        "System.Management.IWbemClassObjectFreeThreaded" // Need to have sharepoint installed, simulated
        "Microsoft.Exchange.Management.SystemManager.WinForms.ExchangeSettingsProvider", // Need to have ?Exchange? installed, simulated
        ??? "System.Security.Principal.WindowsClaimsIdentity", // This FQCN seemed to not exist in the past
 */
        public static IEnumerable<object[]> DangerousObjectFactory()
        {
            var isWindow = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            
            yield return new object[]{ typeof(System.IO.FileInfo) };
            yield return new object[]{ typeof(System.IO.FileSystemInfo) };
            yield return new object[]{ typeof(System.Security.Claims.ClaimsIdentity)};
            yield return new object[]{ typeof(System.Diagnostics.Process)};
            yield return new object[]{ typeof(System.CodeDom.Compiler.TempFileCollection)};
            yield return new object[]{ typeof(System.Management.IWbemClassObjectFreeThreaded)}; // SIMULATED
            yield return new object[]{ typeof(Microsoft.Exchange.Management.SystemManager.WinForms.ExchangeSettingsProvider)}; // SIMULATED
#if !NETFX
            yield return new object[]{ typeof(System.Management.Automation.PSObject)};
#endif
            
            if (isWindow)
            {
                yield return new object[]{ typeof(System.Security.Principal.WindowsIdentity) };
                yield return new object[]{ typeof(System.Security.Principal.WindowsPrincipal)};
#if NETFX
                var ass = typeof(System.Web.Mobile.MobileCapabilities).Assembly;
                var type = ass.GetType("System.Web.UI.MobileControls.SessionViewState+SessionViewStateHistoryItem");
                yield return new object[]{ type };

                yield return new object[]{ typeof(System.Drawing.Design.ToolboxItemContainer)};
                yield return new object[]{ typeof(System.Activities.Presentation.WorkflowDesigner)};
                yield return new object[]{ typeof(Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties)};
                yield return new object[]{ typeof(System.IdentityModel.Tokens.SessionSecurityToken)};
                yield return new object[]{ typeof(System.Web.Security.RolePrincipal) };
                yield return new object[]{ typeof(System.Windows.Forms.AxHost.State)};
                yield return new object[]{ typeof(System.Windows.Data.ObjectDataProvider)};
                yield return new object[]{ typeof(System.Windows.ResourceDictionary)};
                yield return new object[]{ typeof(System.Windows.Forms.BindingSource)};
#endif
            }
        }

        internal class ClassA
        { }
        
        internal class ClassB
        { }
        
        internal class ClassC
        { }
        
        [Fact]
        public void TypeFilterShouldThrowOnNaughtyType()
        {
            var typeFilter = TypeFilterBuilder.Create()
                .Include<ClassA>()
                .Include<ClassB>()
                .Build();

            var options = SerializerOptions.Default
                .WithTypeFilter(typeFilter);

            var serializer = new Serializer(options);
            
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new ClassA(), stream);
                stream.Position = 0;
                Action act = () => serializer.Deserialize<ClassA>(stream);
                act.Should().NotThrow();
                
                stream.Position = 0;
                Action actObj = () => serializer.Deserialize<object>(stream);
                actObj.Should().NotThrow();
            }

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new ClassB(), stream);
                stream.Position = 0;
                Action act = () => serializer.Deserialize<ClassB>(stream);
                act.Should().NotThrow();
                
                stream.Position = 0;
                Action actObj = () => serializer.Deserialize<object>(stream);
                actObj.Should().NotThrow();
            }
            
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new ClassC(), stream);
                stream.Position = 0;
                Action act = () => serializer.Deserialize<ClassC>(stream);
                act.Should().Throw<UserEvilDeserializationException>();
                
                stream.Position = 0;
                Action actObj = () => serializer.Deserialize<object>(stream);
                actObj.Should().Throw<UserEvilDeserializationException>();
            }
        }
    }
}

namespace System.Management
{
    public interface IWbemClassObjectFreeThreaded{ }
}

namespace Microsoft.Exchange.Management.SystemManager.WinForms
{
    public class ExchangeSettingsProvider { }
}