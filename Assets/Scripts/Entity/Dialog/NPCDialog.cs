using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public class NPCDialog
{


    public string DialogOpenText { get; private set; }

    public NPCDialogNode CurrentNode { get; private set; }

    public List<NPCDialogNode> Nodes { get; private set; }


    /// <summary>
    /// Creates the dialog object for an NPC
    /// npc- the NPC to which the dialog belongs
    /// npcOpenText - the text initially displayed to the player 
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="npcOpenText"></param>
    public NPCDialog(NPC npc, string npcOpenText)
    {
        DialogOpenText = npcOpenText;
        Nodes = new List<NPCDialogNode>();
    }

    public void RemoveNode(NPCDialogNode node)
    {
        Nodes.Remove(node);
    }

    public void StartDialog()
    {
        CurrentNode = null;
    }


    public void AddNode(NPCDialogNode node)
    {
        Nodes.Add(node);
        node.SetParent(this);
    }

    public void SetCurrentNode(NPCDialogNode node)
    {
        CurrentNode = node;
    }


    


}