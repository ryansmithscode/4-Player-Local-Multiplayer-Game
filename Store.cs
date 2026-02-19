using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ryan Smith 4 Player Local Multiplayer Game Script

public class Store : MonoBehaviour
{
    public static Store instance = null;
    private static Dictionary<int, CharacterHold> playerList = new Dictionary<int, CharacterHold>(); // Survives scene change

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // The one and only!
            DontDestroyOnLoad(gameObject); // Allows script to move between scenes without being deleted
        }
        else
        {
            Destroy(gameObject); // Overkill but makes sure it's the only one
        }
    }

    public static void AddCharacter(CharacterHold pass, int playerNum)
    {
        playerList.Add(playerNum, pass); // Stores the player's 'data' on the controller number
    }

    public static CharacterHold GetCharacterInfo(int playerNum) // Literally as it says, just gets it correlating to the number that player is 
    {
        playerList.TryGetValue(playerNum, out CharacterHold returnVal);
        return returnVal;
    }
}