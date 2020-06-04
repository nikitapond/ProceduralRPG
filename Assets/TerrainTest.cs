using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTest : MonoBehaviour
{
    public bool Generate = false;
    public Texture2D Texture;
    // Start is called before the first frame update
    void Start()
    {
        CalculateTerrain();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnGUI()
    {
        if (Texture != null)
        {
            int scale = 1;

            GUI.DrawTexture(new Rect(0, 0, Texture.width * scale, Texture.height * scale), Texture);
            //GUI.DrawTexture(new Rect(0, 0, Texture.width * scale, Texture.height * scale), gradient);

        }
    }

    Texture2D gradient;
    Texture2D gradient2;

    TerrainGenerator2 t2;
    public void CalculateTerrain()
    {
        

    }

    private void OnDrawGizmos()
    {
     
    }

}
