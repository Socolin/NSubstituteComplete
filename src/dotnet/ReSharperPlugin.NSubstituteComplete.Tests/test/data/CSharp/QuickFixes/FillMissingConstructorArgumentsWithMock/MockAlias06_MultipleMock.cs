using System;
using System.Runtime.Intrinsics.Arm;
using NSubstitute;
using NUnit.Framework;

public interface IDep1 { }
public interface IDep2 { }

public class FakeDep31 : IDep3<IDep31> { }
public class FakeDep32 : IDep3<IDep32> { }
public interface IDep3<T> { }

public class Service
{
  private readonly IDep3<IDep2> _dep;

  public Service(IDep3<IDep2> dep)
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
