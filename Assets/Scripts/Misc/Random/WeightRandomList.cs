using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A weighted random list allows us to add elements to a list with a 'weight' 
/// We can then ramdomly select an element from this list, with each elements weight value 
/// being directly proportional to the probability of it being picked
/// </summary>
public class WeightRandomList<T>
{

    struct StartEnd
    {
        public float start;
        public float end;
        public StartEnd(float s, float e)
        {
            start = s;
            end = e;
        }
    }

    private List<StartEnd> ElementStartEnd;
    private List<T> Elements;
    private float Accumulated;
    private System.Random Random;
    public WeightRandomList(int seed=-1){
        ElementStartEnd = new List<StartEnd>();
        
        Elements = new List<T>();
        if(seed == -1)
        {
            seed = System.DateTime.Now.Millisecond;
        }
        Random = new System.Random(seed);
    }


    public void AddElement(T item, float weight)
    {
        //The start value of element i = end value of element 'i-1'
        float start = ElementStartEnd.Count == 0 ? 0 : ElementStartEnd[ElementStartEnd.Count - 1].end;
        //We define the elements start and end based on the previous items end, and this items start
        ElementStartEnd.Add(new StartEnd(start, start + weight));
        Elements.Add(item);
        Accumulated += weight;
    }

    public int Count { get { return Elements.Count; } }

    public T GetRandom(bool remove=false) {

        int elementIndex = 0;
        float val = (float)(Random.NextDouble() * Accumulated);
        foreach (StartEnd startEnd in ElementStartEnd)
        {
            if (val < startEnd.end)
                break;
            elementIndex++;
        }

        T item = Elements[elementIndex];


        if (remove)
        {
            float elementWeight = ElementStartEnd[elementIndex].end - ElementStartEnd[elementIndex].start;
            for(int i=elementIndex; i<ElementStartEnd.Count; i++)
            {
                //Modify each
                ElementStartEnd[i] = new StartEnd (ElementStartEnd[i].start - elementWeight, ElementStartEnd[i].end - elementWeight);
            }
            Accumulated -= elementWeight;
            Elements.RemoveAt(elementIndex);
            ElementStartEnd.RemoveAt(elementIndex);
        }

        return item;



    }
    /// <summary>
    /// Resets by clearing all elements
    /// </summary>
    public void Clear()
    {
        Accumulated = 0;
        Elements.Clear();
        ElementStartEnd.Clear();
    }

}