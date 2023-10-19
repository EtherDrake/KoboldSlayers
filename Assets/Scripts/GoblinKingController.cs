using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

public class GoblinKingController : EnemyController
{
    public AudioClip AttackSound;
    public GameObject Bomb;

    // Start is called before the first frame update
    protected override void Start()
    {      
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
        
        Bomb = (GameObject)Resources.Load("Bomb");
        
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag) 
    {
        var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Contains(targetTile)).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);
        if(targetEnemy != null)
        {
            return Slash(tilemap, targetTile, targetMovePosition, targetTag);
        }
        else
        {
            return PlantBomb(tilemap, targetTile, targetMovePosition);
        }
    }

    public override void InitAbilites()
    {
        
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

    private IEnumerator PlantBomb(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition)
    {
        var targetPosition = new Vector3(tilemap.CellToWorld(targetTile).x + 0.5f, tilemap.CellToWorld(targetTile).y + 0.75f, 0f);
        if ((targetPosition.x < targetMovePosition.x && FacingRight) || (targetPosition.x > targetMovePosition.x && !FacingRight))
        {
            Flip();
        }
        var bomb = Instantiate(Bomb, targetPosition, Quaternion.identity);
        
        Animator.SetTrigger("Plant");
        yield break;
    }

    public override async Task<AiDecision> TakeTurn(Tilemap tilemap, List<Vector3Int> availablePositions, List<BaseCharacterController> enemies, Astar astar, Vector3Int[,] gridValues, int[,] walkableValues)
    {
        var allyPositions = GameObject.FindGameObjectsWithTag(tag).Where(e => e.GetComponent<BaseCharacterController>().IsAlive).Select(e => tilemap.WorldToCell(e.transform.position)).ToList();
        var enemyPositions = enemies.Select(e => tilemap.WorldToCell(e.transform.position)).ToList();
        var currentPosition =  tilemap.WorldToCell(transform.position);

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
            if(!priorityItem.IsInRange)
            {
                var tileToPlant = availablePositions.FirstOrDefault(t => Mathf.Abs(priorityItem.PositionToAttack.x - t.x) <= 1 && Mathf.Abs(priorityItem.PositionToAttack.y - t.y) <= 1);
                if(tileToPlant != null)
                {
                    return new AiDecision
                    {
                        IsInRange = true,
                        Enemy = priorityItem.Enemy,
                        PositionToAttack = priorityItem.PositionToAttack,
                        EnemyPosition = tileToPlant,
                        Route = priorityItem.Route
                    };
                }                
            }

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
