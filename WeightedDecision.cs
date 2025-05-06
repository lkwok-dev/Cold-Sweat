using UnityEngine;

// Decision based on weight. The index of the decisions array is the dicision, and the integers within each index is the weight.
[System.Serializable]
public class WeightedDecision : ScriptableObject
{
    [System.Serializable]
    public class StateInt
    {
        public int value;

        public int GetValue()
        {
            return value;
        }
        public void SetValue(int i)
        {
            value = i;
        }
    }

    [HideInInspector] public int[] decisions;

    public WeightedDecision(int[] d)
    {
        decisions = d;
    }
    public void AddDicision(WeightedDecision d)
    {
        if (this.GetType() == d.GetType())
        {
            for (int i = 0; i < decisions.Length; i++)
            {
                this.decisions[i] += d.decisions[i];
            }
        }
        else
        {
            Debug.LogError("Not the right class when adding decisions");
        }

    }

    public int AddAllDicision()
    {
        int total = 0;
        for (int i = 0; i < decisions.Length; i++)
        {
            total += decisions[i];
        }
        return total;
    }

    public int GetRandomIndex()
    {
        int temp = Random.Range(0, AddAllDicision());

        for (int i = 0; i < decisions.Length; i++)
        {
            //Check if the value is negitive. If it is, then set the weight to 0.
            if (decisions[i] <= 0) continue;
            if (temp - decisions[i] < 0)
            {
                return i;
            }
            else
            {
                temp -= decisions[i];
            }
        }
        return 0;
    }
}
