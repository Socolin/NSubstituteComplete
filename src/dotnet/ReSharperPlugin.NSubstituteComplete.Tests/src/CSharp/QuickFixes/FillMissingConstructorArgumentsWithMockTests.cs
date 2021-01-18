using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.NSubstituteComplete.Options;
using ReSharperPlugin.NSubstituteComplete.QuickFixes;

namespace ReSharperPlugin.NSubstituteComplete.Tests.CSharp.QuickFixes
{
    [TestPackages("NSubstitute/4.2.2")]
    [TestNetFramework46]
    [TestCSharpNamingPredefinedRules(NamedElementKinds.PrivateInstanceFields, "_", NamingStyleKinds.aaBb, "")]
    public class FillMissingConstructorArgumentsWithMockTests : CSharpQuickFixTestBase<FillMissingConstructorArgumentsWithMock>
    {
        protected override string RelativeTestDataPath => @"CSharp\QuickFixes\FillMissingConstructorArgumentsWithMock";

        [Test] public void FillFromEmpty_Simple01() { DoNamedTest(); }
        [Test] public void FillFromEmpty_Simple02() { DoNamedTest(); }
        [Test] public void FillLastArg_AddAfterLastMock() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep3<T>=FakeDep3;IDep4=FakeDep4;IDep5=FakeDep5<T>;IDep6<TKey,TValue>=FakeDep6<TKey,TValue>;IDep7=FakeDep7;IDep9<IDep1>=FakeDep91;IDep9<IDep2>=FakeDep92")]
        [Test] public void FillLastArg_ComplexAliases() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep<T>=FakeDep")]
        [Test] public void MockAlias01() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep=FakeDep")]
        [Test] public void MockAlias02() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep=FakeDep<T>")]
        [Test] public void MockAlias03() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep<TKey, TValue>=FakeDep<TKey, TValue>")]
        [Test] public void MockAlias04() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep=FakeDep")]
        [Test] public void MockAlias05_UseMatchingField() { DoNamedTest(); }
        [TestSetting(typeof(NSubstituteCompleteSettings), nameof(NSubstituteCompleteSettings.MockAliases), "IDep3<IDep1>=FakeDep31;IDep3<IDep2>=FakeDep32")]
        [Test] public void MockAlias06_MultipleMock() { DoNamedTest(); }
    }
}
