using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    public int abilityNumber;

    public void OnPointerEnter(PointerEventData eventData)
    {
        AbilityProperties properties;
        switch(abilityNumber)
        {
            case 1: properties = TurnBasedController.abilityProperties1; break;
            case 2: properties = TurnBasedController.abilityProperties2; break;
            case 3: properties = TurnBasedController.abilityProperties3; break;
            default: properties = TurnBasedController.abilityProperties4; break;
        }

        if(properties != null && !string.IsNullOrEmpty(properties.Name))
        {
            TooltipSystem.Show(properties.Name, properties.Description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipSystem.Hide();
    }
}
