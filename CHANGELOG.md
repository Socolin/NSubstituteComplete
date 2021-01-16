# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 1.3.2 - 2020.01-16
Improve Mock aliases. Support more complex scenario with Mock Aliases. As following example
```c#
IGenericDep<IDep1> => new FakeGenericDep1()
IGenericDep<IDep2> => new FakeGenericDep2()
```

## 1.3.1 - 2020.01-16
Improve "QuickFix: Generate missing arguments as mock" it will work better when trying to add new argument to an already mocked constructor.

## 1.3.0 - 2020.01-15
- New feature: MockAliases, see the documentation for more information

## 1.2.0 - 2020.01-14
- Add new Completion: Suggest `Substitute.For<>`
- Improve condition when Quick fix and Completions are shown. It now check for `NSubstitute` assembly.

## 1.1.0 - 2020-01-12
- Improve "QuickFix: Generate missing arguments as mock" now it add mock initializer with the others.
- Improve "Completion: Auto fill argument with `Arg.Any` `Arg.Is`". Automatically add missing `using` when referencing a type. Also improved when it's proposed

## 1.0.0 - 2020-01-05
- Initial version
- QuickFix: Generate missing arguments as mock.
- Completion: Auto fill argument with `Arg.Any` `Arg.Is`
