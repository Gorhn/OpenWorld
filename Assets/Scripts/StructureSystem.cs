using Mirror;
using UnityEngine;

public class StructureSystem : NetworkBehaviour {

    [SyncVar]
    private int maxStructureHealthPoints;
    [SyncVar]
    private int currentStructureHealthPoints;

    public delegate void StructureDestroyed();
    public event StructureDestroyed StructureDestroyedEvent;

    public delegate void StructureRepaired();
    public event StructureRepaired StructureRepairedEvent;

    [Server]
    public void DamageStructure(int damages) {
        if (damages < 0) {
            Debug.LogError("Structure can't be damaged by a negative value. For repairs, use RepairStructure(int repairs) instead.", this);
            return;
        }

        bool standing = currentStructureHealthPoints > 0;        
        currentStructureHealthPoints = Mathf.Max((currentStructureHealthPoints - damages), 0);

        if (standing && currentStructureHealthPoints == 0) {
            StructureDestroyedEvent();
        }
    }

    [Server]
    public void RepairStructure(int repairs) {
        if (repairs < 0) {
            Debug.LogError("Structure can't be repaired by a negative value. For damages, use DamageStructure(int damages) instead.", this);
            return;
        }

        bool repaired = currentStructureHealthPoints == 0 && repairs > 0;
        currentStructureHealthPoints = Mathf.Min((currentStructureHealthPoints + repairs), maxStructureHealthPoints);

        if (repaired) {
            StructureRepairedEvent();
        }
    }

}
