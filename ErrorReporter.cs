using System.Collections;

namespace Cml;

public class ErrorReporter : IEnumerable<Error>
{
    public readonly List<Error> Errors = [];

    public void Append(Error error)
        => Errors.Add(error);

    public void Append(string message, Location location, ErrorType level = ErrorType.Error)
        => Errors.Add(new(message, level, location));

    public int Count { get => Errors.Count; }

    public int CountLevel(ErrorType level)
         => (from er in Errors where er.Level == level select er).Count();

    public IEnumerator<Error> GetEnumerator()
        => Errors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}