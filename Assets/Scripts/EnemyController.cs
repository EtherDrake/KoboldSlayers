using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

public abstract class EnemyController : BaseCharacterController
{
    public GameObject MissMessage;
    public int ScoreCost = 500;
    public int Morale;

    // Start is called before the first frame update
    protected override void Start()
    {      
        Animator = GetComponent<Animator>();
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override IEnumerator Skill1(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        yield return new WaitForSeconds(0.40f);
    }

    public override IEnumerator Skill2(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        yield return new WaitForSeconds(0.40f);
    }

    public override IEnumerator Skill3(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        yield return new WaitForSeconds(0.40f);
    }

    public override IEnumerator Skill4(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        yield return new WaitForSeconds(0.40f);
    }

    public abstract Task<AiDecision> TakeTurn(Tilemap tilemap, List<Vector3Int> availablePositions, List<BaseCharacterController> enemies, Astar astar, Vector3Int[,] gridValues, int[,] walkableValues);   


    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }    

    public override void GetHit(BaseCharacterController attacker, int damage)
    {
        base.GetHit(attacker, damage);
        if(!IsAlive)
        {            
            ScoreboardController.ScoreUpdate.Invoke(ScoreCost);
            ScoreCost = 0;
        }
    }
}
