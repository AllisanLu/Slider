using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class UIArtifact3D : MonoBehaviour
{
    // public Vector3 tempPosition = new Vector3(0,0,0);
    public ArtifactTileButton3D[] buttons;
    //L: The button the user has clicked on
    protected ArtifactTileButton3D currentButton;
    //L: The available buttons the player has to move to from currentButton
    protected List<ArtifactTileButton3D> moveOptionButtons = new List<ArtifactTileButton3D>();

    // DC: Current list of moves being performed 
    protected List<SMove3D> activeMoves = new List<SMove3D>();
    //L: Queue of moves to perform on the grid from the artifact
    //L: IMPORTANT NOTE: The top element in the queue is always the current move being executed.
    protected Queue<SMove3D> moveQueue = new Queue<SMove3D>();
    public int maxMoveQueueSize = 3;    //L: Max size of the queue.

    private static UIArtifact3D _instance;
    
    public void Awake()
    {
        _instance = this;
        activeMoves = new List<SMove3D>();
        moveQueue = new Queue<SMove3D>();
    }

    public void Start()
    {
        SGridAnimator3D.OnSTileMove += QueueCheckAfterMove;
    }

    public static UIArtifact3D GetInstance()
    {
        return _instance;
    }

    //L: Handles when the user attempts to drag and drop a button
    //Plz dont touch it will break
    public virtual void ButtonDragged(BaseEventData eventData) { 
        // Debug.Log("dragging");
        PointerEventData data = (PointerEventData) eventData;

        if (currentButton != null) 
        {
            return;
        }

        ArtifactTileButton3D dragged = data.pointerDrag.GetComponent<ArtifactTileButton3D>();
        if (!dragged.isTileActive || dragged.isForcedDown)
        {
            return;
        }

        ArtifactTileButton3D hovered = null;
        if (data.pointerEnter != null && data.pointerEnter.name == "Image") 
        {
            hovered = data.pointerEnter.transform.parent.gameObject.GetComponent<ArtifactTileButton3D>();
        }

        
        foreach (ArtifactTileButton3D b in GetMoveOptions(dragged)) {
            if (b == hovered) 
            {
                b.SetHighlighted(false);
                b.buttonAnimator.sliderImage.sprite = b.hoverSprite; // = blankSprite
            }
            else 
            {
                b.SetHighlighted(true);
                b.ResetToIslandSprite();
            }
        }
    }
    //Plz dont touch it will break
    public virtual void ButtonDragEnd(BaseEventData eventData) {
        PointerEventData data = (PointerEventData) eventData;


        //Debug.Log("Sent drag end");
        if (currentButton != null) 
        {
            foreach (ArtifactTileButton3D b in GetMoveOptions(currentButton)) 
            {
                b.buttonAnimator.sliderImage.sprite = b.emptySprite;
            }
            return;
        }

        ArtifactTileButton3D dragged = data.pointerDrag.GetComponent<ArtifactTileButton3D>();
        if (!dragged.isTileActive || dragged.isForcedDown)
        {
            return;
        }
        List<ArtifactTileButton3D> moveOptions = GetMoveOptions(dragged);
        foreach (ArtifactTileButton3D b in moveOptions) {
            b.buttonAnimator.sliderImage.sprite = b.emptySprite;
        }
        ArtifactTileButton3D hovered = null;
        if (data.pointerEnter != null && data.pointerEnter.name == "Image") 
        {
            hovered = data.pointerEnter.transform.parent.gameObject.GetComponent<ArtifactTileButton3D>();
        }
        else 
        {
            SelectButton(dragged);
            return;
        }
        
        if (!hovered.isTileActive)
        {
            hovered.buttonAnimator.sliderImage.sprite = hovered.emptySprite;
        }
        //Debug.Log("dragged" + dragged.islandId + "hovered" + hovered.islandId);
        
        bool swapped = false;
        foreach (ArtifactTileButton3D b in moveOptions) {
            b.SetHighlighted(false);
            // b.buttonAnimator.sliderImage.sprite = b.emptySprite;
            if(b == hovered && !swapped) 
            {
                CheckAndSwap(dragged, hovered);
                SGridAnimator3D.OnSTileMove += dragged.AfterStileMoveDragged;
                swapped = true;
            }
        }
        if (!swapped) {
            SelectButton(dragged);
        }
        // dragged.SetPushedDown(false);
    }


    public void OnDisable()
    {
        moveQueue = new Queue<SMove3D>();
        //Debug.Log("Queue Cleared!");
    }

    public void DeselectCurrentButton()
    {
        if (currentButton == null)
            return;

        currentButton.SetSelected(false);
        foreach (ArtifactTileButton3D b in moveOptionButtons)
        {
            b.SetHighlighted(false);
        }
        currentButton = null;
        moveOptionButtons.Clear();
    }
    
    public virtual void SelectButton(ArtifactTileButton3D button)
    {
        // Check if on movement cooldown
        //if (SGrid.GetStile(button.islandId).isMoving)

        //L: This is basically just a bunch of nested logic to determine how to update the UI based on what button the user pressed.

        ArtifactTileButton3D oldCurrButton = currentButton;
        if (currentButton != null)
        {
            if (moveOptionButtons.Contains(button))
            {

                //L: Player makes a move while the tile is still moving, so add the button to the queue.
                CheckAndSwap(currentButton, button);

                moveOptionButtons = GetMoveOptions(currentButton);
                foreach (ArtifactTileButton3D b in buttons)
                {
                    b.SetHighlighted(moveOptionButtons.Contains(b));
                }
            } else 
            {
                DeselectCurrentButton();
            } 
        }

        if (currentButton == null)
        {
            //DeselectCurrentButton(); //L: I don't think this is necessary since currentButton is null and it will just do nothing

            if (!button.isTileActive || oldCurrButton == button)
            {
                //L: Player tried to click an empty tile
                return;
            }

            moveOptionButtons = GetMoveOptions(button);
            if (moveOptionButtons.Count == 0)
            {
                //L: Player tried to click a locked tile (or tile that otherwise had no move options)
                return;
            }
            else
            {
                //L: Player clicked a tile with movement options
                //Debug.Log("Selected button " + button.islandId);
                currentButton = button;
                button.SetSelected(true);
                foreach (ArtifactTileButton3D b in moveOptionButtons)
                {
                    b.SetHighlighted(true);
                }
            }
        }
    }

    // replaces adjacentButtons
    protected virtual List<ArtifactTileButton3D> GetMoveOptions(ArtifactTileButton3D button)
    {
        moveOptionButtons.Clear();

        //Vector2 buttPos = new Vector2(button.x, button.y);
        // foreach (ArtifactTileButton3D b in buttons)
        // {
        //     //if (!b.isTileActive && (buttPos - new Vector2(b.x, b.y)).magnitude == 1)
        //     if (!b.isTileActive && (button.x == b.x || button.y == b.y))
        //     {
        //         adjacentButtons.Add(b);
        //     }
        // }

        Vector3Int[] dirs = {
            Vector3Int.right,
            Vector3Int.up,
            Vector3Int.left,
            Vector3Int.down,
            Vector3Int.forward,
            Vector3Int.back
        };

        foreach (Vector3Int dir in dirs)
        {
            ArtifactTileButton3D b = GetButton(button.x + dir.x, button.y + dir.y, button.z + dir.z);
            int i = 1;
            while (b != null && !b.isTileActive)
            {
                moveOptionButtons.Add(b);
                b = GetButton(button.x + dir.x * i, button.y + dir.y * i, button.z + dir.z);
                i++;
            }
        }

        return moveOptionButtons;
    }

    //L: Swaps the buttons on the UI, but not the actual grid.
    protected void SwapButtons(ArtifactTileButton3D buttonCurrent, ArtifactTileButton3D buttonEmpty)
    {
        int oldCurrX = buttonCurrent.x;
        int oldCurrY = buttonCurrent.y;
        int oldCurrZ = buttonCurrent.z;
        buttonCurrent.SetPosition(buttonEmpty.x, buttonEmpty.y, buttonEmpty.z);
        buttonEmpty.SetPosition(oldCurrX, oldCurrY, oldCurrZ);
    }

    //L: updateGrid - if this is false, it will just update the UI without actually moving the tiles.
    //L: Returns if the swap was successful.
    protected virtual bool CheckAndSwap(ArtifactTileButton3D buttonCurrent, ArtifactTileButton3D buttonEmpty)
    {
        STile3D[,,] currGrid = SGrid3D.current.GetGrid();

        int x = buttonCurrent.x;
        int y = buttonCurrent.y;
        int z = buttonCurrent.z;
        SMove3D swap = new SMoveSwap3D(x, y, z, buttonEmpty.x, buttonEmpty.y, buttonEmpty.z);
 
        // Debug.Log(SGrid.current.CanMove(swap) + " " + moveQueue.Count + " " + maxMoveQueueSize);
        // Debug.Log(buttonCurrent + " " + buttonEmpty);
        if (SGrid3D.current.CanMove(swap) && moveQueue.Count < maxMoveQueueSize)
        {
            //L: Do the move

            QueueCheckAndAdd(new SMoveSwap3D(buttonCurrent.x, buttonCurrent.y, buttonCurrent.z, buttonEmpty.x, buttonEmpty.y, buttonEmpty.z));
            SwapButtons(buttonCurrent, buttonEmpty);

            // Debug.Log("Added move to queue: current length " + moveQueue.Count);
            QueueCheckAfterMove(this, null);
            // if (moveQueue.Count == 1)
            // {
            //     SGrid.current.Move(moveQueue.Peek());
            // }
            return true;
        }
        else
        {
            Debug.Log("Couldn't perform move! (queue full?)");
            return false;
        }
    }

    public void QueueCheckAndAdd(SMove3D move)
    {
        if (moveQueue.Count < maxMoveQueueSize)
        {
            moveQueue.Enqueue(move);
        } 
        else
        {
            Debug.LogWarning("Didn't add to the UIArtifact queue because it was full");
        }

    }

    /*
    public bool QueueCheckAndRemove()
    {
        if (moveQueue.Count > 0)
        {
            SMove move = moveQueue.Dequeue();
            //Debug.Log("Swapping " + currentButton.gameObject.name + " with " + emptyButton.gameObject.name);

            //L: Update the grid since the Artifact UI should have already updated.
            //L: This move should have already been checked since it was queued!
            SGrid.current.Move(move);
            return true;
        }

        return false;
    }
    */

    protected virtual void QueueCheckAfterMove(object sender, SGridAnimator3D.OnTileMoveArgs3D e)
    {
        if (e != null)
        {
            //Debug.Log("Checking for e");
            if (activeMoves.Contains(e.smove))
            {
                //Debug.Log("Move has been removed");
                activeMoves.Remove(e.smove);
            }
        }

        if (moveQueue.Count > 0)
        {
            //Debug.Log("Checking next queued move! Currently queue has " + moveQueue.Count + " moves...");

            SMove3D peekedMove = moveQueue.Peek();
            // check if the peekedMove interferes with any of current moves
            foreach (SMove3D m in activeMoves)
            {
                if (m.Overlaps(peekedMove))
                {
                    // Debug.Log("Move conflicts!");
                    return;
                }
            }

            // Debug.Log("Move doesn't conflict! Performing move.");

            // doesn't interfere! so do the move
            SGrid3D.current.Move(peekedMove);
            activeMoves.Add(moveQueue.Dequeue());
            QueueCheckAfterMove(this, null);
        }
        // if (moveQueue.Count > 0)
        // {
        //     moveQueue.Dequeue();
        // } 
        // else
        // {
        //     Debug.LogWarning("Tried to dequeue from the move queue even though there is nothing in it. This should not happen!");
        // }

        // if (moveQueue.Count > 0)
        // {
        //     SGrid.current.Move(moveQueue.Peek());
        // }
    }

    //public static void UpdatePushedDowns()
    //{
    //    foreach (ArtifactButton b in _instance.buttons)
    //    {
    //        b.UpdatePushedDown();
    //    }
    //}

    //L: Mark the button on the Artifact UI at islandID if it is in the right spot. (also changes the sprite)
    public static void SetButtonComplete(int islandId, bool value)
    {
        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.islandId == islandId)
            {
                b.SetComplete(value);
                return;
            }
        }
    }

    public static void SetButtonPos(int islandId, int x, int y, int z)
    {
        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.islandId == islandId)
            {
                b.SetPosition(x, y, z);
                return;
            }
        }
    }

    protected ArtifactTileButton3D GetButton(int x, int y, int z)
    {
        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.x == x && b.y == y && b.z == z)
            {
                return b;
            }
        }

        return null;
    }

    public ArtifactTileButton3D GetButton(int islandId){

        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.islandId == islandId)
            {
                return b;
            }
        }

        return null;
    }

    public static void AddButton(int islandId)
    {
        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.islandId == islandId)
            {
                b.SetTileActive(true);
                return;
            }
        }
    }

    public void FlickerNewTiles()
    {
        foreach (ArtifactTileButton3D b in _instance.buttons)
        {
            if (b.flickerNext)
            {
                b.Flicker();
            }
        }
    }
}
