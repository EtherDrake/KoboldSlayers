using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipSecondaryTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    public string Title;
    public string Text;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipSystem.Show(Title, Text);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipSystem.Hide();
    }
}
