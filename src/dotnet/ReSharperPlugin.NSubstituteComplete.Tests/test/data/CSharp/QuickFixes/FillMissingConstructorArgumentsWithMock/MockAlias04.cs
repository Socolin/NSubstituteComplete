using System;
using System.Runtime.Intrinsics.Arm;
using NSubstitute;
using NUnit.Framework;

public class FakeDep<TKey, TValue> : IDep<TKey, TValue> { }
public interface IDep<TKey, TValue> { }

public class Service
{
  private readonly IDep<string, int> _dep;

  public Service(IDep<string, int> dep)
  {
    _dep = dep;
  }
}

public class ServiceTests
{
  [SetUp]
  public void Setup()
  {
    _service = new Service({caret});
  }
}
