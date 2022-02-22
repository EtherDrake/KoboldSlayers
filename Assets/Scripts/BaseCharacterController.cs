using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public abstract class BaseCharacterController : MonoBehaviour
{
    //Stats:
    public int Athletics;
    public int Fineese;
    public int Wits;
    public int Proficiency;

    public int MaxHP;
    public int HP;
    public int AC;
    public int BaseSpeed;
    public int Speed;
    public int Actions;    
    public bool IsAlive = true;
    public int Size;
    public List<Vector3Int> Position; 
    public virtual bool IsAiControlled => true;

    public int potionCharges = 3;
    public virtual bool canUlt => false;

    //Objects:
    public Animator Animator;
    public GameObject DamageNotification;

    //Images:
    public Sprite Portrait;
    public Sprite Ability1Icon;
    public Sprite Ability2Icon;
    public Sprite Ability3Icon;
    public Sprite Ability4Icon;

    //Audio:
    public AudioSource AudioSource;


    //Parameters:
    public bool FacingRight;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        IsAlive = true;
        AudioSource.volume = PlayerPrefs.GetFloat("SFXVol", 1f);
        DamageNotification = (GameObject)Resources.Load("DamageNotification");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public abstract IEnumerator AutoAttack(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, string targetTag);
    public AbilityProperties AutoAttackProperties = new AbilityProperties();

    public abstract IEnumerator Skill1(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag);
    public AbilityProperties Skill1Properties = new AbilityProperties();

    public abstract IEnumerator Skill2(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag);
    public AbilityProperties Skill2Properties = new AbilityProperties();

    public abstract IEnumerator Skill3(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag);
    public AbilityProperties Skill3Properties = new AbilityProperties();

    public abstract IEnumerator Skill4(Tilemap tilemap, List<Vector3Int> targetTile, Vector3 targetMovePosition, string targetTag);
    public AbilityProperties Skill4Properties = new AbilityProperties();

    public bool IsInitAbilities = false;
    public abstract void InitAbilites();


    //Rotate character
    public void Flip()
    {        
        FacingRight = !FacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    public virtual void TakeDamage(int damage)
    {
        MagicShield shield = gameObject.GetComponentInChildren(typeof(MagicShield)) as MagicShield;
        if(shield != null)
        {
            damage = 0;
        }
        HP-=damage;
        

        if(HP <= 0)
        {
            HP = 0;
            IsAlive = false;
        }        
    }

    public virtual void StartTurn()
    {
        if(Skill1Properties.Cooldown > 0 )
            Skill1Properties.Cooldown--;
        if(Skill2Properties.Cooldown > 0 )
            Skill2Properties.Cooldown--;
        if(Skill3Properties.Cooldown > 0 )
            Skill3Properties.Cooldown--;
        if(Skill4Properties.Cooldown > 0 )
            Skill4Properties.Cooldown--;                
    }

    public virtual void GetHit(BaseCharacterController attacker, int damage)
    {
        GetComponent<Animator>().SetTrigger("Hit");
        MagicShield shield = gameObject.GetComponentInChildren(typeof(MagicShield)) as MagicShield;

        if(shield == null)
        {
            var notificationPosition = transform.position;
            var xOffset = Random.Range(0, 20) * 0.01f;
            var yOffset = Random.Range(0, 20) * 0.01f;
            notificationPosition.x += 0.25f + xOffset;
            notificationPosition.y += 0.50f + yOffset;

            var damageNotification = Instantiate(DamageNotification, notificationPosition, Quaternion.identity);
            damageNotification.GetComponent<TextMesh>().text = damage.ToString();
            damageNotification.GetComponent<MeshRenderer>().sortingOrder = 50; 
        }

        if(!IsAlive)
        {
            GetComponent<Animator>().SetBool("Dead", true);
        }        
        
        if(shield != null && attacker != null)
        {
            StartCoroutine(shield.Shatter(attacker));
        }
    }     

    public virtual void GetPosition(Tilemap tilemap)
    {
        Vector3Int initialTile = tilemap.WorldToCell(transform.position);
        if(Size == 2)
        {
            initialTile.y -= 1;
        }
        Position = new List<Vector3Int>
        {
            initialTile
        };

        for(int i = 1; i < Size; i++)
        {
            Position.Add(new Vector3Int(initialTile.x - i, initialTile.y, initialTile.z));
        }
    }
}

public class Ability
{
    public Sprite Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public AbilityProperties Properties { get; set; }
    public delegate IEnumerator Action(Tilemap tilemap, Vector3Int targetTile, Vector3 targetMovePosition, List<GameObject> targetEnenmies);
}

public class AbilityProperties
{
    public bool IsRanged { get; set; }
    public bool TargetsEnemies { get; set; }
    public bool IsAOE { get; set; }
    public bool IsInstant { get; set; }
    public int Range{ get; set; }
    public int Radius { get; set; }
    public int NumberOfTargets { get; set; }
    public int SkillNumber { get; set; }
    public int Cooldown { get; set; }
    public int ActionCost { get; set; } = 1;
    public bool TargetsDead { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
}

