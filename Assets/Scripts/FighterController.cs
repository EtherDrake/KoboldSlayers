using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class FighterController : BaseCharacterController
{

    public GameObject MissMessage;
    public bool IsParrying;
    public override bool IsAiControlled => false;

    int advantage;

    public AudioClip SwordSlash;

    // Start is called before the first frame update
    protected override void Start()
    {        
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
        FacingRight = true;

        if(!IsInitAbilities)
        {
            InitAbilites();
        }
        
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }    

    public override IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string tag) 
    {
        Actions -= 1;
        return Slash(tilemap, targetTile, targetMovePosition, tag);
    }

    public override IEnumerator Skill1(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string tag) 
    {
        return SecondWind();
    }

    public override IEnumerator Skill2(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string tag) 
    {
        Actions -= 1;
        return ParryStance();
    }

    public override IEnumerator Skill3(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string tag) 
    {        
        Actions -= 1;
        return Terrify(targetTiles, tag);
    }

    public override IEnumerator Skill4(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string tag) 
    {
        return SteadyStrike();
    }

    public override void StartTurn()
    {
        base.StartTurn();
        if(IsParrying)
        {
            IsParrying = false;
            BaseSpeed = 5;
            Animator.SetBool("InStance", false);            
        }             
        advantage = 0;
    }

    public override void TakeDamage(int damage)
    {
        if(IsParrying)
        {
            damage -= 10;           
        }
        if(damage < 0 )
        {
            damage = 0;
        } 

        base.TakeDamage(damage);
    }

    public override void GetHit(BaseCharacterController attacker, int damage)
    {
        if(IsParrying)
        {
            var notificationPosition = transform.position;
            var xOffset = Random.Range(0, 20) * 0.01f;
            var yOffset = Random.Range(0, 20) * 0.01f;
            notificationPosition.x += 0.25f + xOffset;
            notificationPosition.y += 0.50f + yOffset;

            var damageNotification = Instantiate(DamageNotification, notificationPosition, Quaternion.identity);
            damageNotification.GetComponent<TextMesh>().text = "Parry!";
            damageNotification.GetComponent<MeshRenderer>().sortingOrder = 50; 

            var roll = Random.Range(1, 20);
            var attack = roll + Athletics + Proficiency + advantage;
            var kobold = attacker;
            var hit = attack > kobold.AC || roll == 20;
            if(hit)
            {
                int dices = roll == 20 ? 4 : 2;
                var dmg = Athletics;

                for(int i = 0; i < dices; i++)
                {
                    var damageRoll = Random.Range(1, 6);
                    if(damageRoll < 3)
                    {
                        damageRoll = Random.Range(1, 6);
                    }
                    dmg += damageRoll;
                }
                
                kobold.TakeDamage(dmg);
                kobold.GetHit(this, dmg);
            }
            else
            {
                var missMessagePosition = new Vector3(attacker.transform.position.x + 0.275f, attacker.transform.position.y + 0.25f, attacker.transform.position.z);
                Instantiate(MissMessage, missMessagePosition, Quaternion.identity);
            }
        
            AudioSource.PlayOneShot(SwordSlash);
            Animator.SetTrigger("Hit");
        }
        else
        {
            base.GetHit(attacker, damage);
        }
    }

    public override void InitAbilites()
    {
        AutoAttackProperties = new AbilityProperties
        {
            IsRanged = false,
            TargetsEnemies = true,
            IsInstant = false,
            IsAOE = false,
            Range = 1
        };        

        Skill1Properties = new AbilityProperties
        {
            IsRanged = false,
            TargetsEnemies = false,
            IsInstant = true,
            IsAOE = false,
            Range = 0,
            SkillNumber = 1,
            ActionCost = 0,
            
            Name = "Action surge",
            Description = "Doubles movement and number of attacks for current turn."
        }; 

        Skill2Properties = new AbilityProperties
        {
            IsRanged = false,
            TargetsEnemies = false,
            IsInstant = true,
            IsAOE = false,
            Range = 0,            
            SkillNumber = 2,
            
            Name = "Parry Stance",
            Description = "Assumes parrying stance, reducing incoming damage and counterattacking all incoming attacks. Lasts untill next turn"
        };

        Skill3Properties = new AbilityProperties
        {
            IsRanged = false,
            TargetsEnemies = true,
            IsInstant = true,
            IsAOE = true,
            Range = 0,
            Radius = 2,
            SkillNumber = 3,
            
            Name = "Terrify",
            Description = "Scares off all nearby enemies"
        };

        Skill4Properties = new AbilityProperties
        {
            IsRanged = false,
            TargetsEnemies = false,
            IsInstant = true,
            IsAOE = false,
            Range = 0,
            SkillNumber = 4,
            ActionCost = 0,

            Name = "Steady Strike",
            Description = "Consumes one movement point and adds +1 to attack rolls untill the next turn"
        };
    }

    private IEnumerator Slash(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag)
    {
        if(!IsParrying)
        {
            var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).FirstOrDefault(e => e.GetComponent<BaseCharacterController>().Position.Contains(targetTile) &&  e.GetComponent<BaseCharacterController>().IsAlive);
            var targetPosition = targetEnemy.transform.position;
            if ((targetPosition.x < targetMovePosition.x && FacingRight) || (targetPosition.x > targetMovePosition.x && !FacingRight))
            {
                Flip();
            }

            //Rolling for an attack
            var roll = Random.Range(1, 20);

            var attack = roll + Athletics + Proficiency + advantage;
            var kobold = targetEnemy.GetComponent<BaseCharacterController>();
            var hit = attack > kobold.AC || roll == 20;
            if(hit)
            {
                int dices = roll == 20 ? 4 : 2;
                var damage = Athletics;

                for(int i = 0; i < dices; i++)
                {
                    var damageRoll = Random.Range(1, 6);
                    if(damageRoll < 3)
                    {
                        damageRoll = Random.Range(1, 6);
                    }
                    damage += damageRoll;
                }
                
                kobold.TakeDamage(damage);
                kobold.GetHit(this, damage);
            }
            else
            {
                var missMessagePosition = new Vector3(targetPosition.x + 0.275f, targetPosition.y + 0.25f, targetPosition.z);
                Instantiate(MissMessage, missMessagePosition, Quaternion.identity);
            }
            
            AudioSource.PlayOneShot(SwordSlash);
            Animator.SetTrigger("Attack");
        }
        yield break;
    }

    private IEnumerator SecondWind()
    {
        Actions += 1;
        Speed += BaseSpeed;
        Skill1Properties.Cooldown = 2;
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator ParryStance()
    {
        IsParrying = true;
        Skill2Properties.Cooldown = 2;
        Speed = 0;
        BaseSpeed = 0;
        Actions -= 1;
        Animator.SetBool("InStance", true);
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator Terrify(List<Vector3Int> targetTiles, string targetTag)
    {       
        Skill3Properties.Cooldown = 3;
        Actions -= 1;

        var targetEnemies = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Any(y => targetTiles.Any(z => z.x == y.x && z.y == y.y))
                && x.GetComponent<BaseCharacterController>().IsAlive);        
        
        foreach(var enemy in targetEnemies)
        {
            enemy.GetComponent<EnemyController>().Morale -= 20;
        }

        Animator.SetTrigger("ScarePose");
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator SteadyStrike()
    {
        if(Speed > 0)
        {
            Speed--;
            advantage++;
        }
        yield return new WaitForEndOfFrame();
    }
}
