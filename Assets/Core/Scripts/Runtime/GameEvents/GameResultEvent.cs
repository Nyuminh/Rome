using UnityEngine;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Event triggered when the game ends.
    /// Payload is a boolean: true for Win, false for Lose.
    /// </summary>
    [CreateAssetMenu(fileName = "GameResultEvent", menuName = "Blocks/Events/GameResult Event")]
    public class GameResultEvent : GameEvent<bool>
    {
    }
}
