using System;
using System.Runtime.Intrinsics.Arm;
using NSubstitute;
using NUnit.Framework;

public class FakeDep { public IDep Object; }
public interface IDep { }

public class Service
{
  private readonly IDep _dep;

  public Service(IDep dep)
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
