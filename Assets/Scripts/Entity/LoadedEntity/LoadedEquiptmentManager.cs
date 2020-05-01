using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
public enum LoadedEquiptmentPlacement
{
    head, chest, legs, feet, hands, weaponHand, offHand, weaponSheath, backSheath
}
public class LoadedEquiptmentManager : MonoBehaviour
{

    
    //references to the bone transforms for each equiptment placement
    public GameObject HEAD, CHEST, LEGS, FOOT_L, FOOT_R, HAND_L, HAND_L_END, HAND_R, HAND_R_END;
    public GameObject WEAPONSHEATH, WEAPONSHEATH_END, BACKSHEATH, BACKSHEATH_END;
    private Dictionary<LoadedEquiptmentPlacement, GameObject> EquiptObjects;

    public SkinnedMeshRenderer SMR;
    private LoadedEntity LoadedEntity;
    private void OnEnable()
    {
       // SMR = GetComponentInChildren<SkinnedMeshRenderer>();
        LoadedEntity = GetComponent<LoadedEntity>();
        EquiptObjects = new Dictionary<LoadedEquiptmentPlacement, GameObject>();
    }

    private void Start()
    {
        (LoadedEntity.Entity as HumanoidEntity).EquiptmentManager.SetLoadedEquiptmentManager(this);

        HAND_R.GetComponent<LoadedMeleeWeapon>().SetWeaponDetails(LoadedEntity.Entity, null);
        LoadedEntity.Entity.CombatManager.SetLoadedMeleeWeapon(HAND_R.GetComponent<LoadedMeleeWeapon>());
        EquiptStartItems((LoadedEntity.Entity as HumanoidEntity).EquiptmentManager);
    }
    /// <summary>
    /// Equipts all items that are currently equipt
    /// </summary>
    /// <param name="eqMan"></param>
    private void EquiptStartItems(EquiptmentManager eqMan)
    {
        //Iterate all possible slots
        foreach(LoadedEquiptmentPlacement lep in MiscUtils.GetValues<LoadedEquiptmentPlacement>())
        {
            EquiptableItem eqIt = eqMan.GetEquiptItem(lep);
            if (eqIt != null)
            {
                SetEquiptmentItem(lep, eqIt);
            }
            else
            {
                eqIt = eqMan.GetDefaultItem(lep);
                if (eqIt != null)
                    SetEquiptmentItem(lep, eqIt);
            }
        }
    }

    public void SetEquiptmentItem(LoadedEquiptmentPlacement slot, Item item)
    {
        Debug.Log("[LoadedEquiptmentManager] Adding item " + item + " to slot " + slot);
        
        //Check if an item exists in this slot, if so we destroy it
        GameObject remove;
        EquiptObjects.TryGetValue(slot, out remove);
        if(remove != null)
        {
            DestroyImmediate(remove);
            EquiptObjects.Remove(slot);
            //If the equipt slot required a blendShape, we reset it now
            if(slot == LoadedEquiptmentPlacement.legs)
            {
                SMR.SetBlendShapeWeight(0, 0);                    
            }
            else if (slot == LoadedEquiptmentPlacement.chest)
            {
                SMR.SetBlendShapeWeight(1, 0);
            }

        }
        //If the item is null, we don't need to add an object
        if (item == null)
        {
            //But if the slot is the hand, we need to activate the unarmed melee attack
            if(slot == LoadedEquiptmentPlacement.weaponHand)
            {
                LoadedEntity.Entity.CombatManager.SetLoadedMeleeWeapon(HAND_R.GetComponent<LoadedMeleeWeapon>());
                //Ensure it is enabled so we can deal unarmed attacks
            }else if(slot == LoadedEquiptmentPlacement.legs) {
                SMR.SetBlendShapeWeight(0, 0);
            }
            else if (slot == LoadedEquiptmentPlacement.chest)
            {
                SMR.SetBlendShapeWeight(1, 0);
            }



            return;
        }
            
        //We create the object
        GameObject obj = Instantiate((item as EquiptableItem).GetEquiptItem());
        //Non weapons will have a 
        LoadedEquiptment le = obj.GetComponent<LoadedEquiptment>();

        switch (slot)
        {
            case LoadedEquiptmentPlacement.weaponSheath:
                obj.transform.parent = WEAPONSHEATH.transform;
                break;
            case LoadedEquiptmentPlacement.weaponHand:
                obj.transform.parent = HAND_R.transform;
                break;
            case LoadedEquiptmentPlacement.offHand:
                obj.transform.parent = HAND_L.transform;
                break;
            case LoadedEquiptmentPlacement.legs:
                
                le.transform.parent = SMR.transform;
                le.SMR.bones = SMR.bones;
                le.SMR.rootBone = SMR.rootBone;
                Debug.Log("[LoadedEquiptmentManager] Equipting to legs - setting blend shape to 100");
                SMR.SetBlendShapeWeight(0, 100);
                break;
            case LoadedEquiptmentPlacement.chest:
                le.transform.parent = SMR.transform;
                le.SMR.bones = SMR.bones;
                le.SMR.rootBone = SMR.rootBone;
                Debug.Log("[LoadedEquiptmentManager] Equipting to chest - setting blend shape to 100");
                SMR.SetBlendShapeWeight(1, 100);
                break;

        }

        if (item.HasMetaData)
        {
            Color c = item.MetaData.Color;
            if (c != null)
            {
                if(le != null)
                {
                    le.SMR.material.SetColor("MainColour", c);
                }else if (obj.GetComponent<MeshRenderer>() != null)
                    obj.GetComponent<MeshRenderer>().material.SetColor("MainColour", c);
                else if(obj.GetComponent<SkinnedMeshRenderer>() != null)
                    obj.GetComponent<SkinnedMeshRenderer>().material.SetColor("MainColour", c);
                else if (obj.GetComponentInChildren<MeshRenderer>() != null)
                    obj.GetComponentInChildren<MeshRenderer>().material.SetColor("MainColour", c);
                else if (obj.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    obj.GetComponentInChildren<SkinnedMeshRenderer>().material.SetColor("MainColour", c);
            }
        }


        if (item is Weapon && !(item is RangeWeapon))
        {
            LoadedMeleeWeapon lmw = obj.GetComponent<LoadedMeleeWeapon>();
            if (lmw == null) lmw = obj.GetComponentInChildren<LoadedMeleeWeapon>();
            lmw.SetWeaponDetails(LoadedEntity.Entity, item as Weapon);
            LoadedEntity.Entity.CombatManager.SetLoadedMeleeWeapon(lmw);
        }

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        if (EquiptObjects.ContainsKey(slot))
            EquiptObjects[slot] = obj;
        else
            EquiptObjects.Add(slot, obj);
    }


    public void UnsheathWeapon(LoadedEquiptmentPlacement slot)
    {
        GameObject sheathed = GetObjectInSlot(slot);
        LoadedEntity.AnimationManager.HumanoidCast().DrawFromSheath(HAND_R, sheathed);
        StartCoroutine(WaitToUnsheath(slot));

    }

    private IEnumerator WaitToUnsheath(LoadedEquiptmentPlacement slot)
    {
        yield return new WaitForSeconds(LoadedHumanoidAnimatorManager.GRAB_SHEATHED_WEAPON_ANI_TIME);
        EquiptObjects[LoadedEquiptmentPlacement.weaponHand] = EquiptObjects[slot];
        EquiptObjects[slot] = null;
    }

    public GameObject GetObjectInSlot(LoadedEquiptmentPlacement place)
    {
        if (EquiptObjects.ContainsKey(place))
            return EquiptObjects[place];
        return null;
    }
    public void AddObjectInSlot(LoadedEquiptmentPlacement place, GameObject obj)
    {
        if (EquiptObjects.ContainsKey(place))
            EquiptObjects[place] = obj;
        else
        {
            EquiptObjects.Add(place, obj);
        }
    }
    public void SwapObjectSlots(LoadedEquiptmentPlacement a, LoadedEquiptmentPlacement b)
    {
        GameObject A = GetObjectInSlot(a);
        GameObject B = GetObjectInSlot(b);
        AddObjectInSlot(a, B);
        AddObjectInSlot(b, A);
    }


    private void Update()
    {
        GameObject weaponObj;
        if(EquiptObjects.TryGetValue(LoadedEquiptmentPlacement.weaponHand, out weaponObj))
        {

            weaponObj.transform.rotation = GetBoneRotation(HAND_R, HAND_R_END);
        }
        if(EquiptObjects.TryGetValue(LoadedEquiptmentPlacement.offHand, out weaponObj))
        {
            weaponObj.transform.rotation = GetBoneRotation(HAND_L, HAND_L_END);

        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(HAND_R.transform.position, HAND_R_END.transform.position);
    }








    /// <summary>
    /// Returns the Quaternion rotation that defines the bone
    /// rotation starting at t1 and ending at t2
    /// </summary>
    /// <param name="t1"></param>
    /// <param name="t2"></param>
    /// <returns></returns>
    public static Quaternion GetBoneRotation(GameObject t1, GameObject t2)
    {
        Vector3 dif = (t2.transform.position - t1.transform.position).normalized;

        return Quaternion.FromToRotation(Vector3.up, dif);
        Debug.Log(t2.transform.localPosition + "_" + t1.transform.localPosition + "_" + dif);
        return Quaternion.Euler(dif);
        return Quaternion.LookRotation(t2.transform.localPosition.normalized, Vector3.up);

    }

}