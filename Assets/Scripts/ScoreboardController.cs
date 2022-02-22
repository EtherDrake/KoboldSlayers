using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class MyIntEvent : UnityEvent<int>
{
}

public class ScoreboardController : MonoBehaviour
{
    public static MyIntEvent ScoreUpdate;
    public static MyIntEvent TurnUpdate;

    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI TurnText;

    public static int Score = 0;
    public static int Turn = 0;    

    void OnEnable()
    {
        if (ScoreUpdate == null)
        {
            ScoreUpdate = new MyIntEvent();
        }
        if (TurnUpdate == null)
        {
            TurnUpdate = new MyIntEvent();
        }

        ScoreUpdate.AddListener(updateScore);
        TurnUpdate.AddListener(updateTurn);        
    }

    void OnDisable()
    {
        ScoreUpdate.RemoveListener(updateScore);
        TurnUpdate.RemoveListener(updateTurn); 
    }

    void updateScore(int value)
    {
        Score += value;
        ScoreText.text = Score.ToString();
    }

    void updateTurn(int value)
    {
        Turn = value;
        TurnText.text = Turn.ToString();
    }
}
