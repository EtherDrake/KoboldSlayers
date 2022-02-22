using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

public class ZombieriderController : EnemyController
{   
    private int ChargeBonus;
    public BaseCharacterController Master;

    public AudioClip AttackSound;

    // Start is called before the first frame update
    protected override void Start()
    {      
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
        
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        return Thrust(tilemap, targetTile, targetMovePosition, targetTag);
    }   

    public override void InitAbilites()
    {
        
    }    

    public override void GetHit(BaseCharacterController attacker, int damage)
    {
        base.GetHit(attacker, damage);
        if(!IsAlive)
        {            
            Master.MaxHP += 2;
            Master.HP += 2;
        }
    }

    private IEnumerator Thrust(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag)
    { 
        var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => tilemap.WorldToCell(x.transform.position) == targetTile).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);               
        var targetPosition = targetEnemy.transform.position;
        if ((targetPosition.x < targetMovePosition.x && FacingRight) || (targetPosition.x > targetMovePosition.x && !FacingRight))
        {
            Flip();
        }

        //Rolling for an attack
        var roll = UnityEngine.Random.Range(1, 20);
        var attack = roll + Athletics + Proficiency;
        var enemy = targetEnemy.GetComponent<BaseCharacterController>();
        var hit = attack > enemy.AC || roll == 20;
        if(hit)
        {
            int dices = roll == 20 ? 2 : 1;
            var damage = Athletics;

            for(int i = 0; i < dices; i++)
            {
                var damageRoll = UnityEngine.Random.Range(1, 4);
                damage += damageRoll + ChargeBonus;
            }

            enemy.TakeDamage(damage);
            enemy.GetHit(this, damage);
        }
        else
        {
            var missMessagePosition = new Vector3(targetPosition.x + 0.275f, targetPosition.y + 0.25f, targetPosition.z);
            Instantiate(MissMessage, missMessagePosition, Quaternion.identity);
        }

        AudioSource.PlayOneShot(AttackSound);
        Animator.SetTrigger("Attack");
        yield break;
    }    

    public override async Task<AiDecision> TakeTurn(Tilemap tilemap, List<Vector3Int> availablePositions, List<BaseCharacterController> enemies, Astar astar, Vector3Int[,] gridValues, int[,] walkableValues)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        ChargeBonus = 0;

        var allyPositions = GameObject.FindGameObjectsWithTag(tag).Where(e => e.GetComponent<BaseCharacterController>().IsAlive).Select(e => tilemap.WorldToCell(e.transform.position)).ToList();
        var enemyPositions = enemies.Select(e => tilemap.WorldToCell(e.transform.position)).ToList();        
        var currentPosition =  Position.FirstOrDefault();//tilemap.WorldToCell(transform.position);

        //var unavailablePositions = availablePositions.Where(x => !(x.x < BaseSpeed && x.y < BaseSpeed) && !((x.x == currentPosition.x || x.y == currentPosition.y))).Concat(enemyPositions).ToList();
        //availablePositions = availablePositions.Where(x => x.x == currentPosition.x || x.y == currentPosition.y).ToList();

        List<PriorityItem> priorityItems = new List<PriorityItem>();

        foreach(var enemy in enemies)
        {
            PriorityItem item = new PriorityItem
            {
                Enemy = enemy,
                PriorityValue = 100
            };

            var enemyPosition = tilemap.WorldToCell(enemy.transform.position);
            var positionsNextToEnemy = availablePositions.Where(t => ((enemyPosition.x < t.x && Mathf.Abs(enemyPosition.x - t.x) == 2) || (enemyPosition.x > t.x && Mathf.Abs(enemyPosition.x - t.x) == 1)) && Mathf.Abs(enemyPosition.y - t.y) <= 1);
            if(positionsNextToEnemy.Any(x => availablePositions.Contains(x)))
            {
                item.PriorityValue += 10;
                item.IsInRange = true;
            }

            var masterPosition = tilemap.WorldToCell(Master.transform.position);
            var distanceToMaster = Mathf.Abs(masterPosition.x - enemyPosition.x) + Mathf.Abs(masterPosition.y - enemyPosition.y);
            item.PriorityValue -= distanceToMaster;

            var priorityPath = int.MinValue;
            var positionToAttack = new Vector3Int();
            var route = new List<Spot>();
            if(item.IsInRange)
            {
                if(positionsNextToEnemy.Contains(currentPosition))
                {
                    priorityPath = 0;
                }
                else
                {
                    var position = positionsNextToEnemy.FirstOrDefault();         
                    var routeToEnemy = await Task.Run(() => astar.CreatePath(gridValues, walkableValues, new Vector2Int(position.x, position.y), new Vector2Int(currentPosition.x, currentPosition.y), 1000, enemyPositions));
                    var distance = routeToEnemy.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                    priorityPath = distance;
                    positionToAttack = position;
                    route = routeToEnemy;
                }
            }
            else
            {
                foreach(var position in availablePositions)
                {                       
                    var routeToEnemy = await Task.Run(() => astar.CreatePath(gridValues, walkableValues, new Vector2Int(position.x, position.y), new Vector2Int(enemyPosition.x, enemyPosition.y), 1000, enemyPositions));
                    var distance = routeToEnemy.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                    if(priorityPath > distance)
                    {
                        priorityPath = distance;
                        positionToAttack = position;
                    }
                }
            }
            if(item.IsInRange)
            {
                item.PriorityValue += priorityPath;
                item.PriorityValue -= enemy.HP;
            }
            else
            {
                item.PriorityValue -= priorityPath + enemy.HP;
            }

            item.PositionToAttack = positionToAttack;
            item.Route = route;
            priorityItems.Add(item);
        }

        var priorityItem = priorityItems.OrderByDescending(x => x.PriorityValue).First();
        var dis = priorityItem.Route.Skip(1).Count();

        for(int i = 0; i < dis; i++)
        {
            ChargeBonus += UnityEngine.Random.Range(0, 3);
        }

        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;

        return new AiDecision
        {
            IsInRange = priorityItem.IsInRange,
            Enemy = priorityItem.Enemy,
            PositionToAttack = priorityItem.PositionToAttack,
            EnemyPosition = tilemap.WorldToCell(priorityItem.Enemy.transform.position),
            Route = priorityItem.Route
        }; 
    }

    public bool CheckIfEnoughSpace(Tilemap tilemap, int[,] walkableValues, Vector3Int position)
    {
        var neighbourTile1 = new Vector3Int(position.x - 1, position.y, position.z);
        var neighbourTile2 = new Vector3Int(position.x - 1, position.y, position.z);
        if(!tilemap.HasTile(neighbourTile1) || walkableValues[neighbourTile1.x, neighbourTile1.y] == 0) return false;
        if(!tilemap.HasTile(neighbourTile2) || walkableValues[neighbourTile2.x, neighbourTile2.y] == 0) return false;
        return true;
    }    
}
