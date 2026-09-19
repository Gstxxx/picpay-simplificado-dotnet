namespace PicPay.Domain;

internal static class SystemTime
{
    // Postgres guarda timestamps com precisão de microssegundos.
    public static DateTimeOffset UtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Ticks - now.Ticks % 10, TimeSpan.Zero);
    }
}
