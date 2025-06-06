using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField, Range(0.0f, 20.0f)] private float m_Width = 2.0f;


    [SerializeField] private bool m_UpdateGridCoord = false;
    [SerializeField] private Vector2[] m_CacheGridCoords = new Vector2[3*3];
    public Vector2[] GetCacheGridCoords
    {  
        get { return m_CacheGridCoords; } 
    }

    public float GetGridHalfSize
    {
        get { return (m_Width / 3.0f) * 0.5f; }
    }

    [SerializeField] private int m_GridCellState = 0;


    [Space]
    [Header("Helpers")]
    [SerializeField] private float m_CacheWidth = 0.0f;
    [SerializeField] private Vector3 m_CacheBoardLocation = Vector2.zero;

    [Space]
    [Header("Debugging")]
    [SerializeField] private bool m_DebugShow = true;
    [SerializeField] private Color m_GridAvailableColour = Color.green;
    [SerializeField] private Color m_GridNotAvailableColour = Color.red;


    public void ResetBoard()
    {
        m_GridCellState = 0;
    }


    public bool GetGridCellFlagState(int idx)
    {
        int cell_flag = (int)Mathf.Pow(2, 8 - idx);
        return ((cell_flag &= m_GridCellState) != 0);
    }

    public void RemoveCellFlagStateByIdx(int flag_idx)
    {
        int flag_mask = (int)Mathf.Pow(2, 8 - flag_idx);
        if((flag_mask & m_GridCellState) == flag_mask)
        {
            Debug.Log("Flag was set for" + flag_mask + "idx: " + flag_idx + " removing.");
            m_GridCellState ^= flag_mask;
        }
        else
            Debug.Log("Flag was not set for" + flag_mask + "idx: " + flag_idx + " unsucessful.");
    }

    public void RemoveCellFlagStateWithBinaryMask(int flag_mask)
    {
        if ((flag_mask & m_GridCellState) == flag_mask)
        {
            Debug.Log("Flag was set for" + flag_mask + " removing.");
            m_GridCellState ^= flag_mask;
        }
        else
            Debug.Log("Flag was not set for" + flag_mask + " unsucessful.");
    }


    public bool UseGridCoord(int idx)
    {
        if(idx > 9)
        {
            Debug.Log("Out of bound!!!!!");
            return false;
        }

        //check current flag
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

        int cell_flag = (int)Mathf.Pow(2, 8 - idx);
        int curr_state = m_GridCellState & cell_flag;
        if (curr_state == cell_flag)
        {
            Debug.Log("Unable to use grid already in use");
            return false;
        }
        m_GridCellState += cell_flag;

        return true;
    }


    public bool GetGridCoord(int idx, ref Vector2 grid_point)
    {
        if (idx > 9)
        {
            Debug.Log("Out of bound!!!!!");
            return false;
        }

        grid_point = m_CacheGridCoords[idx];
        return true;
    }
    public Vector2 GetGridCoord(int idx)
    {
        Vector2 out_coord = Vector2.zero; 
        GetGridCoord(idx, ref out_coord);
        return out_coord;
    }

    private bool CheckCacheGridUpdateRequired()
    {
        bool update = false;
        update |= m_Width != m_CacheWidth;
        update |= m_CacheBoardLocation != transform.position;
        if(update)
        {
            m_CacheWidth = m_Width;
            m_CacheBoardLocation = transform.position;
        }
        return update;
    }


    private void WriteDebugText()
    {
        float per_grid_size = m_Width / 3.0f;
        Vector2 board_center = transform.position;
        Vector2 top_left_corner = board_center + new Vector2(-m_Width * 0.5f, m_Width * 0.5f);

        Vector2 first_row_column_center = top_left_corner + new Vector2(per_grid_size * 0.5f, -per_grid_size * 0.5f);
        int array_pointer = 0;
        for(int i = 0; i < 3; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                Vector2 pos = first_row_column_center + new Vector2(j * per_grid_size, -i * per_grid_size);
                m_CacheGridCoords[array_pointer++] = pos;
            }
        }

        m_UpdateGridCoord = false;
    }


    private void OnDrawGizmos()
    {
        if (!m_DebugShow)
            return;

        //row,column 0,0 is the offset of -width/2, +width/2 from board world pos
        Vector2 center = transform.position;
        Vector2 row_column = center + new Vector2(-m_Width*0.5f, m_Width*0.5f);

        Gizmos.color = Color.yellow;
        //draw line from top left to top rigth
        Gizmos.DrawLine(row_column, row_column + new Vector2(m_Width, 0.0f));
        Gizmos.DrawSphere(center, 0.2f);

        //complete board
        Vector2 bottom_left = row_column + new Vector2(0.0f, -m_Width);
        Vector2 top_right = row_column + new Vector2(m_Width, 0.0f);
        Vector2 bottom_right = row_column + new Vector2(m_Width, 0.0f) + new Vector2(0.0f, -m_Width);
        Gizmos.DrawLine(row_column, bottom_left);
        Gizmos.DrawLine(top_right, bottom_right);
        Gizmos.DrawLine(bottom_left, bottom_right);


        //demacation
        //start --> row_colum 
        //3 x 3 board
        // each grid size = m_width / 3
        //line across boundard already draw, i.e start at idx 1
        float per_grid_size = m_Width / 3.0f;
        for(int i = 1; i < 3; i++)
        {
            //vertical 
            Vector2 a = row_column + new Vector2(per_grid_size * i, 0.0f);
            Vector2 b = a + new Vector2(0.0f, -m_Width);
            Gizmos.DrawLine(a, b);

            //horizontal 
            a = row_column - new Vector2(0.0f, per_grid_size * i);
            b = a + new Vector2(m_Width, 0.0f);
            Gizmos.DrawLine(a, b);
        }



        //Cache grids points 
        m_UpdateGridCoord = CheckCacheGridUpdateRequired();
        if (m_UpdateGridCoord)
            WriteDebugText();

        foreach (Vector2 coord in m_CacheGridCoords)
        {
            Gizmos.DrawSphere(coord, 0.2f);
        }

        for(int i = 0; i < 9; i++)
        {
            Gizmos.color = GetGridCellFlagState(i) ? m_GridNotAvailableColour : m_GridAvailableColour;
            Gizmos.DrawSphere(m_CacheGridCoords[i], 0.2f);
        }

    }
}
