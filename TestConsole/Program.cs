using ProcessSlayer.Core;

public class Tits
{
    public static void Main()
    {
        Wrapper unw = new();
        unw.KillProtectedProcessHandles(8076, Operation.FullKill);
    }
}
