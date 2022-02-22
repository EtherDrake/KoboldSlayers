using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    public string PropertyName;
    private Slider _slider;

    // Start is called before the first frame update
    void Start()
    {
        _slider = GetComponent<Slider>();
        _slider.value = PlayerPrefs.GetFloat(PropertyName, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLevel (float sliderValue)
    {
        PlayerPrefs.SetFloat(PropertyName, sliderValue);
    }
}
