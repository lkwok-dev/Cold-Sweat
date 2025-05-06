using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Decision/Attack Dicision")]
public class AttackDecision : WeightedDecision
{
    public int iceAttack;
    public int fireAttack;

    private void OnValidate()
    {
        decisions = new int[] { iceAttack, fireAttack };
    }

    public AttackDecision(): base (new int[2]) {}

    public AttackDecision(AttackDecision aD) : base(new int[2])
    {
        for (int i = 0; i < decisions.Length; i++)
        {
            decisions[i] = aD.decisions[i];
        }
    }

    public AttackDecision(int iA, int fA) : base(new int[2])
    {
        decisions = new int[] { iA, fA };
    }


    public bool GiveTheNextRandomDicision() //Return true if the output is ice, and false if it's fire.
    {
        return GetRandomIndex() == 0;
    }

    public void DisplayLog()
    {
        Debug.Log("Ice Attack: " + iceAttack + "Fire Attack: " + fireAttack);
    }
}
