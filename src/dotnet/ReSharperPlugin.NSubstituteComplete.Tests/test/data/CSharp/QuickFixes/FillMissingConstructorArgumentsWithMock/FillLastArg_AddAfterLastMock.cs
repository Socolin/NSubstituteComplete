using NUnit.Framework;

public class Service
{
  private readonly IDep1 _dep1;
  private readonly IDep2 _dep2;

  public Service(IDep1 dep1, IDep2 dep2)
  {
    _dep1 = dep1;
    _dep2 = dep2;
  }
}

public interface IDep1
{
}

public interface IDep2
{
}

public class ServiceTests
{
  private Service _service;
  private IDep1 _dep1;

  [SetUp]
  public void SetUp()
  {
    _dep1 = Substitute.For<IDep1>();

    var a = "";

    _service = new Service(_dep1{caret});
  }
}
