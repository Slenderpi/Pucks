using UnityEngine;

/// <summary>
/// A PuckLevel is a class that handles the movement, collision detection/logic
/// </summary>
public interface IPuckLevel {


    /// <summary>
    /// Step the level.
    /// </summary>
    /// <returns></returns>
    public int Step();
}
