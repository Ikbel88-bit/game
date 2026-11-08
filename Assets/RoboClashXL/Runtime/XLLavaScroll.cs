using UnityEngine;

public class XLLavaScroll : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.012f, 0.007f);
    Material m;
    Vector2 o;

    void Awake() { m = GetComponent<Renderer>().material; }

    void Update()
    {
        o += speed * Time.deltaTime;
        m.SetTextureOffset("_BaseMap", o);
    }
}
