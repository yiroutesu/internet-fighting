// Assets/Scripts/Stats/IStatSystem.cs
using SIGame.Enums;

namespace SIGame.Stats
{
    public interface IStatSystem
    {
        float GetFinalValue(PlayerStatAttr attr);
    }
}