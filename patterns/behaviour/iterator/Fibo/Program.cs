using System.Diagnostics;
using System.Numerics;

if (args.Length != 1)
{
    Environment.Exit(1);
}

if(!int.TryParse(args[0], out var argCount))
{
    Environment.Exit(1);
}

var stopwatch = new Stopwatch();
stopwatch.Start();

foreach (var valueTuple in Fibonacci(argCount).Select((fib, i) => (fibo: fib, Index: i)))
{
    Console.WriteLine($"{valueTuple.Index.ToString(),3}: {valueTuple.fibo}; {stopwatch.ElapsedMilliseconds}ms");
}
return;


// Итератор, представляющий бесконечную коллекцию
// чисел Фибоначчи.
IEnumerable<BigInteger> Fibonacci(int? requestedCount = null)
{
    BigInteger past = 0;
    BigInteger present = 1;
    BigInteger sum;
    int count = 0;
    int parsedCount = requestedCount ?? -1;
    yield return past;
    yield return present;
    while (true)
    {
        if (count == parsedCount)
        {
            yield break;
        }
        count++;

        try
        {
            checked
            {
                sum = past + present;
                past = present;
                present = sum;
            }
        }
        catch (OverflowException)
        {
            yield break;
        }

        yield return sum;
    }
}
