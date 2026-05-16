namespace JuHuaLiPet;

internal sealed class PetSettings
{
    public double PetScale { get; set; } = 1.0;

    public double Volume { get; set; } = 0.9;

    public void Clamp()
    {
        PetScale = Math.Clamp(PetScale, 0.65, 1.7);
        Volume = Math.Clamp(Volume, 0.0, 1.0);
    }
}
