using System;
using System.Runtime.Intrinsics.Arm;
using NSubstitute;
using NUnit.Framework;

public class FakeDep : IDep<string> { }
public interface IDep<T> where T : class { }

public class Service
{
  private readonly IDep _dep;

  public Service(IDep<string> dep)
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
