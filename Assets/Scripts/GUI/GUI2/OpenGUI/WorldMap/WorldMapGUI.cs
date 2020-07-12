using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class WorldMapGUI : MonoBehaviour
{
    public GameObject WorldMapLocButtonPrefab;


    public RawImage WorldMapImage;

    private WorldMap WorldMap;

    private float Zoom = 1;

    private void OnEnable()
    {
        if (WorldMap == null)
        {
            WorldMap = GameManager.WorldManager.World.WorldMap;
            WorldMapImage.texture = WorldMap.WorldMapBase;



            WorldMapLocation activeLoc = GetActiveQuestWML();
            foreach (WorldMapLocation wml in WorldMap.WorldMapLocations)
            {
                if (wml == null)
                    continue;
                GameObject butObj = Instantiate(WorldMapLocButtonPrefab);
                butObj.transform.SetParent(WorldMapImage.transform,false);
                butObj.transform.localPosition = wml.ChunkPosition.AsVector2()*WorldMap.Scale;
                butObj.GetComponent<WorldMapLocationButton>().SetLocation(this, wml);
                if(activeLoc == wml)
                {
                    butObj.GetComponent<WorldMapLocationButton>().SetActiveQuestButton(true);
                    
                }
                else
                {
                    butObj.GetComponent<WorldMapLocationButton>().SetActiveQuestButton(false);

                }
            }
        }
        else
        {
            WorldMapLocation activeLoc = GetActiveQuestWML();

            //Iterate all world map locations
            foreach (Transform t in WorldMapImage.transform)
            {
                WorldMapLocationButton but = t.gameObject.GetComponent<WorldMapLocationButton>();
                if(but.WorldMapLocation == activeLoc)
                {
                    but.SetActiveQuestButton(true);

                }
                else
                {
                    but.SetActiveQuestButton(false);

                }
            }
        }
    }


    /// <summary>
    /// Finds the world map location associated with the currently active quest, if there is one
    /// </summary>
    /// <returns></returns>
    private WorldMapLocation GetActiveQuestWML()
    {
        //If there is no active quest, then no need for check
        if (GameManager.QuestManager.ActiveQuest == null)
            return null;

        Quest q = GameManager.QuestManager.ActiveQuest;
        if (q.CurrentTask() == null)
            return null;
        return q.CurrentTask().GetAssociatedLocation();
    }

    public void TeleportToLocation(WorldMapLocation wml)
    {
        if (GameManager.WorldManager.InSubworld)
            return;
        GameManager.PlayerManager.Player.MoveEntity(wml.WorldPosition);
    }


    private void Update()
    {
        if(Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            Zoom = Mathf.Clamp(Zoom + Input.GetAxis("Mouse ScrollWheel"), 1, 5);
            WorldMapImage.transform.localScale = new Vector3(Zoom, Zoom,1);
        }
    }
}
