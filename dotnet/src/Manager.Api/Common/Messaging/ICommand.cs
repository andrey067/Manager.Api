namespace Manager.Api.Common.Messaging;

public interface ICommand { }

public interface ICommand<TResponse> : ICommand { }
