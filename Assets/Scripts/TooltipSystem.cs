using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TooltipSystem : MonoBehaviour
{
    private static TooltipSystem current;
    public Tooltip Tooltip;

    public void Awake()
    {
        current = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void Show(string header, string body)
    {
        current.Tooltip.SetText(header, body);
        current.Tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        current.Tooltip.gameObject.SetActive(false);
    }
}
