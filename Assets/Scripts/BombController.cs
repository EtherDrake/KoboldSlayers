using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Threading.Tasks;

public class BombController : EnemyController
{
    public int _timer = 2;    
    public AudioClip ExplosionSound;

    // Start is called before the first frame update
    void Start()
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
        yield return new WaitForSeconds(0.40f);
    }   

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }

    public override void GetHit(BaseCharacterController attacker, int damage)
    {
        base.GetHit(attacker, damage);        
        if(!IsAlive)
        {               
            Destroy(gameObject);
        }
    }

    public override async Task<AiDecision> TakeTurn(Tilemap tilemap, List<Vector3Int> availablePositions, List<BaseCharacterController> enemies, Astar astar, Vector3Int[,] gridValues, int[,] walkableValues)
    {
        _timer--;
        Debug.Log($"Reducing timer. Currently at {_timer}");
        var currentPosition =  tilemap.WorldToCell(transform.position);

        if(_timer == 0)
        {
            var enemyPositions = enemies.Select(e => new {enemy = e, position = tilemap.WorldToCell(e.transform.position)}).ToList();

            var closeEnemies = enemyPositions.Where(x => Mathf.Abs(x.position.x - currentPosition.x) < 3 && Mathf.Abs(x.position.y - currentPosition.y) < 3).ToList();
            foreach(var enemy in closeEnemies)
            {
                var save = UnityEngine.Random.Range(1, 20) + enemy.enemy.Fineese;
                var damage = 0;
                for(int i = 0; i < 3; i++)
                {
                    var damageRoll = UnityEngine.Random.Range(1, 6);
                    damage += damageRoll;
                }

                if(save >= 12)
                {
                    damage = damage / 2;
                }

                enemy.enemy.TakeDamage(damage);
                enemy.enemy.GetHit(this, damage);
            }

            
            AudioSource.PlayOneShot(ExplosionSound);
            Animator.SetTrigger("Explode");      
            Destroy(gameObject, 1.15f);
        }

        return new AiDecision 
        {
            PositionToAttack = currentPosition,
            IsInRange = false
        };
    }    
}
