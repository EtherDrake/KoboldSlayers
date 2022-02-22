using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

public class KoboldController : EnemyController
{
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

    public override void InitAbilites()
    {
        
    }
    
    public override IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        return Slash(tilemap, targetTile, targetMovePosition, targetTag);
    }

    private IEnumerator Slash(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag)
    { 
        var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => tilemap.WorldToCell(x.transform.position) == targetTile).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);               
        var targetPosition = targetEnemy.transform.position;
        if ((targetPosition.x < targetMovePosition.x && FacingRight) || (targetPosition.x > targetMovePosition.x && !FacingRight))
        {
            Flip();
        }

        //Rolling for an attack
        var roll = UnityEngine.Random.Range(1, 20);
        var attack = roll + Fineese + Proficiency;
        var enemy = targetEnemy.GetComponent<BaseCharacterController>();
        var hit = attack > enemy.AC || roll == 20;
        if(hit)
        {
            int dices = roll == 20 ? 2 : 1;
            var damage = Fineese;

            for(int i = 0; i < dices; i++)
            {
                var damageRoll = UnityEngine.Random.Range(1, 6);
                damage += damageRoll;
                if(damage < 0)
                    damage = 0;
            }

            enemy.TakeDamage(damage);
            enemy.GetHit(this, damage);
        }
        else
        {
            var missMessagePosition = new Vector3(targetPosition.x + 0.35f, targetPosition.y + 0.55f, targetPosition.z);
            var missMessage = Instantiate(MissMessage, missMessagePosition, Quaternion.identity);            
            missMessage.GetComponent<MeshRenderer>().sortingOrder = 50; 
        }
        
        AudioSource.PlayOneShot(AttackSound);
        Animator.SetTrigger("Attack");
        yield break;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        Morale -= 10;
    }    

    public override async Task<AiDecision> TakeTurn(Tilemap tilemap, List<Vector3Int> availablePositions, List<BaseCharacterController> enemies, Astar astar, Vector3Int[,] gridValues, int[,] walkableValues)
    {
        var allyPositions = GameObject.FindGameObjectsWithTag(tag).Where(e => e.GetComponent<EnemyController>().IsAlive).Select(e => tilemap.WorldToCell(e.transform.position)).ToList();
        var enemyPositions = enemies.Select(e => tilemap.WorldToCell(e.transform.position)).ToList();
        var currentPosition =  tilemap.WorldToCell(transform.position);


        var alliesNearby = allyPositions.Count(a => a != currentPosition && Mathf.Abs(a.x - currentPosition.x) < 2 && Mathf.Abs(a.y - currentPosition.y) < 2);
        Morale += 2 * alliesNearby;
        if(Morale > 10)
        {
            Morale = 10;
        }

        if(Morale < 0)
        {
            var shortestPath = 10000;
            Vector3Int closestEnemy = new Vector3Int();

            foreach(var enemy in enemies)
            {
                var enemyPosition = tilemap.WorldToCell(enemy.transform.position);
                var routeToEnemy = await Task.Run(() => astar.CreatePath(gridValues, walkableValues, new Vector2Int(enemyPosition.x, enemyPosition.y), new Vector2Int(currentPosition.x, currentPosition.y), 1000, enemyPositions));
                var distance = routeToEnemy.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                if(shortestPath > distance)
                {
                    shortestPath = distance;
                    closestEnemy = enemyPosition;
                }
            }

            var furthestPath = 0;
            var positionToRetreat = new Vector3Int();
            foreach(var position in availablePositions)
            {                        
                var routeToEnemy = await Task.Run(() => astar.CreatePath(gridValues, walkableValues, new Vector2Int(closestEnemy.x, closestEnemy.y), new Vector2Int(position.x, position.y), 1000, enemyPositions));
                var distance = routeToEnemy.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                if(furthestPath < distance)
                {
                    furthestPath = distance;
                    positionToRetreat = position;
                }
            }

            Morale = 5;

            return new AiDecision
            {
                IsInRange = false,
                PositionToAttack = positionToRetreat,                
            };  
        }
        else
        {
            List<PriorityItem> priorityItems = new List<PriorityItem>();

            foreach(var enemy in enemies)
            {
                PriorityItem item = new PriorityItem
                {
                    Enemy = enemy,
                    PriorityValue = 100
                };

                var enemyPosition = tilemap.WorldToCell(enemy.transform.position);
                var positionsNextToEnemy = availablePositions.Where(t => Mathf.Abs(enemyPosition.x - t.x) <= 1 && Mathf.Abs(enemyPosition.y - t.y) <= 1);
                if(positionsNextToEnemy.Any(x => availablePositions.Contains(x)))
                {
                    item.PriorityValue += 10;
                    item.IsInRange = true;
                }

                var shortestPath = 10000;
                var positionToAttack = new Vector3Int();
                var route = new List<Spot>();
                if(item.IsInRange)
                {
                    if(positionsNextToEnemy.Contains(currentPosition))
                    {
                        shortestPath = 0;
                    }
                    else
                    {
                        var position = positionsNextToEnemy.FirstOrDefault();         
                        var routeToEnemy = await Task.Run(() => astar.CreatePath(gridValues, walkableValues, new Vector2Int(position.x, position.y), new Vector2Int(currentPosition.x, currentPosition.y), 1000, enemyPositions));
                        var distance = routeToEnemy.Skip(1).Sum(step => step.difficulty == 0 ? 1 : 2);
                        shortestPath = distance;
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
                        if(shortestPath > distance)
                        {
                            shortestPath = distance;
                            positionToAttack = position;
                        }
                    }
                }

                item.PriorityValue -= shortestPath + enemy.GetComponent<BaseCharacterController>().HP;
                item.PositionToAttack = positionToAttack;
                item.Route = route;
                priorityItems.Add(item);
            }

            var priorityItem = priorityItems.OrderByDescending(x => x.PriorityValue).First();

            return new AiDecision
            {
                IsInRange = priorityItem.IsInRange,
                Enemy = priorityItem.Enemy,
                PositionToAttack = priorityItem.PositionToAttack,
                EnemyPosition = tilemap.WorldToCell(priorityItem.Enemy.transform.position),
                Route = priorityItem.Route
            };           
        }
    }    
}

public class PriorityItem
{
    public BaseCharacterController Enemy { get; set; }
    public int PriorityValue { get; set; }
    public Vector3Int PositionToAttack { get; set; }
    public bool IsInRange { get; set; }
    public List<Spot> Route { get; set; }
}

public class AiDecision
{
    public bool IsInRange { get; set; }
    public Vector3Int PositionToAttack { get; set; }
    public Vector3Int EnemyPosition { get; set; }
    public BaseCharacterController Enemy { get; set; }
    public List<Spot> Route { get; set; }
}
