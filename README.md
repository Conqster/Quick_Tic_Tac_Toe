# Quick Tic-Tac-Toe

## Summary
A quick prototype of a Tic-Tac-Toe game, utilising Bitwise operation for low-level optimisation and resolving game outcome. 


/*
         Each cell in the Tic-Tac-Toe grid is assigned a uniques binary flag:
         input set
         0 = 100000000 = 256
         1 = 010000000 = 128
         2 = 001000000 = 64
         3 = 000100000 = 32
         4 = 000010000 = 16
         5 = 000001000 = 8
         6 = 000000100 = 4
         7 = 000000010 = 2
         8 = 000000001 = 1
         */
        //2 ^ (8 - n)



        /*
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
         */

        int[] win_masks = { 448, 56, 7, 292, 146, 73, 273, 84 }; // win maska
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

|<a href="https://youtu.be/IluDpkAbcWs"><img src = "Docs/Screenshot.png"/>|
|:-|
|<p align = "center"> *A Youtube video of in-game sample.* </p>|
