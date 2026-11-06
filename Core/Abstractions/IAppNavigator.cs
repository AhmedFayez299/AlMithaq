namespace AlMithaq.Core.Abstractions;

public interface IAppNavigator
{
    Task GoToAsync(string route);
    Task GoBackAsync();
}
