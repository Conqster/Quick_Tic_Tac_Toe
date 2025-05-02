# Quick Tic-Tac-Toe

## Summary
A quick prototype of a Tic-Tac-Toe game, utilising bitwise operations for efficient, low-level game state representation and outcome resolution - a performance-conscious approach within a high-level engine (Unity).

### Input Representation
Each cell in the 3 x 3 Tic-Tac-Toe grid is assigned a unique binary using powers of 2 (bit flags).
The flags are mapped as follows:

```
Index:  0   1   2   3   4   5   6   7   8
Binary: 256 128 64  32  16  8   4   2   1

Board:
     0     1     2
     3     4     5
     6     7     8
```
The formula for conversion is: 
flag = 2 ^ (8 -n), where n is the cell index (0 to 8, left to right, top to bottom).

### Win Conditions
Winning patterns are precomputed using bitwise masks for fast evaluation. A win is determined by comparing a player's current cell state with each winning mask using bitwise AND.


Visual examples of win patterns:
```
    possible wins
    x   x   x
    o   o   o
    o   o   o
    outcome: x x x o o o o o o = 448
    o o o x x x o o o = 56
    o o o o o o x x x = 7   
    x   o   o
    x   o   o
    x   o   o
    outcome: x o o x o o x o o = 292
    o x o o x o o x o = 146
    o o x o o x o o x = 73  
    x   o   o
    o   x   o
    o   o   x
    outcome: x o o o x o o o x = 273
    o o x o x o x o o = 84
```

Win Detection Code (Unity C#):
```
    bool complete = false;
    
    ...
    ...
    ...

    int[] win_masks = { 448, 56, 7, 292, 146, 73, 273, 84 }; // win masks
    int curr_player_slot = (curr_player == 0) ? m_Player0Slot : m_Player1Slot;
    for (int i = 0;i < win_masks.Length;i++)
    {
        complete |= ((curr_player_slot & win_masks[i]) == win_masks[i]);
        
        //if a win condition is met, handle game reset
        if(complete)
        {
            m_WinningPlayerIdx = curr_player;
            m_ResetGame = true;
            return win_masks[i];
        }
    }
```

## Quick Demo

|<a href="https://youtu.be/IluDpkAbcWs"><img src = "Tic-Tac-Toe Screenshot.png"/>|
|:-|
|<p align = "center"> *A Youtube video of in-game sample.* </p>|
