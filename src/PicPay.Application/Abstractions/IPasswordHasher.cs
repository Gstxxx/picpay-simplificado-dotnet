namespace PicPay.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Com <paramref name="hash"/> nulo, compara contra um hash fixo e retorna false,
    /// para que login com email inexistente leve o mesmo tempo que senha errada.
    /// </summary>
    bool Verify(string password, string? hash);
}
