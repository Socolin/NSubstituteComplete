using NUnit.Framework;

public class Service
{
  private readonly IDep1 _dep1;

  public Service(IDep1 dep1)
  {
    _dep1 = dep1;
  }
}

public interface IDep1
{
}

public class ServiceTests
{
  private Service _service;

  [SetUp]
  public void SetUp()
  {
    _service = new Service({caret});
  }
}
