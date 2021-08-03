# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 1.5.0 - 2021.08-03
- Rider 2021.2

## 1.4.4 - 2021.07-20
- Update to support 2021.1.4 (unit testing settings options was not loading)

## 1.4.3 - 2021.07-16
- Fix completion not working when trying to complete Substitute.For<> in variable initializer

## 1.4.2 - 2021.07-13
- Fix completion not working when trying to complete Substitute.For<> in object initializer and in variable assignation
- Fix completion not working when trying to complete Arg.*<> with array type like `byte[]`

## 1.4.1 - 2021.04-27 
- Fix error when opening unit tests panel

## 1.4.0 - 2021.04-19
- Rider 2021.1

## 1.3.3 - 2021.01-17
- Improve "QuickFix: Generate missing arguments as mock", when adding a new mock, mock aliases are not ignored anymore when looking for the last mock initializer, so mock initializer will place placed after the other one instead of some unrelated location in some scenario

Before (After adding `_dep3`):
```c#
_dep1 = Substitute.For<IDep1>();
_dep3 = Substitute.For<IDep3>();
_dep2 = new FakeDep2();

_service = new Service(_dep1, _dep2, _dep3)
```
Now:
```c#
_dep1 = Substitute.For<IDep1>();
_dep2 = new FakeDep2();
_dep3 = Substitute.For<IDep3>();

_service = new Service(_dep1, _dep2, _dep3)
```

- Improve "QuickFix: Generate missing arguments as mock": It will only apply changes to the constructor instead of overwriting the constructor invocation.

This following code, when using quickFix to add `_dep9`:

```c#
_service = new Service(
    _dep1,
#pragma warning disable 618
    _dep2,
#pragma warning restore 618
    _dep3, _dep4, _dep5, _dep6, _dep7.Object);
```
Was transformed to:
```c#
_service = new Service(_dep1, _dep2, _dep3, _dep4, _dep5, _dep6, _dep7.Object, _dep9);
```
Now it will be transformed to:
```c#
_service = new Service(
    _dep1,
#pragma warning disable 618
    _dep2,
#pragma warning restore 618
    _dep3, _dep4, _dep5, _dep6, _dep7.Object, _dep9);
```


## 1.3.2 - 2020.01-16
- Improve Mock aliases. Support more complex scenario with Mock Aliases. As following example
```c#
IGenericDep<IDep1> => new FakeGenericDep1()
IGenericDep<IDep2> => new FakeGenericDep2()
```

## 1.3.1 - 2020.01-16
- Improve "QuickFix: Generate missing arguments as mock" it will work better when trying to add new argument to an already mocked constructor.

## 1.3.0 - 2020.01-15
- New feature: MockAliases, see the documentation for more information

## 1.2.0 - 2020.01-14
- Add new Completion: Suggest `Substitute.For<>`
- Improve condition when Quick fix and Completions are shown. It now checks for `NSubstitute` assembly.

## 1.1.0 - 2020-01-12
- Improve "QuickFix: Generate missing arguments as mock" now it adds mock initializer with the others.
- Improve "Completion: Auto fill argument with `Arg.Any` `Arg.Is`". Automatically add missing `using` when referencing a type. Also improved when it's proposed

## 1.0.0 - 2020-01-05
- Initial version
- QuickFix: Generate missing arguments as mock.
- Completion: Auto fill argument with `Arg.Any` `Arg.Is`
