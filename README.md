# NSubstituteComplete

A Rider / Resharper Plugin that provide quick fixes and smart autocomplete when using [NSubstitute](https://nsubstitute.github.io/)
[![JetBrains Plugins](https://img.shields.io/jetbrains/plugin/v/15798)](https://plugins.jetbrains.com/plugin/15798-nsubstitutecomplete)
[![JetBrains ReSharper plugins Version](https://img.shields.io/resharper/v/ReSharperPlugin.NSubstituteComplete)](https://plugins.jetbrains.com/plugin/15801-nsubstitutecomplete)
![GitHub](https://img.shields.io/github/license/Socolin/NSubstituteComplete)


## Build plugin

```shell
./gradlew :buildPlugin
```

## Functionality

### QuickFix: Fill with mocks

![Fill with mocks example](doc/images/QuickFix-FillWithMocks.gif)

### Mock aliases

This quick fix support the "Mock Aliases" feature. This allows you to define (In the _Unit Testing Settings_ panel) a list of type that should not be mocked with `Substitute.For<>` but with an explicit type instead. This is useful for complex type that required a lot of mocking.

Here an [example](https://github.com/Socolin/NaheulbookBackend/blob/master/Naheulbook.Core.Tests.Unit/TestUtils/FakeUnitOfWorkFactory.cs) of such a type

Example of configuration
```c#
IDep1 => FakeDep1()
IDep2<T> = FakeDep2()
IDep3<T> = FakeDep3<T>
IDep4 = FakeDep4<T>
IGenericDep<IDep1> = FakeGenericDep1()
IGenericDep<IDep2> = FakeGenericDep2()
```

If the `Fake` class does not match `IDep` it will search into the members of the `Fake` class a matching member.

[![Mock Aliases example](doc/images/MockedAliases.gif)](doc/images/MockedAliases.gif)


### Completion: Suggest Arg.Any / Arg.Is

![Completion Arg.Any](doc/images/Completion-ArgAnyIs.gif)

## Notes

This plugin was inspired by [MoqComplete](https://github.com/Abc-Arbitrage/Abc.MoqComplete).
Thanks to all the people on the `#dotnet-pluginwriters` Slack channel for their help !

## Other plugins that may interest you

| Plugin |Rider | Resharper |
|-----|-----|------|
| RenameRelatedTests |[![RenameRelatedTestsRirder](https://img.shields.io/jetbrains/plugin/v/15795)](https://plugins.jetbrains.com/plugin/15795-renamerelatedtests)|[![RenameRelatedTestsResharper](https://img.shields.io/resharper/v/ReSharperPlugin.RenameRelatedTests)](https://plugins.jetbrains.com/plugin/15800-renamerelatedtests)|
| AutocompleteTestStrings |[![AutocompleteTestStringsRider](https://img.shields.io/jetbrains/plugin/v/15796)](https://plugins.jetbrains.com/plugin/15796-autocompleteteststrings)|[![AutocompleteTestStringsRider](https://img.shields.io/resharper/v/ReSharperPlugin.AutocompleteTestStrings)](https://plugins.jetbrains.com/plugin/15802-autocompleteteststrings)|
