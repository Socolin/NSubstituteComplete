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

This quick fix support the "Mock Aliases" feature. This allow you to define (In the _Unit Testing Settings_ panel) a list of type that should not be mocked with `Substitute.For<>` but with an explicit type instead. This is useful for complex type that required a lot of mocking.

Here an [example](https://github.com/Socolin/NaheulbookBackend/blob/master/Naheulbook.Core.Tests.Unit/TestUtils/FakeUnitOfWorkFactory.cs) of such a type

[![Mock Aliases example](doc/images/MockedAliases.gif)](doc/images/MockedAliases.gif)


### Completion: Suggest Arg.Any / Arg.Is

![Completion Arg.Any](doc/images/Completion-ArgAnyIs.gif)

## Notes

This plugin was inspired by [MoqComplete](https://github.com/Abc-Arbitrage/Abc.MoqComplete).
Thanks to all the people on the `#dotnet-pluginwriters` slack channel for their help !

## Other plugins that may interest you

|Rider | Resharper |
|-----|------|
|[RenameRelatedTests](https://plugins.jetbrains.com/plugin/15795-renamerelatedtests)|[RenameRelatedTests](https://plugins.jetbrains.com/plugin/15800-renamerelatedtests)|
|[AutocompleteTestStrings](https://plugins.jetbrains.com/plugin/15796-autocompleteteststrings)|[AutocompleteTestStrings](https://plugins.jetbrains.com/plugin/15802-autocompleteteststrings)|
