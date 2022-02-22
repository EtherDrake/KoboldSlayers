using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicShield : MonoBehaviour
{
    public Animator Animator;
    public AudioSource AudioSource;
    public GameObject Sword;
    public AudioClip ShieldShatter;


    // Start is called before the first frame update
    void Start()
    {
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
        AudioSource.volume = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Shatter(BaseCharacterController target)
    {
        Animator.SetTrigger("Shatter");
        AudioSource.PlayOneShot(ShieldShatter);
        yield return new WaitForSeconds(0.50f);

        var targetPosition = target.transform.position;
        var targetDirection = (targetPosition - transform.position).normalized;
        

        //Calculating angle of projectile
        var dir = targetPosition - transform.position;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        //Rolling for an attack
        var damage = Random.Range(1, 10);
        target.TakeDamage(damage);

        //Sending projectile
        yield return new WaitForSeconds(0.40f); //Waiting for the right frame to spawn projectile
        var proj = Instantiate(Sword, transform.position, rotation);

        proj.GetComponent<SwordProjectile>().target = target;
        proj.GetComponent<Rigidbody2D>().velocity = targetDirection * 10f;
        proj.GetComponent<SwordProjectile>().damage = damage;

        Destroy(gameObject);
    }
}
