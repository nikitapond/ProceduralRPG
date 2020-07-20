using UnityEngine;
using UnityEditor;
/// <summary>
/// Holds onto various basic data for an NPC
/// </summary>
/// 
[System.Serializable]
public class BasicNPCData
{

    public NPCJob NPCJob { get; private set; }
    public bool HasJob { get { return NPCJob != null; } }

    public NPCGender Gender { get; private set; }

   

    public void SetJob(NPCJob job)
    {
        NPCJob = job;
        
    }

    public Building House { get; private set; }
    public bool HasHouse { get { return House != null; } }
    public void SetHome(House house)
    {
        House = house;
    }

    //Debug, TODO add check for if npc should go to work.
    public bool ShouldGoToWork { get { return NPCJob != null; } }

    public void SetGender(NPCGender gender)
    {
        Gender = gender;
    }

    public override string ToString()
    {
        string data = "";
        data += "HasJob: " + HasJob + ((HasJob)? ("-" + NPCJob.ToString()) : "") + "\n";
        data += "Gender: " + Gender;
        return data;
    }
}

public enum NPCGender
{
    male, female
}