using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tooltip : MonoBehaviour
{
    public TextMeshProUGUI Header;
    public TextMeshProUGUI Body;
    public LayoutElement LayoutElement;
    public RectTransform rectTransform;
    public ContentSizeFitter ContentSizeFitter;

    public void SetText(string header, string content)
    {
        if(string.IsNullOrEmpty(header))
        {
            Header.gameObject.SetActive(false);
        }
        else
        {            
            Header.gameObject.SetActive(true);
            Header.text = header;
        }

        Body.text = content;

        int headerLength = Header.text.Length;
        int bodyLength = Body.text.Length;
        LayoutElement.enabled = (headerLength > 20 || bodyLength > 20);
        if(!LayoutElement.enabled)
        {
            ContentSizeFitter.enabled = false;
            rectTransform.sizeDelta = new Vector2(50, 10);;
        }
        else
        {            
            ContentSizeFitter.enabled = true;
        }
    }

    public void Update()
    {
        Vector2 position = Input.mousePosition;
        position.y += 15f;

        float pivotX = position.x / Screen.width;
        float pivotY = position.y / Screen.height;

        rectTransform.pivot = new Vector2(pivotX, pivotY );
        transform.position = position;
    }
}
