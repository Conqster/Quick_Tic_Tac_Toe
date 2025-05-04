using Microsoft.Unity.VisualStudio.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class BoardGameManager : MonoBehaviour
{

    [SerializeField] private Board m_Board = null;

    [SerializeField] private int m_CurrPlayerIdx = 0;
    [SerializeField] private Transform m_PlayerIdx0Prefab = null;
    [SerializeField] private Transform m_PlayerIdx1Prefab = null;

    [SerializeField] private GameObject[] m_TrackObjectCreation = new GameObject[3*3];
    [SerializeField] private int m_NextObjectCreationSlot = 0;

    /*
    //current state against outcome
    possible wins
    x   x   x
    o   o   o
    o   o   o
    outcome: x x x o o o o o o
    o o o x x x o o o 
    o o o o o o x x x

    x   o   o
    x   o   o
    x   o   o

    outcome: x o o x o o x o o
    o x o o x o o x o
    o o x o o x o o x

    x   o   o
    o   x   o
    o   o   x
    outcome: x o o o x o o o x
    o o x o x o x o o
 */

    [Space]
    [Header("Game Parameter/Utilities")]
    [SerializeField] private int m_Player0Slot = 0;
    [SerializeField] private int m_Player1Slot = 0;
    [SerializeField] private int m_CachePatternOutput = 0;
    [SerializeField] private int[] m_CachePatternOutputGridIdxs = new int[2];
    [SerializeField] private bool m_ResetGame = false;
    [SerializeField, Range(0.1f, 5.0f)] private float m_GameResetDelay = 2.0f;
    //bytes can only store up to 255 by there are 8 cell (256) 
    //so store each less than one, and when need at one to use
    //fisrt element 0 tracks the current slot/cell to write to
    [SerializeField] private Stack<byte>m_GameplayPiecePlacement = new Stack<byte>();

    [Space]
    [Header("Gameplay Parameters/Utilities")]
    [SerializeField] private int m_WinningPlayerIdx = 0;
    [SerializeField] private int m_Player0Score = 0;
    [SerializeField] private int m_Player1Score = 0;
    [SerializeField] private int m_Draws = 0;

    [Space]
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI m_TextMeshProUGUI = null;
    [SerializeField] private Transform m_Player1Zone = null;
    [SerializeField] private Transform m_Player2Zone = null;
    [SerializeField] private TextMeshProUGUI m_PlayerPlayingTMPro;
    [SerializeField] private TextMeshProUGUI m_Player0ScoreUI = null;
    [SerializeField] private TextMeshProUGUI m_Player1ScoreUI = null;


    [Space]
    [Header("Debugging")]
    [SerializeField] private int m_PrevClosestGrid;
    [SerializeField] private Vector3 m_DebugTouchPos = Vector3.zero;
    private void Awake()
    {
        for (int i = 0; i < m_CachePatternOutputGridIdxs.Length; i++)
            m_CachePatternOutputGridIdxs[i] = -1;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(m_TextMeshProUGUI != null)
            m_TextMeshProUGUI.gameObject.SetActive(false);

        UpdateGameplayUI(0); // <- quick hack 

    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_ResetGame = true;
            //quick hack
            //to make sure none of the player get scored
            m_WinningPlayerIdx = -1;
            m_CachePatternOutput = 0;
        }

        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            int v = GetGameplayLastPiecePlacement();
            if (v < 0)
            {
                Debug.Log("No Piece/Gameplay to rewind.");
                return;
            }
            Debug.Assert(v < 9, "piece idx is outside array bounds");
            //need to clear player slot
            int v_mask = (int)Mathf.Pow(2, 8-v);
            if((v_mask&m_Player0Slot) == v_mask)
            {
                //this player owns this slot
                //clear
                //
                //sample
                //111 100 000  - player
                //000 100 000  = mask

                //111 000 000   XOR
                m_Player0Slot ^= v_mask;
            }
            //check player 2 as well
            if ((v_mask & m_Player1Slot) == v_mask)
                m_Player1Slot ^= v_mask;
            //clear board assignemnt 
            m_Board.RemoveCellFlagStateByIdx(v);
            //m_Board.RemoveCellFlagStateWithBinaryMask(v_mask);

            //clear visuals
            //quick hack get pos & compare with cached 
            DestroyLastCreatedPieceVisuals();
            //prev player because player 
            NextPlayer();
        }

        if(!m_ResetGame)
            CheckMouseClick();
        else
        {
            StartCoroutine(ResetGame());
            m_ResetGame = false;
        }
    }



    private void NextPlayer()
    {
        m_CurrPlayerIdx = (m_CurrPlayerIdx + 1) % 2;
        UpdateGameplayUI(m_CurrPlayerIdx);
    }


    private void DestroyLastCreatedPieceVisuals()
    {
        Destroy(m_TrackObjectCreation[m_NextObjectCreationSlot - 1]);
        m_NextObjectCreationSlot--;
    }

    private IEnumerator ResetGame()
    {
        WinStateUpdateAndUI(m_WinningPlayerIdx);
        //if not 0 there is a winning player
        if(m_CachePatternOutput != 0)
        {
            UpdateGameplayUI(m_WinningPlayerIdx); // <- quick hack 
            int highest_bit = m_CachePatternOutput;
            while((highest_bit & (highest_bit - 1)) != 0)
                highest_bit &= highest_bit - 1; //clears the lowest set bit each iteration 

            int lowest_bit = m_CachePatternOutput & -m_CachePatternOutput;
            //int lowest_bit = m_CachePatternOutput & ~(m_CachePatternOutput - 1);

            //out put would be is inverted 8 -> 0 
            int idx_a = (int)Mathf.Log(highest_bit, 2) - 8;
            int idx_b = (int)Mathf.Log(lowest_bit, 2) - 8;
            m_CachePatternOutputGridIdxs[0] = Mathf.Abs(idx_a);
            m_CachePatternOutputGridIdxs[1] = Mathf.Abs(idx_b);
        }
        

        yield return new WaitForSeconds(m_GameResetDelay);
        m_NextObjectCreationSlot = 0;
        m_Player0Slot = 0;
        m_Player1Slot = 0;
        for (int i = 0; i < m_TrackObjectCreation.Length; i++)
        {
            if (m_TrackObjectCreation[i])
            {
                Destroy(m_TrackObjectCreation[i]);
                m_TrackObjectCreation[i] = null;
            }
        }
        m_Board.ResetBoard();

        m_CachePatternOutput = 0;
        for(int i = 0; i < m_CachePatternOutputGridIdxs.Length; i++)
            m_CachePatternOutputGridIdxs[i] = -1;

        m_CurrPlayerIdx = 0;
        m_TextMeshProUGUI.gameObject.SetActive(false);
        UpdateGameplayUI(0); // <- quick hack 
        m_GameplayPiecePlacement.Clear(); // <-- quick hack
    }


    private int CheckGameOverAndReturnPattern(int curr_player)
    {
        bool complete = false;
        /*
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

        int[] win_masks = { 448, 56, 7, 292, 146, 73, 273, 84 }; // win patterns
        int curr_player_slot = (curr_player == 0) ? m_Player0Slot : m_Player1Slot;
        for (int i = 0;i < win_masks.Length;i++)
        {
            complete |= ((curr_player_slot & win_masks[i]) == win_masks[i]);
            //early out and output
            if(complete)
            {
                m_WinningPlayerIdx = curr_player;
                m_ResetGame = true;
                return win_masks[i];
            }
        }

        complete |= (m_NextObjectCreationSlot >= 9);

        if (complete)
        {
            m_ResetGame = true;
            m_WinningPlayerIdx = -1;
            return 0;
        }


        return 0;
    }


    private void UpdateAndVisualisePieces(int player_idx, int piece_slot, Vector2 point)
    {
        if(player_idx == 0)
        {
            //2 ^ (8 - n)
            m_Player0Slot += (int)Mathf.Pow(2, 8 - piece_slot);
            Debug.Log("PLayer 0 slot: " + Convert.ToString(m_Player0Slot, 2));
        }
        else if(player_idx == 1)
        {
            //2 ^ (8 - n)
            m_Player1Slot += (int)Mathf.Pow(2, 8 - piece_slot);
            Debug.Log("PLayer 1 slot: " + Convert.ToString(m_Player1Slot, 2));
        }
        if (player_idx == 0 && m_PlayerIdx0Prefab != null)
        {
            m_TrackObjectCreation[m_NextObjectCreationSlot++] = Instantiate(m_PlayerIdx0Prefab, point, Quaternion.identity, transform).gameObject;
            Debug.Log("Draw Pieces");
        }
        else if(player_idx ==1 && m_PlayerIdx1Prefab != null)
            m_TrackObjectCreation[m_NextObjectCreationSlot++] = Instantiate(m_PlayerIdx1Prefab, point, Quaternion.identity, transform).gameObject;
    }


    private void CheckMouseClick()
    {
        if (m_Board == null)
            return;

        if(Input.GetMouseButtonDown(0))
        {
            Vector3 mouse_position = Input.mousePosition;
            Camera camera = Camera.main;
            //mouse_position = camera.ScreenToViewportPoint(mouse_position)
            mouse_position = camera.ScreenToWorldPoint(mouse_position);
            Debug.Log("Mouse Pressed: " + mouse_position);


            var board_grid_points = m_Board.GetCacheGridCoords;
            int closest = -1;
            float diff = m_Board.GetGridHalfSize;// int.MaxValue; //could be the lendth so to make fail
            for (int i = 0; i < board_grid_points.Length; i++)
            {
                float curr_diff = Vector2.Distance(board_grid_points[i], new Vector2(mouse_position.x, mouse_position.y));
                if(curr_diff < diff)
                {
                    diff = curr_diff;
                    closest = i;
                }
            }
            m_PrevClosestGrid = closest;
            //set state
            if(closest > -1)
            {
                Debug.Assert(!m_Board.GetGridCellFlagState(closest), "Board slot has already been assign to during this game!!!");
                if (m_Board.UseGridCoord(closest))
                {
                    UpdateAndVisualisePieces(m_CurrPlayerIdx, closest, m_Board.GetGridCoord(closest));
                    CacheCellPiecePlayedPlacement(closest);
                    m_CachePatternOutput = CheckGameOverAndReturnPattern(m_CurrPlayerIdx);
                    NextPlayer();
                }
            }
        }
    }

    private void UpdateGameplayUI(int idx)
    {
        if(idx == 0)
        {
            m_Player1Zone.gameObject.SetActive(true);
            m_Player2Zone.gameObject.SetActive(false);
            m_PlayerPlayingTMPro.text = "Playing: Player One";
            m_PlayerPlayingTMPro.color = Color.blue;
        }
        else if(idx == 1)
        {
            m_Player1Zone.gameObject.SetActive(false);
            m_Player2Zone.gameObject.SetActive(true);
            m_PlayerPlayingTMPro.text = "Playing: Player Two";
            m_PlayerPlayingTMPro.color = Color.red;
        }
    }


    private void OnDrawGizmos()
    {
        if (m_CachePatternOutputGridIdxs[0] != -1)
        {
            int idx_a = m_CachePatternOutputGridIdxs[0];
            int idx_b = m_CachePatternOutputGridIdxs[1];
            Debug.DrawLine(m_Board.GetCacheGridCoords[idx_a], m_Board.GetCacheGridCoords[idx_b]);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(m_DebugTouchPos, 0.2f);
    }




    private void WinStateUpdateAndUI(int idx)
    {
        m_TextMeshProUGUI.gameObject.SetActive(true);
        if(idx != 0 && idx != 1)
        {
            m_TextMeshProUGUI.text = "Draw";
            m_TextMeshProUGUI.color = Color.blue + Color.red;
            m_Draws++;
        }
        else
        {
            m_TextMeshProUGUI.text = (idx == 0) ? "Player One Wins" : "Player Two Wins";
            m_TextMeshProUGUI.color = (idx == 0) ? Color.blue : Color.red;
            if (idx == 0)
            {
                m_Player0Score++;
                m_Player0ScoreUI.text = m_Player0Score.ToString();
            }
            else if(idx == 1)
            {
                m_Player1Score++;
                m_Player1ScoreUI.text = m_Player1Score.ToString();
            }
        }
    }



    void CacheCellPiecePlayedPlacement(int idx)
    {
        //bytes can only store up to 255 by there are 8 cell (256) 
        //so store each less than one, and when need at one to use
        m_GameplayPiecePlacement.Push((byte)idx);
    }

    int GetGameplayLastPiecePlacement()
    {
        if (m_GameplayPiecePlacement.Count <= 0)
            return -1;
        return (int)m_GameplayPiecePlacement.Pop();
    }

}
