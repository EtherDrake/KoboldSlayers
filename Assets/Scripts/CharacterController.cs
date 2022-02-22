using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class CharacterController : BaseCharacterController
{
    //Objects:
    public Transform SpellcastingPoint;
    public GameObject FireboltProjectile;
    public GameObject AcidSplashProjectile;  
    public GameObject MagicMissileProjectile;  
    public GameObject MagicShield;  

    public GameObject ZombieKobold;
    public GameObject Zombieraptor;
    public GameObject Zombierider;

    //Audio:
    public AudioClip FireboltSound;
    public AudioClip AcidSplashSound;
    public AudioClip MagicMissileSound;
    public AudioClip ShieldSound;

    private int ultCharges = 1;
    public override bool canUlt => ultCharges > 0;
    public override bool IsAiControlled => false;


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

        
        ZombieKobold = (GameObject)Resources.Load("ZombieKobold");
        Zombieraptor = (GameObject)Resources.Load("Zombieraptor");
        Zombierider = (GameObject)Resources.Load("Zombierider");
        
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag) 
    {   
        Actions -= 1;
        return Firebolt(tilemap, targetTile, targetTag);
    }

    public override IEnumerator Skill1(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string targetTag) 
    {
        Actions -= 1;
        return AcidSplash(tilemap, targetTiles, targetTag);
    }

    public override IEnumerator Skill2(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string targetTag) 
    {
        Actions -= 1;
        return MagicMissile(tilemap, targetTiles, targetTag);
    }    

    public override IEnumerator Skill3(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string targetTag) 
    {   
        Actions -= 1;
        return KyosRiposte(tilemap, targetTiles, targetTag);
    }  

    public override IEnumerator Skill4(Tilemap tilemap, List<Vector3Int> targetTiles, Vector3 targetMovePosition, string targetTag) 
    {
        Actions -= 1;
        return Necromancy(tilemap, targetTiles, targetTag);
    } 

    public override void InitAbilites()
    {
        AutoAttackProperties = new AbilityProperties
        {
            IsRanged = true,
            TargetsEnemies = true,
            IsInstant = false,
            IsAOE = false,
            Range = 5,
            NumberOfTargets = 1,
            Radius = 0
        };

        Skill1Properties = new AbilityProperties
        {
            IsRanged = true,
            TargetsEnemies = true,
            IsInstant = false,
            IsAOE = true,
            Range = 5,
            NumberOfTargets = 1,
            Radius = 1,
            SkillNumber = 1,
            Name = "Acid splash",
            Description = "Hurls a bubble of acid, dealing 1d6 damage to all enemies within area of effect"
        };

        Skill2Properties = new AbilityProperties
        {
            IsRanged = true,
            TargetsEnemies = true,
            IsInstant = false,
            IsAOE = false,
            Range = 5,
            Radius = 1,
            NumberOfTargets = 3,
            SkillNumber = 2,
            Name = "Magic missile",
            Description = "Creates three darts of magical force. Each dart hits a creature of your and deals 1d4 force damage to its target."
        };

        Skill3Properties = new AbilityProperties
        {
            IsRanged = true,
            TargetsEnemies = false,
            IsInstant = false,
            IsAOE = false,
            Range = 5,
            Radius = 1,
            NumberOfTargets = 1,
            SkillNumber = 3,            
            Name = "Kyo's magic riposte",
            Description = "Covers the target in shield of magical force. Negates one attack, after which shield shatters and deals 1d10 damage to the attacker"
        };

        Skill4Properties = new AbilityProperties
        {
            IsRanged = true,
            TargetsEnemies = true,
            IsInstant = false,
            IsAOE = false,
            Range = 5,
            NumberOfTargets = 1,
            Radius = 1,
            SkillNumber = 4,
            TargetsDead = true,            
            Name = "Animate dead",
            Description = "Imbues the target corpse with a foul mimicry of life, raising it as an Undead creature. Reduces your max hp until the undead servant dies."
        };
    }

    public override void StartTurn()
    {
        base.StartTurn();
    }

    private IEnumerator Firebolt(Tilemap tilemap, Vector3Int targetTile, string targetTag)
    {
        var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Contains(targetTile)).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);

        //Finding direction towards enemy
        var targetPosition = targetEnemy.transform.position;
        var targetDirection = (targetPosition - SpellcastingPoint.position).normalized;
        

        //Calculating angle of projectile
        var dir = targetPosition - SpellcastingPoint.position;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        //Fliping character (if necessary)
        if ((targetPosition.x < transform.position.x && FacingRight) || (targetPosition.x > transform.position.x && !FacingRight))
        {
            Flip();
        }

        //Rolling for an attack
        var roll = Random.Range(1, 20);
        var attack = roll + Wits + Proficiency;
        var kobold = targetEnemy.GetComponent<BaseCharacterController>();
        var hit = attack > kobold.AC || roll == 20;
        int damage = 0;
        if(hit)
        {
            damage = Random.Range(1, 10) + Wits;
            if(roll == 20)
            {
                damage += Random.Range(1, 10);
            }
            kobold.TakeDamage(damage);
        }
        
        AudioSource.PlayOneShot(MagicMissileSound);
        Animator.SetTrigger("Attack");

        //Sending projectile
        yield return new WaitForSeconds(0.40f); //Waiting for the right frame to spawn projectile
        var proj = Instantiate(FireboltProjectile, SpellcastingPoint.position, rotation);

        proj.GetComponent<Projectile>().targetTiles = targetEnemy.GetComponent<BaseCharacterController>().Position;
        proj.GetComponent<Projectile>().tilemap = tilemap;
        proj.GetComponent<Projectile>().hit = hit;
        proj.GetComponent<Projectile>().target = targetEnemy;
        proj.GetComponent<Projectile>().damage = damage;
        proj.GetComponent<Rigidbody2D>().velocity = targetDirection * 10f;
    }    

    private IEnumerator AcidSplash(Tilemap tilemap, List<Vector3Int> targetTiles, string targetTag)
    {       
        foreach(var targetTile in targetTiles)
        {
            var targetEnemies = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Any(y => Mathf.Abs(y.x - targetTile.x) < 2 && Mathf.Abs(y.y - targetTile.y) < 2)
                && x.GetComponent<BaseCharacterController>().IsAlive);

            //Finding target position
            var targetPosition = new Vector3(tilemap.CellToWorld(targetTile).x + 0.5f, tilemap.CellToWorld(targetTile).y + 0.75f, 0f);

            //Finding direction towards target position
            var targetDirection = (targetPosition - SpellcastingPoint.position).normalized;

            //Calculating angle of projectile
            var dir = targetPosition - SpellcastingPoint.position;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.AngleAxis(angle, Vector3.forward); 

            //Fliping character (if necessary)
            if ((targetPosition.x < transform.position.x && FacingRight) || (targetPosition.x > transform.position.x && !FacingRight))
            {
                Flip();
            }

            Dictionary<GameObject, int> damagedEnemies = new Dictionary<GameObject, int>();
            List<GameObject> dodgedEnemies = new List<GameObject>();

            //Damaging enemies
            foreach(var enemy in targetEnemies)
            {
                var kobold = enemy.GetComponent<BaseCharacterController>();
                var roll = Random.Range(1, 20) + kobold.Fineese;
                if(roll < 8 + Wits + Proficiency)
                {
                    var damage = Random.Range(1, 6);
                    kobold.TakeDamage(damage);
                    damagedEnemies.Add(enemy, damage);                
                }
                else 
                {
                    dodgedEnemies.Add(enemy);
                }
            }

            Animator.SetTrigger("Attack");
            yield return new WaitForSeconds(0.40f);

            
            AudioSource.PlayOneShot(AcidSplashSound);
            var proj = Instantiate(AcidSplashProjectile, SpellcastingPoint.position, rotation);

            proj.GetComponent<AcidSplashProjectile>().targetTile = targetTile;
            proj.GetComponent<AcidSplashProjectile>().tilemap = tilemap;
            proj.GetComponent<AcidSplashProjectile>().targets = damagedEnemies;
            proj.GetComponent<AcidSplashProjectile>().dodged = dodgedEnemies;
            proj.GetComponent<Rigidbody2D>().velocity = targetDirection * 10f;
        }
        
        targetTiles.Clear();
        Skill1Properties.Cooldown = 1;
    }

    private IEnumerator MagicMissile(Tilemap tilemap, List<Vector3Int> targetTiles, string targetTag)
    {
        foreach(var targetTile in targetTiles)
        {
            var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Contains(targetTile)).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);
            if(targetEnemy == null)
            {
                continue;
            }

            //Finding direction towards enemy
            var targetPosition = targetEnemy.transform.position;
            var targetDirection = (targetPosition - SpellcastingPoint.position).normalized;
            

            //Calculating angle of projectile
            var dir = targetPosition - SpellcastingPoint.position;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            //Fliping character (if necessary)
            if ((targetPosition.x < transform.position.x && FacingRight) || (targetPosition.x > transform.position.x && !FacingRight))
            {
                Flip();
            }

            //Rolling for an attack
            var kobold = targetEnemy.GetComponent<BaseCharacterController>();

            var damage = Random.Range(1, 4);
            kobold.TakeDamage(damage);

            AudioSource.PlayOneShot(MagicMissileSound);
            Animator.SetTrigger("Attack");

            //Sending projectile
            yield return new WaitForSeconds(0.40f); //Waiting for the right frame to spawn projectile
            var proj = Instantiate(MagicMissileProjectile, SpellcastingPoint.position, rotation);

            proj.GetComponent<MagicMissileController>().targetTiles = targetEnemy.GetComponent<BaseCharacterController>().Position;
            proj.GetComponent<MagicMissileController>().tilemap = tilemap;
            proj.GetComponent<MagicMissileController>().target = targetEnemy;
            proj.GetComponent<MagicMissileController>().damage = damage;
            proj.GetComponent<Rigidbody2D>().velocity = targetDirection * 10f;
        }
        
        targetTiles.Clear();
        Skill2Properties.Cooldown = 2;
    }

    private IEnumerator KyosRiposte(Tilemap tilemap, List<Vector3Int> targetTiles, string targetTag)
    {
        foreach(var targetTile in targetTiles)
        {            
            AudioSource.PlayOneShot(ShieldSound);
            Animator.SetTrigger("Attack");
            var targetAlly = GameObject.FindGameObjectsWithTag(targetTag).Where(x => tilemap.WorldToCell(x.transform.position) == targetTile).FirstOrDefault(x => x.GetComponent<BaseCharacterController>().IsAlive);
            if(targetAlly == null)
            {
                continue;
            }
                
            var shield = Instantiate(MagicShield, targetAlly.transform.position, Quaternion.identity);
            shield.transform.parent = targetAlly.transform;
        }
        
        targetTiles.Clear();        
        Skill3Properties.Cooldown = 3;
        yield return new WaitForEndOfFrame();
    }

    

    private IEnumerator WickedSummon(Tilemap tilemap, List<Vector3Int> targetTiles, string targetTag)
    {
        foreach(var targetTile in targetTiles)
        {            
            AudioSource.PlayOneShot(ShieldSound);
            Animator.SetTrigger("Attack");
            
            var targetPosition = new Vector3(tilemap.CellToWorld(targetTile).x + 0.5f, tilemap.CellToWorld(targetTile).y + 0.75f, 0f);

            var goblinKing = (GameObject)Resources.Load("GoblinKing");
            var gk = Instantiate(goblinKing, targetPosition, Quaternion.identity);
        }
        
        targetTiles.Clear();
        ultCharges--;
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator Necromancy(Tilemap tilemap, List<Vector3Int> targetTiles, string targetTag)
    {
        foreach(var targetTile in targetTiles)
        {
            var targetEnemy = GameObject.FindGameObjectsWithTag(targetTag).Where(x => x.GetComponent<BaseCharacterController>().Position.Contains(targetTile)).FirstOrDefault(x => !x.GetComponent<BaseCharacterController>().IsAlive);
            if(targetEnemy == null)
            {
                continue;
            }

            var enemyController = targetEnemy.GetComponent<BaseCharacterController>();

            if(enemyController.GetType().IsAssignableFrom(typeof(VelociriderController)))
            {
                var zombierider = Instantiate(Zombierider, targetEnemy.transform.position, Quaternion.identity);
                zombierider.GetComponent<ZombieriderController>().Master = this;
                MaxHP -= 2;                
            }
            else if(enemyController.GetType().IsAssignableFrom(typeof(VelociraptorController)))
            {
                var zombierider = Instantiate(Zombieraptor, targetEnemy.transform.position, Quaternion.identity);
                zombierider.GetComponent<ZombieriderController>().Master = this;
                MaxHP -= 2;                
            }
            else if (enemyController.GetType().IsAssignableFrom(typeof(KoboldController)))
            {
                var zombiekobold = Instantiate(ZombieKobold, targetEnemy.transform.position, Quaternion.identity);
                zombiekobold.GetComponent<ZombieKoboldController>().Master = this;                
                MaxHP -= 1;
            }

            GameObject.Destroy(targetEnemy);

            AudioSource.PlayOneShot(ShieldSound);
            Animator.SetTrigger("Attack");
            
            if(HP > MaxHP)
            {
                HP = MaxHP;
            }
        }
        
        targetTiles.Clear();
        Skill4Properties.Cooldown = 2;
        yield return new WaitForEndOfFrame();
    }          
}
