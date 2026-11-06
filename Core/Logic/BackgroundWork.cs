namespace AlMithaq.Core.Logic;

public static class BackgroundWork
{
    public static Task<T> Run<T>(Func<T> work, CancellationToken token = default) => Task.Run(work, token);
    public static Task<T> Run<T>(Func<Task<T>> work, CancellationToken token = default) => Task.Run(work, token);
    public static Task Run(Func<Task> work, CancellationToken token = default) => Task.Run(work, token);
}