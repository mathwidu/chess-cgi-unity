using System;

// A session owns one local UCI process. Null means a read timed out; EOF is an error.
public interface IStockfishSession : IDisposable
{
    void Send(string command);
    string ReadLine(int timeoutMilliseconds);
}

public interface IStockfishSessionFactory : IDisposable
{
    IStockfishSession Open();
}
