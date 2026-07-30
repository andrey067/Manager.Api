using Manager.Api.Common;

namespace Manager.Api.Features.Users;

public static class UserErrors
{
    public static Error NotFound()
        => Error.NotFound(
            "Users.NotFound",
            "Não existe nenhum usuário com o id informado.");

    public static Error EmailConflict()
        => Error.Conflict(
            "Users.EmailConflict",
            "Já existe um usuário cadastrado com o email informado.");

    public static Error BootstrapNotAllowed()
        => Error.Problem(
            "Users.BootstrapNotAllowed",
            "O registro inicial só é permitido quando não existem usuários cadastrados.");
}
